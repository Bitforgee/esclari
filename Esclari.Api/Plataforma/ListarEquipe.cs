using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record ListarEquipe(string Slug) : IRequest<IResult>
{
    public sealed record Item(long Id, string Nome, string EmailMascarado, string CpfMascarado, IReadOnlyList<string> Papeis, bool Ativo, bool PrimeiroAcessoPendente);

    public static void Mapear(RouteGroupBuilder g) => g.MapGet("/instituicoes/{slug}/equipe", (string slug, IMediator m) => m.Send(new ListarEquipe(slug)));
}

public sealed class ListarEquipeHandler(EsclariDb db) : IRequestHandler<ListarEquipe, IResult>
{
    public async ValueTask<IResult> Handle(ListarEquipe m, CancellationToken ct)
    {
        if (await EquipeDaInstituicao.Alvo(db, m.Slug, ct) is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        var equipe = await db.Usuarios.AsNoTracking().Include(u => u.Papeis)
            .Where(u => u.Papeis.Any(p => p.Papel == Papeis.Administrador || p.Papel == Papeis.Atendente)).OrderBy(u => u.Nome).ToListAsync(ct);
        return Results.Ok(equipe.Select(u => new ListarEquipe.Item(u.Id, u.Nome, EquipeDaInstituicao.MascararEmail(u.Email), EquipeDaInstituicao.Mascarar(u.Cpf),
            [.. u.Papeis.Select(p => p.Papel).Where(EquipeDaInstituicao.PapeisPermitidos.Contains).Order()], u.Ativo, u.SenhaHash is null)));
    }
}
