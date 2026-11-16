using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Esclari.Api.Dados;
using Esclari.Api.Instituicoes.Plataforma;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class MarcaTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    [Fact]
    public async Task Emails_citam_Esclari_e_remetente_padrao()
    {
        var i = await f.Instituicao(nome: "Univag");
        var email = $"aluno-{Guid.NewGuid():N}@teste.esclari";
        var u = await f.Usuario(i, [Papeis.Aluno], senha: null, email: email);
        var r = await f.Cliente().PostAsJsonAsync("/api/auth/primeiro-acesso", new { instituicao = i.Slug, cpf = u.Cpf.Valor });
        Assert.Equal(HttpStatusCode.Accepted, r.StatusCode);
        var msg = await PrimeiroAcessoTests.EmailNoMailpit(email);
        Assert.NotNull(msg);
        Assert.Contains("senha no Esclari", msg.Value.GetProperty("Text").GetString());
        Assert.Equal("nao-responda@esclari.com.br", msg.Value.GetProperty("From").GetProperty("Address").GetString());

        var cfg = f.Services.GetRequiredService<IConfiguration>();
        var admin = Criacao.EmailDeAcesso(cfg, i, "admin@teste.esclari", "token", f.Relogio.GetUtcNow().UtcDateTime);
        Assert.Contains("administrador de Univag no Esclari.", admin.Corpo);

        await f.PlataformaMoodle(i);
        var falha = await f.Cliente().Lancar(i.Slug, "id-token", "estado-que-nao-existe");
        Assert.Equal(HttpStatusCode.BadRequest, falha.StatusCode);
        Assert.Contains("Abra o Esclari pelo Moodle de novo.", (await falha.Json()).GetProperty("detail").GetString());

        Assert.DoesNotMatch(new Regex("(?<![A-Za-z])One(?![a-z])"), msg.Value.GetProperty("Text").GetString()! + admin.Corpo + await falha.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cookie_de_sessao_se_chama_esclari_auth()
    {
        var i = await f.Instituicao();
        var cpf = Semente.NovoCpf();
        await f.Usuario(i, [Papeis.Aluno], cpf: cpf);

        var r = await f.Cliente().Login(i.Slug, cpf);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var cookies = r.Headers.GetValues("Set-Cookie").ToList();
        Assert.Single(cookies, c => c.StartsWith("esclari.auth="));
        Assert.DoesNotContain(cookies, c => c.StartsWith("one.auth="));
    }

    [Fact]
    public void Compose_usa_nome_esclari()
    {
        var compose = File.ReadAllText(Path.Combine(ArquiteturaTests.Raiz(), "docker-compose.yml"));
        Assert.Matches(new Regex(@"POSTGRES_DB:\s*esclari\s*$", RegexOptions.Multiline), compose);
        Assert.Matches(new Regex(@"MINIO_ROOT_USER:\s*esclari\s*$", RegexOptions.Multiline), compose);
        Assert.Matches(new Regex(@"Armazenamento__Chave:\s*esclari\s*$", RegexOptions.Multiline), compose);
        Assert.Matches(new Regex(@"Armazenamento__Bucket:\s*esclari\s*$", RegexOptions.Multiline), compose);
        Assert.Contains("ConnectionStrings__Esclari:", compose);
        Assert.Contains("Database=esclari;", compose);
        Assert.DoesNotMatch(new Regex(@"(?<![A-Za-z])[Oo][Nn][Ee](?![a-z])"), compose);
        Assert.DoesNotContain("ConnectionStrings__One", compose);
    }

    [Fact]
    public void UserSecretsId_e_esclari_api()
    {
        var projeto = File.ReadAllText(Path.Combine(ArquiteturaTests.Raiz(), "Esclari.Api", "Esclari.Api.csproj"));
        Assert.Equal("esclari-api", Regex.Match(projeto, @"<UserSecretsId>([^<]*)</UserSecretsId>").Groups[1].Value);
    }
}
