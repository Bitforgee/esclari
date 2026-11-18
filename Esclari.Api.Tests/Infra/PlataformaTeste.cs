using System.Net.Http.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Esclari.Api.Plataforma;

namespace Esclari.Api.Tests.Infra;

public sealed record OperadorLogado(OperadorPlataforma Operador, string Segredo, HttpClient Cliente);

public static class PlataformaTeste
{
    public const string SenhaDoOperador = "operador-teste-1";

    public static string Codigo(this EsclariFactory f, string segredo, int deslocamento = 0) =>
        Totp.Codigo(segredo, Totp.PassoDe(f.Relogio.GetUtcNow()) + deslocamento);

    public static async Task<(OperadorPlataforma Operador, string Segredo)> NovoOperador(this EsclariFactory f, bool comSegundoFator = true, bool comSenha = true, bool ativo = true)
    {
        var segredo = Totp.NovoSegredo();
        var email = $"op-{Guid.NewGuid():N}@equipe.esclari";
        var operador = await f.Db(null, async db =>
        {
            var novo = OperadorPlataforma.Criar("Dev Teste", email, f.Relogio.GetUtcNow().UtcDateTime);
            if (comSenha) novo.DefinirSenha(new PasswordHasher<OperadorPlataforma>().HashPassword(novo, SenhaDoOperador));
            if (comSegundoFator) novo.CadastrarSegundoFator(f.Services.GetRequiredService<ProtecaoDaPlataforma>().Segredo.Protect(segredo), 0);
            if (!ativo) novo.Desativar();
            db.OperadoresPlataforma.Add(novo);
            await db.SaveChangesAsync();
            return novo;
        });
        return (operador, segredo);
    }

    public static Task<HttpResponseMessage> EntrarComSenha(this HttpClient c, string email, string senha = SenhaDoOperador) =>
        c.PostAsJsonAsync("/api/plataforma/auth/entrar", new { email, senha });

    public static Task<HttpResponseMessage> InformarCodigo(this HttpClient c, string codigo) =>
        c.PostAsJsonAsync("/api/plataforma/auth/codigo", new { codigo });

    public static async Task<OperadorLogado> Operador(this EsclariFactory f)
    {
        var (operador, segredo) = await f.NovoOperador();
        var cliente = f.Cliente();
        Assert.Equal(System.Net.HttpStatusCode.OK, (await cliente.EntrarComSenha(operador.Email)).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, (await cliente.InformarCodigo(f.Codigo(segredo))).StatusCode);
        return new(operador, segredo, cliente);
    }
}
