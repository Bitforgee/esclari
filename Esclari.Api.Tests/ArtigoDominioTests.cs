using Esclari.Api.Dados;

namespace Esclari.Api.Tests;

public class ArtigoDominioTests
{
    static readonly DateTime Agora = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
    static readonly Validade Ano = new(new DateOnly(2026, 11, 2), 12);

    static VersaoArtigo Versao(int numero) => new() { Numero = numero, Titulo = $"t{numero}", Perguntas = ["p?"], Resposta = "r", Links = [], CriadaEm = Agora };

    static Artigo Rascunho() => Artigo.Criar(10, null, null, Versao(1), Ano);

    [Fact]
    public void Cada_transicao_como_a_base()
    {
        var a = Artigo.Criar(10, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), Versao(1), Ano);
        Assert.Equal(EstadosArtigo.Rascunho, a.Estado);
        Assert.Equal(10, a.CategoriaId);
        Assert.Equal((new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), (a.VisivelDe, a.VisivelAte));
        Assert.Equal(1, Assert.Single(a.Versoes).Numero);

        a.NovaVersao(Versao(2), 11, null, new DateOnly(2027, 1, 1), Ano);
        Assert.Equal([1, 2], a.Versoes.Select(v => v.Numero));
        Assert.Equal(11, a.CategoriaId);
        Assert.Equal((null, new DateOnly(2027, 1, 1)), (a.VisivelDe, a.VisivelAte));
        Assert.Equal(EstadosArtigo.Rascunho, a.Estado);

        Assert.Null(a.Ativar());
        Assert.Equal(EstadosArtigo.Ativo, a.Estado);
        Assert.Null(a.Arquivar());
        Assert.Equal(EstadosArtigo.Arquivado, a.Estado);
        Assert.Null(a.Ativar());
        Assert.Equal(EstadosArtigo.Ativo, a.Estado);

        var rascunho = Rascunho();
        Assert.Null(rascunho.Arquivar());
        Assert.Equal(EstadosArtigo.Arquivado, rascunho.Estado);
    }

    [Fact]
    public void Ativar_ativo_e_arquivar_arquivado_recusam()
    {
        var ativo = Rascunho();
        ativo.Ativar();
        Assert.Equal("O artigo já está ativo.", ativo.Ativar()?.Motivo);
        Assert.Equal(EstadosArtigo.Ativo, ativo.Estado);

        var arquivado = Rascunho();
        arquivado.Arquivar();
        Assert.Equal("O artigo já está arquivado.", arquivado.Arquivar()?.Motivo);
        Assert.Equal(EstadosArtigo.Arquivado, arquivado.Estado);
        Assert.Single(arquivado.Versoes);
    }

    [Fact]
    public void Versao_sem_numero_maior_lanca()
    {
        var a = Rascunho();
        a.NovaVersao(Versao(2), 10, null, null, Ano);
        foreach (var numero in new[] { 2, 1 })
        {
            Assert.Throws<InvalidOperationException>(() => a.NovaVersao(Versao(numero), 99, null, null, Ano));
            Assert.Equal([1, 2], a.Versoes.Select(v => v.Numero));
            Assert.Equal(10, a.CategoriaId);
        }
        a.NovaVersao(Versao(3), 10, null, null, Ano);
        Assert.Equal([1, 2, 3], a.Versoes.Select(v => v.Numero));
        Assert.Throws<InvalidOperationException>(() => Artigo.Criar(10, null, null, Versao(2), Ano));
    }

    [Fact]
    public void Do_chamado_nasce_rascunho_com_a_origem()
    {
        var validade = new Validade(new DateOnly(2026, 11, 17), 12);

        var a = Artigo.DoChamado(7, 10, Versao(1), validade);

        Assert.Equal(EstadosArtigo.Rascunho, a.Estado);
        Assert.Equal(7, a.OrigemChamadoId);
        Assert.Equal(10, a.CategoriaId);
        Assert.Equal((null, null), (a.VisivelDe, a.VisivelAte));
        Assert.Equal(validade.Ate, a.ValidoAte);
        Assert.Equal(new DateOnly(2027, 11, 17), a.ValidoAte);
        Assert.Equal(1, Assert.Single(a.Versoes).Numero);
        Assert.Null(Rascunho().OrigemChamadoId);
        Assert.Throws<InvalidOperationException>(() => Artigo.DoChamado(7, 10, Versao(2), validade));
    }

    [Fact]
    public void Nova_versao_refaz_a_validade()
    {
        var a = Artigo.Criar(10, null, null, Versao(1), new Validade(new DateOnly(2026, 1, 10), 12));
        Assert.Equal(new DateOnly(2027, 1, 10), a.ValidoAte);

        a.NovaVersao(Versao(2), 10, null, null, new Validade(new DateOnly(2026, 11, 17), 12));

        Assert.Equal(new DateOnly(2027, 11, 17), a.ValidoAte);
        Assert.Null(a.Renovar(new Validade(new DateOnly(2027, 2, 1), 12)));
        Assert.Equal(new DateOnly(2028, 2, 1), a.ValidoAte);
        a.Arquivar();
        Assert.Equal("Item arquivado não pode ser renovado.", a.Renovar(new Validade(new DateOnly(2027, 3, 1), 12))?.Motivo);
        Assert.Equal(new DateOnly(2028, 2, 1), a.ValidoAte);
    }
}
