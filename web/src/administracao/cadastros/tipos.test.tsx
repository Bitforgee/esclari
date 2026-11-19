import { cleanup, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { abrir, acoesDe, api, escolher, me, type Rota } from '../../test/util'

const gestor = me({ id: 99, papeis: ['ADMINISTRADOR'], matriculas: [] })

const catalogo = [
  { codigo: 'assistente.usar', rotulo: 'Usar o assistente', descricao: 'Conversar com o assistente e acompanhar as próprias solicitações.', expoeDadosPessoais: false, deAluno: true },
  { codigo: 'fila.atender', rotulo: 'Atender a fila', descricao: 'Ver e atender os chamados dos setores do usuário.', expoeDadosPessoais: true, deAluno: false },
  { codigo: 'chamados.ver_todos', rotulo: 'Ver os chamados de todos os setores', descricao: 'Ler os chamados de qualquer setor.', expoeDadosPessoais: true, deAluno: false },
  { codigo: 'indicadores.ver', rotulo: 'Ver os indicadores', descricao: 'Abrir os indicadores de atendimento.', expoeDadosPessoais: false, deAluno: false },
  { codigo: 'conhecimento.gerenciar', rotulo: 'Gerenciar a base de conhecimento', descricao: 'Criar e editar artigos e documentos.', expoeDadosPessoais: false, deAluno: false },
  { codigo: 'cadastros.gerenciar', rotulo: 'Gerenciar cadastros', descricao: 'Manter setores, categorias e fluxos.', expoeDadosPessoais: false, deAluno: false },
  { codigo: 'usuarios.gerenciar', rotulo: 'Gerenciar usuários', descricao: 'Criar, editar e desativar usuários.', expoeDadosPessoais: true, deAluno: false },
  { codigo: 'matriculas.importar', rotulo: 'Importar matrículas', descricao: 'Enviar o arquivo CSV de matrículas.', expoeDadosPessoais: true, deAluno: false },
  { codigo: 'tipos.gerenciar', rotulo: 'Gerenciar tipos de usuário', descricao: 'Criar, editar e apagar tipos de usuário.', expoeDadosPessoais: false, deAluno: false },
  { codigo: 'aparencia.gerenciar', rotulo: 'Gerenciar a aparência', descricao: 'Mudar o nome, as cores e as imagens.', expoeDadosPessoais: false, deAluno: false },
  { codigo: 'integracoes.gerenciar', rotulo: 'Gerenciar integrações', descricao: 'Configurar o Moodle e os tokens MCP.', expoeDadosPessoais: false, deAluno: false },
  { codigo: 'mcp.usar', rotulo: 'Usar o acesso por MCP', descricao: 'Consultar a instituição por um token MCP.', expoeDadosPessoais: true, deAluno: false },
]
const doGestor = catalogo.map((f) => f.codigo).filter((c) => c !== 'assistente.usar')

const aluno = { id: 1, nome: 'Aluno', sistema: 'ALUNO', funcoes: ['assistente.usar'], usuarios: 12 }
const gestorDoSistema = { id: 2, nome: 'Gestor', sistema: 'GESTOR', funcoes: doGestor, usuarios: 1 }
const atendente = { id: 3, nome: 'Atendente', sistema: 'ATENDENTE', funcoes: ['fila.atender', 'indicadores.ver', 'mcp.usar'], usuarios: 4 }
const coordenador = { id: 5, nome: 'Coordenador de curso', sistema: null, funcoes: ['indicadores.ver', 'chamados.ver_todos'], usuarios: 2 }
const lider = { id: 4, nome: 'Líder de semestre', sistema: null, funcoes: ['assistente.usar', 'fila.atender', 'indicadores.ver', 'mcp.usar'], usuarios: 0 }

const resposta = (tipos: unknown[]) => ({ tipos, catalogo })

function tipos(rotas: Record<string, Rota> = {}, lista: unknown[] = [coordenador, aluno, lider, gestorDoSistema, atendente], busca = '') {
  const chamadas = api({ 'GET /api/auth/me': () => [200, gestor], 'GET /api/admin/tipos': () => [200, resposta(lista)], ...rotas })
  const tela = abrir('/univag/tipos' + busca)
  return { chamadas, ...tela }
}

const linhas = () => within(screen.getByRole('table')).getAllByRole('row').slice(1)
const nomes = () => linhas().map((l) => within(l).getAllByRole('cell')[0].textContent)
const gravacoes = (chamadas: { metodo: string; url: string; corpo: any }[]) => chamadas.filter((c) => c.metodo !== 'GET').map((c) => [c.metodo, c.url, c.corpo])

describe('tipos de usuário', () => {
  it('lista os tipos de sistema primeiro e depois os próprios por nome, com usuários e funções', async () => {
    tipos()
    expect(await screen.findByRole('heading', { level: 1, name: 'Tipos de usuário' })).toBeInTheDocument()
    await screen.findByText('Líder de semestre')
    const esperados = ['Aluno', 'Gestor', 'Atendente', 'Coordenador de curso', 'Líder de semestre']
    expect(nomes()).toHaveLength(5)
    esperados.forEach((nome, i) => expect(nomes()[i], nome).toMatch(new RegExp(`^${nome}`)))
    expect(linhas().map((l) => !!within(l).queryByText('Tipo de sistema'))).toEqual([true, true, true, false, false])
    const [primeira, , terceira, quarta] = linhas()
    expect(within(primeira).getAllByRole('cell')[1]).toHaveTextContent('12')
    expect(within(primeira).getAllByRole('cell')[2]).toHaveTextContent('Usar o assistente')
    expect(within(terceira).getAllByRole('cell')[2]).toHaveTextContent('Atender a fila, Ver os indicadores, Usar o acesso por MCP')
    expect(within(quarta).getAllByRole('cell')[1]).toHaveTextContent('2')
    expect(within(quarta).getAllByRole('cell')[2]).toHaveTextContent('Ver os indicadores, Ver os chamados de todos os setores')
    expect(screen.getByRole('button', { name: 'Novo tipo' })).toBeInTheDocument()
  })

  it('lista os tipos só com os de sistema: os 3 e o botão Novo tipo', async () => {
    tipos({}, [aluno, gestorDoSistema, atendente])
    await screen.findByText('Atendente')
    expect(linhas()).toHaveLength(3)
    expect(screen.getByRole('button', { name: 'Novo tipo' })).toBeInTheDocument()
  })

  it('lista os tipos: 403 mostra a mensagem do servidor e não a lista', async () => {
    const detail = 'Seu tipo de usuário não tem acesso a esta área.'
    tipos({ 'GET /api/admin/tipos': () => [403, { status: 403, detail }] })
    expect(await screen.findByRole('alert')).toHaveTextContent(detail)
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Novo tipo' })).not.toBeInTheDocument()
  })

  it('ações por tipo: sistema fixo sem ações, atendente só edita, próprio edita e apaga', async () => {
    tipos()
    await screen.findByText('Líder de semestre')
    expect(await acoesDe('Aluno')).toEqual([])
    expect(await acoesDe('Gestor')).toEqual([])
    expect(await acoesDe('Atendente')).toEqual(['Editar'])
    expect(await acoesDe('Coordenador de curso')).toEqual(['Editar', 'Apagar!'])
  })

  it('formulário do tipo mostra rótulo e descrição das 12 funções e o aviso só nas 5 que expõem dados pessoais', async () => {
    tipos()
    await userEvent.click(await screen.findByRole('button', { name: 'Novo tipo' }))
    const d = within(await screen.findByRole('dialog', { name: 'Novo tipo' }))
    expect(d.getAllByRole('checkbox')).toHaveLength(12)
    for (const f of catalogo) {
      const caixa = d.getByRole('checkbox', { name: f.rotulo })
      expect(caixa, f.codigo).not.toBeChecked()
      expect(caixa, f.codigo).toHaveAccessibleDescription(expect.stringContaining(f.descricao))
      if (f.expoeDadosPessoais) expect(caixa, f.codigo).toHaveAccessibleDescription(expect.stringContaining('Mostra dados pessoais dos alunos.'))
      else expect(caixa, f.codigo).not.toHaveAccessibleDescription(expect.stringContaining('Mostra dados pessoais dos alunos.'))
      if (f.deAluno) expect(caixa, f.codigo).toHaveAccessibleDescription(expect.stringContaining('Função de aluno'))
      else expect(caixa, f.codigo).not.toHaveAccessibleDescription(expect.stringContaining('Função de aluno'))
    }
    expect(d.getAllByText('Mostra dados pessoais dos alunos.')).toHaveLength(5)
    expect(d.getAllByText('Função de aluno')).toHaveLength(1)
  })

  it('formulário do tipo envia nome e funções marcadas e fecha ao salvar', async () => {
    const { chamadas } = tipos({ 'POST /api/admin/tipos': () => [201, { id: 9 }] })
    await userEvent.click(await screen.findByRole('button', { name: 'Novo tipo' }))
    const d = within(await screen.findByRole('dialog', { name: 'Novo tipo' }))
    await userEvent.type(d.getByLabelText('Nome'), 'Secretaria')
    await userEvent.click(d.getByRole('checkbox', { name: 'Ver os indicadores' }))
    await userEvent.click(d.getByRole('checkbox', { name: 'Atender a fila' }))
    await userEvent.click(d.getByRole('button', { name: 'Salvar' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(gravacoes(chamadas)).toEqual([['POST', '/api/admin/tipos', { nome: 'Secretaria', funcoes: ['indicadores.ver', 'fila.atender'] }]])
  })

  it('formulário do tipo edita pelo PUT com as funções já marcadas', async () => {
    const { chamadas } = tipos({ 'PUT /api/admin/tipos/5': () => [200, {}] })
    await screen.findByText('Líder de semestre')
    await escolher('Coordenador de curso', 'Editar')
    const d = within(await screen.findByRole('dialog', { name: 'Editar tipo' }))
    expect(d.getByLabelText('Nome')).toHaveValue('Coordenador de curso')
    expect(d.getByRole('checkbox', { name: 'Ver os indicadores' })).toBeChecked()
    expect(d.getByRole('checkbox', { name: 'Ver os chamados de todos os setores' })).toBeChecked()
    expect(d.getByRole('checkbox', { name: 'Atender a fila' })).not.toBeChecked()
    await userEvent.click(d.getByRole('checkbox', { name: 'Ver os indicadores' }))
    await userEvent.click(d.getByRole('button', { name: 'Salvar' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(gravacoes(chamadas)).toEqual([['PUT', '/api/admin/tipos/5', { nome: 'Coordenador de curso', funcoes: ['chamados.ver_todos'] }]])
  })

  it('formulário do tipo: o 409 mostra a mensagem do servidor sem fechar', async () => {
    const detail = 'Já existe um tipo com este nome.'
    tipos({ 'POST /api/admin/tipos': () => [409, { status: 409, detail }] })
    await userEvent.click(await screen.findByRole('button', { name: 'Novo tipo' }))
    const d = within(await screen.findByRole('dialog', { name: 'Novo tipo' }))
    await userEvent.type(d.getByLabelText('Nome'), 'Gestor')
    await userEvent.click(d.getByRole('button', { name: 'Salvar' }))
    expect(await d.findByRole('alert')).toHaveTextContent(detail)
    expect(screen.getByRole('dialog', { name: 'Novo tipo' })).toBeInTheDocument()
  })

  it('formulário do tipo: o 400 de nome e de funções aparece junto do campo', async () => {
    tipos({
      'POST /api/admin/tipos': () => [400, { status: 400, detail: 'x', errors: { nome: ['Informe o nome do tipo.'], funcoes: ['Escolha ao menos uma função.'] } }],
    })
    await userEvent.click(await screen.findByRole('button', { name: 'Novo tipo' }))
    const d = within(await screen.findByRole('dialog', { name: 'Novo tipo' }))
    await userEvent.click(d.getByRole('button', { name: 'Salvar' }))
    expect(await d.findByText('Informe o nome do tipo.')).toBeInTheDocument()
    expect(d.getByText('Escolha ao menos uma função.')).toBeInTheDocument()
  })

  it('apagar pede confirmação com o nome do tipo e só o botão de confirmar envia o DELETE', async () => {
    const { chamadas } = tipos({ 'DELETE /api/admin/tipos/5': () => [204, undefined] })
    await screen.findByText('Líder de semestre')
    await escolher('Coordenador de curso', 'Apagar')
    const d = within(await screen.findByRole('dialog'))
    expect(d.getByText('Apagar o tipo Coordenador de curso?')).toBeInTheDocument()
    await userEvent.click(d.getByRole('button', { name: 'Voltar' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(gravacoes(chamadas)).toEqual([])
    await escolher('Coordenador de curso', 'Apagar')
    expect(gravacoes(chamadas)).toEqual([])
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Apagar' }))
    await waitFor(() => expect(gravacoes(chamadas)).toEqual([['DELETE', '/api/admin/tipos/5', undefined]]))
  })

  it('apagar pede confirmação e o 409 de tipo em uso mostra a mensagem do servidor', async () => {
    const detail = '2 usuário(s) usam este tipo.'
    tipos({ 'DELETE /api/admin/tipos/5': () => [409, { status: 409, detail }] })
    await screen.findByText('Líder de semestre')
    await escolher('Coordenador de curso', 'Apagar')
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Apagar' }))
    expect(await screen.findByRole('alert')).toHaveTextContent(detail)
  })

  it('a contagem leva à lista de usuários filtrada pelo tipo e 0 não é link', async () => {
    const { roteador } = tipos({
      'GET /api/admin/usuarios?tipoId=5': () => [200, [{ id: 7, cpf: '123.456.789-09', nome: 'Dora', email: 'dora@univag.edu.br', ativo: true, deveTrocarSenha: false, tipo: { id: 5, nome: 'Coordenador de curso' }, setores: [] }]],
      'GET /api/admin/setores': () => [200, []],
    })
    await screen.findByText('Líder de semestre')
    expect(screen.queryByRole('link', { name: '0' })).not.toBeInTheDocument()
    expect(within(linhas()[4]).getAllByRole('cell')[1]).toHaveTextContent(/^0$/)
    await userEvent.click(screen.getByRole('link', { name: '2' }))
    expect(await screen.findByText('Dora')).toBeInTheDocument()
    expect(roteador.state.location.pathname + roteador.state.location.search).toBe('/univag/usuarios?tipoId=5')
    expect(screen.getByRole('combobox', { name: 'Tipo' })).toHaveValue('5')
  })

  it('o endereço com editar abre o formulário do tipo e ignora tipo de sistema fixo', async () => {
    tipos({}, undefined, '?editar=5')
    const d = within(await screen.findByRole('dialog', { name: 'Editar tipo' }))
    expect(d.getByLabelText('Nome')).toHaveValue('Coordenador de curso')
    cleanup()
    const { roteador } = tipos({}, undefined, '?editar=1')
    await screen.findByText('Líder de semestre')
    await waitFor(() => expect(roteador.state.location.search).toBe(''))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})