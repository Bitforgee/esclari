using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Usuarios;

public sealed record ListarUsuarios : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) => g.MapGet("/usuarios", (IMediator m) => m.Send(new ListarUsuarios()));
}

public sealed class ListarUsuariosHandler(EsclariDb db) : IRequestHandler<ListarUsuarios, IResult>
{
    public async ValueTask<IResult> Handle(ListarUsuarios m, CancellationToken ct)
    {
        var lista = new List<UsuarioResposta>();
        foreach (var u in await db.Usuarios.Include(u => u.Papeis).OrderBy(u => u.Nome).ToListAsync()) lista.Add(await UsuarioResposta.De(db, u));
        return Results.Ok(lista);
    }
}
