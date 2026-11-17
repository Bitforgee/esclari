namespace Esclari.Api.Tests;

public class CpfValorTests
{
    [Fact]
    public void Tentar_cpf_valido_normaliza_e_mascara()
    {
        var comPontuacao = Cpf.Tentar("111.444.777-35");
        var semPontuacao = Cpf.Tentar("11144477735");

        Assert.NotNull(comPontuacao);
        Assert.Equal(("11144477735", "***.444.777-**"), (comPontuacao.Valor, comPontuacao.Mascarado));
        Assert.Equal("11144477735", semPontuacao?.Valor);
        Assert.Equal("11144477735", comPontuacao.ToString());
    }

    [Fact]
    public void No_texto_acha_cpf_valido_formatado_ou_nao()
    {
        Assert.True(Cpf.NoTexto("O CPF da aluna é 111.444.777-35, confira."));
        Assert.True(Cpf.NoTexto("cpf 11144477735 no cadastro"));
        Assert.True(Cpf.NoTexto("11144477735"));
        Assert.True(Cpf.NoTexto("veja\n111.444.777-35\nobrigado"));
        Assert.False(Cpf.NoTexto("O número 111.444.777-36 não é CPF."));
        Assert.False(Cpf.NoTexto("protocolo 911144477735 e 111444777359"));
        Assert.False(Cpf.NoTexto("Boleto reemitido no portal."));
        Assert.False(Cpf.NoTexto(""));
        Assert.False(Cpf.NoTexto(null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("1114447773")]
    [InlineData("111.444.7a7-35")]
    [InlineData("111.444.777-45")]
    [InlineData("111.444.777-36")]
    public void Tentar_devolve_null_nos_cinco_casos_invalidos(string? texto) => Assert.Null(Cpf.Tentar(texto));
}