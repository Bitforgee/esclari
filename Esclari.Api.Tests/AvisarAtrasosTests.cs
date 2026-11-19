using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Esclari.Api.Chamados.Tarefas;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Tests;

public static class AtrasosTeste
{
    public const string EmAtraso = "em atraso";

    public static Task<List<EmailSaida>> EmailsDeAtraso(this EsclariFactory f, Instituicao i) =>
        f.Db(i.Id, db => db.EmailsSaida.Where(e => e.Assunto.Contains(EmAtraso)).OrderBy(e => e.Id).ToListAsync());

    public static Task<Dictionary<string, DateTime?>> Marcas(this EsclariFactory f, Instituicao i) =>
        f.Db(i.Id, db => db.Chamados.AsNoTracking().ToDictionaryAsync(c => c.Numero, c => c.AtrasoAvisadoEm));

    public static async Task<(Instituicao, Balcao)> ComAdmin(this EsclariFactory f)
    {
        var (i, _) = await Admin.Criar(f);
        return (i, await Balcao.Criar(f, i));
    }

    public static Task<string> EmailDe(this EsclariFactory f, Instituicao i, long usuarioId) =>
        f.Db(i.Id, db => db.Usuarios.Where(u => u.Id == usuarioId).Select(u => u.Email).SingleAsync());

    public static Task<List<string>> EmailsDosAdministradores(this EsclariFactory f, Instituicao i) =>
        f.Db(i.Id, db => db.Usuarios.Where(u => u.Ativo && u.Papeis.Any(p => p.Papel == Papeis.Administrador)).Select(u => u.Email).ToListAsync());
}

public class AvisarAtrasosTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    Agendador Agendador => f.Services.GetRequiredService<Agendador>();
    DateTime Agora => f.Relogio.GetUtcNow().UtcDateTime;

    async Task<string> Atrasado(Balcao b, string estado = Aberto, double horas = 25)
    {
        var numero = await b.Chamado(estado);
        await b.Recuar(numero, TimeSpan.FromHours(horas));
        return numero;
    }

    static async Task Ok(HttpResponseMessage r) => Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri}: {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");

    [Fact]
    public async Task Primeiro_estouro_avisa_o_responsavel()
    {
        var (i, b) = await f.ComAdmin();
        var numero = await Atrasado(b, EmAtendimento);
        var titulo = (await b.Ler(numero)).Titulo;
        var paraA = await f.EmailDe(i, b.A.Usuario.Id);
        var paraB = await f.EmailDe(i, b.B.Usuario.Id);

        await Agendador.Executar();

        var email = Assert.Single(await f.EmailsDeAtraso(i), e => e.Destinatario == paraA);
        Assert.Equal($"[Univag] 1 chamado em atraso no seu atendimento", email.Assunto);
        Assert.Contains(numero, email.Corpo);
        Assert.Contains(titulo, email.Corpo);
        Assert.Contains("sem primeira resposta", email.Corpo);
        Assert.Contains("atrasado há 1 h", email.Corpo);
        Assert.Contains($"http://localhost:8080/{i.Slug}/chamados/{numero}", email.Corpo);
        Assert.Contains($"http://localhost:8080/{i.Slug}/fila?atraso=true", email.Corpo);
        Assert.DoesNotContain(await f.EmailsDeAtraso(i), e => e.Destinatario == paraB);
        Assert.Equal(Agora, (await f.Marcas(i))[numero]);
    }

    [Fact]
    public async Task Sem_responsavel_avisa_os_atendentes_do_setor()
    {
        var (i, b) = await f.ComAdmin();
        var inativo = await f.Atendente(i, b.Triagem);
        await f.Db(i.Id, async db => { (await db.Usuarios.SingleAsync(u => u.Id == inativo.Usuario.Id)).Desativar(); await db.SaveChangesAsync(); });
        var numero = await Atrasado(b);
        Assert.Null((await b.Ler(numero)).ResponsavelId);

        await Agendador.Executar();

        var doSetor = (await f.EmailsDeAtraso(i)).Where(e => e.Assunto.EndsWith("no seu atendimento")).ToList();
        Assert.Equal(new[] { await f.EmailDe(i, b.A.Usuario.Id), await f.EmailDe(i, b.B.Usuario.Id) }.Order(), doSetor.Select(e => e.Destinatario).Order());
        Assert.All(doSetor, e => Assert.Contains(numero, e.Corpo));
        var todos = (await f.EmailsDeAtraso(i)).Select(e => e.Destinatario).ToList();
        Assert.DoesNotContain(await f.EmailDe(i, inativo.Usuario.Id), todos);
        Assert.DoesNotContain(await f.EmailDe(i, b.Alheio.Usuario.Id), todos);
    }

    [Fact]
    public async Task Resumo_ao_administrador()
    {
        var (i, b) = await f.ComAdmin();
        await f.Usuario(i, [Papeis.Administrador]);
        var inativo = await f.Usuario(i, [Papeis.Administrador]);
        await f.Db(i.Id, async db => { (await db.Usuarios.SingleAsync(u => u.Id == inativo.Id)).Desativar(); await db.SaveChangesAsync(); });
        var numeros = new[] { await Atrasado(b), await Atrasado(b, EmAtendimento), await Atrasado(b, Aberto, 130) };
        var admins = await f.EmailsDosAdministradores(i);
        Assert.Equal(2, admins.Count);

        await Agendador.Executar();

        var resumos = (await f.EmailsDeAtraso(i)).Where(e => e.Assunto == "[Univag] 3 chamados em atraso").ToList();
        Assert.Equal(admins.Order(), resumos.Select(e => e.Destinatario).Order());
        Assert.All(resumos, e => Assert.All(numeros, n => Assert.Contains(n, e.Corpo)));
        Assert.All(resumos, e => Assert.Contains($"http://localhost:8080/{i.Slug}/chamados?atraso=true", e.Corpo));
        Assert.DoesNotContain(await f.EmailsDeAtraso(i), e => e.Destinatario == inativo.Email);
        var marcas = await f.Marcas(i);
        Assert.All(numeros, n => Assert.Equal(Agora, marcas[n]));
    }

    [Fact]
    public async Task Resumo_vai_a_quem_tem_chamados_ver_todos()
    {
        var i = await f.Instituicao(nome: "Univag");
        var b = await Balcao.Criar(f, i);
        var comFuncao = await f.ComFuncoes(i, ["chamados.ver_todos"]);
        var semFuncao = await f.ComFuncoes(i, ["indicadores.ver", "fila.atender"], null, b.Triagem);
        var inativo = await f.Usuario(i, [Papeis.Administrador]);
        await f.Db(i.Id, async db => { (await db.Usuarios.SingleAsync(u => u.Id == inativo.Id)).Desativar(); await db.SaveChangesAsync(); });
        await Atrasado(b);

        await Agendador.Executar();

        var resumos = (await f.EmailsDeAtraso(i)).Where(e => e.Assunto == "[Univag] 1 chamado em atraso").ToList();
        Assert.Equal([await f.EmailDe(i, comFuncao.Usuario.Id)], resumos.Select(e => e.Destinatario));
        var todos = (await f.EmailsDeAtraso(i)).Where(e => e.Assunto == "[Univag] 1 chamado em atraso").Select(e => e.Destinatario).ToList();
        Assert.DoesNotContain(inativo.Email, todos);
        Assert.DoesNotContain(await f.EmailDe(i, b.A.Usuario.Id), todos);
        Assert.DoesNotContain(await f.EmailDe(i, semFuncao.Usuario.Id), todos);
    }

    [Fact]
    public async Task Atendentes_do_setor_pela_funcao_fila_atender()
    {
        var (i, b) = await f.ComAdmin();
        var comFuncao = await f.ComFuncoes(i, ["fila.atender"], null, b.Triagem);
        var semFuncao = await f.ComFuncoes(i, ["indicadores.ver"], null, b.Triagem);
        var numero = await Atrasado(b);
        Assert.Null((await b.Ler(numero)).ResponsavelId);

        await Agendador.Executar();

        var doSetor = (await f.EmailsDeAtraso(i)).Where(e => e.Assunto.EndsWith("no seu atendimento")).Select(e => e.Destinatario).ToList();
        Assert.Contains(await f.EmailDe(i, comFuncao.Usuario.Id), doSetor);
        Assert.DoesNotContain(await f.EmailDe(i, semFuncao.Usuario.Id), doSetor);
    }
    [Fact]
    public async Task Antes_de_24_horas_nao_repete()
    {
        var (i, b) = await f.ComAdmin();
        await Atrasado(b);
        await Atrasado(b, EmAtendimento, 200);
        await Agendador.Executar();
        var (emails, marcas) = ((await f.EmailsDeAtraso(i)).Count, await f.Marcas(i));
        Assert.True(emails > 0);

        f.Relogio.Advance(TimeSpan.FromHours(24) - TimeSpan.FromMinutes(1));
        await Agendador.Executar();

        Assert.Equal(emails, (await f.EmailsDeAtraso(i)).Count);
        Assert.Equal(marcas, await f.Marcas(i));
    }

    [Fact]
    public async Task Depois_de_24_horas_so_o_administrador()
    {
        var (i, b) = await f.ComAdmin();
        var numero = await Atrasado(b, EmAtendimento);
        await Agendador.Executar();
        var antes = await f.EmailsDeAtraso(i);
        var admin = Assert.Single(await f.EmailsDosAdministradores(i));
        Assert.Equal(2, antes.Count);

        f.Relogio.Advance(TimeSpan.FromHours(24));
        await Agendador.Executar();

        var novos = (await f.EmailsDeAtraso(i)).Skip(antes.Count).ToList();
        var resumo = Assert.Single(novos);
        Assert.Equal((admin, "[Univag] 1 chamado em atraso"), (resumo.Destinatario, resumo.Assunto));
        Assert.Contains(numero, resumo.Corpo);
        Assert.Equal(EmAtendimento, (await b.Ler(numero)).Estado);
        Assert.Equal(Agora, (await f.Marcas(i))[numero]);
    }

    [Fact]
    public async Task Primeira_execucao_agrupa_por_destinatario()
    {
        var (i, b) = await f.ComAdmin();
        var c = await f.Atendente(i, b.Triagem);
        var bases = new List<(string Numero, int Clones)>();
        foreach (var (atendente, clones) in new[] { (b.A, 13), (b.B, 13), (c, 11) })
        {
            var numero = await b.Chamado();
            await Ok(await atendente.Cliente.PostAsync($"/api/chamados/{numero}/assumir", null));
            await b.Recuar(numero, TimeSpan.FromHours(30));
            await f.Clonar(i, numero, clones);
            bases.Add((numero, clones));
        }
        Assert.Equal(40, await f.Db(i.Id, db => db.Chamados.CountAsync()));

        await Agendador.Executar();

        var emails = await f.EmailsDeAtraso(i);
        Assert.Equal(4, emails.Count);
        Assert.Equal(4, emails.Select(e => e.Destinatario).Distinct().Count());
        var resumo = Assert.Single(emails, e => e.Assunto == "[Univag] 40 chamados em atraso");
        Assert.Equal(40, resumo.Corpo.Split("\n- ").Length - 1);
        foreach (var (atendente, quantos) in new[] { (b.A, 14), (b.B, 14), (c, 12) })
        {
            var email = Assert.Single(emails, e => e.Destinatario == atendente.Usuario.Email);
            Assert.Equal($"[Univag] {quantos} chamados em atraso no seu atendimento", email.Assunto);
        }
        Assert.All((await f.Marcas(i)).Values, m => Assert.Equal(Agora, m));
    }

    [Fact]
    public async Task Sem_destinatario_nao_grava_marca()
    {
        var i = await f.Instituicao(nome: "Univag");
        var aluno = await f.AlunoLogado(i);
        var r = await aluno.Cliente.PostAsJsonAsync("/api/atendimento-direto", new { descricao = "Preciso do boleto" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var numero = (await r.Json()).GetProperty("numero").GetString()!;
        await f.Criado(i, numero, Agora.AddHours(-30));
        bool Aviso(Log l) => l.Nivel == LogLevel.Warning && Equals(l.Campos.GetValueOrDefault("InstituicaoId"), i.Id)
            && Equals(l.Campos.GetValueOrDefault("{OriginalFormat}"), "Aviso de atraso adiado: a instituição {InstituicaoId} não tem destinatário ativo com e-mail para {Chamados} chamados");

        await Agendador.Executar();

        Assert.Null((await f.Marcas(i))[numero]);
        Assert.Empty(await f.EmailsDeAtraso(i));
        Assert.Single(f.Logs.Entradas, Aviso);

        var admin = await f.Usuario(i, [Papeis.Administrador]);
        await Agendador.Executar();

        Assert.Equal(Agora, (await f.Marcas(i))[numero]);
        Assert.Equal(admin.Email, Assert.Single(await f.EmailsDeAtraso(i)).Destinatario);
        Assert.Single(f.Logs.Entradas, Aviso);
    }

    [Fact]
    public async Task Falha_no_email_desfaz_a_marca()
    {
        var (i, b) = await f.ComAdmin();
        var numeros = new[] { await Atrasado(b), await Atrasado(b, EmAtendimento) };

        f.Gravacoes.FalharAoGravarEmail = true;
        try { await Agendador.Executar(); }
        finally { f.Gravacoes.FalharAoGravarEmail = false; }

        var marcas = await f.Marcas(i);
        Assert.All(numeros, n => Assert.Null(marcas[n]));
        Assert.Empty(await f.EmailsDeAtraso(i));
    }

    [Fact]
    public async Task Sai_do_atraso_e_para_de_avisar()
    {
        var (i, b) = await f.ComAdmin();
        var respondido = await Atrasado(b);
        var resolvido = await Atrasado(b);
        var cancelado = await Atrasado(b);
        var aguardando = await Atrasado(b);
        var continua = await Atrasado(b);
        await Agendador.Executar();
        var saem = new[] { respondido, resolvido, cancelado, aguardando };
        var marcadas = await f.Marcas(i);
        Assert.All(saem, n => Assert.NotNull(marcadas[n]));

        await Ok(await Balcao.Mensagem(b.A.Cliente, respondido, "Estamos vendo o boleto."));
        await Ok(await b.A.Cliente.PostAsJsonAsync($"/api/chamados/{resolvido}/resolver", new { texto = "Boleto reemitido." }));
        await Ok(await b.ClienteAluno.PostAsJsonAsync($"/api/chamados/{cancelado}/cancelar", new { motivo = "Resolvi sozinho." }));
        await Ok(await Balcao.Mensagem(b.A.Cliente, aguardando, "Envie o comprovante.", aguardaAluno: true));
        var lista = (await (await b.A.Cliente.GetAsync("/api/chamados/busca?escopo=fila&atraso=true")).Json()).GetProperty("itens").EnumerateArray()
            .Select(x => x.GetProperty("numero").GetString()).ToList();
        Assert.Equal([continua], lista);
        var antes = (await f.EmailsDeAtraso(i)).Count;

        f.Relogio.Advance(AvisarAtrasos.Repeticao);
        await Agendador.Executar();

        var novos = (await f.EmailsDeAtraso(i)).Skip(antes).ToList();
        var resumo = Assert.Single(novos);
        Assert.Contains(continua, resumo.Corpo);
        Assert.All(saem, n => Assert.DoesNotContain(n, resumo.Corpo));
    }

    [Fact]
    public async Task Atraso_nunca_fecha_nem_resolve()
    {
        var (i, b) = await f.ComAdmin();
        var aberto = await Atrasado(b, Aberto, 1000);
        var atendimento = await Atrasado(b, EmAtendimento, 1000);
        var eventos = ((await b.Eventos(aberto)).Count, (await b.Eventos(atendimento)).Count);

        await Agendador.Executar();
        f.Relogio.Advance(AvisarAtrasos.Repeticao);
        await Agendador.Executar();

        Assert.Equal((Aberto, EmAtendimento), ((await b.Ler(aberto)).Estado, (await b.Ler(atendimento)).Estado));
        Assert.Equal(eventos, ((await b.Eventos(aberto)).Count, (await b.Eventos(atendimento)).Count));
        var marcas = await f.Marcas(i);
        Assert.All(new[] { aberto, atendimento }, n => Assert.Equal(Agora, marcas[n]));
        Assert.True((await f.EmailsDeAtraso(i)).Count >= 2);
    }

    [Fact]
    public async Task Email_nao_carrega_dados_do_aluno()
    {
        var (i, b) = await f.ComAdmin();
        await Atrasado(b);
        await Atrasado(b, EmAtendimento);
        var matriculas = await f.Db(i.Id, db => db.Matriculas.Where(m => m.UsuarioId == b.Aluno.Usuario.Id).Select(m => m.Numero).ToListAsync());
        Assert.NotEmpty(matriculas);

        await Agendador.Executar();

        var emails = await f.EmailsDeAtraso(i);
        Assert.Contains(emails, e => e.Assunto.EndsWith("no seu atendimento"));
        Assert.Contains(emails, e => e.Assunto == "[Univag] 2 chamados em atraso");
        foreach (var e in emails)
        {
            Assert.DoesNotContain(b.Aluno.Usuario.Nome, e.Corpo);
            Assert.DoesNotContain(b.Aluno.Usuario.Cpf.Valor, e.Corpo);
            Assert.All(matriculas, m => Assert.DoesNotContain(m, e.Corpo));
        }
    }

    [Fact]
    public async Task Aviso_nao_cruza_instituicoes()
    {
        var (ia, ba) = await f.ComAdmin();
        var (ib, bb) = await f.ComAdmin();
        var daA = await ba.Chamado(descricao: "Boleto da instituição A");
        await ba.Recuar(daA, TimeSpan.FromHours(30));
        var tituloA = (await ba.Ler(daA)).Titulo;
        var daB = await bb.Chamado(descricao: "Matrícula da instituição B");
        await bb.Recuar(daB, TimeSpan.FromHours(30));
        var tituloB = (await bb.Ler(daB)).Titulo;

        await Agendador.Executar();

        var adminB = Assert.Single(await f.EmailsDosAdministradores(ib));
        var deB = await f.EmailsDeAtraso(ib);
        Assert.Contains(deB, e => e.Destinatario == adminB);
        Assert.All(deB, e => Assert.DoesNotContain(tituloA, e.Corpo));
        Assert.All(deB, e => Assert.Contains(tituloB, e.Corpo));
        var deA = await f.EmailsDeAtraso(ia);
        Assert.DoesNotContain(deA, e => e.Destinatario == adminB);
        Assert.All(deA, e => Assert.DoesNotContain(tituloB, e.Corpo));
        Assert.Contains(deA, e => e.Corpo.Contains(tituloA));
    }
}

public class AvisarAtrasosConcorrenciaTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    [Fact]
    public async Task Duas_execucoes_avisam_uma_vez()
    {
        var (i, b) = await f.ComAdmin();
        var numero = await b.Chamado(EmAtendimento);
        await b.Recuar(numero, TimeSpan.FromHours(30));
        var agendador = f.Services.GetRequiredService<Agendador>();
        var lidos = (await agendador.Ler()).Where(x => x.Tipo == "atraso" && x.InstituicaoId == i.Id).ToList();
        Assert.Single(lidos);

        f.Gravacoes.Atraso = TimeSpan.FromMilliseconds(300);
        try { await Task.WhenAll(agendador.Processar(lidos), agendador.Processar(lidos)); }
        finally { f.Gravacoes.Atraso = TimeSpan.Zero; }

        var emails = await f.EmailsDeAtraso(i);
        Assert.Equal(2, emails.Count);
        Assert.Equal(2, emails.Select(e => e.Destinatario).Distinct().Count());
        Assert.Equal(f.Relogio.GetUtcNow().UtcDateTime, (await f.Marcas(i))[numero]);
    }
}
