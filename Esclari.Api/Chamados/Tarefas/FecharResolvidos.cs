using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Chamados.Tarefas;

public sealed class FecharResolvidos(Prazos prazos, ILogger<Agendador> log) : ITarefaAgendada
{
    public string Tipo => "chamado";

    public Task<List<ItemAgendado>> Ler(EsclariDb db, DateTime agora)
    {
        var limite = agora - prazos.Confirmacao;
        return db.Chamados.IgnoreQueryFilters().Where(c => c.Estado == Resolvido && c.ResolvidoEm <= limite)
            .Select(c => new ItemAgendado("chamado", c.Id, c.InstituicaoId)).ToListAsync();
    }

    public async Task Executar(EsclariDb db, IReadOnlyList<ItemAgendado> itens, DateTime agora, CancellationToken parar)
    {
        var id = itens[0].Id;
        var chamado = await db.Chamados.SingleOrDefaultAsync(c => c.Id == id);
        if (chamado is null || chamado.FecharPorInatividade(prazos.ConfirmacaoDias, agora) is not null) return;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return; }
        log.LogInformation("Chamado {ChamadoId} fechado automaticamente (instituição {InstituicaoId})", id, db.InstituicaoAtual);
    }
}
