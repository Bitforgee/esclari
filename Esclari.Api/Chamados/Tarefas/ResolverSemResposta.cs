using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Chamados.Tarefas;

public sealed class ResolverSemResposta(IConfiguration cfg, Prazos prazos) : ITarefaAgendada
{
    public string Tipo => "aguardando";

    public Task<List<ItemAgendado>> Ler(EsclariDb db, DateTime agora)
    {
        var limite = agora - prazos.ResolverAguardando;
        return db.Chamados.IgnoreQueryFilters().Where(c => c.Estado == AguardandoAluno && c.AguardandoDesde <= limite)
            .Select(c => new ItemAgendado("aguardando", c.Id, c.InstituicaoId)).ToListAsync();
    }

    public async Task Executar(EsclariDb db, IReadOnlyList<ItemAgendado> itens, DateTime agora, CancellationToken parar)
    {
        var id = itens[0].Id;
        var chamado = await db.Chamados.SingleOrDefaultAsync(c => c.Id == id);
        if (chamado is null || chamado.ResolverPorFaltaDeResposta(prazos.ResolverAguardandoDias, agora) is not null) return;
        await Avisos.Resolvido(db, cfg, prazos, await db.Instituicoes.SingleAsync(i => i.Id == chamado.InstituicaoId), chamado, Chamado.TextoSemResposta, agora);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { }
    }
}
