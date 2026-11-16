using Esclari.Api.Assistente.Busca;

namespace Esclari.Api.Assistente.Respostas;

public record SaidaModelo(string? Tipo, string? Texto, List<long>? FonteIds, List<Trecho>? Trechos, long? CategoriaId, string? Resumo);
