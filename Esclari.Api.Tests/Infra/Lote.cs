using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Tests.Infra;

public static class Lote
{
    public static Task Clonar(this EsclariFactory f, Instituicao i, string numero, int quantos) =>
        f.Db(i.Id, db => db.Database.ExecuteSqlInterpolatedAsync($"""
            WITH base AS (SELECT * FROM chamado WHERE numero = {numero} AND instituicao_id = {i.Id}),
            novas AS (
                INSERT INTO conversa (instituicao_id, aluno_id, criada_em, desfecho, desfecho_em, ultima_mensagem_em)
                SELECT c.instituicao_id, c.aluno_id, c.criada_em, c.desfecho, c.desfecho_em, c.ultima_mensagem_em
                FROM conversa c JOIN base b ON c.id = b.conversa_id, generate_series(1, {quantos})
                RETURNING id)
            INSERT INTO chamado (instituicao_id, numero, conversa_id, solicitante_id, matricula_id, semestre, titulo, resumo, categoria_id, categoria_original_id,
                origem_categoria, setor_id, responsavel_id, estado, motivo_fechamento, reaberturas, encaminhamentos, criado_em, resolvido_em, fechado_em,
                aguardando_desde, lembrete_em, versao_fluxo_id, etapa_ordem, etapa_desde, numero_original)
            SELECT b.instituicao_id, 'LOTE-' || lpad(n.id::text, 9, '0'), n.id, b.solicitante_id, b.matricula_id, b.semestre, b.titulo, b.resumo, b.categoria_id, b.categoria_original_id,
                b.origem_categoria, b.setor_id, b.responsavel_id, b.estado, b.motivo_fechamento, b.reaberturas, b.encaminhamentos, b.criado_em, b.resolvido_em, b.fechado_em,
                b.aguardando_desde, b.lembrete_em, b.versao_fluxo_id, b.etapa_ordem, b.etapa_desde, b.numero_original
            FROM novas n CROSS JOIN base b
            """));

    public static Task Criado(this EsclariFactory f, Instituicao i, string numero, DateTime criadoEm) =>
        f.Db(i.Id, db => db.Chamados.Where(c => c.Numero == numero).ExecuteUpdateAsync(s => s.SetProperty(c => c.CriadoEm, criadoEm)));

    public static Task Renomear(this EsclariFactory f, Instituicao i, long usuarioId, string nome) =>
        f.Db(i.Id, db => db.Usuarios.Where(u => u.Id == usuarioId).ExecuteUpdateAsync(s => s.SetProperty(u => u.Nome, nome)));
}
