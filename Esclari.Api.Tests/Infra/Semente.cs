using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Tests.Infra;

public record Aluno(Instituicao Instituicao, Usuario Usuario, string Senha, HttpClient Cliente);

public static class Semente
{
    public const string Senha = "senha-boa-123";

    public static string NovoCpf()
    {
        var d = Enumerable.Range(0, 9).Select(_ => Random.Shared.Next(10)).ToList();
        for (var n = 9; n <= 10; n++)
        {
            var soma = 0;
            for (var i = 0; i < n; i++) soma += d[i] * (n + 1 - i);
            var r = soma * 10 % 11;
            d.Add(r == 10 ? 0 : r);
        }
        return string.Concat(d);
    }

    public static string NovoSlug() => "i" + Guid.NewGuid().ToString("N")[..10];

    public static Task<Instituicao> Instituicao(this EsclariFactory f, string? slug = null, string? nome = null, string fuso = "America/Cuiaba") =>
        f.Db(null, async db =>
        {
            var i = Esclari.Api.Dados.Instituicao.Criar(slug ?? NovoSlug(), nome ?? "Instituição de teste", fuso, f.Relogio.GetUtcNow().UtcDateTime);
            db.Instituicoes.Add(i);
            await db.SaveChangesAsync();
            db.InstituicaoAtual = i.Id;
            db.TiposDeUsuario.AddRange(new[] { TipoDeUsuario.Aluno, TipoDeUsuario.Gestor, TipoDeUsuario.Atendente }.Select(TipoDeUsuario.DeSistema));
            db.Setores.Add(Setor.CriarTriagem());
            db.Categorias.Add(Esclari.Api.Dados.Cadastros.Categoria.CriarPadrao());
            await db.SaveChangesAsync();
            return i;
        });

    public static Task<Usuario> Usuario(this EsclariFactory f, Instituicao i, string[] papeis, string? senha = Senha, string? cpf = null,
        string? email = null, string? matricula = null, string situacao = "ATIVA", bool deveTrocarSenha = false, string? tipo = null) =>
        f.Db(i.Id, async db =>
        {
            var u = Esclari.Api.Dados.Usuarios.Usuario.Criar(Cpf.Ler(cpf ?? NovoCpf()), "Pessoa " + Guid.NewGuid().ToString("N")[..6], email ?? $"{Guid.NewGuid():N}@teste.esclari");
            u.DefinirTipo(tipo is null ? await db.TipoPelosPapeis(papeis) : await db.TiposDeUsuario.Include(t => t.Funcoes).SingleAsync(t => t.Nome == tipo));
            if (senha is not null)
            {
                var hash = new PasswordHasher<Usuario>().HashPassword(u, senha);
                if (deveTrocarSenha) u.DefinirSenhaTemporaria(hash);
                else u.TrocarSenha(hash);
            }
            if (matricula is not null || papeis.Contains(Papeis.Aluno))
            {
                var curso = await db.Cursos.FirstOrDefaultAsync();
                if (curso is null)
                {
                    curso = Curso.Criar("Análise e Desenvolvimento de Sistemas");
                    db.Cursos.Add(curso);
                    await db.SaveChangesAsync();
                }
                u.AdicionarMatricula(matricula ?? Random.Shared.NextInt64(1_000_000_000, 9_999_999_999).ToString(), curso.Id, 3, situacao);
            }
            db.Usuarios.Add(u);
            await db.SaveChangesAsync();
            foreach (var papel in papeis.Distinct())
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO usuario_papel (instituicao_id, usuario_id, papel) VALUES ({i.Id}, {u.Id}, {papel})");
            return u;
        });

    public static Task<long> TipoId(this EsclariFactory f, Instituicao i, string sistema) =>
        f.Db(i.Id, db => db.TiposDeUsuario.Where(t => t.Sistema == sistema).Select(t => t.Id).SingleAsync());

    public static Task<TipoDeUsuario> Tipo(this EsclariFactory f, Instituicao i, string nome, params string[] funcoes) =>
        f.Db(i.Id, async db =>
        {
            var t = db.TiposDeUsuario.Add(TipoDeUsuario.Criar(nome, null, funcoes)).Entity;
            await db.SaveChangesAsync();
            return t;
        });

    public static async Task<Atendente> ComFuncoes(this EsclariFactory f, Instituicao i, string[] funcoes, string? matricula = null, params Setor[] setores)
    {
        var tipo = await f.Tipo(i, "Tipo " + Guid.NewGuid().ToString("N")[..8], funcoes);
        var u = await f.Usuario(i, [], tipo: tipo.Nome, matricula: matricula);
        if (setores.Length > 0) await f.Db(i.Id, db => db.VincularSetores(u.Id, [.. setores.Select(s => s.Id)]));
        return new Atendente(u, await f.Entrar(i, u));
    }

    public static async Task<HttpResponseMessage> Login(this HttpClient c, string slug, string cpf, string senha = Senha) =>
        await c.PostAsJsonAsync("/api/auth/login", new { instituicao = slug, cpf, senha });

    public static async Task<HttpClient> Entrar(this EsclariFactory f, Instituicao i, Usuario u, string senha = Senha)
    {
        var c = f.Cliente();
        var r = await c.Login(i.Slug, u.Cpf.Valor, senha);
        Assert.Equal(System.Net.HttpStatusCode.OK, r.StatusCode);
        return c;
    }

    public static async Task<Aluno> AlunoLogado(this EsclariFactory f, Instituicao? i = null)
    {
        i ??= await f.Instituicao();
        var u = await f.Usuario(i, [Papeis.Aluno]);
        return new Aluno(i, u, Senha, await f.Entrar(i, u));
    }

    public static Task<Categoria> Categoria(this EsclariFactory f, Instituicao i, string nome, bool ativa = true) =>
        f.Db(i.Id, async db =>
        {
            var c = Esclari.Api.Dados.Cadastros.Categoria.Criar(nome, ativa);
            db.Categorias.Add(c);
            await db.SaveChangesAsync();
            return c;
        });

    public static long Fonte(long artigoId) => 1_000_000 + artigoId;

    public static Validade Validade(this EsclariFactory f) => new(DateOnly.FromDateTime(f.Relogio.GetUtcNow().UtcDateTime), 12);

    public static Task<Artigo> Artigo(this EsclariFactory f, Instituicao i, string titulo, string resposta, string[]? links = null,
        string estado = EstadosArtigo.Ativo, long? categoriaId = null) =>
        f.Db(i.Id, async db =>
        {
            var versao = new VersaoArtigo { Numero = 1, Titulo = titulo, Perguntas = [titulo + "?"], Resposta = resposta, Links = [.. links ?? []], CriadaEm = f.Relogio.GetUtcNow().UtcDateTime };
            var a = Esclari.Api.Dados.Artigos.Artigo.Criar(categoriaId ?? (await db.Categorias.FirstAsync()).Id, null, null, versao, f.Validade());
            if (estado == EstadosArtigo.Ativo) a.Ativar();
            else if (estado == EstadosArtigo.Arquivado) a.Arquivar();
            db.Artigos.Add(a);
            await db.SaveChangesAsync();
            if (estado == EstadosArtigo.Ativo)
            {
                var trecho = TrechoFonte.DoArtigo(versao, f.Relogio.GetUtcNow().UtcDateTime, new Pgvector.Vector(LlmFalso.Vetor(versao.TextoParaBusca()))).ComId(Fonte(a.Id));
                db.TrechosFonte.Add(trecho);
                await db.SaveChangesAsync();
            }
            return a;
        });

    public static async Task<JsonElement> Json(this HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
}
