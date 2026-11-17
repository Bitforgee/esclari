using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Esclari.Api.Dados;

namespace Esclari.Api.Tests;

public class MigracaoAtrasoTests
{
    const string Servidor = "Host=localhost;Port=5432;Username=postgres;Password=postgres";
    const string AntesDoAtraso = "20261117190700_ValidadeDoConhecimento";

    const string Linhas = """
        SET session_replication_role = replica;
        INSERT INTO chamado (instituicao_id, numero, conversa_id, solicitante_id, matricula_id, semestre, titulo, categoria_id, categoria_original_id,
            origem_categoria, setor_id, estado, reaberturas, encaminhamentos, criado_em)
        VALUES (1, 'ESC-000001', 1, 1, 1, 3, 'Boleto', 1, 1, 'IA', 1, 'ABERTO', 0, 0, now() - interval '30 days'),
               (1, 'ESC-000002', 2, 1, 1, 3, 'Matrícula', 1, 1, 'IA', 1, 'EM_ATENDIMENTO', 0, 0, now() - interval '2 days');
        """;

    const string ColunasDoChamado = """
        SELECT column_name || ',' || data_type || ',' || is_nullable || ',' || coalesce(column_default, '')
        FROM information_schema.columns WHERE table_name = 'chamado' ORDER BY column_name
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
        while (await r.ReadAsync()) lista.Add(r.IsDBNull(0) ? "null" : r.GetValue(0).ToString()!);
        return lista;
    }

    [Fact]
    public async Task Coluna_anulavel_sem_efeito_nos_existentes()
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
            await Migrar(AntesDoAtraso);
            await Executar(cs, Linhas);
            var antes = await Consultar(cs, ColunasDoChamado);
            var linhasAntes = await Consultar(cs, "SELECT numero || ',' || estado || ',' || to_char(criado_em, 'YYYY-MM-DD HH24:MI:SS') FROM chamado ORDER BY numero");

            await Migrar(null);

            var depois = await Consultar(cs, ColunasDoChamado);
            Assert.Equal(["atraso_avisado_em,timestamp with time zone,YES,"], depois.Except(antes));
            Assert.Empty(antes.Except(depois));
            Assert.Equal(antes.Count + 1, depois.Count);
            Assert.Equal(["null", "null"], await Consultar(cs, "SELECT atraso_avisado_em FROM chamado ORDER BY numero"));
            Assert.Equal(linhasAntes, await Consultar(cs, "SELECT numero || ',' || estado || ',' || to_char(criado_em, 'YYYY-MM-DD HH24:MI:SS') FROM chamado ORDER BY numero"));
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await Executar(Servidor, $"DROP DATABASE IF EXISTS {banco} WITH (FORCE)");
        }
    }
}
