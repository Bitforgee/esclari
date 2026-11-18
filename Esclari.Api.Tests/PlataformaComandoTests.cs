using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Plataforma;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class PlataformaComandoTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    static string Email() => $"cmd-{Guid.NewGuid():N}@equipe.esclari";

    async Task<(int Codigo, string Saida, string Erro)> Rodar(params string[] args)
    {
        var (saida, erro) = (new StringWriter(), new StringWriter());
        var codigo = await PlataformaComando.Executar(f.Services, args, saida, erro);
        return (codigo, saida.ToString(), erro.ToString());
    }

    static string Token(string saida) => Regex.Match(saida, @"/plataforma/definir-senha\?token=(\S+)").Groups[1].Value;

    Task<OperadorPlataforma> Linha(string email) => f.Db(null, db => db.OperadoresPlataforma.AsNoTracking().SingleAsync(o => o.Email == email));

    Task<int> Auditorias(long operadorId) => f.Db(null, db => db.AuditoriaDaPlataforma.CountAsync(a => a.OperadorId == operadorId && a.Acao == "OPERADOR_ALTERADO"));

    [Fact]
    public async Task Criar_imprime_o_link_de_60_minutos_e_audita()
    {
        var email = Email();

        var (codigo, saida, erro) = await Rodar("operador", "--email", email, "--nome", "Ana Dev");

        Assert.Equal((0, ""), (codigo, erro));
        var token = Token(saida);
        Assert.NotEqual("", token);
        Assert.Contains("http://localhost:8080/plataforma/definir-senha?token=", saida);
        var operador = await Linha(email);
        Assert.Equal(("Ana Dev", null), (operador.Nome, operador.SenhaHash));
        Assert.Equal(f.Relogio.GetUtcNow().UtcDateTime.AddMinutes(60), operador.ConviteExpiraEm);
        Assert.Equal(1, await Auditorias(operador.Id));
        Assert.Equal(HttpStatusCode.NoContent, (await f.Cliente().PostAsJsonAsync("/api/plataforma/auth/definir-senha", new { token, senha = "senha-nova-123" })).StatusCode);
    }

    [Fact]
    public async Task Convite_novo_invalida_o_anterior_e_operador_com_senha_e_recusado()
    {
        var email = Email();
        var primeiro = Token((await Rodar("operador", "--email", email, "--nome", "Ana Dev")).Saida);

        var (codigo, saida, _) = await Rodar("operador", "--email", email, "--nome", "Ana Dev");
        var segundo = Token(saida);

        Assert.Equal(0, codigo);
        Assert.NotEqual(primeiro, segundo);
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Cliente().PostAsJsonAsync("/api/plataforma/auth/definir-senha", new { token = primeiro, senha = "senha-nova-123" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await f.Cliente().PostAsJsonAsync("/api/plataforma/auth/definir-senha", new { token = segundo, senha = "senha-nova-123" })).StatusCode);

        var depois = await Rodar("operador", "--email", email, "--nome", "Ana Dev");
        Assert.Equal(1, depois.Codigo);
        Assert.Contains("Esse operador já existe.", depois.Erro);
    }

    [Fact]
    public async Task Desativar_reativar_e_redefinir_2fa_com_as_recusas()
    {
        var (a, _) = await f.NovoOperador();
        var (b, _) = await f.NovoOperador();

        var desativar = await Rodar("operador", "--email", a.Email, "--desativar");
        Assert.Equal(0, desativar.Codigo);
        Assert.Contains("desativado", desativar.Saida);
        Assert.False((await Linha(a.Email)).Ativo);

        var reativar = await Rodar("operador", "--email", a.Email, "--reativar");
        Assert.Equal(0, reativar.Codigo);
        Assert.True((await Linha(a.Email)).Ativo);

        var redefinir = await Rodar("operador", "--email", a.Email, "--redefinir-2fa");
        Assert.Equal(0, redefinir.Codigo);
        var depois = await Linha(a.Email);
        Assert.Equal((null, null), (depois.TotpSegredo, depois.TotpUltimoPasso));
        Assert.Equal(3, await Auditorias(a.Id));

        var inexistente = await Rodar("operador", "--email", Email(), "--desativar");
        Assert.Equal(1, inexistente.Codigo);
        Assert.Contains("não há operador", inexistente.Erro);

        await f.Db(null, db => db.OperadoresPlataforma.Where(o => o.Id != b.Id).ExecuteUpdateAsync(s => s.SetProperty(o => o.Ativo, false)));
        var ultimo = await Rodar("operador", "--email", b.Email, "--desativar");
        Assert.Equal(1, ultimo.Codigo);
        Assert.Contains("último operador ativo", ultimo.Erro);
        Assert.True((await Linha(b.Email)).Ativo);
        Assert.Equal(0, await Auditorias(b.Id));
    }

    [Fact]
    public async Task Papel_operador_nao_existe_mais()
    {
        var i = await f.Instituicao();
        var admin = await f.Entrar(i, await f.Usuario(i, [Papeis.Administrador]));

        var r = await admin.PostAsJsonAsync("/api/admin/usuarios", new { cpf = Semente.NovoCpf(), nome = "Novo", email = $"{Guid.NewGuid():N}@teste.esclari", senhaTemporaria = "temporaria-123", papeis = new[] { "OPERADOR" }, setores = Array.Empty<long>() });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("Escolha ao menos um papel.", (await r.Json()).GetProperty("errors").GetProperty("papeis")[0].GetString());
        Assert.DoesNotContain(typeof(Papeis).GetFields().Select(x => x.Name), n => n == "Operador");
        Assert.Equal(["Administrador", "Aluno", "Atendente"], typeof(Papeis).GetFields().Select(x => x.Name).Order());
    }

    [Fact]
    public async Task Instituicao_operador_nao_existe_mais()
    {
        var (saida, erro) = (new StringWriter(), new StringWriter());

        var codigo = await InstituicaoComando.Executar(f.Services, ["operador", "--slug", "demo", "--cpf", "123"], saida, erro);

        Assert.Equal(1, codigo);
        var uso = erro.ToString();
        Assert.Contains("instituicao criar", uso);
        Assert.Contains("instituicao carga", uso);
        Assert.Contains("instituicao avaliar-busca", uso);
        Assert.DoesNotContain("operador", uso);
    }
}
