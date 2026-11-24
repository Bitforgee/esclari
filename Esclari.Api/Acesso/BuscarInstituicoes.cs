using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Acesso;

public sealed record BuscarInstituicoes(string? Busca) : IRequest<IResult>
{
    public sealed record Item(string Slug, string Nome, string? NomeCurto);

    public sealed record Resposta(IReadOnlyList<Item> Itens);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapGet("/instituicoes", (string? busca, IMediator m) => m.Send(new BuscarInstituicoes(busca))).AllowAnonymous().RequireRateLimiting("instituicoes");
}

public sealed class BuscarInstituicoesHandler(EsclariDb db) : IRequestHandler<BuscarInstituicoes, IResult>
{
    public async ValueTask<IResult> Handle(BuscarInstituicoes m, CancellationToken ct)
    {
        var texto = (m.Busca ?? "").Trim();
        if (texto.Length is < 3 or > 60) return Problemas.Invalido("Digite de 3 a 60 caracteres.", "busca");
        var padrao = "%" + texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        var itens = await db.Instituicoes
            .Where(i => i.Ativa && (EF.Functions.ILike(EF.Functions.Unaccent(i.Nome), EF.Functions.Unaccent(padrao), "\\")
                || EF.Functions.ILike(EF.Functions.Unaccent(i.NomeCurto ?? ""), EF.Functions.Unaccent(padrao), "\\")
                || EF.Functions.ILike(i.Slug, padrao, "\\")))
            .OrderBy(i => i.Nome).ThenBy(i => i.Slug).Take(5)
            .Select(i => new BuscarInstituicoes.Item(i.Slug, i.Nome, i.NomeCurto))
            .ToListAsync(ct);
        return Results.Ok(new BuscarInstituicoes.Resposta(itens));
    }
}
