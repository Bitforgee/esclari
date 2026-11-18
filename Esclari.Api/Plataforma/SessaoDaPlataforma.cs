using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed class ProtecaoDaPlataforma(IDataProtectionProvider provedor)
{
    public IDataProtector Etapa { get; } = provedor.CreateProtector("plataforma.etapa");
    public IDataProtector Segredo { get; } = provedor.CreateProtector("plataforma.totp");
}

public sealed record EtapaDeEntrada(long OperadorId, DateTimeOffset ExpiraEm, string? SegredoNovo);

public static class SessaoDaPlataforma
{
    public const string Esquema = "Plataforma";
    public const string CookieDaSessao = "esclari_plataforma";
    public const string CookieDaEtapa = "esclari_plataforma_etapa";
    public const string Caminho = "/api/plataforma";
    public const string SessaoEncerrada = "Sua sessão terminou. Entre de novo.";
    public static readonly TimeSpan SemUso = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan Maxima = TimeSpan.FromHours(8);
    public static readonly TimeSpan DuracaoDaEtapa = TimeSpan.FromMinutes(5);

    public static AuthenticationBuilder AddSessaoDaPlataforma(this AuthenticationBuilder b)
    {
        b.Services.AddOptions<CookieAuthenticationOptions>(Esquema).Configure<TimeProvider>((o, relogio) => o.TimeProvider = relogio);
        return b.AddCookie(Esquema, o =>
        {
            o.Cookie.Name = CookieDaSessao;
            o.Cookie.Path = Caminho;
            o.Cookie.HttpOnly = true;
            o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            o.Cookie.SameSite = SameSiteMode.Strict;
            o.ExpireTimeSpan = SemUso;
            o.SlidingExpiration = true;
            o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
            o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
            o.Events.OnValidatePrincipal = Validar;
        });
    }

    public static ClaimsPrincipal Principal(OperadorPlataforma operador, DateTimeOffset agora) =>
        new(new ClaimsIdentity([new Claim("sub", operador.Id.ToString(CultureInfo.InvariantCulture)), new Claim("inicio", agora.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))], Esquema));

    public static async Task Validar(CookieValidatePrincipalContext ctx)
    {
        var servicos = ctx.HttpContext.RequestServices;
        var principal = ctx.Principal!;
        var agora = servicos.GetRequiredService<TimeProvider>().GetUtcNow();
        var inicio = long.Parse(principal.FindFirstValue("inicio")!, CultureInfo.InvariantCulture);
        var id = long.Parse(principal.FindFirstValue("sub")!, CultureInfo.InvariantCulture);
        var operador = await servicos.GetRequiredService<EsclariDb>().OperadoresPlataforma.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (operador is not { Ativo: true } || agora.ToUnixTimeSeconds() - inicio > Maxima.TotalSeconds)
        {
            ctx.RejectPrincipal();
            await ctx.HttpContext.SignOutAsync(Esquema);
            return;
        }
        ctx.HttpContext.Items[typeof(OperadorPlataforma)] = operador;
        ctx.ShouldRenew = true;
    }

    public static OperadorPlataforma Operador(this HttpContext ctx) => (OperadorPlataforma)ctx.Items[typeof(OperadorPlataforma)]!;

    static CookieOptions OpcoesDaEtapa(HttpContext ctx) =>
        new() { HttpOnly = true, Path = Caminho, SameSite = SameSiteMode.Strict, Secure = ctx.Request.IsHttps, MaxAge = DuracaoDaEtapa, IsEssential = true };

    public static void GravarEtapa(HttpContext ctx, ProtecaoDaPlataforma protecao, EtapaDeEntrada etapa)
    {
        var texto = string.Join('|', etapa.OperadorId, etapa.ExpiraEm.ToUnixTimeSeconds(), etapa.SegredoNovo ?? "");
        ctx.Response.Cookies.Append(CookieDaEtapa, protecao.Etapa.Protect(texto), OpcoesDaEtapa(ctx));
    }

    public static EtapaDeEntrada? LerEtapa(HttpContext ctx, ProtecaoDaPlataforma protecao, DateTimeOffset agora)
    {
        if (!ctx.Request.Cookies.TryGetValue(CookieDaEtapa, out var valor)) return null;
        try
        {
            var partes = protecao.Etapa.Unprotect(valor).Split('|');
            var etapa = new EtapaDeEntrada(long.Parse(partes[0], CultureInfo.InvariantCulture), DateTimeOffset.FromUnixTimeSeconds(long.Parse(partes[1], CultureInfo.InvariantCulture)), partes[2] == "" ? null : partes[2]);
            return etapa.ExpiraEm > agora ? etapa : null;
        }
        catch (Exception e) when (e is System.Security.Cryptography.CryptographicException or FormatException or IndexOutOfRangeException) { return null; }
    }

    public static void ApagarEtapa(HttpContext ctx) => ctx.Response.Cookies.Delete(CookieDaEtapa, new CookieOptions { Path = Caminho });
}
