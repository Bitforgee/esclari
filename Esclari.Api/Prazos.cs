namespace Esclari.Api;

public record Prazos(int AbandonoMinutos, int ConfirmacaoDias, int LembreteAguardandoDias, int ResolverAguardandoDias, int ValidadeConhecimentoMeses = 12)
{
    public const string ChaveValidade = "Prazos:ValidadeConhecimentoMeses";

    public static Prazos Ler(IConfiguration cfg)
    {
        var validade = cfg.GetValue(ChaveValidade, 12);
        if (validade is < 1 or > 60) throw new InvalidOperationException($"{ChaveValidade} deve ficar entre 1 e 60 meses (recebido: {validade}).");
        return new(
            cfg.GetValue("Prazos:AbandonoMinutos", 30),
            cfg.GetValue("Prazos:ConfirmacaoDias", 7),
            cfg.GetValue("Prazos:LembreteAguardandoDias", 3),
            cfg.GetValue("Prazos:ResolverAguardandoDias", 7),
            validade);
    }

    public TimeSpan Abandono => TimeSpan.FromMinutes(AbandonoMinutos);
    public TimeSpan Confirmacao => TimeSpan.FromDays(ConfirmacaoDias);
    public TimeSpan Lembrete => TimeSpan.FromDays(LembreteAguardandoDias);
    public TimeSpan ResolverAguardando => TimeSpan.FromDays(ResolverAguardandoDias);

    public int RejeicaoDias => ConfirmacaoDias;
    public TimeSpan Rejeicao => Confirmacao;
}
