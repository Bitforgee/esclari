namespace Esclari.Api.Dados;

public sealed record ParametrosDaInstituicao
{
    public int? AbandonoMinutos { get; init; }
    public int? ConfirmacaoDias { get; init; }
    public int? LembreteAguardandoDias { get; init; }
    public int? ResolverAguardandoDias { get; init; }
    public int? ValidadeConhecimentoMeses { get; init; }
    public int? PrimeiraRespostaHoras { get; init; }
    public int? ResolucaoHoras { get; init; }
}
