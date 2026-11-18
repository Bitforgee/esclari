using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Tests;

public class PrazosPorInstituicaoTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    Agendador Agendador => f.Services.GetRequiredService<Agendador>();

    static readonly ParametrosDaInstituicao Nenhum = new();

    Task Definir(Instituicao i, ParametrosDaInstituicao p) => f.Db(null, async db =>
    {
        var x = await db.Instituicoes.SingleAsync(y => y.Id == i.Id);
        x.DefinirParametros(p);
        await db.SaveChangesAsync();
    });

    Task RecuarAguardando(Balcao b, string numero, TimeSpan quanto) =>
        f.Db(b.Instituicao.Id, db => db.Chamados.Where(c => c.Numero == numero).ExecuteUpdateAsync(s => s.SetProperty(c => c.AguardandoDesde, c => c.AguardandoDesde - quanto)));

    static async Task<JsonElement> Prazos(HttpClient c) =>
        (await (await c.GetAsync("/api/indicadores/detalhe?de=2026-10-01&ate=2027-12-31")).Json()).GetProperty("prazos");

    [Fact]
    public void Para_usa_o_proprio_ou_o_padrao()
    {
        var padrao = new Prazos(30, 7, 3, 7, 12, 24, 120);
        var i = Instituicao.Criar("teste-para", "Teste", "America/Cuiaba", DateTime.UtcNow);

        Assert.Equal(padrao, padrao.Para(i));
        i.DefinirParametros(new() { AbandonoMinutos = 45, ConfirmacaoDias = 2, LembreteAguardandoDias = 1, ResolverAguardandoDias = 4, ValidadeConhecimentoMeses = 6, PrimeiraRespostaHoras = 8, ResolucaoHoras = 48 });
        Assert.Equal(new Prazos(45, 2, 1, 4, 6, 8, 48), padrao.Para(i));
        i.DefinirParametros(Nenhum with { ConfirmacaoDias = 10, ResolucaoHoras = 200 });
        Assert.Equal(new Prazos(30, 10, 3, 7, 12, 24, 200), padrao.Para(i));
        Assert.Equal(new Prazos(31, 8, 4, 9, 13, 25, 121), new Prazos(31, 8, 4, 9, 13, 25, 121).Para(Instituicao.Criar("outra-para", "Outra", "America/Cuiaba", DateTime.UtcNow)));
    }

    [Fact]
    public async Task Metas_proprias_no_atraso_e_nos_indicadores()
    {
        var propria = await Balcao.Criar(f, await f.Instituicao());
        var outra = await Balcao.Criar(f, await f.Instituicao());
        await Definir(propria.Instituicao, Nenhum with { PrimeiraRespostaHoras = 1 });
        foreach (var b in new[] { propria, outra }) await b.Recuar(await b.Chamado(), TimeSpan.FromHours(2));

        async Task<int> Atrasados(Balcao b) => (await (await b.A.Cliente.GetAsync("/api/chamados/busca?escopo=fila&atraso=true")).Json()).GetProperty("total").GetInt32();

        Assert.Equal(1, await Atrasados(propria));
        Assert.Equal(0, await Atrasados(outra));
        var p = await Prazos(propria.A.Cliente);
        var o = await Prazos(outra.A.Cliente);
        Assert.Equal((1, 1), (p.GetProperty("primeiraRespostaMetaHoras").GetInt32(), p.GetProperty("emAtrasoAgora").GetInt32()));
        Assert.Equal((24, 0), (o.GetProperty("primeiraRespostaMetaHoras").GetInt32(), o.GetProperty("emAtrasoAgora").GetInt32()));
    }

    [Fact]
    public async Task Confirmacao_propria_no_email_e_no_fechamento()
    {
        var propria = await Balcao.Criar(f, await f.Instituicao());
        var outra = await Balcao.Criar(f, await f.Instituicao());
        await Definir(propria.Instituicao, Nenhum with { ConfirmacaoDias = 2 });
        var numeros = new Dictionary<Balcao, string>();
        foreach (var b in new[] { propria, outra }) numeros[b] = await b.Chamado(Resolvido);

        async Task<string> Email(Balcao b) => await f.Db(null, db => db.EmailsSaida.IgnoreQueryFilters()
            .Where(e => e.InstituicaoId == b.Instituicao.Id && e.Corpo.Contains("foi resolvida")).Select(e => e.Corpo).SingleAsync());

        Assert.Contains("(até 2 dias)", await Email(propria));
        Assert.Contains("(até 7 dias)", await Email(outra));

        foreach (var b in new[] { propria, outra }) await b.Recuar(numeros[b], TimeSpan.FromDays(3));
        await Agendador.Executar();

        Assert.Equal(Fechado, (await propria.Ler(numeros[propria])).Estado);
        Assert.Equal(Resolvido, (await outra.Ler(numeros[outra])).Estado);
    }

    [Fact]
    public async Task Aguardando_proprio_lembra_e_resolve()
    {
        var propria = await Balcao.Criar(f, await f.Instituicao());
        var outra = await Balcao.Criar(f, await f.Instituicao());
        await Definir(propria.Instituicao, Nenhum with { LembreteAguardandoDias = 1, ResolverAguardandoDias = 2 });
        var casos = new List<(Balcao b, string lembrar, string resolver)>();
        foreach (var b in new[] { propria, outra })
        {
            var (lembrar, resolver) = (await b.Chamado(AguardandoAluno), await b.Chamado(AguardandoAluno));
            await RecuarAguardando(b, lembrar, TimeSpan.FromHours(36));
            await RecuarAguardando(b, resolver, TimeSpan.FromDays(3));
            casos.Add((b, lembrar, resolver));
        }

        await Agendador.Executar();

        var (p, o) = (casos[0], casos[1]);
        Assert.NotNull((await p.b.Ler(p.lembrar)).LembreteEm);
        Assert.Equal(Resolvido, (await p.b.Ler(p.resolver)).Estado);
        Assert.Null((await o.b.Ler(o.lembrar)).LembreteEm);
        Assert.Equal(AguardandoAluno, (await o.b.Ler(o.resolver)).Estado);
    }

    [Fact]
    public async Task Abandono_proprio()
    {
        var propria = await f.AlunoLogado(await f.Instituicao());
        var outra = await f.AlunoLogado(await f.Instituicao());
        await Definir(propria.Instituicao, Nenhum with { AbandonoMinutos = 10 });
        async Task<long> Conversa(Aluno a) => (await (await a.Cliente.PostAsJsonAsync("/api/conversas", new { mensagem = "oi" })).Json()).GetProperty("conversaId").GetInt64();
        var (dela, daOutra) = (await Conversa(propria), await Conversa(outra));
        f.Relogio.Advance(TimeSpan.FromMinutes(15));

        await Agendador.Executar();

        Assert.Equal("ABANDONADA", await f.Db(propria.Instituicao.Id, db => db.Conversas.Where(c => c.Id == dela).Select(c => c.Desfecho).SingleAsync()));
        Assert.Null(await f.Db(outra.Instituicao.Id, db => db.Conversas.Where(c => c.Id == daOutra).Select(c => c.Desfecho).SingleAsync()));
    }

    [Fact]
    public async Task Validade_propria_so_no_que_nasce_depois()
    {
        var (i, admin) = await Admin.Criar(f);
        var categoria = (await f.Categoria(i, "Acadêmico")).Id;
        object Corpo(string titulo) => new { titulo, perguntas = new[] { titulo + "?" }, categoriaId = categoria, resposta = "Resposta publicada pela instituição.", links = Array.Empty<string>() };
        async Task<(long id, string validoAte)> Criar(string titulo)
        {
            var j = await (await admin.PostAsJsonAsync("/api/admin/artigos", Corpo(titulo))).Json();
            return (j.GetProperty("id").GetInt64(), j.GetProperty("validoAte").GetString()!);
        }
        var hoje = DateOnly.FromDateTime(f.Relogio.GetUtcNow().UtcDateTime.AddHours(-4));

        var antes = await Criar("Antes da mudança");
        await Definir(i, Nenhum with { ValidadeConhecimentoMeses = 3 });
        var depois = await Criar("Depois da mudança");

        Assert.Equal(hoje.AddMonths(12).ToString("yyyy-MM-dd"), antes.validoAte);
        Assert.Equal(hoje.AddMonths(3).ToString("yyyy-MM-dd"), depois.validoAte);
        Assert.Equal(hoje.AddMonths(12), await f.Db(i.Id, db => db.Artigos.Where(a => a.Id == antes.id).Select(a => a.ValidoAte).SingleAsync()));
    }

    [Fact]
    public async Task Valor_novo_vale_na_proxima_leitura()
    {
        var b = await Balcao.Criar(f, await f.Instituicao());
        var op = await f.Operador();
        Assert.Equal(24, (await Prazos(b.A.Cliente)).GetProperty("primeiraRespostaMetaHoras").GetInt32());

        var r = await op.Cliente.PutAsJsonAsync($"/api/plataforma/instituicoes/{b.Instituicao.Slug}/parametros", new { primeiraRespostaHoras = 5 });

        Assert.True(r.IsSuccessStatusCode);
        Assert.Equal(5, (await Prazos(b.A.Cliente)).GetProperty("primeiraRespostaMetaHoras").GetInt32());
    }
}
