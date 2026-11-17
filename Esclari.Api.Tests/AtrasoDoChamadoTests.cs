using Esclari.Api.Dados;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Tests;

public class AtrasoDoChamadoTests
{
    const long Aluno = 2, Atendente = 7;
    static readonly DateTime T0 = new(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);
    static readonly AtrasoDoChamado.Metas Metas = new(TimeSpan.FromHours(24), TimeSpan.FromHours(120));

    static Chamado Novo() => Chamado.Abrir(new("ESC-000001", "Boleto", null, new(1, Aluno, 3, 4), new(10, "IA", 20)), T0);

    static string? Atraso(Chamado c, DateTime agora) => AtrasoDoChamado.Tipo(AtrasoDoChamado.Do(c, agora), Metas, agora);

    [Fact]
    public void Primeira_resposta_vence_em_24_horas_exatas()
    {
        var c = Novo();

        Assert.Null(Atraso(c, T0.AddHours(24).AddMinutes(-1)));
        Assert.Equal(AtrasoDoChamado.PrimeiraResposta, Atraso(c, T0.AddHours(24)));
        Assert.Equal("PRIMEIRA_RESPOSTA", AtrasoDoChamado.PrimeiraResposta);
        c.Assumir(Atendente, null, "Beto", T0.AddHours(1));
        Assert.Equal(EmAtendimento, c.Estado);
        Assert.Null(Atraso(c, T0.AddHours(24).AddMinutes(-1)));
        Assert.Equal(AtrasoDoChamado.PrimeiraResposta, Atraso(c, T0.AddHours(24)));
    }

    [Fact]
    public void So_mensagem_publica_da_equipe_conta_como_resposta()
    {
        var c = Novo();
        var depois = T0.AddHours(30);

        c.Anotar(Atendente, "Verificar no financeiro", T0.AddHours(1));
        Assert.Equal(AtrasoDoChamado.PrimeiraResposta, Atraso(c, depois));
        c.ResponderComoAluno(Aluno, "Alguma novidade?", [], T0.AddHours(2));
        Assert.Equal(AtrasoDoChamado.PrimeiraResposta, Atraso(c, depois));
        c.ResponderComoEquipe(Atendente, "Beto", "Estamos vendo.", [], false, T0.AddHours(3));
        Assert.Null(Atraso(c, depois));
    }

    [Fact]
    public void Resolucao_pausa_em_aguardando_aluno()
    {
        var c = Novo();
        c.ResponderComoEquipe(Atendente, "Beto", "Envie o comprovante.", [], true, T0.AddHours(2));
        c.ResponderComoAluno(Aluno, "Segue.", [], T0.AddHours(32));
        Assert.Equal(EmAtendimento, c.Estado);

        Assert.Null(Atraso(c, T0.AddHours(150).AddMinutes(-1)));
        Assert.Equal(AtrasoDoChamado.Resolucao, Atraso(c, T0.AddHours(150)));
        Assert.Equal("RESOLUCAO", AtrasoDoChamado.Resolucao);
        Assert.Equal(TimeSpan.FromHours(30), AtrasoDoChamado.Do(c, T0.AddHours(150)).Pausado);
    }

    [Fact]
    public void Estados_fora_do_atendimento_nunca_atrasam()
    {
        var aguardando = Novo();
        aguardando.ResponderComoEquipe(Atendente, "Beto", "Envie o comprovante.", [], true, T0.AddHours(1));
        var resolvido = Novo();
        resolvido.Resolver(Atendente, "Beto", "Feito.", T0.AddHours(1));
        var fechado = Novo();
        fechado.Cancelar(Aluno, null, T0.AddHours(1));
        var mil = T0.AddHours(1000);

        Assert.Equal((AguardandoAluno, Resolvido, Fechado), (aguardando.Estado, resolvido.Estado, fechado.Estado));
        Assert.All(new[] { aguardando, resolvido, fechado }, c => Assert.Null(Atraso(c, mil)));
        Assert.Null(AtrasoDoChamado.Tipo(new(AguardandoAluno, T0, false, TimeSpan.Zero), Metas, mil));
        Assert.Null(AtrasoDoChamado.Tipo(new(Resolvido, T0, false, TimeSpan.Zero), Metas, mil));
        Assert.Null(AtrasoDoChamado.Tipo(new(Fechado, T0, false, TimeSpan.Zero), Metas, mil));
        Assert.Equal(AtrasoDoChamado.Resolucao, AtrasoDoChamado.Tipo(new(Aberto, T0, true, TimeSpan.Zero), Metas, mil));
    }

    [Fact]
    public void Os_dois_atrasos_devolvem_primeira_resposta()
    {
        var c = Novo();
        var agora = T0.AddHours(200);

        var vencido = AtrasoDoChamado.Calcular(AtrasoDoChamado.Do(c, agora), Metas, agora);

        Assert.Equal(AtrasoDoChamado.PrimeiraResposta, vencido!.Tipo);
        Assert.True(T0 + Metas.Resolucao <= agora);
        Assert.Equal(T0.AddHours(24), vencido.VenceuEm);
    }
}
