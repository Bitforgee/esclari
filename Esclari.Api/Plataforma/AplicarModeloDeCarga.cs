using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record AplicarModeloDeCarga(string Slug, string Id, OperadorPlataforma Operador) : IRequest<IResult>
{
    public sealed record Resposta(int Setores, int Cursos, int Categorias, int Regras, int Artigos);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/instituicoes/{slug}/carga/modelo/{id}", (string slug, string id, HttpContext ctx, IMediator m) => m.Send(new AplicarModeloDeCarga(slug, id, ctx.Operador())));
}

public sealed class AplicarModeloDeCargaHandler(EsclariDb db, TimeProvider relogio, Prazos prazos) : IRequestHandler<AplicarModeloDeCarga, IResult>
{
    public async ValueTask<IResult> Handle(AplicarModeloDeCarga m, CancellationToken ct)
    {
        var instituicao = await db.Instituicoes.FirstOrDefaultAsync(i => i.Slug == m.Slug, ct);
        var modelo = ModelosDeCarga.Achar(m.Id);
        if (instituicao is null || modelo is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        var (c, erro) = await CargaDoOperador.Aplicar(db, instituicao, modelo.Arquivo, false, relogio.GetUtcNow().UtcDateTime, prazos.Para(instituicao).ValidadeConhecimentoMeses, m.Operador);
        return erro ?? Results.Ok(new AplicarModeloDeCarga.Resposta(c!.Setores, c.Cursos, c.Categorias, c.Regras, c.Artigos));
    }
}
