using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Esclari.Api.Dados;

namespace Esclari.Api;

public static class Persistencia
{
    public static IServiceCollection AddPersistencia(this IServiceCollection s)
    {
        s.AddDbContext<EsclariDb>((sp, o) => o.UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString("Esclari"), n => n.UseVector()).AddInterceptors(sp.GetServices<IInterceptor>()));
        s.AddSingleton<EnviadorSmtp>();
        s.AddSingleton(sp => Prazos.Ler(sp.GetRequiredService<IConfiguration>()));
        s.AddScoped<PrazosAtuais>();
        s.AddSingleton<Agendador>();
        s.AddHostedService(sp => sp.GetRequiredService<Agendador>());
        return s;
    }

    public static void MigrarBanco(this IServiceProvider servicos)
    {
        using var escopo = servicos.CreateScope();
        escopo.ServiceProvider.GetRequiredService<EsclariDb>().Database.Migrate();
    }
}
