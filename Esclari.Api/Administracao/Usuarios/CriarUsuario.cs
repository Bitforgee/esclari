using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Usuarios;

public sealed record CriarUsuario(CriarUsuario.Entrada Corpo, IReadOnlyList<string> FuncoesDoAdmin) : IRequest<IResult>
{
    public sealed record Entrada(string? Cpf, string? Nome, string? Email, string? SenhaTemporaria, long? TipoId, long[]? Setores);

    public static void Mapear(RouteGroupBuilder g) => g.MapPost("/usuarios", (CriarUsuario.Entrada i, HttpContext ctx, IMediator m) => m.Send(new CriarUsuario(i, ctx.Usuario().Tipo!.FuncoesDoTipo())));
}

public sealed class CriarUsuarioHandler(EsclariDb db, IPasswordHasher<Usuario> hasher) : IRequestHandler<CriarUsuario, IResult>
{
    public async ValueTask<IResult> Handle(CriarUsuario m, CancellationToken ct)
    {
        var e = m.Corpo;
        var (erro, tipo) = await ValidacaoDeUsuario.Validar(db, e.Cpf, e.Nome, e.Email, e.SenhaTemporaria, e.TipoId, e.Setores, 0);
        if (erro is not null) return erro;
        if (ValidacaoDeUsuario.PodeAtribuir(m.FuncoesDoAdmin, tipo!, null) is { } recusa) return recusa;
        var u = db.Usuarios.Add(Usuario.Criar(Cpf.Tentar(e.Cpf)!, e.Nome!, e.Email!)).Entity;
        u.DefinirAcesso(tipo!, e.Setores ?? []);
        if (e.SenhaTemporaria is not null) u.DefinirSenhaTemporaria(hasher.HashPassword(u, e.SenhaTemporaria));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException x) when (x.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation }) { return Problemas.Invalido("Escolha o tipo do usuário.", "tipoId"); }
        return Results.Created($"/api/admin/usuarios/{u.Id}", await UsuarioResposta.De(db, u));
    }
}