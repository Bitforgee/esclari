using Microsoft.AspNetCore.Authentication;

namespace Esclari.Api.Plataforma;

public static class SairDaPlataforma
{
    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/auth/sair", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(SessaoDaPlataforma.Esquema);
            SessaoDaPlataforma.ApagarEtapa(ctx);
            return Results.NoContent();
        }).AllowAnonymous();
}

public static class EuDaPlataforma
{
    public sealed record Resposta(long Id, string Nome, string Email);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapGet("/eu", (HttpContext ctx) => Results.Ok(new Resposta(ctx.Operador().Id, ctx.Operador().Nome, ctx.Operador().Email)));
}
