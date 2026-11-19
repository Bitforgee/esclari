using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class FuncoesDoTipoTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    static readonly string[] Doze =
    [
        "assistente.usar", "fila.atender", "chamados.ver_todos", "indicadores.ver", "conhecimento.gerenciar", "cadastros.gerenciar",
        "usuarios.gerenciar", "matriculas.importar", "tipos.gerenciar", "aparencia.gerenciar", "integracoes.gerenciar", "mcp.usar",
    ];

    static Task<Usuario> Carregar(EsclariFactory f, Instituicao i, long id) =>
        f.Db(i.Id, db => db.Usuarios.ComTipo().SingleAsync(u => u.Id == id));

    [Fact]
    public void Catalogo_tem_os_12_codigos_e_recusa_outro()
    {
        Assert.Equal(Doze, Funcoes.Todas);
        Assert.Equal(12, Funcoes.Todas.Distinct().Count());
        Assert.All(Doze, c => Assert.True(Funcoes.Valida(c), c));
        Assert.All(new string?[] { "relatorios.ver", "FILA.ATENDER", "", null }, c => Assert.False(Funcoes.Valida(c), c ?? "null"));
    }

    [Fact]
    public async Task Gestor_calcula_todas_menos_assistente_usar()
    {
        var i = await f.Instituicao();
        var gestor = await f.Usuario(i, [Papeis.Administrador]);

        var u = await Carregar(f, i, gestor.Id);

        string[] dez = ["aparencia.gerenciar", "cadastros.gerenciar", "chamados.ver_todos", "conhecimento.gerenciar", "indicadores.ver", "integracoes.gerenciar", "matriculas.importar", "mcp.usar", "tipos.gerenciar", "usuarios.gerenciar"];
        Assert.Equal(dez, u.FuncoesEfetivas());
        Assert.Contains("tipos.gerenciar", u.FuncoesEfetivas());
        Assert.Contains("usuarios.gerenciar", u.FuncoesEfetivas());
        Assert.Empty(await f.Db(i.Id, db => db.FuncoesDoTipo.Where(x => db.TiposDeUsuario.Any(t => t.Id == x.TipoUsuarioId && t.Sistema == "GESTOR")).ToListAsync()));
    }

    [Fact]
    public async Task Aluno_calcula_so_assistente_usar()
    {
        var i = await f.Instituicao();
        var aluno = await f.Usuario(i, [Papeis.Aluno]);

        var comMatricula = await Carregar(f, i, aluno.Id);
        var semMatricula = Usuario.Criar(Cpf.Ler(Semente.NovoCpf()), "Sem matrícula", "sem@teste.esclari");
        semMatricula.DefinirTipo(TipoDeUsuario.DeSistema("ALUNO"));

        Assert.Equal(["assistente.usar"], comMatricula.FuncoesEfetivas());
        Assert.Equal(["assistente.usar"], semMatricula.FuncoesEfetivas());
        Assert.Empty(await f.Db(i.Id, db => db.FuncoesDoTipo.Where(x => db.TiposDeUsuario.Any(t => t.Id == x.TipoUsuarioId && t.Sistema == "ALUNO")).ToListAsync()));
    }

    [Fact]
    public async Task Fila_atender_exige_setor()
    {
        var i = await f.Instituicao();
        var setor = await f.Setor(i, "Financeiro");
        var proprio = await f.Tipo(i, "Plantonista", "fila.atender", "indicadores.ver");
        var atendente = await f.Usuario(i, [Papeis.Atendente]);
        var doTipoProprio = await f.Usuario(i, [], tipo: proprio.Nome);
        var gestor = await f.Usuario(i, [Papeis.Administrador]);
        long[] todos = [atendente.Id, doTipoProprio.Id, gestor.Id];

        foreach (var id in todos) Assert.DoesNotContain("fila.atender", (await Carregar(f, i, id)).FuncoesEfetivas());
        Assert.Contains("indicadores.ver", (await Carregar(f, i, doTipoProprio.Id)).FuncoesEfetivas());

        await f.Db(i.Id, async db => { foreach (var id in todos) await db.VincularSetores(id, setor.Id); });

        foreach (var id in todos) Assert.Contains("fila.atender", (await Carregar(f, i, id)).FuncoesEfetivas());
    }
}