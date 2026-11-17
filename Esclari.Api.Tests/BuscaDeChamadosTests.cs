using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;
using static Esclari.Api.Dados.Chamados.EstadosChamado;

namespace Esclari.Api.Tests;

public class BuscaDeChamadosTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    static readonly string[] Rotas = ["/api/chamados/busca", "/api/chamados/exportar", "/api/chamados/filtros"];

    static async Task<JsonElement> Buscar(HttpClient c, string consulta = "")
    {
        var r = await c.GetAsync("/api/chamados/busca?escopo=fila" + consulta);
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return await r.Json();
    }

    static List<string> Numeros(JsonElement j) => [.. j.GetProperty("itens").EnumerateArray().Select(i => i.GetProperty("numero").GetString()!)];

    static async Task<List<string>> NumerosDe(HttpClient c, string consulta = "") => Numeros(await Buscar(c, consulta));

    static async Task Ok(HttpResponseMessage r) => Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri}: {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");

    static Task Encaminhar(Balcao b, string numero, Setor setor) =>
        b.A.Cliente.PostAsJsonAsync($"/api/chamados/{numero}/encaminhar", new { setorId = setor.Id, motivo = "é do outro setor" }).ContinueWith(t => Ok(t.Result)).Unwrap();

    [Fact]
    public async Task Sem_filtro_igual_a_fila()
    {
        var b = await Balcao.Criar(f);
        var aberto = await b.Chamado(Aberto);
        var atendimento = await b.Chamado(EmAtendimento);
        var aguardando = await b.Chamado(AguardandoAluno);
        var resolvido = await b.Chamado(Resolvido);
        var fechado = await b.Chamado(Fechado);
        var deOutro = await b.Chamado(Aberto);
        await Encaminhar(b, deOutro, b.Outro);
        await f.Criado(b.Instituicao, aberto, new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));

        var fila = (await (await b.A.Cliente.GetAsync("/api/chamados?escopo=fila")).Json()).EnumerateArray().Select(i => i.GetProperty("numero").GetString()!).ToList();
        var busca = await NumerosDe(b.A.Cliente);

        Assert.Equal(fila, busca);
        Assert.Equal([aberto, atendimento, aguardando, resolvido], busca);
        Assert.DoesNotContain(fechado, busca);
        Assert.DoesNotContain(deOutro, busca);
    }

    [Fact]
    public async Task Estado_filtra_cada_um()
    {
        var b = await Balcao.Criar(f);
        var porEstado = new Dictionary<string, string>();
        foreach (var estado in new[] { Aberto, EmAtendimento, AguardandoAluno, Resolvido, Fechado }) porEstado[estado] = await b.Chamado(estado);

        foreach (var (estado, numero) in porEstado)
            Assert.Equal([numero], await NumerosDe(b.A.Cliente, $"&estado={estado}"));
    }

    [Fact]
    public async Task Estado_repetido_soma()
    {
        var b = await Balcao.Criar(f);
        var aberto = await b.Chamado(Aberto);
        await b.Chamado(EmAtendimento);
        var fechado = await b.Chamado(Fechado);

        Assert.Equal([aberto, fechado], await NumerosDe(b.A.Cliente, "&estado=ABERTO&estado=FECHADO"));
    }

    [Fact]
    public async Task Busca_no_titulo_sem_acento_e_sem_caixa()
    {
        var b = await Balcao.Criar(f);
        var matricula = await b.Chamado(descricao: "Trancar a Matrícula");
        var boleto = await b.Chamado(descricao: "Boleto atrasado");

        Assert.Equal([matricula], await NumerosDe(b.A.Cliente, "&q=matricula"));
        Assert.Equal([matricula], await NumerosDe(b.A.Cliente, "&q=" + Uri.EscapeDataString("MATRÍCULA")));
        Assert.DoesNotContain(boleto, await NumerosDe(b.A.Cliente, "&q=matricula"));
    }

    [Fact]
    public async Task Busca_no_numero()
    {
        var b = await Balcao.Criar(f);
        var primeiro = await b.Chamado(descricao: "Pedido sem digitos");
        var segundo = await b.Chamado(descricao: "Outro pedido sem digitos");

        Assert.Equal([segundo], await NumerosDe(b.A.Cliente, "&q=" + segundo[^3..]));
        Assert.Equal([primeiro], await NumerosDe(b.A.Cliente, "&q=" + primeiro[^3..]));
        Assert.Equal([primeiro, segundo], await NumerosDe(b.A.Cliente, "&q=esc-000"));
    }

    [Fact]
    public async Task Busca_no_nome_do_solicitante()
    {
        var b = await Balcao.Criar(f);
        var dele = await b.Chamado(descricao: "Preciso de ajuda");
        var outroAluno = await f.AlunoLogado(b.Instituicao);
        var r = await outroAluno.Cliente.PostAsJsonAsync("/api/atendimento-direto", new { descricao = "Preciso de ajuda" });
        var doOutro = (await r.Json()).GetProperty("numero").GetString()!;
        await f.Renomear(b.Instituicao, b.Aluno.Usuario.Id, "João Gonçalves Teste");

        var achados = await NumerosDe(b.A.Cliente, "&q=goncalves");

        Assert.Equal([dele], achados);
        Assert.DoesNotContain(doOutro, achados);
    }

    [Fact]
    public async Task Curinga_e_texto_comum()
    {
        var b = await Balcao.Criar(f);
        await b.Chamado(descricao: "Boleto 50 reais");
        await b.Chamado(descricao: "Segunda via do boleto");

        Assert.Equal(0, (await Buscar(b.A.Cliente, "&q=" + Uri.EscapeDataString("%"))).GetProperty("total").GetInt32());
        Assert.Equal(0, (await Buscar(b.A.Cliente, "&q=_")).GetProperty("total").GetInt32());

        var desconto = await b.Chamado(descricao: "Desconto de 50% na mensalidade");
        Assert.Equal([desconto], await NumerosDe(b.A.Cliente, "&q=" + Uri.EscapeDataString("50%")));
    }

    [Fact]
    public async Task Barra_invertida_e_texto_comum()
    {
        var b = await Balcao.Criar(f);
        await b.Chamado(descricao: "Boleto atrasado");

        Assert.Equal(0, (await Buscar(b.A.Cliente, "&q=" + Uri.EscapeDataString("\\"))).GetProperty("total").GetInt32());

        var pasta = await b.Chamado(descricao: @"Arquivo em C:\temp\notas");
        Assert.Equal([pasta], await NumerosDe(b.A.Cliente, "&q=" + Uri.EscapeDataString(@"C:\temp")));
        Assert.Equal([pasta], await NumerosDe(b.A.Cliente, "&q=" + Uri.EscapeDataString("\\")));
    }
    [Fact]
    public async Task Filtra_por_categoria_atual()
    {
        var b = await Balcao.Criar(f);
        var financeiro = await f.Categoria(b.Instituicao, "Financeiro");
        var padrao = await f.Db(b.Instituicao.Id, db => db.Categorias.SingleAsync(c => c.Padrao));
        var reclassificado = await b.Chamado();
        var original = await b.Chamado();
        await Ok(await b.A.Cliente.PostAsJsonAsync($"/api/chamados/{reclassificado}/reclassificar", new { categoriaId = financeiro.Id }));

        Assert.Equal([reclassificado], await NumerosDe(b.A.Cliente, $"&categoriaId={financeiro.Id}"));
        Assert.Equal([original], await NumerosDe(b.A.Cliente, $"&categoriaId={padrao.Id}"));
    }

    [Fact]
    public async Task Filtra_por_setor()
    {
        var b = await Balcao.Criar(f);
        var dosDois = await f.Atendente(b.Instituicao, b.Triagem, b.Outro);
        var naTriagem = await b.Chamado();
        var noOutro = await b.Chamado();
        await Encaminhar(b, noOutro, b.Outro);

        Assert.Equal([noOutro], await NumerosDe(dosDois.Cliente, $"&setorId={b.Outro.Id}"));
        Assert.Equal([naTriagem], await NumerosDe(dosDois.Cliente, $"&setorId={b.Triagem.Id}"));
    }

    [Fact]
    public async Task Setor_alheio_403()
    {
        var b = await Balcao.Criar(f);
        await b.Chamado();

        foreach (var rota in Rotas[..2])
        {
            var r = await b.A.Cliente.GetAsync($"{rota}?escopo=fila&setorId={b.Outro.Id}");
            Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
            Assert.Equal("Este setor não é seu.", (await r.Json()).GetProperty("detail").GetString());
        }
    }

    [Fact]
    public async Task Responsavel_eu_e_ninguem()
    {
        var b = await Balcao.Criar(f);
        var meu = await b.Chamado(EmAtendimento);
        var doB = await b.Chamado();
        await Ok(await b.B.Cliente.PostAsync($"/api/chamados/{doB}/assumir", null));
        var semNinguem = await b.Chamado();

        Assert.Equal([meu], await NumerosDe(b.A.Cliente, "&responsavel=eu"));
        Assert.Equal([semNinguem], await NumerosDe(b.A.Cliente, "&responsavel=ninguem"));
    }

    [Fact]
    public async Task Periodo_no_fuso_da_instituicao()
    {
        var b = await Balcao.Criar(f);
        var noite = await b.Chamado();
        var meiaNoite = await b.Chamado();
        await f.Criado(b.Instituicao, noite, new DateTime(2026, 11, 11, 3, 30, 0, DateTimeKind.Utc));
        await f.Criado(b.Instituicao, meiaNoite, new DateTime(2026, 11, 11, 4, 0, 0, DateTimeKind.Utc));

        Assert.Equal([noite], await NumerosDe(b.A.Cliente, "&de=2026-11-10&ate=2026-11-10"));
        Assert.Equal([meiaNoite], await NumerosDe(b.A.Cliente, "&de=2026-11-11&ate=2026-11-11"));
    }

    [Fact]
    public async Task De_depois_de_ate_400()
    {
        var b = await Balcao.Criar(f);

        var r = await b.A.Cliente.GetAsync("/api/chamados/busca?escopo=fila&de=2026-11-12&ate=2026-11-11");

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.True((await r.Json()).GetProperty("errors").TryGetProperty("de", out _));
    }

    [Fact]
    public async Task Ordem_recentes_e_antigos()
    {
        var b = await Balcao.Criar(f);
        var velho = await b.Chamado();
        var medio = await b.Chamado();
        var novo = await b.Chamado();
        await f.Criado(b.Instituicao, velho, new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));
        await f.Criado(b.Instituicao, medio, new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc));
        await f.Criado(b.Instituicao, novo, new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal([novo, medio, velho], await NumerosDe(b.A.Cliente, "&ordem=recentes"));
        Assert.Equal([velho, medio, novo], await NumerosDe(b.A.Cliente, "&ordem=antigos"));
        Assert.Equal([velho, medio, novo], await NumerosDe(b.A.Cliente));
    }

    [Fact]
    public async Task Pagina_de_50_com_total()
    {
        var b = await Balcao.Criar(f);
        await f.Clonar(b.Instituicao, await b.Chamado(), 50);

        var primeira = await Buscar(b.A.Cliente);
        var segunda = await Buscar(b.A.Cliente, "&pagina=2");
        var terceira = await Buscar(b.A.Cliente, "&pagina=3");

        Assert.Equal((1, 50), (primeira.GetProperty("pagina").GetInt32(), primeira.GetProperty("itens").GetArrayLength()));
        Assert.Equal((2, 1), (segunda.GetProperty("pagina").GetInt32(), segunda.GetProperty("itens").GetArrayLength()));
        Assert.Equal((3, 0), (terceira.GetProperty("pagina").GetInt32(), terceira.GetProperty("itens").GetArrayLength()));
        foreach (var j in new[] { primeira, segunda, terceira })
            Assert.Equal((51, 50), (j.GetProperty("total").GetInt32(), j.GetProperty("porPagina").GetInt32()));
        Assert.Empty(Numeros(primeira).Intersect(Numeros(segunda)));
    }

    [Fact]
    public async Task Forma_da_resposta()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EmAtendimento, "Boleto não chegou");
        var chamado = await b.Ler(numero);
        f.Relogio.Advance(TimeSpan.FromDays(2));

        var j = await Buscar(await f.Entrar(b.Instituicao, b.A.Usuario));

        Assert.Equal(["itens", "total", "pagina", "porPagina"], j.EnumerateObject().Select(p => p.Name));
        var item = Assert.Single(j.GetProperty("itens").EnumerateArray());
        Assert.Equal(["numero", "titulo", "estado", "setor", "categoria", "responsavel", "criadoEm", "dias", "atraso"], item.EnumerateObject().Select(p => p.Name));
        Assert.Equal(numero, item.GetProperty("numero").GetString());
        Assert.Equal("Boleto não chegou", item.GetProperty("titulo").GetString());
        Assert.Equal(EmAtendimento, item.GetProperty("estado").GetString());
        Assert.Equal(b.Triagem.Nome, item.GetProperty("setor").GetString());
        Assert.Equal("Não classificado", item.GetProperty("categoria").GetString());
        Assert.Equal(b.A.Usuario.Nome, item.GetProperty("responsavel").GetString());
        Assert.Equal(chamado.CriadoEm, item.GetProperty("criadoEm").GetDateTime().ToUniversalTime());
        Assert.Equal(2, item.GetProperty("dias").GetInt32());
    }

    [Theory]
    [InlineData("estado=PERDIDO", "estado")]
    [InlineData("ordem=meio", "ordem")]
    [InlineData("responsavel=fulano", "responsavel")]
    [InlineData("escopo=meus", "escopo")]
    [InlineData("pagina=0", "pagina")]
    [InlineData("q=" + "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "q")]
    public async Task Parametro_invalido_400(string consulta, string campo)
    {
        Assert.True(!consulta.StartsWith("q=") || consulta.Length == 103);
        var b = await Balcao.Criar(f);

        var r = await b.A.Cliente.GetAsync("/api/chamados/busca?" + consulta);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.True((await r.Json()).GetProperty("errors").TryGetProperty(campo, out _), campo);
    }

    [Theory]
    [InlineData("estado=PERDIDO", "estado")]
    [InlineData("ordem=meio", "ordem")]
    [InlineData("responsavel=fulano", "responsavel")]
    [InlineData("escopo=meus", "escopo")]
    [InlineData("de=2026-11-12&ate=2026-11-11", "de")]
    [InlineData("q=" + "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "q")]
    public async Task Parametro_invalido_400_na_exportacao(string consulta, string campo)
    {
        var b = await Balcao.Criar(f);

        var r = await b.A.Cliente.GetAsync("/api/chamados/exportar?" + consulta);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.True((await r.Json()).GetProperty("errors").TryGetProperty(campo, out _), campo);
    }
    [Fact]
    public async Task Busca_de_100_caracteres_aparada_200()
    {
        var b = await Balcao.Criar(f);

        var r = await b.A.Cliente.GetAsync("/api/chamados/busca?escopo=fila&q=" + Uri.EscapeDataString("  " + new string('a', 100) + "  "));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Aluno_403()
    {
        var aluno = await f.AlunoLogado();

        foreach (var rota in Rotas)
            Assert.Equal(HttpStatusCode.Forbidden, (await aluno.Cliente.GetAsync(rota + "?escopo=fila")).StatusCode);
    }

    [Fact]
    public async Task Todos_so_para_administrador()
    {
        var b = await Balcao.Criar(f);

        foreach (var rota in Rotas)
            Assert.Equal(HttpStatusCode.Forbidden, (await b.A.Cliente.GetAsync(rota + "?escopo=todos")).StatusCode);
    }

    [Fact]
    public async Task Sem_sessao_401()
    {
        var c = f.Cliente();

        foreach (var rota in Rotas)
            Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync(rota + "?escopo=fila")).StatusCode);
    }

    [Fact]
    public async Task Outra_instituicao_nunca_aparece()
    {
        var b = await Balcao.Criar(f);
        var minha = await b.Chamado(descricao: "Mesmo titulo nas duas");
        var outra = await Balcao.Criar(f);
        for (var n = 0; n < 3; n++) await outra.Chamado(descricao: "Mesmo titulo nas duas");
        var admin = await f.Usuario(b.Instituicao, [Papeis.Administrador]);
        var cliente = await f.Entrar(b.Instituicao, admin);

        var busca = await (await cliente.GetAsync("/api/chamados/busca?escopo=todos&q=titulo")).Json();
        var csv = await (await cliente.GetAsync("/api/chamados/exportar?escopo=todos")).Content.ReadAsStringAsync();
        var filtros = await (await cliente.GetAsync("/api/chamados/filtros?escopo=todos")).Json();

        Assert.Equal([minha], Numeros(busca));
        Assert.Equal(1, busca.GetProperty("total").GetInt32());
        Assert.Contains(minha, csv);
        Assert.Equal(1, csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Length - 1);
        Assert.DoesNotContain(filtros.GetProperty("setores").EnumerateArray(), s => s.GetProperty("id").GetInt64() == outra.Outro.Id || s.GetProperty("id").GetInt64() == outra.Triagem.Id);
        Assert.Equal(2, filtros.GetProperty("setores").GetArrayLength());
    }

    [Fact]
    public async Task Filtros_do_atendente()
    {
        var b = await Balcao.Criar(f);
        await f.Categoria(b.Instituicao, "Boleto");
        await f.Categoria(b.Instituicao, "Antiga", ativa: false);
        await f.Categoria(b.Instituicao, "Acesso ao portal");

        var j = await (await b.A.Cliente.GetAsync("/api/chamados/filtros?escopo=fila")).Json();

        var setor = Assert.Single(j.GetProperty("setores").EnumerateArray());
        Assert.Equal((b.Triagem.Id, b.Triagem.Nome), (setor.GetProperty("id").GetInt64(), setor.GetProperty("nome").GetString()));
        Assert.Equal(["Acesso ao portal", "Boleto", "Não classificado"], j.GetProperty("categorias").EnumerateArray().Select(c => c.GetProperty("nome").GetString()));
        Assert.All(j.GetProperty("categorias").EnumerateArray(), c => Assert.Equal(["id", "nome"], c.EnumerateObject().Select(p => p.Name)));
    }

    [Fact]
    public async Task Extensao_unaccent_instalada()
    {
        var instalada = await f.Db(null, db => db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM pg_extension WHERE extname = 'unaccent'").SingleAsync());

        Assert.Equal(1, instalada);
    }

    DateTime Agora => f.Relogio.GetUtcNow().UtcDateTime;

    async Task<(Balcao, string Primeira, string Resolucao, string EmDia)> ComAtrasos()
    {
        var b = await Balcao.Criar(f);
        var primeira = await b.Chamado(Aberto);
        var resolucao = await b.Chamado(Aberto);
        await Ok(await Balcao.Mensagem(b.A.Cliente, resolucao, "Estamos vendo."));
        var emDia = await b.Chamado(Aberto);
        await f.Criado(b.Instituicao, primeira, Agora.AddHours(-25));
        await f.Criado(b.Instituicao, resolucao, Agora.AddHours(-121));
        await f.Criado(b.Instituicao, emDia, Agora.AddHours(-23));
        return (b, primeira, resolucao, emDia);
    }

    static Dictionary<string, JsonElement> AtrasoPorNumero(JsonElement j) =>
        j.GetProperty("itens").EnumerateArray().ToDictionary(i => i.GetProperty("numero").GetString()!, i => i.GetProperty("atraso"));

    [Fact]
    public async Task Itens_trazem_atraso()
    {
        var (b, primeira, resolucao, emDia) = await ComAtrasos();

        var atrasos = AtrasoPorNumero(await Buscar(b.A.Cliente));

        Assert.Equal("PRIMEIRA_RESPOSTA", atrasos[primeira].GetString());
        Assert.Equal("RESOLUCAO", atrasos[resolucao].GetString());
        Assert.Equal(JsonValueKind.Null, atrasos[emDia].ValueKind);
        Assert.Equal(3, atrasos.Count);
        foreach (var (escopo, cliente) in new[] { ("fila", b.A.Cliente), ("meus", b.ClienteAluno) })
        {
            var r = await cliente.GetAsync($"/api/chamados?escopo={escopo}");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            var itens = (await r.Json()).EnumerateArray().ToList();
            Assert.NotEmpty(itens);
            Assert.All(itens, i => Assert.False(i.TryGetProperty("atraso", out _), escopo));
        }
    }

    [Fact]
    public async Task Filtro_atraso()
    {
        var (b, primeira, resolucao, emDia) = await ComAtrasos();
        var aguardando = await b.Chamado(AguardandoAluno);
        var resolvido = await b.Chamado(Resolvido);
        await f.Criado(b.Instituicao, aguardando, Agora.AddHours(-500));
        await f.Criado(b.Instituicao, resolvido, Agora.AddHours(-500));

        var atrasados = await NumerosDe(b.A.Cliente, "&atraso=true");
        var emDiaLista = await NumerosDe(b.A.Cliente, "&atraso=false");
        var todos = await NumerosDe(b.A.Cliente);

        Assert.Equal([resolucao, primeira], atrasados);
        Assert.Equal(new[] { emDia, aguardando, resolvido }.Order(), emDiaLista.Order());
        Assert.Equal(5, todos.Count);
        Assert.Equal(2, (await Buscar(b.A.Cliente, "&atraso=true")).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Atraso_invalido_responde_400()
    {
        var b = await Balcao.Criar(f);
        await b.Chamado(Aberto);

        foreach (var valor in new[] { "talvez", "" })
        {
            var r = await b.A.Cliente.GetAsync($"/api/chamados/busca?escopo=fila&atraso={valor}");
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
            Assert.True((await r.Json()).GetProperty("errors").TryGetProperty("atraso", out _), valor);
        }
        foreach (var valor in new[] { "true", "false" })
            Assert.Equal(HttpStatusCode.OK, (await b.A.Cliente.GetAsync($"/api/chamados/busca?escopo=fila&atraso={valor}")).StatusCode);
    }
}
