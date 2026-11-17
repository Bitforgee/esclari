using System.Net;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Conhecimento;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class BuscaValidadeTests(EsclariFactoryValidade f) : IClassFixture<EsclariFactoryValidade>
{
    static readonly DateOnly Hoje = EsclariFactoryValidade.Hoje;

    Task<List<string>> Titulos(Instituicao i, string consulta) =>
        f.Db(i.Id, async db => (await BuscarFontes.Buscar(db, LlmFalso.Vetor(consulta), Hoje, 10)).Select(x => x.Titulo).Order().ToList());

    [Fact]
    public async Task Artigo_vencido_sai_da_busca_e_o_do_dia_fica()
    {
        var i = await f.Instituicao();
        var doDia = await f.Artigo(i, "Boleto do dia", "Como pagar o boleto vencido pelo portal.");
        var deOntem = await f.Artigo(i, "Boleto de ontem", "Como pagar o boleto vencido pelo portal.");
        Assert.Equal(["Boleto de ontem", "Boleto do dia"], await Titulos(i, "boleto vencido"));

        await f.ValidadeDoArtigo(i, doDia.Id, Hoje);
        await f.ValidadeDoArtigo(i, deOntem.Id, Hoje.AddDays(-1));

        Assert.Equal(["Boleto do dia"], await Titulos(i, "boleto vencido"));
    }

    [Fact]
    public async Task Documento_vencido_sai_da_busca()
    {
        var i = await f.Instituicao();
        var deOntem = await f.DocumentoSemeado(i, "Calendário de ontem", "Calendário acadêmico do semestre letivo.");
        var doDia = await f.DocumentoSemeado(i, "Calendário do dia", "Calendário acadêmico do semestre letivo.");
        await f.ValidadeDoDocumento(i, deOntem, Hoje.AddDays(-1));
        await f.ValidadeDoDocumento(i, doDia, Hoje);

        Assert.Equal(["Calendário do dia"], await Titulos(i, "calendário acadêmico"));
    }

    [Fact]
    public async Task Renovar_volta_a_busca_sem_reindexar()
    {
        var (i, admin) = await Admin.Criar(f);
        var artigo = await f.Artigo(i, "Boleto vencido", "Como pagar o boleto vencido pelo portal.");
        var documento = await f.DocumentoSemeado(i, "Calendário acadêmico", "Calendário acadêmico do semestre letivo.");
        await f.ValidadeDoArtigo(i, artigo.Id, Hoje.AddDays(-1));
        await f.ValidadeDoDocumento(i, documento, Hoje.AddDays(-1));
        Assert.Empty(await Titulos(i, "boleto vencido calendário acadêmico"));
        var trechos = await f.Db(i.Id, db => db.TrechosFonte.Select(t => new { t.Id, Indexado = t.Embedding != null }).OrderBy(t => t.Id).ToListAsync());

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/admin/artigos/{artigo.Id}/renovar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/admin/documentos/{documento}/renovar", null)).StatusCode);

        Assert.Equal(["Boleto vencido", "Calendário acadêmico"], await Titulos(i, "boleto vencido calendário acadêmico"));
        Assert.Equal(trechos, await f.Db(i.Id, db => db.TrechosFonte.Select(t => new { t.Id, Indexado = t.Embedding != null }).OrderBy(t => t.Id).ToListAsync()));
    }
}
