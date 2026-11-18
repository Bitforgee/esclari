using System.Net;
using System.Net.Http.Json;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class PlataformaLimiteTests(EsclariFactoryComLimites f) : IClassFixture<EsclariFactoryComLimites>
{
    [Fact]
    public async Task Limitador_auth_429_na_sexta_tentativa()
    {
        var c = f.Cliente();
        for (var i = 0; i < 5; i++) Assert.Equal(HttpStatusCode.Unauthorized, (await c.EntrarComSenha("ninguem@equipe.esclari")).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await c.EntrarComSenha("ninguem@equipe.esclari")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await c.InformarCodigo("123456")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await c.PostAsJsonAsync("/api/plataforma/auth/definir-senha", new { token = "x", senha = "senha-nova-123" })).StatusCode);
    }
}
