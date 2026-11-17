using Esclari.Api.Dados;

namespace Esclari.Api.Tests;

public class DocumentoDominioTests
{
    static readonly DateTime Agora = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
    static readonly Validade Ano = new(new DateOnly(2026, 11, 2), 12);

    static VersaoDocumento Versao(int numero) => new() { Numero = numero, NomeArquivo = $"r{numero}.pdf", Chave = $"1/documentos/{Guid.NewGuid():N}", Hash = new string('a', 64), Tamanho = 1, Texto = "t", Paginas = 1, CriadaEm = Agora };

    static Documento Novo() => Documento.Criar("Regulamento", 10, Versao(1), Ano);

    [Fact]
    public void Cada_transicao_como_a_base()
    {
        var d = Novo();
        Assert.Equal((EstadosDocumento.Ativo, "Regulamento", 10), (d.Estado, d.Titulo, d.CategoriaId));
        Assert.Equal(1, Assert.Single(d.Versoes).Numero);

        Assert.Null(d.NovaVersao(Versao(2), Ano));
        Assert.Equal([1, 2], d.Versoes.Select(v => v.Numero));
        Assert.Equal(EstadosDocumento.Ativo, d.Estado);

        Assert.Null(d.Arquivar());
        Assert.Equal(EstadosDocumento.Arquivado, d.Estado);
    }

    [Fact]
    public void Arquivado_recusa_versao_e_arquivar()
    {
        var d = Novo();
        d.Arquivar();
        Assert.Equal("O documento está arquivado.", d.NovaVersao(Versao(2), Ano)?.Motivo);
        Assert.Equal("O documento já está arquivado.", d.Arquivar()?.Motivo);
        Assert.Equal(EstadosDocumento.Arquivado, d.Estado);
        Assert.Equal(1, Assert.Single(d.Versoes).Numero);
    }

    [Fact]
    public void Versao_sem_numero_maior_lanca()
    {
        var d = Novo();
        d.NovaVersao(Versao(2), Ano);
        foreach (var numero in new[] { 2, 1 })
        {
            Assert.Throws<InvalidOperationException>(() => d.NovaVersao(Versao(numero), Ano));
            Assert.Equal([1, 2], d.Versoes.Select(v => v.Numero));
        }
        Assert.Null(d.NovaVersao(Versao(3), Ano));
        Assert.Equal([1, 2, 3], d.Versoes.Select(v => v.Numero));
        Assert.Throws<InvalidOperationException>(() => Documento.Criar("x", 10, Versao(2), Ano));
    }

    [Fact]
    public void Nova_versao_refaz_a_validade()
    {
        var d = Novo();
        Assert.Equal(new DateOnly(2027, 11, 2), d.ValidoAte);

        Assert.Null(d.NovaVersao(Versao(2), new Validade(new DateOnly(2026, 11, 17), 12)));

        Assert.Equal(new DateOnly(2027, 11, 17), d.ValidoAte);
        Assert.Null(d.Renovar(new Validade(new DateOnly(2027, 2, 1), 12)));
        Assert.Equal(new DateOnly(2028, 2, 1), d.ValidoAte);
        Assert.Null(d.Arquivar());
        Assert.Equal("Item arquivado não pode ser renovado.", d.Renovar(new Validade(new DateOnly(2027, 3, 1), 12))?.Motivo);
        Assert.Equal(new DateOnly(2028, 2, 1), d.ValidoAte);
    }
}
