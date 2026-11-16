using Esclari.Api.Dados;
using Esclari.Api.Indicadores;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Tests;

public class CalculoDetalheTests
{
    static readonly TimeZoneInfo Cuiaba = TimeZoneInfo.FindSystemTimeZoneById("America/Cuiaba");
    static readonly DateTime Meio = new(2026, 11, 10, 16, 0, 0, DateTimeKind.Utc);
    static readonly Dictionary<long, string> Categorias = new() { [1] = "Financeiro" };
    static long ultimoEvento;

    static IEnumerable<EventoChamado> Transicoes(long chamado, DateTime criado, params (double h, string de, string para)[] t) =>
        t.Select(x => new EventoChamado { Id = ++ultimoEvento, ChamadoId = chamado, Tipo = "TRANSICAO", De = x.de, Para = x.para, CriadoEm = criado.AddHours(x.h) });

    static ChamadoLido Chamado(long id, DateTime criado, string estado = Aberto, string? motivo = null) =>
        new(id, criado, estado, motivo, 0, 0, 1, 10, 100 + id);

    static Massa Massa(List<ChamadoLido>? chamados = null, List<EventoChamado>? eventos = null, List<RespostaLida>? respostas = null, List<ConversaLida>? conversas = null) =>
        new(chamados ?? [], eventos ?? [], respostas ?? [], conversas, [], Categorias);

    static Detalhe Calcular(Massa m, DateOnly de, DateOnly ate, DateTime? agora = null) => CalculoDetalhe.Calcular(m, de, ate, Cuiaba, agora ?? Meio);

    static Massa UmChamadoComResposta(double horas, long id) =>
        Massa([Chamado(id, Meio)], respostas: [new RespostaLida(id, 900, Meio.AddHours(horas))]);

    [Fact]
    public void Ate_62_dias_um_ponto_por_dia()
    {
        var d = Calcular(Massa(), new(2026, 11, 10), new(2026, 11, 12));

        Assert.Equal("dia", d.Granularidade);
        Assert.Equal([new DateOnly(2026, 11, 10), new(2026, 11, 11), new(2026, 11, 12)], d.Pontos.Select(p => p.Inicio));
        Assert.All(d.Pontos, p => Assert.Equal((0, 0), (p.ChamadosAbertos, p.ChamadosResolvidos)));
    }

    [Fact]
    public void Limite_de_62_dias()
    {
        var inicio = new DateOnly(2026, 9, 1);
        var com62 = Calcular(Massa(), inicio, inicio.AddDays(61));
        var com63 = Calcular(Massa(), inicio, inicio.AddDays(62));

        Assert.Equal(("dia", 62), (com62.Granularidade, com62.Pontos.Count));
        Assert.Equal("semana", com63.Granularidade);
    }

    [Fact]
    public void Semana_comeca_na_segunda_no_fuso()
    {
        var quarta = new DateOnly(2026, 9, 2);
        var chamadoNaSemanaDoPeriodoMasFora = Chamado(1, new(2026, 9, 1, 15, 0, 0, DateTimeKind.Utc));
        var dentro = Chamado(2, new(2026, 9, 3, 15, 0, 0, DateTimeKind.Utc));
        var d = Calcular(Massa([chamadoNaSemanaDoPeriodoMasFora, dentro]), quarta, quarta.AddDays(70));

        Assert.Equal("semana", d.Granularidade);
        Assert.All(d.Pontos, p => Assert.Equal(DayOfWeek.Monday, p.Inicio.DayOfWeek));
        Assert.Equal(new DateOnly(2026, 8, 31), d.Pontos[0].Inicio);
        Assert.Equal(11, d.Pontos.Count);
        Assert.Equal(1, d.Pontos[0].ChamadosAbertos);
        Assert.Equal(1, d.Pontos.Sum(p => p.ChamadosAbertos));
    }

    [Fact]
    public void Ponto_usa_o_fuso_da_instituicao()
    {
        var as2330Local = new DateTime(2026, 11, 11, 3, 30, 0, DateTimeKind.Utc);
        var d = Calcular(Massa([Chamado(1, as2330Local)]), new(2026, 11, 10), new(2026, 11, 11));

        Assert.Equal([1, 0], d.Pontos.Select(p => p.ChamadosAbertos));
    }

    [Fact]
    public void Sem_chamados_tempos_nulos()
    {
        var d = Calcular(Massa(), new(2026, 11, 10), new(2026, 11, 10));

        Assert.Equal(new Tempos(null, null, null, null), d.Tempos);
    }

    [Fact]
    public void Mediana_e_p90_da_primeira_resposta()
    {
        var horas = new double[] { 1, 2, 3, 10 };
        var chamados = horas.Select((h, i) => Chamado(i + 1, Meio)).ToList();
        var respostas = horas.Select((h, i) => new RespostaLida(i + 1, 900, Meio.AddHours(h))).ToList();

        var d = Calcular(Massa(chamados, respostas: respostas), new(2026, 11, 10), new(2026, 11, 10));

        Assert.Equal(2.5, d.Tempos.PrimeiraRespostaMedianaHoras);
        Assert.Equal(10, d.Tempos.PrimeiraRespostaP90Horas);
    }

    [Fact]
    public void Resolucao_desconta_aguardando_aluno()
    {
        var eventos = Transicoes(1, Meio, (1, Aberto, EmAtendimento), (3, EmAtendimento, AguardandoAluno), (7, AguardandoAluno, EmAtendimento), (10, EmAtendimento, Resolvido)).ToList();

        var d = Calcular(Massa([Chamado(1, Meio, Resolvido)], eventos), new(2026, 11, 10), new(2026, 11, 10));

        Assert.Equal(6, d.Tempos.ResolucaoMedianaHoras);
        Assert.Equal(6, d.Tempos.ResolucaoP90Horas);
    }

    [Fact]
    public void Mais_antigo_conta_so_o_que_espera_a_equipe()
    {
        var agora = new DateTime(2026, 11, 19, 3, 0, 0, DateTimeKind.Utc);
        var d = Calcular(Massa([
            Chamado(1, agora.AddDays(-9).AddHours(-3)),
            Chamado(2, agora.AddDays(-20), AguardandoAluno),
            Chamado(3, agora.AddDays(-30), Resolvido),
            Chamado(4, agora.AddDays(-40), Fechado, "CONFIRMADO")]), new(2026, 11, 10), new(2026, 11, 10), agora);

        Assert.Equal(9, d.MaisAntigoDias);
        Assert.Equal(new Dictionary<string, int> { [Aberto] = 1, [EmAtendimento] = 0, [AguardandoAluno] = 1, [Resolvido] = 1 }, d.ChamadosPorEstado);

        var nenhum = Calcular(Massa([Chamado(2, agora.AddDays(-20), AguardandoAluno)]), new(2026, 11, 10), new(2026, 11, 10), agora);
        Assert.Null(nenhum.MaisAntigoDias);
    }

    [Fact]
    public void Desfechos_contam_cada_um_e_ficam_nulos_sem_conversas_lidas()
    {
        var conversas = new[] { Desfechos.Resolvida, Desfechos.Resolvida, Desfechos.Transferida, Desfechos.Abandonada, Desfechos.Encerrada, null }
            .Select(x => new ConversaLida(1, x, false, Meio)).ToList();

        var com = Calcular(Massa(conversas: conversas), new(2026, 11, 10), new(2026, 11, 10));
        var sem = Calcular(Massa(), new(2026, 11, 10), new(2026, 11, 10));

        Assert.Equal(new DesfechosEmContagem(2, 1, 1, 1, 1), com.Desfechos);
        Assert.Equal(6, com.Pontos.Single().Conversas);
        Assert.Null(sem.Desfechos);
        Assert.Null(sem.Pontos.Single().Conversas);
    }
}
