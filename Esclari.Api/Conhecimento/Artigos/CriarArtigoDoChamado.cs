using Mediator;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using static Esclari.Api.Conhecimento.Artigos.ValidacaoDeArtigo;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Conhecimento.Artigos;

public sealed record CriarArtigoDoChamado(string Numero, CriarArtigoDoChamado.Entrada Corpo, long UsuarioId, bool Atendente, bool Administrador) : IRequest<IResult>
{
    public sealed record Entrada(string? Titulo, string[]? Perguntas, string? Resposta, string[]? Links);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapPost("/chamados/{numero}/artigo", (string numero, CriarArtigoDoChamado.Entrada i, HttpContext ctx, IMediator m) => m.Send(Do(numero, i, ctx.Usuario())));

    static CriarArtigoDoChamado Do(string numero, Entrada corpo, Usuario u) => new(numero, corpo, u.Id, u.Tem(Papeis.Atendente), u.Tem(Papeis.Administrador));
}

public sealed class CriarArtigoDoChamadoHandler(EsclariDb db, TimeProvider relogio, PrazosAtuais atuais, ILogger<CriarArtigoDoChamadoHandler> log) : IRequestHandler<CriarArtigoDoChamado, IResult>
{
    public async ValueTask<IResult> Handle(CriarArtigoDoChamado m, CancellationToken ct)
    {
        var prazos = await atuais.Ler();
        var c = await db.Chamados.FirstOrDefaultAsync(x => x.Numero == m.Numero, ct);
        if (c is null) return NaoEncontrado();
        if (await Acesso(c, m) is { } recusa) return recusa;
        var i = m.Corpo;
        if ((await Validar(db, i.Titulo, i.Perguntas, c.CategoriaId, i.Resposta, i.Links, null, null) ?? ComCpf(i)) is { } erro) return erro;
        if (c.Estado is not (Resolvido or Fechado)) return Problemas.Erro(409, "O chamado ainda não foi resolvido.");
        if (await JaOriginou(c.Id) is { } repetido) return repetido;
        var validade = await db.NovaValidade(relogio, prazos);
        var a = Artigo.DoChamado(c.Id, c.CategoriaId, Versao(i.Titulo, i.Perguntas, i.Resposta, i.Links, 1, relogio.GetUtcNow().UtcDateTime), validade);
        db.Artigos.Add(a);
        if (!await Gravou()) return await JaOriginou(c.Id) ?? throw new InvalidOperationException("Conflito sem artigo de origem.");
        log.LogInformation("Rascunho de artigo criado a partir do chamado {Numero} (instituição {InstituicaoId})", c.Numero, c.InstituicaoId);
        return Results.Created($"/api/admin/artigos/{a.Id}", await ArtigoResposta.De(db, a, validade.Hoje));
    }

    static IResult NaoEncontrado() => Problemas.Erro(404, "Chamado não encontrado.");

    async Task<IResult?> Acesso(Chamado c, CriarArtigoDoChamado m)
    {
        if (c.SolicitanteId == m.UsuarioId) return Proibido();
        if (m.Atendente && await db.AtendenteSetores.AnyAsync(a => a.UsuarioId == m.UsuarioId && a.SetorId == c.SetorId)) return null;
        return m.Administrador ? Proibido() : NaoEncontrado();
    }

    static IResult Proibido() => Problemas.Erro(403, "Só a equipe do setor do chamado cria artigo a partir dele.");

    static IResult? ComCpf(CriarArtigoDoChamado.Entrada i)
    {
        const string Mensagem = "Remova o CPF do texto antes de salvar.";
        if (Cpf.NoTexto(i.Titulo)) return Problemas.Invalido(Mensagem, "titulo");
        if ((i.Perguntas ?? []).Any(Cpf.NoTexto)) return Problemas.Invalido(Mensagem, "perguntas");
        return Cpf.NoTexto(i.Resposta) ? Problemas.Invalido(Mensagem, "resposta") : null;
    }

    async Task<IResult?> JaOriginou(long chamadoId)
    {
        var artigo = await db.Artigos.Include(a => a.Versoes).FirstOrDefaultAsync(a => a.OrigemChamadoId == chamadoId);
        return artigo is null ? null : Problemas.Erro(409, $"Este chamado já originou o artigo “{artigo.UltimaVersao.Titulo}”.");
    }

    async Task<bool> Gravou()
    {
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException e) when (Problemas.Conflito(e))
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }
}
