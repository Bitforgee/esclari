using Microsoft.Extensions.DependencyInjection;
using Esclari.Api.Acesso;
using Esclari.Api.Assistente.Busca;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class AgendadorTarefasTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    static readonly string[] TiposDaBase = ["conversa", "chamado", "aguardando", "lembrete", "subir", "email", "atraso", "trecho", "push", "validade"];

    List<ITarefaAgendada> Registradas => [.. f.Services.GetServices<ITarefaAgendada>()];

    [Fact]
    public void Tarefas_registradas_na_ordem_da_base()
    {
        var tarefas = Registradas;

        Assert.Equal(11, tarefas.Count);
        Assert.Equal(TiposDaBase, tarefas.Where(t => t is not ApagarLancamentos).Select(t => t.Tipo));
        Assert.Single(tarefas, t => t is ApagarLancamentos);
    }

    [Fact]
    public void Cada_tarefa_mora_na_fatia_dona_e_so_o_trecho_roda_em_lote()
    {
        var tarefas = Registradas.ToDictionary(t => t.GetType().Name, t => (t.GetType().Namespace, t.Lote));

        Assert.Equal(new Dictionary<string, (string?, int)>
        {
            ["ApagarLancamentos"] = ("Esclari.Api.Acesso", 1),
            ["AbandonarConversas"] = ("Esclari.Api.Assistente.Conversas", 1),
            ["FecharResolvidos"] = ("Esclari.Api.Chamados.Tarefas", 1),
            ["ResolverSemResposta"] = ("Esclari.Api.Chamados.Tarefas", 1),
            ["LembrarAguardando"] = ("Esclari.Api.Chamados.Tarefas", 1),
            ["SubirPorPrazo"] = ("Esclari.Api.Chamados.Tarefas", 1),
            ["EnviarEmails"] = ("Esclari.Api.Chamados.Tarefas", 1),
            ["IndexarTrechos"] = ("Esclari.Api.Assistente.Busca", Embeddings.Lote),
            ["EnviarPushes"] = ("Esclari.Api.Chamados.Tarefas", 1),
            ["AvisarValidade"] = ("Esclari.Api.Conhecimento", 1),
            ["AvisarAtrasos"] = ("Esclari.Api.Chamados.Tarefas", 1),
        }.OrderBy(x => x.Key), tarefas.OrderBy(x => x.Key));
    }
}
