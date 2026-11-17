using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class ArtigoDoChamadoTests(EsclariFactoryValidade f) : IClassFixture<EsclariFactoryValidade>
{
    const string NaoResolvido = "O chamado ainda não foi resolvido.";
    const string ComCpf = "Remova o CPF do texto antes de salvar.";

    static object Corpo(string titulo = "Segunda via do boleto", string[]? perguntas = null, string resposta = "Boleto reemitido.", string[]? links = null) =>
        new { titulo, perguntas = perguntas ?? ["como tiro a segunda via do boleto?"], resposta, links = links ?? [] };

    static Task<HttpResponseMessage> Criar(HttpClient c, string numero, object? corpo = null) =>
        c.PostAsJsonAsync($"/api/chamados/{numero}/artigo", corpo ?? Corpo());

    Task<int> Artigos(Balcao b) => f.Db(b.Instituicao.Id, db => db.Artigos.CountAsync());

    Task<int> DaOrigem(Balcao b, long chamadoId) => f.Db(b.Instituicao.Id, db => db.Artigos.CountAsync(a => a.OrigemChamadoId == chamadoId));

    async Task<HttpClient> Administrador(Instituicao i) => await f.Entrar(i, await f.Usuario(i, [Papeis.Administrador]));

    static async Task Problema(HttpResponseMessage r, HttpStatusCode status, string detalhe)
    {
        var corpo = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == status, $"{(int)r.StatusCode} {corpo}");
        Assert.Equal(detalhe, JsonDocument.Parse(corpo).RootElement.GetProperty("detail").GetString());
    }

    static async Task Campo(HttpResponseMessage r, string campo, string? mensagem = null)
    {
        var corpo = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{campo}: {(int)r.StatusCode} {corpo}");
        var erros = JsonDocument.Parse(corpo).RootElement.GetProperty("errors");
        Assert.True(erros.TryGetProperty(campo, out var textos), $"{campo}: {corpo}");
        if (mensagem is not null) Assert.Equal(mensagem, textos[0].GetString());
    }

    [Fact]
    public async Task Atendente_responsavel_cria_rascunho_do_chamado_resolvido()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EstadosChamado.Resolvido);
        var chamado = await b.Ler(numero);
        Assert.Equal(b.A.Usuario.Id, chamado.ResponsavelId);

        var r = await Criar(b.A.Cliente, numero);

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var j = await r.Json();
        var id = j.GetProperty("id").GetInt64();
        Assert.Equal($"/api/admin/artigos/{id}", r.Headers.Location?.OriginalString);
        Assert.Equal("RASCUNHO", j.GetProperty("estado").GetString());
        Assert.Equal(chamado.CategoriaId, j.GetProperty("categoriaId").GetInt64());
        Assert.Equal(numero, j.GetProperty("origemChamado").GetString());
        Assert.StartsWith("ESC-", numero);
        Assert.Equal("2027-11-17", j.GetProperty("validoAte").GetString());
        Assert.Equal("VIGENTE", j.GetProperty("validade").GetString());
        Assert.Equal("Segunda via do boleto", j.GetProperty("versao").GetProperty("titulo").GetString());
        Assert.Equal(chamado.Id, await f.Db(b.Instituicao.Id, db => db.Artigos.Where(a => a.Id == id).Select(a => a.OrigemChamadoId).SingleAsync()));
    }

    [Fact]
    public async Task Chamado_fechado_tambem_origina()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EstadosChamado.Fechado);

        var r = await Criar(b.A.Cliente, numero);

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var j = await r.Json();
        Assert.Equal(("RASCUNHO", numero), (j.GetProperty("estado").GetString(), j.GetProperty("origemChamado").GetString()));
    }

    [Fact]
    public async Task Atendente_do_setor_sem_ser_responsavel_cria()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EstadosChamado.Resolvido);
        Assert.NotEqual(b.B.Usuario.Id, (await b.Ler(numero)).ResponsavelId);

        var r = await Criar(b.B.Cliente, numero);

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.Equal(numero, (await r.Json()).GetProperty("origemChamado").GetString());
    }

    [Fact]
    public async Task Lider_do_setor_cria()
    {
        var t = await Turma.Criar(f);
        var numero = await t.NaLideranca();
        Assert.Equal(t.Lideranca!.Id, (await t.Ler(numero)).SetorId);
        Assert.True((await t.Lider!.Cliente.PostAsync($"/api/chamados/{numero}/assumir", null)).IsSuccessStatusCode);
        Assert.True((await t.Lider.Cliente.PostAsJsonAsync($"/api/chamados/{numero}/resolver", new { texto = "Nota revisada pela coordenação." })).IsSuccessStatusCode);
        Assert.Equal(EstadosChamado.Resolvido, (await t.Ler(numero)).Estado);

        var r = await Criar(t.Lider.Cliente, numero, Corpo(resposta: "Nota revisada pela coordenação."));

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.Equal(numero, (await r.Json()).GetProperty("origemChamado").GetString());
    }

    [Fact]
    public async Task Categoria_e_a_atual_do_chamado()
    {
        var b = await Balcao.Criar(f);
        var boleto = await f.Categoria(b.Instituicao, "Boleto");
        var matricula = await f.Categoria(b.Instituicao, "Matrícula");
        var numero = await b.Chamado(EstadosChamado.EmAtendimento);
        Assert.True((await b.A.Cliente.PostAsJsonAsync($"/api/chamados/{numero}/reclassificar", new { categoriaId = boleto.Id })).IsSuccessStatusCode);
        Assert.True((await b.A.Cliente.PostAsJsonAsync($"/api/chamados/{numero}/resolver", new { texto = "Boleto reemitido." })).IsSuccessStatusCode);
        Assert.True((await b.A.Cliente.PostAsJsonAsync($"/api/chamados/{numero}/reclassificar", new { categoriaId = matricula.Id })).IsSuccessStatusCode);
        var chamado = await b.Ler(numero);
        Assert.Equal((EstadosChamado.Resolvido, matricula.Id), (chamado.Estado, chamado.CategoriaId));

        var j = await (await Criar(b.A.Cliente, numero)).Json();

        Assert.Equal(matricula.Id, j.GetProperty("categoriaId").GetInt64());
        Assert.Equal("Matrícula", j.GetProperty("categoria").GetString());
        Assert.NotEqual(boleto.Id, j.GetProperty("categoriaId").GetInt64());
    }

    [Fact]
    public async Task Mesmas_regras_do_artigo_do_administrador()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EstadosChamado.Resolvido);

        await Campo(await Criar(b.A.Cliente, numero, Corpo(titulo: "")), "titulo");
        await Campo(await Criar(b.A.Cliente, numero, Corpo(perguntas: [])), "perguntas", "Informe ao menos uma pergunta relacionada.");
        await Campo(await Criar(b.A.Cliente, numero, Corpo(perguntas: ["  "])), "perguntas", "Informe ao menos uma pergunta relacionada.");
        await Campo(await Criar(b.A.Cliente, numero, Corpo(resposta: "")), "resposta", "Informe a resposta, com até 8000 caracteres.");
        await Campo(await Criar(b.A.Cliente, numero, Corpo(resposta: new string('a', 8001))), "resposta", "Informe a resposta, com até 8000 caracteres.");
        await Campo(await Criar(b.A.Cliente, numero, Corpo(links: ["ftp://portal"])), "links", "Todo link deve começar com https:// ou http://.");
        Assert.Equal(0, await Artigos(b));

        var limite = await Criar(b.A.Cliente, numero, Corpo(resposta: new string('a', 8000)));

        Assert.Equal(HttpStatusCode.Created, limite.StatusCode);
        Assert.Equal(1, await Artigos(b));
    }

    [Fact]
    public async Task Chamado_nao_resolvido_responde_409()
    {
        var b = await Balcao.Criar(f);
        foreach (var estado in new[] { EstadosChamado.Aberto, EstadosChamado.EmAtendimento, EstadosChamado.AguardandoAluno })
        {
            var numero = await b.Chamado(estado);
            Assert.Equal(estado, (await b.Ler(numero)).Estado);

            await Problema(await Criar(b.A.Cliente, numero), HttpStatusCode.Conflict, NaoResolvido);
        }
        Assert.Equal(0, await Artigos(b));
    }

    [Fact]
    public async Task Segundo_artigo_do_mesmo_chamado_responde_409()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EstadosChamado.Resolvido);
        Assert.Equal(HttpStatusCode.Created, (await Criar(b.A.Cliente, numero)).StatusCode);

        var r = await Criar(b.B.Cliente, numero, Corpo(titulo: "Outro título para o boleto"));

        await Problema(r, HttpStatusCode.Conflict, "Este chamado já originou o artigo “Segunda via do boleto”.");
        Assert.Equal(1, await DaOrigem(b, (await b.Ler(numero)).Id));
        Assert.Equal(1, await Artigos(b));
    }

    [Fact]
    public async Task Duas_chamadas_simultaneas_criam_um_artigo()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EstadosChamado.Resolvido);

        var respostas = await Task.WhenAll(Criar(b.A.Cliente, numero), Criar(b.B.Cliente, numero));

        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], respostas.Select(r => r.StatusCode).Order());
        await Problema(respostas.Single(r => r.StatusCode == HttpStatusCode.Conflict), HttpStatusCode.Conflict, "Este chamado já originou o artigo “Segunda via do boleto”.");
        Assert.Equal(1, await DaOrigem(b, (await b.Ler(numero)).Id));
    }

    [Fact]
    public async Task Chamado_que_o_usuario_nao_ve_responde_404()
    {
        var b = await Balcao.Criar(f);
        var outra = await Balcao.Criar(f);
        foreach (var _ in Enumerable.Range(0, 3)) await outra.Chamado(EstadosChamado.Resolvido);
        var deOutra = await outra.Chamado(EstadosChamado.Resolvido);
        Assert.False(await f.Db(b.Instituicao.Id, db => db.Chamados.AnyAsync(c => c.Numero == deOutra)));
        var numero = await b.Chamado(EstadosChamado.Resolvido);

        await Problema(await Criar(b.A.Cliente, "ESC-999999"), HttpStatusCode.NotFound, "Chamado não encontrado.");
        await Problema(await Criar(b.A.Cliente, deOutra), HttpStatusCode.NotFound, "Chamado não encontrado.");
        await Problema(await Criar(b.Alheio.Cliente, numero), HttpStatusCode.NotFound, "Chamado não encontrado.");

        Assert.Equal(0, await Artigos(b));
        Assert.Equal(0, await Artigos(outra));
    }

    [Fact]
    public async Task Administrador_e_aluno_recebem_403()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EstadosChamado.Resolvido);
        var admin = await Administrador(b.Instituicao);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/chamados/{numero}")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await Criar(admin, numero)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Criar(b.ClienteAluno, numero)).StatusCode);
        Assert.Equal(0, await Artigos(b));
    }

    [Fact]
    public async Task Sem_sessao_responde_401()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EstadosChamado.Resolvido);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Criar(f.Cliente(), numero)).StatusCode);
        Assert.Equal(0, await Artigos(b));
    }

    [Fact]
    public async Task Cpf_no_texto_responde_400()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EstadosChamado.Resolvido);

        await Campo(await Criar(b.A.Cliente, numero, Corpo(titulo: "Boleto do CPF 111.444.777-35")), "titulo", ComCpf);
        await Campo(await Criar(b.A.Cliente, numero, Corpo(perguntas: ["boleto do 11144477735?"])), "perguntas", ComCpf);
        await Campo(await Criar(b.A.Cliente, numero, Corpo(resposta: "Boleto reemitido para 111.444.777-35.")), "resposta", ComCpf);
        Assert.Equal(0, await Artigos(b));

        var errado = await Criar(b.A.Cliente, numero, Corpo(resposta: "Protocolo 111.444.777-36 do boleto."));

        Assert.Equal(HttpStatusCode.Created, errado.StatusCode);
    }

    [Fact]
    public async Task Recusas_na_ordem_acesso_campos_estado()
    {
        var b = await Balcao.Criar(f);
        var aberto = await b.Chamado();
        var resolvido = await b.Chamado(EstadosChamado.Resolvido);
        Assert.Equal(HttpStatusCode.Created, (await Criar(b.A.Cliente, resolvido)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await Criar(b.ClienteAluno, aberto, Corpo(titulo: ""))).StatusCode);
        await Campo(await Criar(b.A.Cliente, aberto, Corpo(titulo: "")), "titulo");
        await Campo(await Criar(b.A.Cliente, aberto, Corpo(resposta: "CPF 11144477735")), "resposta", ComCpf);
        await Problema(await Criar(b.A.Cliente, aberto), HttpStatusCode.Conflict, NaoResolvido);
        await Campo(await Criar(b.A.Cliente, resolvido, Corpo(titulo: "")), "titulo");
        Assert.Equal(1, await Artigos(b));
    }

    [Fact]
    public async Task Registra_log_sem_o_texto()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EstadosChamado.Resolvido);
        const string Modelo = "Rascunho de artigo criado a partir do chamado {Numero} (instituição {InstituicaoId})";

        Assert.Equal(HttpStatusCode.Created, (await Criar(b.A.Cliente, numero, Corpo(titulo: "Título que não vai ao log", resposta: "Resposta que não vai ao log"))).StatusCode);

        var log = Assert.Single(f.Logs.Entradas, l => Equals(l.Campos.GetValueOrDefault("{OriginalFormat}"), Modelo) && Equals(l.Campos["InstituicaoId"], b.Instituicao.Id));
        Assert.Equal(LogLevel.Information, log.Nivel);
        Assert.Equal(numero, log.Campos["Numero"]);
        Assert.DoesNotContain("Título que não vai ao log", log.Mensagem);
        Assert.DoesNotContain("Resposta que não vai ao log", log.Mensagem);
        Assert.Contains(numero, log.Mensagem);
    }

    [Fact]
    public async Task Indice_unico_parcial_da_origem()
    {
        var b = await Balcao.Criar(f);
        var chamado = await b.Ler(await b.Chamado(EstadosChamado.Resolvido));
        var (tipo, anulavel) = await f.Db(null, async db => (
            await db.Database.SqlQueryRaw<string>("SELECT data_type AS \"Value\" FROM information_schema.columns WHERE table_name = 'artigo' AND column_name = 'origem_chamado_id'").SingleAsync(),
            await db.Database.SqlQueryRaw<string>("SELECT is_nullable AS \"Value\" FROM information_schema.columns WHERE table_name = 'artigo' AND column_name = 'origem_chamado_id'").SingleAsync()));
        Assert.Equal(("bigint", "YES"), (tipo, anulavel));
        var definicao = await f.Db(null, db => db.Database.SqlQueryRaw<string>("SELECT indexdef AS \"Value\" FROM pg_indexes WHERE tablename = 'artigo' AND indexdef LIKE '%origem_chamado_id%'").SingleAsync());
        Assert.Contains("UNIQUE", definicao);
        Assert.EndsWith("WHERE (origem_chamado_id IS NOT NULL)", definicao);

        Task Inserir(Artigo a) => f.Db(b.Instituicao.Id, async db =>
        {
            db.Artigos.Add(a);
            await db.SaveChangesAsync();
        });
        VersaoArtigo Versao() => VersaoArtigo.Criar(new("t", ["p?"], "r", []), 1, f.Relogio.GetUtcNow().UtcDateTime);

        await Inserir(Artigo.Criar(chamado.CategoriaId, null, null, Versao(), f.Validade()));
        await Inserir(Artigo.Criar(chamado.CategoriaId, null, null, Versao(), f.Validade()));
        await Inserir(Artigo.DoChamado(chamado.Id, chamado.CategoriaId, Versao(), f.Validade()));
        var e = await Assert.ThrowsAsync<DbUpdateException>(() => Inserir(Artigo.DoChamado(chamado.Id, chamado.CategoriaId, Versao(), f.Validade())));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(e.InnerException).SqlState);
        Assert.Equal(3, await Artigos(b));
        Assert.Equal(1, await DaOrigem(b, chamado.Id));
    }

    [Fact]
    public async Task Rascunho_so_entra_na_base_depois_de_ativar()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EstadosChamado.Resolvido);
        var j = await (await Criar(b.A.Cliente, numero)).Json();
        var (id, versao) = (j.GetProperty("id").GetInt64(), j.GetProperty("versao").GetProperty("id").GetInt64());
        Task<int> Trechos() => f.Db(b.Instituicao.Id, db => db.TrechosFonte.CountAsync(t => t.VersaoArtigoId == versao));
        Assert.Equal(0, await Trechos());

        var r = await (await Administrador(b.Instituicao)).PostAsync($"/api/admin/artigos/{id}/ativar", null);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("ATIVO", (await r.Json()).GetProperty("estado").GetString());
        Assert.Equal(1, await Trechos());
    }

    [Fact]
    public async Task Lista_traz_a_origem_do_chamado()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(EstadosChamado.Resolvido);
        var doChamado = (await (await Criar(b.A.Cliente, numero)).Json()).GetProperty("id").GetInt64();
        var admin = await Administrador(b.Instituicao);
        var categoria = await f.Categoria(b.Instituicao, "Acadêmico");
        var doAdmin = await admin.NovoArtigo(categoria.Id, ativar: false);

        var origem = await admin.Item("artigos", doChamado);
        var semOrigem = await admin.Item("artigos", doAdmin);

        Assert.Equal(numero, origem.GetProperty("origemChamado").GetString());
        Assert.Equal(JsonValueKind.Null, semOrigem.GetProperty("origemChamado").ValueKind);
    }

    [Fact]
    public async Task Detalhe_diz_se_pode_criar_ou_ja_criou()
    {
        var b = await Balcao.Criar(f);
        var resolvido = await b.Chamado(EstadosChamado.Resolvido);
        var emAtendimento = await b.Chamado(EstadosChamado.EmAtendimento);
        var admin = await Administrador(b.Instituicao);
        static bool Tem(JsonElement j) => j.TryGetProperty("artigoDaResposta", out _);

        Assert.Equal("PODE_CRIAR", (await b.Detalhe(b.A.Cliente, resolvido)).GetProperty("artigoDaResposta").GetString());
        Assert.False(Tem(await b.Detalhe(b.A.Cliente, emAtendimento)));
        Assert.False(Tem(await b.Detalhe(b.ClienteAluno, resolvido)));
        Assert.False(Tem(await b.Detalhe(admin, resolvido)));

        Assert.Equal(HttpStatusCode.Created, (await Criar(b.A.Cliente, resolvido)).StatusCode);

        Assert.Equal("CRIADO", (await b.Detalhe(b.A.Cliente, resolvido)).GetProperty("artigoDaResposta").GetString());
        Assert.Equal("CRIADO", (await b.Detalhe(b.B.Cliente, resolvido)).GetProperty("artigoDaResposta").GetString());
        Assert.False(Tem(await b.Detalhe(b.ClienteAluno, resolvido)));
        Assert.False(Tem(await b.Detalhe(admin, resolvido)));
    }
}
