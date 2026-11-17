using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Esclari.Api.Dados;

namespace Esclari.Api.Tests;

public class MigracaoValidadeTests
{
    const string Servidor = "Host=localhost;Port=5432;Username=postgres;Password=postgres";
    const string AntesDaValidade = "20261117131953_BuscaDeChamados";

    const string Linhas = """
        INSERT INTO instituicao (slug, nome, fuso, ativa, criada_em, ultimo_numero_chamado) VALUES ('a', 'A', 'America/Cuiaba', true, now(), 0);
        INSERT INTO categoria (instituicao_id, nome, ativa, padrao) VALUES (1, 'Geral', true, true);
        INSERT INTO artigo (instituicao_id, categoria_id, estado) VALUES (1, 1, 'ATIVO'), (1, 1, 'RASCUNHO');
        INSERT INTO documento (instituicao_id, titulo, categoria_id, estado) VALUES (1, 'Regulamento', 1, 'ATIVO'), (1, 'Calendário', 1, 'ARQUIVADO');
        """;

    static async Task Executar(string cs, string sql)
    {
        await using var con = new NpgsqlConnection(cs);
        await con.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, con);
        await cmd.ExecuteNonQueryAsync();
    }

    static async Task<List<string>> Consultar(string cs, string sql)
    {
        await using var con = new NpgsqlConnection(cs);
        await con.OpenAsync();
        await using var r = await new NpgsqlCommand(sql, con).ExecuteReaderAsync();
        var lista = new List<string>();
        while (await r.ReadAsync()) lista.Add(r.GetValue(0)?.ToString() ?? "");
        return lista;
    }

    static async Task Com(Func<string, Task> prova)
    {
        var banco = "esclari_mig_" + Guid.NewGuid().ToString("N")[..12];
        var cs = $"{Servidor};Database={banco}";
        await Executar(Servidor, $"CREATE DATABASE {banco}");
        try
        {
            async Task Migrar(string? alvo)
            {
                var opcoes = new DbContextOptionsBuilder<EsclariDb>().UseNpgsql(cs, n => n.UseVector()).Options;
                await using var db = new EsclariDb(opcoes);
                await db.GetService<IMigrator>().MigrateAsync(alvo);
            }
            await Migrar(AntesDaValidade);
            await Executar(cs, Linhas);
            await Migrar(null);
            await prova(cs);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await Executar(Servidor, $"DROP DATABASE IF EXISTS {banco} WITH (FORCE)");
        }
    }

    [Fact]
    public Task Linhas_existentes_ganham_validade_de_12_meses() => Com(async cs =>
    {
        var esperado = (await Consultar(cs, "SELECT to_char((current_date + interval '12 months')::date, 'YYYY-MM-DD')")).Single();

        Assert.Equal([esperado, esperado], await Consultar(cs, "SELECT to_char(valido_ate, 'YYYY-MM-DD') FROM artigo ORDER BY id"));
        Assert.Equal([esperado, esperado], await Consultar(cs, "SELECT to_char(valido_ate, 'YYYY-MM-DD') FROM documento ORDER BY id"));
        Assert.Equal(["0"], await Consultar(cs, "SELECT (SELECT count(*) FROM artigo WHERE valido_ate < current_date) + (SELECT count(*) FROM documento WHERE valido_ate < current_date)"));
    });

    [Fact]
    public Task Coluna_e_obrigatoria_sem_default() => Com(async cs =>
    {
        Assert.Equal(["artigo,date,NO,", "documento,date,NO,"], await Consultar(cs, """
            SELECT table_name || ',' || data_type || ',' || is_nullable || ',' || coalesce(column_default, '')
            FROM information_schema.columns WHERE column_name = 'valido_ate' AND table_name IN ('artigo', 'documento') ORDER BY table_name
            """));

        var artigo = await Assert.ThrowsAsync<PostgresException>(() => Executar(cs, "INSERT INTO artigo (instituicao_id, categoria_id, estado) VALUES (1, 1, 'ATIVO')"));
        var documento = await Assert.ThrowsAsync<PostgresException>(() => Executar(cs, "INSERT INTO documento (instituicao_id, titulo, categoria_id, estado) VALUES (1, 'Novo', 1, 'ATIVO')"));

        Assert.Equal((PostgresErrorCodes.NotNullViolation, "valido_ate"), (artigo.SqlState, artigo.ColumnName));
        Assert.Equal((PostgresErrorCodes.NotNullViolation, "valido_ate"), (documento.SqlState, documento.ColumnName));
    });
}
