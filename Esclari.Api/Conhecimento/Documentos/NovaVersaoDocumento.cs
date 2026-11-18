using Mediator;
using Esclari.Api.Dados;
using static Esclari.Api.Conhecimento.Documentos.DocumentoComVersoes;

namespace Esclari.Api.Conhecimento.Documentos;

public sealed record NovaVersaoDocumento(long Id, IFormCollection Form) : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/documentos/{id:long}/versoes", async (long id, HttpRequest req, IMediator m) =>
            await m.Send(new NovaVersaoDocumento(id, req.HasFormContentType ? await req.ReadFormAsync() : FormCollection.Empty)));
}

public sealed class NovaVersaoDocumentoHandler(EsclariDb db, TimeProvider relogio, Armazenamento armazenamento, PrazosAtuais atuais) : IRequestHandler<NovaVersaoDocumento, IResult>
{
    public async ValueTask<IResult> Handle(NovaVersaoDocumento m, CancellationToken ct)
    {
        var prazos = await atuais.Ler();
        var d = await Ler(db, m.Id);
        if (d is null) return Problemas.Erro(404, "Documento não encontrado.");
        var (pdf, erro) = await LeitorDePdf.Ler(m.Form.Files["arquivo"]);
        if (erro is not null) return erro;

        var agora = relogio.GetUtcNow().UtcDateTime;
        var validade = await db.NovaValidade(relogio, prazos);
        var versao = Versao(db.InstituicaoAtual!.Value, pdf!, d.UltimaVersao.Numero + 1, agora);
        if (d.NovaVersao(versao, validade) is { } recusa) return Problemas.Erro(409, recusa.Motivo);
        await armazenamento.Gravar(versao.Chave, pdf!.Conteudo);
        Trechos(db, versao, agora);
        await db.SaveChangesAsync();
        return Results.Created($"/api/admin/documentos/{d.Id}", await DocumentoResposta.De(db, await Ler(db, m.Id) ?? d, validade.Hoje));
    }
}
