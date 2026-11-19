using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Esclari.Api.Dados;
using Esclari.Api.Plataforma;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class PlataformaAuditoriaTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    const string Lista = "/api/plataforma/instituicoes";

    static MultipartFormDataContent Carga()
    {
        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("{\"setores\":[{\"nome\":\"Financeiro\"}]}")), "arquivo", "carga.json");
        return form;
    }

    Task<List<AuditoriaPlataforma>> Linhas(long operadorId) => f.Db(null, db => db.AuditoriaDaPlataforma.AsNoTracking().Where(a => a.OperadorId == operadorId).OrderBy(a => a.Id).ToListAsync());

    [Fact]
    public async Task Cada_acao_grava_uma_linha_na_mesma_transacao()
    {
        var op = await f.Operador();
        var c = op.Cliente;
        var slug = Semente.NovoSlug();
        var cpf = Semente.NovoCpf();
        await c.PostAsJsonAsync(Lista, new { slug, nome = "Faculdade Auditada", fuso = "America/Cuiaba", admin = new { cpf, nome = "Dona Admin", email = $"{Guid.NewGuid():N}@teste.edu.br" } });
        await c.PutAsJsonAsync($"{Lista}/{slug}", new { nome = "Faculdade Auditada Nova", fuso = "America/Sao_Paulo" });
        await c.PostAsync($"{Lista}/{slug}/carga", Carga());
        await c.PostAsync($"{Lista}/{slug}/reenviar-acesso", null);
        await c.PostAsJsonAsync($"{Lista}/{slug}/desativar", new { motivo = "Contrato encerrado" });
        await c.PostAsync($"{Lista}/{slug}/reativar", null);
        await c.PutAsJsonAsync($"{Lista}/{slug}/parametros", new { confirmacaoDias = 5 });
        var membro = await c.PostAsJsonAsync($"{Lista}/{slug}/equipe", new { cpf = Semente.NovoCpf(), nome = "Ana Equipe", email = $"{Guid.NewGuid():N}@teste.edu.br", tipo = TipoDeUsuario.Gestor });
        var membroId = (await membro.Json()).GetProperty("id").GetInt64();
        await c.PostAsync($"{Lista}/{slug}/equipe/{membroId}/desativar", null);
        var (saida, erro) = (new StringWriter(), new StringWriter());
        Assert.Equal(0, await PlataformaComando.Executar(f.Services, ["operador", "--email", op.Operador.Email, "--redefinir-2fa"], saida, erro));

        var linhas = await Linhas(op.Operador.Id);
        var alvo = await f.Db(null, db => db.Instituicoes.Where(i => i.Slug == slug).Select(i => i.Id).SingleAsync());

        Assert.Equal(AcoesDaPlataforma.Todas.Order(), linhas.Select(l => l.Acao).Order());
        Assert.All(linhas, l => Assert.NotEqual("", l.Resumo));
        Assert.All(linhas.Where(l => l.Acao.StartsWith("INSTITUICAO_") || l.Acao is "ACESSO_REENVIADO" or "CARGA_IMPORTADA" or "USUARIO_CRIADO" or "USUARIO_ALTERADO" or "PARAMETROS_ALTERADOS"), l => Assert.Equal(alvo, l.InstituicaoAlvoId));
        Assert.All(linhas.Where(l => l.Acao.StartsWith("OPERADOR_")), l => Assert.Null(l.InstituicaoAlvoId));
    }

    [Fact]
    public async Task Resumo_da_equipe_traz_o_nome_do_tipo()
    {
        var op = await f.Operador();
        var c = op.Cliente;
        var slug = Semente.NovoSlug();
        await c.PostAsJsonAsync(Lista, new { slug, nome = "Faculdade Auditada", fuso = "America/Cuiaba", admin = new { cpf = Semente.NovoCpf(), nome = "Dona Admin", email = $"{Guid.NewGuid():N}@teste.edu.br" } });
        var instituicao = await f.Db(null, db => db.Instituicoes.SingleAsync(i => i.Slug == slug));
        var setor = await f.Db(instituicao.Id, db => db.Setores.Select(s => s.Id).FirstAsync());
        var cpfGestor = Semente.NovoCpf();
        var emailGestor = $"{Guid.NewGuid():N}@teste.edu.br";
        var gestor = (await (await c.PostAsJsonAsync($"{Lista}/{slug}/equipe", new { cpf = cpfGestor, nome = "Ana Gestora", email = emailGestor, tipo = TipoDeUsuario.Gestor })).Json()).GetProperty("id").GetInt64();
        var atendente = (await (await c.PostAsJsonAsync($"{Lista}/{slug}/equipe", new { cpf = Semente.NovoCpf(), nome = "Beto Atendente", email = $"{Guid.NewGuid():N}@teste.edu.br", tipo = TipoDeUsuario.Atendente, setores = new[] { setor } })).Json()).GetProperty("id").GetInt64();
        await c.PostAsync($"{Lista}/{slug}/equipe/{gestor}/convite", null);
        await c.PostAsync($"{Lista}/{slug}/equipe/{atendente}/desativar", null);
        await c.PostAsync($"{Lista}/{slug}/equipe/{atendente}/reativar", null);

        var linhas = await f.Db(null, db => db.AuditoriaDaPlataforma.Where(a => a.InstituicaoAlvoId == instituicao.Id && (a.Acao == AcoesDaPlataforma.UsuarioCriado || a.Acao == AcoesDaPlataforma.UsuarioAlterado)).OrderBy(a => a.Id).ToListAsync());

        Assert.Equal(11, AcoesDaPlataforma.Todas.Count);
        Assert.Equal([AcoesDaPlataforma.UsuarioCriado, AcoesDaPlataforma.UsuarioCriado, AcoesDaPlataforma.UsuarioAlterado, AcoesDaPlataforma.UsuarioAlterado, AcoesDaPlataforma.UsuarioAlterado], linhas.Select(l => l.Acao));
        Assert.Equal(["(Gestor)", "(Atendente)", "(Gestor)", "(Atendente)", "(Atendente)"], linhas.Select(l => l.Resumo[l.Resumo.IndexOf('(')..]));
        Assert.All(linhas, l =>
        {
            Assert.DoesNotContain(cpfGestor, l.Resumo);
            Assert.DoesNotContain(emailGestor, l.Resumo);
            Assert.DoesNotContain("ADMINISTRADOR", l.Resumo);
        });
    }
    [Fact]
    public async Task Acao_que_volta_nao_deixa_linha()
    {
        var op = await f.Operador();
        var existente = await f.Instituicao();
        var antes = (await Linhas(op.Operador.Id)).Count;

        var repetido = await op.Cliente.PostAsJsonAsync(Lista, new { slug = existente.Slug, nome = "Faculdade", fuso = "America/Cuiaba", admin = new { cpf = Semente.NovoCpf(), nome = "Admin", email = "a@teste.edu.br" } });
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);

        f.Commit.Falhar = true;
        try { await op.Cliente.PostAsJsonAsync(Lista, new { slug = "demo-auditoria", nome = "Faculdade", fuso = "America/Cuiaba", admin = new { cpf = Semente.NovoCpf(), nome = "Admin", email = "a@teste.edu.br" } }); }
        catch (Exception) { }
        finally { f.Commit.Falhar = false; }

        Assert.Equal(antes, (await Linhas(op.Operador.Id)).Count);
        Assert.False(await f.Db(null, db => db.Instituicoes.AnyAsync(x => x.Slug == "demo-auditoria")));
    }

    [Fact]
    public async Task Auditoria_so_inclusao()
    {
        var rotas = f.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText!.StartsWith("/api/plataforma/auditoria"))
            .SelectMany(e => e.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods).ToList();
        var metodos = typeof(AuditoriaPlataforma).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName);
        var setters = typeof(AuditoriaPlataforma).GetProperties().Where(p => p.SetMethod is { IsPublic: true });

        Assert.Equal(["GET"], rotas);
        Assert.Empty(metodos);
        Assert.Empty(setters);
    }

    [Fact]
    public async Task Resumo_sem_dado_pessoal_e_ate_200_caracteres()
    {
        var op = await f.Operador();
        var slug = Semente.NovoSlug();
        var cpf = Semente.NovoCpf();
        var email = $"{Guid.NewGuid():N}@teste.edu.br";
        await op.Cliente.PostAsJsonAsync(Lista, new { slug, nome = "Faculdade Auditada", fuso = "America/Cuiaba", admin = new { cpf, nome = "Dona Admin", email } });
        await op.Cliente.PostAsync($"{Lista}/{slug}/reenviar-acesso", null);
        await op.Cliente.PutAsJsonAsync($"{Lista}/{slug}", new { nome = "Faculdade Auditada Nova", fuso = "America/Sao_Paulo" });
        var carga = new MultipartFormDataContent { { new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("{\"setores\":[{\"nome\":\"Financeiro\"}]}")), "arquivo", "carga.json" } };
        await op.Cliente.PostAsync($"{Lista}/{slug}/carga", carga);
        await op.Cliente.PostAsJsonAsync($"{Lista}/{slug}/desativar", new { motivo = new string('m', 200) });
        await op.Cliente.PostAsync($"{Lista}/{slug}/reativar", null);
        await op.Cliente.PutAsJsonAsync($"{Lista}/{slug}/parametros", new { confirmacaoDias = 5 });
        await op.Cliente.PostAsJsonAsync($"{Lista}/{slug}/desativar", new { motivo = new string('m', 200) });
        var membro = await op.Cliente.PostAsJsonAsync($"{Lista}/{slug}/equipe", new { cpf = Semente.NovoCpf(), nome = "Ana Equipe", email = $"{Guid.NewGuid():N}@teste.edu.br", tipo = TipoDeUsuario.Gestor });
        var membroId = (await membro.Json()).GetProperty("id").GetInt64();
        await op.Cliente.PostAsync($"{Lista}/{slug}/equipe/{membroId}/desativar", null);
        await PlataformaComando.Executar(f.Services, ["operador", "--email", op.Operador.Email, "--redefinir-2fa"], new StringWriter(), new StringWriter());

        var linhas = await Linhas(op.Operador.Id);
        var tokens = await f.Db(null, db => db.Tokens.IgnoreQueryFilters().Select(t => t.Hash).ToListAsync());

        Assert.Equal(AcoesDaPlataforma.Todas.Order(), linhas.Select(l => l.Acao).Distinct().Order());
        foreach (var l in linhas)
        {
            Assert.True(l.Resumo.Length <= 200, l.Acao);
            Assert.DoesNotContain(cpf, l.Resumo);
            Assert.DoesNotContain(email, l.Resumo);
            Assert.DoesNotMatch(@"\d{3}\.?\d{3}\.?\d{3}-?\d{2}", l.Resumo);
            Assert.DoesNotMatch(@"token|senha|password", l.Resumo.ToLowerInvariant());
            Assert.DoesNotContain(tokens, h => l.Resumo.Contains(h));
        }
        Assert.All(linhas.Where(l => l.Acao == "INSTITUICAO_DESATIVADA"), l => Assert.Equal(200, l.Resumo.Length));
    }

    [Fact]
    public async Task Pagina_de_50_do_mais_novo_ao_mais_antigo_com_filtro()
    {
        var op = await f.Operador();
        var alvo = await f.Instituicao();
        var outra = await f.Instituicao();
        var base_ = f.Relogio.GetUtcNow().UtcDateTime;
        await f.Db(null, async db =>
        {
            for (var n = 0; n < 55; n++) db.AuditoriaDaPlataforma.Add(AuditoriaPlataforma.Registrar(AcoesDaPlataforma.InstituicaoAlterada, op.Operador.Id, alvo.Id, $"item {n:00}", base_.AddMinutes(-60 + n)));
            db.AuditoriaDaPlataforma.Add(AuditoriaPlataforma.Registrar(AcoesDaPlataforma.InstituicaoAlterada, op.Operador.Id, outra.Id, "de outra", base_));
            await db.SaveChangesAsync();
        });

        var um = await (await op.Cliente.GetAsync($"/api/plataforma/auditoria?instituicao={alvo.Slug}&pagina=1")).Json();
        var dois = await (await op.Cliente.GetAsync($"/api/plataforma/auditoria?instituicao={alvo.Slug}&pagina=2")).Json();

        Assert.Equal(55, um.GetProperty("total").GetInt32());
        var itens = um.GetProperty("itens").EnumerateArray().ToList();
        Assert.Equal(50, itens.Count);
        Assert.Equal("item 54", itens[0].GetProperty("resumo").GetString());
        Assert.Equal("item 05", itens[^1].GetProperty("resumo").GetString());
        Assert.Equal(["acao", "operador", "instituicao", "resumo", "criadaEm"], itens[0].EnumerateObject().Select(p => p.Name));
        Assert.Equal(alvo.Slug, itens[0].GetProperty("instituicao").GetString());
        Assert.Equal(5, dois.GetProperty("itens").GetArrayLength());
        Assert.DoesNotContain(itens, i => i.GetProperty("resumo").GetString() == "de outra");
    }

    [Fact]
    public async Task Pagina_e_instituicao_invalidas_400()
    {
        var op = await f.Operador();

        Assert.Equal(HttpStatusCode.BadRequest, (await op.Cliente.GetAsync("/api/plataforma/auditoria?pagina=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await op.Cliente.GetAsync("/api/plataforma/auditoria?instituicao=nao-existe")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await op.Cliente.GetAsync("/api/plataforma/auditoria")).StatusCode);
    }

    [Fact]
    public async Task Acoes_fechadas_e_resumo_curto()
    {
        var agora = f.Relogio.GetUtcNow().UtcDateTime;

        foreach (var acao in AcoesDaPlataforma.Todas) Assert.Equal(acao, AuditoriaPlataforma.Registrar(acao, 1, null, "resumo", agora).Acao);
        Assert.Equal(11, AcoesDaPlataforma.Todas.Count);
        Assert.Throws<ArgumentException>(() => AuditoriaPlataforma.Registrar("QUALQUER_COISA", 1, null, "resumo", agora));
        Assert.Equal(200, AuditoriaPlataforma.Registrar(AcoesDaPlataforma.OperadorEntrou, 1, null, new string('r', 500), agora).Resumo.Length);
        var tamanho = await f.Db(null, db => db.Database.SqlQueryRaw<int>("SELECT character_maximum_length AS \"Value\" FROM information_schema.columns WHERE table_name = 'auditoria_plataforma' AND column_name = 'resumo'").SingleAsync());
        Assert.Equal(200, tamanho);
    }
}
