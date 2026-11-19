using Microsoft.EntityFrameworkCore;
using Npgsql;
using Esclari.Api.Dados;
using Esclari.Api.Plataforma;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class TiposNoBancoTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    static async Task<string?> Estado(Func<Task> acao)
    {
        try
        {
            await acao();
            return null;
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException p)
        {
            return p.SqlState;
        }
        catch (PostgresException p)
        {
            return p.SqlState;
        }
    }

    [Fact]
    public async Task Instituicao_nova_grava_so_as_funcoes_do_atendente()
    {
        var nova = new NovaInstituicao(Semente.NovoSlug(), "Faculdade Nova", "America/Cuiaba", Cpf.Ler(Semente.NovoCpf()), "Gestora", $"{Guid.NewGuid():N}@teste.esclari");
        var (instituicao, _) = await f.Db(null, db => Criacao.Gravar(db, nova, f.Relogio.GetUtcNow().UtcDateTime, null));

        var tipos = await f.Db(instituicao.Id, db => db.TiposDeUsuario.Include(t => t.Funcoes).ToListAsync());

        Assert.Equal(["ALUNO", "ATENDENTE", "GESTOR"], tipos.Select(t => t.Sistema).Order());
        Assert.Empty(tipos.Single(t => t.Sistema == "ALUNO").Funcoes);
        Assert.Empty(tipos.Single(t => t.Sistema == "GESTOR").Funcoes);
        Assert.Equal(["fila.atender", "indicadores.ver", "mcp.usar"], tipos.Single(t => t.Sistema == "ATENDENTE").Funcoes.Select(x => x.Funcao).Order());
        var admin = await f.Db(instituicao.Id, db => db.Usuarios.Include(u => u.Tipo).SingleAsync());
        Assert.Equal("GESTOR", admin.Tipo!.Sistema);
    }

    [Fact]
    public async Task Nome_unico_por_instituicao_sem_diferenciar_maiusculas()
    {
        var a = await f.Instituicao();
        var b = await f.Instituicao();
        await f.Tipo(a, "Coordenador de Curso");

        var repetido = await Estado(() => f.Tipo(a, "coordenador DE curso"));
        var outra = await Estado(() => f.Tipo(b, "coordenador DE curso"));

        Assert.Equal("23505", repetido);
        Assert.Null(outra);
    }

    [Fact]
    public async Task Sistema_unico_por_instituicao()
    {
        var a = await f.Instituicao();
        var b = await f.Instituicao();
        await f.Db(b.Id, db => db.TiposDeUsuario.Where(t => t.Sistema == "GESTOR").ExecuteDeleteAsync());

        var segundo = await Estado(() => f.Db(a.Id, async db =>
        {
            db.TiposDeUsuario.Add(TipoDeUsuario.Criar("Outro gestor", "GESTOR", []));
            await db.SaveChangesAsync();
        }));
        var dois = await Estado(async () =>
        {
            await f.Tipo(a, "Comum 1");
            await f.Tipo(a, "Comum 2");
        });
        var emOutra = await Estado(() => f.Db(b.Id, async db =>
        {
            db.TiposDeUsuario.Add(TipoDeUsuario.Criar("Gestor da B", "GESTOR", []));
            await db.SaveChangesAsync();
        }));

        Assert.Equal("23505", segundo);
        Assert.Null(dois);
        Assert.Null(emOutra);
    }

    [Fact]
    public async Task Usuario_exige_tipo()
    {
        var i = await f.Instituicao();
        await using var con = new NpgsqlConnection(f.ConnectionString);
        await con.OpenAsync();
        await using var coluna = new NpgsqlCommand("SELECT data_type, is_nullable FROM information_schema.columns WHERE table_name = 'usuario' AND column_name = 'tipo_usuario_id'", con);
        await using (var r = await coluna.ExecuteReaderAsync())
        {
            Assert.True(await r.ReadAsync());
            Assert.Equal(("bigint", "NO"), (r.GetString(0), r.GetString(1)));
        }

        var estado = await Estado(async () =>
        {
            await using var cmd = new NpgsqlCommand($"INSERT INTO usuario (instituicao_id, cpf, nome, email, deve_trocar_senha, ativo, tipo_usuario_id) VALUES ({i.Id}, '{Semente.NovoCpf()}', 'Sem tipo', 'sem@teste.esclari', false, true, NULL)", con);
            await cmd.ExecuteNonQueryAsync();
        });

        Assert.Equal("23502", estado);
    }

    [Fact]
    public async Task Tipo_em_uso_nao_apaga()
    {
        var i = await f.Instituicao();
        var tipo = await f.Tipo(i, "Em uso", "indicadores.ver");
        var u = await f.Usuario(i, [], tipo: tipo.Nome);

        var estado = await Estado(() => f.Db(i.Id, db => db.TiposDeUsuario.Where(t => t.Id == tipo.Id).ExecuteDeleteAsync()));

        Assert.Equal("23001", estado);
        Assert.Equal(tipo.Id, await f.Db(i.Id, db => db.Usuarios.Where(x => x.Id == u.Id).Select(x => x.TipoUsuarioId).SingleAsync()));
    }

    [Fact]
    public async Task Funcao_unica_por_tipo_e_some_com_o_tipo()
    {
        var i = await f.Instituicao();
        var a = await f.Tipo(i, "A", "tipos.gerenciar");
        var b = await f.Tipo(i, "B", "tipos.gerenciar");

        var repetida = await Estado(() => f.Db(i.Id, db => db.Database.ExecuteSqlRawAsync($"INSERT INTO tipo_usuario_funcao (instituicao_id, tipo_usuario_id, funcao) VALUES ({i.Id}, {a.Id}, 'tipos.gerenciar')")));

        Assert.Equal("23505", repetida);
        Assert.Equal(2, await f.Db(i.Id, db => db.FuncoesDoTipo.CountAsync(x => x.Funcao == "tipos.gerenciar")));
        Assert.Equal([a.Id, b.Id], (await f.Db(i.Id, db => db.FuncoesDoTipo.Where(x => x.Funcao == "tipos.gerenciar").Select(x => x.TipoUsuarioId).ToListAsync())).Order());

        await f.Db(i.Id, db => db.TiposDeUsuario.Where(t => t.Id == a.Id).ExecuteDeleteAsync());

        Assert.Equal([b.Id], await f.Db(i.Id, db => db.FuncoesDoTipo.Where(x => x.Funcao == "tipos.gerenciar").Select(x => x.TipoUsuarioId).ToListAsync()));
    }
}