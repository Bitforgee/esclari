using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Conhecimento.Documentos;

public sealed record BaixarVersaoDocumento(long Id, Usuario Usuario) : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) =>
        g.MapGet("/documentos/versoes/{id:long}", (long id, HttpContext ctx, IMediator m) => m.Send(new BaixarVersaoDocumento(id, ctx.Usuario())));
}

public sealed class BaixarVersaoDocumentoHandler(EsclariDb db, Armazenamento armazenamento) : IRequestHandler<BaixarVersaoDocumento, IResult>
{
    record PdfBaixado(string Chave, string NomeArquivo, long Tamanho);

    public async ValueTask<IResult> Handle(BaixarVersaoDocumento m, CancellationToken ct)
    {
        var u = m.Usuario;
        if (!u.TipoLibera(Funcoes.FilaAtender) && !u.TipoLibera(Funcoes.ConhecimentoGerenciar) || await db.SoLideranca(u)) return Problemas.Erro(404, "Documento não encontrado.");
        var v = await db.VersoesDocumento.Where(x => x.Id == m.Id).Select(x => new PdfBaixado(x.Chave, x.NomeArquivo, x.Tamanho)).FirstOrDefaultAsync();
        return v is null ? Problemas.Erro(404, "Documento não encontrado.")
            : new ResultadoComTamanho(Results.Stream(await armazenamento.Baixar(v.Chave), Arquivos.Pdf, v.NomeArquivo), v.Tamanho);
    }
}
