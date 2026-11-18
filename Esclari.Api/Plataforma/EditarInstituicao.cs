using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record EditarInstituicao(string Slug, EditarInstituicao.Entrada Corpo, OperadorPlataforma Operador) : IRequest<IResult>
{
    public sealed record Entrada(string? Nome, string? Fuso);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPut("/instituicoes/{slug}", (string slug, Entrada i, HttpContext ctx, IMediator m) => m.Send(new EditarInstituicao(slug, i, ctx.Operador())));
}

public sealed class EditarInstituicaoHandler(EsclariDb db, TimeProvider relogio, Prazos prazos) : IRequestHandler<EditarInstituicao, IResult>
{
    public async ValueTask<IResult> Handle(EditarInstituicao m, CancellationToken ct)
    {
        var instituicao = await db.Instituicoes.FirstOrDefaultAsync(i => i.Slug == m.Slug, ct);
        if (instituicao is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        var (nome, fuso) = (m.Corpo.Nome?.Trim() ?? "", m.Corpo.Fuso?.Trim() ?? "");
        if (nome.Length is < 3 or > 120) return Problemas.Invalido("O nome deve ter de 3 a 120 caracteres.", "nome");
        if (!(TimeZoneInfo.TryFindSystemTimeZoneById(fuso, out var tz) && tz.HasIanaId)) return Problemas.Invalido($"\"{fuso}\" não é um fuso IANA (ex.: America/Cuiaba).", "fuso");

        var agora = relogio.GetUtcNow().UtcDateTime;
        instituicao.AlterarIdentificacao(nome, fuso);
        db.AuditoriaDaPlataforma.Add(AuditoriaPlataforma.Registrar(AcoesDaPlataforma.InstituicaoAlterada, m.Operador.Id, instituicao.Id, $"Alterou nome e fuso de {instituicao.Slug}", agora));
        await db.SaveChangesAsync(ct);
        return Results.Ok((await ConsultaDasInstituicoes.Ler(db, agora, prazos, m.Slug)).Single());
    }
}
