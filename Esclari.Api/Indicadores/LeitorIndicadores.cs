using Microsoft.EntityFrameworkCore;
using Esclari.Api.Conhecimento;
using Esclari.Api.Dados;
using Esclari.Api.Dados.Conversas;

namespace Esclari.Api.Indicadores;

public sealed record PedidoValido(DateOnly De, DateOnly Ate, List<long>? Setores);

public sealed class LeitorIndicadores(EsclariDb db)
{
    sealed record ConversaComSemBase(long AlunoId, string? Desfecho, bool SemBase, DateTime CriadaEm);
    sealed record ConversaCitada(long Id, long CategoriaId);

    public async Task<(PedidoValido?, IResult?)> Validar(Usuario u, DateOnly? de, DateOnly? ate, long? setorId)
    {
        if (!u.Tem(Papeis.Administrador) && !u.Tem(Papeis.Atendente)) return (null, Problemas.Erro(403, "Os indicadores são para administradores e atendentes."));
        if (await db.SoLideranca(u)) return (null, Problemas.Erro(403, "Os indicadores não estão disponíveis para a liderança de turma."));
        if (de is null || ate is null) return (null, Problemas.Invalido("Informe o período (de e ate).", "de"));
        if (de > ate) return (null, Problemas.Invalido("A data inicial deve ser anterior ou igual à final.", "de"));
        var (setores, recusa) = await Escopo(u, setorId);
        return recusa is not null ? (null, recusa) : (new PedidoValido(de.Value, ate.Value, setores), null);
    }
    async Task<(List<long>?, IResult?)> Escopo(Usuario u, long? setorId)
    {
        if (!u.Tem(Papeis.Administrador))
        {
            var meus = await db.AtendenteSetores.Where(a => a.UsuarioId == u.Id).Select(a => a.SetorId).ToListAsync();
            if (setorId is not null && !meus.Contains(setorId.Value)) return (null, Problemas.Erro(403, "Este setor não é seu."));
            return (setorId is null ? meus : [setorId.Value], null);
        }
        if (setorId is null) return (null, null);
        return await db.Setores.AnyAsync(s => s.Id == setorId) ? ([setorId.Value], null) : (null, Problemas.Invalido("Setor inexistente.", "setorId"));
    }

    public async Task<Massa> Ler(List<long>? setores, (DateTime Inicio, DateTime Fim) periodo)
    {
        var todos = await db.Chamados.Where(c => c.MotivoFechamento != MotivosFechamento.Duplicado || c.MotivoFechamento == null)
            .Select(c => new ChamadoLido(c.Id, c.CriadoEm, c.Estado, c.MotivoFechamento, c.Reaberturas, c.Encaminhamentos, c.CategoriaOriginalId, c.SetorId, c.SolicitanteId)).ToListAsync();
        var eventos = await db.EventosChamado.Where(e => e.Tipo == TiposEvento.Transicao || e.Tipo == TiposEvento.Encaminhamento)
            .OrderBy(e => e.CriadoEm).ThenBy(e => e.Id).ToListAsync();
        if (setores is not null)
            todos = CalculoIndicadores.DosSetores(todos, eventos, setores, await db.Setores.Where(s => setores.Contains(s.Id)).Select(s => s.Nome).ToListAsync());
        var ids = todos.Select(c => c.Id).ToList();
        var respostas = await db.MensagensChamado.Where(m => ids.Contains(m.ChamadoId) && m.Tipo == TiposMensagem.Publica)
            .Select(m => new RespostaLida(m.ChamadoId, m.AutorId, m.CriadaEm)).ToListAsync();
        var categorias = await db.Categorias.ToDictionaryAsync(c => c.Id, c => c.Nome);
        if (setores is not null) return new Massa(todos, eventos, respostas, null, null, categorias);
        var (conversas, citadas) = await Conversas(periodo.Inicio, periodo.Fim);
        return new Massa(todos, eventos, respostas, conversas, citadas, categorias);
    }

    async Task<(List<ConversaLida>, List<long>)> Conversas(DateTime inicio, DateTime fim)
    {
        var conversas = (await db.Conversas.Where(c => c.CriadaEm >= inicio && c.CriadaEm < fim)
                .Select(c => new ConversaComSemBase(c.AlunoId, c.Desfecho, db.MensagensConversa.Any(m => m.ConversaId == c.Id && m.TipoFinal == Tipos.SemBase), c.CriadaEm))
                .ToListAsync())
            .Select(c => new ConversaLida(c.AlunoId, c.Desfecho, c.SemBase, c.CriadaEm)).ToList();
        var citadas = await (
            from ci in db.CategoriasCitadas()
            join m in db.MensagensConversa on ci.MensagemConversaId equals m.Id
            join c in db.Conversas on m.ConversaId equals c.Id
            where c.CriadaEm >= inicio && c.CriadaEm < fim
            select new ConversaCitada(c.Id, ci.CategoriaId)).Distinct().ToListAsync();
        return (conversas, citadas.Select(x => x.CategoriaId).ToList());
    }
}
