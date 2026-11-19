using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Usuarios;

public sealed record TipoResposta(long Id, string Nome, string? Sistema, IReadOnlyList<string> Funcoes, int Usuarios)
{
    public static TipoResposta De(TipoDeUsuario t, int usuarios) =>
        new(t.Id, t.Nome, t.Sistema, [.. t.FuncoesDoTipo().Order(StringComparer.Ordinal)], usuarios);
}