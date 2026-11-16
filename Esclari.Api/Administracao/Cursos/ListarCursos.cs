using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Cursos;

public sealed record ListarCursos : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) => g.MapGet("/cursos", (IMediator m) => m.Send(new ListarCursos()));
}

public sealed class ListarCursosHandler(EsclariDb db) : IRequestHandler<ListarCursos, IResult>
{
    public async ValueTask<IResult> Handle(ListarCursos m, CancellationToken ct) =>
        Results.Ok((await db.Cursos.OrderBy(e => e.Id).ToListAsync(ct)).Select(CursoResposta.De));
}
