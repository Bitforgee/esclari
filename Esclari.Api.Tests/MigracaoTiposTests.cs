using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Esclari.Api.Dados;

namespace Esclari.Api.Tests;

public class MigracaoTiposTests
{
    const string Servidor = "Host=localhost;Port=5432;Username=postgres;Password=postgres";
    const string Anterior = "20261118195644_ParametrosPorInstituicao";
    const string Nova = "TiposDeUsuario";

    sealed class BancoMigrado(string nome) : IAsyncDisposable
    {
        public string Nome { get; } = nome;
        public string Cs => $"{Servidor};Database={Nome}";
        public List<string> Avisos { get; } = [];

        public static async Task<BancoMigrado> AteAnterior()
        {
            var b = new BancoMigrado("esclari_mig_" + Guid.NewGuid().ToString("N")[..12]);
            await Executar(Servidor, $"CREATE DATABASE {b.Nome}");
            await b.Migrar(Anterior);
            return b;
        }

        public async Task Migrar(string? alvo)
        {
            await using var con = new NpgsqlConnection(Cs);
            con.Notice += (_, e) => Avisos.Add(e.Notice.MessageText);
            var opcoes = new DbContextOptionsBuilder<EsclariDb>().UseNpgsql(con, n => n.UseVector()).Options;
            await using var db = new EsclariDb(opcoes);
            await db.GetService<IMigrator>().MigrateAsync(alvo);
        }

        public async ValueTask DisposeAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await Executar(Servidor, $"DROP DATABASE IF EXISTS {Nome} WITH (FORCE)");
        }
    }

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
        while (await r.ReadAsync()) lista.Add(string.Join("|", Enumerable.Range(0, r.FieldCount).Select(i => r.IsDBNull(i) ? "-" : r.GetValue(i).ToString())));
        return lista;
    }

    static string Instituicao(long id) =>
        $"INSERT INTO instituicao (id, slug, nome, fuso, ativa, criada_em) VALUES ({id}, 'i{id}', 'Instituição {id}', 'America/Cuiaba', true, now());";

    static string Usuario(long id, long instituicao, params string[] papeis) =>
        $"INSERT INTO usuario (id, instituicao_id, cpf, nome, email, deve_trocar_senha, ativo) VALUES ({id}, {instituicao}, '{id:00000000000}', 'Pessoa {id}', 'p{id}@teste.esclari', false, true);" +
        string.Concat(papeis.Select(p => $"INSERT INTO usuario_papel (instituicao_id, usuario_id, papel) VALUES ({instituicao}, {id}, '{p}');"));

    static string Setor(long id, long instituicao) =>
        $"INSERT INTO setor (id, instituicao_id, nome, triagem, ativo) VALUES ({id}, {instituicao}, 'Setor {id}', false, true);";

    static string Atende(long usuario, long setor, long instituicao) =>
        $"INSERT INTO atendente_setor (instituicao_id, usuario_id, setor_id) VALUES ({instituicao}, {usuario}, {setor});";

    static string Matricula(long id, long usuario, long curso, long instituicao) =>
        $"INSERT INTO matricula (id, instituicao_id, usuario_id, curso_id, numero, semestre, situacao) VALUES ({id}, {instituicao}, {usuario}, {curso}, '{id:0000000000}', 3, 'ATIVA');";

    static string Curso(long id, long instituicao) => $"INSERT INTO curso (id, instituicao_id, nome) VALUES ({id}, {instituicao}, 'Curso {id}');";

    static Task<List<string>> TipoDosUsuarios(BancoMigrado b) =>
        Consultar(b.Cs, "SELECT u.id, coalesce(t.sistema, t.nome) FROM usuario u JOIN tipo_usuario t ON t.id = u.tipo_usuario_id ORDER BY u.id");

    [Fact]
    public async Task Ordem_de_conversao_por_papeis()
    {
        await using var b = await BancoMigrado.AteAnterior();
        await Executar(b.Cs, Instituicao(1) +
            Usuario(1, 1, "ADMINISTRADOR") + Usuario(2, 1, "ADMINISTRADOR", "ATENDENTE") + Usuario(3, 1, "ADMINISTRADOR", "ALUNO") +
            Usuario(4, 1, "ADMINISTRADOR", "ALUNO", "ATENDENTE") + Usuario(5, 1, "ALUNO", "ATENDENTE") + Usuario(6, 1, "ATENDENTE") +
            Usuario(7, 1, "ALUNO") + Usuario(8, 1));

        await b.Migrar(null);

        Assert.Equal(
            ["1|GESTOR", "2|GESTOR", "3|GESTOR", "4|GESTOR", "5|Líder de semestre", "6|ATENDENTE", "7|ALUNO", "8|ALUNO"],
            await TipoDosUsuarios(b));
    }

    [Fact]
    public async Task Preserva_setores_matriculas_e_usuario_papel()
    {
        await using var b = await BancoMigrado.AteAnterior();
        await Executar(b.Cs, Instituicao(1) + Instituicao(2) + Curso(1, 1) + Setor(1, 1) + Setor(2, 1) + Setor(3, 2) +
            Usuario(1, 1, "ATENDENTE") + Usuario(2, 1, "ALUNO", "ATENDENTE") + Usuario(3, 2, "ADMINISTRADOR") +
            Atende(1, 1, 1) + Atende(1, 2, 1) + Atende(2, 1, 1) + Atende(3, 3, 2) +
            Matricula(1, 2, 1, 1) + Matricula(2, 2, 1, 1) + Matricula(3, 1, 1, 1));
        var setores = await Consultar(b.Cs, "SELECT id, instituicao_id, usuario_id, setor_id FROM atendente_setor ORDER BY id");
        var matriculas = await Consultar(b.Cs, "SELECT id, usuario_id, curso_id, numero, situacao FROM matricula ORDER BY id");
        var papeis = await Consultar(b.Cs, "SELECT id, usuario_id, papel FROM usuario_papel ORDER BY id");
        Assert.Equal((4, 3, 4), (setores.Count, matriculas.Count, papeis.Count));

        await b.Migrar(null);

        Assert.Equal(setores, await Consultar(b.Cs, "SELECT id, instituicao_id, usuario_id, setor_id FROM atendente_setor ORDER BY id"));
        Assert.Equal(matriculas, await Consultar(b.Cs, "SELECT id, usuario_id, curso_id, numero, situacao FROM matricula ORDER BY id"));
        Assert.Equal(papeis, await Consultar(b.Cs, "SELECT id, usuario_id, papel FROM usuario_papel ORDER BY id"));
        Assert.Equal(["1|ALUNO", "1|ATENDENTE", "1|GESTOR", "2|ALUNO", "2|ATENDENTE", "2|GESTOR"],
            await Consultar(b.Cs, "SELECT instituicao_id, sistema FROM tipo_usuario WHERE sistema IS NOT NULL ORDER BY instituicao_id, sistema"));
    }

    [Fact]
    public async Task Avisa_o_total_de_aluno_com_administrador_por_instituicao()
    {
        await using var b = await BancoMigrado.AteAnterior();
        await Executar(b.Cs, Instituicao(41) + Instituicao(42) +
            Usuario(1, 41, "ALUNO", "ADMINISTRADOR") + Usuario(2, 41, "ALUNO", "ADMINISTRADOR") + Usuario(3, 41, "ADMINISTRADOR") +
            Usuario(4, 42, "ALUNO") + Usuario(5, 42, "ADMINISTRADOR"));

        await b.Migrar(null);

        var avisos = b.Avisos.Where(a => a.Contains("41") || a.Contains("42")).ToList();
        var aviso = Assert.Single(avisos);
        Assert.Contains("41", aviso);
        Assert.Matches(@"\b2\b", aviso);
        Assert.DoesNotContain("42", aviso);
    }

    [Fact]
    public async Task Lider_de_semestre_nasce_com_as_quatro_funcoes()
    {
        await using var b = await BancoMigrado.AteAnterior();
        await Executar(b.Cs, Instituicao(1) + Curso(1, 1) + Setor(1, 1) +
            Usuario(1, 1, "ALUNO", "ATENDENTE") + Usuario(2, 1, "ALUNO", "ATENDENTE") + Usuario(3, 1, "ALUNO", "ATENDENTE", "ADMINISTRADOR") +
            Atende(1, 1, 1) + Atende(2, 1, 1) + Matricula(1, 1, 1, 1) + Matricula(2, 2, 1, 1));

        await b.Migrar(null);

        Assert.Equal(["Líder de semestre|-"], await Consultar(b.Cs, "SELECT nome, sistema FROM tipo_usuario WHERE sistema IS NULL"));
        Assert.Equal(["assistente.usar", "fila.atender", "indicadores.ver", "mcp.usar"],
            await Consultar(b.Cs, "SELECT f.funcao FROM tipo_usuario_funcao f JOIN tipo_usuario t ON t.id = f.tipo_usuario_id WHERE t.sistema IS NULL ORDER BY f.funcao"));
        Assert.Equal(["1|Líder de semestre", "2|Líder de semestre", "3|GESTOR"], await TipoDosUsuarios(b));
        Assert.Equal(["1", "2"], await Consultar(b.Cs, "SELECT usuario_id FROM atendente_setor ORDER BY usuario_id"));
        Assert.Equal(["1", "2"], await Consultar(b.Cs, "SELECT usuario_id FROM matricula ORDER BY usuario_id"));
    }

    [Fact]
    public async Task Sem_lider_nao_cria_o_tipo()
    {
        await using var b = await BancoMigrado.AteAnterior();
        await Executar(b.Cs, Instituicao(1) + Instituicao(2) +
            Usuario(1, 1, "ADMINISTRADOR") + Usuario(2, 1, "ATENDENTE") + Usuario(3, 1, "ALUNO", "ATENDENTE", "ADMINISTRADOR") + Usuario(4, 1, "ALUNO") +
            Usuario(5, 2, "ALUNO", "ATENDENTE"));

        await b.Migrar(null);

        Assert.Equal(["1|3", "2|4"], await Consultar(b.Cs, "SELECT instituicao_id, count(*) FROM tipo_usuario GROUP BY instituicao_id ORDER BY instituicao_id"));
        Assert.Empty(await Consultar(b.Cs, "SELECT 1 FROM tipo_usuario WHERE instituicao_id = 1 AND nome = 'Líder de semestre'"));
        Assert.Equal(["5|Líder de semestre"], (await TipoDosUsuarios(b)).Where(l => l.StartsWith("5|")).ToList());
    }

    [Fact]
    public async Task Falha_no_meio_desfaz_tudo()
    {
        await using var b = await BancoMigrado.AteAnterior();
        await Executar(b.Cs, Instituicao(1) + Usuario(1, 1, "ADMINISTRADOR") +
            "SET session_replication_role = replica; INSERT INTO usuario (id, instituicao_id, cpf, nome, email, deve_trocar_senha, ativo) VALUES (99, 777, '00000000099', 'Órfão', 'orfao@teste.esclari', false, true);");

        await Assert.ThrowsAnyAsync<Exception>(() => b.Migrar(null));

        Assert.Equal([""], await Consultar(b.Cs, "SELECT coalesce(to_regclass('tipo_usuario')::text, '')"));
        Assert.Empty(await Consultar(b.Cs, $"SELECT 1 FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '%_{Nova}'"));
        Assert.Empty(await Consultar(b.Cs, "SELECT 1 FROM information_schema.columns WHERE table_name = 'usuario' AND column_name = 'tipo_usuario_id'"));
    }

    [Fact]
    public async Task Snapshot_acompanha_o_modelo()
    {
        await using var b = await BancoMigrado.AteAnterior();
        var opcoes = new DbContextOptionsBuilder<EsclariDb>().UseNpgsql(b.Cs, n => n.UseVector()).Options;
        await using var db = new EsclariDb(opcoes);
        await db.Database.MigrateAsync();

        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Contains(db.Model.GetEntityTypes(), e => e.GetTableName() == "usuario_papel");
        Assert.Equal(["usuario_papel"], await Consultar(b.Cs, "SELECT table_name FROM information_schema.tables WHERE table_name = 'usuario_papel'"));
    }
}
