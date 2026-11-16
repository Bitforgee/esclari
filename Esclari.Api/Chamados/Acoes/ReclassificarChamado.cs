using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Chamados.Permissao;
using Esclari.Api.Dados;
using static Esclari.Api.Chamados.Permissao.AcoesChamado;

namespace Esclari.Api.Chamados.Acoes;

public sealed record ReclassificarChamado(string Numero, ReclassificarChamado.Entrada Corpo, Usuario Usuario, Instituicao Instituicao) : IAcaoChamado
{
    public sealed record Entrada(long? CategoriaId);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/chamados/{numero}/reclassificar", (string numero, ReclassificarChamado.Entrada i, HttpContext ctx, IMediator m) => m.Send(new ReclassificarChamado(numero, i, ctx.Usuario(), ctx.Instituicao())));

    public string Acao => "reclassificar";

    public async ValueTask<IResult?> Validar(AcessoChamado a, EsclariDb db) =>
        await db.Categorias.AnyAsync(c => c.Id == Corpo.CategoriaId && c.Ativa) ? null : Problemas.Invalido("Escolha uma categoria ativa.", "categoriaId");
}

public sealed class ReclassificarChamadoHandler(EsclariDb db, AcessoChamado a) : IRequestHandler<ReclassificarChamado, IResult>
{
    public async ValueTask<IResult> Handle(ReclassificarChamado m, CancellationToken ct)
    {
        var i = m.Corpo;
        var nomes = await db.Categorias.Where(c => c.Id == a.Chamado.CategoriaId || c.Id == i.CategoriaId).ToDictionaryAsync(c => c.Id, c => c.Nome);
        a.Chamado.Reclassificar(m.Usuario.Id, i.CategoriaId!.Value, nomes[a.Chamado.CategoriaId], nomes[i.CategoriaId.Value], a.Agora);
        return Results.Empty;
    }
}
