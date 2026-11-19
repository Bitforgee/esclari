using Microsoft.EntityFrameworkCore;
using Npgsql;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Usuarios;

public static class ValidacaoDeTipo
{
    public const int Maximo = 30;
    public const int TamanhoDoNome = 60;

    public static string Limpar(string? nome) => (nome ?? "").Trim();

    public static IResult? Dados(string? nome, string[]? funcoes)
    {
        if (Limpar(nome) is not { Length: > 0 and <= TamanhoDoNome }) return Problemas.Invalido($"Informe o nome do tipo, com até {TamanhoDoNome} caracteres.", "nome");
        if ((funcoes ?? []).Any(f => !Funcoes.Valida(f))) return Problemas.Invalido("Escolha só funções da lista.", "funcoes");
        return null;
    }

    public static IResult? PodeDar(IReadOnlyList<string> tem, IEnumerable<string> funcoes) =>
        funcoes.All(f => f == Funcoes.AssistenteUsar || tem.Contains(f)) ? null : ErrosDeAdmin.SemPoderParaOTipo();

    public static IResult DeSistema(string acao) => Problemas.Erro(409, $"Este tipo é de sistema e não pode ser {acao}.");

    public static async Task<IResult?> NomeRepetido(EsclariDb db, string nome, long? exceto)
    {
        var minusculo = nome.ToLower();
        return await db.TiposDeUsuario.AnyAsync(t => t.Id != exceto && t.Nome.ToLower() == minusculo)
            ? ErrosDeAdmin.Repetido("Já existe um tipo com este nome.")
            : null;
    }

    public static bool NomeJaUsado(DbUpdateException e) =>
        e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}