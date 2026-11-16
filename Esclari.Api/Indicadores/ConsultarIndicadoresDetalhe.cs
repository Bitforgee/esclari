using Mediator;
using Esclari.Api.Dados;

namespace Esclari.Api.Indicadores;

public sealed record ConsultarIndicadoresDetalhe(DateOnly? De, DateOnly? Ate, long? SetorId, Usuario Usuario, Instituicao Instituicao) : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) =>
        g.MapGet("/indicadores/detalhe", (DateOnly? de, DateOnly? ate, long? setorId, HttpContext ctx, IMediator m) =>
            m.Send(new ConsultarIndicadoresDetalhe(de, ate, setorId, ctx.Usuario(), ctx.Instituicao())));
}

public sealed class ConsultarIndicadoresDetalheHandler(EsclariDb db, TimeProvider relogio) : IRequestHandler<ConsultarIndicadoresDetalhe, IResult>
{
    public async ValueTask<IResult> Handle(ConsultarIndicadoresDetalhe m, CancellationToken ct)
    {
        var leitor = new LeitorIndicadores(db);
        var (pedido, recusa) = await leitor.Validar(m.Usuario, m.De, m.Ate, m.SetorId);
        if (recusa is not null) return recusa;

        var fuso = TimeZoneInfo.FindSystemTimeZoneById(m.Instituicao.Fuso);
        var massa = await leitor.Ler(pedido!.Setores, CalculoIndicadores.Periodo(pedido.De, pedido.Ate, fuso));
        return Results.Ok(CalculoDetalhe.Calcular(massa, pedido.De, pedido.Ate, fuso, relogio.GetUtcNow().UtcDateTime));
    }
}
