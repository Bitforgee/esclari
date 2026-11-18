using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class PlataformaInstituicoesTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    const string Lista = "/api/plataforma/instituicoes";
    OperadorLogado? operador;

    async Task<HttpClient> Cliente() => (operador ??= await f.Operador()).Cliente;

    static object Nova(string slug, string? nome = null) =>
        new { slug, nome = nome ?? "Faculdade Teste", fuso = "America/Cuiaba", admin = new { cpf = Semente.NovoCpf(), nome = "Dona Admin", email = $"{Guid.NewGuid():N}@teste.edu.br" } };

    static MultipartFormDataContent Carga()
    {
        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("{\"setores\":[{\"nome\":\"Financeiro\"}]}")), "arquivo", "carga.json");
        return form;
    }

    [Fact]
    public async Task Rotas_de_hoje_mantem_o_contrato_com_a_sessao_da_plataforma()
    {
        var c = await Cliente();
        var slug = Semente.NovoSlug();
        var alvo = await f.Instituicao();
        await f.Usuario(alvo, [Papeis.Administrador]);

        var listar = await c.GetAsync(Lista);
        var criar = await c.PostAsJsonAsync(Lista, Nova(slug));
        var carga = await c.PostAsync($"{Lista}/{alvo.Slug}/carga", Carga());
        var desativar = await c.PostAsJsonAsync($"{Lista}/{alvo.Slug}/desativar", new { motivo = "Encerrado pela equipe" });
        var reativar = await c.PostAsync($"{Lista}/{alvo.Slug}/reativar", null);
        var reenviar = await c.PostAsync($"{Lista}/{alvo.Slug}/reenviar-acesso", null);

        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.Accepted],
            [listar.StatusCode, criar.StatusCode, carga.StatusCode, desativar.StatusCode, reativar.StatusCode, reenviar.StatusCode]);
        Assert.Equal($"{Lista}/{slug}", criar.Headers.Location!.OriginalString);
        Assert.Equal(["slug", "nome", "fuso", "ativa", "criadaEm", "administradores"], (await criar.Json()).EnumerateObject().Select(p => p.Name));
        Assert.Equal(["setores", "cursos", "categorias", "regras", "artigos"], (await carga.Json()).EnumerateObject().Select(p => p.Name));
        var inativada = await desativar.Json();
        Assert.Equal((alvo.Slug, false), (inativada.GetProperty("slug").GetString(), inativada.GetProperty("ativa").GetBoolean()));
        Assert.Equal(1, (await reenviar.Json()).GetProperty("enviados").GetInt32());
    }

    [Fact]
    public async Task Rotas_sem_sessao_da_plataforma_401()
    {
        var i = await f.Instituicao();
        var admin = await f.Entrar(i, await f.Usuario(i, [Papeis.Administrador]));
        var json = () => JsonContent.Create(new { motivo = "abc", nome = "Abc", fuso = "America/Cuiaba" });

        foreach (var c in new[] { f.Cliente(), admin })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync(Lista)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync($"{Lista}/{i.Slug}")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await c.PutAsync($"{Lista}/{i.Slug}", json())).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsync(Lista, json())).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsync($"{Lista}/{i.Slug}/desativar", json())).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsync($"{Lista}/{i.Slug}/reativar", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsync($"{Lista}/{i.Slug}/reenviar-acesso", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsync($"{Lista}/{i.Slug}/carga", Carga())).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/plataforma/auditoria")).StatusCode);
        }
        Assert.True((await f.Db(null, db => db.Instituicoes.SingleAsync(x => x.Id == i.Id))).Ativa);
    }

    [Fact]
    public async Task Slug_plataforma_e_reservado()
    {
        var c = await Cliente();
        var antes = await f.Db(null, db => db.Instituicoes.CountAsync());

        var r = await c.PostAsJsonAsync(Lista, Nova("plataforma"));

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(["slug"], (await r.Json()).GetProperty("errors").EnumerateObject().Select(p => p.Name));
        Assert.Equal(antes, await f.Db(null, db => db.Instituicoes.CountAsync()));
    }

    [Fact]
    public async Task Desativar_exige_motivo_de_3_a_200_caracteres()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        foreach (var motivo in new[] { "", "ab", new string('x', 201) })
        {
            var r = await c.PostAsJsonAsync($"{Lista}/{i.Slug}/desativar", new { motivo });
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.True((await f.Db(null, db => db.Instituicoes.SingleAsync(x => x.Id == i.Id))).Ativa);
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync($"{Lista}/{i.Slug}/desativar", new { })).StatusCode);

        var ok = await c.PostAsJsonAsync($"{Lista}/{i.Slug}/desativar", new { motivo = "Contrato encerrado" });

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.False((await f.Db(null, db => db.Instituicoes.SingleAsync(x => x.Id == i.Id))).Ativa);
        Assert.Contains("Contrato encerrado", await f.Db(null, db => db.AuditoriaDaPlataforma.Where(a => a.InstituicaoAlvoId == i.Id && a.Acao == "INSTITUICAO_DESATIVADA").Select(a => a.Resumo).SingleAsync()));
    }

    [Fact]
    public async Task Detalhe_200_mascarado_e_404()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        var admin = await f.Usuario(i, [Papeis.Administrador], senha: null);
        await f.Usuario(i, [Papeis.Aluno]);

        var r = await c.GetAsync($"{Lista}/{i.Slug}");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        Assert.Equal(i.Slug, j.GetProperty("slug").GetString());
        Assert.Equal("AGUARDANDO_PRIMEIRO_ACESSO", j.GetProperty("situacao").GetString());
        var administradores = j.GetProperty("administradoresDaInstituicao").EnumerateArray().ToList();
        var unico = Assert.Single(administradores);
        Assert.Equal(admin.Email[..1] + "***" + admin.Email[admin.Email.IndexOf('@')..], unico.GetProperty("emailMascarado").GetString());
        Assert.True(unico.GetProperty("primeiroAcessoPendente").GetBoolean());
        Assert.Equal(["nome", "emailMascarado", "primeiroAcessoPendente"], unico.EnumerateObject().Select(p => p.Name));
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"{Lista}/nao-existe")).StatusCode);
    }

    [Fact]
    public async Task Editar_altera_so_nome_e_fuso()
    {
        var c = await Cliente();
        var i = await f.Instituicao();

        var r = await c.PutAsJsonAsync($"{Lista}/{i.Slug}", new { nome = "Nome Novo da Faculdade", fuso = "America/Sao_Paulo", slug = "outro-slug" });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        Assert.Equal(("Nome Novo da Faculdade", "America/Sao_Paulo", i.Slug), (j.GetProperty("nome").GetString(), j.GetProperty("fuso").GetString(), j.GetProperty("slug").GetString()));
        var linha = await f.Db(null, db => db.Instituicoes.AsNoTracking().SingleAsync(x => x.Id == i.Id));
        Assert.Equal(("Nome Novo da Faculdade", "America/Sao_Paulo", i.Slug, i.Cor), (linha.Nome, linha.Fuso, linha.Slug, linha.Cor));
    }

    [Fact]
    public async Task Editar_400_por_campo_e_404()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        var antes = await f.Db(null, db => db.Instituicoes.AsNoTracking().SingleAsync(x => x.Id == i.Id));

        foreach (var (corpo, campo) in new (object, string)[]
        {
            (new { nome = "ab", fuso = "America/Cuiaba" }, "nome"),
            (new { nome = new string('n', 121), fuso = "America/Cuiaba" }, "nome"),
            (new { nome = "Nome Valido", fuso = "Marte/Olimpo" }, "fuso"),
        })
        {
            var r = await c.PutAsJsonAsync($"{Lista}/{i.Slug}", corpo);
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Equal([campo], (await r.Json()).GetProperty("errors").EnumerateObject().Select(p => p.Name));
        }
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync($"{Lista}/nao-existe", new { nome = "Nome Valido", fuso = "America/Cuiaba" })).StatusCode);
        var depois = await f.Db(null, db => db.Instituicoes.AsNoTracking().SingleAsync(x => x.Id == i.Id));
        Assert.Equal((antes.Nome, antes.Fuso), (depois.Nome, depois.Fuso));
    }
}
