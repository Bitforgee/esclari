using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Usuarios;

public sealed record AlterarUsuario(long Id, AlterarUsuario.Entrada Corpo, Usuario Admin) : IRequest<IResult>
{
    public sealed record Entrada(string? Cpf, string? Nome, string? Email, string? SenhaTemporaria, string[]? Papeis, long[]? Setores);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPut("/usuarios/{id:long}", (long id, AlterarUsuario.Entrada i, HttpContext ctx, IMediator m) => m.Send(new AlterarUsuario(id, i, ctx.Usuario())));
}

public sealed class AlterarUsuarioHandler(EsclariDb db, IPasswordHasher<Usuario> hasher) : IRequestHandler<AlterarUsuario, IResult>
{
    public async ValueTask<IResult> Handle(AlterarUsuario m, CancellationToken ct)
    {
        var u = await db.Usuarios.Include(x => x.Papeis).Include(x => x.Setores).Include(x => x.Tipo).FirstOrDefaultAsync(x => x.Id == m.Id, ct);
        if (u is null) return ErrosDeAdmin.NaoEncontrado();
        var e = m.Corpo;
        if (await ValidacaoDeUsuario.Validar(db, e.Cpf, e.Nome, e.Email, e.SenhaTemporaria, e.Papeis, e.Setores, m.Id) is { } erro) return erro;
        if (await GuardaDePapeis(u, m) is { } recusa) return recusa;
        u.Atualizar(new DadosUsuario(Cpf.Tentar(e.Cpf)!, e.Nome!, e.Email!));
        await db.AplicarPapeis(u, e.Papeis!, e.Setores ?? []);
        if (e.SenhaTemporaria is not null) u.DefinirSenhaTemporaria(hasher.HashPassword(u, e.SenhaTemporaria));
        await db.SaveChangesAsync(ct);
        return Results.Ok(await UsuarioResposta.De(db, u));
    }

    async Task<IResult?> GuardaDePapeis(Usuario u, AlterarUsuario m)
    {
        var novos = m.Corpo.Papeis!.ToHashSet();
        if (u.Id == m.Admin.Id && !u.Papeis.Select(p => p.Papel).ToHashSet().SetEquals(novos)) return ErrosDeAdmin.ProprioPapel();
        return await ValidacaoDeUsuario.UltimoAdministrador(db, u, PonteDePapeis.EhAdministrador(novos));
    }
}
