namespace Esclari.Api.Dados.Usuarios;

public class FuncaoDoTipo : IDaInstituicao
{
    FuncaoDoTipo() { }

    public long Id { get; private set; }
    public long InstituicaoId { get; set; }
    public long TipoUsuarioId { get; private set; }
    public string Funcao { get; private set; } = "";

    internal static FuncaoDoTipo Criar(string funcao) => new() { Funcao = funcao };
}