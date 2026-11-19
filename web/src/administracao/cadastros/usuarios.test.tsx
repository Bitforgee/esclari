import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { abrir, acoesDe, api, escolher, me, type Rota } from '../../test/util'

const admin = me({ id: 99, papeis: ['ADMINISTRADOR'], matriculas: [] })
const setores = [
  { id: 1, nome: 'Triagem', triagem: true, ativo: true },
  { id: 2, nome: 'Financeiro', triagem: false, ativo: true },
  { id: 3, nome: 'Antigo', triagem: false, ativo: false },
]
const tipoAluno = { id: 3, nome: 'Aluno' }
const tipoGestor = { id: 1, nome: 'Gestor' }
const tipoAtendente = { id: 2, nome: 'Atendente' }
const carla = { id: 1, cpf: '123.456.789-09', nome: 'Carla', email: 'carla@univag.edu.br', ativo: true, deveTrocarSenha: false, tipo: tipoAtendente, setores: [2] }
const ana = { id: 99, cpf: '111.444.777-35', nome: 'Ana', email: 'ana@univag.edu.br', ativo: true, deveTrocarSenha: false, tipo: tipoGestor, setores: [] }
const bia = { id: 2, cpf: '987.654.321-00', nome: 'Bia', email: 'bia@univag.edu.br', ativo: false, deveTrocarSenha: false, tipo: tipoAluno, setores: [] }
const FuncoesDoGestor = ['fila.atender', 'chamados.ver_todos', 'indicadores.ver', 'conhecimento.gerenciar', 'cadastros.gerenciar', 'usuarios.gerenciar', 'matriculas.importar', 'tipos.gerenciar', 'aparencia.gerenciar', 'integracoes.gerenciar', 'mcp.usar']
const tipos = {
  tipos: [
    { ...tipoAluno, sistema: 'ALUNO', funcoes: ['assistente.usar'], usuarios: 1 },
    { ...tipoGestor, sistema: 'GESTOR', funcoes: FuncoesDoGestor, usuarios: 1 },
    { ...tipoAtendente, sistema: 'ATENDENTE', funcoes: ['fila.atender', 'indicadores.ver', 'mcp.usar'], usuarios: 1 },
    { id: 4, nome: 'Líder de semestre', sistema: null, funcoes: ['assistente.usar', 'fila.atender', 'indicadores.ver', 'mcp.usar'], usuarios: 0 },
    { id: 5, nome: 'Coordenador de curso', sistema: null, funcoes: ['indicadores.ver'], usuarios: 0 },
  ],
  catalogo: [],
}

function usuarios(rotas: Record<string, Rota> = {}) {
  const chamadas = api({ 'GET /api/auth/me': () => [200, admin], 'GET /api/admin/usuarios': () => [200, [carla, bia]], 'GET /api/admin/setores': () => [200, setores], 'GET /api/admin/tipos': () => [200, tipos], ...rotas })
  abrir('/univag/usuarios')
  return chamadas
}

const item = async (nome: string) => (await screen.findByText(nome)).closest('tr') as HTMLElement
const posts = (chamadas: { metodo: string; url: string; corpo: any }[]) => chamadas.filter((c) => c.metodo !== 'GET').map((c) => [c.metodo, c.url, c.corpo])

describe('usuários', () => {
  it('lista papéis, setores e o selo de desativado', async () => {
    usuarios()
    const ativa = await item('Carla')
    expect(ativa).toHaveTextContent('123.456.789-09 · carla@univag.edu.br')
    expect(ativa).toHaveTextContent('Atendente · Financeiro')
    expect(within(ativa).queryByText('Desativado')).not.toBeInTheDocument()
    expect(within(ativa).getByRole('button', { name: 'Ações de Carla' })).toBeInTheDocument()
    const inativa = await item('Bia')
    expect(within(inativa).getByText('Desativado')).toBeInTheDocument()
    expect(inativa).toHaveTextContent('987.654.321-00 · bia@univag.edu.br · Aluno')
    expect(within(inativa).getAllByRole('cell')[3]).toHaveTextContent(/^Aluno$/)
    expect(within(inativa).queryByRole('button')).not.toBeInTheDocument()
    expect(screen.queryByText('Cadastre a equipe ou importe as matrículas.')).not.toBeInTheDocument()
  })

  it('ações de usuário no menu', async () => {
    usuarios()
    await screen.findByText('Carla')
    expect(await acoesDe('Carla')).toEqual(['Editar', 'Desativar!'])
    expect(screen.queryByRole('button', { name: 'Ações de Bia' })).not.toBeInTheDocument()
  })

  it('o próprio gestor não desativa a conta nem muda o próprio tipo', async () => {
    const chamadas = usuarios({ 'GET /api/admin/usuarios': () => [200, [ana, carla]], 'PUT /api/admin/usuarios/99': () => [200, {}] })
    await screen.findByRole('button', { name: 'Ações de Ana' })
    expect(await acoesDe('Ana')).toEqual(['Editar'])
    expect(await acoesDe('Carla')).toEqual(['Editar', 'Desativar!'])
    await escolher('Ana', 'Editar')
    const d = within(await screen.findByRole('dialog'))
    expect(d.getByText('Você não pode mudar o próprio tipo. Peça a outro gestor.')).toBeInTheDocument()
    expect(d.queryByLabelText('Tipo')).not.toBeInTheDocument()
    expect(d.queryByRole('combobox')).not.toBeInTheDocument()
    await userEvent.click(d.getByRole('button', { name: 'Salvar usuário' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(posts(chamadas)).toEqual([['PUT', '/api/admin/usuarios/99', { cpf: '111.444.777-35', nome: 'Ana', email: 'ana@univag.edu.br', tipoId: 1, setores: [] }]])
  })

  it('mostra o erro da API ao salvar ou desativar sem sobrar gestor', async () => {
    const detail = 'A instituição precisa de ao menos um gestor ativo.'
    usuarios({
      'PUT /api/admin/usuarios/1': () => [409, { status: 409, detail }],
      'POST /api/admin/usuarios/1/desativar': () => [409, { status: 409, detail }],
    })
    await escolher('Carla', 'Editar')
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Salvar usuário' }))
    expect(await within(screen.getByRole('dialog')).findByRole('alert')).toHaveTextContent(detail)
    await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Cancelar' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    await escolher('Carla', 'Desativar')
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Desativar' }))
    expect(await screen.findByText(detail)).toBeInTheDocument()
  })
  it('carregando mostra 3 esqueletos', async () => {
    api({ 'GET /api/auth/me': () => [200, admin], 'GET /api/admin/usuarios': () => new Promise(() => {}), 'GET /api/admin/setores': () => [200, setores], 'GET /api/admin/tipos': () => [200, tipos] })
    abrir('/univag/usuarios')
    expect(await screen.findByRole('heading', { name: 'Usuários' })).toBeInTheDocument()
    expect(document.querySelectorAll('[data-slot="skeleton"]')).toHaveLength(3)
  })

  it('editar troca setores e a senha temporária e mantém o tipo', async () => {
    const chamadas = usuarios({ 'PUT /api/admin/usuarios/1': () => [200, {}] })
    await escolher('Carla', 'Editar')
    const d = within(await screen.findByRole('dialog'))
    expect(d.getByText('Editar usuário')).toBeInTheDocument()
    expect(d.getByLabelText('CPF')).toHaveValue('123.456.789-09')
    expect(d.getByLabelText('Nome')).toHaveValue('Carla')
    expect(d.getByLabelText('E-mail')).toHaveValue('carla@univag.edu.br')
    expect(d.getByLabelText('Tipo')).toHaveValue('2')
    expect(d.getByLabelText('Financeiro')).toBeChecked()
    expect(d.getByLabelText('Triagem')).not.toBeChecked()
    expect(d.queryByLabelText('Antigo')).not.toBeInTheDocument()
    await userEvent.click(d.getByLabelText('Financeiro'))
    await userEvent.click(d.getByLabelText('Triagem'))
    await userEvent.type(d.getByLabelText(/Senha temporária/), 'temporaria-123')
    await userEvent.click(d.getByRole('button', { name: 'Salvar usuário' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(posts(chamadas)).toEqual([
      ['PUT', '/api/admin/usuarios/1', { cpf: '123.456.789-09', nome: 'Carla', email: 'carla@univag.edu.br', tipoId: 2, setores: [1], senhaTemporaria: 'temporaria-123' }],
    ])
  })

  it('campo Tipo de escolha única no lugar das caixas de papel, preenchido pelos tipos do servidor', async () => {
    usuarios()
    await userEvent.click(await screen.findByRole('button', { name: /Novo/ }))
    const d = within(await screen.findByRole('dialog'))
    expect(d.getByText('Novo usuário')).toBeInTheDocument()
    const campo = d.getByLabelText('Tipo')
    expect(campo.tagName).toBe('SELECT')
    expect(within(campo).getAllByRole('option').map((o) => o.textContent)).toEqual(['Escolha o tipo', 'Aluno', 'Gestor', 'Atendente', 'Líder de semestre', 'Coordenador de curso'])
    for (const papel of ['Administrador', 'Atendente', 'Aluno']) expect(d.queryByRole('checkbox', { name: papel })).not.toBeInTheDocument()
    expect(d.queryByText('Papéis')).not.toBeInTheDocument()
  })

  it('campo Tipo mostra os setores só quando o tipo tem fila.atender e não é GESTOR', async () => {
    usuarios()
    await userEvent.click(await screen.findByRole('button', { name: /Novo/ }))
    const d = within(await screen.findByRole('dialog'))
    expect(d.queryByText('Setores de quem atende')).not.toBeInTheDocument()
    for (const [tipo, comSetores] of [['Atendente', true], ['Líder de semestre', true], ['Gestor', false], ['Aluno', false], ['Coordenador de curso', false]] as const) {
      await userEvent.selectOptions(d.getByLabelText('Tipo'), tipo)
      if (comSetores) expect(d.getByText('Setores de quem atende'), tipo).toBeInTheDocument()
      else expect(d.queryByText('Setores de quem atende'), tipo).not.toBeInTheDocument()
    }
  })

  it('campo Tipo: o POST envia tipoId e não envia papeis, e só os setores de quem atende', async () => {
    const chamadas = usuarios({ 'POST /api/admin/usuarios': () => [201, {}] })
    await userEvent.click(await screen.findByRole('button', { name: /Novo/ }))
    const d = within(await screen.findByRole('dialog'))
    await userEvent.type(d.getByLabelText('CPF'), '123.456.789-09')
    await userEvent.type(d.getByLabelText('Nome'), 'Davi')
    await userEvent.type(d.getByLabelText('E-mail'), 'davi@univag.edu.br')
    await userEvent.selectOptions(d.getByLabelText('Tipo'), 'Líder de semestre')
    await userEvent.click(d.getByLabelText('Triagem'))
    await userEvent.selectOptions(d.getByLabelText('Tipo'), 'Gestor')
    await userEvent.click(d.getByRole('button', { name: 'Salvar usuário' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(posts(chamadas)).toEqual([['POST', '/api/admin/usuarios', { cpf: '123.456.789-09', nome: 'Davi', email: 'davi@univag.edu.br', tipoId: 1, setores: [] }]])
    expect(posts(chamadas)[0][2]).not.toHaveProperty('papeis')
  })

  it('campo Tipo: mostra os erros do servidor sem fechar o formulário', async () => {
    usuarios({
      'POST /api/admin/usuarios': (corpo) =>
        corpo.tipoId === null
          ? [400, { status: 400, detail: 'Escolha o tipo do usuário.', errors: { tipoId: ['Escolha o tipo do usuário.'] } }]
          : [400, { status: 400, detail: 'Escolha ao menos um setor para quem atende a fila.', errors: { setores: ['Escolha ao menos um setor para quem atende a fila.'] } }],
    })
    await userEvent.click(await screen.findByRole('button', { name: /Novo/ }))
    const d = within(await screen.findByRole('dialog'))
    await userEvent.click(d.getByRole('button', { name: 'Salvar usuário' }))
    expect(await d.findByText('Escolha o tipo do usuário.')).toBeInTheDocument()
    await userEvent.selectOptions(d.getByLabelText('Tipo'), 'Atendente')
    await userEvent.click(d.getByRole('button', { name: 'Salvar usuário' }))
    expect(await d.findByText('Escolha ao menos um setor para quem atende a fila.')).toBeInTheDocument()
    expect(screen.getByRole('dialog')).toBeInTheDocument()
  })

  it('campo Tipo: 403 de atribuição aparece no formulário', async () => {
    const detail = 'Você não pode atribuir ou retirar um tipo com funções que o seu tipo não tem.'
    usuarios({ 'PUT /api/admin/usuarios/1': () => [403, { status: 403, detail }] })
    await escolher('Carla', 'Editar')
    const d = within(await screen.findByRole('dialog'))
    await userEvent.click(d.getByRole('button', { name: 'Salvar usuário' }))
    expect(await d.findByRole('alert')).toHaveTextContent(detail)
  })

  it('a lista mostra o filtro Tipo com todos os tipos e começa sem filtro', async () => {
    const chamadas = usuarios()
    await item('Carla')
    const filtro = screen.getByRole('combobox', { name: 'Tipo' })
    expect(within(filtro).getAllByRole('option').map((o) => o.textContent)).toEqual(['Todos os tipos', 'Aluno', 'Gestor', 'Atendente', 'Líder de semestre', 'Coordenador de curso'])
    expect(chamadas.filter((c) => c.url.startsWith('/api/admin/usuarios')).map((c) => c.url)).toEqual(['/api/admin/usuarios'])
  })

  it('filtra por tipo pedindo tipoId ao servidor e mostrando só esses usuários', async () => {
    const chamadas = usuarios({ 'GET /api/admin/usuarios?tipoId=2': () => [200, [carla]], 'GET /api/admin/usuarios?tipoId=5': () => [200, []] })
    await item('Carla')
    expect(screen.getByText('Bia')).toBeInTheDocument()
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Tipo' }), 'Atendente')
    await waitFor(() => expect(chamadas.map((c) => c.url)).toContain('/api/admin/usuarios?tipoId=2'))
    await waitFor(() => expect(screen.queryByText('Bia')).not.toBeInTheDocument())
    expect(screen.getByText('Carla')).toBeInTheDocument()
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Tipo' }), 'Coordenador de curso')
    expect(await screen.findByText('Nenhum usuário combina com a busca.')).toBeInTheDocument()
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Tipo' }), 'Todos os tipos')
    expect(await screen.findByText('Bia')).toBeInTheDocument()
  })

  it('filtra por tipo a partir do endereço com tipoId', async () => {
    const chamadas = api({
      'GET /api/auth/me': () => [200, admin],
      'GET /api/admin/usuarios?tipoId=2': () => [200, [carla]],
      'GET /api/admin/setores': () => [200, setores],
      'GET /api/admin/tipos': () => [200, tipos],
    })
    abrir('/univag/usuarios?tipoId=2')
    expect(await item('Carla')).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: 'Tipo' })).toHaveValue('2')
    expect(chamadas.filter((c) => c.url.startsWith('/api/admin/usuarios')).map((c) => c.url)).toEqual(['/api/admin/usuarios?tipoId=2'])
  })
  it('separador antes de Desativar', async () => {
    usuarios()
    await userEvent.click(await screen.findByRole('button', { name: 'Ações de Carla' }))
    const menu = await screen.findByRole('menu')
    const itens = within(menu).getAllByRole('menuitem')
    const sep = within(menu).getByRole('separator')
    expect(itens[1]).toHaveTextContent('Desativar')
    expect(sep.compareDocumentPosition(itens[1]) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(itens[0].compareDocumentPosition(sep) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })
  it('desativar pede confirmação', async () => {
    const chamadas = usuarios({ 'POST /api/admin/usuarios/1/desativar': () => [200, {}] })
    await escolher('Carla', 'Desativar')
    const d = within(await screen.findByRole('dialog'))
    expect(d.getByText('Desativar Carla?')).toBeInTheDocument()
    await userEvent.click(d.getByRole('button', { name: 'Voltar' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(posts(chamadas)).toEqual([])
    await escolher('Carla', 'Desativar')
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Desativar' }))
    await waitFor(() => expect(posts(chamadas)).toEqual([['POST', '/api/admin/usuarios/1/desativar', {}]]))
  })

  it('toasts de usuário só no sucesso', async () => {
    await new Promise((r) => setTimeout(r, 50))
    let tentativa = 0
    usuarios({
      'PUT /api/admin/usuarios/1': () => (++tentativa === 1 ? [422, { status: 422, detail: 'CPF repetido.' }] : [200, {}]),
      'POST /api/admin/usuarios/1/desativar': () => [200, {}],
    })
    const avisos = () => screen.getByRole('region', { name: /^Notificações/ })
    await escolher('Carla', 'Editar')
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Salvar usuário' }))
    expect(await within(screen.getByRole('dialog')).findByRole('alert')).toHaveTextContent('CPF repetido.')
    expect(avisos()).not.toHaveTextContent('Usuário salvo.')
    await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Salvar usuário' }))
    await waitFor(() => expect(avisos()).toHaveTextContent('Usuário salvo.'))
    await escolher('Carla', 'Desativar')
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Desativar' }))
    await waitFor(() => expect(avisos()).toHaveTextContent('Usuário desativado.'))
  })

  it('ações de linha mostram o Spinner enquanto gravam', async () => {
    let responder!: () => void
    usuarios({ 'POST /api/admin/usuarios/1/desativar': () => new Promise((resolve) => (responder = () => resolve([200, {}]))) })
    await escolher('Carla', 'Desativar')
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Desativar' }))
    const gatilho = await screen.findByRole('button', { name: 'Ações de Carla' })
    await waitFor(() => expect(gatilho).toBeDisabled())
    expect(within(gatilho).getByRole('status')).toBeInTheDocument()
    responder()
    await waitFor(() => expect(within(gatilho).queryByRole('status')).not.toBeInTheDocument())
  })
})
