using System.Net;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class MoodleLimiteTests : IAsyncLifetime
{
    readonly EsclariFactoryComLimites f = new();

    public ValueTask InitializeAsync() => f.InitializeAsync();
    public ValueTask DisposeAsync() => f.DisposeAsync();

    [Fact]
    public async Task Sexta_chamada_429()
    {
        var i = await f.Instituicao();
        await f.PlataformaMoodle(i);
        var c = f.Cliente();
        var login = $"/api/moodle/{i.Slug}/login?iss={Uri.EscapeDataString(MoodleFalso.Emissor)}&client_id={MoodleFalso.ClientId}&login_hint=h";

        for (var n = 0; n < 5; n++) Assert.Equal(HttpStatusCode.Redirect, (await c.GetAsync(login)).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await c.GetAsync(login)).StatusCode);

        await using var outra = new EsclariFactoryComLimites();
        await outra.InitializeAsync();
        var j = await outra.Instituicao();
        await outra.PlataformaMoodle(j);
        var c2 = outra.Cliente();
        for (var n = 0; n < 5; n++) Assert.Equal(HttpStatusCode.BadRequest, (await c2.Lancar(j.Slug, "x.y.z", "estado")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await c2.Lancar(j.Slug, "x.y.z", "estado")).StatusCode);
    }
}
