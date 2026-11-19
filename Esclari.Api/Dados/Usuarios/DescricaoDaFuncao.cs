namespace Esclari.Api.Dados.Usuarios;

public sealed record DescricaoDaFuncao(string Codigo, string Rotulo, string Descricao, bool ExpoeDadosPessoais, bool DeAluno)
{
    public static readonly IReadOnlyList<DescricaoDaFuncao> Catalogo =
    [
        new(Funcoes.AssistenteUsar, "Usar o assistente", "Conversar com o assistente, pedir atendimento direto e acompanhar as próprias solicitações. Exige matrícula ativa.", false, true),
        new(Funcoes.FilaAtender, "Atender a fila", "Ver e atender os chamados dos setores em que a pessoa está vinculada. Exige ao menos um setor.", true, false),
        new(Funcoes.ChamadosVerTodos, "Ver os chamados de todos os setores", "Ler os chamados de qualquer setor, exportar a lista e receber o aviso de chamados atrasados.", true, false),
        new(Funcoes.IndicadoresVer, "Ver os indicadores", "Abrir os indicadores de atendimento dos setores a que a pessoa tem acesso.", false, false),
        new(Funcoes.ConhecimentoGerenciar, "Gerenciar a base de conhecimento", "Criar e editar artigos e documentos e receber o aviso de conteúdo perto de vencer.", false, false),
        new(Funcoes.CadastrosGerenciar, "Gerenciar cadastros", "Manter setores, categorias, cursos, respostas prontas e fluxos de atendimento.", false, false),
        new(Funcoes.UsuariosGerenciar, "Gerenciar usuários", "Criar, editar e desativar usuários, ver o CPF deles e escolher o tipo de cada um.", true, false),
        new(Funcoes.MatriculasImportar, "Importar matrículas", "Enviar o arquivo CSV de matrículas, que traz CPF, nome e e-mail dos alunos.", true, false),
        new(Funcoes.TiposGerenciar, "Gerenciar tipos de usuário", "Criar, editar e apagar tipos de usuário e escolher as funções de cada um.", false, false),
        new(Funcoes.AparenciaGerenciar, "Gerenciar a aparência", "Mudar o nome, as cores e as imagens da instituição.", false, false),
        new(Funcoes.IntegracoesGerenciar, "Gerenciar integrações", "Configurar o Moodle e criar ou revogar tokens de acesso ao MCP.", false, false),
        new(Funcoes.McpUsar, "Usar o acesso por MCP", "Consultar a instituição por um token MCP, com os mesmos dados que a pessoa vê no sistema.", true, false),
    ];
}
