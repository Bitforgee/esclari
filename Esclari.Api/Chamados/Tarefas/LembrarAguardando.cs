using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Chamados.Tarefas;

public sealed class LembrarAguardando(IConfiguration cfg, Prazos prazos) : ITarefaAgendada
{
    public string Tipo => "lembrete";

    public async Task<List<ItemAgendado>> Ler(EsclariDb db, DateTime agora)
    {
        var itens = new List<ItemAgendado>();
        foreach (var (id, p) in await prazos.PorInstituicao(db))
        {
            var (limiteResolver, limiteLembrete) = (agora - p.ResolverAguardando, agora - p.Lembrete);
            itens.AddRange(await db.Chamados.IgnoreQueryFilters().Where(c => c.InstituicaoId == id && c.Estado == AguardandoAluno && c.LembreteEm == null && c.AguardandoDesde <= limiteLembrete && c.AguardandoDesde > limiteResolver)
                .Select(c => new ItemAgendado("lembrete", c.Id, c.InstituicaoId)).ToListAsync());
        }
        return itens;
    }

    public async Task Executar(EsclariDb db, IReadOnlyList<ItemAgendado> itens, DateTime agora, CancellationToken parar)
    {
        var id = itens[0].Id;
        var chamado = await db.Chamados.SingleOrDefaultAsync(c => c.Id == id);
        if (chamado is null || chamado.LembrarAluno((await prazos.DaInstituicao(db, chamado.InstituicaoId)).LembreteAguardandoDias, agora) is not null) return;
        await Avisos.AguardaResposta(db, cfg, await db.Instituicoes.SingleAsync(i => i.Id == chamado.InstituicaoId), chamado, agora);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { }
    }
}
