using System.Text;
using System.Text.RegularExpressions;

namespace Esclari.Api.Tests;

public class LayoutEmailTests
{
    const string Base = "http://localhost:8080";
    const string Chamado = Base + "/univag/chamados/ESC-000001";

    static string Html(string corpo = "Olá, Ana.", string nome = "Univag & Cia", string? cor = "#8a1538", bool logo = true,
        string assunto = "[Univag] Aviso", string? titulo = null, AcaoDoEmail? acao = null) =>
        LayoutEmail.Html(nome, cor, logo, corpo, assunto, titulo, acao, Base);

    static string Bloco(string html, string abertura)
    {
        var inicio = html.IndexOf(abertura, StringComparison.Ordinal);
        Assert.True(inicio >= 0, $"sem {abertura}");
        var fim = html.IndexOf("@media", inicio + abertura.Length, StringComparison.Ordinal);
        if (fim < 0) fim = html.IndexOf("</style>", inicio, StringComparison.Ordinal);
        return html[inicio..fim];
    }

    [Fact]
    public void Documento_declara_idioma_titulo_viewport_e_esquema_de_cor()
    {
        var html = Html(assunto: "[Univag & Cia] Aviso");

        Assert.Contains("<html lang=\"pt-BR\">", html);
        Assert.Contains("<title>[Univag &amp; Cia] Aviso</title>", html);
        Assert.Contains("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">", html);
        Assert.Contains("<meta name=\"color-scheme\" content=\"light dark\">", html);
    }

    [Fact]
    public void Tabelas_de_layout_coluna_unica_sem_script_nem_recurso_remoto()
    {
        var html = Html("Olá.\n\n- um\n- dois\n\nhttps://exemplo.com/a", titulo: "Título", acao: new("Abrir", Chamado));

        var tabelas = Regex.Matches(html, "<table").Count;
        Assert.True(tabelas > 0);
        Assert.Equal(tabelas, Regex.Matches(html, "<table[^>]*role=\"presentation\"").Count);
        Assert.Contains("max-width:600px", html);
        Assert.Contains("width=\"100%\"", html);
        Assert.DoesNotContain("<script", html);
        Assert.DoesNotContain("<form", html);
        Assert.DoesNotContain("<link", html);
        Assert.DoesNotMatch(@"(src=|url\()[""']?https?:", html);
    }

    [Fact]
    public void Texto_16px_entrelinha_e_tinta()
    {
        var html = Html();

        Assert.Contains("font-size:16px", html);
        Assert.Contains("line-height:1.5", html);
        Assert.Contains("font-family:Arial,Helvetica,sans-serif", html);
        Assert.Contains("color:#0e1330", html);
        Assert.Contains("background-color:#ffffff", html);
    }

    [Fact]
    public void Logo_no_cabecalho_branco_sob_filete_da_cor()
    {
        var html = Html(logo: true);

        var imagem = html.IndexOf("<img src=\"cid:logo\" alt=\"Univag &amp; Cia\"", StringComparison.Ordinal);
        Assert.True(imagem > 0);
        Assert.Contains("max-height:40px", html);
        var filete = Regex.Match(html, "height:4px[^\"]*background-color:#8a1538|background-color:#8a1538[^\"]*height:4px");
        Assert.True(filete.Success);
        Assert.True(filete.Index < imagem);
        Assert.Matches("class=\"cabecalho\"[^>]*background-color:#ffffff", html);
    }

    [Fact]
    public void Sem_logo_mostra_o_nome_em_20px_negrito()
    {
        var html = Html(logo: false);

        Assert.Matches("font-size:20px[^\"]*font-weight:bold[^>]*>Univag &amp; Cia<|font-weight:bold[^\"]*font-size:20px[^>]*>Univag &amp; Cia<", html);
        Assert.DoesNotContain("cid:", html);
        Assert.DoesNotContain("<img", html);
    }

    [Fact]
    public void Cor_da_instituicao_ou_tinta_no_filete_e_no_botao()
    {
        var acao = new AcaoDoEmail("Abrir", Chamado);

        var com = Html(cor: "#8a1538", acao: acao);
        var sem = Html(cor: null, acao: acao);

        Assert.Equal(2, Regex.Matches(com, "background-color:#8a1538").Count);
        Assert.Equal(2, Regex.Matches(sem, "background-color:#0e1330").Count);
        Assert.DoesNotContain("#C83A23", com, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#C83A23", sem, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pre_visualizacao_oculta_com_90_caracteres()
    {
        var html = Html(new string('a', 200) + "\n\nSegundo parágrafo.", titulo: "Meu título");

        var oculto = Regex.Match(html, "<div style=\"display:none[^\"]*\">([^<]*)</div>");
        Assert.True(oculto.Success);
        Assert.Equal(new string('a', 90), oculto.Groups[1].Value);
        Assert.True(oculto.Index < html.IndexOf("Meu título", StringComparison.Ordinal));
        Assert.DoesNotContain("Segundo parágrafo.</div>", html);
    }

    [Fact]
    public void Tema_escuro_troca_as_quatro_cores_e_preserva_cabecalho_e_botao()
    {
        var html = Html(acao: new("Abrir", Chamado));

        var escuro = Bloco(html, "@media (prefers-color-scheme: dark)");

        Assert.Contains(".fundo{background-color:#0e0e18!important}", escuro);
        Assert.Contains(".cartao{background-color:#151620!important}", escuro);
        Assert.Contains(".texto{color:#f0eee9!important}", escuro);
        Assert.Contains(".suave{color:#a0a1a8!important}", escuro);
        Assert.DoesNotContain(".cabecalho", escuro);
        Assert.DoesNotContain(".botao", escuro);
        Assert.DoesNotContain("#ffffff", escuro);
    }

    [Fact]
    public void Rodape_com_remetente_e_aviso_automatico()
    {
        var html = Html();

        Assert.Contains("Enviado pelo Esclari para Univag &amp; Cia.", html);
        Assert.Contains("Este aviso é automático; não responda este e-mail.", html);
        Assert.Matches("class=\"suave\"[^>]*font-size:14px[^>]*color:#5c5d64|class=\"suave\"[^>]*color:#5c5d64[^>]*font-size:14px", html);
    }

    [Fact]
    public void Celular_reduz_preenchimento_e_botao_inteiro()
    {
        var html = Html(acao: new("Abrir", Chamado));

        var celular = Bloco(html, "@media (max-width:480px)");

        Assert.Contains(".miolo{padding-left:20px!important;padding-right:20px!important}", celular);
        Assert.Contains(".botao{display:block!important;width:100%!important}", celular);
        Assert.Matches("class=\"miolo\"[^>]*padding:[^\"]*32px", html);
    }

    [Fact]
    public void Escapa_corpo_titulo_rotulo_e_nome()
    {
        const string perigo = "<script>alert(1)</script> & \"";

        var html = Html(perigo, nome: perigo, assunto: perigo, titulo: perigo, acao: new(perigo, Chamado));

        Assert.DoesNotContain("<script", html);
        Assert.True(Regex.Matches(html, "&lt;script&gt;alert\\(1\\)&lt;/script&gt; &amp; &quot;").Count >= 4);
    }

    [Fact]
    public void Cinquenta_itens_cabem_em_100_kb()
    {
        var corpo = new StringBuilder("Resumo.\n\n");
        for (var n = 0; n < 50; n++) corpo.Append($"- ESC-{n:000000} {new string('x', 120)} {Base}/univag/chamados/ESC-{n:000000}\n");

        var html = Html(corpo.ToString(), titulo: "Chamados em atraso", acao: new("Ver chamados em atraso", Base + "/univag/fila?atraso=true"));

        Assert.True(Encoding.UTF8.GetByteCount(html) <= 100_000, $"{Encoding.UTF8.GetByteCount(html)} bytes");
    }

    [Fact]
    public void Titulo_em_h1_de_24px_antes_do_texto()
    {
        var com = Html("Texto do aviso.", titulo: "Sua solicitação foi resolvida");
        var sem = Html("Texto do aviso.");

        Assert.Matches("<h1[^>]*font-size:24px[^>]*font-weight:bold|<h1[^>]*font-weight:bold[^>]*font-size:24px", com);
        Assert.True(com.IndexOf("<h1", StringComparison.Ordinal) < com.IndexOf("<p class=\"texto\"", StringComparison.Ordinal));
        Assert.DoesNotContain("<h1", sem);
    }

    [Fact]
    public void Botao_de_44px_com_versao_vml_para_outlook()
    {
        var html = Html(acao: new("Confirmar ou rejeitar", Chamado));

        var botao = Regex.Match(html, "<a href=\"" + Regex.Escape(Chamado) + "\"[^>]*class=\"botao\"[^>]*>|<a class=\"botao\"[^>]*href=\"" + Regex.Escape(Chamado) + "\"[^>]*>").Value;
        Assert.NotEqual("", botao);
        foreach (var estilo in new[] { "min-height:44px", "color:#ffffff", "font-size:16px", "font-weight:bold", "border-radius:6px", "background-color:#8a1538" })
            Assert.Contains(estilo, botao);
        Assert.Contains("Confirmar ou rejeitar</a>", html);
        Assert.Contains("<!--[if mso]>", html);
        Assert.Contains("v:roundrect", html);
    }

    [Fact]
    public void Endereco_por_extenso_abaixo_do_botao_sem_repetir_a_linha()
    {
        var html = Html($"Para ler e responder, abra:\n\n{Chamado}\n", acao: new("Ver a resposta", Chamado));

        var botao = html.IndexOf("class=\"botao\"", StringComparison.Ordinal);
        var reserva = html.IndexOf("Se o botão não abrir, copie este endereço:", StringComparison.Ordinal);
        Assert.True(botao > 0 && reserva > botao);
        Assert.Equal(2, Regex.Matches(html, "<a [^>]*href=\"" + Regex.Escape(Chamado) + "\"").Count);
        Assert.Single(Regex.Matches(html, ">" + Regex.Escape(Chamado) + "</a>"));
        Assert.Contains("Para ler e responder, abra:", html);
        Assert.DoesNotContain(">" + Chamado + "</p>", html);
    }

    [Theory]
    [InlineData("https://outro.exemplo/x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://localhost:8080.outro.exemplo/x")]
    public void Acao_fora_da_base_nao_vira_botao(string url)
    {
        var html = Html("Olá.", acao: new("Abrir agora", url));

        Assert.DoesNotContain("Abrir agora", html);
        Assert.DoesNotContain("class=\"botao\"", html);
        Assert.DoesNotContain("Se o botão não abrir", html);
        Assert.DoesNotContain("outro.exemplo", html);
        Assert.DoesNotContain("javascript:", html);
    }

    [Fact]
    public void Paragrafos_listas_e_links_do_corpo()
    {
        var html = Html("Primeiro.\nContinua.\n\n- item um\n- item dois\n\nhttps://exemplo.com/a?x=1&y=2\n\njavascript:alert(1)\n");

        Assert.Contains("Primeiro.<br>Continua.", html);
        Assert.Single(Regex.Matches(html, "<ul"));
        Assert.Equal(2, Regex.Matches(html, "<li").Count);
        Assert.Contains("item um</li>", html);
        Assert.Contains("<a href=\"https://exemplo.com/a?x=1&amp;y=2\"", html);
        Assert.Contains("javascript:alert(1)", html);
        Assert.DoesNotContain("href=\"javascript", html);
    }

    [Fact]
    public void Bloco_com_maior_que_vira_citacao()
    {
        var html = Html("Sua solicitação foi resolvida:\n\n> Linha um.\n> Linha dois.\n\nPara confirmar, abra.\n");

        Assert.Matches("<blockquote[^>]*border-left[^>]*>[^<]*Linha um\\.<br>Linha dois\\.", html);
        Assert.Single(Regex.Matches(html, "<blockquote"));
        Assert.DoesNotContain("&gt; Linha", html);
    }
}
