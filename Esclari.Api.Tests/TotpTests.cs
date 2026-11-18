using Esclari.Api.Plataforma;

namespace Esclari.Api.Tests;

public class TotpTests
{
    const string Segredo = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
    static readonly DateTimeOffset Agora = DateTimeOffset.FromUnixTimeSeconds(1_111_111_109);

    [Theory]
    [InlineData(59L, "94287082")]
    [InlineData(1_111_111_109L, "07081804")]
    [InlineData(1_111_111_111L, "14050471")]
    [InlineData(1_234_567_890L, "89005924")]
    [InlineData(2_000_000_000L, "69279037")]
    [InlineData(20_000_000_000L, "65353130")]
    public void Vetores_do_RFC_6238_com_SHA1(long segundos, string esperado)
    {
        var passo = Totp.PassoDe(DateTimeOffset.FromUnixTimeSeconds(segundos));

        Assert.Equal(esperado, Totp.Codigo(Segredo, passo, 8));
        Assert.Equal(esperado[2..], Totp.Codigo(Segredo, passo));
    }

    [Fact]
    public void Janela_de_um_passo_para_cada_lado()
    {
        var atual = Totp.PassoDe(Agora);

        foreach (var deslocamento in new long[] { -1, 0, 1 })
            Assert.Equal(atual + deslocamento, Totp.Verificar(Segredo, Totp.Codigo(Segredo, atual + deslocamento), Agora, null));
        foreach (var deslocamento in new long[] { -2, 2 })
            Assert.Null(Totp.Verificar(Segredo, Totp.Codigo(Segredo, atual + deslocamento), Agora, null));
    }

    [Fact]
    public void Passo_ja_usado_e_recusado()
    {
        var atual = Totp.PassoDe(Agora);
        var codigo = Totp.Codigo(Segredo, atual);

        Assert.Null(Totp.Verificar(Segredo, codigo, Agora, atual));
        Assert.Null(Totp.Verificar(Segredo, codigo, Agora, atual + 1));
        Assert.Equal(atual, Totp.Verificar(Segredo, codigo, Agora, atual - 1));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12a456")]
    [InlineData(null)]
    public void Codigo_fora_do_formato_nao_passa(string? codigo) => Assert.Null(Totp.Verificar(Segredo, codigo, Agora, null));

    [Fact]
    public void Segredo_novo_tem_20_bytes_em_base32_e_a_uri_do_aplicativo()
    {
        var segredo = Totp.NovoSegredo();

        Assert.Equal(32, segredo.Length);
        Assert.Equal(20, Totp.DeBase32(segredo).Length);
        Assert.Matches("^[A-Z2-7]{32}$", segredo);
        Assert.Equal($"otpauth://totp/Esclari:ana@equipe.esclari?secret={segredo}&issuer=Esclari", Totp.Uri("ana@equipe.esclari", segredo));
    }
}
