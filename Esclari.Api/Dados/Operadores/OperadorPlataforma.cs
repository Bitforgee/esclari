using System.Buffers.Text;
using System.Security.Cryptography;

namespace Esclari.Api.Dados.Operadores;

public class OperadorPlataforma
{
    public static readonly TimeSpan ValidadeDoConvite = TimeSpan.FromMinutes(60);

    OperadorPlataforma() { }

    public long Id { get; private set; }
    public string Nome { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string? SenhaHash { get; private set; }
    public bool Ativo { get; private set; } = true;
    public string? TotpSegredo { get; private set; }
    public long? TotpUltimoPasso { get; private set; }
    public string? ConviteHash { get; private set; }
    public DateTime? ConviteExpiraEm { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public bool TemSenha => SenhaHash is not null;
    public bool TemSegundoFator => TotpSegredo is not null;

    public static OperadorPlataforma Criar(string nome, string email, DateTime agora) =>
        new() { Nome = nome.Trim(), Email = email.Trim().ToLowerInvariant(), CriadoEm = agora };

    public string NovoConvite(DateTime agora)
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        ConviteHash = Tokens.Hash(token);
        ConviteExpiraEm = agora + ValidadeDoConvite;
        return token;
    }

    public bool ConviteValido(DateTime agora) => ConviteHash is not null && ConviteExpiraEm > agora;

    public void DefinirSenha(string hash)
    {
        SenhaHash = hash;
        ConviteHash = null;
        ConviteExpiraEm = null;
    }

    public void Desativar() => Ativo = false;

    public void Reativar() => Ativo = true;

    public void CadastrarSegundoFator(string segredoProtegido, long passo) => (TotpSegredo, TotpUltimoPasso) = (segredoProtegido, passo);

    public void RegistrarPasso(long passo) => TotpUltimoPasso = passo;

    public void RedefinirSegundoFator() => (TotpSegredo, TotpUltimoPasso) = (null, null);
}
