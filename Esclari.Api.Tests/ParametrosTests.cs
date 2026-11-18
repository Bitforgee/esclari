using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Esclari.Api.Dados;
using Esclari.Api.Tests.Infra;

namespace Esclari.Api.Tests;

public class ParametrosTests(EsclariFactory f) : IClassFixture<EsclariFactory>
{
    OperadorLogado? operador;

    async Task<HttpClient> Cliente() => (operador ??= await f.Operador()).Cliente;

    static string Rota(Instituicao i) => $"/api/plataforma/instituicoes/{i.Slug}/parametros";

    static readonly string[] Nomes = ["abandonoMinutos", "confirmacaoDias", "lembreteAguardandoDias", "resolverAguardandoDias", "validadeConhecimentoMeses", "primeiraRespostaHoras", "resolucaoHoras"];

    static (int? valor, int padrao, int efetivo) Um(JsonElement j, string nome)
    {
        var p = j.GetProperty(nome);
        return (p.GetProperty("valor").ValueKind == JsonValueKind.Null ? null : p.GetProperty("valor").GetInt32(), p.GetProperty("padrao").GetInt32(), p.GetProperty("efetivo").GetInt32());
    }

    [Fact]
    public async Task Sete_colunas_anulaveis_e_nulas_na_criacao()
    {
        await using var con = new NpgsqlConnection(f.ConnectionString);
        await con.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT column_name, data_type, is_nullable FROM information_schema.columns WHERE table_name = 'instituicao' AND column_name LIKE 'prazo_%' ORDER BY column_name", con);
        await using var r = await cmd.ExecuteReaderAsync();
        var colunas = new List<string>();
        while (await r.ReadAsync())
        {
            Assert.Equal(("integer", "YES"), (r.GetString(1), r.GetString(2)));
            colunas.Add(r.GetString(0));
        }
        Assert.Equal(["prazo_abandono_minutos", "prazo_confirmacao_dias", "prazo_lembrete_aguardando_dias", "prazo_primeira_resposta_horas", "prazo_resolucao_horas", "prazo_resolver_aguardando_dias", "prazo_validade_conhecimento_meses"], colunas);

        var i = await f.Instituicao();
        Assert.Equal(new ParametrosDaInstituicao(), (await f.Db(null, db => db.Instituicoes.SingleAsync(x => x.Id == i.Id))).Parametros);
    }

    [Fact]
    public async Task Ver_valor_padrao_e_efetivo()
    {
        var c = await Cliente();
        var i = await f.Instituicao();

        var r = await c.GetAsync(Rota(i));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        Assert.Equal(Nomes, j.EnumerateObject().Select(p => p.Name));
        Assert.Equal([(null, 30, 30), (null, 7, 7), (null, 3, 3), (null, 7, 7), (null, 12, 12), (null, 24, 24), (null, 120, 120)], Nomes.Select(n => Um(j, n)));
    }

    [Fact]
    public async Task Alterar_grava_restaura_e_audita()
    {
        var c = await Cliente();
        var i = await f.Instituicao();

        var r = await c.PutAsJsonAsync(Rota(i), new { abandonoMinutos = 45, confirmacaoDias = 2, primeiraRespostaHoras = 8, resolucaoHoras = 48 });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        Assert.Equal((45, 30, 45), Um(j, "abandonoMinutos"));
        Assert.Equal((null, 3, 3), Um(j, "lembreteAguardandoDias"));
        Assert.Equal((8, 24, 8), Um(j, "primeiraRespostaHoras"));
        Assert.Equal(new ParametrosDaInstituicao { AbandonoMinutos = 45, ConfirmacaoDias = 2, PrimeiraRespostaHoras = 8, ResolucaoHoras = 48 }, (await f.Db(null, db => db.Instituicoes.SingleAsync(x => x.Id == i.Id))).Parametros);

        var restaura = await (await c.PutAsJsonAsync(Rota(i), new { abandonoMinutos = (int?)null, confirmacaoDias = 2, primeiraRespostaHoras = 8, resolucaoHoras = 48 })).Json();
        Assert.Equal((null, 30, 30), Um(restaura, "abandonoMinutos"));
        Assert.Equal(new ParametrosDaInstituicao { ConfirmacaoDias = 2, PrimeiraRespostaHoras = 8, ResolucaoHoras = 48 }, (await f.Db(null, db => db.Instituicoes.SingleAsync(x => x.Id == i.Id))).Parametros);

        var resumos = await f.Db(null, db => db.AuditoriaDaPlataforma.Where(a => a.InstituicaoAlvoId == i.Id && a.Acao == AcoesDaPlataforma.ParametrosAlterados).OrderBy(a => a.Id).Select(a => a.Resumo).ToListAsync());
        Assert.Equal(2, resumos.Count);
        Assert.Contains("abandonoMinutos=45", resumos[0]);
        Assert.Contains("confirmacaoDias=2", resumos[0]);
        Assert.Contains("resolucaoHoras=48", resumos[0]);
        Assert.Contains("abandonoMinutos=padrão", resumos[1]);
        Assert.DoesNotContain("confirmacaoDias", resumos[1]);
    }

    [Fact]
    public async Task Faixas_400_por_campo()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        var fora = new (string campo, int valor)[]
        {
            ("abandonoMinutos", 4), ("abandonoMinutos", 1441), ("confirmacaoDias", 0), ("confirmacaoDias", 61),
            ("lembreteAguardandoDias", 0), ("lembreteAguardandoDias", 91), ("resolverAguardandoDias", 0), ("resolverAguardandoDias", 91),
            ("validadeConhecimentoMeses", 0), ("validadeConhecimentoMeses", 61), ("primeiraRespostaHoras", 0), ("primeiraRespostaHoras", 8761), ("resolucaoHoras", 0), ("resolucaoHoras", 8761),
        };
        foreach (var (campo, valor) in fora)
        {
            var r = await c.PutAsJsonAsync(Rota(i), new Dictionary<string, int> { [campo] = valor });
            Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{campo}={valor}: {(int)r.StatusCode}");
            Assert.True((await r.Json()).GetProperty("errors").TryGetProperty(campo, out _), campo);
        }
        Assert.Equal(new ParametrosDaInstituicao(), (await f.Db(null, db => db.Instituicoes.SingleAsync(x => x.Id == i.Id))).Parametros);

        foreach (var limites in new object[]
        {
            new { abandonoMinutos = 5, confirmacaoDias = 1, lembreteAguardandoDias = 1, resolverAguardandoDias = 90, validadeConhecimentoMeses = 1, primeiraRespostaHoras = 1, resolucaoHoras = 8760 },
            new { abandonoMinutos = 1440, confirmacaoDias = 60, lembreteAguardandoDias = 89, resolverAguardandoDias = 90, validadeConhecimentoMeses = 60, primeiraRespostaHoras = 8760, resolucaoHoras = 8760 },
        })
            Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync(Rota(i), limites)).StatusCode);
    }

    [Fact]
    public async Task Conjunto_efetivo_400_sem_gravar()
    {
        var c = await Cliente();
        var i = await f.Instituicao();

        foreach (var (corpo, campo) in new (object, string)[]
        {
            (new { primeiraRespostaHoras = 10, resolucaoHoras = 9 }, "resolucaoHoras"),
            (new { resolucaoHoras = 23 }, "resolucaoHoras"),
            (new { lembreteAguardandoDias = 7 }, "lembreteAguardandoDias"),
            (new { lembreteAguardandoDias = 5, resolverAguardandoDias = 4 }, "lembreteAguardandoDias"),
        })
        {
            var r = await c.PutAsJsonAsync(Rota(i), corpo);
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.True((await r.Json()).GetProperty("errors").TryGetProperty(campo, out _), campo);
        }
        Assert.Equal(new ParametrosDaInstituicao(), (await f.Db(null, db => db.Instituicoes.SingleAsync(x => x.Id == i.Id))).Parametros);
        Assert.Equal(0, await f.Db(null, db => db.AuditoriaDaPlataforma.CountAsync(a => a.InstituicaoAlvoId == i.Id)));
    }

    [Fact]
    public async Task Slug_404_e_sem_sessao_401()
    {
        var c = await Cliente();
        var i = await f.Instituicao();

        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/plataforma/instituicoes/nao-existe-xx/parametros")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync("/api/plataforma/instituicoes/nao-existe-xx/parametros", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Cliente().GetAsync(Rota(i))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Cliente().PutAsJsonAsync(Rota(i), new { })).StatusCode);
    }

    [Fact]
    public async Task Mesmo_put_duas_vezes_deixa_o_mesmo_estado()
    {
        var c = await Cliente();
        var i = await f.Instituicao();
        var corpo = new { abandonoMinutos = 45, confirmacaoDias = 2, primeiraRespostaHoras = 8, resolucaoHoras = 48 };

        var primeira = await c.PutAsJsonAsync(Rota(i), corpo);
        var depoisDaPrimeira = await f.Db(null, db => db.Instituicoes.AsNoTracking().SingleAsync(x => x.Id == i.Id));
        var segunda = await c.PutAsJsonAsync(Rota(i), corpo);
        var depoisDaSegunda = await f.Db(null, db => db.Instituicoes.AsNoTracking().SingleAsync(x => x.Id == i.Id));

        Assert.Equal((HttpStatusCode.OK, HttpStatusCode.OK), (primeira.StatusCode, segunda.StatusCode));
        Assert.Equal(await primeira.Content.ReadAsStringAsync(), await segunda.Content.ReadAsStringAsync());
        Assert.Equal(new ParametrosDaInstituicao { AbandonoMinutos = 45, ConfirmacaoDias = 2, PrimeiraRespostaHoras = 8, ResolucaoHoras = 48 }, depoisDaPrimeira.Parametros);
        Assert.Equal(depoisDaPrimeira.Parametros, depoisDaSegunda.Parametros);
        var resumos = await f.Db(null, db => db.AuditoriaDaPlataforma.Where(a => a.InstituicaoAlvoId == i.Id && a.Acao == AcoesDaPlataforma.ParametrosAlterados).OrderBy(a => a.Id).Select(a => a.Resumo).ToListAsync());
        Assert.Equal(2, resumos.Count);
        Assert.EndsWith("nenhuma mudança", resumos[1]);
    }
}
