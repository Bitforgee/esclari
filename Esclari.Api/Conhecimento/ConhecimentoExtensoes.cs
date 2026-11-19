using Esclari.Api.Conhecimento.Artigos;
using Esclari.Api.Conhecimento.Documentos;

namespace Esclari.Api.Conhecimento;

public static class ConhecimentoExtensoes
{
    public static IServiceCollection AddConhecimento(this IServiceCollection s) => s.AddSingleton<ITarefaAgendada, AvisarValidade>();

    public static void MapConhecimento(this RouteGroupBuilder api)
    {
        VerVersaoArtigo.Mapear(api);
        CriarArtigoDoChamado.Mapear(api);
        BaixarVersaoDocumento.Mapear(api);
        var admin = api.MapAdmin(Funcoes.ConhecimentoGerenciar);
        ListarArtigos.Mapear(admin);
        CriarArtigo.Mapear(admin);
        AlterarArtigo.Mapear(admin);
        AtivarArtigo.Mapear(admin);
        ArquivarArtigo.Mapear(admin);
        RenovarArtigo.Mapear(admin);
        ListarDocumentos.Mapear(admin);
        CriarDocumento.Mapear(admin);
        NovaVersaoDocumento.Mapear(admin);
        ArquivarDocumento.Mapear(admin);
        RenovarDocumento.Mapear(admin);
    }
}
