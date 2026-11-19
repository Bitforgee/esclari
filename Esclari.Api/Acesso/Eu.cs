using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Acesso;

public sealed record Eu(Usuario Usuario, Instituicao Instituicao) : IRequest<IResult>
{
    public sealed record Resposta(long Id, string Nome, string CpfMascarado, Resposta.DoTipo Tipo, IReadOnlyList<string> Permissoes, bool DeveTrocarSenha,
        Resposta.DaInstituicao Instituicao, IReadOnlyList<Resposta.DaMatricula> Matriculas)
    {
        public sealed record DoTipo(long Id, string Nome);

        public sealed record DaInstituicao(string Slug, string Nome, string Fuso);

        public sealed record DaMatricula(long Id, string Numero, string Curso, int Semestre, string Situacao);
    }

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapGet("/auth/me", (HttpContext ctx, IMediator m) => m.Send(new Eu(ctx.Usuario(), ctx.Instituicao())));
}

public sealed class EuHandler(EsclariDb db) : IRequestHandler<Eu, IResult>
{
    public async ValueTask<IResult> Handle(Eu m, CancellationToken ct)
    {
        var u = m.Usuario;
        var i = m.Instituicao;
        var matriculas = await db.Matriculas.Where(x => x.UsuarioId == u.Id).OrderBy(x => x.Numero)
            .Select(x => new Eu.Resposta.DaMatricula(x.Id, x.Numero, x.Curso!.Nome, x.Semestre, x.Situacao))
            .ToListAsync();
        return Results.Ok(new Eu.Resposta(u.Id, u.Nome, u.Cpf.Mascarado, new Eu.Resposta.DoTipo(u.TipoUsuarioId, u.Tipo!.Nome), u.FuncoesEfetivas(), u.DeveTrocarSenha,
            new Eu.Resposta.DaInstituicao(i.Slug, i.Nome, i.Fuso), matriculas));
    }
}
