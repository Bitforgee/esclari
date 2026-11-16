using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Conhecimento.Documentos;

public static class DocumentoComVersoes
{
    public static VersaoDocumento Versao(long instituicaoId, LeitorDePdf.Pdf pdf, int numero, DateTime agora) =>
        VersaoDocumento.Criar(new(pdf.Nome, Armazenamento.NovaChave(instituicaoId, "documentos"), Armazenamento.Hash(pdf.Conteudo), pdf.Conteudo.Length), pdf.Texto, pdf.Paginas, numero, agora);

    public static void Trechos(EsclariDb db, VersaoDocumento v, DateTime agora) =>
        db.TrechosFonte.AddRange(DivisorDeTexto.Dividir(v.Texto).Select((t, i) => TrechoFonte.DoDocumento(v, i, t, agora)));

    public static Task<Documento?> Ler(EsclariDb db, long id) =>
        db.Documentos.ComUltimaVersao().FirstOrDefaultAsync(d => d.Id == id);
}
