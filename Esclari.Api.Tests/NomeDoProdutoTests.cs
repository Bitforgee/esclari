using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Esclari.Api.Tests;

public partial class NomeDoProdutoTests
{
    [GeneratedRegex(@"(?<![A-Za-z])(One|ONE)(?![a-z])|(?<![A-Za-z0-9])one[-._]|[-._]one(?![A-Za-z0-9])")]
    private static partial Regex NomeAntigo();

    static readonly string[] PastasDeHistorico = [".specs/features/", "web/node_modules/", "Esclari.Api/wwwroot/"];

    static readonly string[] ArquivosDeHistorico =
    [
        ".specs/lessons.json", ".specs/LESSONS.md", ".specs/relatorio-nome.md",
        "Esclari.Api.Tests/NomeDoProdutoTests.cs",
        "Esclari.Api.Tests/McpFerramentasTests.cs", "Esclari.Api.Tests/McpAcessoTests.cs", "Esclari.Api.Tests/TokenMcpDominioTests.cs", "Esclari.Api.Tests/MarcaTests.cs",
        "web/package-lock.json",
        "web/src/acesso/marca.test.tsx", "web/src/administracao/marca.test.tsx", "web/src/test/marca.test.ts",
    ];

    static readonly string[] Binarios = [".png", ".jpg", ".jpeg", ".gif", ".ico", ".webp", ".woff", ".woff2", ".ttf", ".pdf", ".zip", ".mp3", ".mp4", ".wav", ".snk"];

    static readonly string[] TrechosPermitidos =
    [
        @"D:\One\One",
        "mvp-one",
        "mcp-one",
        "prompt-evolucao-one.md",
        "One or more validation errors",
        "=== 'one'",
    ];

    static readonly string[] PrefixosDoStateAnteriores =
    [
        "**Feature**: `refatoracao-back-limpa`",
        "**Next step**: `mcp-one`",
        "**Branch**: `main` (AD-043)",
    ];

    static string Raiz() => ArquiteturaTests.Raiz();

    static List<string> Rastreados()
    {
        var p = Process.Start(new ProcessStartInfo("git", "ls-files -z") { WorkingDirectory = Raiz(), RedirectStandardOutput = true, StandardOutputEncoding = Encoding.UTF8 })!;
        var saida = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        Assert.Equal(0, p.ExitCode);
        return saida.Split('\0', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    static bool Ignorado(string caminho) =>
        PastasDeHistorico.Any(caminho.StartsWith) || ArquivosDeHistorico.Contains(caminho) ||
        (caminho.StartsWith(".specs/prompt-") && caminho.EndsWith(".md")) ||
        Binarios.Contains(Path.GetExtension(caminho).ToLowerInvariant());

    static int LinhaDoNovoAd(string[] linhas) => Array.FindIndex(linhas, l => l.StartsWith("| AD-058 |"));

    static bool LinhaAntigaDoState(string[] linhas, int i, int novoAd) =>
        i <= novoAd || PrefixosDoStateAnteriores.Any(linhas[i].StartsWith);

    [Fact]
    public void Nenhum_arquivo_rastreado_cita_One_fora_do_historico()
    {
        var achados = new List<string>();
        foreach (var caminho in Rastreados().Where(c => !Ignorado(c)))
        {
            var completo = Path.Combine(Raiz(), caminho);
            if (!File.Exists(completo)) continue;
            var bytes = File.ReadAllBytes(completo);
            if (Array.IndexOf(bytes, (byte)0) >= 0) continue;
            var linhas = Encoding.UTF8.GetString(bytes).Split('\n');
            var novoAd = caminho == ".specs/STATE.md" ? LinhaDoNovoAd(linhas) : -1;
            for (var i = 0; i < linhas.Length; i++)
            {
                var linha = linhas[i];
                if (!NomeAntigo().IsMatch(linha)) continue;
                if (novoAd >= 0 && LinhaAntigaDoState(linhas, i, novoAd)) continue;
                var restante = TrechosPermitidos.Aggregate(linha, (l, t) => l.Replace(t, ""));
                if (!NomeAntigo().IsMatch(restante)) continue;
                achados.Add($"{caminho}:{i + 1}: {linha.Trim()[..Math.Min(140, linha.Trim().Length)]}");
            }
        }
        Assert.True(achados.Count == 0, "Nome antigo encontrado:\n" + string.Join("\n", achados));
    }

    [Fact]
    public void State_registra_o_AD_do_nome()
    {
        var linhas = File.ReadAllLines(Path.Combine(Raiz(), ".specs", "STATE.md"));
        var ad = Assert.Single(linhas, l => l.StartsWith("| AD-058 |"));
        Assert.Contains("Esclari", ad);
        Assert.Contains("ESC-NNNNNN", ad);
        Assert.Contains("esclarimcp_", ad);
        Assert.Contains("esclari.com.br", ad);
        Assert.Contains("active", ad);
    }
}
