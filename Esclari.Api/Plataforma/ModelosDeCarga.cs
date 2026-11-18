using Esclari.Api.Instituicoes.Carga;

namespace Esclari.Api.Plataforma;

public static class ModelosDeCarga
{
    public sealed record Modelo(string Id, string Nome, string Descricao, ArquivoCarga Arquivo);

    const string Secretaria = "Secretaria Acadêmica";
    const string Financeiro = "Financeiro";
    const string Coordenacao = "Coordenação de Curso";

    static readonly (string Categoria, string Setor)[] Roteamento =
    [
        ("Boleto e mensalidade", Financeiro),
        ("Matrícula e rematrícula", Secretaria),
        ("Documentos e declarações", Secretaria),
        ("Calendário e provas", Coordenacao),
        ("Estágio e TCC", Coordenacao),
        ("Ouvidoria", Coordenacao),
    ];

    public static readonly IReadOnlyList<Modelo> Todos =
    [
        new("padrao-esclari", "Padrão Esclari", "Três setores, seis categorias e uma regra de roteamento por categoria. Sem artigos: quem publica é a instituição.",
            new([.. new[] { Secretaria, Financeiro, Coordenacao }.Select(n => new SetorCarga(n))], null,
                [.. Roteamento.Select(r => new CategoriaCarga(r.Categoria))],
                [.. Roteamento.Select(r => new RegraCarga(r.Categoria, r.Setor, false))], null)),
    ];

    public static Modelo? Achar(string id) => Todos.FirstOrDefault(m => m.Id == id);
}
