using Mediator;
using Esclari.Api.Dados;

namespace Esclari.Api.Conhecimento.Artigos;

public sealed record RenovarArtigo(long Id) : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/artigos/{id:long}/renovar", (long id, IMediator m) => m.Send(new RenovarArtigo(id)));
}

public sealed class RenovarArtigoHandler(EsclariDb db, TimeProvider relogio, PrazosAtuais atuais) : IRequestHandler<RenovarArtigo, IResult>
{
    public async ValueTask<IResult> Handle(RenovarArtigo m, CancellationToken ct)
    {
        var prazos = await atuais.Ler();
        var a = await ArtigoComVersoes.Ler(db, m.Id);
        if (a is null) return Problemas.Erro(404, "Artigo não encontrado.");
        var validade = await db.NovaValidade(relogio, prazos);
        if (a.Renovar(validade) is { } recusa) return Problemas.Erro(409, recusa.Motivo);
        await db.SaveChangesAsync();
        return Results.Ok(await ArtigoResposta.De(db, a, validade.Hoje));
    }
}
