using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Usuarios;

public sealed record AlterarUsuario(long Id, AlterarUsuario.Entrada Corpo, Usuario Admin) : IRequest<IResult>
{
    public sealed record Entrada(string? Cpf, string? Nome, string? Email, string? SenhaTemporaria, long? TipoId, long[]? Setores);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPut("/usuarios/{id:long}", (long id, AlterarUsuario.Entrada i, HttpContext ctx, IMediator m) => m.Send(new AlterarUsuario(id, i, ctx.Usuario())));
}

public sealed class AlterarUsuarioHandler(EsclariDb db, IPasswordHasher<Usuario> hasher) : IRequestHandler<AlterarUsuario, IResult>
{
    public async ValueTask<IResult> Handle(AlterarUsuario m, CancellationToken ct)
    {
        var u = await db.Usuarios.Include(x => x.Setores).Include(x => x.Tipo).ThenInclude(t => t!.Funcoes).FirstOrDefaultAsync(x => x.Id == m.Id, ct);
        if (u is null) return ErrosDeAdmin.NaoEncontrado();
        var e = m.Corpo;
        var (erro, tipo) = await ValidacaoDeUsuario.Validar(db, e.Cpf, e.Nome, e.Email, e.SenhaTemporaria, e.TipoId, e.Setores, m.Id);
        if (erro is not null) return erro;
        if (ValidacaoDeUsuario.PodeAtribuir(m.Admin.Tipo!.FuncoesDoTipo(), tipo!, u.Tipo) is { } semPoder) return semPoder;
        if (await GuardaDoTipo(u, m, tipo!) is { } recusa) return recusa;
        u.Atualizar(new DadosUsuario(Cpf.Tentar(e.Cpf)!, e.Nome!, e.Email!));
        u.DefinirAcesso(tipo!, e.Setores ?? []);
        if (e.SenhaTemporaria is not null) u.DefinirSenhaTemporaria(hasher.HashPassword(u, e.SenhaTemporaria));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException x) when (x.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation }) { return Problemas.Invalido("Escolha o tipo do usuário.", "tipoId"); }
        return Results.Ok(await UsuarioResposta.De(db, u));
    }

    async Task<IResult?> GuardaDoTipo(Usuario u, AlterarUsuario m, TipoDeUsuario novo)
    {
        if (u.Id == m.Admin.Id && u.TipoUsuarioId != novo.Id) return ErrosDeAdmin.ProprioTipo();
        return await ValidacaoDeUsuario.UltimoGestor(db, u, novo.Sistema == TipoDeUsuario.Gestor);
    }
}