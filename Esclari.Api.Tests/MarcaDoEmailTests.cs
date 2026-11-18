using System.Buffers.Binary;
using System.Reflection;

namespace Esclari.Api.Tests;

public class MarcaDoEmailTests
{
    [Fact]
    public void Png_do_Esclari_tem_348_por_80_sem_canal_alfa()
    {
        var png = EnviadorSmtp.LogoDoEsclari();

        Assert.Equal([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A], png[..8]);
        Assert.Equal(348u, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16, 4)));
        Assert.Equal(80u, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20, 4)));
        Assert.Contains(png[25], new byte[] { 2, 3 });
    }

    [Fact]
    public void Png_do_Esclari_esta_embutido_no_projeto()
    {
        var recursos = typeof(EnviadorSmtp).Assembly.GetManifestResourceNames();
        Assert.Contains("logo-horizontal-email.png", recursos);

        var raiz = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var projeto = File.ReadAllText(Path.Combine(raiz, "Esclari.Api", "Esclari.Api.csproj"));
        Assert.Contains("<EmbeddedResource Include=\"Instituicoes\\Aparencia\\logo-horizontal-email.png\" LogicalName=\"logo-horizontal-email.png\" />", projeto);
        Assert.True(File.Exists(Path.Combine(raiz, "Esclari.Api", "Instituicoes", "Aparencia", "logo-horizontal-email.png")));
    }
}
