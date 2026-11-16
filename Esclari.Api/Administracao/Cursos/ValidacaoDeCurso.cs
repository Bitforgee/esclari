using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Cursos;

public static class ValidacaoDeCurso
{
    public static async Task<IResult?> Coordenacao(EsclariDb db, long? setorCoordenacaoId)
    {
        if (setorCoordenacaoId is not { } id) return null;
        if (await db.Setores.AnyAsync(x => x.Id == id && x.CursoId != null)) return Problemas.Invalido("Uma liderança de semestre não pode ser a coordenação de um curso.", "setorCoordenacaoId");
        return await db.Setores.AnyAsync(x => x.Id == id) ? null : Problemas.Invalido("Setor de coordenação inexistente.", "setorCoordenacaoId");
    }
}
