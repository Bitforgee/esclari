using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Tests;

public class IndicadoresDetalheTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    static readonly DateTime Base = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
    const string Periodo = "de=2026-11-10&ate=2026-11-10";

    record Mundo(Instituicao I, HttpClient Admin, Setor Triagem, Setor Financeiro, Categoria Categoria, Usuario Atendente, Usuario Aluno);

    async Task<Mundo> Criar()
    {
        var (i, admin) = await Admin.Criar(f);
        var triagem = await f.Db(i.Id, db => db.Setores.SingleAsync());
        return new Mundo(i, admin, triagem, await f.Setor(i, "Financeiro"), await f.Categoria(i, "Financeiro"), await f.Usuario(i, [Papeis.Atendente]), await f.Usuario(i, [Papeis.Aluno]));
    }

    Task<Conversa> Conversa(Mundo m, DateTime em, string? desfecho) =>
        f.Db(m.I.Id, async db =>
        {
            var c = Esclari.Api.Dados.Conversas.Conversa.Iniciar(m.Aluno.Id, em);
            switch (desfecho)
            {
                case Desfechos.Resolvida: c.Avaliar(true, em); break;
                case Desfechos.Encerrada: c.Encerrar(em); break;
                case Desfechos.Transferida: c.Transferir(em); break;
                case Desfechos.Abandonada: c.Abandonar(em); break;
            }
            c.Registrar(MensagemConversa.DoAluno("Pergunta", [], em));
            db.Conversas.Add(c);
            await db.SaveChangesAsync();
            return c;
        });

    async Task Chamado(Mundo m, DateTime criado, string estado, (double h, string de, string para)[] transicoes, string? motivo = null, Setor? setor = null)
    {
        var conversa = await Conversa(m, criado, Desfechos.Transferida);
        await f.Db(m.I.Id, async db =>
        {
            var numero = await db.Database.SqlQuery<long>($"UPDATE instituicao SET ultimo_numero_chamado = ultimo_numero_chamado + 1 WHERE id = {m.I.Id} RETURNING ultimo_numero_chamado AS \"Value\"").ToListAsync();
            var c = Esclari.Api.Dados.Chamados.Chamado.Abrir(new($"ESC-{numero.Single():D6}", "Pergunta", null,
                new(conversa.Id, m.Aluno.Id, await db.Matriculas.Where(x => x.UsuarioId == m.Aluno.Id).Select(x => x.Id).FirstAsync(), 3), new(m.Categoria.Id, "IA", (setor ?? m.Financeiro).Id)), criado);
            var e = db.Entry(c);
            (e.Property(x => x.Estado).CurrentValue, e.Property(x => x.MotivoFechamento).CurrentValue) = (estado, motivo);
            db.Chamados.Add(c);
            await db.SaveChangesAsync();
            db.EventosChamado.AddRange(transicoes.Select(t => new EventoChamado { ChamadoId = c.Id, Tipo = "TRANSICAO", De = t.de, Para = t.para, CriadoEm = criado.AddHours(t.h) }));
            await db.SaveChangesAsync();
        });
    }

    static async Task<JsonElement> Pedir(HttpClient c, string rota, string consulta = Periodo)
    {
        var r = await c.GetAsync($"/api/indicadores{rota}?{consulta}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return await r.Json();
    }

    [Fact]
    public async Task Desfechos_somam_o_total_de_conversas()
    {
        var m = await Criar();
        foreach (var d in new[] { Desfechos.Resolvida, Desfechos.Resolvida, Desfechos.Transferida, Desfechos.Abandonada, Desfechos.Encerrada, null })
            await Conversa(m, Base, d);
        await Conversa(m, Base.AddDays(-1), Desfechos.Resolvida);

        var j = (await Pedir(m.Admin, "/detalhe")).GetProperty("desfechos");

        var chaves = new[] { "resolvidas", "transferidas", "abandonadas", "encerradas", "semDesfecho" };
        Assert.Equal([2, 1, 1, 1, 1], chaves.Select(k => j.GetProperty(k).GetInt32()));
        Assert.Equal(chaves.Sum(k => j.GetProperty(k).GetInt32()), (await Pedir(m.Admin, "")).GetProperty("conversas").GetInt32());
    }

    [Fact]
    public async Task Com_setor_desfechos_e_conversas_sao_nulos()
    {
        var m = await Criar();
        await Conversa(m, Base, Desfechos.Resolvida);

        var j = await Pedir(m.Admin, "/detalhe", $"{Periodo}&setorId={m.Financeiro.Id}");

        Assert.Equal(JsonValueKind.Null, j.GetProperty("desfechos").ValueKind);
        Assert.All(j.GetProperty("pontos").EnumerateArray(), p => Assert.Equal(JsonValueKind.Null, p.GetProperty("conversas").ValueKind));
    }

    [Fact]
    public async Task Serie_conta_cada_coisa_pela_sua_data()
    {
        var m = await Criar();
        await Conversa(m, Base.AddDays(1), null);
        await Chamado(m, Base, Fechado, [(2, Aberto, EmAtendimento), (5, EmAtendimento, Resolvido), (30, Resolvido, Fechado)], motivo: "CONFIRMADO");
        await Chamado(m, Base.AddHours(1), Aberto, [], motivo: null);
        await Chamado(m, Base.AddHours(1), Fechado, [(2, Aberto, EmAtendimento), (3, EmAtendimento, Resolvido)], motivo: "DUPLICADO");

        var j = await Pedir(m.Admin, "/detalhe", "de=2026-11-10&ate=2026-11-11");
        var pontos = j.GetProperty("pontos").EnumerateArray().ToList();
        int Valor(int i, string k) => pontos[i].GetProperty(k).GetInt32();

        Assert.Equal("dia", j.GetProperty("granularidade").GetString());
        Assert.Equal(["2026-11-10", "2026-11-11"], pontos.Select(p => p.GetProperty("inicio").GetString()));
        Assert.Equal([3, 1], [Valor(0, "conversas"), Valor(1, "conversas")]);
        Assert.Equal([2, 0], [Valor(0, "chamadosAbertos"), Valor(1, "chamadosAbertos")]);
        var resolvidos = Valor(0, "chamadosResolvidos") + Valor(1, "chamadosResolvidos");
        Assert.Equal(1, resolvidos);
        Assert.Equal((await Pedir(m.Admin, "", "de=2026-11-10&ate=2026-11-11")).GetProperty("chamadosResolvidos").GetInt32(), resolvidos);
    }

    [Fact]
    public async Task Estados_somam_os_chamados_abertos()
    {
        var m = await Criar();
        var c = await Conversa(m, Base, Desfechos.Transferida);
        await Chamado(m, Base, Aberto, []);
        await Chamado(m, Base, EmAtendimento, []);
        await Chamado(m, Base, EmAtendimento, []);
        await Chamado(m, Base, AguardandoAluno, []);
        await Chamado(m, Base, Resolvido, []);
        await Chamado(m, Base, Fechado, [], motivo: "CONFIRMADO");

        var j = await Pedir(m.Admin, "/detalhe");
        var estados = j.GetProperty("chamadosPorEstado");

        Assert.Equal([1, 2, 1, 1], new[] { Aberto, EmAtendimento, AguardandoAluno, Resolvido }.Select(e => estados.GetProperty(e).GetInt32()));
        Assert.Equal(5, (await Pedir(m.Admin, "")).GetProperty("chamadosAbertos").GetInt32());
        Assert.False(estados.TryGetProperty(Fechado, out _));
    }

    [Fact]
    public async Task Quem_pode_e_o_formato_da_resposta()
    {
        var m = await Criar();
        var curso = await f.Db(m.I.Id, async db => { var c = Curso.Criar("ADS"); db.Cursos.Add(c); await db.SaveChangesAsync(); return c; });
        var lideranca = await f.Lideranca(m.I, curso, 3, "Líderes ADS 3º");
        var outro = await f.Setor(m.I, "Secretaria");
        var rota = $"/api/indicadores/detalhe?{Periodo}";

        Assert.Equal(HttpStatusCode.Forbidden, (await (await f.AlunoLogado(m.I)).Cliente.GetAsync(rota)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await f.Atendente(m.I, lideranca)).Cliente.GetAsync(rota)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await f.Atendente(m.I, m.Financeiro)).Cliente.GetAsync($"{rota}&setorId={outro.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await m.Admin.GetAsync("/api/indicadores/detalhe")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await m.Admin.GetAsync("/api/indicadores/detalhe?de=2026-11-10&ate=2026-11-09")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Cliente().GetAsync(rota)).StatusCode);

        var j = await Pedir(m.Admin, "/detalhe");
        Assert.Equal(["granularidade", "pontos", "desfechos", "chamadosPorEstado", "maisAntigoDias", "tempos", "prazos"], j.EnumerateObject().Select(p => p.Name));
        Assert.Equal(["inicio", "conversas", "chamadosAbertos", "chamadosResolvidos"], j.GetProperty("pontos")[0].EnumerateObject().Select(p => p.Name));
        Assert.Equal(["resolvidas", "transferidas", "abandonadas", "encerradas", "semDesfecho"], j.GetProperty("desfechos").EnumerateObject().Select(p => p.Name));
        Assert.Equal(["primeiraRespostaMedianaHoras", "primeiraRespostaP90Horas", "resolucaoMedianaHoras", "resolucaoP90Horas"], j.GetProperty("tempos").EnumerateObject().Select(p => p.Name));
    }

    DateTime Agora => f.Relogio.GetUtcNow().UtcDateTime;

    [Fact]
    public async Task Prazos_traz_metas_e_atraso_agora()
    {
        var m = await Criar();
        await Chamado(m, Agora.AddHours(-144), Aberto, []);
        await Chamado(m, Agora.AddHours(-30), Aberto, []);
        await Chamado(m, Agora.AddHours(-26), EmAtendimento, [(1, Aberto, EmAtendimento)]);
        await Chamado(m, Agora.AddHours(-2), Aberto, []);
        await Chamado(m, Agora.AddHours(-500), AguardandoAluno, [(1, Aberto, AguardandoAluno)]);

        var p = (await Pedir(m.Admin, "/detalhe")).GetProperty("prazos");

        Assert.Equal(["primeiraRespostaMetaHoras", "resolucaoMetaHoras", "primeiraRespostaNoPrazo", "resolucaoNoPrazo", "emAtrasoAgora", "maisAntigoEmAtrasoDias"], p.EnumerateObject().Select(x => x.Name));
        Assert.Equal((24, 120), (p.GetProperty("primeiraRespostaMetaHoras").GetInt32(), p.GetProperty("resolucaoMetaHoras").GetInt32()));
        Assert.Equal(3, p.GetProperty("emAtrasoAgora").GetInt32());
        Assert.Equal(5, p.GetProperty("maisAntigoEmAtrasoDias").GetInt32());
        Assert.Equal(JsonValueKind.Null, p.GetProperty("primeiraRespostaNoPrazo").ValueKind);
        Assert.Equal(JsonValueKind.Null, p.GetProperty("resolucaoNoPrazo").ValueKind);
        Assert.False((await Pedir(m.Admin, "")).TryGetProperty("prazos", out _));
    }

    [Fact]
    public async Task Prazos_com_setor()
    {
        var m = await Criar();
        await Chamado(m, Agora.AddHours(-30), Aberto, []);
        await Chamado(m, Agora.AddHours(-48), Aberto, []);
        await Chamado(m, Agora.AddHours(-300), Aberto, [], setor: m.Triagem);

        var todos = (await Pedir(m.Admin, "/detalhe")).GetProperty("prazos");
        var doSetor = (await Pedir(m.Admin, "/detalhe", $"{Periodo}&setorId={m.Financeiro.Id}")).GetProperty("prazos");

        Assert.Equal((3, 11), (todos.GetProperty("emAtrasoAgora").GetInt32(), todos.GetProperty("maisAntigoEmAtrasoDias").GetInt32()));
        Assert.Equal((2, 1), (doSetor.GetProperty("emAtrasoAgora").GetInt32(), doSetor.GetProperty("maisAntigoEmAtrasoDias").GetInt32()));
    }

    [Fact]
    public async Task Status_continuam_os_mesmos()
    {
        var m = await Criar();
        var rota = $"/api/indicadores/detalhe?{Periodo}";

        Assert.Equal(HttpStatusCode.OK, (await m.Admin.GetAsync(rota)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await m.Admin.GetAsync("/api/indicadores/detalhe?de=2026-11-10&ate=2026-11-09")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await m.Admin.GetAsync("/api/indicadores/detalhe")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Cliente().GetAsync(rota)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await f.AlunoLogado(m.I)).Cliente.GetAsync(rota)).StatusCode);
    }
}