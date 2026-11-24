using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class BuscaDeInstituicoesTests
{
    static Task Detalhar(EsclariFactory f, Instituicao i, string? curto, bool ativa = true) =>
        f.Db(null, db => db.Instituicoes.Where(x => x.Id == i.Id).ExecuteUpdateAsync(s => s
            .SetProperty(x => x.NomeCurto, curto).SetProperty(x => x.Ativa, ativa)));

    static async Task<JsonElement> Buscar(HttpClient c, string busca, HttpStatusCode esperado = HttpStatusCode.OK)
    {
        var r = await c.GetAsync("/api/instituicoes?busca=" + Uri.EscapeDataString(busca));
        Assert.Equal(esperado, r.StatusCode);
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Acha_por_nome_sigla_e_codigo_sem_acento()
    {
        await using var f = new EsclariFactory();
        await f.InitializeAsync();
        var b = await f.Instituicao("beta-cod", "Faculdade Beta");
        var a = await f.Instituicao("alfa-cod", "Centro Universitário Várzea Grande");
        await Detalhar(f, a, "Univag");
        await Detalhar(f, b, "Faltu");

        var porNome = await Buscar(f.Cliente(), "varzea");
        var item = Assert.Single(porNome.GetProperty("itens").EnumerateArray());
        Assert.Equal("""{"slug":"alfa-cod","nome":"Centro Universitário Várzea Grande","nomeCurto":"Univag"}""", item.GetRawText());
        Assert.Single((await Buscar(f.Cliente(), "UNIVAG")).GetProperty("itens").EnumerateArray());
        Assert.Single((await Buscar(f.Cliente(), "beta-c")).GetProperty("itens").EnumerateArray());

        var ordem = (await Buscar(f.Cliente(), "cod")).GetProperty("itens").EnumerateArray().Select(x => x.GetProperty("slug").GetString());
        Assert.Equal(["alfa-cod", "beta-cod"], ordem);
    }

    [Fact]
    public async Task Limita_a_cinco_e_nunca_devolve_inativa()
    {
        await using var f = new EsclariFactory();
        await f.InitializeAsync();
        for (var n = 1; n <= 7; n++) await Detalhar(f, await f.Instituicao($"lote-{n}", $"Lote {n}"), null);
        await Detalhar(f, await f.Instituicao("lote-inativa", "Lote inativa"), null, ativa: false);

        var itens = (await Buscar(f.Cliente(), "lote")).GetProperty("itens").EnumerateArray().Select(x => x.GetProperty("slug").GetString()).ToList();
        Assert.Equal(["lote-1", "lote-2", "lote-3", "lote-4", "lote-5"], itens);
        Assert.Empty((await Buscar(f.Cliente(), "inativa")).GetProperty("itens").EnumerateArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("  ab  ")]
    public async Task Valida_o_tamanho_do_texto(string? busca)
    {
        await using var f = new EsclariFactory();
        await f.InitializeAsync();
        var r = await f.Cliente().GetAsync("/api/instituicoes" + (busca is null ? "" : "?busca=" + Uri.EscapeDataString(busca)));
        await Recusa(r);
        await Recusa(await f.Cliente().GetAsync("/api/instituicoes?busca=" + new string('a', 61)));
        await Detalhar(f, await f.Instituicao("apar-ada", "Aparada"), null);
        Assert.Single((await Buscar(f.Cliente(), "  apa  ")).GetProperty("itens").EnumerateArray());
    }

    static async Task Recusa(HttpResponseMessage r)
    {
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Digite de 3 a 60 caracteres.", j.GetProperty("errors").GetProperty("busca")[0].GetString());
    }

    [Fact]
    public async Task Curinga_vira_texto_literal()
    {
        await using var f = new EsclariFactory();
        await f.InitializeAsync();
        await Detalhar(f, await f.Instituicao("curinga-um", "Curinga Um"), null);
        await Detalhar(f, await f.Instituicao("curinga-dois", "Curinga 100% Dois"), null);

        Assert.Empty((await Buscar(f.Cliente(), "%%%")).GetProperty("itens").EnumerateArray());
        Assert.Empty((await Buscar(f.Cliente(), "c_r")).GetProperty("itens").EnumerateArray());
        Assert.Single((await Buscar(f.Cliente(), "100%")).GetProperty("itens").EnumerateArray());
        Assert.Empty((await Buscar(f.Cliente(), @"\\\")).GetProperty("itens").EnumerateArray());
    }

    [Fact]
    public async Task Passou_do_limite_responde_429()
    {
        await using var f = new EsclariFactory();
        await f.InitializeAsync();
        await using var limitada = f.WithWebHostBuilder(h => h.UseSetting("Limites:BuscaDeInstituicaoPorMinuto", "2"));
        var c = limitada.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/instituicoes?busca=abc")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/instituicoes?busca=abc")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await c.GetAsync("/api/instituicoes?busca=abc")).StatusCode);
    }
}
