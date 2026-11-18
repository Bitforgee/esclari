using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Acesso;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record DefinirSenhaDaPlataforma(DefinirSenhaDaPlataforma.Entrada Corpo) : IRequest<IResult>
{
    public sealed record Entrada(string? Token, string? Senha);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/auth/definir-senha", (Entrada i, IMediator m) => m.Send(new DefinirSenhaDaPlataforma(i))).AllowAnonymous().RequireRateLimiting("auth");
}

public sealed class DefinirSenhaDaPlataformaHandler(EsclariDb db, IPasswordHasher<OperadorPlataforma> hasher, TimeProvider relogio) : IRequestHandler<DefinirSenhaDaPlataforma, IResult>
{
    const string LinkInvalido = "O link é inválido ou venceu. Peça um convite novo.";

    public async ValueTask<IResult> Handle(DefinirSenhaDaPlataforma m, CancellationToken ct)
    {
        var (token, senha) = (m.Corpo.Token ?? "", m.Corpo.Senha ?? "");
        if (!Senha.Valida(senha)) return Problemas.Invalido(Senha.ForaDoTamanho, "senha");
        var hash = Tokens.Hash(token);
        var operador = token == "" ? null : await db.OperadoresPlataforma.FirstOrDefaultAsync(o => o.ConviteHash == hash, ct);
        if (operador is null || !operador.ConviteValido(relogio.GetUtcNow().UtcDateTime)) return Problemas.Invalido(LinkInvalido, "token");

        operador.DefinirSenha(hasher.HashPassword(operador, senha));
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
