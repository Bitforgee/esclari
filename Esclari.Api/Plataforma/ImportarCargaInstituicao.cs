using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Instituicoes.Carga;

namespace Esclari.Api.Plataforma;

public sealed record ImportarCargaInstituicao(string Slug, IFormCollection Form, OperadorPlataforma Operador) : IRequest<IResult>
{
    public sealed record Resposta(int Setores, int Cursos, int Categorias, int Regras, int Artigos);

    public const long Limite = 2 * 1024 * 1024;

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/instituicoes/{slug}/carga", async (string slug, HttpContext ctx, IMediator m) =>
            await m.Send(new ImportarCargaInstituicao(slug, ctx.Request.HasFormContentType ? await ctx.Request.ReadFormAsync() : FormCollection.Empty, ctx.Operador())));
}

public sealed class ImportarCargaInstituicaoHandler(EsclariDb db, TimeProvider relogio, Prazos prazos) : IRequestHandler<ImportarCargaInstituicao, IResult>
{
    static IResult Recusa(IEnumerable<string> erros) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["arquivo"] = [.. erros] }, detail: "O arquivo de carga tem erros.", title: Problemas.Titulo(400));

    public async ValueTask<IResult> Handle(ImportarCargaInstituicao m, CancellationToken ct)
    {
        var instituicao = await db.Instituicoes.FirstOrDefaultAsync(i => i.Slug == m.Slug, ct);
        if (instituicao is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        if (m.Form.Files["arquivo"] is not { } arquivo) return Problemas.Invalido("Envie o arquivo JSON no campo arquivo.", "arquivo");
        if (arquivo.Length > ImportarCargaInstituicao.Limite) return Problemas.Erro(413, "O arquivo passa de 2 MB.");

        ArquivoCarga? arq;
        try
        {
            await using var corpo = arquivo.OpenReadStream();
            arq = await JsonSerializer.DeserializeAsync<ArquivoCarga>(corpo, JsonSerializerOptions.Web, ct);
        }
        catch (JsonException e) { return Recusa([$"JSON inválido ({e.Message})"]); }
        if (arq is null) return Recusa(["O arquivo está vazio."]);

        db.InstituicaoAtual = instituicao.Id;
        var existentes = await CargaInicial.Existentes(db);
        var (erros, plano) = CargaValidacao.Validar(arq, existentes);
        if (erros.Count > 0) return Recusa(erros);

        var agora = relogio.GetUtcNow().UtcDateTime;
        var auditoria = AuditoriaPlataforma.Registrar(AcoesDaPlataforma.CargaImportada, m.Operador.Id, instituicao.Id, $"Importou a carga de {instituicao.Slug}", agora);
        var (setores, categorias) = await CargaInicial.Gravar(db, arq, existentes, plano, agora, instituicao.Fuso, prazos.ValidadeConhecimentoMeses, auditoria);
        return Results.Ok(new ImportarCargaInstituicao.Resposta(setores, plano.Cursos.Count, categorias, plano.Regras.Count, plano.Artigos.Count));
    }
}
