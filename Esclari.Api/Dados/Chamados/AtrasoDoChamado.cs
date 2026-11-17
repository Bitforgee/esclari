using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Dados.Chamados;

public static class AtrasoDoChamado
{
    public const string PrimeiraResposta = "PRIMEIRA_RESPOSTA";
    public const string Resolucao = "RESOLUCAO";

    public sealed record Metas(TimeSpan PrimeiraResposta, TimeSpan Resolucao);

    public sealed record Mensagem(long? AutorId, string Tipo);

    public sealed record Transicao(string? De, string? Para, DateTime CriadoEm);

    public sealed record Situacao(string Estado, DateTime CriadoEm, bool RespondidoPelaEquipe, TimeSpan Pausado);

    public sealed record Vencido(string Tipo, DateTime VenceuEm);

    public static bool EmAtendimentoAberto(string estado) => estado is Aberto or EmAtendimento;

    public static bool RespondidoPelaEquipe(long solicitanteId, IEnumerable<Mensagem> mensagens) =>
        mensagens.Any(m => m.Tipo == TiposMensagem.Publica && m.AutorId is { } autor && autor != solicitanteId);

    public static TimeSpan Pausado(IEnumerable<Transicao> transicoes, DateTime agora)
    {
        var pausado = TimeSpan.Zero;
        DateTime? desde = null;
        foreach (var t in transicoes.OrderBy(t => t.CriadoEm))
        {
            if (t.Para == AguardandoAluno) desde ??= t.CriadoEm;
            else if (t.De == AguardandoAluno && desde is { } inicio) { pausado += t.CriadoEm - inicio; desde = null; }
        }
        return desde is { } aberto ? pausado + (agora - aberto) : pausado;
    }

    public static Situacao Do(Chamado c, DateTime agora) =>
        new(c.Estado, c.CriadoEm, RespondidoPelaEquipe(c.SolicitanteId, c.Mensagens.Select(m => new Mensagem(m.AutorId, m.Tipo))),
            Pausado(c.Eventos.Where(e => e.Tipo == TiposEvento.Transicao).Select(e => new Transicao(e.De, e.Para, e.CriadoEm)), agora));

    public static Vencido? Calcular(Situacao s, Metas metas, DateTime agora)
    {
        if (!EmAtendimentoAberto(s.Estado)) return null;
        var primeira = s.CriadoEm + metas.PrimeiraResposta;
        if (!s.RespondidoPelaEquipe && primeira <= agora) return new(PrimeiraResposta, primeira);
        var resolucao = s.CriadoEm + metas.Resolucao + s.Pausado;
        return resolucao <= agora ? new(Resolucao, resolucao) : null;
    }

    public static string? Tipo(Situacao s, Metas metas, DateTime agora) => Calcular(s, metas, agora)?.Tipo;

    public static DateTime? PrevisaoDaPrimeiraResposta(string estado, DateTime criadoEm, bool respondidoPelaEquipe, Metas metas) =>
        EmAtendimentoAberto(estado) && !respondidoPelaEquipe ? criadoEm + metas.PrimeiraResposta : null;
}
