using Esclari.Api.Administracao.Categorias;
using Esclari.Api.Administracao.Cursos;
using Esclari.Api.Administracao.Fluxos;
using Esclari.Api.Administracao.Integracoes;
using Esclari.Api.Administracao.Matriculas;
using Esclari.Api.Administracao.RespostasProntas;
using Esclari.Api.Administracao.Setores;
using Esclari.Api.Administracao.Usuarios;

namespace Esclari.Api.Administracao;

public static class AdministracaoExtensoes
{
    public static IServiceCollection AddAdministracao(this IServiceCollection s) => s;

    public static void MapAdministracao(this RouteGroupBuilder api)
    {
        var usuarios = api.MapAdmin(Funcoes.UsuariosGerenciar);
        ListarUsuarios.Mapear(usuarios);
        CriarUsuario.Mapear(usuarios);
        AlterarUsuario.Mapear(usuarios);
        DesativarUsuario.Mapear(usuarios);
        ListarTipos.Mapear(api.MapAdmin(Funcoes.TiposGerenciar, Funcoes.UsuariosGerenciar));
        var tipos = api.MapAdmin(Funcoes.TiposGerenciar);
        CriarTipo.Mapear(tipos);
        AlterarTipo.Mapear(tipos);
        ApagarTipo.Mapear(tipos);
        ImportarMatriculas.Mapear(api.MapAdmin(Funcoes.MatriculasImportar));
        var admin = api.MapAdmin(Funcoes.CadastrosGerenciar);
        ListarSetores.Mapear(admin);
        CriarSetor.Mapear(admin);
        AlterarSetor.Mapear(admin);
        ListarCategorias.Mapear(admin);
        CriarCategoria.Mapear(admin);
        AlterarCategoria.Mapear(admin);
        ListarCursos.Mapear(admin);
        CriarCurso.Mapear(admin);
        AlterarCurso.Mapear(admin);
        ListarRespostasProntas.Mapear(admin);
        CriarRespostaPronta.Mapear(admin);
        AlterarRespostaPronta.Mapear(admin);
        ListarFluxos.Mapear(admin);
        CriarFluxo.Mapear(admin);
        SalvarRascunho.Mapear(admin);
        DescartarRascunho.Mapear(admin);
        PublicarFluxo.Mapear(admin);
        SimularFluxo.Mapear(admin);
        var integracoes = api.MapAdmin(Funcoes.IntegracoesGerenciar);
        VerMoodle.Mapear(integracoes);
        AlterarMoodle.Mapear(integracoes);
        VerMcp.Mapear(integracoes);
        CriarTokenMcp.Mapear(integracoes);
        RevogarTokenMcp.Mapear(integracoes);
    }
}
