using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Plataforma;

public sealed record ReenviarAcesso(string Slug, OperadorPlataforma Operador) : IRequest<IResult>
{
    public sealed record Resposta(int Enviados);

    public static void Mapear(RouteGroupBuilder g) => g.MapPost("/instituicoes/{slug}/reenviar-acesso", (string slug, HttpContext ctx, IMediator m) => m.Send(new ReenviarAcesso(slug, ctx.Operador())));
}

public sealed class ReenviarAcessoHandler(EsclariDb db, TimeProvider relogio, IConfiguration cfg) : IRequestHandler<ReenviarAcesso, IResult>
{
    public async ValueTask<IResult> Handle(ReenviarAcesso m, CancellationToken ct)
    {
        var instituicao = await db.Instituicoes.FirstOrDefaultAsync(i => i.Slug == m.Slug, ct);
        if (instituicao is null) return Problemas.Erro(404, ErrosDaPlataforma.NaoEncontrada);
        if (!instituicao.Ativa) return Problemas.Erro(409, "A instituição está desativada.");

        db.InstituicaoAtual = instituicao.Id;
        var admins = await db.Usuarios.Where(u => u.Ativo && u.Papeis.Any(p => p.Papel == Papeis.Administrador)).ToListAsync(ct);
        if (admins.Count == 0) return Problemas.Erro(409, "Esta instituição não tem administrador ativo para receber o acesso.");

        var agora = relogio.GetUtcNow().UtcDateTime;
        foreach (var admin in admins)
        {
            var (token, registro) = TokenAcesso.Criar(instituicao.Slug, admin.Id, agora);
            db.Tokens.Add(registro);
            db.EmailsSaida.Add(Criacao.EmailDeAcesso(cfg, instituicao, admin.Email, token, agora));
        }
        db.AuditoriaDaPlataforma.Add(AuditoriaPlataforma.Registrar(AcoesDaPlataforma.AcessoReenviado, m.Operador.Id, instituicao.Id, $"Reenviou o acesso de {admins.Count} administrador(es) de {instituicao.Slug}", agora));
        await db.SaveChangesAsync(ct);
        return Results.Accepted(value: new ReenviarAcesso.Resposta(admins.Count));
    }
}
