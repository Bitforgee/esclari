using System.Text;

namespace Esclari.Api.Chamados;

public static class CsvDeChamados
{
    public const string Separador = ";";
    public const string FimDeLinha = "\r\n";

    static readonly char[] DeFormula = ['=', '+', '-', '@', '\t', '\r'];
    static readonly char[] PedemAspas = [';', '"', '\r', '\n'];

    public static string Celula(string? valor)
    {
        var texto = valor ?? "";
        if (texto.Length > 0 && DeFormula.Contains(texto[0])) texto = "'" + texto;
        return texto.IndexOfAny(PedemAspas) >= 0 ? "\"" + texto.Replace("\"", "\"\"") + "\"" : texto;
    }

    public static string Linha(IEnumerable<string?> celulas) => string.Join(Separador, celulas.Select(Celula)) + FimDeLinha;

    public static byte[] Arquivo(IEnumerable<string> linhas)
    {
        var texto = new StringBuilder();
        foreach (var linha in linhas) texto.Append(linha);
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(texto.ToString())];
    }
}
