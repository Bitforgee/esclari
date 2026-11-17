using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Esclari.Api.Dados;

namespace Esclari.Api.Conhecimento;

public sealed class AvisarValidade(IConfiguration cfg, ILogger<Agendador> log) : ITarefaAgendada
{
    public string Tipo => "validade";

    sealed record Candidato(string Tipo, long Id, string Titulo, DateOnly ValidoAte, string Marco);

    sealed record Lido(long Id, string Titulo, DateOnly ValidoAte);

    sealed record Registrado(string Tipo, long ItemId, DateOnly ValidoAte, string Marco);

    static readonly (string Marco, string Titulo, string Filtro)[] Grupos =
    [
        (AvisoValidade.Vencido, "Vencidos (o assistente já não cita estes itens):", "vencidos"),
        (AvisoValidade.Em7Dias, "Vencem em até 7 dias:", "em-30-dias"),
        (AvisoValidade.Em30Dias, "Vencem em até 30 dias:", "em-30-dias"),
    ];

    public async Task<List<ItemAgendado>> Ler(EsclariDb db, DateTime agora)
    {
        var limite = DateOnly.FromDateTime(agora).AddDays(Validade.DiasEmBreve + 1);
        var artigos = db.Artigos.IgnoreQueryFilters().Where(a => a.Estado == EstadosArtigo.Ativo && a.ValidoAte <= limite).Select(a => a.InstituicaoId);
        var documentos = db.Documentos.IgnoreQueryFilters().Where(d => d.Estado == EstadosDocumento.Ativo && d.ValidoAte <= limite).Select(d => d.InstituicaoId);
        var ids = await artigos.Union(documentos).Distinct().ToListAsync();
        return ids.Select(id => new ItemAgendado(Tipo, id, id)).ToList();
    }

    public async Task Executar(EsclariDb db, IReadOnlyList<ItemAgendado> itens, DateTime agora, CancellationToken parar)
    {
        var instituicao = await db.Instituicoes.SingleAsync(i => i.Id == itens[0].InstituicaoId, parar);
        var novos = await Novos(db, Validade.HojeNoFuso(instituicao.Fuso, agora), parar);
        if (novos.Count == 0) return;
        var admins = await db.Usuarios.Where(u => u.Ativo && u.Email != "" && u.Papeis.Any(p => p.Papel == Papeis.Administrador)).Select(u => u.Email).ToListAsync(parar);
        if (admins.Count == 0)
        {
            log.LogWarning("Aviso de validade adiado: a instituição {InstituicaoId} não tem administrador ativo com e-mail", instituicao.Id);
            return;
        }
        db.AvisosValidade.AddRange(novos.Select(c => AvisoValidade.Criar(c.Tipo, c.Id, c.ValidoAte, c.Marco, agora)));
        var (assunto, corpo) = Email(instituicao, novos);
        db.EmailsSaida.AddRange(admins.Select(email => EmailSaida.Criar(email, assunto, corpo, agora)));
        try { await db.SaveChangesAsync(parar); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { return; }
        log.LogInformation("Avisos de validade da instituição {InstituicaoId}: vencidos={Vencidos} em7Dias={Em7Dias} em30Dias={Em30Dias}",
            instituicao.Id, Contar(novos, AvisoValidade.Vencido), Contar(novos, AvisoValidade.Em7Dias), Contar(novos, AvisoValidade.Em30Dias));
    }

    static int Contar(List<Candidato> novos, string marco) => novos.Count(c => c.Marco == marco);

    static async Task<List<Candidato>> Novos(EsclariDb db, DateOnly hoje, CancellationToken parar)
    {
        var limite = hoje.AddDays(Validade.DiasEmBreve);
        var artigos = await db.VersoesAtuais().Where(v => v.Artigo!.Estado == EstadosArtigo.Ativo && v.Artigo.ValidoAte <= limite)
            .Select(v => new Lido(v.ArtigoId, v.Titulo, v.Artigo!.ValidoAte)).ToListAsync(parar);
        var documentos = await db.Documentos.Where(d => d.Estado == EstadosDocumento.Ativo && d.ValidoAte <= limite)
            .Select(d => new Lido(d.Id, d.Titulo, d.ValidoAte)).ToListAsync(parar);
        var candidatos = artigos.Select(a => new Candidato(AvisoValidade.Artigo, a.Id, a.Titulo, a.ValidoAte, AvisoValidade.MarcoPara(a.ValidoAte, hoje)!))
            .Concat(documentos.Select(d => new Candidato(AvisoValidade.Documento, d.Id, d.Titulo, d.ValidoAte, AvisoValidade.MarcoPara(d.ValidoAte, hoje)!)))
            .ToList();
        var ids = candidatos.Select(c => c.Id).ToList();
        var registrados = (await db.AvisosValidade.Where(a => ids.Contains(a.ItemId)).Select(a => new Registrado(a.Tipo, a.ItemId, a.ValidoAte, a.Marco)).ToListAsync(parar))
            .ToLookup(a => (a.Tipo, a.ItemId, a.ValidoAte), a => a.Marco);
        return candidatos.Where(c => !AvisoValidade.JaAvisado(c.Marco, registrados[(c.Tipo, c.Id, c.ValidoAte)])).ToList();
    }

    (string, string) Email(Instituicao instituicao, List<Candidato> novos)
    {
        var assunto = $"[{instituicao.Nome}] Base de conhecimento: {novos.Count} {(novos.Count == 1 ? "item" : "itens")} para renovar";
        var corpo = new StringBuilder("Estes itens da base de conhecimento precisam de revisão. Renove o que continua certo e atualize o que mudou.\n");
        foreach (var (marco, titulo, filtro) in Grupos)
        {
            var doGrupo = novos.Where(c => c.Marco == marco).OrderBy(c => c.ValidoAte).ThenBy(c => c.Titulo).ToList();
            if (doGrupo.Count == 0) continue;
            corpo.Append($"\n{titulo}\n");
            foreach (var c in doGrupo) corpo.Append($"- {Nome(c.Tipo)}: {c.Titulo} ({(marco == AvisoValidade.Vencido ? "venceu em" : "vence em")} {c.ValidoAte.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)})\n");
            foreach (var tipo in doGrupo.Select(c => c.Tipo).Distinct().Order())
                corpo.Append($"Abrir: {Tokens.UrlBase(cfg)}/{instituicao.Slug}/{Lista(tipo)}?validade={filtro}\n");
        }
        return (assunto, corpo.ToString());
    }

    static string Nome(string tipo) => tipo == AvisoValidade.Artigo ? "Artigo" : "Documento";

    static string Lista(string tipo) => tipo == AvisoValidade.Artigo ? "artigos" : "documentos";
}
