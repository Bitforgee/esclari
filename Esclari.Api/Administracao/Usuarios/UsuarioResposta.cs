using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Usuarios;

public sealed record UsuarioResposta(long Id, string Cpf, string Nome, string Email, bool Ativo, bool DeveTrocarSenha, UsuarioResposta.DoTipo Tipo, IReadOnlyList<long> Setores)
{
    public sealed record DoTipo(long Id, string Nome);

    public static async Task<UsuarioResposta> De(EsclariDb db, Usuario u) =>
        new(u.Id, u.Cpf.Valor, u.Nome, u.Email, u.Ativo, u.DeveTrocarSenha, new DoTipo(u.TipoUsuarioId, u.Tipo!.Nome),
            await db.AtendenteSetores.Where(a => a.UsuarioId == u.Id).Select(a => a.SetorId).ToListAsync());
}