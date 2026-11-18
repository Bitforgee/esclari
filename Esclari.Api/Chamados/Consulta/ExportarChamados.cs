using System.Globalization;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Chamados.Consulta;

public sealed record ExportarChamados(FiltroDeChamados Filtro, long UsuarioId) : IRequest<IResult>
{
    public const int LimiteDeLinhas = 10_000;

    public static readonly string[] Colunas =
    [
        "Número", "Título", "Estado", "Setor", "Categoria", "Categoria original", "Responsável", "Curso", "Semestre", "Aberto em",
        "Primeira resposta em", "Resolvido em", "Fechado em", "Motivo de fechamento", "Reaberturas", "Encaminhamentos",
    ];

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapGet("/chamados/exportar", ([AsParameters] FiltroDeChamados filtro, HttpContext ctx, IMediator m) =>
            m.Send(new ExportarChamados(filtro, ctx.Usuario().Id))).RequireRateLimiting("exportar");
}

public sealed class ExportarChamadosHandler(EsclariDb db, TimeProvider relogio, PrazosAtuais atuais, ILogger<ExportarChamadosHandler> log) : IRequestHandler<ExportarChamados, IResult>
{
    static readonly Dictionary<string, string> RotuloDoEstado = new()
    {
        [Aberto] = "Aberto", [EmAtendimento] = "Em atendimento", [AguardandoAluno] = "Aguardando aluno", [Resolvido] = "Resolvido", [Fechado] = "Fechado",
    };

    static readonly Dictionary<string, string> RotuloDoMotivo = new()
    {
        [MotivosFechamento.Confirmado] = "Confirmado pelo aluno", [MotivosFechamento.Cancelado] = "Cancelado pelo aluno",
        [MotivosFechamento.Duplicado] = "Duplicado", [MotivosFechamento.FechadoAutomaticamente] = "Fechado automaticamente",
    };

    sealed record Linha(string Numero, string Titulo, string Estado, string Setor, string Categoria, string CategoriaOriginal, string? Responsavel, string Curso,
        int Semestre, DateTime CriadoEm, DateTime? PrimeiraResposta, DateTime? ResolvidoEm, DateTime? FechadoEm, string? Motivo, int Reaberturas, int Encaminhamentos);

    public async ValueTask<IResult> Handle(ExportarChamados m, CancellationToken ct)
    {
        var prazos = await atuais.Ler();
        var (pedido, recusa) = await m.Filtro.Consultar(db, m.UsuarioId, prazos, relogio.GetUtcNow().UtcDateTime);
        if (recusa is not null) return recusa;
        if (await db.SoLideranca(pedido!.Usuario)) return Problemas.Erro(403, "A exportação não está disponível para a liderança de turma.");
        if (await pedido.Consulta.CountAsync(ct) > ExportarChamados.LimiteDeLinhas)
            return Problemas.Erro(400, "A exportação passa de 10.000 chamados. Refine os filtros.");
        var linhas = await Ler(pedido.Consulta, ct);
        var fuso = TimeZoneInfo.FindSystemTimeZoneById(pedido.Instituicao.Fuso);
        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(relogio.GetUtcNow().UtcDateTime, fuso));
        log.LogInformation("Exportação de chamados: usuário {UsuarioId}, instituição {InstituicaoId}, {Linhas} linhas, filtros {Filtros}",
            pedido.Usuario.Id, pedido.Instituicao.Id, linhas.Count, string.Join(",", m.Filtro.Usados()));
        var corpo = CsvDeChamados.Arquivo([CsvDeChamados.Linha(ExportarChamados.Colunas), .. linhas.Select(l => CsvDeChamados.Linha(Celulas(l, fuso)))]);
        return new ArquivoCsv(corpo, $"chamados-{pedido.Instituicao.Slug}-{hoje:yyyy-MM-dd}.csv");
    }

    Task<List<Linha>> Ler(IQueryable<Chamado> consulta, CancellationToken ct) =>
        consulta.Select(c => new Linha(c.Numero, c.Titulo, c.Estado,
            db.Setores.Where(s => s.Id == c.SetorId).Select(s => s.Nome).Single(),
            db.Categorias.Where(x => x.Id == c.CategoriaId).Select(x => x.Nome).Single(),
            db.Categorias.Where(x => x.Id == c.CategoriaOriginalId).Select(x => x.Nome).Single(),
            db.Usuarios.Where(x => x.Id == c.ResponsavelId).Select(x => x.Nome).SingleOrDefault(),
            db.Matriculas.Where(x => x.Id == c.MatriculaId).Select(x => db.Cursos.Where(k => k.Id == x.CursoId).Select(k => k.Nome).Single()).Single(),
            c.Semestre, c.CriadoEm,
            db.MensagensChamado.Where(x => x.ChamadoId == c.Id && x.Tipo == TiposMensagem.Publica && x.AutorId != null && x.AutorId != c.SolicitanteId).Min(x => (DateTime?)x.CriadaEm),
            c.ResolvidoEm, c.FechadoEm, c.MotivoFechamento, c.Reaberturas, c.Encaminhamentos)).ToListAsync(ct);

    static string?[] Celulas(Linha l, TimeZoneInfo fuso) =>
    [
        l.Numero, l.Titulo, RotuloDoEstado.GetValueOrDefault(l.Estado, l.Estado), l.Setor, l.Categoria, l.CategoriaOriginal, l.Responsavel, l.Curso,
        l.Semestre.ToString(CultureInfo.InvariantCulture), Data(l.CriadoEm, fuso), Data(l.PrimeiraResposta, fuso), Data(l.ResolvidoEm, fuso), Data(l.FechadoEm, fuso),
        l.Motivo is null ? null : RotuloDoMotivo.GetValueOrDefault(l.Motivo, l.Motivo), l.Reaberturas.ToString(CultureInfo.InvariantCulture), l.Encaminhamentos.ToString(CultureInfo.InvariantCulture),
    ];

    static string? Data(DateTime? utc, TimeZoneInfo fuso) =>
        utc is { } d ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(d, DateTimeKind.Utc), fuso).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : null;

    sealed class ArquivoCsv(byte[] corpo, string nome) : IResult
    {
        public Task ExecuteAsync(HttpContext http)
        {
            http.Response.ContentType = "text/csv; charset=utf-8";
            http.Response.Headers.ContentDisposition = $"attachment; filename=\"{nome}\"";
            http.Response.ContentLength = corpo.Length;
            return http.Response.Body.WriteAsync(corpo).AsTask();
        }
    }
}
