using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Chamados.Consulta;
using Esclari.Api.Chamados.Permissao;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class AutorizacaoPorFuncaoTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    const string Periodo = "de=2026-11-01&ate=2026-11-08";
    const string SemAcesso = "Seu tipo de usuário não tem acesso a esta área.";

    static async Task<string> Detalhe(HttpResponseMessage r) => (await r.Json()).GetProperty("detail").GetString()!;

    [Fact]
    public async Task Sem_a_funcao_403_com_a_mensagem()
    {
        var i = await f.Instituicao();
        var setor = await f.Setor(i, "Financeiro");
        var aluno = await f.AlunoLogado(i);
        var atendente = await f.Atendente(i, setor);
        var semNada = await f.ComFuncoes(i, ["indicadores.ver"]);
        string[] rotas = ["/api/admin/usuarios", "/api/admin/setores", "/api/admin/artigos", "/api/conhecimento/validade"];

        foreach (var cliente in new[] { aluno.Cliente, atendente.Cliente, semNada.Cliente })
            foreach (var rota in rotas)
            {
                var r = await cliente.GetAsync(rota);
                Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
                var detalhe = await Detalhe(r);
                Assert.Equal(SemAcesso, detalhe);
                Assert.DoesNotContain("Só administradores podem usar esta área.", await r.Content.ReadAsStringAsync());
            }
    }

    [Fact]
    public async Task Mudanca_vale_na_requisicao_seguinte()
    {
        var i = await f.Instituicao();
        var quem = await f.ComFuncoes(i, ["indicadores.ver"]);
        Assert.Equal(HttpStatusCode.OK, (await quem.Cliente.GetAsync($"/api/indicadores?{Periodo}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await quem.Cliente.GetAsync("/api/admin/usuarios")).StatusCode);

        await f.Db(i.Id, db => db.FuncoesDoTipo.Where(x => x.TipoUsuarioId == quem.Usuario.TipoUsuarioId).ExecuteDeleteAsync());

        Assert.Equal(HttpStatusCode.Forbidden, (await quem.Cliente.GetAsync($"/api/indicadores?{Periodo}")).StatusCode);

        var gestor = await f.Db(i.Id, db => db.TiposDeUsuario.Where(t => t.Sistema == "GESTOR").Select(t => t.Id).SingleAsync());
        await f.Db(i.Id, db => db.Usuarios.Where(u => u.Id == quem.Usuario.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.TipoUsuarioId, gestor)));

        Assert.Equal(HttpStatusCode.OK, (await quem.Cliente.GetAsync("/api/admin/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await quem.Cliente.GetAsync($"/api/indicadores?{Periodo}")).StatusCode);
    }

    static async Task<(HttpStatusCode, string)> Chamar(HttpClient c, string rota)
    {
        var r = await c.GetAsync(rota);
        return (r.StatusCode, await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Matriz_de_status_por_tipo_de_sistema()
    {
        var b = await Balcao.Criar(f);
        await b.Chamado();
        var gestor = await f.Entrar(b.Instituicao, await f.Usuario(b.Instituicao, [Papeis.Administrador]));
        var categoria = await f.Db(b.Instituicao.Id, db => db.Categorias.SingleAsync(c => c.Padrao));
        var clientes = new[] { ("ALUNO", b.ClienteAluno), ("ATENDENTE", b.A.Cliente), ("GESTOR", gestor) };

        var fila = new List<(HttpStatusCode, int)>();
        foreach (var (_, c) in clientes)
        {
            var (status, corpo) = await Chamar(c, "/api/chamados?escopo=fila");
            fila.Add((status, System.Text.Json.JsonDocument.Parse(corpo).RootElement.GetArrayLength()));
        }
        Assert.Equal([(HttpStatusCode.OK, 0), (HttpStatusCode.OK, 1), (HttpStatusCode.OK, 0)], fila);

        var esperado = new (string Rota, HttpStatusCode[] Status)[]
        {
            ("/api/chamados/busca?escopo=todos", [HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.OK]),
            ($"/api/indicadores?{Periodo}", [HttpStatusCode.Forbidden, HttpStatusCode.OK, HttpStatusCode.OK]),
            ("/api/admin/usuarios", [HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.OK]),
            ("/api/auth/me", [HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK]),
        };
        foreach (var (rota, status) in esperado)
            Assert.Equal(status, (await Task.WhenAll(clientes.Select(x => Chamar(x.Item2, rota)))).Select(x => x.Item1));

        var conversas = new List<HttpStatusCode>();
        foreach (var (_, c) in clientes)
        {
            f.Llm.Devolver(new { tipo = "pergunta", texto = "Pode detalhar?", categoriaId = categoria.Id, resumo = "Dúvida." });
            conversas.Add((await c.PostAsJsonAsync("/api/conversas", new { mensagem = "preciso de ajuda com o boleto" })).StatusCode);
        }
        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden], conversas);
    }

    [Fact]
    public async Task Gestor_com_setor_atende_nos_seus_e_le_nos_demais()
    {
        var t = await Turma.Criar(f);
        var comum = await f.Db(t.I.Id, db => db.Setores.SingleAsync(s => s.Triagem));
        var gestor = await f.Usuario(t.I, [Papeis.Administrador]);
        await f.Db(t.I.Id, db => db.VincularSetores(gestor.Id, comum.Id, t.Lideranca!.Id));
        var naLideranca = await t.NaLideranca();
        var noComum = await t.Aluno.Cliente.PostAsJsonAsync("/api/atendimento-direto", new { descricao = "Preciso de ajuda com o boleto" });
        var numeroComum = (await noComum.Json()).GetProperty("numero").GetString()!;
        var noTerceiro = (await (await t.Aluno.Cliente.PostAsJsonAsync("/api/atendimento-direto", new { descricao = "Outra dúvida" })).Json()).GetProperty("numero").GetString()!;
        var proprio = (await (await t.Aluno.Cliente.PostAsJsonAsync("/api/atendimento-direto", new { descricao = "Dúvida do gestor" })).Json()).GetProperty("numero").GetString()!;
        await f.Db(t.I.Id, async db =>
        {
            await db.Chamados.Where(c => c.Numero == noTerceiro).ExecuteUpdateAsync(s => s.SetProperty(c => c.SetorId, t.Coordenacao.Id));
            await db.Chamados.Where(c => c.Numero == proprio).ExecuteUpdateAsync(s => s.SetProperty(c => c.SolicitanteId, gestor.Id));
        });

        var papeis = await f.Db(t.I.Id, async db =>
        {
            var u = await db.Usuarios.ComTipo().SingleAsync(x => x.Id == gestor.Id);
            var lista = new List<string?>();
            foreach (var numero in new[] { numeroComum, naLideranca, noTerceiro, proprio })
                lista.Add((await ChamadoDoUsuario.Carregar(db, u, numero)).Item2);
            return lista;
        });

        Assert.Equal([PermissoesChamado.Atendente, PermissoesChamado.Lider, PermissoesChamado.Leitura, PermissoesChamado.Aluno], papeis);
    }

    static async Task<HttpStatusCode> Status(HttpClient c, HttpMethod metodo, string rota, HttpContent? corpo = null)
    {
        using var pedido = new HttpRequestMessage(metodo, rota) { Content = corpo };
        return (await c.SendAsync(pedido)).StatusCode;
    }

    [Fact]
    public async Task Cada_rota_libera_so_a_sua_funcao()
    {
        var i = await f.Instituicao();
        var setor = await f.Setor(i, "Financeiro");
        var categoria = await f.Db(i.Id, db => db.Categorias.SingleAsync(c => c.Padrao));
        var rotas = new (string Funcao, HttpMethod Metodo, string Rota)[]
        {
            ("usuarios.gerenciar", HttpMethod.Get, "/api/admin/usuarios"),
            ("matriculas.importar", HttpMethod.Post, "/api/admin/matriculas/importacao"),
            ("cadastros.gerenciar", HttpMethod.Get, "/api/admin/setores"),
            ("cadastros.gerenciar", HttpMethod.Get, "/api/admin/categorias"),
            ("cadastros.gerenciar", HttpMethod.Get, "/api/admin/cursos"),
            ("cadastros.gerenciar", HttpMethod.Get, "/api/admin/respostas-prontas"),
            ("cadastros.gerenciar", HttpMethod.Get, "/api/admin/fluxos"),
            ("conhecimento.gerenciar", HttpMethod.Get, "/api/admin/artigos"),
            ("conhecimento.gerenciar", HttpMethod.Get, "/api/admin/documentos"),
            ("conhecimento.gerenciar", HttpMethod.Get, "/api/conhecimento/validade"),
            ("aparencia.gerenciar", HttpMethod.Get, "/api/admin/aparencia"),
            ("integracoes.gerenciar", HttpMethod.Get, "/api/admin/moodle"),
            ("integracoes.gerenciar", HttpMethod.Get, "/api/admin/mcp"),
            ("chamados.ver_todos", HttpMethod.Get, "/api/chamados/busca?escopo=todos"),
            ("chamados.ver_todos", HttpMethod.Get, "/api/chamados/exportar?escopo=todos"),
            ("indicadores.ver", HttpMethod.Get, $"/api/indicadores?{Periodo}"),
            ("indicadores.ver", HttpMethod.Get, $"/api/indicadores/detalhe?{Periodo}"),
            ("fila.atender", HttpMethod.Get, "/api/chamados?escopo=fila"),
            ("fila.atender", HttpMethod.Get, $"/api/respostas-prontas?setorId={setor.Id}"),
            ("fila.atender", HttpMethod.Post, "/api/chamados/ESC-999999/artigo"),
            ("assistente.usar", HttpMethod.Post, "/api/conversas"),
            ("assistente.usar", HttpMethod.Post, "/api/atendimento-direto"),
        };
        Assert.Equal(22, rotas.Length);

        foreach (var funcao in rotas.Select(r => r.Funcao).Distinct())
        {
            var quem = await f.ComFuncoes(i, [funcao], matricula: funcao == "assistente.usar" ? Random.Shared.NextInt64(1_000_000_000, 9_999_999_999).ToString() : null, setor);
            foreach (var (_, metodo, rota) in rotas.Where(r => r.Funcao == funcao))
            {
                f.Llm.Devolver(new { tipo = "pergunta", texto = "Pode detalhar?", categoriaId = categoria.Id, resumo = "Dúvida." });
                HttpContent? corpo = metodo != HttpMethod.Post ? null
                    : rota.Contains("importacao") ? new MultipartFormDataContent()
                    : JsonContent.Create(rota.Contains("conversas") ? new { mensagem = "preciso de ajuda" } : rota.Contains("atendimento") ? (object)new { descricao = "Preciso de ajuda" } : new { });
                var status = await Status(quem.Cliente, metodo, rota, corpo);
                Assert.True(status != HttpStatusCode.Forbidden, $"{funcao}: {metodo} {rota} respondeu {(int)status}");
            }
            var outra = funcao == "usuarios.gerenciar" ? "/api/admin/setores" : "/api/admin/usuarios";
            Assert.Equal(HttpStatusCode.Forbidden, (await quem.Cliente.GetAsync(outra)).StatusCode);
        }
    }
}
