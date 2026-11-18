using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Instituicoes.Carga;

namespace Esclari.Api.Plataforma;

public sealed record Contagens(int Setores, int Cursos, int Categorias, int Regras, int Artigos);

public static class CargaDoOperador
{
    static readonly UTF8Encoding Utf8Estrito = new(false, true);

    public static IResult Recusa(IEnumerable<string> erros) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["arquivo"] = [.. erros] }, detail: "O arquivo de carga tem erros.", title: Problemas.Titulo(400));

    public static (ArquivoCarga? Arquivo, IResult? Recusa) Ler(byte[] bytes)
    {
        string texto;
        try { texto = Utf8Estrito.GetString(bytes); }
        catch (DecoderFallbackException) { return (null, Recusa(["O arquivo deve estar em UTF-8."])); }
        try
        {
            var arq = JsonSerializer.Deserialize<ArquivoCarga>(texto, JsonSerializerOptions.Web);
            return arq is null ? (null, Recusa(["O arquivo está vazio."])) : (arq, null);
        }
        catch (JsonException e) { return (null, Recusa([$"JSON inválido ({e.Message})"])); }
    }

    static List<T>? Novos<T>(List<T>? itens, Func<T, string?> nome, Func<string, bool> existe) =>
        itens?.Where(x => nome(x) is not { } n || !existe(n)).ToList();

    static async Task<ArquivoCarga> SemRepetidos(EsclariDb db, ArquivoCarga arq, Existentes existentes)
    {
        var titulos = (await db.VersoesArtigo.Select(v => v.Titulo).ToListAsync()).ToHashSet();
        return new(
            Novos(arq.Setores, s => s.Nome, existentes.Setores.ContainsKey),
            Novos(arq.Cursos, c => c.Nome, existentes.Cursos.Contains),
            Novos(arq.Categorias, c => c.Nome, existentes.Categorias.ContainsKey),
            Novos(arq.Regras, r => r.Categoria, existentes.CategoriasComRegra.Contains),
            Novos(arq.Artigos, a => a.Titulo, titulos.Contains));
    }
    public static async Task<(Contagens?, IResult?)> Aplicar(EsclariDb db, Instituicao instituicao, ArquivoCarga arq, bool simular, DateTime agora, int meses, OperadorPlataforma operador)
    {
        db.InstituicaoAtual = instituicao.Id;
        var existentes = await CargaInicial.Existentes(db);
        arq = await SemRepetidos(db, arq, existentes);
        var (erros, plano) = CargaValidacao.Validar(arq, existentes);
        if (erros.Count > 0) return (null, Recusa(erros));

        if (simular)
        {
            var (novosSetores, novasCategorias) = (plano.Setores.Values.Except(existentes.Setores.Values).Count(), plano.Categorias.Values.Except(existentes.Categorias.Values).Count());
            return (new(novosSetores, plano.Cursos.Count, novasCategorias, plano.Regras.Count, plano.Artigos.Count), null);
        }
        var auditoria = AuditoriaPlataforma.Registrar(AcoesDaPlataforma.CargaImportada, operador.Id, instituicao.Id, $"Importou a carga de {instituicao.Slug}", agora);
        var (setores, categorias) = await CargaInicial.Gravar(db, arq, existentes, plano, agora, instituicao.Fuso, meses, auditoria);
        return (new(setores, plano.Cursos.Count, categorias, plano.Regras.Count, plano.Artigos.Count), null);
    }
}
