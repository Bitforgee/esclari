using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Dados.Usuarios;

public static class Papeis
{
    public const string Aluno = "ALUNO";
    public const string Atendente = "ATENDENTE";
    public const string Administrador = "ADMINISTRADOR";

    public static string? Sistema(IEnumerable<string> papeis)
    {
        var lista = papeis.ToList();
        if (lista.Contains(Administrador)) return TipoDeUsuario.Gestor;
        if (lista.Contains(Aluno) && lista.Contains(Atendente)) return null;
        return lista.Contains(Atendente) ? TipoDeUsuario.Atendente : TipoDeUsuario.Aluno;
    }

    public static async Task<TipoDeUsuario> TipoPelosPapeis(this EsclariDb db, IEnumerable<string> papeis)
    {
        if (Sistema(papeis) is { } sistema) return await db.TipoDeSistema(sistema);
        return await db.TiposDeUsuario.Include(t => t.Funcoes).FirstOrDefaultAsync(t => t.Sistema == null && t.Nome == TipoDeUsuario.NomeDoLider)
            ?? db.TiposDeUsuario.Add(TipoDeUsuario.Criar(TipoDeUsuario.NomeDoLider, null, Funcoes.DoLiderDeSemestre)).Entity;
    }
}