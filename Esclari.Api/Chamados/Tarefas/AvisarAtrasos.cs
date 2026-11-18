using System.Text;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Chamados.Tarefas;

public sealed class AvisarAtrasos(IConfiguration cfg, Prazos prazos, ILogger<Agendador> log) : ITarefaAgendada
{
    public static readonly TimeSpan Repeticao = TimeSpan.FromHours(24);

    public string Tipo => "atraso";

    sealed record DoSetor(long SetorId, string Email);

    sealed record Pessoa(long Id, string Email);

    sealed record Avisado(Chamado Chamado, AtrasoDoChamado.Vencido Vencido);

    sealed record Carta(string Para, string Assunto, string Corpo, List<Avisado> Chamados, string Titulo, AcaoDoEmail Acao);

    public Task<List<ItemAgendado>> Ler(EsclariDb db, DateTime agora)
    {
        var (primeira, resolucao, resumo) = (agora - prazos.PrimeiraResposta, agora - prazos.Resolucao, agora - Repeticao);
        return db.Chamados.IgnoreQueryFilters()
            .Where(c => (c.Estado == Aberto || c.Estado == EmAtendimento) && (c.AtrasoAvisadoEm == null || c.AtrasoAvisadoEm <= resumo))
            .Where(c => c.CriadoEm <= resolucao || (c.CriadoEm <= primeira && !db.MensagensChamado.IgnoreQueryFilters()
                .Any(m => m.ChamadoId == c.Id && m.Tipo == TiposMensagem.Publica && m.AutorId != null && m.AutorId != c.SolicitanteId)))
            .Select(c => c.InstituicaoId).Distinct()
            .Select(id => new ItemAgendado("atraso", id, id)).ToListAsync();
    }

    public async Task Executar(EsclariDb db, IReadOnlyList<ItemAgendado> itens, DateTime agora, CancellationToken parar)
    {
        var instituicao = await db.Instituicoes.SingleAsync(i => i.Id == itens[0].InstituicaoId, parar);
        var vencidos = await FiltroDeChamados.Atrasados(db, db.Chamados, prazos.Metas, agora);
        var ids = vencidos.Keys.ToList();
        var resumo = agora - Repeticao;
        var pendentes = (await db.Chamados.Where(c => ids.Contains(c.Id) && (c.AtrasoAvisadoEm == null || c.AtrasoAvisadoEm <= resumo)).ToListAsync(parar))
            .Select(c => new Avisado(c, vencidos[c.Id])).OrderBy(a => a.Vencido.VenceuEm).ThenBy(a => a.Chamado.Id).ToList();
        if (pendentes.Count == 0) return;
        var cartas = await AosSetores(db, instituicao, pendentes.Where(a => a.Chamado.AtrasoAvisadoEm is null).ToList(), agora, parar);
        cartas.AddRange((await Administradores(db, parar)).Select(email => Escrever(instituicao, email, pendentes, agora, false)));
        var avisados = cartas.SelectMany(c => c.Chamados).Select(a => a.Chamado).Distinct().ToList();
        if (avisados.Count < pendentes.Count)
            log.LogWarning("Aviso de atraso adiado: a instituição {InstituicaoId} não tem destinatário ativo com e-mail para {Chamados} chamados", instituicao.Id, pendentes.Count - avisados.Count);
        if (avisados.Count == 0) return;
        foreach (var c in avisados) c.MarcarAtrasoAvisado(agora);
        db.EmailsSaida.AddRange(cartas.Select(c => EmailSaida.Criar(c.Para, c.Assunto, c.Corpo, agora, new(c.Titulo, c.Acao))));
        try { await db.SaveChangesAsync(parar); }
        catch (DbUpdateConcurrencyException) { return; }
        log.LogInformation("Atrasos da instituição {InstituicaoId}: primeiraResposta={PrimeiraResposta} resolucao={Resolucao} emails={Emails}",
            instituicao.Id, Contar(vencidos, AtrasoDoChamado.PrimeiraResposta), Contar(vencidos, AtrasoDoChamado.Resolucao), cartas.Count);
    }

    static int Contar(Dictionary<long, AtrasoDoChamado.Vencido> vencidos, string tipo) => vencidos.Values.Count(v => v.Tipo == tipo);

    static Task<List<string>> Administradores(EsclariDb db, CancellationToken parar) =>
        db.Usuarios.Where(u => u.Ativo && u.Email != "" && u.Papeis.Any(p => p.Papel == Papeis.Administrador)).Select(u => u.Email).Distinct().ToListAsync(parar);

    async Task<List<Carta>> AosSetores(EsclariDb db, Instituicao instituicao, List<Avisado> novos, DateTime agora, CancellationToken parar)
    {
        if (novos.Count == 0) return [];
        var responsaveis = novos.Select(a => a.Chamado.ResponsavelId).OfType<long>().Distinct().ToList();
        var pessoas = (await db.Usuarios.Where(u => responsaveis.Contains(u.Id) && u.Ativo && u.Email != "").Select(u => new Pessoa(u.Id, u.Email)).ToListAsync(parar))
            .ToDictionary(p => p.Id, p => p.Email);
        var setores = novos.Where(a => a.Chamado.ResponsavelId is null).Select(a => a.Chamado.SetorId).Distinct().ToList();
        var equipes = (await db.AtendenteSetores.Where(a => setores.Contains(a.SetorId))
                .Join(db.Usuarios.Where(u => u.Ativo && u.Email != "" && u.Papeis.Any(p => p.Papel == Papeis.Atendente)), a => a.UsuarioId, u => u.Id, (a, u) => new DoSetor(a.SetorId, u.Email))
                .ToListAsync(parar)).ToLookup(d => d.SetorId, d => d.Email);
        IEnumerable<string> Para(Chamado c) =>
            c.ResponsavelId is { } r ? (pessoas.TryGetValue(r, out var email) ? [email] : []) : equipes[c.SetorId].Distinct();
        return novos.SelectMany(a => Para(a.Chamado).Select(email => (email, a))).GroupBy(x => x.email, x => x.a)
            .Select(g => Escrever(instituicao, g.Key, [.. g], agora, true)).ToList();
    }

    Carta Escrever(Instituicao i, string para, List<Avisado> chamados, DateTime agora, bool doSetor)
    {
        var quantos = chamados.Count == 1 ? "1 chamado" : $"{chamados.Count} chamados";
        var assunto = doSetor ? $"[{i.Nome}] {quantos} em atraso no seu atendimento" : $"[{i.Nome}] {quantos} em atraso";
        var corpo = new StringBuilder(doSetor
            ? "Estes chamados passaram da meta de atendimento e esperam a equipe.\n"
            : "Resumo diário: estes chamados estão fora da meta de atendimento.\n");
        corpo.Append($"Meta: primeira resposta em {prazos.PrimeiraRespostaHoras} h; resolução em {prazos.ResolucaoHoras} h.\n\n");
        foreach (var a in chamados)
            corpo.Append($"- {a.Chamado.Numero} · {a.Chamado.Titulo} · {Rotulo(a.Vencido.Tipo)} · atrasado há {Tempo(agora - a.Vencido.VenceuEm)}\n  {Avisos.Link(cfg, i, a.Chamado)}\n");
        var todos = $"{Tokens.UrlBase(cfg)}/{i.Slug}/{(doSetor ? "fila" : "chamados")}?atraso=true";
        corpo.Append($"\nVer todos os chamados em atraso:\n{todos}\n");
        return new Carta(para, assunto, corpo.ToString(), chamados, doSetor ? "Chamados em atraso" : "Resumo de chamados em atraso", new("Ver chamados em atraso", todos));
    }

    static string Rotulo(string tipo) => tipo == AtrasoDoChamado.PrimeiraResposta ? "sem primeira resposta" : "sem resolução";

    static string Tempo(TimeSpan t) => t.TotalHours < 1 ? "menos de 1 h" : t.TotalHours < 48 ? $"{(int)t.TotalHours} h" : $"{(int)t.TotalDays} dias";
}
