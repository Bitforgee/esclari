using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Tests;

public class ExportarChamadosTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    const string Cabecalho = "Número;Título;Estado;Setor;Categoria;Categoria original;Responsável;Curso;Semestre;Aberto em;Primeira resposta em;Resolvido em;Fechado em;Motivo de fechamento;Reaberturas;Encaminhamentos";
    static readonly TimeZoneInfo Cuiaba = TimeZoneInfo.FindSystemTimeZoneById("America/Cuiaba");

    static async Task<HttpResponseMessage> Exportar(HttpClient c, string consulta = "")
    {
        var r = await c.GetAsync("/api/chamados/exportar?escopo=fila" + consulta);
        return r;
    }

    static async Task<string[]> Linhas(HttpResponseMessage r)
    {
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        var texto = Encoding.UTF8.GetString(await r.Content.ReadAsByteArrayAsync());
        return texto.TrimStart('\uFEFF').Split("\r\n")[..^1];
    }

    static string[] Celulas(string linha) => linha.Split(';');

    static string Data(DateTime? utc) => utc is { } d ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(d, DateTimeKind.Utc), Cuiaba).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : "";

    static async Task Ok(HttpResponseMessage r) => Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri}: {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");

    [Fact]
    public async Task Cabecalhos_do_arquivo()
    {
        var alvo = new DateTimeOffset(2026, 11, 11, 3, 30, 0, TimeSpan.Zero);
        if (f.Relogio.GetUtcNow() < alvo) f.Relogio.SetUtcNow(alvo);
        Assert.Equal(alvo, f.Relogio.GetUtcNow());
        var b = await Balcao.Criar(f);
        await b.Chamado();

        var r = await Exportar(b.A.Cliente);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("text/csv; charset=utf-8", r.Content.Headers.ContentType!.ToString());
        Assert.Equal($"attachment; filename=\"chamados-{b.Instituicao.Slug}-2026-11-10.csv\"", Assert.Single(r.Content.Headers.GetValues("Content-Disposition")));
    }

    [Fact]
    public async Task Formato_do_arquivo()
    {
        var b = await Balcao.Criar(f);
        var base_ = await b.Chamado();
        await f.Clonar(b.Instituicao, base_, 54);
        await f.Criado(b.Instituicao, base_, new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));

        var r = await Exportar(b.A.Cliente, "&ordem=recentes");
        var bytes = await r.Content.ReadAsByteArrayAsync();
        var texto = Encoding.UTF8.GetString(bytes);

        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        Assert.Equal(texto.Split('\n').Length - 1, texto.Split("\r\n").Length - 1);
        Assert.EndsWith("\r\n", texto);
        var linhas = await Linhas(r);
        Assert.Equal(Cabecalho, linhas[0]);
        var esperados = await f.Db(b.Instituicao.Id, db => db.Chamados.OrderByDescending(c => c.CriadoEm).ThenByDescending(c => c.Id).Select(c => c.Numero).ToListAsync());
        Assert.Equal(55, esperados.Count);
        Assert.Equal(esperados, linhas[1..].Select(l => Celulas(l)[0]));
        Assert.Equal(base_, esperados[^1]);
        Assert.All(linhas, l => Assert.Equal(16, Celulas(l).Length));
    }

    [Fact]
    public async Task Valores_de_cada_coluna()
    {
        var b = await Balcao.Criar(f);
        var financeiro = await f.Categoria(b.Instituicao, "Financeiro");
        var fechado = await b.Chamado(descricao: "Boleto em dobro");
        await Ok(await b.A.Cliente.PostAsJsonAsync($"/api/chamados/{fechado}/reclassificar", new { categoriaId = financeiro.Id }));
        await Ok(await b.A.Cliente.PostAsJsonAsync($"/api/chamados/{fechado}/encaminhar", new { setorId = b.Outro.Id, motivo = "é do financeiro" }));
        await Ok(await b.Alheio.Cliente.PostAsync($"/api/chamados/{fechado}/assumir", null));
        await Ok(await b.Alheio.Cliente.PostAsJsonAsync($"/api/chamados/{fechado}/resolver", new { texto = "Estornado." }));
        await Ok(await b.ClienteAluno.PostAsync($"/api/chamados/{fechado}/confirmar", null));
        await f.Criado(b.Instituicao, fechado, new DateTime(2026, 11, 11, 3, 30, 0, DateTimeKind.Utc));
        var aberto = await b.Chamado(descricao: "Ainda aberto");
        await Ok(await b.A.Cliente.PostAsJsonAsync($"/api/chamados/{aberto}/encaminhar", new { setorId = b.Outro.Id, motivo = "é do financeiro" }));
        var c = await b.Ler(fechado);

        var linhas = await Linhas(await Exportar(b.Alheio.Cliente, "&estado=FECHADO&estado=ABERTO"));

        var l = Celulas(linhas.Single(x => x.StartsWith(fechado + ";")));
        Assert.Equal([fechado, "Boleto em dobro", "Fechado", "Financeiro", "Financeiro", "Não classificado", b.Alheio.Usuario.Nome, "Análise e Desenvolvimento de Sistemas", "3",
            "10/11/2026 23:30", l[10], Data(c.ResolvidoEm), Data(c.FechadoEm), "Confirmado pelo aluno", "0", "1"], l);
        Assert.NotEqual("", l[11]);
        Assert.NotEqual("", l[12]);
        var a = Celulas(linhas.Single(x => x.StartsWith(aberto + ";")));
        Assert.Equal(("Aberto", "", "", ""), (a[2], a[11], a[12], a[13]));
    }

    [Fact]
    public async Task Rotulos_de_estado_e_motivo()
    {
        var b = await Balcao.Criar(f);
        var porEstado = new Dictionary<string, string>();
        foreach (var estado in new[] { Aberto, EmAtendimento, AguardandoAluno, Resolvido, Fechado }) porEstado[estado] = await b.Chamado(estado);
        var cancelado = await b.Chamado();
        await Ok(await b.ClienteAluno.PostAsJsonAsync($"/api/chamados/{cancelado}/cancelar", new { motivo = "resolvi sozinho" }));
        var duplicado = await b.Chamado();
        await Ok(await b.A.Cliente.PostAsJsonAsync($"/api/chamados/{duplicado}/duplicado", new { numeroOriginal = porEstado[Aberto] }));
        var automatico = await b.Chamado(Resolvido);
        await b.Recuar(automatico, TimeSpan.FromDays(30));
        await f.Services.GetRequiredService<Agendador>().Executar();
        Assert.Equal(Fechado, (await b.Ler(automatico)).Estado);

        var linhas = await Linhas(await Exportar(b.A.Cliente, "&estado=ABERTO&estado=EM_ATENDIMENTO&estado=AGUARDANDO_ALUNO&estado=RESOLVIDO&estado=FECHADO"));
        string[] Da(string numero) => Celulas(linhas.Single(x => x.StartsWith(numero + ";")));

        Assert.Equal(["Aberto", "Em atendimento", "Aguardando aluno", "Resolvido", "Fechado"], porEstado.Values.Select(n => Da(n)[2]));
        Assert.Equal("Confirmado pelo aluno", Da(porEstado[Fechado])[13]);
        Assert.Equal("Cancelado pelo aluno", Da(cancelado)[13]);
        Assert.Equal("Duplicado", Da(duplicado)[13]);
        Assert.Equal("Fechado automaticamente", Da(automatico)[13]);
    }
    [Fact]
    public async Task Primeira_resposta_da_equipe()
    {
        var b = await Balcao.Criar(f);
        var respondido = await b.Chamado(EmAtendimento);
        await Ok(await Balcao.Mensagem(b.A.Cliente, respondido, "nota antes", "NOTA"));
        f.Relogio.Advance(TimeSpan.FromMinutes(7));
        await Ok(await Balcao.Mensagem(b.ClienteAluno, respondido, "aluno pergunta de novo"));
        f.Relogio.Advance(TimeSpan.FromMinutes(7));
        await Ok(await Balcao.Mensagem(b.A.Cliente, respondido, "Resposta da equipe"));
        f.Relogio.Advance(TimeSpan.FromMinutes(7));
        await Ok(await Balcao.Mensagem(b.A.Cliente, respondido, "Segunda resposta"));
        var semResposta = await b.Chamado(EmAtendimento);
        var primeira = await f.Db(b.Instituicao.Id, db => db.MensagensChamado
            .Where(m => db.Chamados.Any(c => c.Id == m.ChamadoId && c.Numero == respondido) && m.Texto == "Resposta da equipe").Select(m => m.CriadaEm).SingleAsync());

        var linhas = await Linhas(await Exportar(b.A.Cliente));

        Assert.Equal(Data(primeira), Celulas(linhas.Single(x => x.StartsWith(respondido + ";")))[10]);
        Assert.Equal("", Celulas(linhas.Single(x => x.StartsWith(semResposta + ";")))[10]);
    }

    [Fact]
    public async Task Sem_dado_pessoal_do_aluno()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EmAtendimento);
        await Ok(await Balcao.Mensagem(b.A.Cliente, numero, "NOTA-SECRETA-XYZ", "NOTA"));
        await Ok(await Balcao.Mensagem(b.A.Cliente, numero, "RESPOSTA-PUBLICA-XYZ"));
        await Ok(await Balcao.Mensagem(b.ClienteAluno, numero, "MENSAGEM-DO-ALUNO-XYZ"));
        var aluno = await f.Db(b.Instituicao.Id, db => db.Usuarios.Include(u => u.Matriculas).SingleAsync(u => u.Id == b.Aluno.Usuario.Id));

        var csv = string.Join("\r\n", await Linhas(await Exportar(b.A.Cliente)));

        Assert.Contains(numero, csv);
        foreach (var proibido in new[] { aluno.Cpf.Valor, aluno.Matriculas[0].Numero, aluno.Nome, aluno.Email, "NOTA-SECRETA-XYZ", "RESPOSTA-PUBLICA-XYZ", "MENSAGEM-DO-ALUNO-XYZ" })
            Assert.DoesNotContain(proibido, csv);
    }

    [Fact]
    public async Task Titulo_com_formula_sai_como_texto()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(descricao: "=HYPERLINK(1)");

        var linhas = await Linhas(await Exportar(b.A.Cliente));

        Assert.Equal("'=HYPERLINK(1)", Celulas(linhas.Single(x => x.StartsWith(numero + ";")))[1]);
    }

    [Fact]
    public async Task Limite_de_10000()
    {
        var b = await Balcao.Criar(f);
        var base_ = await b.Chamado();
        await f.Clonar(b.Instituicao, base_, 9_999);

        var linhas = await Linhas(await Exportar(b.A.Cliente));
        Assert.Equal(10_000, linhas.Length - 1);

        await f.Clonar(b.Instituicao, base_, 1);
        var r = await Exportar(b.A.Cliente);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("A exportação passa de 10.000 chamados. Refine os filtros.", (await r.Json()).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Lideranca_nao_exporta()
    {
        var t = await Turma.Criar(f);
        await t.NaLideranca();

        var r = await t.Lider!.Cliente.GetAsync("/api/chamados/exportar?escopo=fila");

        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal("A exportação não está disponível para a liderança de turma.", (await r.Json()).GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.OK, (await t.Lider.Cliente.GetAsync("/api/chamados/busca?escopo=fila")).StatusCode);
    }

    [Fact]
    public async Task Log_da_exportacao_sem_o_texto()
    {
        var b = await Balcao.Criar(f);
        await b.Chamado(Fechado, "boleto pago");
        await b.Chamado(Fechado, "boleto vencido");
        await b.Chamado(Aberto, "boleto aberto");

        await Linhas(await Exportar(b.A.Cliente, "&q=boleto&estado=FECHADO"));

        var log = Assert.Single(f.Logs.Entradas, l => Equals(l.Campos.GetValueOrDefault("{OriginalFormat}"), "Exportação de chamados: usuário {UsuarioId}, instituição {InstituicaoId}, {Linhas} linhas, filtros {Filtros}")
            && Equals(l.Campos["UsuarioId"], b.A.Usuario.Id));
        Assert.Equal(LogLevel.Information, log.Nivel);
        Assert.Equal(b.Instituicao.Id, log.Campos["InstituicaoId"]);
        Assert.Equal(2, log.Campos["Linhas"]);
        var filtros = ((string)log.Campos["Filtros"]!).Split(',');
        Assert.Contains("q", filtros);
        Assert.Contains("estado", filtros);
        Assert.DoesNotContain(f.Logs.Entradas, l => l.Categoria.EndsWith("ExportarChamadosHandler") && l.Mensagem.Contains("boleto", StringComparison.OrdinalIgnoreCase));
    }
}
