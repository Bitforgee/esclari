using Mediator;

namespace Esclari.Api.Plataforma;

public sealed record ListarModelosDeCarga : IRequest<IResult>
{
    public sealed record Item(string Id, string Nome, string Descricao, int Setores, int Categorias, int Regras);

    public static void Mapear(RouteGroupBuilder g) => g.MapGet("/modelos-de-carga", (IMediator m) => m.Send(new ListarModelosDeCarga()));
}

public sealed class ListarModelosDeCargaHandler : IRequestHandler<ListarModelosDeCarga, IResult>
{
    public ValueTask<IResult> Handle(ListarModelosDeCarga m, CancellationToken ct) =>
        ValueTask.FromResult(Results.Ok(ModelosDeCarga.Todos.Select(x =>
            new ListarModelosDeCarga.Item(x.Id, x.Nome, x.Descricao, x.Arquivo.Setores!.Count, x.Arquivo.Categorias!.Count, x.Arquivo.Regras!.Count))));
}
