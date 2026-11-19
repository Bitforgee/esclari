using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record ListarEquipe(string Slug) : IRequest<IResult>
{
    public sealed record Item(long Id, string Nome, string EmailMascarado, string CpfMascarado, Item.DoTipo Tipo, bool Ativo, bool PrimeiroAcessoPendente)
    {
        public sealed record DoTipo(long Id, string Nome, string? Sistema);
    }

    public static void Mapear(RouteGroupBuilder g) => g.MapGet("/instituicoes/{slug}/equipe", (string slug, IMediator m) => m.Send(new ListarEquipe(slug)));
}

public sealed class ListarEquipeHandler(EsclariDb db) : IRequestHandler<ListarEquipe, IResult>
{
    public async ValueTask<IResult> Handle(ListarEquipe m, CancellationToken ct)
    {
        if (await EquipeDaInstituicao.Alvo(db, m.Slug, ct) is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        var equipe = await db.Usuarios.AsNoTracking().Include(u => u.Tipo)
            .Where(u => u.Tipo!.Sistema != TipoDeUsuario.Aluno).OrderBy(u => u.Nome).ToListAsync(ct);
        return Results.Ok(equipe.Select(u => new ListarEquipe.Item(u.Id, u.Nome, EquipeDaInstituicao.MascararEmail(u.Email), EquipeDaInstituicao.Mascarar(u.Cpf),
            new ListarEquipe.Item.DoTipo(u.TipoUsuarioId, u.Tipo!.Nome, u.Tipo.Sistema), u.Ativo, u.SenhaHash is null)));
    }
}
