using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Integracoes;

public sealed record RevogarTokenMcp(long Id) : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) => g.MapDelete("/mcp/tokens/{id:long}", (long id, IMediator m) => m.Send(new RevogarTokenMcp(id)));
}

public sealed class RevogarTokenMcpHandler(EsclariDb db, TimeProvider relogio) : IRequestHandler<RevogarTokenMcp, IResult>
{
    public async ValueTask<IResult> Handle(RevogarTokenMcp m, CancellationToken ct)
    {
        var token = await db.TokensMcp.FirstOrDefaultAsync(t => t.Id == m.Id, ct);
        if (token is null) return ErrosDeAdmin.NaoEncontrado();
        token.Revogar(relogio.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
