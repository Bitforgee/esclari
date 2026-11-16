using System.Net;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class InstituicaoRotaTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    [Fact]
    public async Task Slug_ativo_200()
    {
        await f.Instituicao("univag", "Univag");

        var r = await f.Cliente().GetAsync("/api/instituicoes/univag");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("Univag", (await r.Json()).GetProperty("nome").GetString());
    }

    [Fact]
    public async Task Slug_inexistente_ou_inativo_404()
    {
        var inativa = await f.Instituicao();
        await f.Db(null, db => db.Instituicoes.Where(i => i.Id == inativa.Id).ExecuteUpdateAsync(s => s.SetProperty(i => i.Ativa, false)));

        foreach (var slug in new[] { "nao-existe", inativa.Slug })
        {
            var r = await f.Cliente().GetAsync($"/api/instituicoes/{slug}");
            Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
            Assert.Equal("Instituição não encontrada.", (await r.Json()).GetProperty("detail").GetString());
        }
    }
}
