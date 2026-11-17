using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Conhecimento;
using Esclari.Api.Dados;

namespace Esclari.Api.Indicadores;

public sealed record ConsultarValidade : IRequest<IResult>
{
    public sealed record Contagem(int Vigentes, int VenceEm30Dias, int Vencidos);

    public sealed record Resposta(Contagem Artigos, Contagem Documentos);

    public static void Mapear(RouteGroupBuilder api) =>
        api.MapGroup("/conhecimento").SoAdministrador().MapGet("/validade", (IMediator m) => m.Send(new ConsultarValidade()));
}

public sealed class ConsultarValidadeHandler(EsclariDb db, TimeProvider relogio) : IRequestHandler<ConsultarValidade, IResult>
{
    public async ValueTask<IResult> Handle(ConsultarValidade m, CancellationToken ct)
    {
        var hoje = await db.Hoje(relogio);
        var artigos = await db.Artigos.Where(a => a.Estado == EstadosArtigo.Ativo).Select(a => a.ValidoAte).ToListAsync(ct);
        var documentos = await db.Documentos.Where(d => d.Estado == EstadosDocumento.Ativo).Select(d => d.ValidoAte).ToListAsync(ct);
        return Results.Ok(new ConsultarValidade.Resposta(Contar(artigos, hoje), Contar(documentos, hoje)));
    }

    static ConsultarValidade.Contagem Contar(List<DateOnly> datas, DateOnly hoje)
    {
        var situacoes = datas.Select(d => Validade.Situacao(d, hoje)).ToList();
        return new(situacoes.Count(s => s == Validade.Vigente), situacoes.Count(s => s == Validade.VenceEmBreve), situacoes.Count(s => s == Validade.Vencido));
    }
}
