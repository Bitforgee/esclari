using Mediator;
using Esclari.Api.Dados;
using static Esclari.Api.Conhecimento.Documentos.DocumentoComVersoes;

namespace Esclari.Api.Conhecimento.Documentos;

public sealed record RenovarDocumento(long Id) : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/documentos/{id:long}/renovar", (long id, IMediator m) => m.Send(new RenovarDocumento(id)));
}

public sealed class RenovarDocumentoHandler(EsclariDb db, TimeProvider relogio, PrazosAtuais atuais) : IRequestHandler<RenovarDocumento, IResult>
{
    public async ValueTask<IResult> Handle(RenovarDocumento m, CancellationToken ct)
    {
        var prazos = await atuais.Ler();
        var d = await Ler(db, m.Id);
        if (d is null) return Problemas.Erro(404, "Documento não encontrado.");
        var validade = await db.NovaValidade(relogio, prazos);
        if (d.Renovar(validade) is { } recusa) return Problemas.Erro(409, recusa.Motivo);
        await db.SaveChangesAsync();
        return Results.Ok(await DocumentoResposta.De(db, d, validade.Hoje));
    }
}
