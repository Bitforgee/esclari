using Mediator;
using Esclari.Api.Chamados.Acoes;
using Esclari.Api.Chamados.Consulta;
using Esclari.Api.Dados;
using Esclari.Api.Dados.Fluxos;
using static Esclari.Api.Assistente.Conversas.ConversaDoAluno;

namespace Esclari.Api.Assistente.Conversas;

public sealed record VerDestino(long Id, long? MatriculaId, Usuario Usuario) : IRequest<IResult>
{
    public sealed record Resposta(bool Lideranca);

    public static void Mapear(RouteGroupBuilder g) =>
        g.MapGet("/conversas/{id:long}/destino", (long id, long? matriculaId, HttpContext ctx, IMediator m) => m.Send(new VerDestino(id, matriculaId, ctx.Usuario())));
}

public sealed class VerDestinoHandler(EsclariDb db) : IRequestHandler<VerDestino, IResult>
{
    public async ValueTask<IResult> Handle(VerDestino m, CancellationToken ct)
    {
        var conversa = await DoAluno(db, m.Usuario, m.Id);
        if (conversa is null) return NaoEncontrada();
        if (conversa.Desfecho is not null) return JaEncerrada();
        var matricula = await Transferencia.Matricula(db, conversa.AlunoId, m.MatriculaId);
        if (matricula is null) return Transferencia.MatriculaInvalida();
        var (categoria, _) = await Transferencia.EscolherCategoria(db, conversa);
        var rota = await ConsultasDeFluxo.Rotear(db, categoria, matricula, true);
        return Results.Ok(new VerDestino.Resposta(rota.Passo is { } p && DestinosEtapa.PedeCiencia(p.Etapa.Destino)));
    }
}
