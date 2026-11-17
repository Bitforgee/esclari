using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Esclari.Api.Dados;
using Esclari.Api.Dados.Trechos;
using Esclari.Api.Tests.Infra;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Tests;

public class AgendadorLogsTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    const string CategoriaDaBase = "Esclari.Api.Agendador";

    Agendador Agendador => f.Services.GetRequiredService<Agendador>();

    List<Log> Do(string modelo) => [.. f.Logs.Entradas.Where(l => Equals(l.Campos.GetValueOrDefault("{OriginalFormat}"), modelo))];

    [Fact]
    public async Task Abandono_de_conversa_loga_na_categoria_do_agendador()
    {
        var aluno = await f.AlunoLogado();
        var id = (await (await aluno.Cliente.PostAsJsonAsync("/api/conversas", new { mensagem = "oi" })).Json()).GetProperty("conversaId").GetInt64();
        f.Relogio.Advance(TimeSpan.FromMinutes(30));

        await Agendador.Executar();

        var log = Assert.Single(Do("Conversa {ConversaId} abandonada (instituição {InstituicaoId})"), l => Equals(l.Campos["ConversaId"], id));
        Assert.Equal((CategoriaDaBase, LogLevel.Information), (log.Categoria, log.Nivel));
    }

    [Fact]
    public async Task Avisar_validade_registra_contagem_por_marco()
    {
        var (i, _) = await Admin.Criar(f);
        var hoje = f.Hoje(i);
        await f.ArtigoQueVence(i, "Abono de faltas", hoje.AddDays(-1));
        await f.DocumentoQueVence(i, "Calendário acadêmico", hoje.AddDays(-3));
        await f.ArtigoQueVence(i, "Trancamento", hoje.AddDays(5));
        await f.DocumentoQueVence(i, "Regulamento", hoje.AddDays(20));

        await Agendador.Executar();

        var log = Assert.Single(Do("Avisos de validade da instituição {InstituicaoId}: vencidos={Vencidos} em7Dias={Em7Dias} em30Dias={Em30Dias}"), l => Equals(l.Campos["InstituicaoId"], i.Id));
        Assert.Equal((CategoriaDaBase, LogLevel.Information), (log.Categoria, log.Nivel));
        Assert.Equal((2, 1, 1), ((int)log.Campos["Vencidos"]!, (int)log.Campos["Em7Dias"]!, (int)log.Campos["Em30Dias"]!));
        Assert.All(new[] { "Abono", "Calendário", "Trancamento", "Regulamento" }, t => Assert.DoesNotContain(t, log.Mensagem));
    }

    [Fact]
    public async Task Avisar_atrasos_registra_contagens()
    {
        var (i, _) = await Admin.Criar(f);
        var b = await Balcao.Criar(f, i);
        var primeira = await b.Chamado(Aberto, "Boleto vencido do semestre");
        await b.Recuar(primeira, TimeSpan.FromHours(30));
        var resolucao = await b.Chamado(Aberto, "Rematrícula pendente");
        Assert.True((await Balcao.Mensagem(b.A.Cliente, resolucao, "Estamos vendo.")).IsSuccessStatusCode);
        await b.Recuar(resolucao, TimeSpan.FromHours(130));
        var outra = await b.Chamado(Aberto, "Histórico escolar");
        await b.Recuar(outra, TimeSpan.FromHours(26));

        await Agendador.Executar();

        var log = Assert.Single(Do("Atrasos da instituição {InstituicaoId}: primeiraResposta={PrimeiraResposta} resolucao={Resolucao} emails={Emails}"), l => Equals(l.Campos["InstituicaoId"], i.Id));
        Assert.Equal((CategoriaDaBase, LogLevel.Information), (log.Categoria, log.Nivel));
        Assert.Equal((2, 1), ((int)log.Campos["PrimeiraResposta"]!, (int)log.Campos["Resolucao"]!));
        var emails = await f.Db(i.Id, db => db.EmailsSaida.CountAsync(e => e.Assunto.Contains("em atraso")));
        Assert.Equal(emails, (int)log.Campos["Emails"]!);
        Assert.True(emails >= 2);
        Assert.All(new[] { "Boleto", "Rematrícula", "Histórico" }, t => Assert.DoesNotContain(t, log.Mensagem));
    }

    [Fact]
    public async Task Fechamento_automatico_loga_na_categoria_do_agendador()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(Resolvido);
        await b.Recuar(numero, TimeSpan.FromDays(7));
        var chamadoId = (await b.Ler(numero)).Id;

        await Agendador.Executar();

        var log = Assert.Single(Do("Chamado {ChamadoId} fechado automaticamente (instituição {InstituicaoId})"), l => Equals(l.Campos["ChamadoId"], chamadoId));
        Assert.Equal((CategoriaDaBase, LogLevel.Information), (log.Categoria, log.Nivel));
    }

    [Fact]
    public async Task Indexacao_de_trechos_loga_na_categoria_do_agendador()
    {
        var i = await f.Instituicao();
        var artigo = await f.Artigo(i, "Rascunho", "x", estado: EstadosArtigo.Rascunho);
        await f.Db(i.Id, async db =>
        {
            var versao = await db.VersoesArtigo.Where(v => v.ArtigoId == artigo.Id).Select(v => v.Id).SingleAsync();
            db.TrechosFonte.Add(TrechoFonte.DaVersaoDeArtigo(versao, 0, "trecho unico", f.Relogio.GetUtcNow().UtcDateTime.AddDays(-1)));
            await db.SaveChangesAsync();
        });

        await Agendador.Executar();

        Assert.Contains(Do("Agendador indexou {Quantidade} trechos de fonte"), l => (l.Categoria, l.Nivel) == (CategoriaDaBase, LogLevel.Information));
    }

    [Fact]
    public async Task Falha_de_embeddings_loga_na_categoria_do_agendador()
    {
        var i = await f.Instituicao();
        var artigo = await f.Artigo(i, "Rascunho", "x", estado: EstadosArtigo.Rascunho);
        await f.Db(i.Id, async db =>
        {
            var versao = await db.VersoesArtigo.Where(v => v.ArtigoId == artigo.Id).Select(v => v.Id).SingleAsync();
            db.TrechosFonte.Add(TrechoFonte.DaVersaoDeArtigo(versao, 0, "trecho com falha", f.Relogio.GetUtcNow().UtcDateTime.AddDays(-1)));
            await db.SaveChangesAsync();
        });
        f.Llm.ResponderEmbeddings = (_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
        try { await Agendador.Executar(); }
        finally { f.Llm.ResponderEmbeddings = null; }

        var log = Assert.Single(Do("Falha ao gerar embeddings de {Quantidade} trechos da instituição {InstituicaoId}, tentativa {Tentativas}"), l => Equals(l.Campos["InstituicaoId"], i.Id));
        Assert.Equal((CategoriaDaBase, LogLevel.Warning), (log.Categoria, log.Nivel));
    }
}
