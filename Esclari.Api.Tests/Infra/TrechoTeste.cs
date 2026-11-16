using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Tests.Infra;

public static class TrechoTeste
{
    public static async Task<long> SemEmbedding(this EsclariFactory f, Instituicao i)
    {
        var artigo = await f.Artigo(i, "Segunda via do boleto", "A segunda via do boleto sai no Portal do Aluno, no menu Financeiro.");
        return await f.Db(i.Id, async db =>
        {
            var versaoId = await db.VersoesArtigo.Where(v => v.ArtigoId == artigo.Id).Select(v => v.Id).FirstAsync();
            var trecho = TrechoFonte.DaVersaoDeArtigo(versaoId, 99, "A segunda via do boleto sai no Portal do Aluno.", f.Relogio.GetUtcNow().UtcDateTime.AddDays(-1));
            db.TrechosFonte.Add(trecho);
            await db.SaveChangesAsync();
            return trecho.Id;
        });
    }
}
