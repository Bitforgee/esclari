namespace Esclari.Api.Dados.Avisos;

public class AvisoValidade : IDaInstituicao
{
    public const string Artigo = "ARTIGO";
    public const string Documento = "DOCUMENTO";
    public const string Em30Dias = "EM_30_DIAS";
    public const string Em7Dias = "EM_7_DIAS";
    public const string Vencido = "VENCIDO";

    static readonly string[] Gravidade = [Em30Dias, Em7Dias, Vencido];

    AvisoValidade() { }

    public long Id { get; private set; }
    public long InstituicaoId { get; set; }
    public string Tipo { get; private set; } = "";
    public long ItemId { get; private set; }
    public DateOnly ValidoAte { get; private set; }
    public string Marco { get; private set; } = "";
    public DateTime CriadoEm { get; private set; }

    public static string? MarcoPara(DateOnly validoAte, DateOnly hoje) => (validoAte.DayNumber - hoje.DayNumber) switch
    {
        < 0 => Vencido,
        <= 7 => Em7Dias,
        <= 30 => Em30Dias,
        _ => null,
    };

    public static bool JaAvisado(string marco, IEnumerable<string> registrados) =>
        registrados.Any(r => Array.IndexOf(Gravidade, r) >= Array.IndexOf(Gravidade, marco));

    public static AvisoValidade Criar(string tipo, long itemId, DateOnly validoAte, string marco, DateTime agora) =>
        new() { Tipo = tipo, ItemId = itemId, ValidoAte = validoAte, Marco = marco, CriadoEm = agora };
}
