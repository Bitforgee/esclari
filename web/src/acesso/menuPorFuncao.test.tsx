import { cleanup, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { abrir, api, chamado, instituicao, me, rotasDaFila } from '../test/util'

const celular = () =>
  vi.stubGlobal('matchMedia', (media: string) => ({ media, matches: media.startsWith('(max-width: 767px)'), addEventListener() {}, removeEventListener() {}, addListener() {}, removeListener() {} }))

const comFuncoes = (permissoes: string[], tipo = 'Tipo de teste') => ({
  'GET /api/auth/me': () => [200, me({ tipo: { id: 9, nome: tipo }, permissoes })] as [number, unknown],
  'GET /api/instituicoes/univag': () => [200, instituicao()] as [number, unknown],
})

const links = async () => within(await screen.findByRole('navigation')).queryAllByRole('link').map((l) => l.textContent)

async function entrar() {
  await userEvent.type(await screen.findByLabelText('CPF'), '123.456.789-09')
  await userEvent.type(screen.getByLabelText('Senha'), 'senha-boa-123')
  await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))
}

afterEach(() => {
  window.innerWidth = 1024
  vi.unstubAllGlobals()
})

const MenuPorFuncao: [string, string[]][] = [
  ['assistente.usar', ['Assistente', 'Minhas solicitações']],
  ['fila.atender', ['Fila']],
  ['chamados.ver_todos', ['Chamados']],
  ['indicadores.ver', ['Indicadores']],
  ['conhecimento.gerenciar', ['Artigos', 'Documentos']],
  ['usuarios.gerenciar', ['Usuários']],
  ['matriculas.importar', ['Matrículas']],
  ['cadastros.gerenciar', ['Cadastros']],
  ['aparencia.gerenciar', ['Aparência']],
  ['integracoes.gerenciar', ['Integrações']],
  ['tipos.gerenciar', ['Tipos de usuário']],
  ['mcp.usar', []],
]

describe('menu por função', () => {
  it.each(MenuPorFuncao)('uma função só: %s', async (funcao, itens) => {
    api(comFuncoes([funcao]))
    abrir('/univag/conta')
    expect(await links()).toEqual(itens)
  })

  it('cobre as 12 funções do catálogo', () => {
    expect(MenuPorFuncao.map(([f]) => f).sort()).toEqual(
      ['assistente.usar', 'fila.atender', 'chamados.ver_todos', 'indicadores.ver', 'conhecimento.gerenciar', 'cadastros.gerenciar', 'usuarios.gerenciar', 'matriculas.importar', 'tipos.gerenciar', 'aparencia.gerenciar', 'integracoes.gerenciar', 'mcp.usar'].sort(),
    )
  })
})

describe('tela de entrada por função', () => {
  it.each([
    [['assistente.usar', 'fila.atender'], 'Assistente'],
    [['fila.atender', 'indicadores.ver'], 'Fila'],
    [['chamados.ver_todos', 'usuarios.gerenciar'], 'Chamados'],
    [['indicadores.ver', 'cadastros.gerenciar'], 'Indicadores'],
    [['conhecimento.gerenciar', 'usuarios.gerenciar'], 'Artigos'],
    [['usuarios.gerenciar'], 'Usuários'],
  ])('entrada com %j abre %s', async (permissoes, tela) => {
    api({
      ...comFuncoes(permissoes),
      ...rotasDaFila([]),
      'POST /api/auth/login': () => [200, { id: 1, nome: 'Ana', tipo: { id: 9, nome: 'Tipo de teste' }, permissoes, deveTrocarSenha: false, instituicao: { slug: 'univag', nome: 'Univag' } }],
    })
    abrir('/univag')
    await entrar()
    expect(await screen.findByRole('heading', { level: 1, name: tela })).toBeInTheDocument()
  })

  it('endereço de tela sem a função volta para a tela de entrada', async () => {
    api({ ...comFuncoes(['fila.atender', 'indicadores.ver']), ...rotasDaFila([]) })
    const { roteador } = abrir('/univag/usuarios')
    await waitFor(() => expect(roteador.state.location.pathname).toBe('/univag/fila'))
    cleanup()
    api({ ...comFuncoes(['usuarios.gerenciar']) })
    const outro = abrir('/univag/tipos')
    await waitFor(() => expect(outro.roteador.state.location.pathname).toBe('/univag/usuarios'))
  })
})

describe('sem funções liberadas', () => {
  const Mensagem = 'Seu tipo de usuário ainda não tem funções liberadas. Fale com o gestor da sua instituição.'

  it.each([[[]], [['mcp.usar']]])('permissões %j', async (permissoes) => {
    api(comFuncoes(permissoes))
    const { roteador } = abrir('/univag/assistente')
    expect(await screen.findByText(Mensagem)).toBeInTheDocument()
    expect(roteador.state.location.pathname).toBe('/univag/conta')
    expect(await links()).toEqual([])
    await userEvent.click(screen.getByRole('button', { name: /Ana/ }))
    expect(await screen.findByRole('menuitem', { name: 'Sair' })).toBeInTheDocument()
  })

  it.each([[[]], [['mcp.usar']]])('no celular só a aba Conta (%j)', async (permissoes) => {
    celular()
    api(comFuncoes(permissoes))
    abrir('/univag/assistente')
    expect(await screen.findByText(Mensagem)).toBeInTheDocument()
    const abas = within(await screen.findByRole('navigation', { name: 'Navegação' }))
    expect(abas.getAllByRole('link').map((l) => l.textContent)).toEqual(['Conta'])
    expect(screen.getByRole('button', { name: 'Sair' })).toBeInTheDocument()
  })

  it('quem tem alguma função não vê a mensagem', async () => {
    api({ ...comFuncoes(['usuarios.gerenciar']) })
    abrir('/univag/usuarios')
    await screen.findByRole('heading', { level: 1, name: 'Usuários' })
    expect(screen.queryByText(Mensagem)).not.toBeInTheDocument()
  })
})

describe('líder de semestre', () => {
  const Lider = ['assistente.usar', 'fila.atender', 'indicadores.ver', 'mcp.usar']
  const rotas = () => ({
    ...comFuncoes(Lider, 'Líder de semestre'),
    ...rotasDaFila([]),
    'GET /api/chamados/ESC-000001': () => [200, chamado()] as [number, unknown],
    'GET /api/push/chave': () => [200, { chavePublica: 'BAEC' }] as [number, unknown],
    'POST /api/auth/login': () => [200, { id: 1, nome: 'Ana', tipo: { id: 9, nome: 'Líder de semestre' }, permissoes: Lider, deveTrocarSenha: false, instituicao: { slug: 'univag', nome: 'Univag' } }] as [number, unknown],
  })

  it('menu na ordem do aluno e da fila', async () => {
    api(rotas())
    abrir('/univag/conta')
    expect(await links()).toEqual(['Assistente', 'Minhas solicitações', 'Fila', 'Indicadores'])
  })

  it('entra no assistente com o botão de avisos do aluno', async () => {
    Object.defineProperty(navigator, 'serviceWorker', { configurable: true, value: { ready: Promise.resolve({ pushManager: { getSubscription: async () => null } }) } })
    vi.stubGlobal('PushManager', class {})
    vi.stubGlobal('Notification', { requestPermission: async () => 'granted' })
    api(rotas())
    abrir('/univag')
    await entrar()
    const cabecalho = (await screen.findByRole('heading', { level: 1, name: 'Assistente' })).closest('header') as HTMLElement
    expect(await within(cabecalho).findByRole('button', { name: 'Ativar avisos neste aparelho' })).toBeInTheDocument()
    Reflect.deleteProperty(navigator, 'serviceWorker')
  })

  it('Voltar do chamado leva à fila', async () => {
    celular()
    api(rotas())
    abrir('/univag/chamados/ESC-000001')
    await userEvent.click(await screen.findByRole('button', { name: 'Voltar' }))
    await waitFor(() => expect(document.querySelector('header h1')).toHaveTextContent('Fila'))
  })
})