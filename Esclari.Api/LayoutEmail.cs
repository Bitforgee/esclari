using System.Text;
using System.Text.RegularExpressions;
using Esclari.Api.Dados.Emails;

namespace Esclari.Api;

public static partial class LayoutEmail
{
    const string Tinta = "#0e1330";
    const string Fonte = "font-family:Arial,Helvetica,sans-serif";
    const string Texto = Fonte + ";font-size:16px;line-height:1.5;color:" + Tinta;
    const int TamanhoDaPrevia = 90;

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")] private static partial Regex CorValida();
    [GeneratedRegex(@"^https?://\S+$")] private static partial Regex Endereco();
    [GeneratedRegex(@"\n[ \t]*\n")] private static partial Regex LinhaEmBranco();

    static string Escapar(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    public static string Html(string nome, string? cor, bool logo, string corpo, string assunto, string? titulo, AcaoDoEmail? acao, string urlBase, bool logoDoEsclari = false)
    {
        var marca = cor is not null && CorValida().IsMatch(cor) ? cor : Tinta;
        var blocos = Blocos(corpo);
        var botao = acao is not null && acao.Url.StartsWith(urlBase.TrimEnd('/') + "/", StringComparison.Ordinal) ? acao : null;
        var html = new StringBuilder();
        html.Append("<!DOCTYPE html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
            .Append("<meta name=\"color-scheme\" content=\"light dark\"><meta name=\"supported-color-schemes\" content=\"light dark\">")
            .Append($"<title>{Escapar(assunto)}</title>")
            .Append("<style>@media (prefers-color-scheme: dark){.fundo{background-color:#0e0e18!important}.cartao{background-color:#151620!important}.texto{color:#f0eee9!important}.suave{color:#a0a1a8!important}}")
            .Append("@media (max-width:480px){.miolo{padding-left:20px!important;padding-right:20px!important}.botao{display:block!important;width:100%!important}}</style></head>")
            .Append("<body class=\"fundo\" style=\"margin:0;padding:0;background-color:#f7f5ee\">")
            .Append(Previa(corpo))
            .Append("<table role=\"presentation\" width=\"100%\" class=\"fundo\" style=\"background-color:#f7f5ee\"><tr><td align=\"center\" style=\"padding:24px 12px\">")
            .Append("<table role=\"presentation\" width=\"100%\" style=\"max-width:600px;width:100%\">")
            .Append(Cabecalho(nome, marca, logo, logoDoEsclari))
            .Append("<tr><td class=\"cartao\" style=\"background-color:#ffffff\"><table role=\"presentation\" width=\"100%\"><tr><td class=\"miolo\" style=\"padding:32px\">")
            .Append(titulo is null ? "" : $"<h1 class=\"texto\" style=\"margin:0 0 16px;{Fonte};font-size:24px;line-height:1.3;font-weight:bold;color:{Tinta}\">{Escapar(titulo)}</h1>")
            .Append(Conteudo(blocos, marca, botao?.Url))
            .Append(botao is null ? "" : Botao(botao, marca))
            .Append("</td></tr></table></td></tr>")
            .Append(Rodape(nome))
            .Append("</table></td></tr></table></body></html>");
        return html.ToString();
    }

    static List<string> Blocos(string corpo) =>
        [.. LinhaEmBranco().Split(corpo.Replace("\r", "").Trim('\n')).Select(b => b.TrimEnd()).Where(b => b != "")];

    static string Previa(string corpo)
    {
        var primeiro = Blocos(corpo).FirstOrDefault();
        if (primeiro is null) return "";
        var texto = string.Join(" ", primeiro.Split('\n').Select(l => l.Trim()));
        return $"<div style=\"display:none;max-height:0;overflow:hidden;mso-hide:all\">{Escapar(texto.Length > TamanhoDaPrevia ? texto[..TamanhoDaPrevia] : texto)}</div>";
    }

    static string Cabecalho(string nome, string marca, bool logo, bool logoDoEsclari)
    {
        var nomeEmTexto = $"<span style=\"{Fonte};font-size:20px;font-weight:bold;color:{Tinta};vertical-align:middle\">{Escapar(nome)}</span>";
        var imagem = $"<img src=\"cid:logo\" alt=\"{(logoDoEsclari ? "Esclari" : Escapar(nome))}\" style=\"max-height:40px;border:0;vertical-align:middle\">";
        var identidade = !logo ? nomeEmTexto : logoDoEsclari ? $"{imagem}<span style=\"padding-left:12px\"></span>{nomeEmTexto}" : imagem;
        return $"<tr><td style=\"height:4px;line-height:4px;font-size:0;background-color:{marca}\">&nbsp;</td></tr>" +
            $"<tr><td class=\"cabecalho\" style=\"background-color:#ffffff;padding:20px 32px\">{identidade}</td></tr>";
    }

    static string Rodape(string nome)
    {
        const string estilo = "margin:0 0 4px;" + Fonte + ";font-size:14px;line-height:1.5;color:#5c5d64";
        return $"<tr><td style=\"padding:16px 32px 0\"><p class=\"suave\" style=\"{estilo}\">Enviado pelo Esclari para {Escapar(nome)}.</p>" +
            $"<p class=\"suave\" style=\"{estilo}\">Este aviso é automático; não responda este e-mail.</p></td></tr>";
    }

    static string Botao(AcaoDoEmail acao, string marca)
    {
        var url = Escapar(acao.Url);
        var rotulo = Escapar(acao.Rotulo);
        return "<table role=\"presentation\" width=\"100%\" style=\"margin:8px 0 16px\"><tr><td>" +
            $"<!--[if mso]><v:roundrect xmlns:v=\"urn:schemas-microsoft-com:vml\" href=\"{url}\" style=\"height:48px;v-text-anchor:middle;width:260px\" arcsize=\"12%\" stroke=\"f\" fillcolor=\"{marca}\">" +
            $"<w:anchorlock/><center style=\"color:#ffffff;font-family:Arial,sans-serif;font-size:16px;font-weight:bold\">{rotulo}</center></v:roundrect><![endif]-->" +
            $"<!--[if !mso]><!--><a href=\"{url}\" class=\"botao\" style=\"display:inline-block;min-height:44px;line-height:44px;padding:0 24px;background-color:{marca};color:#ffffff;{Fonte};font-size:16px;font-weight:bold;text-decoration:none;text-align:center;border-radius:6px\">{rotulo}</a><!--<![endif]-->" +
            "</td></tr></table>" +
            $"<p class=\"suave\" style=\"margin:0;{Fonte};font-size:14px;line-height:1.5;color:#5c5d64\">Se o botão não abrir, copie este endereço:<br><a href=\"{url}\" style=\"color:inherit;text-decoration:underline;word-break:break-all\">{url}</a></p>";
    }

    static string Conteudo(List<string> blocos, string marca, string? urlDaAcao)
    {
        var html = new StringBuilder();
        foreach (var bloco in blocos)
            foreach (var (tipo, linhas) in Grupos(bloco))
                html.Append(Elemento(tipo, linhas, marca, urlDaAcao));
        return html.ToString();
    }

    static List<(char Tipo, List<string> Linhas)> Grupos(string bloco)
    {
        var grupos = new List<(char Tipo, List<string> Linhas)>();
        foreach (var linha in bloco.Split('\n'))
        {
            var tipo = Tipo(linha, grupos.Count > 0 ? grupos[^1].Tipo : ' ');
            if (grupos.Count == 0 || grupos[^1].Tipo != tipo) grupos.Add((tipo, []));
            grupos[^1].Linhas.Add(tipo == 'l' && linha.StartsWith("- ") ? linha[2..] : tipo == 'c' ? linha[2..] : linha.TrimEnd());
        }
        return grupos;
    }

    static char Tipo(string linha, char anterior) =>
        linha.StartsWith("- ") ? 'l' : linha.StartsWith("> ") ? 'c' : anterior == 'l' && linha.StartsWith(' ') ? 'l' : 'p';

    static string Elemento(char tipo, List<string> linhas, string marca, string? urlDaAcao) => tipo switch
    {
        'l' => "<ul class=\"texto\" style=\"margin:0 0 16px;padding-left:24px;" + Texto + "\">" + Itens(linhas, urlDaAcao) + "</ul>",
        'c' => $"<blockquote class=\"texto\" style=\"margin:0 0 16px;padding:4px 0 4px 16px;border-left:4px solid {marca};{Texto}\">{string.Join("<br>", linhas.Select(Escapar))}</blockquote>",
        _ => Paragrafo(linhas, urlDaAcao)
    };

    static string Itens(List<string> linhas, string? urlDaAcao)
    {
        var itens = new List<List<string>>();
        foreach (var linha in linhas)
            if (itens.Count == 0 || !linha.StartsWith(' ')) itens.Add([linha]);
            else itens[^1].Add(linha.Trim());
        return string.Concat(itens.Select(i => "<li style=\"margin:0 0 8px\">" + Linhas(i, urlDaAcao) + "</li>"));
    }

    static string Paragrafo(List<string> linhas, string? urlDaAcao)
    {
        var texto = Linhas(linhas, urlDaAcao);
        return texto == "" ? "" : "<p class=\"texto\" style=\"margin:0 0 16px;" + Texto + "\">" + texto + "</p>";
    }

    static string Linhas(List<string> linhas, string? urlDaAcao) =>
        string.Join("<br>", linhas.Where(l => l != urlDaAcao).Select(Linha));

    static string Linha(string linha) =>
        Endereco().IsMatch(linha.Trim()) ? $"<a href=\"{Escapar(linha.Trim())}\" style=\"color:inherit;text-decoration:underline;word-break:break-all\">{Escapar(linha.Trim())}</a>" : Escapar(linha);
}
