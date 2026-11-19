using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record DesativarMembro(string Slug, long Id, OperadorPlataforma Operador) : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/instituicoes/{slug}/equipe/{id:long}/desativar", (string slug, long id, HttpContext ctx, IMediator m) => m.Send(new DesativarMembro(slug, id, ctx.Operador())));
}

public sealed class DesativarMembroHandler(EsclariDb db, TimeProvider relogio) : IRequestHandler<DesativarMembro, IResult>
{
    public async ValueTask<IResult> Handle(DesativarMembro m, CancellationToken ct)
    {
        var instituicao = await EquipeDaInstituicao.Alvo(db, m.Slug, ct);
        if (instituicao is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        var usuario = await EquipeDaInstituicao.Membro(db, m.Id, ct);
        if (usuario is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        if (usuario is { Ativo: true } && usuario.EDoTipoDeSistema(TipoDeUsuario.Gestor) && !await OutroAdministradorAtivo(usuario.Id, ct))
            return Problemas.Erro(409, "Este é o último administrador ativo da instituição.");

        usuario.Desativar();
        db.AuditoriaDaPlataforma.Add(AuditoriaPlataforma.Registrar(AcoesDaPlataforma.UsuarioAlterado, m.Operador.Id, instituicao.Id, EquipeDaInstituicao.Resumo("Desativou", usuario), relogio.GetUtcNow().UtcDateTime));
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    Task<bool> OutroAdministradorAtivo(long id, CancellationToken ct) =>
        db.Usuarios.AnyAsync(u => u.Id != id && u.Ativo && u.Tipo!.Sistema == TipoDeUsuario.Gestor, ct);
}
