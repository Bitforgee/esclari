using Mediator;
using Esclari.Api.Chamados.Permissao;
using Esclari.Api.Dados;
using static Esclari.Api.Chamados.Permissao.AcoesChamado;

namespace Esclari.Api.Chamados.Acoes;

public sealed record ConfirmarChamado(string Numero, Usuario Usuario, Instituicao Instituicao) : IAcaoChamado
{
    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/chamados/{numero}/confirmar", (string numero, HttpContext ctx, IMediator m) => m.Send(new ConfirmarChamado(numero, ctx.Usuario(), ctx.Instituicao())));

    public string Acao => "confirmar";

    public ValueTask<IResult?> Validar(AcessoChamado a, EsclariDb db) => ValueTask.FromResult<IResult?>(null);
}

public sealed class ConfirmarChamadoHandler(AcessoChamado a) : IRequestHandler<ConfirmarChamado, IResult>
{
    public ValueTask<IResult> Handle(ConfirmarChamado m, CancellationToken ct)
    {
        a.Chamado.Confirmar(m.Usuario.Id, a.Agora);
        return ValueTask.FromResult(Results.Empty);
    }
}
