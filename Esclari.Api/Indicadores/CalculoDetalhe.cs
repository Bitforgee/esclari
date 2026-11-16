using Esclari.Api.Dados;
using Esclari.Api.Dados.Conversas;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Indicadores;

public record Ponto(DateOnly Inicio, int? Conversas, int ChamadosAbertos, int ChamadosResolvidos);
public record DesfechosEmContagem(int Resolvidas, int Transferidas, int Abandonadas, int Encerradas, int SemDesfecho);
public record Tempos(double? PrimeiraRespostaMedianaHoras, double? PrimeiraRespostaP90Horas, double? ResolucaoMedianaHoras, double? ResolucaoP90Horas);
public record Detalhe(string Granularidade, List<Ponto> Pontos, DesfechosEmContagem? Desfechos, Dictionary<string, int> ChamadosPorEstado, int? MaisAntigoDias, Tempos Tempos);

public static class CalculoDetalhe
{
    const int LimiteDeDiasPorDia = 62;
    static readonly string[] Estados = [Aberto, EmAtendimento, AguardandoAluno, Resolvido];

    public static Detalhe Calcular(Massa m, DateOnly de, DateOnly ate, TimeZoneInfo fuso, DateTime agora)
    {
        var (inicio, fim) = CalculoIndicadores.Periodo(de, ate, fuso);
        var eventos = m.Eventos.OrderBy(e => e.CriadoEm).ThenBy(e => e.Id).ToLookup(e => e.ChamadoId);
        var criados = m.Chamados.Where(c => c.CriadoEm >= inicio && c.CriadoEm < fim).ToList();
        var porSemana = ate.DayNumber - de.DayNumber + 1 > LimiteDeDiasPorDia;
        return new Detalhe(porSemana ? "semana" : "dia", Serie(m, (de, ate), (inicio, fim), fuso, porSemana), ContarDesfechos(m.Conversas),
            PorEstado(m.Chamados), MaisAntigo(m.Chamados, agora), CalcularTempos(m, criados, eventos));
    }

    static List<Ponto> Serie(Massa m, (DateOnly De, DateOnly Ate) dias, (DateTime Inicio, DateTime Fim) janela, TimeZoneInfo fuso, bool porSemana)
    {
        DateOnly Balde(DateTime utc)
        {
            var local = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, fuso));
            return porSemana ? local.AddDays(-(((int)local.DayOfWeek + 6) % 7)) : local;
        }
        bool Dentro(DateTime t) => t >= janela.Inicio && t < janela.Fim;
        var conversas = m.Conversas?.Where(c => Dentro(c.CriadaEm)).ToLookup(c => Balde(c.CriadaEm));
        var abertos = m.Chamados.Where(c => Dentro(c.CriadoEm)).ToLookup(c => Balde(c.CriadoEm));
        var resolvidos = UltimaResolucaoNoPeriodo(m, Dentro).ToLookup(Balde);
        return Baldes(dias, porSemana).Select(b => new Ponto(b, conversas?[b].Count(), abertos[b].Count(), resolvidos[b].Count())).ToList();
    }

    static IEnumerable<DateTime> UltimaResolucaoNoPeriodo(Massa m, Func<DateTime, bool> dentro)
    {
        var ids = m.Chamados.Select(c => c.Id).ToHashSet();
        return m.Eventos.Where(e => e.Tipo == TiposEvento.Transicao && e.Para == Resolvido && dentro(e.CriadoEm) && ids.Contains(e.ChamadoId))
            .GroupBy(e => e.ChamadoId).Select(g => g.Max(e => e.CriadoEm));
    }

    static IEnumerable<DateOnly> Baldes((DateOnly De, DateOnly Ate) dias, bool porSemana)
    {
        var passo = porSemana ? 7 : 1;
        var primeiro = porSemana ? dias.De.AddDays(-(((int)dias.De.DayOfWeek + 6) % 7)) : dias.De;
        for (var d = primeiro; d <= dias.Ate; d = d.AddDays(passo)) yield return d;
    }

    static DesfechosEmContagem? ContarDesfechos(List<ConversaLida>? conversas)
    {
        if (conversas is null) return null;
        int Contar(string? d) => conversas.Count(c => c.Desfecho == d);
        return new(Contar(Desfechos.Resolvida), Contar(Desfechos.Transferida), Contar(Desfechos.Abandonada), Contar(Desfechos.Encerrada), Contar(null));
    }

    static Dictionary<string, int> PorEstado(List<ChamadoLido> chamados) =>
        Estados.ToDictionary(e => e, e => chamados.Count(c => c.Estado == e));

    static int? MaisAntigo(List<ChamadoLido> chamados, DateTime agora)
    {
        var esperando = chamados.Where(c => c.Estado is Aberto or EmAtendimento).Select(c => c.CriadoEm).ToList();
        return esperando.Count == 0 ? null : (int)(agora - esperando.Min()).TotalDays;
    }

    static Tempos CalcularTempos(Massa m, List<ChamadoLido> criados, ILookup<long, EventoChamado> eventos)
    {
        var primeira = CalculoIndicadores.TemposDePrimeiraResposta(m, criados).Select(t => t.TotalHours).ToList();
        var resolucao = criados.Where(c => CalculoIndicadores.Resolucoes(eventos[c.Id]).Any())
            .Select(c => CalculoIndicadores.Resolucao(c, eventos[c.Id]).TotalHours).ToList();
        return new(Mediana(primeira), Percentil90(primeira), Mediana(resolucao), Percentil90(resolucao));
    }

    static double? Mediana(List<double> valores)
    {
        if (valores.Count == 0) return null;
        var o = valores.Order().ToList();
        var meio = o.Count / 2;
        return Math.Round(o.Count % 2 == 1 ? o[meio] : (o[meio - 1] + o[meio]) / 2, 2);
    }

    static double? Percentil90(List<double> valores)
    {
        if (valores.Count == 0) return null;
        var posto = (int)Math.Ceiling(0.9 * valores.Count);
        return Math.Round(valores.Order().ElementAt(posto - 1), 2);
    }
}
