using System.Net;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class SpaTests
{
    [Fact]
    public async Task Spa_em_slug_e_api_404_em_problem_json()
    {
        var wwwroot = Directory.CreateTempSubdirectory("esclari-wwwroot-").FullName;
        await File.WriteAllTextAsync(Path.Combine(wwwroot, "index.html"), "<!doctype html><title>One</title><div id=\"raiz\">spa-esclari</div>");
        await using var f = new EsclariFactory { WebRoot = wwwroot };
        await f.InitializeAsync();

        var spa = await f.Cliente().GetAsync("/univag/assistente");
        var api = await f.Cliente().GetAsync("/api/inexistente");

        Assert.Equal(HttpStatusCode.OK, spa.StatusCode);
        Assert.Equal("text/html", spa.Content.Headers.ContentType?.MediaType);
        Assert.Contains("spa-esclari", await spa.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, api.StatusCode);
        Assert.Equal("application/problem+json", api.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("spa-esclari", await api.Content.ReadAsStringAsync());
    }
}
