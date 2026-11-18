using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record ListarSetoresDaInstituicao(string Slug) : IRequest<IResult>
{
    public sealed record Item(long Id, string Nome);

    public static void Mapear(RouteGroupBuilder g) => g.MapGet("/instituicoes/{slug}/setores", (string slug, IMediator m) => m.Send(new ListarSetoresDaInstituicao(slug)));
}

public sealed class ListarSetoresDaInstituicaoHandler(EsclariDb db) : IRequestHandler<ListarSetoresDaInstituicao, IResult>
{
    public async ValueTask<IResult> Handle(ListarSetoresDaInstituicao m, CancellationToken ct)
    {
        if (await EquipeDaInstituicao.Alvo(db, m.Slug, ct) is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        return Results.Ok(await db.Setores.AsNoTracking().Where(s => s.Ativo).OrderBy(s => s.Nome).Select(s => new ListarSetoresDaInstituicao.Item(s.Id, s.Nome)).ToListAsync(ct));
    }
}
