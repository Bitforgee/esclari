using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Esclari.Api.Plataforma;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class PlataformaAcessoTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    const string Entrada = "/api/plataforma/auth/entrar";
    const string Codigo = "/api/plataforma/auth/codigo";
    const string DefinirSenha = "/api/plataforma/auth/definir-senha";

    static List<string> Cookies(HttpResponseMessage r) => r.Headers.TryGetValues("Set-Cookie", out var v) ? [.. v] : [];

    static bool Grava(HttpResponseMessage r, string nome) => Cookies(r).Any(c => c.StartsWith(nome + "=") && !c.StartsWith(nome + "=;"));

    Task<OperadorPlataforma> Recarregar(long id) => f.Db(null, db => db.OperadoresPlataforma.AsNoTracking().SingleAsync(o => o.Id == id));

    async Task<string> Convite(long id)
    {
        var token = "";
        await f.Db(null, async db =>
        {
            var o = await db.OperadoresPlataforma.SingleAsync(x => x.Id == id);
            token = o.NovoConvite(f.Relogio.GetUtcNow().UtcDateTime);
            await db.SaveChangesAsync();
        });
        return token;
    }

    [Fact]
    public async Task Operador_nao_pertence_a_instituicao()
    {
        var (operador, _) = await f.NovoOperador();
        var colunas = await f.Db(null, db => db.Database.SqlQueryRaw<string>("SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_name = 'operador_plataforma'").ToListAsync());
        var i = await f.Instituicao();
        var admin = await f.Entrar(i, await f.Usuario(i, [Papeis.Administrador]));

        var lista = await (await admin.GetAsync("/api/admin/usuarios")).Json();

        Assert.DoesNotContain("instituicao_id", colunas);
        Assert.Contains("email", colunas);
        Assert.DoesNotContain(lista.EnumerateArray(), u => u.GetProperty("email").GetString() == operador.Email);
    }

    [Fact]
    public async Task Definir_senha_204_e_token_de_uso_unico()
    {
        var (operador, _) = await f.NovoOperador(comSegundoFator: false, comSenha: false);
        var token = await Convite(operador.Id);
        var c = f.Cliente();

        var primeira = await c.PostAsJsonAsync(DefinirSenha, new { token, senha = "senha-nova-123" });
        var segunda = await c.PostAsJsonAsync(DefinirSenha, new { token, senha = "outra-senha-123" });

        Assert.Equal(HttpStatusCode.NoContent, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, segunda.StatusCode);
        var linha = await Recarregar(operador.Id);
        Assert.NotNull(linha.SenhaHash);
        Assert.NotEqual("senha-nova-123", linha.SenhaHash);
        Assert.Null(linha.ConviteHash);
        Assert.Equal(HttpStatusCode.OK, (await c.EntrarComSenha(operador.Email, "senha-nova-123")).StatusCode);
    }

    [Fact]
    public async Task Definir_senha_400_nos_quatro_casos()
    {
        var (operador, _) = await f.NovoOperador(comSegundoFator: false, comSenha: false);
        var token = await Convite(operador.Id);
        var c = f.Cliente();

        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(DefinirSenha, new { token = "inexistente", senha = "senha-nova-123" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(DefinirSenha, new { token, senha = new string('a', 7) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(DefinirSenha, new { token, senha = new string('a', 129) })).StatusCode);
        f.Relogio.Advance(TimeSpan.FromMinutes(61));
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(DefinirSenha, new { token, senha = "senha-nova-123" })).StatusCode);
        Assert.Null((await Recarregar(operador.Id)).SenhaHash);
    }

    [Fact]
    public async Task Entrar_com_segundo_fator_pede_o_codigo()
    {
        var (operador, _) = await f.NovoOperador();

        var r = await f.Cliente().EntrarComSenha(operador.Email);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        Assert.Equal("CODIGO", j.GetProperty("etapa").GetString());
        Assert.Equal(["etapa", "segredo", "uri"], j.EnumerateObject().Select(p => p.Name));
        Assert.True(Grava(r, "esclari_plataforma_etapa"));
        Assert.False(Grava(r, "esclari_plataforma"));
    }

    [Fact]
    public async Task Entrar_sem_segundo_fator_devolve_segredo_e_uri()
    {
        var (operador, _) = await f.NovoOperador(comSegundoFator: false);

        var r = await f.Cliente().EntrarComSenha(operador.Email);

        var j = await r.Json();
        Assert.Equal("CADASTRAR", j.GetProperty("etapa").GetString());
        var segredo = j.GetProperty("segredo").GetString()!;
        Assert.Matches("^[A-Z2-7]{32}$", segredo);
        Assert.Equal($"otpauth://totp/Esclari:{operador.Email}?secret={segredo}&issuer=Esclari&image=http%3A%2F%2Flocalhost%3A8080%2Ficons%2Ficon-192.png", j.GetProperty("uri").GetString());
        Assert.True(Grava(r, "esclari_plataforma_etapa"));
        Assert.Null((await Recarregar(operador.Id)).TotpSegredo);
    }

    [Fact]
    public async Task Entrar_401_com_a_mesma_frase_nos_tres_casos()
    {
        var (certo, _) = await f.NovoOperador();
        var (desativado, _) = await f.NovoOperador(ativo: false);
        var c = f.Cliente();

        var respostas = new[]
        {
            await c.EntrarComSenha("ninguem@equipe.esclari"),
            await c.EntrarComSenha(certo.Email, "senha-errada-123"),
            await c.EntrarComSenha(desativado.Email),
        };

        foreach (var r in respostas)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
            Assert.Equal("E-mail ou senha inválidos.", (await r.Json()).GetProperty("detail").GetString());
            Assert.False(Grava(r, "esclari_plataforma_etapa"));
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Codigo_abre_a_sessao_nos_tres_passos_e_grava_o_segredo_no_cadastro(int deslocamento)
    {
        var (operador, segredo) = await f.NovoOperador();
        var c = f.Cliente();
        await c.EntrarComSenha(operador.Email);

        var r = await c.InformarCodigo(f.Codigo(segredo, deslocamento));

        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        Assert.True(Grava(r, "esclari_plataforma"));
        Assert.Contains(Cookies(r), x => x.StartsWith("esclari_plataforma_etapa=;"));
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/plataforma/eu")).StatusCode);
        Assert.Equal(Totp.PassoDe(f.Relogio.GetUtcNow()) + deslocamento, (await Recarregar(operador.Id)).TotpUltimoPasso);
        Assert.Equal(1, await f.Db(null, db => db.AuditoriaDaPlataforma.CountAsync(a => a.OperadorId == operador.Id && a.Acao == "OPERADOR_ENTROU")));
    }

    [Fact]
    public async Task Codigo_no_cadastro_grava_o_segredo_protegido()
    {
        var (operador, _) = await f.NovoOperador(comSegundoFator: false);
        var c = f.Cliente();
        var segredo = (await (await c.EntrarComSenha(operador.Email)).Json()).GetProperty("segredo").GetString()!;

        var r = await c.InformarCodigo(f.Codigo(segredo));

        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        var linha = await Recarregar(operador.Id);
        Assert.NotNull(linha.TotpSegredo);
        Assert.NotEqual(segredo, linha.TotpSegredo);
        Assert.Equal(segredo, f.Services.GetRequiredService<ProtecaoDaPlataforma>().Segredo.Unprotect(linha.TotpSegredo!));
    }

    [Fact]
    public async Task Codigo_401_errado_repetido_sem_etapa_e_etapa_vencida()
    {
        var (operador, segredo) = await f.NovoOperador();
        var certo = f.Codigo(segredo);
        var errado = certo == "000000" ? "000001" : "000000";
        var c = f.Cliente();
        await c.EntrarComSenha(operador.Email);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.InformarCodigo(errado)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.InformarCodigo(certo)).StatusCode);

        var outro = f.Cliente();
        await outro.EntrarComSenha(operador.Email);
        var repetido = await outro.InformarCodigo(certo);
        Assert.Equal(HttpStatusCode.Unauthorized, repetido.StatusCode);
        Assert.False(Grava(repetido, "esclari_plataforma"));

        var semEtapa = await f.Cliente().InformarCodigo(f.Codigo(segredo, 1));
        Assert.Equal(HttpStatusCode.Unauthorized, semEtapa.StatusCode);

        var vencido = f.Cliente();
        await vencido.EntrarComSenha(operador.Email);
        f.Relogio.Advance(TimeSpan.FromMinutes(6));
        var tardio = await vencido.InformarCodigo(f.Codigo(segredo));
        Assert.Equal(HttpStatusCode.Unauthorized, tardio.StatusCode);
        Assert.False(Grava(tardio, "esclari_plataforma"));
    }

    [Fact]
    public async Task Corpo_invalido_400()
    {
        var (operador, _) = await f.NovoOperador();
        var c = f.Cliente();
        await c.EntrarComSenha(operador.Email);

        Assert.Equal(HttpStatusCode.BadRequest, (await c.InformarCodigo("12345")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.InformarCodigo("12a456")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.EntrarComSenha("")).StatusCode);
    }

    [Fact]
    public async Task Sessao_expira_aos_30_minutos_sem_uso_e_aos_8_horas()
    {
        var parada = await f.Operador();
        f.Relogio.Advance(TimeSpan.FromMinutes(31));
        Assert.Equal(HttpStatusCode.Unauthorized, (await parada.Cliente.GetAsync("/api/plataforma/eu")).StatusCode);

        var ativa = await f.Operador();
        for (var i = 0; i < 23; i++)
        {
            f.Relogio.Advance(TimeSpan.FromMinutes(20));
            Assert.Equal(HttpStatusCode.OK, (await ativa.Cliente.GetAsync("/api/plataforma/eu")).StatusCode);
        }
        f.Relogio.Advance(TimeSpan.FromMinutes(21));
        Assert.Equal(HttpStatusCode.Unauthorized, (await ativa.Cliente.GetAsync("/api/plataforma/eu")).StatusCode);
    }

    [Fact]
    public async Task Sair_apaga_os_dois_cookies()
    {
        var op = await f.Operador();

        var r = await op.Cliente.PostAsync("/api/plataforma/auth/sair", null);

        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        Assert.Contains(Cookies(r), c => c.StartsWith("esclari_plataforma=;") && c.Contains("expires=Thu, 01 Jan 1970"));
        Assert.Contains(Cookies(r), c => c.StartsWith("esclari_plataforma_etapa=;"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await op.Cliente.GetAsync("/api/plataforma/eu")).StatusCode);
    }

    [Fact]
    public async Task Eu_devolve_so_id_nome_e_email()
    {
        var op = await f.Operador();

        var r = await op.Cliente.GetAsync("/api/plataforma/eu");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        Assert.Equal(["id", "nome", "email"], j.EnumerateObject().Select(p => p.Name));
        Assert.Equal((op.Operador.Id, op.Operador.Nome, op.Operador.Email), (j.GetProperty("id").GetInt64(), j.GetProperty("nome").GetString(), j.GetProperty("email").GetString()));
    }

    [Fact]
    public async Task Cada_cookie_so_vale_no_proprio_mundo()
    {
        var i = await f.Instituicao();
        var admin = await f.Entrar(i, await f.Usuario(i, [Papeis.Administrador]));
        var op = await f.Operador();

        Assert.Equal(HttpStatusCode.Unauthorized, (await admin.GetAsync("/api/plataforma/instituicoes")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await admin.GetAsync("/api/plataforma/eu")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await op.Cliente.GetAsync("/api/chamados/busca")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await op.Cliente.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await op.Cliente.GetAsync("/api/plataforma/instituicoes")).StatusCode);
    }

    [Fact]
    public async Task Operador_desativado_perde_a_sessao_na_requisicao_seguinte()
    {
        var op = await f.Operador();
        Assert.Equal(HttpStatusCode.OK, (await op.Cliente.GetAsync("/api/plataforma/eu")).StatusCode);

        await f.Db(null, async db =>
        {
            (await db.OperadoresPlataforma.SingleAsync(o => o.Id == op.Operador.Id)).Desativar();
            await db.SaveChangesAsync();
        });

        Assert.Equal(HttpStatusCode.Unauthorized, (await op.Cliente.GetAsync("/api/plataforma/eu")).StatusCode);
    }

    [Fact]
    public async Task Atributos_dos_cookies_da_plataforma()
    {
        var (operador, segredo) = await f.NovoOperador();
        var c = f.Cliente();

        var entrar = await c.EntrarComSenha(operador.Email);
        var etapa = Cookies(entrar).Single(x => x.StartsWith("esclari_plataforma_etapa="));
        var codigo = await c.InformarCodigo(f.Codigo(segredo));
        var sessao = Cookies(codigo).Single(x => x.StartsWith("esclari_plataforma="));

        foreach (var cookie in new[] { etapa, sessao })
        {
            Assert.Contains("path=/api/plataforma", cookie);
            Assert.Contains("httponly", cookie);
            Assert.Contains("samesite=strict", cookie);
            Assert.Contains("secure", cookie);
        }
        Assert.Contains("max-age=300", etapa);
    }
}
