using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;

namespace Esclari.Api.Instituicoes.Carga;

public static class CargaInicial
{
    public static async Task<Existentes> Existentes(EsclariDb db) =>
        new(await db.Setores.ToDictionaryAsync(s => s.Nome), await db.Categorias.ToDictionaryAsync(c => c.Nome),
            (await db.Cursos.Select(c => c.Nome).ToListAsync()).ToHashSet(), (await db.Categorias.Where(c => db.Fluxos.Any(f => f.CategoriaId == c.Id && f.CursoId == null && f.Semestre == null)).Select(c => c.Nome).ToListAsync()).ToHashSet());

    public static async Task<(int, int)> Gravar(EsclariDb db, ArquivoCarga arq, Existentes existentes, Plano plano, DateTime agora, string fuso, int mesesDeValidade, AuditoriaPlataforma? auditoria = null)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var novosSetores = plano.Setores.Values.Except(existentes.Setores.Values).ToList();
        var novasCategorias = plano.Categorias.Values.Except(existentes.Categorias.Values).ToList();
        db.Setores.AddRange(novosSetores);
        db.Categorias.AddRange(novasCategorias);
        await db.SaveChangesAsync();
        foreach (var (c, i) in (arq.Cursos ?? []).Select((c, i) => (c, i)))
            plano.Cursos[i].DefinirCoordenacao(c.Coordenacao is null ? null : plano.Setores[c.Coordenacao].Id);
        db.Cursos.AddRange(plano.Cursos);
        foreach (var (destino, categoria, setor) in plano.Regras)
        {
            var fluxo = Fluxo.Criar(categoria.Id, null, null);
            fluxo.SalvarRascunho([new EtapaFluxo.Dados(destino, setor?.Id, null, null, null)]);
            fluxo.Publicar(agora);
            db.Fluxos.Add(fluxo);
        }
        foreach (var (a, categoria) in plano.Artigos)
        {
            var versao = VersaoArtigo.Criar(new(a.Titulo!, a.Perguntas!, a.Resposta!, a.Links), 1, agora);
            var artigo = Artigo.Criar(categoria.Id, null, null, versao, new Validade(Validade.HojeNoFuso(fuso, agora), mesesDeValidade));
            artigo.Ativar();
            db.Artigos.Add(artigo);
            db.TrechosFonte.Add(TrechoFonte.DoArtigo(versao, agora));
        }
        if (auditoria is not null) db.AuditoriaDaPlataforma.Add(auditoria);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return (novosSetores.Count, novasCategorias.Count);
    }
}
