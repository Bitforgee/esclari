using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Chamados.Consulta;

public sealed record FiltrosDaBusca(string? Escopo, long UsuarioId) : IRequest<IResult>
{
    public sealed record Opcao(long Id, string Nome);

    public sealed record Resposta(IReadOnlyList<Opcao> Setores, IReadOnlyList<Opcao> Categorias);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapGet("/chamados/filtros", (string? escopo, HttpContext ctx, IMediator m) => m.Send(new FiltrosDaBusca(escopo, ctx.Usuario().Id)));
}

public sealed class FiltrosDaBuscaHandler(EsclariDb db) : IRequestHandler<FiltrosDaBusca, IResult>
{
    public async ValueTask<IResult> Handle(FiltrosDaBusca m, CancellationToken ct)
    {
        var u = await FiltroDeChamados.Quem(db, m.UsuarioId);
        var deTodos = m.Escopo == FiltroDeChamados.Todos;
        if (FiltroDeChamados.Recusa(u, deTodos) is { } recusa) return recusa;
        var meus = deTodos ? null : await FiltroDeChamados.SetoresDe(db, u);
        var setores = await db.Setores.Where(s => meus == null || meus.Contains(s.Id)).OrderBy(s => s.Nome).ThenBy(s => s.Id)
            .Select(s => new FiltrosDaBusca.Opcao(s.Id, s.Nome)).ToListAsync(ct);
        var categorias = await db.Categorias.Where(c => c.Ativa).OrderBy(c => c.Nome).ThenBy(c => c.Id)
            .Select(c => new FiltrosDaBusca.Opcao(c.Id, c.Nome)).ToListAsync(ct);
        return Results.Ok(new FiltrosDaBusca.Resposta(setores, categorias));
    }
}
