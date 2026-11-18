using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Esclari.Api.Dados;

namespace Esclari.Api.Tests;

public class MigracaoOperadorTests
{
    const string Servidor = "Host=localhost;Port=5432;Username=postgres;Password=postgres";
    const string AntesDaLimpeza = "20261118172453_OperadoresDaPlataforma";

    const string Linhas = """
        SET session_replication_role = replica;
        INSERT INTO usuario_papel (instituicao_id, usuario_id, papel)
        VALUES (1, 1, 'ALUNO'), (1, 2, 'ATENDENTE'), (1, 3, 'ADMINISTRADOR'), (1, 3, 'OPERADOR'), (1, 4, 'OPERADOR');
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
        while (await r.ReadAsync()) lista.Add(r.GetValue(0).ToString()!);
        return lista;
    }

    [Fact]
    public async Task Apaga_so_as_linhas_operador()
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
            await Migrar(AntesDaLimpeza);
            await Executar(cs, Linhas);
            Assert.Equal(["ADMINISTRADOR", "ALUNO", "ATENDENTE", "OPERADOR", "OPERADOR"], await Consultar(cs, "SELECT papel FROM usuario_papel ORDER BY papel"));

            await Migrar(null);

            Assert.Equal(["ADMINISTRADOR", "ALUNO", "ATENDENTE"], await Consultar(cs, "SELECT papel FROM usuario_papel ORDER BY papel"));
            Assert.Equal(["ux_operador_plataforma_email"], await Consultar(cs, "SELECT indexname FROM pg_indexes WHERE tablename = 'operador_plataforma' AND indexname = 'ux_operador_plataforma_email'"));
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await Executar(Servidor, $"DROP DATABASE IF EXISTS {banco} WITH (FORCE)");
        }
    }
}
