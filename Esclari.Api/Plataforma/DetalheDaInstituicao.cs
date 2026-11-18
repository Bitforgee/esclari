using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record DetalheDaInstituicao(string Slug) : IRequest<IResult>
{
    public sealed record Administrador(string Nome, string EmailMascarado, bool PrimeiroAcessoPendente);

    public sealed record Resposta(
        string Slug, string Nome, string Fuso, bool Ativa, DateTime CriadaEm, int Administradores, string Situacao, Saude Saude, IReadOnlyList<string> Atencao,
        IReadOnlyList<Administrador> AdministradoresDaInstituicao);

    public static void Mapear(RouteGroupBuilder g) => g.MapGet("/instituicoes/{slug}", (string slug, IMediator m) => m.Send(new DetalheDaInstituicao(slug)));

    public static string Mascarar(string email)
    {
        var arroba = email.IndexOf('@');
        return arroba <= 0 ? "***" : email[..1] + "***" + email[arroba..];
    }
}

public sealed class DetalheDaInstituicaoHandler(EsclariDb db, TimeProvider relogio, Prazos prazos) : IRequestHandler<DetalheDaInstituicao, IResult>
{
    public sealed class LinhaDeAdministrador
    {
        public string Nome { get; set; } = "";
        public string Email { get; set; } = "";
        public bool SemSenha { get; set; }
    }

    public async ValueTask<IResult> Handle(DetalheDaInstituicao m, CancellationToken ct)
    {
        var item = (await ConsultaDasInstituicoes.Ler(db, relogio.GetUtcNow().UtcDateTime, prazos, m.Slug)).FirstOrDefault();
        if (item is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        var administradores = await db.Database.SqlQuery<LinhaDeAdministrador>($"""
            SELECT u.nome AS "Nome", u.email AS "Email", (u.senha_hash IS NULL) AS "SemSenha"
            FROM usuario u JOIN instituicao i ON i.id = u.instituicao_id
            WHERE i.slug = {m.Slug} AND u.ativo AND EXISTS (SELECT 1 FROM usuario_papel p WHERE p.usuario_id = u.id AND p.papel = 'ADMINISTRADOR')
            ORDER BY u.nome
            """).ToListAsync(ct);
        return Results.Ok(new DetalheDaInstituicao.Resposta(item.Slug, item.Nome, item.Fuso, item.Ativa, item.CriadaEm, item.Administradores, item.Situacao, item.Saude, item.Atencao,
            [.. administradores.Select(a => new DetalheDaInstituicao.Administrador(a.Nome, DetalheDaInstituicao.Mascarar(a.Email), a.SemSenha))]));
    }
}
