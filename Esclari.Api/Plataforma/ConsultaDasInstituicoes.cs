using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record Saude(
    DateTime? UltimaAtividadeEm, int ChamadosAbertos, int? ChamadoMaisAntigoAbertoDias, int Conversas30Dias, int Respostas30Dias,
    double? SemBaseTaxa30Dias, int AlunosAtivos, int EquipeAtiva, int EmailsComFalha, int ConteudoVencido);

public sealed record ItemDaInstituicao(
    string Slug, string Nome, string Fuso, bool Ativa, DateTime CriadaEm, int Administradores, string Situacao, Saude Saude, IReadOnlyList<string> Atencao);

public sealed class LinhaDeSaude
{
    public long InstituicaoId { get; set; }
    public DateTime? UltimaAtividadeEm { get; set; }
    public int ChamadosAbertos { get; set; }
    public DateTime? MaisAntigoAbertoEm { get; set; }
    public int Conversas30Dias { get; set; }
    public int Respostas30Dias { get; set; }
    public int SemBase30Dias { get; set; }
    public int AlunosAtivos { get; set; }
    public int EquipeAtiva { get; set; }
    public int EmailsComFalha { get; set; }
    public int ConteudoVencido { get; set; }
    public int AdministradoresAtivos { get; set; }
    public int AdministradoresComSenha { get; set; }
}

public static class ConsultaDasInstituicoes
{
    public const int DiasParaParada = 14;
    public const int RespostasParaTaxa = 20;

    public static async Task<List<ItemDaInstituicao>> Ler(EsclariDb db, DateTime agora, Prazos prazos, string? slug = null)
    {
        var desde = agora.AddDays(-30);
        var hoje = DateOnly.FromDateTime(agora);
        var linhas = (await db.Database.SqlQuery<LinhaDeSaude>($"""
            SELECT i.id AS "InstituicaoId",
              GREATEST((SELECT max(c.criada_em) FROM conversa c WHERE c.instituicao_id = i.id), (SELECT max(h.criado_em) FROM chamado h WHERE h.instituicao_id = i.id)) AS "UltimaAtividadeEm",
              (SELECT count(*) FROM chamado h WHERE h.instituicao_id = i.id AND h.estado IN ('ABERTO', 'EM_ATENDIMENTO', 'AGUARDANDO_ALUNO'))::int AS "ChamadosAbertos",
              (SELECT min(h.criado_em) FROM chamado h WHERE h.instituicao_id = i.id AND h.estado IN ('ABERTO', 'EM_ATENDIMENTO', 'AGUARDANDO_ALUNO')) AS "MaisAntigoAbertoEm",
              (SELECT count(*) FROM conversa c WHERE c.instituicao_id = i.id AND c.criada_em >= {desde})::int AS "Conversas30Dias",
              (SELECT count(*) FROM mensagem_conversa m WHERE m.instituicao_id = i.id AND m.autor = 'ASSISTENTE' AND m.criada_em >= {desde})::int AS "Respostas30Dias",
              (SELECT count(*) FROM mensagem_conversa m WHERE m.instituicao_id = i.id AND m.autor = 'ASSISTENTE' AND m.tipo_final = 'sem_base' AND m.criada_em >= {desde})::int AS "SemBase30Dias",
              (SELECT count(*) FROM usuario u WHERE u.instituicao_id = i.id AND u.ativo AND EXISTS (SELECT 1 FROM usuario_papel p WHERE p.usuario_id = u.id AND p.papel = 'ALUNO'))::int AS "AlunosAtivos",
              (SELECT count(*) FROM usuario u WHERE u.instituicao_id = i.id AND u.ativo AND EXISTS (SELECT 1 FROM usuario_papel p WHERE p.usuario_id = u.id AND p.papel IN ('ATENDENTE', 'ADMINISTRADOR')))::int AS "EquipeAtiva",
              (SELECT count(*) FROM email_saida e WHERE e.instituicao_id = i.id AND e.enviado_em IS NULL AND e.tentativas >= {Agendador.MaxTentativas})::int AS "EmailsComFalha",
              ((SELECT count(*) FROM artigo a WHERE a.instituicao_id = i.id AND a.estado = 'ATIVO' AND a.valido_ate < {hoje})
               + (SELECT count(*) FROM documento d WHERE d.instituicao_id = i.id AND d.estado = 'ATIVO' AND d.valido_ate < {hoje}))::int AS "ConteudoVencido",
              (SELECT count(*) FROM usuario u WHERE u.instituicao_id = i.id AND u.ativo AND EXISTS (SELECT 1 FROM usuario_papel p WHERE p.usuario_id = u.id AND p.papel = 'ADMINISTRADOR'))::int AS "AdministradoresAtivos",
              (SELECT count(*) FROM usuario u WHERE u.instituicao_id = i.id AND u.ativo AND u.senha_hash IS NOT NULL AND EXISTS (SELECT 1 FROM usuario_papel p WHERE p.usuario_id = u.id AND p.papel = 'ADMINISTRADOR'))::int AS "AdministradoresComSenha"
            FROM instituicao i
            """).ToListAsync()).ToDictionary(l => l.InstituicaoId);
        var instituicoes = await db.Instituicoes.AsNoTracking().Where(i => slug == null || i.Slug == slug).ToListAsync();
        return [.. instituicoes.Select(i => Item(i, linhas[i.Id], agora, prazos)).OrderBy(Grupo).ThenBy(x => x.Nome, StringComparer.CurrentCultureIgnoreCase)];
    }

    static int Grupo(ItemDaInstituicao i) => !i.Ativa ? 2 : i.Atencao.Count > 0 ? 0 : 1;

    static ItemDaInstituicao Item(Instituicao i, LinhaDeSaude l, DateTime agora, Prazos prazos)
    {
        var dias = l.MaisAntigoAbertoEm is { } antigo ? (int?)Math.Floor((agora - antigo).TotalDays) : null;
        var saude = new Saude(l.UltimaAtividadeEm, l.ChamadosAbertos, dias, l.Conversas30Dias, l.Respostas30Dias,
            l.Respostas30Dias == 0 ? null : (double)l.SemBase30Dias / l.Respostas30Dias, l.AlunosAtivos, l.EquipeAtiva, l.EmailsComFalha, l.ConteudoVencido);
        return new(i.Slug, i.Nome, i.Fuso, i.Ativa, i.CriadaEm, l.AdministradoresAtivos, Situacao(i, l), saude, i.Ativa ? Atencao(i, saude, agora, prazos) : []);
    }

    static string Situacao(Instituicao i, LinhaDeSaude l) =>
        !i.Ativa ? "DESATIVADA" : l.AdministradoresAtivos > 0 && l.AdministradoresComSenha == 0 ? "AGUARDANDO_PRIMEIRO_ACESSO" : "ATIVA";

    static List<string> Atencao(Instituicao i, Saude s, DateTime agora, Prazos prazos)
    {
        var limite = TimeSpan.FromDays(DiasParaParada);
        var atencao = new List<string>();
        if (s.UltimaAtividadeEm is { } ultima ? agora - ultima >= limite : agora - i.CriadaEm > limite) atencao.Add("PARADA");
        if (s.Respostas30Dias >= RespostasParaTaxa && s.SemBaseTaxa30Dias > 1.0 / 3) atencao.Add("MUITOS_SEM_BASE");
        if (s.EmailsComFalha > 0) atencao.Add("EMAILS_FALHANDO");
        if (s.ConteudoVencido > 0) atencao.Add("CONTEUDO_VENCIDO");
        if (s.ChamadoMaisAntigoAbertoDias > prazos.ResolucaoHoras / 24) atencao.Add("FILA_ANTIGA");
        return atencao;
    }
}
