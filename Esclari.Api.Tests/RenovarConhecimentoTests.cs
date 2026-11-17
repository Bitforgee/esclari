using System.Net;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class RenovarConhecimentoTests(EsclariFactoryValidade f) : IClassFixture<EsclariFactoryValidade>
{
    async Task<(Instituicao i, HttpClient admin, long categoria)> Preparar()
    {
        var (i, admin) = await Admin.Criar(f);
        return (i, admin, (await f.Categoria(i, "Acadêmico")).Id);
    }

    Task<int> VersoesDoArtigo(Instituicao i, long id) => f.Db(i.Id, db => db.VersoesArtigo.CountAsync(v => v.ArtigoId == id));
    Task<int> VersoesDoDocumento(Instituicao i, long id) => f.Db(i.Id, db => db.VersoesDocumento.CountAsync(v => v.DocumentoId == id));
    Task<DateOnly> ValidadeArtigo(Instituicao i, long id) => f.Db(i.Id, db => db.Artigos.Where(a => a.Id == id).Select(a => a.ValidoAte).SingleAsync());
    Task<DateOnly> ValidadeDocumento(Instituicao i, long id) => f.Db(i.Id, db => db.Documentos.Where(d => d.Id == id).Select(d => d.ValidoAte).SingleAsync());

    [Fact]
    public async Task Renovar_artigo_ativo_nao_cria_versao()
    {
        var (i, admin, categoria) = await Preparar();
        var id = await admin.NovoArtigo(categoria);
        await f.ValidadeDoArtigo(i, id, new DateOnly(2026, 12, 1));

        foreach (var _ in new[] { 1, 2 })
        {
            var r = await admin.PostAsync($"/api/admin/artigos/{id}/renovar", null);

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            var j = await r.Json();
            Assert.Equal(("2027-11-17", "VIGENTE", "ATIVO"), (j.GetProperty("validoAte").GetString(), j.GetProperty("validade").GetString(), j.GetProperty("estado").GetString()));
            Assert.Equal(1, j.GetProperty("versao").GetProperty("numero").GetInt32());
            Assert.Equal(1, await VersoesDoArtigo(i, id));
            Assert.Equal(new DateOnly(2027, 11, 17), await ValidadeArtigo(i, id));
        }
    }

    [Fact]
    public async Task Renovar_rascunho_responde_200()
    {
        var (i, admin, categoria) = await Preparar();
        var id = await admin.NovoArtigo(categoria, ativar: false);
        await f.ValidadeDoArtigo(i, id, new DateOnly(2026, 11, 20));

        var r = await admin.PostAsync($"/api/admin/artigos/{id}/renovar", null);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(("2027-11-17", "RASCUNHO"), ((await r.Json()).GetProperty("validoAte").GetString(), (await r.Json()).GetProperty("estado").GetString()));
        Assert.Equal(new DateOnly(2027, 11, 17), await ValidadeArtigo(i, id));
    }

    [Fact]
    public async Task Renovar_documento_ativo_nao_cria_versao()
    {
        var (i, admin, categoria) = await Preparar();
        var id = await admin.NovoDocumento(categoria);
        await f.ValidadeDoDocumento(i, id, new DateOnly(2026, 11, 1));

        var r = await admin.PostAsync($"/api/admin/documentos/{id}/renovar", null);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        Assert.Equal(("2027-11-17", "VIGENTE"), (j.GetProperty("validoAte").GetString(), j.GetProperty("validade").GetString()));
        Assert.Equal(1, j.GetProperty("versao").GetProperty("numero").GetInt32());
        Assert.Equal(1, await VersoesDoDocumento(i, id));
        Assert.Equal(new DateOnly(2027, 11, 17), await ValidadeDocumento(i, id));
    }

    [Fact]
    public async Task Renovar_artigo_arquivado_responde_409()
    {
        var (i, admin, categoria) = await Preparar();
        var id = await admin.NovoArtigo(categoria);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/admin/artigos/{id}/arquivar", null)).StatusCode);
        await f.ValidadeDoArtigo(i, id, new DateOnly(2026, 11, 10));

        var r = await admin.PostAsync($"/api/admin/artigos/{id}/renovar", null);

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("Item arquivado não pode ser renovado.", (await r.Json()).GetProperty("detail").GetString());
        Assert.Equal(new DateOnly(2026, 11, 10), await ValidadeArtigo(i, id));
    }

    [Fact]
    public async Task Renovar_documento_arquivado_responde_409()
    {
        var (i, admin, categoria) = await Preparar();
        var id = await admin.NovoDocumento(categoria);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/admin/documentos/{id}/arquivar", null)).StatusCode);
        await f.ValidadeDoDocumento(i, id, new DateOnly(2026, 11, 10));

        var r = await admin.PostAsync($"/api/admin/documentos/{id}/renovar", null);

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("Item arquivado não pode ser renovado.", (await r.Json()).GetProperty("detail").GetString());
        Assert.Equal(new DateOnly(2026, 11, 10), await ValidadeDocumento(i, id));
    }

    [Fact]
    public async Task Renovar_inexistente_ou_de_outra_instituicao_responde_404()
    {
        var (_, admin, _) = await Preparar();
        var (j, outroAdmin, outraCategoria) = await Preparar();
        var artigoAlheio = await outroAdmin.NovoArtigo(outraCategoria);
        var documentoAlheio = await outroAdmin.NovoDocumento(outraCategoria);
        await f.ValidadeDoArtigo(j, artigoAlheio, new DateOnly(2026, 11, 10));
        await f.ValidadeDoDocumento(j, documentoAlheio, new DateOnly(2026, 11, 10));

        var respostas = new List<(HttpResponseMessage r, string detalhe)>
        {
            (await admin.PostAsync("/api/admin/artigos/999999/renovar", null), "Artigo não encontrado."),
            (await admin.PostAsync($"/api/admin/artigos/{artigoAlheio}/renovar", null), "Artigo não encontrado."),
            (await admin.PostAsync("/api/admin/documentos/999999/renovar", null), "Documento não encontrado."),
            (await admin.PostAsync($"/api/admin/documentos/{documentoAlheio}/renovar", null), "Documento não encontrado."),
        };

        foreach (var (r, detalhe) in respostas)
        {
            Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
            Assert.Equal(detalhe, (await r.Json()).GetProperty("detail").GetString());
        }
        Assert.Equal(new DateOnly(2026, 11, 10), await ValidadeArtigo(j, artigoAlheio));
        Assert.Equal(new DateOnly(2026, 11, 10), await ValidadeDocumento(j, documentoAlheio));
    }

    [Fact]
    public async Task Renovar_exige_administrador()
    {
        var (i, admin, categoria) = await Preparar();
        var artigo = await admin.NovoArtigo(categoria);
        var documento = await admin.NovoDocumento(categoria);
        await f.ValidadeDoArtigo(i, artigo, new DateOnly(2026, 11, 10));
        await f.ValidadeDoDocumento(i, documento, new DateOnly(2026, 11, 10));
        var setor = await f.Db(i.Id, db => db.Setores.SingleAsync());
        var clientes = new[] { ((await f.Atendente(i, setor)).Cliente, HttpStatusCode.Forbidden), ((await f.AlunoLogado(i)).Cliente, HttpStatusCode.Forbidden), (f.Cliente(), HttpStatusCode.Unauthorized) };

        foreach (var (cliente, status) in clientes)
            foreach (var rota in new[] { $"/api/admin/artigos/{artigo}/renovar", $"/api/admin/documentos/{documento}/renovar" })
                Assert.Equal(status, (await cliente.PostAsync(rota, null)).StatusCode);

        Assert.Equal(new DateOnly(2026, 11, 10), await ValidadeArtigo(i, artigo));
        Assert.Equal(new DateOnly(2026, 11, 10), await ValidadeDocumento(i, documento));
    }
}
