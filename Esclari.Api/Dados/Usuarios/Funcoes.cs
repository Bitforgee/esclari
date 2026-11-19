namespace Esclari.Api.Dados.Usuarios;

public static class Funcoes
{
    public const string AssistenteUsar = "assistente.usar";
    public const string FilaAtender = "fila.atender";
    public const string ChamadosVerTodos = "chamados.ver_todos";
    public const string IndicadoresVer = "indicadores.ver";
    public const string ConhecimentoGerenciar = "conhecimento.gerenciar";
    public const string CadastrosGerenciar = "cadastros.gerenciar";
    public const string UsuariosGerenciar = "usuarios.gerenciar";
    public const string MatriculasImportar = "matriculas.importar";
    public const string TiposGerenciar = "tipos.gerenciar";
    public const string AparenciaGerenciar = "aparencia.gerenciar";
    public const string IntegracoesGerenciar = "integracoes.gerenciar";
    public const string McpUsar = "mcp.usar";

    public static readonly IReadOnlyList<string> Todas =
    [
        AssistenteUsar, FilaAtender, ChamadosVerTodos, IndicadoresVer, ConhecimentoGerenciar, CadastrosGerenciar,
        UsuariosGerenciar, MatriculasImportar, TiposGerenciar, AparenciaGerenciar, IntegracoesGerenciar, McpUsar,
    ];

    public static readonly IReadOnlyList<string> DoAtendente = [FilaAtender, IndicadoresVer, McpUsar];

    public static readonly IReadOnlyList<string> DoLiderDeSemestre = [AssistenteUsar, FilaAtender, IndicadoresVer, McpUsar];

    public static bool Valida(string? codigo) => codigo is not null && Todas.Contains(codigo);
}