using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Administracao.Usuarios;

public sealed record CriarTipo(CriarTipo.Entrada Corpo, IReadOnlyList<string> FuncoesDoAdmin) : IRequest<IResult>
{
    public sealed record Entrada(string? Nome, string[]? Funcoes);

    public static void Mapear(RouteGroupBuilder g) => g.MapPost("/tipos", (CriarTipo.Entrada i, HttpContext ctx, IMediator m) => m.Send(new CriarTipo(i, ctx.Usuario().Tipo!.FuncoesDoTipo())));
}

public sealed class CriarTipoHandler(EsclariDb db) : IRequestHandler<CriarTipo, IResult>
{
    public async ValueTask<IResult> Handle(CriarTipo m, CancellationToken ct)
    {
        var e = m.Corpo;
        if (ValidacaoDeTipo.Dados(e.Nome, e.Funcoes) is { } invalido) return invalido;
        var funcoes = (e.Funcoes ?? []).Distinct().ToList();
        if (ValidacaoDeTipo.PodeDar(m.FuncoesDoAdmin, funcoes) is { } semPoder) return semPoder;
        var nome = ValidacaoDeTipo.Limpar(e.Nome);
        if (await db.TiposDeUsuario.CountAsync(ct) >= ValidacaoDeTipo.Maximo) return ErrosDeAdmin.Repetido($"A instituição já tem {ValidacaoDeTipo.Maximo} tipos de usuário.");
        if (await ValidacaoDeTipo.NomeRepetido(db, nome, null) is { } repetido) return repetido;
        var tipo = db.TiposDeUsuario.Add(TipoDeUsuario.Criar(nome, null, funcoes)).Entity;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException x) when (ValidacaoDeTipo.NomeJaUsado(x)) { return ErrosDeAdmin.Repetido("Já existe um tipo com este nome."); }
        return Results.Created($"/api/admin/tipos/{tipo.Id}", TipoResposta.De(tipo, 0));
    }
}