using Esclari.Api.Dados;

namespace Esclari.Api;

public sealed class PrazosAtuais(Prazos padrao, EsclariDb db)
{
    Prazos? lido;

    public async Task<Prazos> Ler() => lido ??= db.InstituicaoAtual is { } id ? await padrao.DaInstituicao(db, id) : padrao;
}
