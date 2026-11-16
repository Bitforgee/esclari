using Esclari.Api.Dados;

namespace Esclari.Api;

public static class Sessao
{
    public static Usuario Usuario(this HttpContext ctx) => (Usuario)ctx.Items[typeof(Usuario)]!;
    public static Instituicao Instituicao(this HttpContext ctx) => (Instituicao)ctx.Items[typeof(Instituicao)]!;
}