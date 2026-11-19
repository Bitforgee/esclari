using System.Text.Json.Serialization;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Instituicoes.Carga;

public sealed record ManifestoInstituicao(string Slug) : IRequest<IResult>
{
    public const string CorPadrao = "#f7f5ee";
    public const string FundoDoApp = "#F7F5EE";
    public const string IconeMaskable = "/icons/icon-maskable-512.png";

    public sealed record IconeGravado(int Versao, string Conteudo);

    public sealed record Icone([property: JsonPropertyName("src")] string Src, [property: JsonPropertyName("sizes")] string Sizes, [property: JsonPropertyName("type")] string Type, [property: JsonPropertyName("purpose")] string Purpose);

    public sealed record Resposta(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("short_name")] string ShortName,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("start_url")] string StartUrl,
        [property: JsonPropertyName("scope")] string Scope,
        [property: JsonPropertyName("display")] string Display,
        [property: JsonPropertyName("theme_color")] string ThemeColor,
        [property: JsonPropertyName("background_color")] string BackgroundColor,
        [property: JsonPropertyName("icons")] IReadOnlyList<Icone> Icons);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapGet("/instituicoes/{slug}/manifest.webmanifest", (string slug, IMediator m) => m.Send(new ManifestoInstituicao(slug))).AllowAnonymous();

    public static string Iniciais(string nome)
    {
        var partes = nome.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return partes.Length == 0 ? "" : (partes[0][..1] + (partes.Length > 1 ? partes[^1][..1] : "")).ToUpperInvariant();
    }

    public static IReadOnlyList<Icone> Icones(string slug, int? versaoDoIcone, string tipoDoIcone)
    {
        if (versaoDoIcone is not { } v)
            return [new("/icons/icon-192.png", "192x192", "image/png", "any"), new("/icons/icon-512.png", "512x512", "image/png", "any"), new(IconeMaskable, "512x512", "image/png", "maskable")];
        var src = $"/api/instituicoes/{slug}/imagens/icone?v={v}";
        return [new(src, "192x192", tipoDoIcone, "any"), new(src, "512x512", tipoDoIcone, "any")];
    }
}

public sealed class ManifestoInstituicaoHandler(EsclariDb db) : IRequestHandler<ManifestoInstituicao, IResult>
{
    public async ValueTask<IResult> Handle(ManifestoInstituicao m, CancellationToken ct)
    {
        var i = await db.Instituicoes.FirstOrDefaultAsync(x => x.Slug == m.Slug && x.Ativa, ct);
        if (i is null) return Problemas.Erro(404, "Instituição não encontrada.");
        db.InstituicaoAtual = i.Id;
        var icone = await db.ImagensInstituicao.Where(x => x.Tipo == TiposDeImagem.Icone && x.RemovidaEm == null).Select(x => new ManifestoInstituicao.IconeGravado(x.Versao, x.Conteudo)).FirstOrDefaultAsync(ct);
        var resposta = new ManifestoInstituicao.Resposta(i.Nome, i.NomeCurto ?? ManifestoInstituicao.Iniciais(i.Nome), $"/{i.Slug}/", $"/{i.Slug}/", $"/{i.Slug}/", "standalone",
            i.Cor ?? ManifestoInstituicao.CorPadrao, ManifestoInstituicao.FundoDoApp, ManifestoInstituicao.Icones(i.Slug, icone?.Versao, icone?.Conteudo ?? Arquivos.Png));
        return Results.Json(resposta, contentType: "application/manifest+json");
    }
}
