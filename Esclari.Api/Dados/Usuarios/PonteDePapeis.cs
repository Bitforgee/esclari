namespace Esclari.Api.Dados.Usuarios;

public static class PonteDePapeis
{
    public static readonly string[] Validos = [Papeis.Aluno, Papeis.Atendente, Papeis.Administrador];
    public static readonly string[] DaEquipe = [Papeis.Administrador, Papeis.Atendente];
    public static readonly string[] DoGestor = [Papeis.Administrador];
    public static readonly string[] DoAluno = [Papeis.Aluno];

    public static bool ExigeSetor(IEnumerable<string> papeis) => papeis.Contains(Papeis.Atendente);

    public static bool EhAdministrador(IEnumerable<string> papeis) => papeis.Contains(Papeis.Administrador);

    public static string Funcao(IEnumerable<string> papeis) => EhAdministrador(papeis) ? "administrador" : "atendente";

    public static string? Sistema(IEnumerable<string> papeis)
    {
        var lista = papeis.ToList();
        if (EhAdministrador(lista)) return TipoDeUsuario.Gestor;
        if (lista.Contains(Papeis.Aluno) && lista.Contains(Papeis.Atendente)) return null;
        return lista.Contains(Papeis.Atendente) ? TipoDeUsuario.Atendente : TipoDeUsuario.Aluno;
    }
}