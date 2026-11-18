using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public static class ConhecimentoTeste
{
    public static object CorpoArtigo(long categoriaId, string titulo = "Abono de faltas") =>
        new { titulo, perguntas = new[] { "posso abonar falta?" }, categoriaId, resposta = "Faltas não são abonadas.", links = Array.Empty<string>() };

    public static async Task<long> NovoArtigo(this HttpClient admin, long categoriaId, string titulo = "Abono de faltas", bool ativar = true)
    {
        var r = await admin.PostAsJsonAsync("/api/admin/artigos", CorpoArtigo(categoriaId, titulo));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var id = (await r.Json()).GetProperty("id").GetInt64();
        if (ativar) Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/admin/artigos/{id}/ativar", null)).StatusCode);
        return id;
    }

    public static async Task<long> NovoDocumento(this HttpClient admin, long categoriaId, string titulo = "Regulamento de estágio")
    {
        var r = await admin.Subir(titulo, categoriaId, Pdfs.Criar("Regulamento do estagio supervisionado."));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Json()).GetProperty("id").GetInt64();
    }

    public static Task ValidadeDoArtigo(this EsclariFactory f, Instituicao i, long id, DateOnly validoAte) =>
        f.Db(i.Id, db => db.Artigos.Where(a => a.Id == id).ExecuteUpdateAsync(s => s.SetProperty(a => a.ValidoAte, validoAte)));

    public static Task ValidadeDoDocumento(this EsclariFactory f, Instituicao i, long id, DateOnly validoAte) =>
        f.Db(i.Id, db => db.Documentos.Where(d => d.Id == id).ExecuteUpdateAsync(s => s.SetProperty(d => d.ValidoAte, validoAte)));

    public static async Task<JsonElement> Item(this HttpClient admin, string lista, long id) =>
        (await (await admin.GetAsync($"/api/admin/{lista}")).Json()).EnumerateArray().Single(x => x.GetProperty("id").GetInt64() == id);

    public static Task<long> DocumentoSemeado(this EsclariFactory f, Instituicao i, string titulo, string texto, bool arquivado = false) =>
        f.Db(i.Id, async db =>
        {
            var versao = new VersaoDocumento { Numero = 1, NomeArquivo = "d.pdf", Chave = $"{i.Id}/documentos/{Guid.NewGuid():N}", Hash = new string('a', 64), Tamanho = 1, Texto = texto, Paginas = 1, CriadaEm = f.Relogio.GetUtcNow().UtcDateTime };
            var d = Documento.Criar(titulo, (await db.Categorias.FirstAsync()).Id, versao, f.Validade());
            if (arquivado) d.Arquivar();
            db.Documentos.Add(d);
            await db.SaveChangesAsync();
            db.TrechosFonte.Add(TrechoFonte.DoDocumento(versao, 0, texto, f.Relogio.GetUtcNow().UtcDateTime, new Pgvector.Vector(LlmFalso.Vetor(texto))));
            await db.SaveChangesAsync();
            return d.Id;
        });
}

public class ValidadeConhecimentoTests(EsclariFactoryValidade f, EsclariFactoryMadrugada madrugada) : IClassFixture<EsclariFactoryValidade>, IClassFixture<EsclariFactoryMadrugada>
{
    static readonly DateOnly Hoje = EsclariFactoryValidade.Hoje;

    async Task<(Instituicao i, HttpClient admin, long categoria)> Preparar(EsclariFactory fabrica)
    {
        var (i, admin) = await Admin.Criar(fabrica);
        return (i, admin, (await fabrica.Categoria(i, "Acadêmico")).Id);
    }

    [Fact]
    public async Task Criar_artigo_grava_validade_de_12_meses()
    {
        var (i, admin, categoria) = await Preparar(f);

        var r = await admin.PostAsJsonAsync("/api/admin/artigos", ConhecimentoTeste.CorpoArtigo(categoria));

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var j = await r.Json();
        Assert.Equal("2027-11-17", j.GetProperty("validoAte").GetString());
        var id = j.GetProperty("id").GetInt64();
        Assert.Equal(new DateOnly(2027, 11, 17), await f.Db(i.Id, db => db.Artigos.Where(a => a.Id == id).Select(a => a.ValidoAte).SingleAsync()));
    }

    [Fact]
    public async Task Criar_documento_grava_validade_de_12_meses()
    {
        var (i, admin, categoria) = await Preparar(f);

        var r = await admin.Subir("Regulamento de estágio", categoria, Pdfs.Criar("Regulamento do estagio supervisionado."));

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var j = await r.Json();
        Assert.Equal("2027-11-17", j.GetProperty("validoAte").GetString());
        var id = j.GetProperty("id").GetInt64();
        Assert.Equal(new DateOnly(2027, 11, 17), await f.Db(i.Id, db => db.Documentos.Where(d => d.Id == id).Select(d => d.ValidoAte).SingleAsync()));
    }

    [Fact]
    public async Task Validade_usa_a_data_no_fuso_da_instituicao()
    {
        Assert.Equal(new DateTimeOffset(2026, 11, 18, 1, 0, 0, TimeSpan.Zero), madrugada.Relogio.GetUtcNow());
        var (i, admin, categoria) = await Preparar(madrugada);
        Assert.Equal("America/Cuiaba", i.Fuso);

        var artigo = await (await admin.PostAsJsonAsync("/api/admin/artigos", ConhecimentoTeste.CorpoArtigo(categoria))).Json();
        var documento = await (await admin.Subir("Regulamento", categoria, Pdfs.Criar("Regulamento do estagio supervisionado."))).Json();

        Assert.Equal("2027-11-17", artigo.GetProperty("validoAte").GetString());
        Assert.Equal("2027-11-17", documento.GetProperty("validoAte").GetString());
    }

    [Fact]
    public async Task Nova_versao_pela_rota_refaz_a_validade()
    {
        var (i, admin, categoria) = await Preparar(f);
        var artigo = await admin.NovoArtigo(categoria);
        var documento = await admin.NovoDocumento(categoria);
        await f.ValidadeDoArtigo(i, artigo, new DateOnly(2027, 1, 10));
        await f.ValidadeDoDocumento(i, documento, new DateOnly(2027, 1, 10));

        var a = await admin.PutAsJsonAsync($"/api/admin/artigos/{artigo}", ConhecimentoTeste.CorpoArtigo(categoria, "Abono de faltas, versão 2"));
        var d = await admin.NovaVersao(documento, Pdfs.Criar("Regulamento do estagio supervisionado, versao dois."));

        Assert.Equal(HttpStatusCode.OK, a.StatusCode);
        Assert.Equal(HttpStatusCode.Created, d.StatusCode);
        Assert.Equal("2027-11-17", (await a.Json()).GetProperty("validoAte").GetString());
        Assert.Equal("2027-11-17", (await d.Json()).GetProperty("validoAte").GetString());
        Assert.Equal(new DateOnly(2027, 11, 17), await f.Db(i.Id, db => db.Artigos.Where(x => x.Id == artigo).Select(x => x.ValidoAte).SingleAsync()));
        Assert.Equal(new DateOnly(2027, 11, 17), await f.Db(i.Id, db => db.Documentos.Where(x => x.Id == documento).Select(x => x.ValidoAte).SingleAsync()));
    }

    [Fact]
    public async Task Vencido_continua_ativo_e_aparece_como_vencido()
    {
        var (i, admin, categoria) = await Preparar(f);
        var artigo = await admin.NovoArtigo(categoria);
        var documento = await admin.NovoDocumento(categoria);
        await f.ValidadeDoArtigo(i, artigo, Hoje.AddDays(-1));
        await f.ValidadeDoDocumento(i, documento, Hoje.AddDays(-1));

        var a = await admin.Item("artigos", artigo);
        var d = await admin.Item("documentos", documento);

        Assert.Equal(("ATIVO", "VENCIDO", "2026-11-16"), (a.GetProperty("estado").GetString(), a.GetProperty("validade").GetString(), a.GetProperty("validoAte").GetString()));
        Assert.Equal(("ATIVO", "VENCIDO", "2026-11-16"), (d.GetProperty("estado").GetString(), d.GetProperty("validade").GetString(), d.GetProperty("validoAte").GetString()));
        Assert.Equal(EstadosArtigo.Ativo, await f.Db(i.Id, db => db.Artigos.Where(x => x.Id == artigo).Select(x => x.Estado).SingleAsync()));
        Assert.Equal(EstadosDocumento.Ativo, await f.Db(i.Id, db => db.Documentos.Where(x => x.Id == documento).Select(x => x.Estado).SingleAsync()));
    }

    [Fact]
    public async Task Lista_traz_validade_e_situacao()
    {
        var (i, admin, categoria) = await Preparar(f);
        var casos = new (DateOnly validoAte, string situacao)[]
        {
            (new DateOnly(2027, 11, 17), "VIGENTE"),
            (Hoje.AddDays(31), "VIGENTE"),
            (Hoje.AddDays(30), "VENCE_EM_BREVE"),
            (Hoje, "VENCE_EM_BREVE"),
            (Hoje.AddDays(-1), "VENCIDO"),
        };

        foreach (var (validoAte, situacao) in casos)
        {
            var artigo = await admin.NovoArtigo(categoria);
            var documento = await admin.NovoDocumento(categoria);
            await f.ValidadeDoArtigo(i, artigo, validoAte);
            await f.ValidadeDoDocumento(i, documento, validoAte);

            var a = await admin.Item("artigos", artigo);
            var d = await admin.Item("documentos", documento);

            var esperado = (validoAte.ToString("yyyy-MM-dd"), situacao);
            Assert.Equal(esperado, (a.GetProperty("validoAte").GetString(), a.GetProperty("validade").GetString()));
            Assert.Equal(esperado, (d.GetProperty("validoAte").GetString(), d.GetProperty("validade").GetString()));
        }
    }

    [Fact]
    public async Task Resumo_conta_vigentes_a_vencer_e_vencidos()
    {
        var (i, admin, categoria) = await Preparar(f);
        var (vigente, aVencer, vencido) = (await admin.NovoArtigo(categoria), await admin.NovoArtigo(categoria), await admin.NovoArtigo(categoria));
        await f.ValidadeDoArtigo(i, aVencer, Hoje.AddDays(30));
        await f.ValidadeDoArtigo(i, vencido, Hoje.AddDays(-1));
        var documentoVencido = await admin.NovoDocumento(categoria);
        await admin.NovoDocumento(categoria);
        await f.ValidadeDoDocumento(i, documentoVencido, Hoje.AddDays(-1));
        var rascunho = await admin.NovoArtigo(categoria, ativar: false);
        await f.ValidadeDoArtigo(i, rascunho, Hoje.AddDays(-1));
        var arquivado = await admin.NovoArtigo(categoria);
        await f.ValidadeDoArtigo(i, arquivado, Hoje.AddDays(-1));
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/admin/artigos/{arquivado}/arquivar", null)).StatusCode);
        Assert.NotEqual(vigente, aVencer);

        var r = await admin.GetAsync("/api/conhecimento/validade");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("""{"artigos":{"vigentes":1,"venceEm30Dias":1,"vencidos":1},"documentos":{"vigentes":1,"venceEm30Dias":0,"vencidos":1}}""", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Resumo_exige_administrador()
    {
        var i = await f.Instituicao();
        var setor = await f.Db(i.Id, db => db.Setores.SingleAsync());

        Assert.Equal(HttpStatusCode.Forbidden, (await (await f.Atendente(i, setor)).Cliente.GetAsync("/api/conhecimento/validade")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await f.AlunoLogado(i)).Cliente.GetAsync("/api/conhecimento/validade")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Cliente().GetAsync("/api/conhecimento/validade")).StatusCode);
    }
}
