using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Usuarios;

public sealed record AlterarTipo(long Id, AlterarTipo.Entrada Corpo, IReadOnlyList<string> FuncoesDoAdmin) : IRequest<IResult>
{
    public sealed record Entrada(string? Nome, string[]? Funcoes);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPut("/tipos/{id:long}", (long id, AlterarTipo.Entrada i, HttpContext ctx, IMediator m) => m.Send(new AlterarTipo(id, i, ctx.Usuario().Tipo!.FuncoesDoTipo())));
}

public sealed class AlterarTipoHandler(EsclariDb db) : IRequestHandler<AlterarTipo, IResult>
{
    public async ValueTask<IResult> Handle(AlterarTipo m, CancellationToken ct)
    {
        var tipo = await db.TiposDeUsuario.Include(t => t.Funcoes).FirstOrDefaultAsync(t => t.Id == m.Id, ct);
        if (tipo is null) return ErrosDeAdmin.NaoEncontrado();
        var e = m.Corpo;
        if (ValidacaoDeTipo.Dados(e.Nome, e.Funcoes) is { } invalido) return invalido;
        var nome = ValidacaoDeTipo.Limpar(e.Nome);
        if (!tipo.Editavel || (tipo.Sistema is not null && nome != tipo.Nome)) return ValidacaoDeTipo.DeSistema("alterado");
        var funcoes = (e.Funcoes ?? []).Distinct().ToList();
        if (ValidacaoDeTipo.PodeDar(m.FuncoesDoAdmin, funcoes) is { } semPoder) return semPoder;
        if (await ValidacaoDeTipo.NomeRepetido(db, nome, tipo.Id) is { } repetido) return repetido;
        tipo.Redefinir(nome, funcoes);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException x) when (ValidacaoDeTipo.NomeJaUsado(x)) { return ErrosDeAdmin.Repetido("Já existe um tipo com este nome."); }
        return Results.Ok(TipoResposta.De(tipo, await db.Usuarios.CountAsync(u => u.TipoUsuarioId == tipo.Id, ct)));
    }
}