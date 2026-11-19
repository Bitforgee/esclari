using Esclari.Api.Dados;

namespace Esclari.Api;

public static class GrupoAdministrador
{
    public static RouteGroupBuilder MapAdmin(this RouteGroupBuilder api, string funcao) => api.MapGroup("/admin").ExigirFuncao(funcao);

    public static RouteGroupBuilder ExigirFuncao(this RouteGroupBuilder g, string funcao) =>
        g.AddEndpointFilter(async (c, next) =>
            c.HttpContext.Usuario().Pode(funcao) ? await next(c) : Problemas.Erro(403, "Seu tipo de usuário não tem acesso a esta área."));
}
