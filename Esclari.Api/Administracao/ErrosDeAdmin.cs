namespace Esclari.Api.Administracao;

public static class ErrosDeAdmin
{
    public static IResult NaoEncontrado() => Problemas.Erro(404, "Item não encontrado.");

    public static IResult Repetido(string detalhe) => Problemas.Erro(409, detalhe);

    public static IResult ProprioTipo() => Problemas.Erro(409, "Você não pode mudar o próprio tipo. Peça a outro gestor.");

    public static IResult PropriaConta() => Problemas.Erro(409, "Você não pode desativar a própria conta.");

    public static IResult SemGestor() => Problemas.Erro(409, "A instituição precisa de ao menos um gestor ativo.");

    public static IResult SemPoderParaOTipo() => Problemas.Erro(403, "Você não pode atribuir ou retirar um tipo com funções que o seu tipo não tem.");
}
