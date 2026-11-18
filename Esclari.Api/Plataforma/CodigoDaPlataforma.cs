using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record ResultadoCodigo(OperadorPlataforma? Operador, IResult Resposta);

public sealed record CodigoDaPlataforma(CodigoDaPlataforma.Entrada Corpo, EtapaDeEntrada? Etapa) : IRequest<ResultadoCodigo>
{
    public sealed record Entrada(string? Codigo);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/auth/codigo", async (Entrada i, HttpContext ctx, IMediator m, ProtecaoDaPlataforma protecao, TimeProvider relogio) =>
        {
            var r = await m.Send(new CodigoDaPlataforma(i, SessaoDaPlataforma.LerEtapa(ctx, protecao, relogio.GetUtcNow())));
            if (r.Operador is not { } operador) return r.Resposta;
            await ctx.SignInAsync(SessaoDaPlataforma.Esquema, SessaoDaPlataforma.Principal(operador, relogio.GetUtcNow()));
            SessaoDaPlataforma.ApagarEtapa(ctx);
            return r.Resposta;
        }).AllowAnonymous().RequireRateLimiting("auth");
}

public sealed class CodigoDaPlataformaHandler(EsclariDb db, TimeProvider relogio, ProtecaoDaPlataforma protecao) : IRequestHandler<CodigoDaPlataforma, ResultadoCodigo>
{
    public async ValueTask<ResultadoCodigo> Handle(CodigoDaPlataforma m, CancellationToken ct)
    {
        var codigo = m.Corpo.Codigo?.Trim();
        if (codigo is not { Length: Totp.Digitos } || !codigo.All(char.IsAsciiDigit)) return new(null, Problemas.Invalido("Informe os 6 dígitos do aplicativo.", "codigo"));
        var operador = m.Etapa is { } e ? await db.OperadoresPlataforma.FirstOrDefaultAsync(o => o.Id == e.OperadorId && o.Ativo, ct) : null;
        if (operador is null) return new(null, Problemas.Erro(401, SessaoDaPlataforma.SessaoEncerrada));

        var segredo = m.Etapa!.SegredoNovo ?? protecao.Segredo.Unprotect(operador.TotpSegredo!);
        var agora = relogio.GetUtcNow();
        if (Totp.Verificar(segredo, codigo, agora, operador.TotpUltimoPasso) is not { } passo) return new(null, Problemas.Erro(401, "Código inválido."));

        if (operador.TemSegundoFator) operador.RegistrarPasso(passo);
        else operador.CadastrarSegundoFator(protecao.Segredo.Protect(segredo), passo);
        db.AuditoriaDaPlataforma.Add(AuditoriaPlataforma.Registrar(AcoesDaPlataforma.OperadorEntrou, operador.Id, null, $"{operador.Nome} entrou no console", agora.UtcDateTime));
        await db.SaveChangesAsync(ct);
        return new(operador, Results.NoContent());
    }
}
