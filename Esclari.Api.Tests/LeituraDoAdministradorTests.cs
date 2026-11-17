using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class LeituraDoAdministradorTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    static async Task Ok(HttpResponseMessage r) => Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri}: {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");

    async Task<HttpClient> Administrador(Instituicao i) => await f.Entrar(i, await f.Usuario(i, [Papeis.Administrador]));

    [Fact]
    public async Task Todos_os_setores()
    {
        var b = await Balcao.Criar(f);
        var secretaria = await f.Setor(b.Instituicao, "Secretaria");
        var naTriagem = await b.Chamado();
        var noFinanceiro = await b.Chamado();
        var naSecretaria = await b.Chamado();
        await Ok(await b.A.Cliente.PostAsJsonAsync($"/api/chamados/{noFinanceiro}/encaminhar", new { setorId = b.Outro.Id, motivo = "financeiro" }));
        await Ok(await b.A.Cliente.PostAsJsonAsync($"/api/chamados/{naSecretaria}/encaminhar", new { setorId = secretaria.Id, motivo = "secretaria" }));
        var admin = await Administrador(b.Instituicao);

        var busca = await (await admin.GetAsync("/api/chamados/busca?escopo=todos")).Json();
        var csv = Encoding.UTF8.GetString(await (await admin.GetAsync("/api/chamados/exportar?escopo=todos")).Content.ReadAsByteArrayAsync());
        var fila = await (await admin.GetAsync("/api/chamados/busca?escopo=fila")).Json();

        Assert.Equal([naTriagem, noFinanceiro, naSecretaria], busca.GetProperty("itens").EnumerateArray().Select(i => i.GetProperty("numero").GetString()));
        Assert.Equal(["Triagem", "Financeiro", "Secretaria"], busca.GetProperty("itens").EnumerateArray().Select(i => i.GetProperty("setor").GetString()));
        var linhas = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)[1..];
        Assert.Equal([naTriagem, noFinanceiro, naSecretaria], linhas.Select(l => l.Split(';')[0]));
        Assert.Equal(0, fila.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Filtros_com_todos_os_setores()
    {
        var b = await Balcao.Criar(f);
        await f.Setor(b.Instituicao, "Secretaria");
        var admin = await Administrador(b.Instituicao);
        var todos = await f.Db(b.Instituicao.Id, db => db.Setores.OrderBy(s => s.Nome).Select(s => s.Id).ToListAsync());

        var j = await (await admin.GetAsync("/api/chamados/filtros?escopo=todos")).Json();

        Assert.Equal(3, todos.Count);
        Assert.Equal(todos.Order(), j.GetProperty("setores").EnumerateArray().Select(s => s.GetProperty("id").GetInt64()).Order());
    }

    async Task<(Cenario c, string numero)> Transferido()
    {
        var c = await Cenario.Criar(f);
        var triagem = await f.Db(c.Instituicao.Id, db => db.Setores.SingleAsync(s => s.Triagem));
        var atendente = await f.Atendente(c.Instituicao, triagem);
        f.Llm.Devolver(c.RespostaBoletoValida());
        var (id, _) = await c.Primeira();
        var numero = (await (await c.Cliente.PostAsJsonAsync($"/api/conversas/{id}/transferir", new { })).Json()).GetProperty("numero").GetString()!;
        await Ok(await Balcao.Mensagem(atendente.Cliente, numero, "NOTA-SECRETA: aluno já ligou duas vezes", "NOTA"));
        await Ok(await Balcao.Mensagem(atendente.Cliente, numero, "Vou verificar seu boleto."));
        return (c, numero);
    }

    [Fact]
    public async Task Le_o_detalhe_sem_acoes()
    {
        var (c, numero) = await Transferido();
        var admin = await Administrador(c.Instituicao);

        var r = await admin.GetAsync($"/api/chamados/{numero}");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        Assert.Contains(j.GetProperty("mensagens").EnumerateArray(), m => m.GetProperty("tipo").GetString() == "NOTA" && m.GetProperty("texto").GetString()!.StartsWith("NOTA-SECRETA"));
        var fonte = Assert.Single(j.GetProperty("transcricao")[1].GetProperty("fontes").EnumerateArray());
        Assert.Equal("Como emitir o boleto do semestre", fonte.GetProperty("titulo").GetString());
        Assert.Equal(JsonValueKind.Array, j.GetProperty("eventos").ValueKind);
        Assert.NotEqual(0, j.GetProperty("eventos").GetArrayLength());
        Assert.Empty(j.GetProperty("acoes").EnumerateArray());
    }

    [Fact]
    public async Task Baixa_o_anexo_em_leitura()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado();
        var png = Balcao.Arquivo(Balcao.Png, 3000);
        await Ok(await Balcao.ComAnexos(b.ClienteAluno, numero, "segue o comprovante", "PUBLICA", ("comprovante.png", png)));
        var id = await f.Db(b.Instituicao.Id, db => db.Anexos.Where(a => db.MensagensChamado.Any(m => m.Id == a.MensagemChamadoId && db.Chamados.Any(c => c.Id == m.ChamadoId && c.Numero == numero))).Select(a => a.Id).SingleAsync());
        var admin = await Administrador(b.Instituicao);
        var deOutra = await Administrador((await Balcao.Criar(f)).Instituicao);

        var r = await admin.GetAsync($"/api/anexos/{id}");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(png, await r.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await deOutra.GetAsync($"/api/anexos/{id}")).StatusCode);
    }
    [Fact]
    public async Task Nenhuma_acao_em_leitura()
    {
        var b = await Balcao.Criar(f);
        var numero = await b.Chamado(Esclari.Api.Dados.Chamados.EstadosChamado.EmAtendimento);
        var categoria = await f.Categoria(b.Instituicao, "Financeiro");
        var admin = await Administrador(b.Instituicao);
        var antes = await b.Ler(numero);
        var eventos = (await b.Eventos(numero)).Count;

        var posts = VisibilidadeTests.RotasDoChamado(numero, b.Outro.Id, categoria.Id).Where(x => x.metodo == HttpMethod.Post).ToList();
        Assert.Equal(10, posts.Count);
        foreach (var (metodo, rota, corpo) in posts)
        {
            var r = await admin.SendAsync(new HttpRequestMessage(metodo, rota) { Content = corpo });
            Assert.True(r.StatusCode == HttpStatusCode.Forbidden, $"{rota}: {(int)r.StatusCode}");
            Assert.Equal("Seu papel neste chamado não permite esta ação.", (await r.Json()).GetProperty("detail").GetString());
        }
        var depois = await b.Ler(numero);
        Assert.Equal((antes.Estado, antes.ResponsavelId, antes.SetorId, antes.CategoriaId), (depois.Estado, depois.ResponsavelId, depois.SetorId, depois.CategoriaId));
        Assert.Equal(eventos, (await b.Eventos(numero)).Count);
    }
}
