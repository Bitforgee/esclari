using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Chamados.Consulta;

public sealed record BuscarChamados(FiltroDeChamados Filtro, int? Pagina, long UsuarioId) : IRequest<IResult>
{
    public const int PorPagina = 50;

    public sealed record Item(string Numero, string Titulo, string Estado, string Setor, string Categoria, string? Responsavel, DateTime CriadoEm, int Dias);

    public sealed record Resposta(IReadOnlyList<Item> Itens, int Total, int Pagina, int PorPagina);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapGet("/chamados/busca", ([AsParameters] FiltroDeChamados filtro, int? pagina, HttpContext ctx, IMediator m) =>
            m.Send(new BuscarChamados(filtro, pagina, ctx.Usuario().Id)));
}

public sealed class BuscarChamadosHandler(EsclariDb db, TimeProvider relogio) : IRequestHandler<BuscarChamados, IResult>
{
    record Linha(string Numero, string Titulo, string Estado, string Setor, string Categoria, string? Responsavel, DateTime CriadoEm);

    public async ValueTask<IResult> Handle(BuscarChamados m, CancellationToken ct)
    {
        var (pedido, recusa) = await m.Filtro.Consultar(db, m.UsuarioId);
        if (recusa is not null) return recusa;
        if (m.Pagina is < 1) return Problemas.Invalido("A página deve ser 1 ou maior.", "pagina");
        var pagina = m.Pagina ?? 1;
        var consulta = pedido!.Consulta;
        var total = await consulta.CountAsync(ct);
        var linhas = await consulta.Skip((pagina - 1) * BuscarChamados.PorPagina).Take(BuscarChamados.PorPagina)
            .Select(c => new Linha(c.Numero, c.Titulo, c.Estado,
                db.Setores.Where(s => s.Id == c.SetorId).Select(s => s.Nome).Single(),
                db.Categorias.Where(x => x.Id == c.CategoriaId).Select(x => x.Nome).Single(),
                db.Usuarios.Where(x => x.Id == c.ResponsavelId).Select(x => x.Nome).SingleOrDefault(), c.CriadoEm))
            .ToListAsync(ct);
        var agora = relogio.GetUtcNow().UtcDateTime;
        var itens = linhas.Select(c => new BuscarChamados.Item(c.Numero, c.Titulo, c.Estado, c.Setor, c.Categoria, c.Responsavel, c.CriadoEm, (int)(agora - c.CriadoEm).TotalDays)).ToList();
        return Results.Ok(new BuscarChamados.Resposta(itens, total, pagina, BuscarChamados.PorPagina));
    }
}
