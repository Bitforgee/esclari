using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Conhecimento.Artigos;

public sealed record ArtigoResposta(long Id, string Estado, string? Indexacao, long CategoriaId, string Categoria, DateOnly? VisivelDe, DateOnly? VisivelAte, DateOnly ValidoAte, string Validade, ArtigoResposta.DaVersao Versao, string? OrigemChamado)
{
    public sealed record DaVersao(long Id, int Numero, string Titulo, IReadOnlyList<string> Perguntas, string Resposta, IReadOnlyList<string> Links, DateTime CriadaEm);

    public static ArtigoResposta De(Artigo a, string categoria, Dictionary<long, string> indexacao, DateOnly hoje, Dictionary<long, string> origens)
    {
        var v = a.UltimaVersao;
        return new(a.Id, a.Estado, indexacao.GetValueOrDefault(v.Id), a.CategoriaId, categoria, a.VisivelDe, a.VisivelAte, a.ValidoAte, Dados.Validade.Situacao(a.ValidoAte, hoje),
            new(v.Id, v.Numero, v.Titulo, v.Perguntas, v.Resposta, v.Links, v.CriadaEm), a.OrigemChamadoId is { } c ? origens.GetValueOrDefault(c) : null);
    }

    public static Task<Dictionary<long, string>> Origens(EsclariDb db, IEnumerable<Artigo> artigos)
    {
        var ids = artigos.Select(a => a.OrigemChamadoId).OfType<long>().ToList();
        return db.Chamados.Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Numero);
    }

    public static async Task<ArtigoResposta> De(EsclariDb db, Artigo a, DateOnly hoje) =>
        De(a, await db.Categorias.Where(c => c.Id == a.CategoriaId).Select(c => c.Nome).SingleAsync(), await BuscarFontes.Indexacao(db, [a.Versoes.Max(v => v.Id)]), hoje, await Origens(db, [a]));
}
