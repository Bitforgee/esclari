using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Esclari.Api.Dados;
using Esclari.Api.Dados.Avisos;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public record LinhaDeAviso(string Tipo, long ItemId, DateOnly ValidoAte, string Marco);

public static class AvisosTeste
{
    public const string AssuntoDeAviso = "Base de conhecimento:";

    public static DateOnly Hoje(this EsclariFactory f, Instituicao i) => Validade.HojeNoFuso(i.Fuso, f.Relogio.GetUtcNow().UtcDateTime);

    public static Task<List<LinhaDeAviso>> Avisos(this EsclariFactory f, Instituicao i) =>
        f.Db(i.Id, db => db.AvisosValidade.OrderBy(a => a.Id).Select(a => new LinhaDeAviso(a.Tipo, a.ItemId, a.ValidoAte, a.Marco)).ToListAsync());

    public static Task<List<EmailSaida>> EmailsDeAviso(this EsclariFactory f, Instituicao i) =>
        f.Db(i.Id, db => db.EmailsSaida.Where(e => e.Assunto.Contains(AssuntoDeAviso)).OrderBy(e => e.Id).ToListAsync());

    public static async Task<long> ArtigoQueVence(this EsclariFactory f, Instituicao i, string titulo, DateOnly validoAte, string estado = EstadosArtigo.Ativo)
    {
        var a = await f.Artigo(i, titulo, "Resposta de " + titulo, estado: estado);
        await f.ValidadeDoArtigo(i, a.Id, validoAte);
        return a.Id;
    }

    public static async Task<long> DocumentoQueVence(this EsclariFactory f, Instituicao i, string titulo, DateOnly validoAte, bool arquivado = false)
    {
        var id = await f.DocumentoSemeado(i, titulo, "Texto de " + titulo, arquivado);
        await f.ValidadeDoDocumento(i, id, validoAte);
        return id;
    }
}

public class AvisarValidadeTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    Task Rodar() => f.Services.GetRequiredService<Agendador>().Executar();

    async Task<Instituicao> ComAdmin()
    {
        var (i, _) = await Admin.Criar(f);
        return i;
    }

    [Fact]
    public async Task Marco_de_30_dias()
    {
        var i = await ComAdmin();
        var hoje = f.Hoje(i);
        var em30 = await f.ArtigoQueVence(i, "Trancamento", hoje.AddDays(30));
        await f.ArtigoQueVence(i, "Rematrícula", hoje.AddDays(31));

        await Rodar();

        Assert.Equal([new LinhaDeAviso("ARTIGO", em30, hoje.AddDays(30), "EM_30_DIAS")], await f.Avisos(i));
        Assert.Single(await f.EmailsDeAviso(i));
    }

    [Fact]
    public async Task Marco_de_7_dias()
    {
        var i = await ComAdmin();
        var hoje = f.Hoje(i);
        var em7 = await f.ArtigoQueVence(i, "Trancamento", hoje.AddDays(7));
        var em8 = await f.ArtigoQueVence(i, "Rematrícula", hoje.AddDays(8));
        await f.Db(i.Id, async db =>
        {
            db.AvisosValidade.Add(AvisoValidade.Criar("ARTIGO", em8, hoje.AddDays(8), "EM_30_DIAS", f.Relogio.GetUtcNow().UtcDateTime));
            await db.SaveChangesAsync();
        });

        await Rodar();

        Assert.Equal([new LinhaDeAviso("ARTIGO", em8, hoje.AddDays(8), "EM_30_DIAS"), new LinhaDeAviso("ARTIGO", em7, hoje.AddDays(7), "EM_7_DIAS")], await f.Avisos(i));
        var email = Assert.Single(await f.EmailsDeAviso(i));
        Assert.DoesNotContain("Rematrícula", email.Corpo);
    }

    [Fact]
    public async Task Marco_de_vencido()
    {
        var i = await ComAdmin();
        var hoje = f.Hoje(i);
        var ontem = await f.ArtigoQueVence(i, "Trancamento", hoje.AddDays(-1));
        var doDia = await f.ArtigoQueVence(i, "Rematrícula", hoje);

        await Rodar();

        Assert.Equal([new LinhaDeAviso("ARTIGO", doDia, hoje, "EM_7_DIAS"), new LinhaDeAviso("ARTIGO", ontem, hoje.AddDays(-1), "VENCIDO")], (await f.Avisos(i)).OrderBy(l => l.Marco).ToList());
    }

    [Fact]
    public async Task Cruzar_varios_marcos_grava_so_o_mais_grave()
    {
        var i = await ComAdmin();
        var hoje = f.Hoje(i);
        var id = await f.ArtigoQueVence(i, "Trancamento", hoje.AddDays(5));
        var vencido = await f.DocumentoQueVence(i, "Calendário", hoje.AddDays(-40));

        await Rodar();

        Assert.Equal([new LinhaDeAviso("ARTIGO", id, hoje.AddDays(5), "EM_7_DIAS"), new LinhaDeAviso("DOCUMENTO", vencido, hoje.AddDays(-40), "VENCIDO")], (await f.Avisos(i)).OrderBy(l => l.Tipo).ToList());
        var corpo = Assert.Single(await f.EmailsDeAviso(i)).Corpo;
        Assert.Equal(1, corpo.Split("Trancamento").Length - 1);
        Assert.Equal(1, corpo.Split("Calendário").Length - 1);
    }

    [Fact]
    public async Task Um_email_por_administrador_com_os_itens_por_marco()
    {
        var (i, _) = await Admin.Criar(f);
        var segundo = await f.Usuario(i, [Papeis.Administrador]);
        var primeiro = await f.Db(i.Id, db => db.Usuarios.Where(u => u.Id != segundo.Id && u.Papeis.Any(p => p.Papel == Papeis.Administrador)).Select(u => u.Email).SingleAsync());
        var inativo = await f.Usuario(i, [Papeis.Administrador]);
        await f.Db(i.Id, async db => { (await db.Usuarios.SingleAsync(u => u.Id == inativo.Id)).Desativar(); await db.SaveChangesAsync(); });
        var setor = await f.Db(i.Id, db => db.Setores.SingleAsync());
        await f.Atendente(i, setor);
        var hoje = f.Hoje(i);
        await f.ArtigoQueVence(i, "Abono de faltas", hoje.AddDays(-2));
        await f.DocumentoQueVence(i, "Regulamento de estágio", hoje.AddDays(6));
        await f.ArtigoQueVence(i, "Trancamento de matrícula", hoje.AddDays(25));

        await Rodar();

        var emails = await f.EmailsDeAviso(i);
        Assert.Equal(new[] { primeiro, segundo.Email }.Order(), emails.Select(e => e.Destinatario).Order());
        Assert.All(emails, e => Assert.Equal("[Univag] Base de conhecimento: 3 itens para renovar", e.Assunto));
        Assert.Single(emails.Select(e => e.Corpo).Distinct());
        var linhas = emails[0].Corpo.Split('\n');
        string Data(int dias) => hoje.AddDays(dias).ToString("dd/MM/yyyy");
        var vencidos = Array.FindIndex(linhas, l => l.StartsWith("Vencidos"));
        var em7 = Array.FindIndex(linhas, l => l.StartsWith("Vencem em até 7 dias"));
        var em30 = Array.FindIndex(linhas, l => l.StartsWith("Vencem em até 30 dias"));
        Assert.True(vencidos >= 0 && vencidos < em7 && em7 < em30, emails[0].Corpo);
        Assert.Equal($"- Artigo: Abono de faltas (venceu em {Data(-2)})", linhas[vencidos + 1]);
        Assert.Equal($"Abrir: http://localhost:8080/{i.Slug}/artigos?validade=vencidos", linhas[vencidos + 2]);
        Assert.Equal($"- Documento: Regulamento de estágio (vence em {Data(6)})", linhas[em7 + 1]);
        Assert.Equal($"Abrir: http://localhost:8080/{i.Slug}/documentos?validade=em-30-dias", linhas[em7 + 2]);
        Assert.Equal($"- Artigo: Trancamento de matrícula (vence em {Data(25)})", linhas[em30 + 1]);
        Assert.Equal($"Abrir: http://localhost:8080/{i.Slug}/artigos?validade=em-30-dias", linhas[em30 + 2]);
        Assert.Equal(["ARTIGO", "ARTIGO", "DOCUMENTO"], (await f.Avisos(i)).Select(a => a.Tipo).Order());
    }

    [Fact]
    public async Task Segunda_execucao_nao_repete()
    {
        var i = await ComAdmin();
        var hoje = f.Hoje(i);
        await f.ArtigoQueVence(i, "Trancamento", hoje.AddDays(-1));
        await f.DocumentoQueVence(i, "Calendário", hoje.AddDays(20));
        await Rodar();
        var (avisos, emails) = ((await f.Avisos(i)).Count, (await f.EmailsDeAviso(i)).Count);
        Assert.Equal((2, 1), (avisos, emails));

        await Rodar();

        Assert.Equal((2, 1), ((await f.Avisos(i)).Count, (await f.EmailsDeAviso(i)).Count));
    }

    [Fact]
    public async Task Renovado_avisa_de_novo_no_ano_seguinte()
    {
        var (i, admin) = await Admin.Criar(f);
        var hoje = f.Hoje(i);
        var id = await f.ArtigoQueVence(i, "Trancamento", hoje.AddDays(30));
        await Rodar();
        Assert.Equal([new LinhaDeAviso("ARTIGO", id, hoje.AddDays(30), "EM_30_DIAS")], await f.Avisos(i));

        var renovado = await admin.PostAsync($"/api/admin/artigos/{id}/renovar", null);
        Assert.Equal(HttpStatusCode.OK, renovado.StatusCode);
        var novo = DateOnly.Parse((await renovado.Json()).GetProperty("validoAte").GetString()!);
        Assert.Equal(hoje.AddMonths(12), novo);
        await Rodar();
        Assert.Single(await f.Avisos(i));

        f.Relogio.Advance(TimeSpan.FromDays(novo.DayNumber - 30 - hoje.DayNumber));
        Assert.Equal(novo.AddDays(-30), f.Hoje(i));
        await Rodar();

        Assert.Equal([new LinhaDeAviso("ARTIGO", id, hoje.AddDays(30), "EM_30_DIAS"), new LinhaDeAviso("ARTIGO", id, novo, "EM_30_DIAS")], await f.Avisos(i));
        Assert.Equal(2, (await f.EmailsDeAviso(i)).Count);
    }

    [Fact]
    public async Task Sem_administrador_nao_grava_aviso()
    {
        var i = await f.Instituicao(nome: "Univag");
        var setor = await f.Db(i.Id, db => db.Setores.SingleAsync());
        await f.Atendente(i, setor);
        var semEmail = await f.Usuario(i, [Papeis.Administrador], email: "");
        Assert.Equal("", semEmail.Email);
        var hoje = f.Hoje(i);
        await f.ArtigoQueVence(i, "Trancamento", hoje.AddDays(-1));

        await Rodar();

        Assert.Empty(await f.Avisos(i));
        Assert.Empty(await f.EmailsDeAviso(i));
        Assert.Contains(f.Logs.Entradas, l => l.Nivel == LogLevel.Warning && Equals(l.Campos.GetValueOrDefault("{OriginalFormat}"), "Aviso de validade adiado: a instituição {InstituicaoId} não tem administrador ativo com e-mail") && Equals(l.Campos["InstituicaoId"], i.Id));

        await f.Usuario(i, [Papeis.Administrador]);
        await Rodar();

        Assert.Single(await f.Avisos(i));
        Assert.Single(await f.EmailsDeAviso(i));
    }

    [Fact]
    public async Task Falha_no_email_desfaz_os_avisos()
    {
        var i = await ComAdmin();
        var hoje = f.Hoje(i);
        await f.ArtigoQueVence(i, "Trancamento", hoje.AddDays(-1));
        await f.DocumentoQueVence(i, "Calendário", hoje.AddDays(3));

        f.Gravacoes.FalharAoGravarEmail = true;
        try { await Rodar(); }
        finally { f.Gravacoes.FalharAoGravarEmail = false; }

        Assert.Empty(await f.Avisos(i));
        Assert.Empty(await f.EmailsDeAviso(i));

        await Rodar();

        Assert.Equal(2, (await f.Avisos(i)).Count);
        Assert.Single(await f.EmailsDeAviso(i));
    }

    [Fact]
    public async Task Arquivado_e_rascunho_nao_geram_aviso()
    {
        var i = await ComAdmin();
        var hoje = f.Hoje(i);
        await f.ArtigoQueVence(i, "Arquivado", hoje.AddDays(-1), EstadosArtigo.Arquivado);
        await f.ArtigoQueVence(i, "Rascunho", hoje.AddDays(-1), EstadosArtigo.Rascunho);
        await f.DocumentoQueVence(i, "Documento arquivado", hoje.AddDays(-1), arquivado: true);

        await Rodar();

        Assert.Empty(await f.Avisos(i));
        Assert.Empty(await f.EmailsDeAviso(i));
    }

    [Fact]
    public async Task Indice_unico_do_aviso()
    {
        var i = await f.Instituicao();
        var dia = new DateOnly(2027, 1, 10);
        var agora = f.Relogio.GetUtcNow().UtcDateTime;
        await f.Db(i.Id, async db =>
        {
            db.AvisosValidade.Add(AvisoValidade.Criar("ARTIGO", 42, dia, "EM_30_DIAS", agora));
            await db.SaveChangesAsync();
        });

        var repetido = await Assert.ThrowsAsync<DbUpdateException>(() => f.Db(i.Id, async db =>
        {
            db.AvisosValidade.Add(AvisoValidade.Criar("ARTIGO", 42, dia, "EM_30_DIAS", agora));
            await db.SaveChangesAsync();
        }));
        Assert.Equal(Npgsql.PostgresErrorCodes.UniqueViolation, ((Npgsql.PostgresException)repetido.InnerException!).SqlState);

        await f.Db(i.Id, async db =>
        {
            db.AvisosValidade.Add(AvisoValidade.Criar("ARTIGO", 42, dia.AddMonths(12), "EM_30_DIAS", agora));
            db.AvisosValidade.Add(AvisoValidade.Criar("DOCUMENTO", 42, dia, "EM_30_DIAS", agora));
            db.AvisosValidade.Add(AvisoValidade.Criar("ARTIGO", 42, dia, "EM_7_DIAS", agora));
            await db.SaveChangesAsync();
        });
        Assert.Equal(4, (await f.Avisos(i)).Count);
    }

    [Fact]
    public async Task Aviso_nao_cruza_instituicoes()
    {
        var a = await ComAdmin();
        var b = await ComAdmin();
        await f.ArtigoQueVence(a, "Trancamento", f.Hoje(a).AddDays(-1));

        await Rodar();

        Assert.Single(await f.Avisos(a));
        Assert.Single(await f.EmailsDeAviso(a));
        Assert.Empty(await f.Avisos(b));
        Assert.Empty(await f.EmailsDeAviso(b));
        var deA = await f.Db(a.Id, db => db.AvisosValidade.Select(x => x.InstituicaoId).SingleAsync());
        Assert.Equal(a.Id, deA);
        var emailsDeB = await f.Db(b.Id, db => db.Usuarios.Select(u => u.Email).ToListAsync());
        Assert.Empty(await f.Db(a.Id, db => db.EmailsSaida.Where(e => emailsDeB.Contains(e.Destinatario)).ToListAsync()));
    }
}

public class AvisarValidadeConcorrenciaTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    [Fact]
    public async Task Duas_execucoes_avisam_uma_vez()
    {
        var (i, _) = await Admin.Criar(f);
        var segundo = await f.Usuario(i, [Papeis.Administrador]);
        Assert.NotNull(segundo);
        var id = await f.ArtigoQueVence(i, "Trancamento", f.Hoje(i).AddDays(-1));
        var agendador = f.Services.GetRequiredService<Agendador>();
        var lidos = (await agendador.Ler()).Where(x => x.Tipo == "validade" && x.InstituicaoId == i.Id).ToList();
        Assert.Single(lidos);

        f.Gravacoes.Atraso = TimeSpan.FromMilliseconds(300);
        try { await Task.WhenAll(agendador.Processar(lidos), agendador.Processar(lidos)); }
        finally { f.Gravacoes.Atraso = TimeSpan.Zero; }

        Assert.Equal([new LinhaDeAviso("ARTIGO", id, f.Hoje(i).AddDays(-1), "VENCIDO")], await f.Avisos(i));
        var emails = await f.EmailsDeAviso(i);
        Assert.Equal(2, emails.Count);
        Assert.Equal(2, emails.Select(e => e.Destinatario).Distinct().Count());
    }
}
