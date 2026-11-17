using System.Net;
using System.Text;
using Esclari.Api.Chamados;
using Esclari.Api.Chamados.Consulta;
using Esclari.Api.Tests.Infra;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Tests;

public class CsvDeChamadosTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    [Theory]
    [InlineData("a;b", "\"a;b\"")]
    [InlineData("diz \"oi\"", "\"diz \"\"oi\"\"\"")]
    [InlineData("linha\rquebrada", "\"linha\rquebrada\"")]
    [InlineData("linha\nquebrada", "\"linha\nquebrada\"")]
    [InlineData("simples", "simples")]
    [InlineData(null, "")]
    public void Aspas_quando_precisa(string? valor, string esperado) => Assert.Equal(esperado, CsvDeChamados.Celula(valor));

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+5565999", "'+5565999")]
    [InlineData("-3", "'-3")]
    [InlineData("@SOMA(1)", "'@SOMA(1)")]
    [InlineData("\tx", "'\tx")]
    [InlineData("\rx", "\"'\rx\"")]
    [InlineData("a=b", "a=b")]
    public void Prefixo_contra_formula(string valor, string esperado) => Assert.Equal(esperado, CsvDeChamados.Celula(valor));

    static async Task<string[]> Linhas(HttpClient c, string consulta)
    {
        var r = await c.GetAsync("/api/chamados/exportar?escopo=fila" + consulta);
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return Encoding.UTF8.GetString(await r.Content.ReadAsByteArrayAsync()).TrimStart('\uFEFF').Split("\r\n")[..^1];
    }

    [Fact]
    public async Task Exporta_so_os_atrasados()
    {
        var b = await Balcao.Criar(f);
        var atrasado = await b.Chamado(Aberto);
        var emDia = await b.Chamado(Aberto);
        var agora = f.Relogio.GetUtcNow().UtcDateTime;
        await f.Criado(b.Instituicao, atrasado, agora.AddHours(-30));
        await f.Criado(b.Instituicao, emDia, agora.AddHours(-2));

        var so = await Linhas(b.A.Cliente, "&atraso=true");
        var todas = await Linhas(b.A.Cliente, "");

        Assert.Equal(2, so.Length);
        Assert.Equal(string.Join(";", ExportarChamados.Colunas), so[0]);
        Assert.Equal(16, so[0].Split(';').Length);
        Assert.Equal(16, so[1].Split(';').Length);
        Assert.Equal(atrasado, so[1].Split(';')[0]);
        Assert.Equal(3, todas.Length);
        Assert.Equal([emDia], (await Linhas(b.A.Cliente, "&atraso=false")).Skip(1).Select(l => l.Split(';')[0]));
    }
}
