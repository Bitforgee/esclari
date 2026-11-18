namespace Esclari.Api.Dados.Operadores;

public static class AcoesDaPlataforma
{
    public const string OperadorEntrou = "OPERADOR_ENTROU";
    public const string InstituicaoCriada = "INSTITUICAO_CRIADA";
    public const string InstituicaoAlterada = "INSTITUICAO_ALTERADA";
    public const string InstituicaoDesativada = "INSTITUICAO_DESATIVADA";
    public const string InstituicaoReativada = "INSTITUICAO_REATIVADA";
    public const string AcessoReenviado = "ACESSO_REENVIADO";
    public const string CargaImportada = "CARGA_IMPORTADA";
    public const string OperadorAlterado = "OPERADOR_ALTERADO";

    public static readonly IReadOnlyList<string> Todas =
        [OperadorEntrou, InstituicaoCriada, InstituicaoAlterada, InstituicaoDesativada, InstituicaoReativada, AcessoReenviado, CargaImportada, OperadorAlterado];
}
