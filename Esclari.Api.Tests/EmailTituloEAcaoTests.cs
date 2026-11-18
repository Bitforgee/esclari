using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Esclari.Api.Dados;
using Esclari.Api.Instituicoes.Plataforma;
using Esclari.Api.Tests.Infra;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Tests;

public class EmailTituloEAcaoTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    Agendador Agendador => f.Services.GetRequiredService<Agendador>();

    static string Link(Balcao b, string numero) => $"http://localhost:8080/{b.Instituicao.Slug}/chamados/{numero}";

    Task<List<EmailSaida>> Emails(Balcao b) => f.Db(b.Instituicao.Id, db => db.EmailsSaida.Where(e => e.Destinatario == b.Aluno.Usuario.Email).OrderBy(e => e.Id).ToListAsync());

    static void Confere(EmailSaida e, string titulo, string rotulo, string url) =>
        Assert.Equal((titulo, rotulo, url), (e.Titulo, e.AcaoRotulo, e.AcaoUrl));

    [Fact]
    public async Task Nova_resposta_grava_titulo_e_acao()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado();

        await Balcao.Mensagem(b.A.Cliente, numero, "Vou verificar.");

        Confere(Assert.Single(await Emails(b)), "Sua solicitação recebeu uma resposta", "Ver a resposta", Link(b, numero));
    }

    [Fact]
    public async Task Resolvido_grava_titulo_e_acao()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EmAtendimento);

        await b.A.Cliente.PostAsJsonAsync($"/api/chamados/{numero}/resolver", new { texto = "Boleto reemitido no portal." });

        Confere((await Emails(b)).Last(), "Sua solicitação foi resolvida", "Confirmar ou rejeitar", Link(b, numero));
    }

    [Fact]
    public async Task Resolvido_grava_a_resolucao_como_citacao()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EmAtendimento);

        await b.A.Cliente.PostAsJsonAsync($"/api/chamados/{numero}/resolver", new { texto = "Boleto reemitido.\nAcesse o portal." });

        var corpo = (await Emails(b)).Last().Corpo;
        Assert.Contains("> Boleto reemitido.\n> Acesse o portal.\n", corpo);
        Assert.Contains("Boleto reemitido.", corpo);
        Assert.Contains("Acesse o portal.", corpo);
    }

    [Fact]
    public async Task Aguarda_resposta_grava_titulo_e_acao()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(AguardandoAluno);
        await f.Db(b.Instituicao.Id, db => db.Chamados.Where(c => c.Numero == numero).ExecuteUpdateAsync(s => s.SetProperty(c => c.AguardandoDesde, c => c.AguardandoDesde - TimeSpan.FromDays(3))));

        await Agendador.Executar();

        var e = Assert.Single(await Emails(b), x => x.Assunto == $"[Univag] {numero} aguarda sua resposta");
        Confere(e, "A equipe aguarda a sua resposta", "Responder", Link(b, numero));
    }

    [Fact]
    public async Task Atrasos_gravam_titulo_e_acao_do_setor_e_do_administrador()
    {
        var (i, b) = await f.ComAdmin();
        var numero = await b.Chamado(EmAtendimento);
        await b.Recuar(numero, TimeSpan.FromHours(25));
        var paraA = await f.EmailDe(i, b.A.Usuario.Id);
        var @base = "http://localhost:8080/" + i.Slug;

        await Agendador.Executar();
        var doSetor = Assert.Single(await f.EmailsDeAtraso(i), e => e.Destinatario == paraA);
        Confere(doSetor, "Chamados em atraso", "Ver chamados em atraso", @base + "/fila?atraso=true");
        Assert.Contains(@base + "/fila?atraso=true", doSetor.Corpo);

        var antes = (await f.EmailsDeAtraso(i)).Count;
        f.Relogio.Advance(TimeSpan.FromHours(24));
        await Agendador.Executar();
        var resumo = Assert.Single((await f.EmailsDeAtraso(i)).Skip(antes));
        Confere(resumo, "Resumo de chamados em atraso", "Ver chamados em atraso", @base + "/chamados?atraso=true");
        Assert.Contains(@base + "/chamados?atraso=true", resumo.Corpo);
    }

    [Fact]
    public async Task Validade_grava_titulo_e_acao()
    {
        var (i, _) = await Admin.Criar(f);
        var hoje = f.Hoje(i);
        await f.ArtigoQueVence(i, "Trancamento", hoje.AddDays(5));
        await f.DocumentoQueVence(i, "Calendário", hoje.AddDays(-40));

        await Agendador.Executar();

        var e = Assert.Single(await f.EmailsDeAviso(i));
        var primeiro = Regex.Match(e.Corpo, @"Abrir: (\S+)").Groups[1].Value;
        Assert.NotEqual("", primeiro);
        Confere(e, "Conteúdo para renovar", "Abrir a lista", primeiro);
    }

    [Fact]
    public async Task Primeiro_acesso_envia_titulo_botao_e_aviso()
    {
        var i = await f.Instituicao();
        var u = await f.Usuario(i, [Papeis.Aluno], senha: null);

        await f.Cliente().PostAsJsonAsync("/api/auth/primeiro-acesso", new { instituicao = i.Slug, cpf = u.Cpf.Valor });

        var msg = (await PrimeiroAcessoTests.EmailNoMailpit(u.Email))!.Value;
        var html = msg.GetProperty("HTML").GetString()!;
        Assert.Contains("<h1", html);
        Assert.Contains("Crie a sua senha", html);
        Assert.Contains("Criar senha</a>", html);
        Assert.Contains($"http://localhost:8080/{i.Slug}/definir-senha?token=", html);
        Assert.Contains("Se você não pediu isso, ignore este e-mail.", html);
        Assert.Contains("60 minutos", html);
    }

    [Fact]
    public void Criacao_da_instituicao_grava_titulo_acao_e_aviso()
    {
        var cfg = f.Services.GetRequiredService<IConfiguration>();
        var i = Instituicao.Criar("demo-titulo", "Faculdade Demo", "America/Cuiaba", f.Relogio.GetUtcNow().UtcDateTime);

        var e = Criacao.EmailDeAcesso(cfg, i, "admin@demo.edu.br", "TOKEN123", f.Relogio.GetUtcNow().UtcDateTime);

        Confere(e, "Crie a sua senha", "Criar senha", "http://localhost:8080/demo-titulo/definir-senha?token=TOKEN123");
        Assert.Contains("Se você não pediu isso, ignore este e-mail.", e.Corpo);
        Assert.Contains("60 minutos", e.Corpo);
    }

    [Fact]
    public async Task Titulo_e_acao_voltam_do_banco_iguais()
    {
        var i = await f.Instituicao();
        var para = $"{Guid.NewGuid():N}@aluno.com";
        await f.Db(i.Id, async db =>
        {
            db.EmailsSaida.Add(EmailSaida.Criar(para, "Aviso", "Texto", f.Relogio.GetUtcNow().UtcDateTime, new("Meu título", new("Abrir", "http://localhost:8080/x/y"))));
            await db.SaveChangesAsync();
        });

        var e = await f.Db(i.Id, db => db.EmailsSaida.AsNoTracking().SingleAsync(x => x.Destinatario == para));

        Confere(e, "Meu título", "Abrir", "http://localhost:8080/x/y");
    }
}
