using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using static Esclari.Api.Conhecimento.Documentos.DocumentoComVersoes;

namespace Esclari.Api.Conhecimento.Documentos;

public sealed record CriarDocumento(IFormCollection Form) : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/documentos", async (HttpRequest req, IMediator m) =>
            await m.Send(new CriarDocumento(req.HasFormContentType ? await req.ReadFormAsync() : FormCollection.Empty)));
}

public sealed class CriarDocumentoHandler(EsclariDb db, TimeProvider relogio, Armazenamento armazenamento, PrazosAtuais atuais) : IRequestHandler<CriarDocumento, IResult>
{
    public async ValueTask<IResult> Handle(CriarDocumento m, CancellationToken ct)
    {
        var prazos = await atuais.Ler();
        var form = m.Form;
        var titulo = form["titulo"].ToString();
        if (!TituloDeConhecimento.Valido(titulo)) return Problemas.Invalido(TituloDeConhecimento.Recusa, "titulo");
        if (!long.TryParse(form["categoriaId"], out var categoriaId) || !await db.Categorias.AnyAsync(c => c.Id == categoriaId && c.Ativa))
            return Problemas.Invalido("Escolha uma categoria ativa.", "categoriaId");
        var (pdf, erro) = await LeitorDePdf.Ler(form.Files["arquivo"]);
        if (erro is not null) return erro;

        var agora = relogio.GetUtcNow().UtcDateTime;
        var primeira = Versao(db.InstituicaoAtual!.Value, pdf!, 1, agora);
        await armazenamento.Gravar(primeira.Chave, pdf!.Conteudo);
        var validade = await db.NovaValidade(relogio, prazos);
        var d = Documento.Criar(titulo, categoriaId, primeira, validade);
        Trechos(db, d.Versoes.Single(), agora);
        db.Documentos.Add(d);
        await db.SaveChangesAsync();
        return Results.Created($"/api/admin/documentos/{d.Id}", await DocumentoResposta.De(db, d, validade.Hoje));
    }
}
