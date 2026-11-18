using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class ImplantacaoProntidaoTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    OperadorLogado? operador;

    async Task<HttpClient> Cliente() => (operador ??= await f.Operador()).Cliente;

    async Task<System.Text.Json.JsonElement> Prontidao(Instituicao i) =>
        (await (await (await Cliente()).GetAsync($"/api/plataforma/instituicoes/{i.Slug}")).Json());

    static Dictionary<string, bool> Itens(System.Text.Json.JsonElement detalhe) =>
        detalhe.GetProperty("prontidao").GetProperty("itens").EnumerateArray().ToDictionary(x => x.GetProperty("codigo").GetString()!, x => x.GetProperty("ok").GetBoolean());

    [Fact]
    public async Task Quatro_itens_e_pronta()
    {
        var c = await Cliente();
        var i = await f.Instituicao();

        var vazio = await Prontidao(i);
        Assert.Equal(["ADMINISTRADOR_COM_SENHA", "ATENDENTE_NO_SETOR", "CATEGORIA_COM_REGRA", "ARTIGO_ATIVO"], vazio.GetProperty("prontidao").GetProperty("itens").EnumerateArray().Select(x => x.GetProperty("codigo").GetString()));
        Assert.All(Itens(vazio).Values, ok => Assert.False(ok));
        Assert.False(vazio.GetProperty("prontidao").GetProperty("pronta").GetBoolean());

        await f.Usuario(i, [Papeis.Administrador], senha: null);
        Assert.False(Itens(await Prontidao(i))["ADMINISTRADOR_COM_SENHA"]);
        await f.Usuario(i, [Papeis.Administrador]);
        Assert.True(Itens(await Prontidao(i))["ADMINISTRADOR_COM_SENHA"]);

        var setor = await f.Setor(i, "Financeiro");
        var atendente = await f.Usuario(i, [Papeis.Atendente]);
        Assert.False(Itens(await Prontidao(i))["ATENDENTE_NO_SETOR"]);
        await f.Db(i.Id, async db =>
        {
            var u = await db.Usuarios.Include(x => x.Setores).Include(x => x.Papeis).SingleAsync(x => x.Id == atendente.Id);
            u.DefinirAcesso([Papeis.Atendente], [setor.Id]);
            await db.SaveChangesAsync();
        });
        Assert.True(Itens(await Prontidao(i))["ATENDENTE_NO_SETOR"]);
        await f.Db(i.Id, db => db.Usuarios.Where(x => x.Id == atendente.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Ativo, false)));
        Assert.False(Itens(await Prontidao(i))["ATENDENTE_NO_SETOR"]);
        await f.Db(i.Id, db => db.Usuarios.Where(x => x.Id == atendente.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Ativo, true)));

        Assert.False(Itens(await Prontidao(i))["CATEGORIA_COM_REGRA"]);
        await f.Db(i.Id, async db =>
        {
            var padrao = await db.Categorias.SingleAsync(x => x.Padrao);
            var fluxo = Fluxo.Criar(padrao.Id, null, null);
            fluxo.SalvarRascunho([new EtapaFluxo.Dados(DestinosEtapa.Setor, (await db.Setores.FirstAsync()).Id, null, null, null)]);
            fluxo.Publicar(f.Relogio.GetUtcNow().UtcDateTime);
            db.Fluxos.Add(fluxo);
            await db.SaveChangesAsync();
        });
        Assert.False(Itens(await Prontidao(i))["CATEGORIA_COM_REGRA"]);
        await c.PostAsync($"/api/plataforma/instituicoes/{i.Slug}/carga/modelo/padrao-esclari", null);
        Assert.True(Itens(await Prontidao(i))["CATEGORIA_COM_REGRA"]);

        Assert.False(Itens(await Prontidao(i))["ARTIGO_ATIVO"]);
        var categoria = await f.Db(i.Id, db => db.Categorias.Where(x => !x.Padrao).Select(x => x.Id).FirstAsync());
        var vencido = await f.Artigo(i, "Antigo", "Texto vencido.", categoriaId: categoria);
        var ontem = DateOnly.FromDateTime(f.Relogio.GetUtcNow().UtcDateTime).AddDays(-1);
        await f.Db(i.Id, db => db.Artigos.Where(x => x.Id == vencido.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.ValidoAte, ontem)));
        Assert.False(Itens(await Prontidao(i))["ARTIGO_ATIVO"]);
        await f.Artigo(i, "Boleto", "Veja o portal.", categoriaId: categoria);
        var depois = await Prontidao(i);
        Assert.True(Itens(depois)["ARTIGO_ATIVO"], depois.ToString());
        var pronta = await Prontidao(i);
        Assert.All(Itens(pronta).Values, ok => Assert.True(ok));
        Assert.True(pronta.GetProperty("prontidao").GetProperty("pronta").GetBoolean());
    }

    [Fact]
    public async Task Situacao_configurando_ativa_desativada()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        await f.Usuario(i, [Papeis.Administrador]);
        var setor = await f.Setor(i, "Financeiro");
        var atendente = await f.Usuario(i, [Papeis.Atendente]);
        await f.Db(i.Id, async db =>
        {
            var u = await db.Usuarios.Include(x => x.Setores).Include(x => x.Papeis).SingleAsync(x => x.Id == atendente.Id);
            u.DefinirAcesso([Papeis.Atendente], [setor.Id]);
            await db.SaveChangesAsync();
        });
        await c.PostAsync($"/api/plataforma/instituicoes/{i.Slug}/carga/modelo/padrao-esclari", null);

        Assert.Equal("CONFIGURANDO", (await Prontidao(i)).GetProperty("situacao").GetString());
        var categoria = await f.Db(i.Id, db => db.Categorias.Where(x => !x.Padrao).Select(x => x.Id).FirstAsync());
        await f.Artigo(i, "Boleto", "Veja o portal.", categoriaId: categoria);
        var depois = await Prontidao(i);
        Assert.True(Itens(depois)["ARTIGO_ATIVO"], depois.ToString());
        Assert.Equal("ATIVA", (await Prontidao(i)).GetProperty("situacao").GetString());
        await c.PostAsJsonAsync($"/api/plataforma/instituicoes/{i.Slug}/desativar", new { motivo = "Fim do teste" });
        Assert.Equal("DESATIVADA", (await Prontidao(i)).GetProperty("situacao").GetString());
    }
}
