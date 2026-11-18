using Esclari.Api.Dados;
using Esclari.Api.Instituicoes.Aparencia;
using Esclari.Api.Instituicoes.Carga;

namespace Esclari.Api.Instituicoes;

public static class InstituicoesExtensoes
{
    public static IServiceCollection AddInstituicoes(this IServiceCollection s) => s;

    public static void MapInstituicoes(this RouteGroupBuilder api)
    {
        BaixarImagemInstituicao.Mapear(api);
        ManifestoInstituicao.Mapear(api);
        var admin = api.MapAdmin();
        VerAparencia.Mapear(admin);
        AlterarAparencia.Mapear(admin);
        EnviarImagemInstituicao.Mapear(admin);
        RemoverImagemInstituicao.Mapear(admin);
    }
}
