using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public static class EquipeDaInstituicao
{
    public static readonly string[] PapeisPermitidos = PonteDePapeis.DaEquipe;

    public static async Task<Instituicao?> Alvo(EsclariDb db, string slug, CancellationToken ct)
    {
        var instituicao = await db.Instituicoes.FirstOrDefaultAsync(i => i.Slug == slug, ct);
        if (instituicao is not null) db.InstituicaoAtual = instituicao.Id;
        return instituicao;
    }

    public static Task<Usuario?> Membro(EsclariDb db, long id, CancellationToken ct) =>
        db.Usuarios.Include(u => u.Papeis).Include(u => u.Setores).Include(u => u.Tipo)
            .FirstOrDefaultAsync(u => u.Id == id && u.Tipo!.Sistema != TipoDeUsuario.Aluno, ct);

    public static string Resumo(string verbo, Usuario u) => $"{verbo} o usuário {u.Id} ({string.Join(", ", u.Papeis.Select(p => p.Papel).Order())})";

    public static string MascararEmail(string email)
    {
        var arroba = email.IndexOf('@');
        return arroba <= 0 ? "***" : email[..1] + "***" + email[arroba..];
    }

    public static string Mascarar(Cpf cpf)
    {
        var digitos = new string(cpf.ToString().Where(char.IsDigit).ToArray());
        return digitos.Length == 11 ? $"***.{digitos[3..6]}.{digitos[6..9]}-**" : "***";
    }

    public static async Task<IResult?> Validar(EsclariDb db, CriarMembro.Entrada e) => Pessoa(e) ?? await PapeisESetores(db, e);

    static IResult? Pessoa(CriarMembro.Entrada e)
    {
        if (Cpf.Tentar(e.Cpf) is null) return Problemas.Invalido("CPF inválido.", "cpf");
        if (string.IsNullOrWhiteSpace(e.Nome) || e.Nome.Length > 200) return Problemas.Invalido("Informe o nome.", "nome");
        if (e.Email is null || !EnviadorSmtp.Valido().IsMatch(e.Email)) return Problemas.Invalido("E-mail inválido.", "email");
        return null;
    }

    static async Task<IResult?> PapeisESetores(EsclariDb db, CriarMembro.Entrada e)
    {
        if (e.Papeis is not { Length: > 0 } || e.Papeis.Any(p => !PapeisPermitidos.Contains(p))) return Problemas.Invalido("Escolha administrador, atendente ou os dois.", "papeis");
        var setores = e.Setores?.Distinct().ToArray() ?? [];
        if (PonteDePapeis.ExigeSetor(e.Papeis) && setores.Length == 0) return Problemas.Invalido("Escolha ao menos um setor para o atendente.", "setores");
        if (await db.Setores.CountAsync(s => setores.Contains(s.Id)) != setores.Length) return Problemas.Invalido("Setor inexistente.", "setores");
        return null;
    }

    public static async Task<IResult?> Repetido(EsclariDb db, CriarMembro.Entrada e)
    {
        var cpf = Cpf.Tentar(e.Cpf);
        if (await db.Usuarios.AnyAsync(u => u.Cpf == cpf)) return Problemas.Erro(409, "Já existe um usuário com este CPF na instituição.");
        if (await db.Usuarios.AnyAsync(u => u.Email == e.Email)) return Problemas.Erro(409, "Já existe um usuário com este e-mail na instituição.");
        return null;
    }
}
