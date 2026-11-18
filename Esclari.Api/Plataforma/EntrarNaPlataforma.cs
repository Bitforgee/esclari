using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record ResultadoEntrada(EtapaDeEntrada? Etapa, IResult Resposta);

public sealed record EntrarNaPlataforma(EntrarNaPlataforma.Entrada Corpo) : IRequest<ResultadoEntrada>
{
    public sealed record Entrada(string? Email, string? Senha);
    public sealed record Resposta(string Etapa, string? Segredo, string? Uri);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/auth/entrar", async (Entrada i, HttpContext ctx, IMediator m, ProtecaoDaPlataforma protecao) =>
        {
            var r = await m.Send(new EntrarNaPlataforma(i));
            if (r.Etapa is { } etapa) SessaoDaPlataforma.GravarEtapa(ctx, protecao, etapa);
            return r.Resposta;
        }).AllowAnonymous().RequireRateLimiting("auth");
}

public sealed class EntrarNaPlataformaHandler(EsclariDb db, IPasswordHasher<OperadorPlataforma> hasher, TimeProvider relogio) : IRequestHandler<EntrarNaPlataforma, ResultadoEntrada>
{
    const string CredenciaisInvalidas = "E-mail ou senha inválidos.";

    static readonly OperadorPlataforma Ficticio = OperadorPlataforma.Criar("-", "-", DateTime.UnixEpoch);
    static readonly string HashFicticio = new PasswordHasher<OperadorPlataforma>().HashPassword(Ficticio, "senha-ficticia");

    public async ValueTask<ResultadoEntrada> Handle(EntrarNaPlataforma m, CancellationToken ct)
    {
        var email = m.Corpo.Email?.Trim().ToLowerInvariant() ?? "";
        if (email == "") return new(null, Problemas.Invalido("Informe o e-mail.", "email"));
        if (string.IsNullOrEmpty(m.Corpo.Senha)) return new(null, Problemas.Invalido("Informe a senha.", "senha"));

        var operador = await db.OperadoresPlataforma.FirstOrDefaultAsync(o => o.Email == email, ct);
        var podeEntrar = operador is { Ativo: true, SenhaHash: not null };
        var confere = hasher.VerifyHashedPassword(operador ?? Ficticio, podeEntrar ? operador!.SenhaHash! : HashFicticio, m.Corpo.Senha) != PasswordVerificationResult.Failed;
        if (!podeEntrar || !confere) return new(null, Problemas.Erro(401, CredenciaisInvalidas));

        var expira = relogio.GetUtcNow() + SessaoDaPlataforma.DuracaoDaEtapa;
        if (operador!.TemSegundoFator) return new(new(operador.Id, expira, null), Results.Ok(new EntrarNaPlataforma.Resposta("CODIGO", null, null)));
        var segredo = Totp.NovoSegredo();
        return new(new(operador.Id, expira, segredo), Results.Ok(new EntrarNaPlataforma.Resposta("CADASTRAR", segredo, Totp.Uri(operador.Email, segredo))));
    }
}
