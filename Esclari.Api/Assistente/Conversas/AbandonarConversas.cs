using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Dados.Conversas;

namespace Esclari.Api.Assistente.Conversas;

public sealed class AbandonarConversas(Prazos prazos, ILogger<Agendador> log) : ITarefaAgendada
{
    public string Tipo => "conversa";

    public async Task<List<ItemAgendado>> Ler(EsclariDb db, DateTime agora)
    {
        var itens = new List<ItemAgendado>();
        foreach (var (id, p) in await prazos.PorInstituicao(db))
            itens.AddRange(await db.Conversas.IgnoreQueryFilters().Where(c => c.InstituicaoId == id).Where(Conversa.Abandonavel(agora - p.Abandono))
                .Select(c => new ItemAgendado("conversa", c.Id, c.InstituicaoId)).ToListAsync());
        return itens;
    }

    public async Task Executar(EsclariDb db, IReadOnlyList<ItemAgendado> itens, DateTime agora, CancellationToken parar)
    {
        var id = itens[0].Id;
        var p = await prazos.DaInstituicao(db, itens[0].InstituicaoId);
        var n = await db.Conversas.Where(c => c.Id == id).Where(Conversa.Abandonavel(agora - p.Abandono))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Desfecho, Desfechos.Abandonada).SetProperty(c => c.DesfechoEm, agora));
        if (n == 1) log.LogInformation("Conversa {ConversaId} abandonada (instituição {InstituicaoId})", id, db.InstituicaoAtual);
    }
}
