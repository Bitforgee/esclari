using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record AlterarParametros(string Slug, AlterarParametros.Entrada Corpo, OperadorPlataforma Operador) : IRequest<IResult>
{
    public sealed record Entrada(
        int? AbandonoMinutos, int? ConfirmacaoDias, int? LembreteAguardandoDias, int? ResolverAguardandoDias,
        int? ValidadeConhecimentoMeses, int? PrimeiraRespostaHoras, int? ResolucaoHoras);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPut("/instituicoes/{slug}/parametros", (string slug, Entrada i, HttpContext ctx, IMediator m) => m.Send(new AlterarParametros(slug, i, ctx.Operador())));
}

public sealed class AlterarParametrosHandler(EsclariDb db, Prazos prazos, TimeProvider relogio) : IRequestHandler<AlterarParametros, IResult>
{
    public async ValueTask<IResult> Handle(AlterarParametros m, CancellationToken ct)
    {
        var instituicao = await db.Instituicoes.FirstOrDefaultAsync(i => i.Slug == m.Slug, ct);
        if (instituicao is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        var e = m.Corpo;
        var novo = new ParametrosDaInstituicao
        {
            AbandonoMinutos = e.AbandonoMinutos, ConfirmacaoDias = e.ConfirmacaoDias, LembreteAguardandoDias = e.LembreteAguardandoDias, ResolverAguardandoDias = e.ResolverAguardandoDias,
            ValidadeConhecimentoMeses = e.ValidadeConhecimentoMeses, PrimeiraRespostaHoras = e.PrimeiraRespostaHoras, ResolucaoHoras = e.ResolucaoHoras,
        };
        if (ParametrosDaPlataforma.Validar(novo, prazos) is { } invalido) return invalido;

        var resumo = $"Alterou os parâmetros de {instituicao.Slug}: {ParametrosDaPlataforma.Mudancas(instituicao.Parametros, novo)}";
        instituicao.DefinirParametros(novo);
        db.AuditoriaDaPlataforma.Add(AuditoriaPlataforma.Registrar(AcoesDaPlataforma.ParametrosAlterados, m.Operador.Id, instituicao.Id, resumo, relogio.GetUtcNow().UtcDateTime));
        await db.SaveChangesAsync(ct);
        return Results.Ok(ParametrosDaPlataforma.Resposta(instituicao, prazos));
    }
}
