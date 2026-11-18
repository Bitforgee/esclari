using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Chamados.Tarefas;

public sealed class ResolverSemResposta(IConfiguration cfg, Prazos prazos) : ITarefaAgendada
{
    public string Tipo => "aguardando";

    public async Task<List<ItemAgendado>> Ler(EsclariDb db, DateTime agora)
    {
        var itens = new List<ItemAgendado>();
        foreach (var (id, p) in await prazos.PorInstituicao(db))
        {
            var limite = agora - p.ResolverAguardando;
            itens.AddRange(await db.Chamados.IgnoreQueryFilters().Where(c => c.InstituicaoId == id && c.Estado == AguardandoAluno && c.AguardandoDesde <= limite)
                .Select(c => new ItemAgendado("aguardando", c.Id, c.InstituicaoId)).ToListAsync());
        }
        return itens;
    }

    public async Task Executar(EsclariDb db, IReadOnlyList<ItemAgendado> itens, DateTime agora, CancellationToken parar)
    {
        var id = itens[0].Id;
        var chamado = await db.Chamados.SingleOrDefaultAsync(c => c.Id == id);
        if (chamado is null) return;
        var instituicao = await db.Instituicoes.SingleAsync(i => i.Id == chamado.InstituicaoId);
        var p = prazos.Para(instituicao);
        if (chamado.ResolverPorFaltaDeResposta(p.ResolverAguardandoDias, agora) is not null) return;
        await Avisos.Resolvido(db, cfg, p, instituicao, chamado, Chamado.TextoSemResposta, agora);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { }
    }
}
