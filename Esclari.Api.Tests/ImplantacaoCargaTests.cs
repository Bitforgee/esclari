using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class ImplantacaoCargaTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    OperadorLogado? operador;

    async Task<HttpClient> Cliente() => (operador ??= await f.Operador()).Cliente;

    static MultipartFormDataContent Arquivo(byte[] bytes)
    {
        var form = new MultipartFormDataContent { { new ByteArrayContent(bytes), "arquivo", "carga.json" } };
        return form;
    }

    static MultipartFormDataContent Arquivo(string json) => Arquivo(Encoding.UTF8.GetBytes(json));

    const string Valida = """{"setores":[{"nome":"Financeiro"},{"nome":"Secretaria"}],"categorias":[{"nome":"Boleto"}],"regras":[{"categoria":"Boleto","setor":"Financeiro"}]}""";

    [Fact]
    public async Task Simular_conta_e_nao_grava()
    {
        var c = await Cliente();
        var i = await f.Instituicao();

        var r = await c.PostAsync($"/api/plataforma/instituicoes/{i.Slug}/carga?simular=true", Arquivo(Valida));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var corpo = await r.Json();
        Assert.Equal(2, corpo.GetProperty("setores").GetInt32());
        Assert.Equal(1, corpo.GetProperty("categorias").GetInt32());
        Assert.Equal(1, corpo.GetProperty("regras").GetInt32());
        Assert.Equal(1, await f.Db(i.Id, db => db.Setores.CountAsync()));
        Assert.Equal(1, await f.Db(i.Id, db => db.Categorias.CountAsync()));
        Assert.Equal(0, await f.Db(null, db => db.AuditoriaDaPlataforma.CountAsync(a => a.InstituicaoAlvoId == i.Id && a.Acao == AcoesDaPlataforma.CargaImportada)));
    }

    [Fact]
    public async Task Arquivo_invalido_400_com_todos_os_erros()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        const string invalida = """{"setores":[{"nome":"A"},{"nome":"A"}],"regras":[{"categoria":"Inexistente","setor":"Nenhum"}]}""";

        foreach (var simular in new[] { "true", "false" })
        {
            var r = await c.PostAsync($"/api/plataforma/instituicoes/{i.Slug}/carga?simular={simular}", Arquivo(invalida));
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.True((await r.Json()).GetProperty("errors").GetProperty("arquivo").GetArrayLength() >= 2);
        }
        Assert.Equal(1, await f.Db(i.Id, db => db.Setores.CountAsync()));
    }

    [Fact]
    public async Task Arquivo_fora_de_utf8_400()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        var latin1 = Encoding.Latin1.GetBytes("""{"setores":[{"nome":"Secretaria Acadêmica"}]}""");

        var r = await c.PostAsync($"/api/plataforma/instituicoes/{i.Slug}/carga", Arquivo(latin1));

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("O arquivo deve estar em UTF-8.", (await r.Json()).GetProperty("errors").GetProperty("arquivo")[0].GetString());
    }

    [Fact]
    public async Task Importar_duas_vezes_nao_duplica()
    {
        var c = await Cliente();
        var i = await f.Instituicao();

        var bruta = await c.PostAsync($"/api/plataforma/instituicoes/{i.Slug}/carga", Arquivo(Valida));
        Assert.True(bruta.IsSuccessStatusCode, await bruta.Content.ReadAsStringAsync());
        var primeira = await bruta.Json();
        var segunda = await (await c.PostAsync($"/api/plataforma/instituicoes/{i.Slug}/carga", Arquivo(Valida))).Json();

        Assert.Equal(2, primeira.GetProperty("setores").GetInt32());
        Assert.Equal(0, segunda.GetProperty("setores").GetInt32());
        Assert.Equal(0, segunda.GetProperty("categorias").GetInt32());
        Assert.Equal(3, await f.Db(i.Id, db => db.Setores.CountAsync()));
        Assert.Equal(2, await f.Db(i.Id, db => db.Categorias.CountAsync()));
    }

    [Fact]
    public async Task Modelos_listam_o_padrao_esclari()
    {
        var c = await Cliente();

        var r = await c.GetAsync("/api/plataforma/modelos-de-carga");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var modelo = Assert.Single((await r.Json()).EnumerateArray());
        Assert.Equal("padrao-esclari", modelo.GetProperty("id").GetString());
        Assert.Equal([3, 6, 6], [modelo.GetProperty("setores").GetInt32(), modelo.GetProperty("categorias").GetInt32(), modelo.GetProperty("regras").GetInt32()]);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Cliente().GetAsync("/api/plataforma/modelos-de-carga")).StatusCode);
    }

    [Fact]
    public async Task Aplicar_modelo_grava_e_audita()
    {
        var c = await Cliente();
        var i = await f.Instituicao();

        var r = await c.PostAsync($"/api/plataforma/instituicoes/{i.Slug}/carga/modelo/padrao-esclari", null);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var corpo = await r.Json();
        Assert.Equal([3, 6, 6, 0], [corpo.GetProperty("setores").GetInt32(), corpo.GetProperty("categorias").GetInt32(), corpo.GetProperty("regras").GetInt32(), corpo.GetProperty("artigos").GetInt32()]);
        Assert.Equal(0, await f.Db(i.Id, db => db.Artigos.CountAsync()));
        Assert.Equal(1, await f.Db(null, db => db.AuditoriaDaPlataforma.CountAsync(a => a.InstituicaoAlvoId == i.Id && a.Acao == AcoesDaPlataforma.CargaImportada)));
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync($"/api/plataforma/instituicoes/{i.Slug}/carga/modelo/nao-existe", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync("/api/plataforma/instituicoes/nao-existe-xx/carga/modelo/padrao-esclari", null)).StatusCode);
    }
}
