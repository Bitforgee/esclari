using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class TiposDeUsuarioTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    const string Rota = "/api/admin/tipos";

    static object Corpo(string nome, params string[] funcoes) => new { nome, funcoes };

    async Task<(Instituicao i, HttpClient gestor)> Gestor()
    {
        var i = await f.Instituicao();
        return (i, await f.Entrar(i, await f.Usuario(i, [Papeis.Administrador])));
    }

    Task<List<TipoDeUsuario>> Tipos(Instituicao i) => f.Db(i.Id, db => db.TiposDeUsuario.Include(t => t.Funcoes).OrderBy(t => t.Id).ToListAsync());

    static string[] Funcoes(TipoDeUsuario t) => [.. t.Funcoes.Select(x => x.Funcao).Order(StringComparer.Ordinal)];

    static string[] Lista(System.Text.Json.JsonElement j) => [.. j.EnumerateArray().Select(x => x.GetString()!)];

    [Fact]
    public async Task Listar_devolve_tipos_e_catalogo()
    {
        var (i, gestor) = await Gestor();

        var r = await gestor.GetAsync(Rota);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        var tipos = j.GetProperty("tipos").EnumerateArray().ToList();
        Assert.Equal(["ALUNO", "ATENDENTE", "GESTOR"], tipos.Select(t => t.GetProperty("sistema").GetString()));
        Assert.Equal([0, 0, 1], tipos.Select(t => t.GetProperty("usuarios").GetInt32()));
        Assert.Equal(["assistente.usar"], Lista(tipos[0].GetProperty("funcoes")));
        Assert.Equal(["fila.atender", "indicadores.ver", "mcp.usar"], Lista(tipos[1].GetProperty("funcoes")));
        Assert.Equal(11, tipos[2].GetProperty("funcoes").GetArrayLength());
        var catalogo = j.GetProperty("catalogo").EnumerateArray().ToList();
        Assert.Equal(12, catalogo.Count);
        Assert.Equal(Esclari.Api.Dados.Usuarios.Funcoes.Todas, catalogo.Select(c => c.GetProperty("codigo").GetString()));
        Assert.All(catalogo, c =>
        {
            Assert.NotEqual("", c.GetProperty("rotulo").GetString());
            Assert.NotEqual("", c.GetProperty("descricao").GetString());
        });
        Assert.Equal(["chamados.ver_todos", "fila.atender", "matriculas.importar", "mcp.usar", "usuarios.gerenciar"],
            catalogo.Where(c => c.GetProperty("expoeDadosPessoais").GetBoolean()).Select(c => c.GetProperty("codigo").GetString()!).Order());
        Assert.Equal(["assistente.usar"], catalogo.Where(c => c.GetProperty("deAluno").GetBoolean()).Select(c => c.GetProperty("codigo").GetString()));
        await f.Usuario(i, [Papeis.Aluno]);
        var depois = (await (await gestor.GetAsync(Rota)).Json()).GetProperty("tipos").EnumerateArray().Select(t => t.GetProperty("usuarios").GetInt32());
        Assert.Equal([1, 0, 1], depois);
    }

    [Fact]
    public async Task Listar_aceita_tipos_gerenciar_ou_usuarios_gerenciar()
    {
        var i = await f.Instituicao();
        var porUsuarios = await f.ComFuncoes(i, [Esclari.Api.Dados.Usuarios.Funcoes.UsuariosGerenciar]);
        var porTipos = await f.ComFuncoes(i, [Esclari.Api.Dados.Usuarios.Funcoes.TiposGerenciar]);
        var nenhuma = await f.ComFuncoes(i, [Esclari.Api.Dados.Usuarios.Funcoes.IndicadoresVer, Esclari.Api.Dados.Usuarios.Funcoes.CadastrosGerenciar]);

        foreach (var c in new[] { porUsuarios.Cliente, porTipos.Cliente })
        {
            var r = await c.GetAsync(Rota);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            var j = await r.Json();
            Assert.Equal(6, j.GetProperty("tipos").GetArrayLength());
            Assert.Equal(12, j.GetProperty("catalogo").GetArrayLength());
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await nenhuma.Cliente.GetAsync(Rota)).StatusCode);
    }

    [Fact]
    public async Task Criar_201_com_funcoes_gravadas_juntas()
    {
        var (i, gestor) = await Gestor();

        var r = await gestor.PostAsJsonAsync(Rota, Corpo("Coordenador de curso", "indicadores.ver", "cadastros.gerenciar"));

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var j = await r.Json();
        Assert.Equal("Coordenador de curso", j.GetProperty("nome").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, j.GetProperty("sistema").ValueKind);
        Assert.Equal(["cadastros.gerenciar", "indicadores.ver"], Lista(j.GetProperty("funcoes")));
        Assert.Equal(0, j.GetProperty("usuarios").GetInt32());
        var criado = Assert.Single((await Tipos(i)).Where(t => t.Id == j.GetProperty("id").GetInt64()));
        Assert.Equal((i.Id, (string?)null), (criado.InstituicaoId, criado.Sistema));
        Assert.Equal(["cadastros.gerenciar", "indicadores.ver"], Funcoes(criado));
        Assert.Equal(2, await f.Db(i.Id, db => db.FuncoesDoTipo.CountAsync(x => x.TipoUsuarioId == criado.Id)));
    }

    [Fact]
    public async Task Nome_de_0_a_60_caracteres()
    {
        var (i, gestor) = await Gestor();
        var existente = (await (await gestor.PostAsJsonAsync(Rota, Corpo("Existente", "indicadores.ver"))).Json()).GetProperty("id").GetInt64();
        var antes = (await Tipos(i)).Count;

        foreach (var nome in new[] { "", "   ", new string('n', 61) })
        {
            var post = await gestor.PostAsJsonAsync(Rota, Corpo(nome, "indicadores.ver"));
            var put = await gestor.PutAsJsonAsync($"{Rota}/{existente}", Corpo(nome, "indicadores.ver"));
            foreach (var r in new[] { post, put })
            {
                Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
                Assert.True((await r.Json()).GetProperty("errors").TryGetProperty("nome", out _));
            }
        }
        Assert.Equal(antes, (await Tipos(i)).Count);
        Assert.Equal("Existente", (await Tipos(i)).Single(t => t.Id == existente).Nome);

        var sessenta = new string('n', 60);
        Assert.Equal(HttpStatusCode.Created, (await gestor.PostAsJsonAsync(Rota, Corpo(sessenta, "indicadores.ver"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await gestor.PutAsJsonAsync($"{Rota}/{existente}", Corpo(new string('m', 60), "indicadores.ver"))).StatusCode);
    }

    [Fact]
    public async Task Funcao_fora_do_catalogo_400()
    {
        var (i, gestor) = await Gestor();
        var existente = (await (await gestor.PostAsJsonAsync(Rota, Corpo("Existente", "indicadores.ver"))).Json()).GetProperty("id").GetInt64();
        var antes = (await Tipos(i)).Count;

        foreach (var funcoes in new[] { new[] { "relatorios.ver" }, ["indicadores.ver", "FILA.ATENDER"] })
        {
            var post = await gestor.PostAsJsonAsync(Rota, Corpo("Novo", funcoes));
            var put = await gestor.PutAsJsonAsync($"{Rota}/{existente}", Corpo("Existente", funcoes));
            foreach (var r in new[] { post, put })
            {
                Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
                Assert.True((await r.Json()).GetProperty("errors").TryGetProperty("funcoes", out _));
            }
        }
        var tipos = await Tipos(i);
        Assert.Equal(antes, tipos.Count);
        Assert.Equal(["indicadores.ver"], Funcoes(tipos.Single(t => t.Id == existente)));
    }

    [Fact]
    public async Task Nome_repetido_409()
    {
        var (i, gestor) = await Gestor();
        await gestor.PostAsJsonAsync(Rota, Corpo("Coordenador de curso", "indicadores.ver"));
        var outro = (await (await gestor.PostAsJsonAsync(Rota, Corpo("Outro", "indicadores.ver"))).Json()).GetProperty("id").GetInt64();
        var proprio = (await (await gestor.PostAsJsonAsync(Rota, Corpo("Caixa Alta", "indicadores.ver"))).Json()).GetProperty("id").GetInt64();

        Assert.Equal(HttpStatusCode.Conflict, (await gestor.PostAsJsonAsync(Rota, Corpo("coordenador DE curso", "indicadores.ver"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await gestor.PostAsJsonAsync(Rota, Corpo("gestor"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await gestor.PutAsJsonAsync($"{Rota}/{outro}", Corpo("COORDENADOR DE CURSO", "indicadores.ver"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await gestor.PutAsJsonAsync($"{Rota}/{proprio}", Corpo("CAIXA ALTA", "indicadores.ver"))).StatusCode);

        var nomes = (await Tipos(i)).Select(t => t.Nome).ToList();
        Assert.Equal(1, nomes.Count(n => n.Equals("Coordenador de curso", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains("CAIXA ALTA", nomes);
    }

    [Fact]
    public async Task Limite_de_30_tipos_409()
    {
        var (i, gestor) = await Gestor();
        var vizinha = await f.Instituicao();
        var gestorVizinho = await f.Entrar(vizinha, await f.Usuario(vizinha, [Papeis.Administrador]));
        await f.Db(i.Id, async db =>
        {
            for (var n = 1; n <= 26; n++) db.TiposDeUsuario.Add(TipoDeUsuario.Criar($"Tipo {n}", null, ["indicadores.ver"]));
            await db.SaveChangesAsync();
        });
        Assert.Equal(29, (await Tipos(i)).Count);

        var trinta = await gestor.PostAsJsonAsync(Rota, Corpo("O trigésimo", "indicadores.ver"));
        var trintaEUm = await gestor.PostAsJsonAsync(Rota, Corpo("O trigésimo primeiro", "indicadores.ver"));

        Assert.Equal(HttpStatusCode.Created, trinta.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, trintaEUm.StatusCode);
        Assert.Equal("A instituição já tem 30 tipos de usuário.", (await trintaEUm.Json()).GetProperty("detail").GetString());
        Assert.Equal(30, (await Tipos(i)).Count);
        Assert.Equal(HttpStatusCode.Created, (await gestorVizinho.PostAsJsonAsync(Rota, Corpo("Na vizinha", "indicadores.ver"))).StatusCode);
    }

    [Fact]
    public async Task Alterar_tipo_proprio_e_funcoes_do_atendente_200()
    {
        var (i, gestor) = await Gestor();
        var id = (await (await gestor.PostAsJsonAsync(Rota, Corpo("Antes", "indicadores.ver", "mcp.usar"))).Json()).GetProperty("id").GetInt64();

        var r = await gestor.PutAsJsonAsync($"{Rota}/{id}", Corpo("Depois", "indicadores.ver", "cadastros.gerenciar"));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        Assert.Equal("Depois", j.GetProperty("nome").GetString());
        Assert.Equal(["cadastros.gerenciar", "indicadores.ver"], Lista(j.GetProperty("funcoes")));
        var gravado = (await Tipos(i)).Single(t => t.Id == id);
        Assert.Equal("Depois", gravado.Nome);
        Assert.Equal(["cadastros.gerenciar", "indicadores.ver"], Funcoes(gravado));

        var atendente = (await Tipos(i)).Single(t => t.Sistema == TipoDeUsuario.Atendente);
        var ok = await gestor.PutAsJsonAsync($"{Rota}/{atendente.Id}", Corpo("Atendente", "fila.atender", "indicadores.ver"));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(["fila.atender", "indicadores.ver"], Funcoes((await Tipos(i)).Single(t => t.Id == atendente.Id)));
    }

    [Fact]
    public async Task Tipo_de_sistema_intocavel_409()
    {
        var (i, gestor) = await Gestor();
        var antes = await Tipos(i);
        var (atendente, gestorTipo, aluno) = (antes.Single(t => t.Sistema == "ATENDENTE"), antes.Single(t => t.Sistema == "GESTOR"), antes.Single(t => t.Sistema == "ALUNO"));

        var pedidos = new[]
        {
            (atendente.Id, Corpo("Atendente novo", "fila.atender")),
            (gestorTipo.Id, Corpo("Gestor", "indicadores.ver")),
            (gestorTipo.Id, Corpo("Chefia", "indicadores.ver")),
            (aluno.Id, Corpo("Aluno", "assistente.usar", "indicadores.ver")),
            (aluno.Id, Corpo("Estudante", "assistente.usar")),
        };
        foreach (var (id, corpo) in pedidos)
        {
            var r = await gestor.PutAsJsonAsync($"{Rota}/{id}", corpo);
            Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
            Assert.Equal("Este tipo é de sistema e não pode ser alterado.", (await r.Json()).GetProperty("detail").GetString());
        }
        var depois = await Tipos(i);
        Assert.Equal(antes.Select(t => (t.Id, t.Nome, string.Join(",", Funcoes(t)))), depois.Select(t => (t.Id, t.Nome, string.Join(",", Funcoes(t)))));
    }

    [Fact]
    public async Task So_da_o_que_tem_403()
    {
        var i = await f.Instituicao();
        var gerente = await f.ComFuncoes(i, ["tipos.gerenciar", "indicadores.ver"]);
        var antes = (await Tipos(i)).Count;

        var recusado = await gerente.Cliente.PostAsJsonAsync(Rota, Corpo("Ousado", "usuarios.gerenciar"));
        Assert.Equal(HttpStatusCode.Forbidden, recusado.StatusCode);
        Assert.Equal(antes, (await Tipos(i)).Count);

        var criado = await gerente.Cliente.PostAsJsonAsync(Rota, Corpo("Comedido", "indicadores.ver"));
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var id = (await criado.Json()).GetProperty("id").GetInt64();
        var put = await gerente.Cliente.PutAsJsonAsync($"{Rota}/{id}", Corpo("Comedido", "indicadores.ver", "usuarios.gerenciar"));
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
        Assert.Equal(["indicadores.ver"], Funcoes((await Tipos(i)).Single(t => t.Id == id)));
    }

    [Fact]
    public async Task Apagar_tipo_sem_usuarios_204()
    {
        var (i, gestor) = await Gestor();
        var id = (await (await gestor.PostAsJsonAsync(Rota, Corpo("Temporario", "indicadores.ver", "mcp.usar"))).Json()).GetProperty("id").GetInt64();

        var r = await gestor.DeleteAsync($"{Rota}/{id}");

        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        Assert.DoesNotContain(await Tipos(i), t => t.Id == id);
        Assert.Equal(0, await f.Db(i.Id, db => db.FuncoesDoTipo.CountAsync(x => x.TipoUsuarioId == id)));
        var lista = (await (await gestor.GetAsync(Rota)).Json()).GetProperty("tipos").EnumerateArray();
        Assert.DoesNotContain(lista, t => t.GetProperty("id").GetInt64() == id);
    }

    [Fact]
    public async Task Apagar_tipo_em_uso_409_com_a_quantidade()
    {
        var (i, gestor) = await Gestor();
        var tipo = await f.Tipo(i, "Em uso", "indicadores.ver");
        var u1 = await f.Usuario(i, [], tipo: tipo.Nome);
        var u2 = await f.Usuario(i, [], tipo: tipo.Nome);

        var r = await gestor.DeleteAsync($"{Rota}/{tipo.Id}");

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("2 usuário(s) usam este tipo.", (await r.Json()).GetProperty("detail").GetString());
        Assert.Contains(await Tipos(i), t => t.Id == tipo.Id);

        var aluno = await f.TipoId(i, TipoDeUsuario.Aluno);
        await f.Db(i.Id, db => db.Usuarios.Where(u => u.Id == u1.Id || u.Id == u2.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.TipoUsuarioId, aluno)));
        Assert.Equal(HttpStatusCode.NoContent, (await gestor.DeleteAsync($"{Rota}/{tipo.Id}")).StatusCode);
    }

    [Fact]
    public async Task Apagar_tipo_de_sistema_409()
    {
        var (i, gestor) = await Gestor();

        foreach (var t in (await Tipos(i)).Where(t => t.Sistema is not null))
        {
            var r = await gestor.DeleteAsync($"{Rota}/{t.Id}");
            Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
            Assert.Equal("Este tipo é de sistema e não pode ser apagado.", (await r.Json()).GetProperty("detail").GetString());
        }
        Assert.Equal(3, (await Tipos(i)).Count);
    }

    [Fact]
    public async Task Id_inexistente_ou_de_outra_instituicao_404()
    {
        var (i, gestor) = await Gestor();
        var vizinha = await f.Instituicao();
        var daVizinha = await f.Tipo(vizinha, "Da vizinha", "indicadores.ver");

        foreach (var id in new[] { 999999L, daVizinha.Id })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await gestor.PutAsJsonAsync($"{Rota}/{id}", Corpo("Invadido", "mcp.usar"))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await gestor.DeleteAsync($"{Rota}/{id}")).StatusCode);
        }
        var intacto = (await Tipos(vizinha)).Single(t => t.Id == daVizinha.Id);
        Assert.Equal("Da vizinha", intacto.Nome);
        Assert.Equal(["indicadores.ver"], Funcoes(intacto));
    }

    [Fact]
    public async Task Sem_tipos_gerenciar_403_nas_quatro_rotas()
    {
        var i = await f.Instituicao();
        var setor = await f.Db(i.Id, db => db.Setores.SingleAsync());
        var (atendente, aluno) = ((await f.Atendente(i, setor)).Cliente, (await f.AlunoLogado(i)).Cliente);
        var soUsuarios = (await f.ComFuncoes(i, ["usuarios.gerenciar"])).Cliente;
        var gerente = (await f.ComFuncoes(i, ["tipos.gerenciar", "indicadores.ver"])).Cliente;
        var alvo = (await f.Tipo(i, "Alvo", "indicadores.ver")).Id;

        foreach (var c in new[] { atendente, soUsuarios })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync(Rota, Corpo("Novo", "indicadores.ver"))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.PutAsJsonAsync($"{Rota}/{alvo}", Corpo("Alvo", "indicadores.ver"))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.DeleteAsync($"{Rota}/{alvo}")).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await atendente.GetAsync(Rota)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await aluno.GetAsync(Rota)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await gerente.GetAsync(Rota)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await gerente.PostAsJsonAsync(Rota, Corpo("Novo", "indicadores.ver"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await gerente.PutAsJsonAsync($"{Rota}/{alvo}", Corpo("Alvo", "indicadores.ver"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await gerente.DeleteAsync($"{Rota}/{alvo}")).StatusCode);
    }

    [Fact]
    public async Task Sem_sessao_401_nas_quatro_rotas()
    {
        var anonimo = f.Cliente();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Rota)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsJsonAsync(Rota, Corpo("Novo"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PutAsJsonAsync($"{Rota}/1", Corpo("Novo"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.DeleteAsync($"{Rota}/1")).StatusCode);
    }

    [Fact]
    public async Task Apagar_e_atribuir_juntos_nunca_deixam_usuario_sem_tipo()
    {
        var (i, gestor) = await Gestor();
        var alvo = await f.Usuario(i, [Papeis.Aluno]);
        var aluno = await f.TipoId(i, TipoDeUsuario.Aluno);

        for (var n = 0; n < 20; n++)
        {
            var tipo = await f.Tipo(i, $"Corrida {n}", "indicadores.ver");
            var apagar = gestor.DeleteAsync($"{Rota}/{tipo.Id}");
            var atribuir = gestor.PutAsJsonAsync($"/api/admin/usuarios/{alvo.Id}", new { cpf = alvo.Cpf.Valor, nome = alvo.Nome, email = alvo.Email, tipoId = tipo.Id, setores = Array.Empty<long>() });
            var (d, p) = (await apagar, await atribuir);

            if (d.StatusCode == HttpStatusCode.NoContent)
            {
                Assert.Equal(HttpStatusCode.BadRequest, p.StatusCode);
                Assert.True((await p.Json()).GetProperty("errors").TryGetProperty("tipoId", out _));
            }
            else
            {
                Assert.Equal((HttpStatusCode.Conflict, HttpStatusCode.OK), (d.StatusCode, p.StatusCode));
                await gestor.PutAsJsonAsync($"/api/admin/usuarios/{alvo.Id}", new { cpf = alvo.Cpf.Valor, nome = alvo.Nome, email = alvo.Email, tipoId = aluno, setores = Array.Empty<long>() });
                Assert.Equal(HttpStatusCode.NoContent, (await gestor.DeleteAsync($"{Rota}/{tipo.Id}")).StatusCode);
            }
            Assert.Equal(0, await f.Db(i.Id, db => db.Usuarios.CountAsync(u => !db.TiposDeUsuario.Any(t => t.Id == u.TipoUsuarioId))));
        }
    }

    [Fact]
    public async Task Funcao_de_aluno_e_a_unica_isenta()
    {
        var i = await f.Instituicao();
        var gerente = await f.ComFuncoes(i, ["tipos.gerenciar"]);

        var aluno = await gerente.Cliente.PostAsJsonAsync(Rota, Corpo("Aluno extra", "assistente.usar"));
        Assert.Equal(HttpStatusCode.Created, aluno.StatusCode);
        var antes = (await Tipos(i)).Count;

        foreach (var codigo in Esclari.Api.Dados.Usuarios.Funcoes.Todas.Where(c => c is not ("assistente.usar" or "tipos.gerenciar")))
        {
            var r = await gerente.Cliente.PostAsJsonAsync(Rota, Corpo($"Com {codigo}", codigo));
            Assert.True(r.StatusCode == HttpStatusCode.Forbidden, $"{codigo}: {(int)r.StatusCode}");
        }
        Assert.Equal(antes, (await Tipos(i)).Count);
        Assert.Equal(HttpStatusCode.Created, (await gerente.Cliente.PostAsJsonAsync(Rota, Corpo("Com tipos", "tipos.gerenciar"))).StatusCode);
    }
}
