using Esclari.Api.Dados;
using Esclari.Api.Dados.Cadastros;

namespace Esclari.Api.Tests;

public class CursoDominioTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validar_nome_vazio_devolve_Invalido(string? nome)
    {
        Assert.Equal(new Invalido("Informe o nome do curso."), Curso.Validar(nome));
    }

    [Fact]
    public void Validar_limita_o_nome_a_200_caracteres()
    {
        Assert.Null(Curso.Validar(new string('a', 200)));
        Assert.Equal(new Invalido("Informe o nome do curso."), Curso.Validar(new string('a', 201)));
    }
}
