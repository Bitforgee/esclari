using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record ImportarCargaInstituicao(string Slug, IFormCollection Form, bool Simular, OperadorPlataforma Operador) : IRequest<IResult>
{
    public sealed record Resposta(int Setores, int Cursos, int Categorias, int Regras, int Artigos);

    public const long Limite = 2 * 1024 * 1024;

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/instituicoes/{slug}/carga", async (string slug, bool? simular, HttpContext ctx, IMediator m) =>
            await m.Send(new ImportarCargaInstituicao(slug, ctx.Request.HasFormContentType ? await ctx.Request.ReadFormAsync() : FormCollection.Empty, simular ?? false, ctx.Operador())));
}

public sealed class ImportarCargaInstituicaoHandler(EsclariDb db, TimeProvider relogio, Prazos prazos) : IRequestHandler<ImportarCargaInstituicao, IResult>
{
    public async ValueTask<IResult> Handle(ImportarCargaInstituicao m, CancellationToken ct)
    {
        var instituicao = await db.Instituicoes.FirstOrDefaultAsync(i => i.Slug == m.Slug, ct);
        if (instituicao is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        if (m.Form.Files["arquivo"] is not { } arquivo) return Problemas.Invalido("Envie o arquivo JSON no campo arquivo.", "arquivo");
        if (arquivo.Length > ImportarCargaInstituicao.Limite) return Problemas.Erro(413, "O arquivo passa de 2 MB.");

        using var memoria = new MemoryStream();
        await using (var corpo = arquivo.OpenReadStream()) await corpo.CopyToAsync(memoria, ct);
        var (arq, recusa) = CargaDoOperador.Ler(memoria.ToArray());
        if (recusa is not null) return recusa;
        var (c, erro) = await CargaDoOperador.Aplicar(db, instituicao, arq!, m.Simular, relogio.GetUtcNow().UtcDateTime, prazos.ValidadeConhecimentoMeses, m.Operador);
        return erro ?? Results.Ok(new ImportarCargaInstituicao.Resposta(c!.Setores, c.Cursos, c.Categorias, c.Regras, c.Artigos));
    }
}
