using System.Buffers.Binary;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Instituicoes.Aparencia;

public sealed record EnviarImagemInstituicao(string Tipo, IFormCollection Form) : IRequest<IResult>
{
    public sealed record Resposta(string Tipo, int Versao);

    public const string IconeInvalido = "O ícone deve ser um PNG ou JPEG quadrado de pelo menos 512 px.";
    const long UmMb = 1024 * 1024;

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/aparencia/imagens/{tipo}", async (string tipo, HttpRequest req, IMediator m) =>
            await m.Send(new EnviarImagemInstituicao(tipo, req.HasFormContentType ? await req.ReadFormAsync() : FormCollection.Empty)));

    public static long Limite(string tipo) => tipo == TiposDeImagem.Login ? 3 * UmMb : UmMb;

    public static bool IconeAceito(byte[] imagem, string tipo) =>
        (tipo == Arquivos.Png ? DoPng(imagem) : DoJpeg(imagem)) is var (largura, altura) && largura == altura && largura >= 512;

    static (long, long)? DoPng(byte[] png) =>
        png.Length < 24 ? null : (BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16)), BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20)));

    static (long, long)? DoJpeg(byte[] jpeg)
    {
        var i = 2;
        while (i + 9 <= jpeg.Length && jpeg[i] == 0xFF)
        {
            if (InicioDeQuadro(jpeg[i + 1])) return (BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(i + 7)), BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(i + 5)));
            i += 2 + BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(i + 2));
        }
        return null;
    }

    static bool InicioDeQuadro(byte marcador) => marcador is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC);
}

public sealed class EnviarImagemInstituicaoHandler(EsclariDb db, Armazenamento armazenamento) : IRequestHandler<EnviarImagemInstituicao, IResult>
{
    public async ValueTask<IResult> Handle(EnviarImagemInstituicao m, CancellationToken ct)
    {
        var tipo = m.Tipo;
        if (!TiposDeImagem.Todos.Contains(tipo)) return Problemas.Erro(404, "Tipo de imagem não encontrado.");
        if (m.Form.Files["arquivo"] is not { } arquivo) return Problemas.Invalido("Envie a imagem no campo arquivo.", "arquivo");

        var icone = tipo == TiposDeImagem.Icone;
        var limite = EnviarImagemInstituicao.Limite(tipo);
        var (lido, recusa) = await Arquivos.Ler(arquivo, limite, [Arquivos.Png, Arquivos.Jpeg],
            $"A imagem passa de {limite / 1024 / 1024} MB.", icone ? "O ícone deve ser PNG ou JPEG." : "Envie uma imagem PNG ou JPEG.");
        if (lido is null) return recusa!;
        if (icone && !EnviarImagemInstituicao.IconeAceito(lido.Bytes, lido.Tipo)) return Problemas.Invalido(EnviarImagemInstituicao.IconeInvalido, "arquivo");

        var chave = Armazenamento.NovaChave(db.InstituicaoAtual!.Value, "aparencia");
        await armazenamento.Gravar(chave, lido.Bytes);
        var imagem = await db.ImagensInstituicao.FirstOrDefaultAsync(x => x.Tipo == tipo, ct);
        if (imagem is null) db.ImagensInstituicao.Add(imagem = ImagemInstituicao.Criar(tipo));
        imagem.Substituir(lido.Tipo, lido.Bytes.Length, Armazenamento.Hash(lido.Bytes), chave);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new EnviarImagemInstituicao.Resposta(tipo, imagem.Versao));
    }
}
