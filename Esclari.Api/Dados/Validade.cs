namespace Esclari.Api.Dados;

public readonly record struct Validade(DateOnly Hoje, int Meses)
{
    public const string Vigente = "VIGENTE";
    public const string VenceEmBreve = "VENCE_EM_BREVE";
    public const string Vencido = "VENCIDO";
    public const int DiasEmBreve = 30;
    public const string RecusaArquivado = "Item arquivado não pode ser renovado.";

    public DateOnly Ate => Hoje.AddMonths(Meses);

    public static DateOnly HojeNoFuso(string fuso, DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(fuso)));

    public static string Situacao(DateOnly validoAte, DateOnly hoje) =>
        validoAte < hoje ? Vencido : validoAte.DayNumber - hoje.DayNumber <= DiasEmBreve ? VenceEmBreve : Vigente;
}
