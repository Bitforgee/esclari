using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Acesso;

public sealed class ApagarLancamentos : ITarefaAgendada
{
    public string Tipo => "lancamento";

    public Task<List<ItemAgendado>> Ler(EsclariDb db, DateTime agora) => Task.FromResult<List<ItemAgendado>>([]);

    public Task Executar(EsclariDb db, IReadOnlyList<ItemAgendado> itens, DateTime agora, CancellationToken parar) => Task.CompletedTask;

    public async Task Limpar(EsclariDb db, DateTime agora, CancellationToken parar)
    {
        var limite = agora - TimeSpan.FromDays(1);
        await db.LtiLancamentos.IgnoreQueryFilters().Where(l => l.ExpiraEm < limite).ExecuteDeleteAsync(parar);
    }
}
