using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Chamados.Permissao;
using Esclari.Api.Dados;
using static Esclari.Api.Chamados.Permissao.AcoesChamado;

namespace Esclari.Api.Chamados.Acoes;

public sealed record MarcarDuplicado(string Numero, MarcarDuplicado.Entrada Corpo, Usuario Usuario, Instituicao Instituicao) : IAcaoChamado
{
    public sealed record Entrada(string? NumeroOriginal);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/chamados/{numero}/duplicado", (string numero, MarcarDuplicado.Entrada i, HttpContext ctx, IMediator m) => m.Send(new MarcarDuplicado(numero, i, ctx.Usuario(), ctx.Instituicao())));

    public string Acao => "duplicado";

    public async ValueTask<IResult?> Validar(AcessoChamado a, EsclariDb db) =>
        Corpo.NumeroOriginal != a.Chamado.Numero && await db.Chamados.AnyAsync(c => c.Numero == Corpo.NumeroOriginal && c.SolicitanteId == a.Chamado.SolicitanteId)
            ? null : Problemas.Invalido("Informe outro chamado do mesmo aluno.", "numeroOriginal");
}

public sealed class MarcarDuplicadoHandler(EsclariDb db, AcessoChamado a, IConfiguration cfg) : IRequestHandler<MarcarDuplicado, IResult>
{
    public async ValueTask<IResult> Handle(MarcarDuplicado m, CancellationToken ct)
    {
        a.Chamado.MarcarDuplicado(m.Usuario.Id, m.Corpo.NumeroOriginal!, a.Agora);
        await Avisos.NovaResposta(db, cfg, m.Instituicao, a.Chamado, a.Agora);
        return Results.Empty;
    }
}
