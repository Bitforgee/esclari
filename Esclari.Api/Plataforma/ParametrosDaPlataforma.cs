using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record Parametro(int? Valor, int Padrao, int Efetivo);

public sealed record ParametrosResposta(
    Parametro AbandonoMinutos, Parametro ConfirmacaoDias, Parametro LembreteAguardandoDias, Parametro ResolverAguardandoDias,
    Parametro ValidadeConhecimentoMeses, Parametro PrimeiraRespostaHoras, Parametro ResolucaoHoras);

public static class ParametrosDaPlataforma
{
    sealed record Definicao(string Campo, string Rotulo, int Minimo, int Maximo, Func<ParametrosDaInstituicao, int?> Proprio, Func<Prazos, int> Padrao);

    static readonly Definicao[] Definicoes =
    [
        new("abandonoMinutos", "O abandono", 5, 1440, p => p.AbandonoMinutos, p => p.AbandonoMinutos),
        new("confirmacaoDias", "A confirmação", 1, 60, p => p.ConfirmacaoDias, p => p.ConfirmacaoDias),
        new("lembreteAguardandoDias", "O lembrete", 1, 90, p => p.LembreteAguardandoDias, p => p.LembreteAguardandoDias),
        new("resolverAguardandoDias", "A resolução automática", 1, 90, p => p.ResolverAguardandoDias, p => p.ResolverAguardandoDias),
        new("validadeConhecimentoMeses", "A validade", 1, 60, p => p.ValidadeConhecimentoMeses, p => p.ValidadeConhecimentoMeses),
        new("primeiraRespostaHoras", "A meta de primeira resposta", 1, Prazos.MaximoDeHoras, p => p.PrimeiraRespostaHoras, p => p.PrimeiraRespostaHoras),
        new("resolucaoHoras", "A meta de resolução", 1, Prazos.MaximoDeHoras, p => p.ResolucaoHoras, p => p.ResolucaoHoras),
    ];

    static Parametro Um(Definicao d, ParametrosDaInstituicao proprio, Prazos padrao) =>
        new(d.Proprio(proprio), d.Padrao(padrao), d.Proprio(proprio) ?? d.Padrao(padrao));

    public static ParametrosResposta Resposta(Instituicao i, Prazos padrao)
    {
        var p = i.Parametros;
        var v = Definicoes.Select(d => Um(d, p, padrao)).ToArray();
        return new(v[0], v[1], v[2], v[3], v[4], v[5], v[6]);
    }

    public static IResult? Validar(ParametrosDaInstituicao novo, Prazos padrao)
    {
        foreach (var d in Definicoes)
            if (d.Proprio(novo) is { } valor && (valor < d.Minimo || valor > d.Maximo))
                return Problemas.Invalido($"{d.Rotulo} deve ficar entre {d.Minimo} e {d.Maximo}.", d.Campo);
        var efetivo = padrao.Com(novo);
        if (efetivo.ResolucaoHoras < efetivo.PrimeiraRespostaHoras)
            return Problemas.Invalido("A meta de resolução não pode ser menor que a de primeira resposta.", "resolucaoHoras");
        return efetivo.LembreteAguardandoDias >= efetivo.ResolverAguardandoDias
            ? Problemas.Invalido("O lembrete precisa vir antes da resolução automática.", "lembreteAguardandoDias")
            : null;
    }

    public static string Mudancas(ParametrosDaInstituicao antes, ParametrosDaInstituicao depois)
    {
        var mudou = Definicoes.Where(d => d.Proprio(antes) != d.Proprio(depois)).Select(d => $"{d.Campo}={d.Proprio(depois)?.ToString() ?? "padrão"}").ToList();
        return mudou.Count == 0 ? "nenhuma mudança" : string.Join(", ", mudou);
    }
}
