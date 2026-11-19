using Microsoft.EntityFrameworkCore;
using Esclari.Api.Chamados.Permissao;
using Esclari.Api.Dados;

namespace Esclari.Api.Chamados.Consulta;

public static class ChamadoDoUsuario
{
    public static IResult NaoEncontrado() => Problemas.Erro(404, "Chamado não encontrado.");

    public static async Task<(Chamado?, string?)> Carregar(EsclariDb db, Usuario u, string numero)
    {
        var c = await db.Chamados.FirstOrDefaultAsync(x => x.Numero == numero);
        if (c is null) return (null, null);
        if (c.SolicitanteId == u.Id) return (c, PermissoesChamado.Aluno);
        if (u.Pode(Funcoes.FilaAtender) && await db.AtendenteSetores.AnyAsync(a => a.UsuarioId == u.Id && a.SetorId == c.SetorId))
        {
            var lideranca = await db.Setores.AnyAsync(s => s.Id == c.SetorId && s.CursoId != null);
            return (c, lideranca ? PermissoesChamado.Lider : PermissoesChamado.Atendente);
        }
        return u.Pode(Funcoes.ChamadosVerTodos) ? (c, PermissoesChamado.Leitura) : (null, null);
    }
}
