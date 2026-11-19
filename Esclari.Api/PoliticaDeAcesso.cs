using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api;

public static class PoliticaDeAcesso
{
    public static IQueryable<Usuario> ComTipo(this IQueryable<Usuario> usuarios) =>
        usuarios.Include(u => u.Tipo).ThenInclude(t => t!.Funcoes).Include(u => u.Setores);

    public static Expression<Func<Usuario, bool>> TemFuncao(string funcao) => funcao switch
    {
        Funcoes.AssistenteUsar => u => u.Tipo!.Sistema == TipoDeUsuario.Aluno || u.Tipo.Funcoes.Any(f => f.Funcao == Funcoes.AssistenteUsar),
        Funcoes.FilaAtender => u => u.Setores.Any() && (u.Tipo!.Sistema == TipoDeUsuario.Gestor || u.Tipo.Funcoes.Any(f => f.Funcao == Funcoes.FilaAtender)),
        _ when Funcoes.Valida(funcao) => u => u.Tipo!.Sistema == TipoDeUsuario.Gestor || u.Tipo.Funcoes.Any(f => f.Funcao == funcao),
        _ => throw new ArgumentOutOfRangeException(nameof(funcao)),
    };

    public static async Task<TipoDeUsuario> TipoDeSistema(this EsclariDb db, string sistema) =>
        await db.TiposDeUsuario.Include(t => t.Funcoes).FirstOrDefaultAsync(t => t.Sistema == sistema)
        ?? db.TiposDeUsuario.Add(TipoDeUsuario.DeSistema(sistema)).Entity;

    public static async Task<TipoDeUsuario> TipoPelosPapeis(this EsclariDb db, IEnumerable<string> papeis)
    {
        if (PonteDePapeis.Sistema(papeis) is { } sistema) return await db.TipoDeSistema(sistema);
        return await db.TiposDeUsuario.Include(t => t.Funcoes).FirstOrDefaultAsync(t => t.Sistema == null && t.Nome == TipoDeUsuario.NomeDoLider)
            ?? db.TiposDeUsuario.Add(TipoDeUsuario.Criar(TipoDeUsuario.NomeDoLider, null, Funcoes.DoLiderDeSemestre)).Entity;
    }

    public static async Task AplicarPapeis(this EsclariDb db, Usuario u, IEnumerable<string> papeis, IEnumerable<long> setores)
    {
        var lista = papeis.ToList();
        u.DefinirAcesso(lista, setores);
        u.DefinirTipo(await db.TipoPelosPapeis(lista));
    }

    public static async Task<Usuario> NovoAluno(this EsclariDb db, Cpf cpf, string nome, string email)
    {
        var u = Usuario.Criar(cpf, nome, email, PonteDePapeis.DoAluno);
        u.DefinirTipo(await db.TipoDeSistema(TipoDeUsuario.Aluno));
        return u;
    }
}