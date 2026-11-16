using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Setores;

public sealed record ListarSetores : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) => g.MapGet("/setores", (IMediator m) => m.Send(new ListarSetores()));
}

public sealed class ListarSetoresHandler(EsclariDb db) : IRequestHandler<ListarSetores, IResult>
{
    public async ValueTask<IResult> Handle(ListarSetores m, CancellationToken ct) =>
        Results.Ok((await db.Setores.OrderBy(e => e.Id).ToListAsync(ct)).Select(SetorResposta.De));
}
