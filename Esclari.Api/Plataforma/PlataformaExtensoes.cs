using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Esclari.Api.Plataforma;

public static class PlataformaExtensoes
{
    public static IServiceCollection AddPlataforma(this IServiceCollection s)
    {
        s.AddScoped<IPasswordHasher<OperadorPlataforma>, PasswordHasher<OperadorPlataforma>>();
        s.AddSingleton<ProtecaoDaPlataforma>();
        s.AddAuthentication().AddSessaoDaPlataforma();
        return s;
    }

    public static void MapPlataforma(this WebApplication app)
    {
        var g = app.MapGroup(SessaoDaPlataforma.Caminho);
        EntrarNaPlataforma.Mapear(g);
        CodigoDaPlataforma.Mapear(g);
        DefinirSenhaDaPlataforma.Mapear(g);
        SairDaPlataforma.Mapear(g);
        var restrita = g.MapGroup("").RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = SessaoDaPlataforma.Esquema });
        EuDaPlataforma.Mapear(restrita);
        ListarInstituicoes.Mapear(restrita);
        DetalheDaInstituicao.Mapear(restrita);
        EditarInstituicao.Mapear(restrita);
        CriarInstituicao.Mapear(restrita);
        ImportarCargaInstituicao.Mapear(restrita);
        DesativarInstituicao.Mapear(restrita);
        ReativarInstituicao.Mapear(restrita);
        ReenviarAcesso.Mapear(restrita);
        ListarAuditoria.Mapear(restrita);
    }
}
