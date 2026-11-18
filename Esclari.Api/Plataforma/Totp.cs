using System.Security.Cryptography;

namespace Esclari.Api.Plataforma;

public static class Totp
{
    public const int Digitos = 6;
    public const int Passo = 30;
    const string AlfabetoBase32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string NovoSegredo() => Base32(RandomNumberGenerator.GetBytes(20));

    public static long PassoDe(DateTimeOffset instante) => instante.ToUnixTimeSeconds() / Passo;

    public static string Codigo(string segredoBase32, long passo, int digitos = Digitos)
    {
        var contador = BitConverter.GetBytes(passo);
        if (BitConverter.IsLittleEndian) Array.Reverse(contador);
        var hash = HMACSHA1.HashData(DeBase32(segredoBase32), contador);
        var deslocamento = hash[^1] & 0x0F;
        var binario = ((hash[deslocamento] & 0x7F) << 24) | (hash[deslocamento + 1] << 16) | (hash[deslocamento + 2] << 8) | hash[deslocamento + 3];
        return (binario % (int)Math.Pow(10, digitos)).ToString().PadLeft(digitos, '0');
    }

    public static long? Verificar(string segredoBase32, string? codigo, DateTimeOffset agora, long? ultimoPasso)
    {
        if (codigo is not { Length: Digitos } || !codigo.All(char.IsAsciiDigit)) return null;
        var atual = PassoDe(agora);
        for (var passo = atual - 1; passo <= atual + 1; passo++)
            if (passo > (ultimoPasso ?? -1) && CryptographicOperations.FixedTimeEquals(System.Text.Encoding.ASCII.GetBytes(Codigo(segredoBase32, passo)), System.Text.Encoding.ASCII.GetBytes(codigo)))
                return passo;
        return null;
    }

    public static string Uri(string email, string segredoBase32) =>
        $"otpauth://totp/Esclari:{email}?secret={segredoBase32}&issuer=Esclari";

    public static string Base32(byte[] dados)
    {
        var saida = new System.Text.StringBuilder();
        int buffer = 0, bits = 0;
        foreach (var b in dados)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                saida.Append(AlfabetoBase32[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }
        if (bits > 0) saida.Append(AlfabetoBase32[(buffer << (5 - bits)) & 31]);
        return saida.ToString();
    }

    public static byte[] DeBase32(string texto)
    {
        var saida = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in texto.TrimEnd('=').ToUpperInvariant())
        {
            buffer = (buffer << 5) | AlfabetoBase32.IndexOf(c);
            bits += 5;
            if (bits >= 8)
            {
                saida.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }
        return [.. saida];
    }
}
