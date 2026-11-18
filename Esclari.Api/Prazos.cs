using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api;

public record Prazos(int AbandonoMinutos, int ConfirmacaoDias, int LembreteAguardandoDias, int ResolverAguardandoDias, int ValidadeConhecimentoMeses = 12,
    int PrimeiraRespostaHoras = 24, int ResolucaoHoras = 120)
{
    public const string ChaveValidade = "Prazos:ValidadeConhecimentoMeses";
    public const string ChavePrimeiraResposta = "Prazos:PrimeiraRespostaHoras";
    public const string ChaveResolucao = "Prazos:ResolucaoHoras";
    public const int MaximoDeHoras = 8760;

    public static Prazos Ler(IConfiguration cfg)
    {
        var validade = cfg.GetValue(ChaveValidade, 12);
        if (validade is < 1 or > 60) throw new InvalidOperationException($"{ChaveValidade} deve ficar entre 1 e 60 meses (recebido: {validade}).");
        var primeira = Horas(cfg, ChavePrimeiraResposta, 24);
        var resolucao = Horas(cfg, ChaveResolucao, 120);
        if (resolucao < primeira) throw new InvalidOperationException($"{ChaveResolucao} ({resolucao}) não pode ser menor que {ChavePrimeiraResposta} ({primeira}).");
        return new(
            cfg.GetValue("Prazos:AbandonoMinutos", 30),
            cfg.GetValue("Prazos:ConfirmacaoDias", 7),
            cfg.GetValue("Prazos:LembreteAguardandoDias", 3),
            cfg.GetValue("Prazos:ResolverAguardandoDias", 7),
            validade, primeira, resolucao);
    }

    static int Horas(IConfiguration cfg, string chave, int padrao)
    {
        var horas = cfg.GetValue(chave, padrao);
        if (horas is < 1 or > MaximoDeHoras) throw new InvalidOperationException($"{chave} deve ficar entre 1 e {MaximoDeHoras} horas (recebido: {horas}).");
        return horas;
    }

    public Prazos Para(Instituicao i) => Com(i.Parametros);

    public Prazos Com(ParametrosDaInstituicao p) => new(
        p.AbandonoMinutos ?? AbandonoMinutos, p.ConfirmacaoDias ?? ConfirmacaoDias, p.LembreteAguardandoDias ?? LembreteAguardandoDias, p.ResolverAguardandoDias ?? ResolverAguardandoDias,
        p.ValidadeConhecimentoMeses ?? ValidadeConhecimentoMeses, p.PrimeiraRespostaHoras ?? PrimeiraRespostaHoras, p.ResolucaoHoras ?? ResolucaoHoras);

    public async Task<IReadOnlyList<(long Id, Prazos Prazos)>> PorInstituicao(EsclariDb db) =>
        [.. (await db.Instituicoes.AsNoTracking().ToListAsync()).Select(i => (i.Id, Para(i)))];

    public async Task<Prazos> DaInstituicao(EsclariDb db, long instituicaoId) => Para(await db.Instituicoes.AsNoTracking().SingleAsync(i => i.Id == instituicaoId));

    public TimeSpan Abandono => TimeSpan.FromMinutes(AbandonoMinutos);
    public TimeSpan Confirmacao => TimeSpan.FromDays(ConfirmacaoDias);
    public TimeSpan Lembrete => TimeSpan.FromDays(LembreteAguardandoDias);
    public TimeSpan ResolverAguardando => TimeSpan.FromDays(ResolverAguardandoDias);
    public TimeSpan PrimeiraResposta => TimeSpan.FromHours(PrimeiraRespostaHoras);
    public TimeSpan Resolucao => TimeSpan.FromHours(ResolucaoHoras);
    public AtrasoDoChamado.Metas Metas => new(PrimeiraResposta, Resolucao);

    public int RejeicaoDias => ConfirmacaoDias;
    public TimeSpan Rejeicao => Confirmacao;
}
