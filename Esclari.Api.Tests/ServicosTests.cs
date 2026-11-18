using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class ServicosTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    const string Arquivo = "servicos.txt";

    static readonly string[] Adicionadas =
    [
        "Singleton Esclari.Api.EnviadorSmtp -> Esclari.Api.EnviadorSmtp",
        "Singleton Esclari.Api.Plataforma.ProtecaoDaPlataforma -> Esclari.Api.Plataforma.ProtecaoDaPlataforma",
        "Scoped Esclari.Api.PrazosAtuais -> Esclari.Api.PrazosAtuais",
        "Singleton Esclari.Api.ITarefaAgendada -> Esclari.Api.Acesso.ApagarLancamentos",
        "Singleton Esclari.Api.ITarefaAgendada -> Esclari.Api.Assistente.Conversas.AbandonarConversas",
        "Singleton Esclari.Api.ITarefaAgendada -> Esclari.Api.Chamados.Tarefas.FecharResolvidos",
        "Singleton Esclari.Api.ITarefaAgendada -> Esclari.Api.Chamados.Tarefas.ResolverSemResposta",
        "Singleton Esclari.Api.ITarefaAgendada -> Esclari.Api.Chamados.Tarefas.LembrarAguardando",
        "Singleton Esclari.Api.ITarefaAgendada -> Esclari.Api.Chamados.Tarefas.SubirPorPrazo",
        "Singleton Esclari.Api.ITarefaAgendada -> Esclari.Api.Chamados.Tarefas.EnviarEmails",
        "Singleton Esclari.Api.ITarefaAgendada -> Esclari.Api.Chamados.Tarefas.AvisarAtrasos",
        "Singleton Esclari.Api.ITarefaAgendada -> Esclari.Api.Assistente.Busca.IndexarTrechos",
        "Singleton Esclari.Api.ITarefaAgendada -> Esclari.Api.Chamados.Tarefas.EnviarPushes",
        "Singleton Esclari.Api.ITarefaAgendada -> Esclari.Api.Conhecimento.AvisarValidade",
    ];

    static readonly string[] Removidas =
    [
        "Singleton Esclari.Api.Email -> Esclari.Api.Email",
    ];

    static string Nome(Type? t) => t is null ? "fabrica" : t.IsGenericType ? t.ToString() : t.FullName!;

    static bool DoApi(ServiceDescriptor d) =>
        (d.ServiceType.Namespace?.StartsWith("Esclari.Api") ?? false) && !d.ServiceType.Name.EndsWith("Handler", StringComparison.Ordinal);

    [Fact]
    public async Task Tempos_de_vida_iguais_ao_golden()
    {
        IServiceCollection? colecao = null;
        await using var derivada = f.WithWebHostBuilder(b => b.ConfigureTestServices(s => colecao = s));
        _ = derivada.Services;

        var servicos = colecao!.Where(DoApi).Where(d => d.ServiceType != typeof(IHostedService))
            .Select(d => $"{d.Lifetime} {Nome(d.ServiceType)} -> {Nome(d.ImplementationType)}").Distinct().Order(StringComparer.Ordinal).ToList();
        var hospedados = colecao!.Where(d => d.ServiceType == typeof(IHostedService)).Select(d => $"{d.Lifetime} {Nome(d.ImplementationType)}").ToList();
        var vivo = string.Join("\n", ["## servicos", .. servicos, "## hospedados", .. hospedados]);

        var doGolden = Dourado.Secao(Arquivo, "servicos").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var esperados = doGolden.Except(Removidas).Concat(Adicionadas).Order(StringComparer.Ordinal);
        var esperado = string.Join("\n", ["## servicos", .. esperados, "## hospedados", .. Dourado.Secao(Arquivo, "hospedados").Split('\n', StringSplitOptions.RemoveEmptyEntries)]);

        Assert.Empty(Removidas.Except(doGolden));
        Assert.Empty(Adicionadas.Intersect(doGolden));
        Dourado.Igual(Arquivo, esperado, vivo);
    }
}
