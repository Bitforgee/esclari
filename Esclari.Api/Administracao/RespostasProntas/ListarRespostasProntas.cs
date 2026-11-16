using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.RespostasProntas;

public sealed record ListarRespostasProntas : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) => g.MapGet("/respostas-prontas", (IMediator m) => m.Send(new ListarRespostasProntas()));
}

public sealed class ListarRespostasProntasHandler(EsclariDb db) : IRequestHandler<ListarRespostasProntas, IResult>
{
    public async ValueTask<IResult> Handle(ListarRespostasProntas m, CancellationToken ct) =>
        Results.Ok((await db.RespostasProntas.OrderBy(e => e.Id).ToListAsync(ct)).Select(RespostaProntaResposta.De));
}
