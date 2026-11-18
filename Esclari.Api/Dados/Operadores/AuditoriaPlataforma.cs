namespace Esclari.Api.Dados.Operadores;

public class AuditoriaPlataforma
{
    public const int LimiteDoResumo = 200;

    AuditoriaPlataforma() { }

    public long Id { get; private set; }
    public string Acao { get; private set; } = "";
    public long OperadorId { get; private set; }
    public long? InstituicaoAlvoId { get; private set; }
    public string Resumo { get; private set; } = "";
    public DateTime CriadaEm { get; private set; }

    public static AuditoriaPlataforma Registrar(string acao, long operadorId, long? instituicaoAlvoId, string resumo, DateTime agora)
    {
        if (!AcoesDaPlataforma.Todas.Contains(acao)) throw new ArgumentException($"Ação desconhecida: {acao}", nameof(acao));
        var curto = resumo.Length > LimiteDoResumo ? resumo[..LimiteDoResumo] : resumo;
        return new() { Acao = acao, OperadorId = operadorId, InstituicaoAlvoId = instituicaoAlvoId, Resumo = curto, CriadaEm = agora };
    }
}
