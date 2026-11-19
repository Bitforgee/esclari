namespace Esclari.Api.Administracao;

public static class ErrosDeAdmin
{
    public static IResult NaoEncontrado() => Problemas.Erro(404, "Item não encontrado.");

    public static IResult Repetido(string detalhe) => Problemas.Erro(409, detalhe);

    public static IResult ProprioPapel() => Problemas.Erro(409, "Você não pode mudar o próprio papel. Peça a outro administrador.");

    public static IResult PropriaConta() => Problemas.Erro(409, "Você não pode desativar a própria conta.");

    public static IResult SemAdministrador() => Problemas.Erro(409, "A instituição precisa de ao menos um administrador ativo.");
}
