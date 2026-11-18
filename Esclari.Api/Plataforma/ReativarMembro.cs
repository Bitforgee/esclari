using Mediator;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record ReativarMembro(string Slug, long Id, OperadorPlataforma Operador) : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/instituicoes/{slug}/equipe/{id:long}/reativar", (string slug, long id, HttpContext ctx, IMediator m) => m.Send(new ReativarMembro(slug, id, ctx.Operador())));
}

public sealed class ReativarMembroHandler(EsclariDb db, TimeProvider relogio) : IRequestHandler<ReativarMembro, IResult>
{
    public async ValueTask<IResult> Handle(ReativarMembro m, CancellationToken ct)
    {
        var instituicao = await EquipeDaInstituicao.Alvo(db, m.Slug, ct);
        if (instituicao is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        var usuario = await EquipeDaInstituicao.Membro(db, m.Id, ct);
        if (usuario is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);

        usuario.Reativar();
        db.AuditoriaDaPlataforma.Add(AuditoriaPlataforma.Registrar(AcoesDaPlataforma.UsuarioAlterado, m.Operador.Id, instituicao.Id, EquipeDaInstituicao.Resumo("Reativou", usuario), relogio.GetUtcNow().UtcDateTime));
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
