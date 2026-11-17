using Esclari.Api.Dados;

namespace Esclari.Api.Instituicoes.Carga;

public sealed class ComandoCarga(EsclariDb db, TimeProvider relogio, Prazos prazos)
{
    public async Task<List<string>> Executar(Dictionary<string, string> o, TextWriter saida)
    {
        var (instituicao, arq, erro) = await ArquivoDeComando.Ler<ArquivoCarga>(db, o);
        if (erro is not null) return [erro];
        if (arq is null) return ["--arquivo: vazio"];

        db.InstituicaoAtual = instituicao!.Id;
        var existentes = await CargaInicial.Existentes(db);
        var (erros, plano) = CargaValidacao.Validar(arq, existentes);
        if (erros.Count > 0) return erros;

        var (setores, categorias) = await CargaInicial.Gravar(db, arq, existentes, plano, relogio.GetUtcNow().UtcDateTime, instituicao.Fuso, prazos.ValidadeConhecimentoMeses);
        saida.WriteLine($"setores: {setores}");
        saida.WriteLine($"cursos: {plano.Cursos.Count}");
        saida.WriteLine($"categorias: {categorias}");
        saida.WriteLine($"regras: {plano.Regras.Count}");
        saida.WriteLine($"artigos: {plano.Artigos.Count}");
        return [];
    }
}
