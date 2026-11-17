using Esclari.Api.Chamados;

namespace Esclari.Api.Tests;

public class CsvDeChamadosTests
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
}
