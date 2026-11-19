namespace Esclari.Api.Dados.Usuarios;

public class Usuario : IDaInstituicao
{
    Usuario() { }

    public long Id { get; private set; }
    public long InstituicaoId { get; set; }
    public Cpf Cpf { get; private set; } = Cpf.Ler("");
    public string Nome { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string? SenhaHash { get; private set; }
    public bool DeveTrocarSenha { get; private set; }
    public bool Ativo { get; private set; } = true;
    public long TipoUsuarioId { get; private set; }
    public TipoDeUsuario? Tipo { get; private set; }
    public List<UsuarioPapel> Papeis { get; private set; } = [];
    public List<Matricula> Matriculas { get; private set; } = [];
    public List<AtendenteSetor> Setores { get; private set; } = [];

    public static Usuario Criar(Cpf cpf, string nome, string email, IEnumerable<string>? papeis = null) =>
        new() { Cpf = cpf, Nome = nome, Email = email, Papeis = [.. (papeis ?? []).Distinct().Select(UsuarioPapel.Criar)] };

    public void DefinirTipo(TipoDeUsuario tipo)
    {
        Tipo = tipo;
        TipoUsuarioId = tipo.Id;
    }

    public IReadOnlyList<string> FuncoesEfetivas()
    {
        var tipo = Tipo ?? throw new InvalidOperationException("Usuário carregado sem o tipo.");
        return [.. tipo.FuncoesDoTipo().Where(f => f != Funcoes.FilaAtender || Setores.Count > 0).Order(StringComparer.Ordinal)];
    }

    public bool Pode(string funcao) => FuncoesEfetivas().Contains(funcao);

    public bool TipoLibera(string funcao) => (Tipo ?? throw new InvalidOperationException("Usuário carregado sem o tipo.")).FuncoesDoTipo().Contains(funcao);

    public bool EDoTipoDeSistema(string sistema) => Tipo?.Sistema == sistema;

    public void Atualizar(DadosUsuario dados)
    {
        Cpf = dados.Cpf;
        AlterarContato(dados.Nome, dados.Email);
    }

    public void AlterarContato(string nome, string email)
    {
        Nome = nome;
        Email = email;
    }

    public void AdicionarPapel(string papel)
    {
        if (!Papeis.Any(p => p.Papel == papel)) Papeis.Add(UsuarioPapel.Criar(papel));
    }

    public void RemoverPapel(string papel) => Papeis.RemoveAll(p => p.Papel == papel);

    public void DefinirPapeis(IEnumerable<string> papeis)
    {
        var novos = papeis.Distinct().ToList();
        Papeis.RemoveAll(p => !novos.Contains(p.Papel));
        foreach (var papel in novos) AdicionarPapel(papel);
    }

    public void VincularSetores(IEnumerable<long> setores)
    {
        var novos = setores.Distinct().ToList();
        Setores.RemoveAll(a => !novos.Contains(a.SetorId));
        Setores.AddRange(novos.Except(Setores.Select(a => a.SetorId)).Select(AtendenteSetor.Criar));
    }

    public void DefinirAcesso(IEnumerable<string> papeis, IEnumerable<long> setores)
    {
        var lista = papeis.ToList();
        DefinirPapeis(lista);
        VincularSetores(PonteDePapeis.ExigeSetor(lista) ? setores : []);
    }

    public Matricula AdicionarMatricula(string numero, long cursoId, int semestre, string situacao)
    {
        var matricula = Matricula.Criar(numero, cursoId, semestre, situacao);
        Matriculas.Add(matricula);
        return matricula;
    }

    public void DefinirSenhaTemporaria(string hash)
    {
        SenhaHash = hash;
        DeveTrocarSenha = true;
    }

    public void DefinirSenha(string hash, TokenAcesso token, DateTime agora)
    {
        TrocarSenha(hash);
        token.Usar(agora);
    }

    public void TrocarSenha(string hash)
    {
        SenhaHash = hash;
        DeveTrocarSenha = false;
    }

    public void Desativar() => Ativo = false;
    public void Reativar() => Ativo = true;
}

