using Microsoft.EntityFrameworkCore;
using Esclari.Api.Acesso;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Usuarios;

public static class ValidacaoDeUsuario
{
    public static async Task<(IResult? Erro, TipoDeUsuario? Tipo)> Validar(EsclariDb db, string? cpf, string? nome, string? email, string? senhaTemporaria, long? tipoId, long[]? setores, long id)
    {
        if (Pessoa(cpf, nome, email) is { } pessoa) return (pessoa, null);
        var tipo = tipoId is null ? null : await db.TiposDeUsuario.Include(t => t.Funcoes).FirstOrDefaultAsync(t => t.Id == tipoId);
        if (tipo is null) return (Problemas.Invalido("Escolha o tipo do usuário.", "tipoId"), null);
        return (await SetoresESenha(db, tipo, senhaTemporaria, setores) ?? await Repetidos(db, cpf, email, id), tipo);
    }

    public static IResult? PodeAtribuir(IReadOnlyList<string> tem, TipoDeUsuario novo, TipoDeUsuario? atual)
    {
        var exigidas = novo.FuncoesDoTipo().Concat(atual?.FuncoesDoTipo() ?? []).Where(f => f != Funcoes.AssistenteUsar);
        return exigidas.All(tem.Contains) ? null : ErrosDeAdmin.SemPoderParaOTipo();
    }

    public static async Task<IResult?> UltimoGestor(EsclariDb db, Usuario alvo, bool continuaGestor)
    {
        if (continuaGestor || !alvo.Ativo || !await db.Usuarios.AnyAsync(u => u.Id == alvo.Id && u.Tipo!.Sistema == TipoDeUsuario.Gestor)) return null;
        var existeOutro = await db.Usuarios.AnyAsync(u => u.Id != alvo.Id && u.Ativo && u.Tipo!.Sistema == TipoDeUsuario.Gestor);
        return existeOutro ? null : ErrosDeAdmin.SemGestor();
    }

    public static bool Nome(string? nome, int max) => !string.IsNullOrWhiteSpace(nome) && nome.Length <= max;

    static IResult? Pessoa(string? cpf, string? nome, string? email)
    {
        if (Cpf.Tentar(cpf) is null) return Problemas.Invalido("CPF inválido.", "cpf");
        if (!Nome(nome, 200)) return Problemas.Invalido("Informe o nome.", "nome");
        if (email is null || !EnviadorSmtp.Valido().IsMatch(email)) return Problemas.Invalido("E-mail inválido.", "email");
        return null;
    }

    static async Task<IResult?> SetoresESenha(EsclariDb db, TipoDeUsuario tipo, string? senhaTemporaria, long[]? setoresInformados)
    {
        var setores = setoresInformados ?? [];
        if (tipo.ExigeSetor && setores.Length == 0) return Problemas.Invalido("Escolha ao menos um setor para quem atende a fila.", "setores");
        if (await db.Setores.CountAsync(s => setores.Contains(s.Id)) != setores.Distinct().Count()) return Problemas.Invalido("Setor inexistente.", "setores");
        if (senhaTemporaria is not null && !Senha.Valida(senhaTemporaria))
            return Problemas.Invalido($"A senha temporária deve ter de {Senha.Minimo} a {Senha.Maximo} caracteres.", "senhaTemporaria");
        return null;
    }

    static async Task<IResult?> Repetidos(EsclariDb db, string? cpfInformado, string? email, long id)
    {
        var cpf = Cpf.Tentar(cpfInformado);
        if (await db.Usuarios.AnyAsync(u => u.Id != id && u.Cpf == cpf)) return ErrosDeAdmin.Repetido("Já existe um usuário com este CPF na instituição.");
        if (await db.Usuarios.AnyAsync(u => u.Id != id && u.Email == email)) return ErrosDeAdmin.Repetido("Já existe um usuário com este e-mail na instituição.");
        return null;
    }
}