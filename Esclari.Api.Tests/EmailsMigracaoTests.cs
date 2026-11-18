using Microsoft.EntityFrameworkCore;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class EmailsMigracaoTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    [Fact]
    public async Task Colunas_titulo_acao_rotulo_e_acao_url_anulaveis()
    {
        var colunas = await f.Db(null, db => db.Database.SqlQueryRaw<string>(
            "SELECT column_name || ',' || data_type || ',' || is_nullable AS \"Value\" FROM information_schema.columns WHERE table_name = 'email_saida' ORDER BY column_name").ToListAsync());

        Assert.Contains("titulo,text,YES", colunas);
        Assert.Contains("acao_rotulo,text,YES", colunas);
        Assert.Contains("acao_url,text,YES", colunas);
        Assert.Equal(
            ["acao_rotulo", "acao_url", "assunto", "corpo", "destinatario", "enviado_em", "id", "instituicao_id", "proxima_tentativa", "tentativas", "titulo"],
            colunas.Select(c => c.Split(',')[0]).Order().ToList());
    }
}
