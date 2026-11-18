using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class EmailMarcaTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    const string Corpo = "Olá, Ana & <script>alert(1)</script>.\n\nAbra o link: http://localhost:8080/x?a=1&b=2\n";
    static readonly HttpClient Mailpit = new() { BaseAddress = new Uri("http://localhost:8025") };

    async Task<(Instituicao, byte[])> InstituicaoComMarca(bool comLogo = true, string? cor = "#8a1538")
    {
        var i = await f.Instituicao(nome: "Univag & Cia");
        await f.Db(null, db => db.Instituicoes.Where(x => x.Id == i.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Cor, cor)));
        var logo = ImagemTeste.Png(tamanho: 200);
        if (comLogo) await ImagemTeste.Enviar(await f.Entrar(i, await f.Usuario(i, [Papeis.Administrador])), "logo", logo);
        return (i, logo);
    }

    async Task<string> Enfileirar(Instituicao i, string corpo = Corpo, string? titulo = null, AcaoDoEmail? acao = null)
    {
        var para = $"{Guid.NewGuid():N}@aluno.com";
        await f.Db(i.Id, async db =>
        {
            db.EmailsSaida.Add(EmailSaida.Criar(para, "Aviso", corpo, f.Relogio.GetUtcNow().UtcDateTime, titulo is null ? null : new(titulo, acao)));
            await db.SaveChangesAsync();
        });
        await f.Services.GetRequiredService<Agendador>().Executar();
        return para;
    }

    static string Texto(JsonElement msg) => msg.GetProperty("Text").GetString()!.Replace("\r\n", "\n");

    static async Task<string> Bruto(JsonElement msg) => await Mailpit.GetStringAsync($"/api/v1/message/{msg.GetProperty("ID").GetString()}/raw");

    [Fact]
    public async Task Email_vai_com_texto_e_html()
    {
        var (i, _) = await InstituicaoComMarca();
        var para = await Enfileirar(i);

        var msg = (await PrimeiroAcessoTests.EmailNoMailpit(para))!.Value;

        Assert.Contains("multipart/alternative", await Bruto(msg));
        Assert.Equal(Corpo, Texto(msg));
        Assert.Contains("<", msg.GetProperty("HTML").GetString());
        Assert.NotNull(await f.Db(i.Id, db => db.EmailsSaida.Where(e => e.Destinatario == para).Select(e => e.EnviadoEm).SingleAsync()));

        var u = await f.Usuario(i, [Papeis.Aluno], senha: null);
        await f.Cliente().PostAsJsonAsync("/api/auth/primeiro-acesso", new { instituicao = i.Slug, cpf = u.Cpf.Valor });
        var acesso = (await PrimeiroAcessoTests.EmailNoMailpit(u.Email))!.Value;
        Assert.Contains("background-color:#8a1538", acesso.GetProperty("HTML").GetString());
        Assert.Contains("Crie sua senha", acesso.GetProperty("Subject").GetString());
    }

    [Fact]
    public async Task Html_com_cor_logo_e_corpo_escapado()
    {
        var (i, logo) = await InstituicaoComMarca();
        var msg = (await PrimeiroAcessoTests.EmailNoMailpit(await Enfileirar(i)))!.Value;

        var html = msg.GetProperty("HTML").GetString()!;

        Assert.Contains("background-color:#8a1538", html);
        Assert.Contains("<img src=\"cid:logo\" alt=\"Univag &amp; Cia\"", html);
        Assert.Contains("Univag &amp; Cia", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("Ana &amp; ", html);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("<p", html);
        var inline = Assert.Single(msg.GetProperty("Inline").EnumerateArray());
        Assert.Equal("logo", inline.GetProperty("ContentID").GetString());
        var bytes = await Mailpit.GetByteArrayAsync($"/api/v1/message/{msg.GetProperty("ID").GetString()}/part/{inline.GetProperty("PartID").GetString()}");
        Assert.Equal(logo, bytes);

        var (semMarca, _) = await InstituicaoComMarca(comLogo: false, cor: null);
        var simples = (await PrimeiroAcessoTests.EmailNoMailpit(await Enfileirar(semMarca)))!.Value.GetProperty("HTML").GetString()!;
        Assert.Contains("background-color:#0e1330", simples);
        Assert.Contains("background-color:#ffffff", simples);
        Assert.DoesNotContain("alt=\"Esclari\"", html);
        Assert.Contains("<img src=\"cid:logo\" alt=\"Esclari\"", simples);
    }

    [Fact]
    public async Task Minio_fora_envia_sem_logo()
    {
        var (i, _) = await InstituicaoComMarca();
        var para = $"{Guid.NewGuid():N}@aluno.com";
        await f.Db(i.Id, async db =>
        {
            db.EmailsSaida.Add(EmailSaida.Criar(para, "Aviso", Corpo, f.Relogio.GetUtcNow().UtcDateTime));
            await db.SaveChangesAsync();
        });

        using (f.MinioFora()) await f.Services.GetRequiredService<Agendador>().Executar();

        Assert.NotNull(await f.Db(i.Id, db => db.EmailsSaida.Where(e => e.Destinatario == para).Select(e => e.EnviadoEm).SingleAsync()));
        var msg = (await PrimeiroAcessoTests.EmailNoMailpit(para))!.Value;
        var html = msg.GetProperty("HTML").GetString()!;
        Assert.Contains("<img src=\"cid:logo\" alt=\"Esclari\"", html);
        Assert.Contains("Univag &amp; Cia", html);
        Assert.Equal(Corpo, Texto(msg));
    }

    [Fact]
    public async Task Sem_logo_propria_leva_a_logo_do_Esclari_e_o_nome()
    {
        var (i, _) = await InstituicaoComMarca(comLogo: false, cor: null);
        var msg = (await PrimeiroAcessoTests.EmailNoMailpit(await Enfileirar(i)))!.Value;

        var html = msg.GetProperty("HTML").GetString()!;

        Assert.Contains("<img src=\"cid:logo\" alt=\"Esclari\"", html);
        Assert.Contains("max-height:40px", html);
        Assert.Matches("font-size:20px;font-weight:bold[^\"]*\">Univag &amp; Cia<", html);
        var inline = Assert.Single(msg.GetProperty("Inline").EnumerateArray());
        Assert.Equal("logo", inline.GetProperty("ContentID").GetString());
        Assert.Equal("image/png", inline.GetProperty("ContentType").GetString());
        var bytes = await Mailpit.GetByteArrayAsync($"/api/v1/message/{msg.GetProperty("ID").GetString()}/part/{inline.GetProperty("PartID").GetString()}");
        Assert.Equal(EnviadorSmtp.LogoDoEsclari(), bytes);
    }

    [Fact]
    public async Task Texto_puro_igual_ao_corpo_mesmo_com_titulo_e_acao()
    {
        var (i, _) = await InstituicaoComMarca();
        var corpo = "Sua solicitação ESC-000001 recebeu uma resposta.\n\nPara ler, abra:\nhttp://localhost:8080/univag/chamados/ESC-000001\n";

        var msg = (await PrimeiroAcessoTests.EmailNoMailpit(await Enfileirar(i, corpo, "Título do aviso", new("Rótulo do botão", "http://localhost:8080/univag/chamados/ESC-000001"))))!.Value;

        Assert.Equal(corpo, Texto(msg));
        Assert.DoesNotContain("Título do aviso", Texto(msg));
        Assert.DoesNotContain("Rótulo do botão", Texto(msg));
        Assert.DoesNotContain("Enviado pelo Esclari", Texto(msg));
        Assert.Contains("Título do aviso", msg.GetProperty("HTML").GetString());
    }

    [Fact]
    public async Task Linha_antiga_sai_no_molde_novo_sem_titulo_e_sem_botao()
    {
        var (i, _) = await InstituicaoComMarca();

        var para = await Enfileirar(i);

        var html = (await PrimeiroAcessoTests.EmailNoMailpit(para))!.Value.GetProperty("HTML").GetString()!;
        Assert.Contains("<html lang=\"pt-BR\">", html);
        Assert.DoesNotContain("<h1", html);
        Assert.DoesNotContain("class=\"botao\"", html);
        Assert.NotNull(await f.Db(i.Id, db => db.EmailsSaida.Where(e => e.Destinatario == para).Select(e => e.EnviadoEm).SingleAsync()));
    }

    [Fact]
    public async Task Html_check_do_mailpit_com_no_maximo_15_por_cento_sem_suporte()
    {
        var (i, _) = await InstituicaoComMarca();
        var corpo = "Estes chamados passaram da meta.\n\n- ESC-000001 · Boleto · atrasado há 1 h\n- ESC-000002 · Matrícula · atrasado há 2 h\n";

        var msg = (await PrimeiroAcessoTests.EmailNoMailpit(await Enfileirar(i, corpo, "Chamados em atraso", new("Ver chamados em atraso", "http://localhost:8080/univag/fila?atraso=true"))))!.Value;

        var verificacao = JsonDocument.Parse(await Mailpit.GetStringAsync($"/api/v1/message/{msg.GetProperty("ID").GetString()}/html-check")).RootElement;
        var semSuporte = verificacao.GetProperty("Total").GetProperty("Unsupported").GetDouble();
        Assert.True(semSuporte <= 15, $"{semSuporte}% sem suporte");
    }
}