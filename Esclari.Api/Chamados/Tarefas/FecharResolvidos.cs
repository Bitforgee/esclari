using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Chamados.Tarefas;

public sealed class FecharResolvidos(Prazos prazos, ILogger<Agendador> log) : ITarefaAgendada
{
    public string Tipo => "chamado";

    public async Task<List<ItemAgendado>> Ler(EsclariDb db, DateTime agora)
    {
        var itens = new List<ItemAgendado>();
        foreach (var (id, p) in await prazos.PorInstituicao(db))
        {
            var limite = agora - p.Confirmacao;
            itens.AddRange(await db.Chamados.IgnoreQueryFilters().Where(c => c.InstituicaoId == id && c.Estado == Resolvido && c.ResolvidoEm <= limite)
                .Select(c => new ItemAgendado("chamado", c.Id, c.InstituicaoId)).ToListAsync());
        }
        return itens;
    }

    public async Task Executar(EsclariDb db, IReadOnlyList<ItemAgendado> itens, DateTime agora, CancellationToken parar)
    {
        var id = itens[0].Id;
        var chamado = await db.Chamados.SingleOrDefaultAsync(c => c.Id == id);
        if (chamado is null || chamado.FecharPorInatividade((await prazos.DaInstituicao(db, chamado.InstituicaoId)).ConfirmacaoDias, agora) is not null) return;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return; }
        log.LogInformation("Chamado {ChamadoId} fechado automaticamente (instituição {InstituicaoId})", id, db.InstituicaoAtual);
    }
}
