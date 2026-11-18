using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record ListarAuditoria(string? Instituicao, int Pagina) : IRequest<IResult>
{
    public const int PorPagina = 50;

    public sealed record Item(string Acao, string Operador, string? Instituicao, string Resumo, DateTime CriadaEm);
    public sealed record Resposta(IReadOnlyList<Item> Itens, int Total);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapGet("/auditoria", (string? instituicao, int? pagina, IMediator m) => m.Send(new ListarAuditoria(instituicao, pagina ?? 1)));
}

public sealed class ListarAuditoriaHandler(EsclariDb db) : IRequestHandler<ListarAuditoria, IResult>
{
    public async ValueTask<IResult> Handle(ListarAuditoria m, CancellationToken ct)
    {
        if (m.Pagina < 1) return Problemas.Invalido("A página começa em 1.", "pagina");
        long? alvo = null;
        if (!string.IsNullOrEmpty(m.Instituicao))
        {
            alvo = await db.Instituicoes.Where(i => i.Slug == m.Instituicao).Select(i => (long?)i.Id).FirstOrDefaultAsync(ct);
            if (alvo is null) return Problemas.Invalido("Instituição desconhecida.", "instituicao");
        }

        var consulta = db.AuditoriaDaPlataforma.Where(a => alvo == null || a.InstituicaoAlvoId == alvo);
        var total = await consulta.CountAsync(ct);
        var itens = await consulta.OrderByDescending(a => a.CriadaEm).ThenByDescending(a => a.Id)
            .Skip((m.Pagina - 1) * ListarAuditoria.PorPagina).Take(ListarAuditoria.PorPagina)
            .Select(a => new ListarAuditoria.Item(a.Acao,
                db.OperadoresPlataforma.Where(o => o.Id == a.OperadorId).Select(o => o.Nome).First(),
                db.Instituicoes.Where(i => i.Id == a.InstituicaoAlvoId).Select(i => i.Slug).FirstOrDefault(), a.Resumo, a.CriadaEm))
            .ToListAsync(ct);
        return Results.Ok(new ListarAuditoria.Resposta(itens, total));
    }
}
