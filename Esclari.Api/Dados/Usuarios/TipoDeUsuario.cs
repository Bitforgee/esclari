namespace Esclari.Api.Dados.Usuarios;

public class TipoDeUsuario : IDaInstituicao
{
    public const string Aluno = "ALUNO";
    public const string Gestor = "GESTOR";
    public const string Atendente = "ATENDENTE";
    public const string NomeDoLider = "Líder de semestre";

    TipoDeUsuario() { }

    public long Id { get; private set; }
    public long InstituicaoId { get; set; }
    public string Nome { get; private set; } = "";
    public string? Sistema { get; private set; }
    public List<FuncaoDoTipo> Funcoes { get; private set; } = [];

    public static TipoDeUsuario Criar(string nome, string? sistema, IEnumerable<string> funcoes) =>
        new() { Nome = nome, Sistema = sistema, Funcoes = [.. funcoes.Distinct().Select(FuncaoDoTipo.Criar)] };

    public static TipoDeUsuario DeSistema(string sistema) => sistema switch
    {
        Aluno => Criar("Aluno", Aluno, []),
        Gestor => Criar("Gestor", Gestor, []),
        Atendente => Criar("Atendente", Atendente, Usuarios.Funcoes.DoAtendente),
        _ => throw new ArgumentOutOfRangeException(nameof(sistema)),
    };

    public IReadOnlyList<string> FuncoesDoTipo() => Sistema switch
    {
        Aluno => [Usuarios.Funcoes.AssistenteUsar],
        Gestor => [.. Usuarios.Funcoes.Todas.Where(f => f != Usuarios.Funcoes.AssistenteUsar)],
        _ => [.. Funcoes.Select(f => f.Funcao)],
    };
}