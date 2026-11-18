using Mediator;
using Esclari.Api.Chamados.Permissao;
using Esclari.Api.Dados;
using static Esclari.Api.Chamados.Permissao.AcoesChamado;

namespace Esclari.Api.Chamados.Acoes;

public sealed record RejeitarChamado(string Numero, RejeitarChamado.Entrada Corpo, Usuario Usuario, Instituicao Instituicao) : IAcaoChamado
{
    public sealed record Entrada(string? Motivo);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/chamados/{numero}/rejeitar", (string numero, RejeitarChamado.Entrada i, HttpContext ctx, IMediator m) => m.Send(new RejeitarChamado(numero, i, ctx.Usuario(), ctx.Instituicao())));

    public string Acao => "rejeitar";

    public ValueTask<IResult?> Validar(AcessoChamado a, EsclariDb db) =>
        ValueTask.FromResult(Texto(Corpo.Motivo, "motivo", "Explique o motivo da rejeição, com até 4000 caracteres."));
}

public sealed class RejeitarChamadoHandler(EsclariDb db, AcessoChamado a, PrazosAtuais atuais) : IRequestHandler<RejeitarChamado, IResult>
{
    public async ValueTask<IResult> Handle(RejeitarChamado m, CancellationToken ct) => await RejeitarResolucao(db, a, m.Usuario, await atuais.Ler(), m.Corpo.Motivo!);
}
