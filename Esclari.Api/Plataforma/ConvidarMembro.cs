using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record ConvidarMembro(string Slug, long Id, OperadorPlataforma Operador) : IRequest<IResult>
{
    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/instituicoes/{slug}/equipe/{id:long}/convite", (string slug, long id, HttpContext ctx, IMediator m) => m.Send(new ConvidarMembro(slug, id, ctx.Operador())));
}

public sealed class ConvidarMembroHandler(EsclariDb db, TimeProvider relogio, IConfiguration cfg) : IRequestHandler<ConvidarMembro, IResult>
{
    public async ValueTask<IResult> Handle(ConvidarMembro m, CancellationToken ct)
    {
        var instituicao = await EquipeDaInstituicao.Alvo(db, m.Slug, ct);
        if (instituicao is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        var usuario = await EquipeDaInstituicao.Membro(db, m.Id, ct);
        if (usuario is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        if (usuario.SenhaHash is not null) return Problemas.Erro(409, "Esta pessoa já definiu a senha.");
        if (!usuario.Ativo) return Problemas.Erro(409, "Reative o usuário antes de convidar.");

        var agora = relogio.GetUtcNow().UtcDateTime;
        foreach (var antigo in await db.Tokens.Where(t => t.UsuarioId == usuario.Id && t.UsadoEm == null).ToListAsync(ct)) antigo.Usar(agora);
        var (token, registro) = TokenAcesso.Criar(instituicao.Slug, usuario.Id, agora);
        db.Tokens.Add(registro);
        db.EmailsSaida.Add(Criacao.EmailDeAcesso(cfg, instituicao, usuario.Email, token, agora, usuario.Tem(Papeis.Administrador) ? "administrador" : "atendente"));
        db.AuditoriaDaPlataforma.Add(AuditoriaPlataforma.Registrar(AcoesDaPlataforma.UsuarioAlterado, m.Operador.Id, instituicao.Id, EquipeDaInstituicao.Resumo("Convidou de novo", usuario), agora));
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
