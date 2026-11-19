using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Chamados.Consulta;
using Esclari.Api.Dados;
using Esclari.Api.Dados.Fluxos;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class LiderDeSemestreTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    async Task<(Turma, Aluno)> ComLider()
    {
        var t = await Turma.Criar(f, comLider: false);
        var u = await f.Usuario(t.I, [Papeis.Aluno, Papeis.Atendente]);
        await f.Db(t.I.Id, db => db.VincularSetores(u.Id, t.Lideranca!.Id));
        return (t, new Aluno(t.I, u, Semente.Senha, await f.Entrar(t.I, u)));
    }

    [Fact]
    public async Task Lider_migrado_segue_com_assistente_fila_e_solicitacoes()
    {
        var (t, lider) = await ComLider();
        f.Llm.Devolver(new { tipo = "pergunta", texto = "Pode detalhar?", categoriaId = t.Duvida.Id, resumo = "Dúvida sobre a nota." });

        var conversa = await lider.Cliente.PostAsJsonAsync("/api/conversas", new { mensagem = "preciso de ajuda com a nota" });
        var minhas = await lider.Cliente.GetAsync("/api/chamados");
        var numero = await t.NaLideranca();
        var fila = await lider.Cliente.GetAsync("/api/chamados?escopo=fila");
        var detalhe = await lider.Cliente.GetAsync($"/api/chamados/{numero}");

        Assert.Equal(HttpStatusCode.Created, conversa.StatusCode);
        Assert.Equal(HttpStatusCode.OK, minhas.StatusCode);
        Assert.Equal(HttpStatusCode.OK, fila.StatusCode);
        Assert.Equal([numero], (await fila.Json()).EnumerateArray().Select(x => x.GetProperty("numero").GetString()));
        Assert.Equal(HttpStatusCode.OK, detalhe.StatusCode);
        var corpo = await detalhe.Content.ReadAsStringAsync();
        Assert.Equal(0, System.Text.Json.JsonDocument.Parse(corpo).RootElement.GetProperty("transcricao").GetArrayLength());
        var matricula = await f.Db(t.I.Id, db => db.Matriculas.Where(m => m.UsuarioId == t.Aluno.Usuario.Id).Select(m => m.Numero).SingleAsync());
        Assert.DoesNotContain(matricula, corpo);
        Assert.DoesNotContain("anexos", corpo);
    }

    [Fact]
    public async Task Sem_matricula_ativa_403_nas_duas_rotas()
    {
        var i = await f.Instituicao();
        var categoria = await f.Db(i.Id, db => db.Categorias.SingleAsync(c => c.Padrao));
        var tipo = await f.Tipo(i, "Só assistente", "assistente.usar");
        var u = await f.Usuario(i, [], tipo: tipo.Nome, matricula: "1234567890", situacao: "INATIVA");
        var c = await f.Entrar(i, u);

        var conversa = await c.PostAsJsonAsync("/api/conversas", new { mensagem = "oi" });
        var direto = await c.PostAsJsonAsync("/api/atendimento-direto", new { descricao = "Preciso de ajuda" });

        Assert.Equal((HttpStatusCode.Forbidden, HttpStatusCode.Forbidden), (conversa.StatusCode, direto.StatusCode));
        Assert.Equal("O assistente é só para alunos com matrícula ativa.", (await conversa.Json()).GetProperty("detail").GetString());
        Assert.Equal("O atendimento é só para alunos com matrícula ativa.", (await direto.Json()).GetProperty("detail").GetString());

        await f.Db(i.Id, db => db.Matriculas.Where(m => m.UsuarioId == u.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.Situacao, "ATIVA")));
        f.Llm.Devolver(new { tipo = "pergunta", texto = "Pode detalhar?", categoriaId = categoria.Id, resumo = "Dúvida." });

        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/conversas", new { mensagem = "oi" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/atendimento-direto", new { descricao = "Preciso de ajuda" })).StatusCode);
    }

    [Fact]
    public async Task Lider_nao_recebe_a_propria_solicitacao()
    {
        var (t, lider) = await ComLider();
        var contexto = await f.Db(t.I.Id, db => ConsultasDeFluxo.CarregarContexto(db, t.Curso.Id, Turma.Semestre, lider.Usuario.Id));
        Assert.Null(await f.Db(t.I.Id, db => ConsultasDeFluxo.Lideranca(db, t.Curso.Id, Turma.Semestre, lider.Usuario.Id)));
        var etapa = await f.Db(t.I.Id, db => db.EtapasFluxo.Where(e => e.Destino == DestinosEtapa.LiderancaSemestre).FirstAsync());
        Assert.Equal(RoteamentoDeFluxo.SemLideranca, RoteamentoDeFluxo.Etapa(etapa, contexto).Motivo);

        var proprio = await t.Transferir(aluno: lider);

        Assert.Equal(t.Coordenacao.Id, (await t.Ler(proprio)).SetorId);

        var segundo = await f.Atendente(t.I, t.Lideranca!);
        Assert.Equal(t.Lideranca!.Id, await f.Db(t.I.Id, db => ConsultasDeFluxo.Lideranca(db, t.Curso.Id, Turma.Semestre, lider.Usuario.Id)));
        var novo = await t.Transferir(aluno: lider);

        Assert.Equal(t.Lideranca.Id, (await t.Ler(novo)).SetorId);
        Assert.True(segundo.Usuario.Ativo);
    }
}
