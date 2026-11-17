using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Chamados;

public sealed record FiltroDeChamados(string? Escopo, string? Q, string[]? Estado, long? SetorId, long? CategoriaId, string? Responsavel, DateOnly? De, DateOnly? Ate, string? Ordem)
{
    public const string Fila = "fila";
    public const string Todos = "todos";
    public const int LimiteDoTexto = 100;

    static readonly string[] Estados = [Aberto, EmAtendimento, AguardandoAluno, Resolvido, Fechado];
    static readonly string[] DaFila = [Aberto, EmAtendimento, AguardandoAluno, Resolvido];
    static readonly string?[] Escopos = [null, Fila, Todos];
    static readonly string?[] Ordens = [null, "antigos", "recentes"];
    static readonly string?[] Responsaveis = [null, "eu", "ninguem"];

    public sealed record Pedido(IQueryable<Chamado> Consulta, Usuario Usuario, Instituicao Instituicao);

    public string Texto() => Q?.Trim() ?? "";

    public bool DeTodos() => Escopo == Todos;

    public IEnumerable<string> Usados()
    {
        var usados = new (string nome, bool usado)[]
        {
            ("escopo", Escopo is not null), ("q", Texto() != ""), ("estado", Estado is { Length: > 0 }), ("setorId", SetorId is not null), ("categoriaId", CategoriaId is not null),
            ("responsavel", Responsavel is not null), ("de", De is not null), ("ate", Ate is not null), ("ordem", Ordem is not null),
        };
        return usados.Where(u => u.usado).Select(u => u.nome);
    }

    IResult? Invalido()
    {
        var recusas = new (bool recusa, string campo, string detalhe)[]
        {
            (!Escopos.Contains(Escopo), "escopo", "O escopo deve ser fila ou todos."),
            ((Estado ?? []).Any(e => !Estados.Contains(e)), "estado", $"O estado deve ser um destes: {string.Join(", ", Estados)}."),
            (!Responsaveis.Contains(Responsavel), "responsavel", "O responsável deve ser eu ou ninguem."),
            (!Ordens.Contains(Ordem), "ordem", "A ordem deve ser antigos ou recentes."),
            (De > Ate, "de", "A data inicial deve ser anterior ou igual à final."),
            (Texto().Length > LimiteDoTexto, "q", $"A busca deve ter até {LimiteDoTexto} caracteres."),
        };
        return recusas.Where(r => r.recusa).Select(r => Problemas.Invalido(r.detalhe, r.campo)).FirstOrDefault();
    }

    public static async Task<Usuario> Quem(EsclariDb db, long usuarioId) =>
        await db.Usuarios.Include(u => u.Papeis).SingleAsync(u => u.Id == usuarioId);

    public static IResult? Recusa(Usuario u, bool deTodos)
    {
        if (!u.Tem(Papeis.Atendente) && !u.Tem(Papeis.Administrador)) return Problemas.Erro(403, "A lista de chamados é para a equipe.");
        return deTodos && !u.Tem(Papeis.Administrador) ? Problemas.Erro(403, "Só o administrador vê os chamados de todos os setores.") : null;
    }

    public static Task<List<long>> SetoresDe(EsclariDb db, Usuario u) =>
        db.AtendenteSetores.Where(a => a.UsuarioId == u.Id).Select(a => a.SetorId).ToListAsync();

    public async Task<(Pedido?, IResult?)> Consultar(EsclariDb db, long usuarioId)
    {
        var u = await Quem(db, usuarioId);
        var recusa = Recusa(u, DeTodos()) ?? Invalido();
        if (recusa is not null) return (null, recusa);
        var setores = DeTodos() ? null : await SetoresDe(db, u);
        if (setores is not null && SetorId is { } s && !setores.Contains(s)) return (null, Problemas.Erro(403, "Este setor não é seu."));
        var i = await db.Instituicoes.SingleAsync(x => x.Id == db.InstituicaoAtual);
        var fuso = TimeZoneInfo.FindSystemTimeZoneById(i.Fuso);
        var consulta = PorTexto(db, PorPeriodo(PorCampos(db.Chamados, setores, u.Id), fuso));
        return (new Pedido(Ordenar(consulta), u, i), null);
    }

    IQueryable<Chamado> PorCampos(IQueryable<Chamado> q, List<long>? setores, long usuarioId)
    {
        var estados = Estado is { Length: > 0 } ? Estado : DaFila;
        q = q.Where(c => estados.Contains(c.Estado));
        if (setores is not null) q = q.Where(c => setores.Contains(c.SetorId));
        if (SetorId is { } setor) q = q.Where(c => c.SetorId == setor);
        if (CategoriaId is { } categoria) q = q.Where(c => c.CategoriaId == categoria);
        if (Responsavel == "eu") return q.Where(c => c.ResponsavelId == usuarioId);
        return Responsavel == "ninguem" ? q.Where(c => c.ResponsavelId == null) : q;
    }

    IQueryable<Chamado> PorPeriodo(IQueryable<Chamado> q, TimeZoneInfo fuso)
    {
        if (De is { } de)
        {
            var inicio = Utc(de, fuso);
            q = q.Where(c => c.CriadoEm >= inicio);
        }
        if (Ate is not { } ate) return q;
        var fim = Utc(ate.AddDays(1), fuso);
        return q.Where(c => c.CriadoEm < fim);
    }

    static DateTime Utc(DateOnly dia, TimeZoneInfo fuso) => TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(TimeOnly.MinValue), fuso);

    IQueryable<Chamado> PorTexto(EsclariDb db, IQueryable<Chamado> q)
    {
        var texto = Texto();
        if (texto == "") return q;
        var padrao = "%" + texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        return q.Where(c => EF.Functions.ILike(EF.Functions.Unaccent(c.Numero), EF.Functions.Unaccent(padrao), "\\")
            || EF.Functions.ILike(EF.Functions.Unaccent(c.Titulo), EF.Functions.Unaccent(padrao), "\\")
            || db.Usuarios.Any(u => u.Id == c.SolicitanteId && EF.Functions.ILike(EF.Functions.Unaccent(u.Nome), EF.Functions.Unaccent(padrao), "\\")));
    }

    IQueryable<Chamado> Ordenar(IQueryable<Chamado> q) =>
        Ordem == "recentes" ? q.OrderByDescending(c => c.CriadoEm).ThenByDescending(c => c.Id) : q.OrderBy(c => c.CriadoEm).ThenBy(c => c.Id);
}
