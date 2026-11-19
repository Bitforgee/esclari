using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class ImplantacaoEquipeTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    OperadorLogado? operador;

    async Task<HttpClient> Cliente() => (operador ??= await f.Operador()).Cliente;

    static string Equipe(Instituicao i) => $"/api/plataforma/instituicoes/{i.Slug}/equipe";

    static object Corpo(string? tipo, long[]? setores = null, string? cpf = null, string? email = null, object? extra = null) =>
        new { cpf = cpf ?? Semente.NovoCpf(), nome = "Ana Equipe", email = email ?? $"{Guid.NewGuid():N}@teste.edu.br", tipo, setores = setores ?? [], senha = (extra as string) };

    Task<long> SetorDe(Instituicao i) => f.Db(i.Id, db => db.Setores.Select(s => s.Id).FirstAsync());

    [Fact]
    public async Task Setores_da_instituicao_alvo()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        await f.Setor(i, "Financeiro");

        var r = await c.GetAsync($"/api/plataforma/instituicoes/{i.Slug}/setores");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var lista = (await r.Json()).EnumerateArray().ToList();
        Assert.Equal(["Financeiro", "Triagem"], lista.Select(s => s.GetProperty("nome").GetString()).Order());
        Assert.All(lista, s => Assert.True(s.GetProperty("id").GetInt64() > 0));
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/plataforma/instituicoes/nao-existe-xx/setores")).StatusCode);
    }

    [Fact]
    public async Task Criar_membro_por_tipo_convida_e_audita()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        var setor = await SetorDe(i);
        var email = $"{Guid.NewGuid():N}@teste.edu.br";

        var r = await c.PostAsJsonAsync(Equipe(i), Corpo(TipoDeUsuario.Atendente, [setor], email: email));

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var corpo = await r.Json();
        var id = corpo.GetProperty("id").GetInt64();
        Assert.Equal("Atendente", corpo.GetProperty("tipo").GetProperty("nome").GetString());
        Assert.False(corpo.TryGetProperty("papeis", out _));
        var usuario = await f.Db(i.Id, db => db.Usuarios.Include(u => u.Tipo).Include(u => u.Setores).SingleAsync(u => u.Id == id));
        Assert.Null(usuario.SenhaHash);
        Assert.Equal(TipoDeUsuario.Atendente, usuario.Tipo!.Sistema);
        var emails = await f.Db(null, db => db.EmailsSaida.IgnoreQueryFilters().Where(e => e.InstituicaoId == i.Id && e.Destinatario == email).ToListAsync());
        var enviado = Assert.Single(emails);
        Assert.Contains("atendente de", enviado.Corpo);
        Assert.Contains("vale 60 minutos", enviado.Corpo);
        Assert.Equal(1, await f.Db(i.Id, db => db.Tokens.CountAsync(t => t.UsuarioId == id)));
        Assert.Equal(1, await f.Db(null, db => db.AuditoriaDaPlataforma.CountAsync(a => a.InstituicaoAlvoId == i.Id && a.Acao == AcoesDaPlataforma.UsuarioCriado)));

        var gestorEmail = $"{Guid.NewGuid():N}@teste.edu.br";
        var gestor = await c.PostAsJsonAsync(Equipe(i), Corpo(TipoDeUsuario.Gestor, email: gestorEmail));
        Assert.Equal(HttpStatusCode.Created, gestor.StatusCode);
        Assert.Equal("Gestor", (await gestor.Json()).GetProperty("tipo").GetProperty("nome").GetString());
        var enviadoAoGestor = Assert.Single(await f.Db(null, db => db.EmailsSaida.IgnoreQueryFilters().Where(e => e.InstituicaoId == i.Id && e.Destinatario == gestorEmail).ToListAsync()));
        Assert.Contains("gestor de", enviadoAoGestor.Corpo);
        Assert.Contains("vale 60 minutos", enviadoAoGestor.Corpo);
    }
    [Fact]
    public async Task Tipo_invalido_400()
    {
        var c = await Cliente();
        var i = await f.Instituicao();

        foreach (var tipo in new[] { null, TipoDeUsuario.Aluno, "x", "OPERADOR", "" })
        {
            var r = await c.PostAsJsonAsync(Equipe(i), Corpo(tipo));
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.True((await r.Json()).GetProperty("errors").TryGetProperty("tipo", out _));
        }
        var soPapeis = await c.PostAsJsonAsync(Equipe(i), new { cpf = Semente.NovoCpf(), nome = "Ana", email = $"{Guid.NewGuid():N}@teste.edu.br", papeis = new[] { Papeis.Administrador } });
        Assert.Equal(HttpStatusCode.BadRequest, soPapeis.StatusCode);
        Assert.True((await soPapeis.Json()).GetProperty("errors").TryGetProperty("tipo", out _));
        Assert.Equal(0, await f.Db(i.Id, db => db.Usuarios.CountAsync()));
    }

    [Fact]
    public async Task Senha_no_corpo_e_ignorada()
    {
        var c = await Cliente();
        var i = await f.Instituicao();

        var r = await c.PostAsJsonAsync(Equipe(i), new { cpf = Semente.NovoCpf(), nome = "Ana", email = $"{Guid.NewGuid():N}@teste.edu.br", tipo = TipoDeUsuario.Gestor, senha = "Senha-do-operador-1", senhaTemporaria = "Outra-senha-1" });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.DoesNotContain("senha", (await r.Content.ReadAsStringAsync()).ToLowerInvariant());
        Assert.Null((await f.Db(i.Id, db => db.Usuarios.SingleAsync())).SenhaHash);
    }

    [Fact]
    public async Task Atendente_exige_setor_da_propria_instituicao()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        var outra = await f.Instituicao();
        var setorDaOutra = await SetorDe(outra);

        foreach (var setores in new[] { Array.Empty<long>(), [setorDaOutra], [987654321L]})
        {
            var r = await c.PostAsJsonAsync(Equipe(i), Corpo(TipoDeUsuario.Atendente, setores));
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.True((await r.Json()).GetProperty("errors").TryGetProperty("setores", out _));
        }
    }

    [Fact]
    public async Task Repetido_409_e_invalido_400()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        var cpf = Semente.NovoCpf();
        var email = $"{Guid.NewGuid():N}@teste.edu.br";
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync(Equipe(i), Corpo(TipoDeUsuario.Gestor, cpf: cpf, email: email))).StatusCode);

        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync(Equipe(i), Corpo(TipoDeUsuario.Gestor, cpf: cpf))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync(Equipe(i), Corpo(TipoDeUsuario.Gestor, email: email))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(Equipe(i), Corpo(TipoDeUsuario.Gestor, cpf: "123.456.789-00"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(Equipe(i), Corpo(TipoDeUsuario.Gestor, email: "sem-arroba"))).StatusCode);
    }

    [Fact]
    public async Task Lista_so_equipe_por_tipo_mascarada()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        await f.Usuario(i, [Papeis.Aluno]);
        await f.Usuario(i, [Papeis.Administrador], senha: null, cpf: "12345678909");
        await f.Usuario(i, [Papeis.Atendente]);
        var lider = await f.Usuario(i, [Papeis.Aluno, Papeis.Atendente], senha: null);

        var r = await c.GetAsync(Equipe(i));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var itens = (await r.Json()).EnumerateArray().ToList();
        Assert.Equal(["Atendente", "Gestor", TipoDeUsuario.NomeDoLider], itens.Select(x => x.GetProperty("tipo").GetProperty("nome").GetString()!).Order());
        Assert.All(itens, x => Assert.False(x.TryGetProperty("papeis", out _)));
        var item = itens.Single(x => x.GetProperty("tipo").GetProperty("nome").GetString() == "Gestor");
        Assert.Equal("***.456.789-**", item.GetProperty("cpfMascarado").GetString());
        Assert.Matches(@"^.\*\*\*@", item.GetProperty("emailMascarado").GetString()!);
        Assert.Equal("GESTOR", item.GetProperty("tipo").GetProperty("sistema").GetString());
        Assert.True(item.GetProperty("primeiroAcessoPendente").GetBoolean());
        Assert.True(item.GetProperty("ativo").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsync($"{Equipe(i)}/{lider.Id}/convite", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsync($"{Equipe(i)}/{lider.Id}/desativar", null)).StatusCode);
        Assert.False((await f.Db(i.Id, db => db.Usuarios.SingleAsync(u => u.Id == lider.Id))).Ativo);
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsync($"{Equipe(i)}/{lider.Id}/reativar", null)).StatusCode);
        Assert.True((await f.Db(i.Id, db => db.Usuarios.SingleAsync(u => u.Id == lider.Id))).Ativo);
    }
    [Fact]
    public async Task Convite_novo_invalida_o_anterior()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        var id = (await (await c.PostAsJsonAsync(Equipe(i), Corpo(TipoDeUsuario.Gestor))).Json()).GetProperty("id").GetInt64();

        var r = await c.PostAsync($"{Equipe(i)}/{id}/convite", null);

        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        var email = await f.Db(i.Id, db => db.Usuarios.Where(u => u.Id == id).Select(u => u.Email).SingleAsync());
        Assert.Equal(2, await f.Db(null, db => db.EmailsSaida.IgnoreQueryFilters().CountAsync(e => e.InstituicaoId == i.Id && e.Destinatario == email)));
        var tokens = await f.Db(i.Id, db => db.Tokens.Where(t => t.UsuarioId == id).OrderBy(t => t.Id).ToListAsync());
        Assert.Equal(2, tokens.Count);
        Assert.False(tokens[0].Valido(f.Relogio.GetUtcNow().UtcDateTime));
        Assert.True(tokens[1].Valido(f.Relogio.GetUtcNow().UtcDateTime));

        var comSenha = await f.Usuario(i, [Papeis.Administrador]);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync($"{Equipe(i)}/{comSenha.Id}/convite", null)).StatusCode);
    }

    [Fact]
    public async Task Desativar_protege_o_ultimo_gestor()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        var unico = await f.Usuario(i, [Papeis.Administrador]);

        var recusa = await c.PostAsync($"{Equipe(i)}/{unico.Id}/desativar", null);
        Assert.Equal(HttpStatusCode.Conflict, recusa.StatusCode);
        Assert.Equal("A instituição precisa de ao menos um gestor ativo.", (await recusa.Json()).GetProperty("detail").GetString());
        var outro = await f.Usuario(i, [Papeis.Administrador]);
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsync($"{Equipe(i)}/{outro.Id}/desativar", null)).StatusCode);

        Assert.False((await f.Db(i.Id, db => db.Usuarios.SingleAsync(u => u.Id == outro.Id))).Ativo);
        Assert.Equal(1, await f.Db(null, db => db.AuditoriaDaPlataforma.CountAsync(a => a.InstituicaoAlvoId == i.Id && a.Acao == AcoesDaPlataforma.UsuarioAlterado)));
    }

    [Fact]
    public async Task Reativar_volta_a_ativo()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        await f.Usuario(i, [Papeis.Administrador]);
        var outro = await f.Usuario(i, [Papeis.Atendente]);
        await c.PostAsync($"{Equipe(i)}/{outro.Id}/desativar", null);

        var r = await c.PostAsync($"{Equipe(i)}/{outro.Id}/reativar", null);

        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        Assert.True((await f.Db(i.Id, db => db.Usuarios.SingleAsync(u => u.Id == outro.Id))).Ativo);
        Assert.Equal(2, await f.Db(null, db => db.AuditoriaDaPlataforma.CountAsync(a => a.InstituicaoAlvoId == i.Id && a.Acao == AcoesDaPlataforma.UsuarioAlterado)));
    }

    [Fact]
    public async Task Aluno_e_outra_instituicao_404()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        var outra = await f.Instituicao();
        var aluno = await f.Usuario(i, [Papeis.Aluno]);
        var deOutra = await f.Usuario(outra, [Papeis.Administrador], senha: null);

        foreach (var id in new[] { aluno.Id, deOutra.Id, 987654321L })
            foreach (var acao in new[] { "convite", "desativar", "reativar" })
                Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync($"{Equipe(i)}/{id}/{acao}", null)).StatusCode);
        Assert.True((await f.Db(i.Id, db => db.Usuarios.SingleAsync(u => u.Id == aluno.Id))).Ativo);
    }

    [Fact]
    public async Task Resumo_sem_cpf_nem_email()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        var cpf = Semente.NovoCpf();
        var email = $"{Guid.NewGuid():N}@teste.edu.br";
        var id = (await (await c.PostAsJsonAsync(Equipe(i), Corpo(TipoDeUsuario.Gestor, [await SetorDe(i)], cpf: cpf, email: email))).Json()).GetProperty("id").GetInt64();
        await f.Usuario(i, [Papeis.Administrador]);
        await c.PostAsync($"{Equipe(i)}/{id}/convite", null);
        await c.PostAsync($"{Equipe(i)}/{id}/desativar", null);

        var linhas = await f.Db(null, db => db.AuditoriaDaPlataforma.Where(a => a.InstituicaoAlvoId == i.Id && (a.Acao == AcoesDaPlataforma.UsuarioCriado || a.Acao == AcoesDaPlataforma.UsuarioAlterado)).ToListAsync());

        Assert.Equal(3, linhas.Count);
        Assert.All(linhas, l =>
        {
            Assert.Contains($"usuário {id}", l.Resumo);
            Assert.Contains("(Gestor)", l.Resumo);
            Assert.DoesNotContain(email, l.Resumo);
            Assert.DoesNotContain(cpf, l.Resumo);
            Assert.DoesNotMatch(@"\d{3}\.?\d{3}\.?\d{3}-?\d{2}", l.Resumo);
        });
    }

    [Fact]
    public async Task Sem_sessao_401_em_toda_rota_nova()
    {
        var anonimo = f.Cliente();
        var i = await f.Instituicao();
        var u = Equipe(i);
        var b = $"/api/plataforma/instituicoes/{i.Slug}";

        foreach (var (metodo, url) in new[] { ("GET", "/api/plataforma/modelos-de-carga"), ("POST", $"{b}/carga/modelo/padrao-esclari"), ("POST", $"{b}/carga?simular=true"), ("GET", $"{b}/setores"), ("GET", u), ("POST", u), ("POST", $"{u}/1/convite"), ("POST", $"{u}/1/desativar"), ("POST", $"{u}/1/reativar") })
        {
            var r = await anonimo.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), url) { Content = metodo == "GET" ? null : JsonContent.Create(new { }) });
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        }
    }
}
