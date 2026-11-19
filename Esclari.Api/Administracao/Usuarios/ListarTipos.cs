using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Usuarios;

public sealed record ListarTipos : IRequest<IResult>
{
    public sealed record Lista(IReadOnlyList<TipoResposta> Tipos, IReadOnlyList<Lista.DaFuncao> Catalogo)
    {
        public sealed record DaFuncao(string Codigo, string Rotulo, string Descricao, bool ExpoeDadosPessoais, bool DeAluno);
    }

    public sealed record Contagem(long Tipo, int Total);

    public static void Mapear(RouteGroupBuilder g) => g.MapGet("/tipos", (IMediator m) => m.Send(new ListarTipos()));
}

public sealed class ListarTiposHandler(EsclariDb db) : IRequestHandler<ListarTipos, IResult>
{
    public async ValueTask<IResult> Handle(ListarTipos m, CancellationToken ct)
    {
        var contagens = await db.Usuarios.GroupBy(u => u.TipoUsuarioId).Select(g => new ListarTipos.Contagem(g.Key, g.Count())).ToDictionaryAsync(g => g.Tipo, g => g.Total, ct);
        var tipos = await db.TiposDeUsuario.Include(t => t.Funcoes).OrderBy(t => t.Sistema == null).ThenBy(t => t.Sistema).ThenBy(t => t.Nome).ToListAsync(ct);
        var catalogo = DescricaoDaFuncao.Catalogo.Select(d => new ListarTipos.Lista.DaFuncao(d.Codigo, d.Rotulo, d.Descricao, d.ExpoeDadosPessoais, d.DeAluno));
        return Results.Ok(new ListarTipos.Lista([.. tipos.Select(t => TipoResposta.De(t, contagens.GetValueOrDefault(t.Id)))], [.. catalogo]));
    }
}