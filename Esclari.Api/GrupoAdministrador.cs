using Esclari.Api.Dados;

namespace Esclari.Api;

public static class GrupoAdministrador
{
    public static RouteGroupBuilder MapAdmin(this RouteGroupBuilder api, params string[] funcoes) => api.MapGroup("/admin").ExigirFuncao(funcoes);

    public static RouteGroupBuilder ExigirFuncao(this RouteGroupBuilder g, params string[] funcoes) =>
        g.AddEndpointFilter(async (c, next) =>
        {
            var usuario = c.HttpContext.Usuario();
            return funcoes.Any(usuario.Pode) ? await next(c) : Problemas.Erro(403, "Seu tipo de usuário não tem acesso a esta área.");
        });
}
