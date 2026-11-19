using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Usuarios;

public sealed record ApagarTipo(long Id) : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) => g.MapDelete("/tipos/{id:long}", (long id, IMediator m) => m.Send(new ApagarTipo(id)));
}

public sealed class ApagarTipoHandler(EsclariDb db) : IRequestHandler<ApagarTipo, IResult>
{
    public async ValueTask<IResult> Handle(ApagarTipo m, CancellationToken ct)
    {
        var tipo = await db.TiposDeUsuario.Include(t => t.Funcoes).FirstOrDefaultAsync(t => t.Id == m.Id, ct);
        if (tipo is null) return ErrosDeAdmin.NaoEncontrado();
        if (tipo.Sistema is not null) return ValidacaoDeTipo.DeSistema("apagado");
        var emUso = await db.Usuarios.CountAsync(u => u.TipoUsuarioId == tipo.Id, ct);
        if (emUso > 0) return ErrosDeAdmin.Repetido($"{emUso} usuário(s) usam este tipo.");
        db.TiposDeUsuario.Remove(tipo);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException x) when (x.InnerException is PostgresException { SqlState: PostgresErrorCodes.RestrictViolation or PostgresErrorCodes.ForeignKeyViolation })
        {
            return ErrosDeAdmin.Repetido("Este tipo acabou de ser atribuído a um usuário e não pode ser apagado.");
        }
        return Results.NoContent();
    }
}