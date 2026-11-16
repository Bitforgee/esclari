using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Conhecimento.Artigos;

public static class ArtigoComVersoes
{
    public static Task<Artigo?> Ler(EsclariDb db, long id) => db.Artigos.Include(a => a.Versoes).FirstOrDefaultAsync(a => a.Id == id);
}
