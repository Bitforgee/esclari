using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class AtribuirTipoTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    const string Usuarios = "/api/admin/usuarios";

    static object Corpo(string cpf, long? tipoId, string? nome = null, string? email = null, long[]? setores = null) =>
        new { cpf, nome = nome ?? "Pessoa Nova", email = email ?? $"{Guid.NewGuid():N}@univag.edu.br", tipoId, setores = setores ?? [] };

    static object CorpoDe(Usuario u, long? tipoId, string? nome = null, long[]? setores = null) => Corpo(u.Cpf.Valor, tipoId, nome ?? u.Nome, u.Email, setores);

    async Task<(Instituicao i, HttpClient gestor, Usuario eu)> Gestor()
    {
        var i = await f.Instituicao();
        var eu = await f.Usuario(i, [Papeis.Administrador]);
        return (i, await f.Entrar(i, eu), eu);
    }

    Task<Usuario> Ler(Instituicao i, long id) => f.Db(i.Id, db => db.Usuarios.Include(u => u.Tipo).SingleAsync(u => u.Id == id));

    Task<List<long>> Setores(Instituicao i, long id) => f.Db(i.Id, db => db.AtendenteSetores.Where(a => a.UsuarioId == id).Select(a => a.SetorId).OrderBy(s => s).ToListAsync());

    [Fact]
    public async Task Criar_e_alterar_com_tipo_devolvem_tipo_sem_papeis()
    {
        var (i, gestor, _) = await Gestor();
        var coordenador = await f.Tipo(i, "Coordenador de curso", "indicadores.ver");
        var leitor = await f.Tipo(i, "Leitor", "indicadores.ver", "mcp.usar");

        var criado = await gestor.PostAsJsonAsync(Usuarios, Corpo(Semente.NovoCpf(), coordenador.Id));

        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var j = await criado.Json();
        Assert.Equal((coordenador.Id, "Coordenador de curso"), (j.GetProperty("tipo").GetProperty("id").GetInt64(), j.GetProperty("tipo").GetProperty("nome").GetString()));
        Assert.False(j.TryGetProperty("papeis", out _));
        var id = j.GetProperty("id").GetInt64();

        var editado = await gestor.PutAsJsonAsync($"{Usuarios}/{id}", CorpoDe(await Ler(i, id), leitor.Id));

        Assert.Equal(HttpStatusCode.OK, editado.StatusCode);
        var e = await editado.Json();
        Assert.Equal((leitor.Id, "Leitor"), (e.GetProperty("tipo").GetProperty("id").GetInt64(), e.GetProperty("tipo").GetProperty("nome").GetString()));
        Assert.False(e.TryGetProperty("papeis", out _));
        Assert.Equal(leitor.Id, (await Ler(i, id)).TipoUsuarioId);
        Assert.Equal(1, await f.Db(i.Id, db => db.Usuarios.CountAsync(u => u.Id == id)));
    }

    [Fact]
    public async Task Tipo_ausente_inexistente_ou_de_outra_instituicao_400()
    {
        var (i, gestor, _) = await Gestor();
        var vizinha = await f.Instituicao();
        var daVizinha = await f.Tipo(vizinha, "Da vizinha", "indicadores.ver");
        var alvo = await f.Usuario(i, [Papeis.Aluno]);
        var antes = await f.Db(i.Id, db => db.Usuarios.CountAsync());

        foreach (var tipoId in new long?[] { null, 999999, daVizinha.Id })
        {
            var post = await gestor.PostAsJsonAsync(Usuarios, Corpo(Semente.NovoCpf(), tipoId));
            var put = await gestor.PutAsJsonAsync($"{Usuarios}/{alvo.Id}", CorpoDe(alvo, tipoId));
            foreach (var r in new[] { post, put })
            {
                Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
                Assert.True((await r.Json()).GetProperty("errors").TryGetProperty("tipoId", out _));
            }
        }
        Assert.Equal(antes, await f.Db(i.Id, db => db.Usuarios.CountAsync()));
        Assert.Equal(TipoDeUsuario.Aluno, (await Ler(i, alvo.Id)).Tipo!.Sistema);
    }

    [Fact]
    public async Task Tipo_com_fila_atender_exige_setor_menos_o_gestor()
    {
        var (i, gestor, _) = await Gestor();
        var triagem = await f.Db(i.Id, db => db.Setores.SingleAsync());
        var proprio = await f.Tipo(i, "Atende e vê", "fila.atender", "indicadores.ver");
        var (atendente, gestorTipo) = (await f.TipoId(i, TipoDeUsuario.Atendente), await f.TipoId(i, TipoDeUsuario.Gestor));
        var alvo = await f.Usuario(i, [Papeis.Aluno]);
        var antes = await f.Db(i.Id, db => db.Usuarios.CountAsync());

        foreach (var tipoId in new[] { atendente, proprio.Id })
        {
            var post = await gestor.PostAsJsonAsync(Usuarios, Corpo(Semente.NovoCpf(), tipoId, setores: []));
            var put = await gestor.PutAsJsonAsync($"{Usuarios}/{alvo.Id}", CorpoDe(alvo, tipoId, setores: []));
            foreach (var r in new[] { post, put })
            {
                Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
                var erro = (await r.Json()).GetProperty("errors").GetProperty("setores")[0].GetString();
                Assert.Equal("Escolha ao menos um setor para quem atende a fila.", erro);
            }
        }
        Assert.Equal(antes, await f.Db(i.Id, db => db.Usuarios.CountAsync()));

        Assert.Equal(HttpStatusCode.Created, (await gestor.PostAsJsonAsync(Usuarios, Corpo(Semente.NovoCpf(), gestorTipo, setores: []))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await gestor.PutAsJsonAsync($"{Usuarios}/{alvo.Id}", CorpoDe(alvo, gestorTipo, setores: []))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await gestor.PostAsJsonAsync(Usuarios, Corpo(Semente.NovoCpf(), atendente, setores: [triagem.Id]))).StatusCode);
    }

    [Fact]
    public async Task Trocar_para_tipo_sem_fila_atender_remove_os_setores()
    {
        var (i, gestor, _) = await Gestor();
        var financeiro = await f.Setor(i, "Financeiro");
        var triagem = await f.Db(i.Id, db => db.Setores.SingleAsync(s => s.Triagem));
        var aluno = await f.TipoId(i, TipoDeUsuario.Aluno);
        var semFila = await f.Tipo(i, "Só indicadores", "indicadores.ver");

        foreach (var destino in new[] { aluno, semFila.Id })
        {
            var u = await f.Usuario(i, [Papeis.Atendente]);
            await f.Db(i.Id, db => db.VincularSetores(u.Id, triagem.Id, financeiro.Id));
            Assert.Equal(2, (await Setores(i, u.Id)).Count);

            var r = await gestor.PutAsJsonAsync($"{Usuarios}/{u.Id}", CorpoDe(u, destino, setores: [triagem.Id, financeiro.Id]));

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Empty(await Setores(i, u.Id));
            Assert.Equal(destino, (await Ler(i, u.Id)).TipoUsuarioId);
        }
    }

    [Fact]
    public async Task So_atribui_tipo_com_funcoes_que_tem_menos_a_de_aluno()
    {
        var i = await f.Instituicao();
        var chefe = await f.ComFuncoes(i, ["usuarios.gerenciar"]);
        var comIndicadores = await f.Tipo(i, "Com indicadores", "indicadores.ver");
        var soAssistente = await f.Tipo(i, "Aluno extra", "assistente.usar");
        var aluno = await f.TipoId(i, TipoDeUsuario.Aluno);
        var alvo = await f.Usuario(i, [Papeis.Aluno]);
        var comIndicadoresHoje = await f.Usuario(i, [], tipo: comIndicadores.Nome);
        const string Mensagem = "Você não pode atribuir ou retirar um tipo com funções que o seu tipo não tem.";

        var atribuir = await chefe.Cliente.PutAsJsonAsync($"{Usuarios}/{alvo.Id}", CorpoDe(alvo, comIndicadores.Id));
        var retirar = await chefe.Cliente.PutAsJsonAsync($"{Usuarios}/{comIndicadoresHoje.Id}", CorpoDe(comIndicadoresHoje, aluno));
        var criar = await chefe.Cliente.PostAsJsonAsync(Usuarios, Corpo(Semente.NovoCpf(), comIndicadores.Id));

        foreach (var r in new[] { atribuir, retirar, criar })
        {
            Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
            Assert.Equal(Mensagem, (await r.Json()).GetProperty("detail").GetString());
        }
        Assert.Equal(aluno, (await Ler(i, alvo.Id)).TipoUsuarioId);
        Assert.Equal(comIndicadores.Id, (await Ler(i, comIndicadoresHoje.Id)).TipoUsuarioId);

        var soDeAluno = await chefe.Cliente.PutAsJsonAsync($"{Usuarios}/{alvo.Id}", CorpoDe(alvo, soAssistente.Id));
        Assert.Equal(HttpStatusCode.OK, soDeAluno.StatusCode);
        Assert.Equal(soAssistente.Id, (await Ler(i, alvo.Id)).TipoUsuarioId);
    }

    [Fact]
    public async Task Gestor_nao_muda_o_proprio_tipo()
    {
        var (i, gestor, eu) = await Gestor();
        var outroGestor = await f.Usuario(i, [Papeis.Administrador]);
        var aluno = await f.TipoId(i, TipoDeUsuario.Aluno);
        var gestorTipo = await f.TipoId(i, TipoDeUsuario.Gestor);

        var recusa = await gestor.PutAsJsonAsync($"{Usuarios}/{eu.Id}", CorpoDe(eu, aluno));

        Assert.Equal(HttpStatusCode.Conflict, recusa.StatusCode);
        Assert.Equal("Você não pode mudar o próprio tipo. Peça a outro gestor.", (await recusa.Json()).GetProperty("detail").GetString());
        Assert.Equal(gestorTipo, (await Ler(i, eu.Id)).TipoUsuarioId);

        var mesmoTipo = await gestor.PutAsJsonAsync($"{Usuarios}/{eu.Id}", CorpoDe(eu, gestorTipo, "Nome Novo"));
        Assert.Equal(HttpStatusCode.OK, mesmoTipo.StatusCode);
        Assert.Equal("Nome Novo", (await Ler(i, eu.Id)).Nome);

        var deOutro = await gestor.PutAsJsonAsync($"{Usuarios}/{outroGestor.Id}", CorpoDe(outroGestor, aluno));
        Assert.Equal(HttpStatusCode.OK, deOutro.StatusCode);
        Assert.Equal(aluno, (await Ler(i, outroGestor.Id)).TipoUsuarioId);
    }

    [Fact]
    public async Task Sem_sessao_401_e_sem_funcao_403_nas_rotas_de_usuario()
    {
        var i = await f.Instituicao();
        var setor = await f.Db(i.Id, db => db.Setores.SingleAsync());
        var alvo = await f.Usuario(i, [Papeis.Aluno]);
        var aluno = await f.TipoId(i, TipoDeUsuario.Aluno);
        var atendente = (await f.Atendente(i, setor)).Cliente;
        var semFuncao = (await f.ComFuncoes(i, ["tipos.gerenciar", "indicadores.ver"])).Cliente;
        var corpo = CorpoDe(alvo, aluno);

        foreach (var c in new[] { atendente, semFuncao })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync(Usuarios)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync(Usuarios, corpo)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.PutAsJsonAsync($"{Usuarios}/{alvo.Id}", corpo)).StatusCode);
        }
        var anonimo = f.Cliente();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Usuarios)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsJsonAsync(Usuarios, corpo)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PutAsJsonAsync($"{Usuarios}/{alvo.Id}", corpo)).StatusCode);
    }

    [Fact]
    public async Task Listar_usuarios_com_tipo()
    {
        var (i, gestor, eu) = await Gestor();
        var aluno = await f.Usuario(i, [Papeis.Aluno]);
        var proprio = await f.Tipo(i, "Coordenador de curso", "indicadores.ver");
        var coordenador = await f.Usuario(i, [], tipo: proprio.Nome);

        var r = await gestor.GetAsync(Usuarios);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var itens = (await r.Json()).EnumerateArray().ToList();
        Assert.All(itens, x => Assert.False(x.TryGetProperty("papeis", out _)));
        string Nome(long id) => itens.Single(x => x.GetProperty("id").GetInt64() == id).GetProperty("tipo").GetProperty("nome").GetString()!;
        Assert.Equal(["Gestor", "Aluno", "Coordenador de curso"], [Nome(eu.Id), Nome(aluno.Id), Nome(coordenador.Id)]);
        Assert.Equal(proprio.Id, itens.Single(x => x.GetProperty("id").GetInt64() == coordenador.Id).GetProperty("tipo").GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task Filtrar_usuarios_por_tipo()
    {
        var (i, gestor, _) = await Gestor();
        var proprio = await f.Tipo(i, "Coordenador de curso", "indicadores.ver");
        var (c1, c2) = (await f.Usuario(i, [], tipo: proprio.Nome), await f.Usuario(i, [], tipo: proprio.Nome));
        await f.Usuario(i, [Papeis.Aluno]);
        await f.Usuario(i, [Papeis.Aluno]);

        async Task<long[]> Ids(string consulta) =>
            [.. (await (await gestor.GetAsync($"{Usuarios}{consulta}")).Json()).EnumerateArray().Select(x => x.GetProperty("id").GetInt64()).Order()];

        Assert.Equal(new[] { c1.Id, c2.Id }.Order(), await Ids($"?tipoId={proprio.Id}"));
        Assert.Empty(await Ids("?tipoId=999999"));
        Assert.Equal(5, (await Ids("")).Length);
    }
}
