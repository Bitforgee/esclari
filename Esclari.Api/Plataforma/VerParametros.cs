using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record VerParametros(string Slug) : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) => g.MapGet("/instituicoes/{slug}/parametros", (string slug, IMediator m) => m.Send(new VerParametros(slug)));
}

public sealed class VerParametrosHandler(EsclariDb db, Prazos prazos) : IRequestHandler<VerParametros, IResult>
{
    public async ValueTask<IResult> Handle(VerParametros m, CancellationToken ct)
    {
        var instituicao = await db.Instituicoes.AsNoTracking().FirstOrDefaultAsync(i => i.Slug == m.Slug, ct);
        return instituicao is null ? Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada) : Results.Ok(ParametrosDaPlataforma.Resposta(instituicao, prazos));
    }
}
