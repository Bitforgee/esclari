using Mediator;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record ListarInstituicoes : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) => g.MapGet("/instituicoes", (IMediator m) => m.Send(new ListarInstituicoes()));
}

public sealed class ListarInstituicoesHandler(EsclariDb db, TimeProvider relogio, Prazos prazos) : IRequestHandler<ListarInstituicoes, IResult>
{
    public async ValueTask<IResult> Handle(ListarInstituicoes m, CancellationToken ct) =>
        Results.Ok(await ConsultaDasInstituicoes.Ler(db, relogio.GetUtcNow().UtcDateTime, prazos));
}
