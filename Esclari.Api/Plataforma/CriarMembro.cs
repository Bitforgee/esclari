using Mediator;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record CriarMembro(string Slug, CriarMembro.Entrada Corpo, OperadorPlataforma Operador) : IRequest<IResult>
{
    public sealed record Entrada(string? Cpf, string? Nome, string? Email, string[]? Papeis, long[]? Setores);
    public sealed record Resposta(long Id);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/instituicoes/{slug}/equipe", (string slug, Entrada i, HttpContext ctx, IMediator m) => m.Send(new CriarMembro(slug, i, ctx.Operador())));
}

public sealed class CriarMembroHandler(EsclariDb db, TimeProvider relogio, IConfiguration cfg) : IRequestHandler<CriarMembro, IResult>
{
    public async ValueTask<IResult> Handle(CriarMembro m, CancellationToken ct)
    {
        var instituicao = await EquipeDaInstituicao.Alvo(db, m.Slug, ct);
        if (instituicao is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        var e = m.Corpo;
        if (await EquipeDaInstituicao.Validar(db, e) is { } invalido) return invalido;
        if (await EquipeDaInstituicao.Repetido(db, e) is { } repetido) return repetido;

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var agora = relogio.GetUtcNow().UtcDateTime;
        var usuario = db.Usuarios.Add(Usuario.Criar(Cpf.Tentar(e.Cpf)!, e.Nome!.Trim(), e.Email!)).Entity;
        usuario.DefinirAcesso(e.Papeis!, e.Setores ?? []);
        await db.SaveChangesAsync(ct);
        var (token, registro) = TokenAcesso.Criar(instituicao.Slug, usuario.Id, agora);
        db.Tokens.Add(registro);
        db.EmailsSaida.Add(Criacao.EmailDeAcesso(cfg, instituicao, usuario.Email, token, agora, e.Papeis!.Contains(Papeis.Administrador) ? "administrador" : "atendente"));
        db.AuditoriaDaPlataforma.Add(AuditoriaPlataforma.Registrar(AcoesDaPlataforma.UsuarioCriado, m.Operador.Id, instituicao.Id, EquipeDaInstituicao.Resumo("Criou", usuario), agora));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Results.Created($"/api/plataforma/instituicoes/{instituicao.Slug}/equipe/{usuario.Id}", new CriarMembro.Resposta(usuario.Id));
    }
}
