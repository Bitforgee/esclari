using Mediator;
using Esclari.Api.Dados;

namespace Esclari.Api.Indicadores;

public sealed record ConsultarIndicadores(DateOnly? De, DateOnly? Ate, long? SetorId, Usuario Usuario, Instituicao Instituicao) : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) =>
        g.MapGet("/indicadores", (DateOnly? de, DateOnly? ate, long? setorId, HttpContext ctx, IMediator m) =>
            m.Send(new ConsultarIndicadores(de, ate, setorId, ctx.Usuario(), ctx.Instituicao())));
}

public sealed class ConsultarIndicadoresHandler(EsclariDb db) : IRequestHandler<ConsultarIndicadores, IResult>
{
    public async ValueTask<IResult> Handle(ConsultarIndicadores m, CancellationToken ct)
    {
        var leitor = new LeitorIndicadores(db);
        var (pedido, recusa) = await leitor.Validar(m.Usuario, m.De, m.Ate, m.SetorId);
        if (recusa is not null) return recusa;

        var fuso = TimeZoneInfo.FindSystemTimeZoneById(m.Instituicao.Fuso);
        var massa = await leitor.Ler(pedido!.Setores, CalculoIndicadores.Periodo(pedido.De, pedido.Ate, fuso));
        return Results.Ok(CalculoIndicadores.Calcular(massa, pedido.De, pedido.Ate, fuso, m.SetorId));
    }
}