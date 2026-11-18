using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record DesativarInstituicao(string Slug, DesativarInstituicao.Entrada Corpo, OperadorPlataforma Operador) : IRequest<IResult>
{
    public sealed record Entrada(string? Motivo);
    public sealed record Resposta(string Slug, bool Ativa);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/instituicoes/{slug}/desativar", (string slug, Entrada i, HttpContext ctx, IMediator m) => m.Send(new DesativarInstituicao(slug, i, ctx.Operador())));
}

public sealed class DesativarInstituicaoHandler(EsclariDb db, TimeProvider relogio) : IRequestHandler<DesativarInstituicao, IResult>
{
    public async ValueTask<IResult> Handle(DesativarInstituicao m, CancellationToken ct)
    {
        var instituicao = await db.Instituicoes.FirstOrDefaultAsync(i => i.Slug == m.Slug, ct);
        if (instituicao is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        var motivo = m.Corpo.Motivo?.Trim() ?? "";
        if (motivo.Length is < 3 or > 200) return Problemas.Invalido("Informe o motivo, de 3 a 200 caracteres.", "motivo");
        instituicao.Desativar();
        db.AuditoriaDaPlataforma.Add(AuditoriaPlataforma.Registrar(AcoesDaPlataforma.InstituicaoDesativada, m.Operador.Id, instituicao.Id, $"Desativou {instituicao.Slug}: {motivo}", relogio.GetUtcNow().UtcDateTime));
        await db.SaveChangesAsync(ct);
        return Results.Ok(new DesativarInstituicao.Resposta(instituicao.Slug, instituicao.Ativa));
    }
}
