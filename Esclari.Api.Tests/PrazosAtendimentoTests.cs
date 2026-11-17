using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Esclari.Api.Tests.Infra;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Tests;

public class PrazosAtendimentoTests(EsclariFactoryAtraso f) : IClassFixture<EsclariFactoryAtraso>
{
    static async Task<long> NovaConversa(Aluno a) =>
        (await (await a.Cliente.PostAsJsonAsync("/api/conversas", new { mensagem = "preciso do boleto" })).Json()).GetProperty("conversaId").GetInt64();

    static JsonElement? Prazo(JsonElement detalhe) =>
        detalhe.TryGetProperty("prazoPrimeiraRespostaEm", out var p) && p.ValueKind != JsonValueKind.Null ? p : null;

    [Fact]
    public async Task Transferir_devolve_o_prazo_da_primeira_resposta()
    {
        var aluno = await f.AlunoLogado();
        var id = await NovaConversa(aluno);

        var r = await aluno.Cliente.PostAsJsonAsync($"/api/conversas/{id}/transferir", new { });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var corpo = await r.Json();
        Assert.Equal(24, corpo.GetProperty("prazoPrimeiraRespostaHoras").GetInt32());
        Assert.StartsWith("ESC-", corpo.GetProperty("numero").GetString());
        Assert.Equal(Aberto, corpo.GetProperty("estado").GetString());
        Assert.Equal("Triagem", corpo.GetProperty("setor").GetString());

        await using var oito = new EsclariFactoryMetasConfiguradas(new() { [Prazos.ChavePrimeiraResposta] = "8" });
        var outro = await oito.AlunoLogado();
        var outra = await NovaConversa(outro);
        var r8 = await outro.Cliente.PostAsJsonAsync($"/api/conversas/{outra}/transferir", new { });
        Assert.Equal(HttpStatusCode.Created, r8.StatusCode);
        Assert.Equal(8, (await r8.Json()).GetProperty("prazoPrimeiraRespostaHoras").GetInt32());
    }

    [Fact]
    public async Task Detalhe_do_aluno_traz_a_previsao()
    {
        var b = await Balcao.Criar(f);
        Assert.Equal(new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc), f.Relogio.GetUtcNow().UtcDateTime);
        var numero = await b.Chamado();

        var detalhe = await b.Detalhe(b.ClienteAluno, numero);

        Assert.Equal(Aberto, detalhe.GetProperty("estado").GetString());
        Assert.Equal("2026-11-18T12:00:00Z", Prazo(detalhe)!.Value.GetString());
        Assert.Null(Prazo(await b.Detalhe(b.A.Cliente, numero)));
    }

    [Fact]
    public async Task Previsao_some_depois_da_resposta_e_fora_dos_estados_abertos()
    {
        var b = await Balcao.Criar(f);
        var assumido = await b.Chamado(EmAtendimento);
        Assert.NotNull(Prazo(await b.Detalhe(b.ClienteAluno, assumido)));
        var respondido = await b.Chamado();
        Assert.Equal(HttpStatusCode.Created, (await Balcao.Mensagem(b.A.Cliente, respondido, "Estamos vendo o boleto.")).StatusCode);
        Assert.Equal(EmAtendimento, (await b.Ler(respondido)).Estado);

        Assert.Null(Prazo(await b.Detalhe(b.ClienteAluno, respondido)));
        foreach (var estado in new[] { AguardandoAluno, Resolvido, Fechado })
        {
            var numero = await b.Chamado(estado);
            var detalhe = await b.Detalhe(b.ClienteAluno, numero);
            Assert.Equal(estado, detalhe.GetProperty("estado").GetString());
            Assert.Null(Prazo(detalhe));
        }
    }

    [Fact]
    public async Task Chamado_de_outro_aluno_responde_404_sem_prazo()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado();
        var outra = await f.AlunoLogado(b.Instituicao);

        var r = await outra.Cliente.GetAsync($"/api/chamados/{numero}");

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        var texto = await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain("prazoPrimeiraRespostaEm", texto);
        Assert.DoesNotContain("2026-11-18", texto);
        Assert.NotNull(Prazo(await b.Detalhe(b.ClienteAluno, numero)));
    }

    [Fact]
    public async Task Status_das_rotas_alteradas()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado();
        var anonimo = f.Cliente();

        Assert.Equal(HttpStatusCode.OK, (await b.A.Cliente.GetAsync("/api/chamados/busca?atraso=true")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await b.A.Cliente.GetAsync("/api/chamados/busca?atraso=talvez")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/api/chamados/busca?atraso=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await b.ClienteAluno.GetAsync("/api/chamados/busca?atraso=true")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await b.ClienteAluno.GetAsync($"/api/chamados/{numero}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync($"/api/chamados/{numero}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.ClienteAluno.GetAsync("/api/chamados/ESC-999999")).StatusCode);
    }

    [Fact]
    public async Task Transferir_mantem_os_status()
    {
        var aluno = await f.AlunoLogado();
        var id = await NovaConversa(aluno);
        var outro = await f.AlunoLogado(aluno.Instituicao);

        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Cliente().PostAsJsonAsync($"/api/conversas/{id}/transferir", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outro.Cliente.PostAsJsonAsync($"/api/conversas/{id}/transferir", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await aluno.Cliente.PostAsJsonAsync($"/api/conversas/{id}/transferir", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await aluno.Cliente.PostAsJsonAsync($"/api/conversas/{id}/transferir", new { })).StatusCode);
    }
}
