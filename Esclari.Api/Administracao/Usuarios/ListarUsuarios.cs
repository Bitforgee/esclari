using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Usuarios;

public sealed record ListarUsuarios(long? TipoId) : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) => g.MapGet("/usuarios", (long? tipoId, IMediator m) => m.Send(new ListarUsuarios(tipoId)));
}

public sealed class ListarUsuariosHandler(EsclariDb db) : IRequestHandler<ListarUsuarios, IResult>
{
    public async ValueTask<IResult> Handle(ListarUsuarios m, CancellationToken ct)
    {
        var lista = new List<UsuarioResposta>();
        foreach (var u in await db.Usuarios.Include(u => u.Tipo).Where(u => m.TipoId == null || u.TipoUsuarioId == m.TipoId).OrderBy(u => u.Nome).ToListAsync(ct))
            lista.Add(await UsuarioResposta.De(db, u));
        return Results.Ok(lista);
    }
}