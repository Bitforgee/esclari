using System.Net;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class ManifestoTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    [Fact]
    public async Task Manifesto_da_instituicao()
    {
        var i = await f.Instituicao(slug: "univag", nome: "Centro Universitário Vale");
        await f.Db(i.Id, async db =>
        {
            await db.Instituicoes.Where(x => x.Id == i.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.NomeCurto, "Univag").SetProperty(x => x.Cor, "#8a1538"));
            db.ImagensInstituicao.Add(ImagemTeste.Icone());
            await db.SaveChangesAsync();
        });

        var r = await f.Cliente().GetAsync("/api/instituicoes/univag/manifest.webmanifest");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("application/manifest+json", r.Content.Headers.ContentType!.MediaType);
        var m = await r.Json();
        Assert.Equal(("Centro Universitário Vale", "Univag", "/univag/", "/univag/", "standalone", "#8a1538", "#F7F5EE"),
            (m.GetProperty("name").GetString(), m.GetProperty("short_name").GetString(), m.GetProperty("start_url").GetString(), m.GetProperty("scope").GetString(),
             m.GetProperty("display").GetString(), m.GetProperty("theme_color").GetString(), m.GetProperty("background_color").GetString()));
        var icones = m.GetProperty("icons").EnumerateArray().ToList();
        Assert.Equal(2, icones.Count);
        Assert.Equal(["192x192", "512x512"], icones.Select(x => x.GetProperty("sizes").GetString()));
        Assert.All(icones, x =>
        {
            Assert.Equal("/api/instituicoes/univag/imagens/icone?v=2", x.GetProperty("src").GetString());
            Assert.Equal("image/png", x.GetProperty("type").GetString());
        });
    }

    [Fact]
    public async Task Manifesto_padrao_sem_aparencia()
    {
        var i = await f.Instituicao(nome: "Centro Universitário Vale");

        var r = await f.Cliente().GetAsync($"/api/instituicoes/{i.Slug}/manifest.webmanifest");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var m = await r.Json();
        Assert.Equal("CV", m.GetProperty("short_name").GetString());
        Assert.Equal("#f7f5ee", m.GetProperty("theme_color").GetString());
        Assert.Equal(i.Nome, m.GetProperty("name").GetString());
        Assert.Equal($"/{i.Slug}/", m.GetProperty("start_url").GetString());
        var icones = m.GetProperty("icons").EnumerateArray().ToList();
        Assert.Equal(["/icons/icon-192.png", "/icons/icon-512.png", "/icons/icon-maskable-512.png"], icones.Select(x => x.GetProperty("src").GetString()));
        Assert.Equal(["192x192", "512x512", "512x512"], icones.Select(x => x.GetProperty("sizes").GetString()));
    }

    [Fact]
    public async Task Instituicao_inexistente_ou_inativa_responde_404()
    {
        var inativa = await f.Instituicao();
        await f.Db(null, db => db.Instituicoes.Where(x => x.Id == inativa.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Ativa, false)));
        var c = f.Cliente();

        foreach (var slug in new[] { "nao-existe", inativa.Slug })
        {
            var r = await c.GetAsync($"/api/instituicoes/{slug}/manifest.webmanifest");
            Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
            Assert.Equal("application/problem+json", r.Content.Headers.ContentType!.MediaType);
            var corpo = await r.Json();
            Assert.Equal(404, corpo.GetProperty("status").GetInt32());
            Assert.Equal("Não encontrado", corpo.GetProperty("title").GetString());
        }
    }

    private async Task<(string Slug, System.Text.Json.JsonElement Manifesto)> ManifestoDe(bool comIcone)
    {
        var i = await f.Instituicao();
        if (comIcone)
            await f.Db(i.Id, async db =>
            {
                db.ImagensInstituicao.Add(ImagemTeste.Icone());
                await db.SaveChangesAsync();
            });
        var r = await f.Cliente().GetAsync($"/api/instituicoes/{i.Slug}/manifest.webmanifest");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (i.Slug, await r.Json());
    }

    private static List<(string Src, string Sizes, string Purpose)> Icones(System.Text.Json.JsonElement m) =>
        m.GetProperty("icons").EnumerateArray().Select(x => (x.GetProperty("src").GetString()!, x.GetProperty("sizes").GetString()!, x.GetProperty("purpose").GetString()!)).ToList();

    [Fact]
    public async Task Manifesto_declara_o_id_da_instituicao()
    {
        var (slug, m) = await ManifestoDe(false);

        Assert.Equal($"/{slug}/", m.GetProperty("id").GetString());
    }

    [Fact]
    public async Task Sem_icone_proprio_lista_o_maskable()
    {
        var icones = Icones((await ManifestoDe(false)).Manifesto);

        Assert.Equal(
            [("/icons/icon-192.png", "192x192", "any"), ("/icons/icon-512.png", "512x512", "any"), ("/icons/icon-maskable-512.png", "512x512", "maskable")],
            icones);
    }

    [Fact]
    public async Task Com_icone_proprio_so_lista_any()
    {
        var (slug, m) = await ManifestoDe(true);
        var icones = Icones(m);

        Assert.Equal(2, icones.Count);
        Assert.All(icones, x =>
        {
            Assert.Equal($"/api/instituicoes/{slug}/imagens/icone?v=2", x.Src);
            Assert.Equal("any", x.Purpose);
        });
        Assert.DoesNotContain(icones, x => x.Purpose == "maskable");
    }

    [Fact]
    public async Task Trocar_a_marca_nao_muda_id_scope_nem_start_url()
    {
        var i = await f.Instituicao();
        var endereco = $"/api/instituicoes/{i.Slug}/manifest.webmanifest";
        var antes = await (await f.Cliente().GetAsync(endereco)).Json();

        await f.Db(i.Id, async db =>
        {
            await db.Instituicoes.Where(x => x.Id == i.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Cor, "#8a1538").SetProperty(x => x.NomeCurto, "Marca"));
            db.ImagensInstituicao.Add(ImagemTeste.Icone());
            await db.SaveChangesAsync();
        });
        var depois = await (await f.Cliente().GetAsync(endereco)).Json();

        Assert.Equal("#8a1538", depois.GetProperty("theme_color").GetString());
        foreach (var campo in new[] { "id", "scope", "start_url" })
            Assert.Equal(antes.GetProperty(campo).GetString(), depois.GetProperty(campo).GetString());
    }

    [Fact]
    public async Task Instituicao_nova_nao_grava_a_marca_do_Esclari()
    {
        var i = await f.Instituicao();

        var linha = await f.Db(i.Id, db => db.Instituicoes.AsNoTracking().SingleAsync(x => x.Id == i.Id));
        var imagens = await f.Db(i.Id, db => db.ImagensInstituicao.CountAsync());
        var publica = await (await f.Cliente().GetAsync($"/api/instituicoes/{i.Slug}")).Json();

        Assert.Null(linha.Cor);
        Assert.Equal(0, imagens);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, publica.GetProperty("cor").ValueKind);
        var figuras = publica.GetProperty("imagens");
        foreach (var tipo in new[] { "logo", "icone", "login", "assistente" })
            Assert.Equal(System.Text.Json.JsonValueKind.Null, figuras.GetProperty(tipo).ValueKind);
    }

    [Fact]
    public async Task Manifesto_declara_o_fundo_do_app()
    {
        var (_, m) = await ManifestoDe(false);

        Assert.Equal("#F7F5EE", m.GetProperty("background_color").GetString());
    }}
