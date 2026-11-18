using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public static class PlataformaComando
{
    static readonly string[] Acoes = ["--desativar", "--reativar", "--redefinir-2fa"];

    public static async Task<int> Executar(IServiceProvider servicos, string[] args, TextWriter saida, TextWriter erro)
    {
        using var scope = servicos.CreateScope();
        var sp = scope.ServiceProvider;
        var acao = args.FirstOrDefault(Acoes.Contains);
        string[] pares = [.. args.Where(a => !Acoes.Contains(a))];
        var opcoes = new Dictionary<string, string>();
        for (var i = 1; i + 1 < pares.Length; i += 2)
            if (pares[i].StartsWith("--")) opcoes[pares[i][2..]] = pares[i + 1];

        var erros = args.FirstOrDefault() == "operador"
            ? await ActivatorUtilities.CreateInstance<ComandoOperadorDaPlataforma>(sp).Executar(opcoes, acao, saida)
            : ["uso: plataforma operador --email --nome | plataforma operador --email [--desativar | --reativar | --redefinir-2fa]"];
        foreach (var e in erros) erro.WriteLine(e);
        return erros.Count == 0 ? 0 : 1;
    }
}

public sealed class ComandoOperadorDaPlataforma(EsclariDb db, TimeProvider relogio, IConfiguration cfg)
{
    public const string JaExiste = "Esse operador já existe.";

    public async Task<List<string>> Executar(Dictionary<string, string> o, string? acao, TextWriter saida)
    {
        var email = o.GetValueOrDefault("email")?.Trim().ToLowerInvariant() ?? "";
        if (!EnviadorSmtp.Valido().IsMatch(email)) return ["--email: informe um e-mail válido"];
        var operador = await db.OperadoresPlataforma.FirstOrDefaultAsync(x => x.Email == email);
        return acao is null ? await CriarOuConvidar(o, operador, email, saida) : await Alterar(operador, acao, saida);
    }

    async Task<List<string>> CriarOuConvidar(Dictionary<string, string> o, OperadorPlataforma? operador, string email, TextWriter saida)
    {
        if (operador is { TemSenha: true }) return [JaExiste];
        var agora = relogio.GetUtcNow().UtcDateTime;
        var nome = o.GetValueOrDefault("nome")?.Trim() ?? "";
        if (operador is null && nome.Length is < 3 or > 120) return ["--nome: informe de 3 a 120 caracteres"];
        var criado = operador is null;
        operador ??= db.OperadoresPlataforma.Add(OperadorPlataforma.Criar(nome, email, agora)).Entity;
        var token = operador.NovoConvite(agora);
        await db.SaveChangesAsync();
        db.AuditoriaDaPlataforma.Add(AuditoriaPlataforma.Registrar(AcoesDaPlataforma.OperadorAlterado, operador.Id, null, criado ? $"Comando: criou o operador {operador.Nome}" : $"Comando: novo convite para {operador.Nome}", agora));
        await db.SaveChangesAsync();
        saida.WriteLine($"Operador {operador.Nome} {(criado ? "criado" : "convidado de novo")}.");
        saida.WriteLine($"Link para criar a senha (vale 60 minutos): {Tokens.UrlBase(cfg)}/plataforma/definir-senha?token={token}");
        return [];
    }

    async Task<List<string>> Alterar(OperadorPlataforma? operador, string acao, TextWriter saida)
    {
        if (operador is null) return ["--email: não há operador com esse e-mail"];
        if (acao == "--desativar" && operador.Ativo && !await db.OperadoresPlataforma.AnyAsync(x => x.Ativo && x.Id != operador.Id)) return ["Não é possível desativar o último operador ativo."];
        var frase = acao switch
        {
            "--desativar" => Aplicar(operador.Desativar, "desativado"),
            "--reativar" => Aplicar(operador.Reativar, "reativado"),
            _ => Aplicar(operador.RedefinirSegundoFator, "com o segundo fator redefinido"),
        };
        db.AuditoriaDaPlataforma.Add(AuditoriaPlataforma.Registrar(AcoesDaPlataforma.OperadorAlterado, operador.Id, null, $"Comando: {operador.Nome} {frase}", relogio.GetUtcNow().UtcDateTime));
        await db.SaveChangesAsync();
        saida.WriteLine($"Operador {operador.Nome} {frase}.");
        return [];
    }

    static string Aplicar(Action mudanca, string frase)
    {
        mudanca();
        return frase;
    }
}
