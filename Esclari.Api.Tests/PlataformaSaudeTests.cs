using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class PlataformaSaudeTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    const string Lista = "/api/plataforma/instituicoes";
    static readonly string[] ChavesDaSaude = ["ultimaAtividadeEm", "chamadosAbertos", "chamadoMaisAntigoAbertoDias", "conversas30Dias", "respostas30Dias", "semBaseTaxa30Dias", "alunosAtivos", "equipeAtiva", "emailsComFalha", "conteudoVencido"];

    static long proximaConversa = 100_000;
    OperadorLogado? operador;
    DateTime Agora => f.Relogio.GetUtcNow().UtcDateTime;

    static string Ts(DateTime d) => $"'{d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}+00'";

    async Task<List<JsonElement>> Itens(params string[] slugs)
    {
        operador ??= await f.Operador();
        var corpo = await (await operador.Cliente.GetAsync(Lista)).Json();
        return [.. corpo.EnumerateArray().Where(x => slugs.Contains(x.GetProperty("slug").GetString()))];
    }

    async Task<JsonElement> Item(Instituicao i) => (await Itens(i.Slug)).Single();

    Task Sql(string sql) => f.Db(null, db => db.Database.ExecuteSqlRawAsync($"SET session_replication_role = replica;\n{sql}\nSET session_replication_role = DEFAULT;"));

    Task Chamado(Instituicao i, string numero, string estado, DateTime criadoEm, string titulo = "Boleto") =>
        Sql($"INSERT INTO chamado (instituicao_id, numero, conversa_id, titulo, estado, criado_em) VALUES ({i.Id}, '{numero}', {Interlocked.Increment(ref proximaConversa)}, '{titulo}', '{estado}', {Ts(criadoEm)});");

    Task Conversa(Instituicao i, DateTime criadaEm, int quantas = 1) =>
        Sql(string.Concat(Enumerable.Range(0, quantas).Select(_ => $"INSERT INTO conversa (instituicao_id, aluno_id, criada_em, ultima_mensagem_em) VALUES ({i.Id}, 1, {Ts(criadaEm)}, {Ts(criadaEm)});\n")));

    Task Respostas(Instituicao i, int total, int semBase, DateTime em, string autor = "ASSISTENTE") =>
        Sql(string.Concat(Enumerable.Range(0, total).Select(n =>
            $"INSERT INTO mensagem_conversa (instituicao_id, conversa_id, autor, texto, tipo_final, criada_em) VALUES ({i.Id}, 1, '{autor}', 'SEGREDO-TEXTO', {(n < semBase ? "'sem_base'" : "'resposta'")}, {Ts(em)});\n")));

    [Fact]
    public async Task Contagens_e_datas_batem_com_os_dados_semeados()
    {
        var i = await f.Instituicao();
        var agora = Agora;
        await Chamado(i, "ESC-000001", "ABERTO", agora.AddDays(-5));
        await Chamado(i, "ESC-000002", "EM_ATENDIMENTO", agora.AddDays(-1));
        await Chamado(i, "ESC-000003", "RESOLVIDO", agora.AddDays(-3));
        await Conversa(i, agora.AddDays(-10), 3);
        await Conversa(i, agora.AddDays(-40));
        await Respostas(i, 4, 1, agora.AddDays(-2));
        await Respostas(i, 1, 0, agora.AddDays(-40));
        await Respostas(i, 2, 0, agora.AddDays(-2), autor: "ALUNO");
        await f.Usuario(i, [Papeis.Aluno]);
        await f.Usuario(i, [Papeis.Aluno]);
        var inativo = await f.Usuario(i, [Papeis.Aluno]);
        await f.Db(i.Id, db => db.Usuarios.Where(u => u.Id == inativo.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Ativo, false)));
        var setor = await f.Db(i.Id, db => db.Setores.SingleAsync());
        await f.Atendente(i, setor);

        var saude = (await Item(i)).GetProperty("saude");

        Assert.Equal(2, saude.GetProperty("chamadosAbertos").GetInt32());
        Assert.Equal(5, saude.GetProperty("chamadoMaisAntigoAbertoDias").GetInt32());
        Assert.Equal(3, saude.GetProperty("conversas30Dias").GetInt32());
        Assert.Equal(4, saude.GetProperty("respostas30Dias").GetInt32());
        Assert.Equal(0.25, saude.GetProperty("semBaseTaxa30Dias").GetDouble());
        Assert.Equal(2, saude.GetProperty("alunosAtivos").GetInt32());
        Assert.Equal(1, saude.GetProperty("equipeAtiva").GetInt32());
        Assert.Equal(agora.AddDays(-1), saude.GetProperty("ultimaAtividadeEm").GetDateTime().ToUniversalTime());
    }

    [Fact]
    public async Task Instituicao_vazia_devolve_nulos()
    {
        var i = await f.Instituicao();

        var saude = (await Item(i)).GetProperty("saude");

        foreach (var chave in new[] { "ultimaAtividadeEm", "chamadoMaisAntigoAbertoDias", "semBaseTaxa30Dias" })
            Assert.Equal(JsonValueKind.Null, saude.GetProperty(chave).ValueKind);
        Assert.Equal(0, saude.GetProperty("chamadosAbertos").GetInt32());
    }

    [Fact]
    public async Task Emails_com_falha_e_conteudo_vencido_so_contam_o_que_deve()
    {
        var i = await f.Instituicao();
        var hoje = DateOnly.FromDateTime(Agora);
        var agora = Agora;
        await Sql($"""
            INSERT INTO email_saida (instituicao_id, destinatario, assunto, corpo, tentativas, proxima_tentativa, enviado_em) VALUES
              ({i.Id}, 'a@x.com', 'a', 'a', 5, {Ts(agora)}, NULL),
              ({i.Id}, 'b@x.com', 'b', 'b', 4, {Ts(agora)}, NULL),
              ({i.Id}, 'c@x.com', 'c', 'c', 5, {Ts(agora)}, {Ts(agora)});
            INSERT INTO artigo (instituicao_id, categoria_id, estado, valido_ate) VALUES
              ({i.Id}, 1, 'ATIVO', '{hoje.AddDays(-1):yyyy-MM-dd}'), ({i.Id}, 1, 'ATIVO', '{hoje:yyyy-MM-dd}'), ({i.Id}, 1, 'RASCUNHO', '{hoje.AddDays(-9):yyyy-MM-dd}');
            INSERT INTO documento (instituicao_id, titulo, categoria_id, estado, valido_ate) VALUES
              ({i.Id}, 'd1', 1, 'ATIVO', '{hoje.AddDays(-2):yyyy-MM-dd}'), ({i.Id}, 'd2', 1, 'ARQUIVADO', '{hoje.AddDays(-2):yyyy-MM-dd}');
            """);

        var saude = (await Item(i)).GetProperty("saude");

        Assert.Equal(1, saude.GetProperty("emailsComFalha").GetInt32());
        Assert.Equal(2, saude.GetProperty("conteudoVencido").GetInt32());
    }

    [Fact]
    public async Task Saude_so_tem_contagens_uma_fracao_e_uma_data()
    {
        var i = await f.Instituicao();
        var aluno = await f.Usuario(i, [Papeis.Aluno]);
        await Chamado(i, "ESC-000009", "ABERTO", Agora.AddDays(-1), titulo: "SEGREDO-TITULO");
        await Respostas(i, 1, 0, Agora.AddDays(-1));
        operador ??= await f.Operador();

        var bruto = await (await operador.Cliente.GetAsync(Lista)).Content.ReadAsStringAsync();
        var item = (await Item(i));

        Assert.Equal(ChavesDaSaude, item.GetProperty("saude").EnumerateObject().Select(p => p.Name));
        Assert.DoesNotContain("SEGREDO", bruto);
        Assert.DoesNotContain(aluno.Email, bruto);
        Assert.DoesNotContain(aluno.Cpf.Valor, bruto);
        Assert.DoesNotContain("ESC-000009", bruto);
    }

    [Fact]
    public async Task Atencao_nos_limites_de_cada_etiqueta()
    {
        var agora = Agora;
        async Task<List<string?>> Etiquetas(Func<Instituicao, Task> semear)
        {
            var i = await f.Instituicao();
            await semear(i);
            return [.. (await Item(i)).GetProperty("atencao").EnumerateArray().Select(a => a.GetString())];
        }

        Assert.Contains("PARADA", await Etiquetas(i => Conversa(i, agora.AddDays(-14))));
        Assert.DoesNotContain("PARADA", await Etiquetas(i => Conversa(i, agora.AddDays(-13))));

        Assert.Contains("MUITOS_SEM_BASE", await Etiquetas(i => Respostas(i, 20, 7, agora.AddDays(-1))));
        Assert.DoesNotContain("MUITOS_SEM_BASE", await Etiquetas(i => Respostas(i, 19, 19, agora.AddDays(-1))));
        Assert.DoesNotContain("MUITOS_SEM_BASE", await Etiquetas(i => Respostas(i, 20, 6, agora.AddDays(-1))));

        Assert.Contains("EMAILS_FALHANDO", await Etiquetas(i => Sql($"INSERT INTO email_saida (instituicao_id, destinatario, assunto, corpo, tentativas, proxima_tentativa) VALUES ({i.Id}, 'a@x.com', 'a', 'a', 5, {Ts(agora)});")));
        Assert.DoesNotContain("EMAILS_FALHANDO", await Etiquetas(i => Task.CompletedTask));

        Assert.Contains("CONTEUDO_VENCIDO", await Etiquetas(i => Sql($"INSERT INTO documento (instituicao_id, titulo, categoria_id, estado, valido_ate) VALUES ({i.Id}, 'd', 1, 'ATIVO', '{DateOnly.FromDateTime(agora).AddDays(-1):yyyy-MM-dd}');")));

        Assert.Contains("FILA_ANTIGA", await Etiquetas(i => Chamado(i, "ESC-000001", "ABERTO", agora.AddDays(-6))));
        Assert.DoesNotContain("FILA_ANTIGA", await Etiquetas(i => Chamado(i, "ESC-000001", "ABERTO", agora.AddDays(-5))));
    }

    [Fact]
    public async Task Ordem_da_lista_atencao_ativas_desativadas()
    {
        var zeta = await f.Instituicao(nome: "Zeta Atencao");
        var omega = await f.Instituicao(nome: "Omega Atencao");
        var alfa = await f.Instituicao(nome: "Alfa Normal");
        var beta = await f.Instituicao(nome: "Beta Desativada");
        var gama = await f.Instituicao(nome: "Gama Desativada Com Atencao");
        foreach (var i in new[] { zeta, omega, gama })
            await Sql($"INSERT INTO email_saida (instituicao_id, destinatario, assunto, corpo, tentativas, proxima_tentativa) VALUES ({i.Id}, 'a@x.com', 'a', 'a', 5, {Ts(Agora)});");
        await f.Db(null, db => db.Instituicoes.Where(x => x.Id == beta.Id || x.Id == gama.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Ativa, false)));

        var itens = await Itens(zeta.Slug, omega.Slug, alfa.Slug, beta.Slug, gama.Slug);

        Assert.Equal(["Omega Atencao", "Zeta Atencao", "Alfa Normal", "Beta Desativada", "Gama Desativada Com Atencao"], itens.Select(x => x.GetProperty("nome").GetString()));
        Assert.Empty(itens.Last().GetProperty("atencao").EnumerateArray());
    }

    [Fact]
    public async Task Situacao_nos_tres_valores()
    {
        var aguardando = await f.Instituicao();
        await f.Usuario(aguardando, [Papeis.Administrador], senha: null);
        var ativa = await f.Instituicao();
        await f.Usuario(ativa, [Papeis.Administrador]);
        var semAdmin = await f.Instituicao();
        var desativada = await f.Instituicao();
        await f.Usuario(desativada, [Papeis.Administrador], senha: null);
        await f.Db(null, db => db.Instituicoes.Where(x => x.Id == desativada.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Ativa, false)));

        Task<string?> Situacao(Instituicao i) => Item(i).ContinueWith(t => t.Result.GetProperty("situacao").GetString());

        Assert.Equal("AGUARDANDO_PRIMEIRO_ACESSO", await Situacao(aguardando));
        Assert.Equal("ATIVA", await Situacao(ativa));
        Assert.Equal("ATIVA", await Situacao(semAdmin));
        Assert.Equal("DESATIVADA", await Situacao(desativada));
    }
}
