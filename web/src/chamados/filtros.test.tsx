import { act, fireEvent, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { abrir, api, FILA, me, type Chamada, type Rota } from '../test/util'

const atendente = me({ papeis: ['ATENDENTE'], matriculas: [] })
const opcoes = { setores: [{ id: 2, nome: 'Financeiro' }, { id: 1, nome: 'Triagem' }], categorias: [{ id: 5, nome: 'Boleto' }] }
const item = (n: number, titulo = `pedido ${n}`) => ({ numero: `ESC-${String(n).padStart(6, '0')}`, titulo, estado: 'ABERTO', setor: 'Triagem', categoria: 'Boleto', responsavel: null, criadoEm: '2026-11-02T12:00:00Z', dias: 1 })
const pagina = (itens: unknown[], total = itens.length, numero = 1) => ({ itens, total, pagina: numero, porPagina: 50 })

function abrirFila(consulta: string, rotas: Record<string, Rota>) {
  const chamadas = api({ 'GET /api/auth/me': () => [200, atendente], 'GET /api/chamados/filtros?escopo=fila': () => [200, opcoes], ...rotas })
  const { roteador } = abrir('/univag/fila' + consulta)
  return { chamadas, roteador }
}

const buscas = (chamadas: Chamada[], url: string) => chamadas.filter((c) => c.metodo === 'GET' && c.url === url).length
const campo = () => screen.findByRole('searchbox', { name: 'Buscar por número, título ou aluno' })
const params = (roteador: { state: { location: { search: string } } }) => new URLSearchParams(roteador.state.location.search)
const linhasDoCorpo = () => within(screen.getByRole('table')).getAllByRole('row').slice(1)

async function abrirPainel() {
  await userEvent.click(await screen.findByRole('button', { name: /^Filtros/ }))
  return within(await screen.findByRole('dialog', { name: 'Filtros' }))
}

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

describe('filtros da fila', () => {
  it('barra da fila', async () => {
    abrirFila('', { [`GET ${FILA}`]: () => [200, pagina([item(1)])] })
    await screen.findByText('ESC-000001')
    const busca = await campo()
    const filtros = screen.getByRole('button', { name: 'Filtros' })
    const exportar = screen.getByRole('button', { name: 'Exportar CSV' })
    const tabela = screen.getByRole('table')
    for (const el of [busca, filtros, exportar]) expect(el.compareDocumentPosition(tabela) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it('busca 300 ms depois', async () => {
    const { chamadas, roteador } = abrirFila('', { [`GET ${FILA}`]: () => [200, pagina([item(1)])], [`GET ${FILA}&q=boleto`]: () => [200, pagina([item(2, 'boleto')])] })
    const busca = await campo()
    await screen.findByText('ESC-000001')
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
    fireEvent.change(busca, { target: { value: 'boleto' } })
    await act(async () => vi.advanceTimersByTime(299))
    expect(buscas(chamadas, `${FILA}&q=boleto`)).toBe(0)
    await act(async () => vi.advanceTimersByTimeAsync(1))
    await act(async () => vi.advanceTimersByTimeAsync(50))
    vi.useRealTimers()
    await screen.findByText('ESC-000002')
    expect(buscas(chamadas, `${FILA}&q=boleto`)).toBe(1)
    expect(params(roteador).get('q')).toBe('boleto')
  })

  it('ordem da tela', async () => {
    abrirFila('?estado=FECHADO', { [`GET ${FILA}&estado=FECHADO`]: () => [200, pagina([item(1)])] })
    await screen.findByText('ESC-000001')
    const barra = document.querySelector('[data-barra-da-busca]')!
    const etiquetas = document.querySelector('[data-etiquetas]')!
    const contagem = screen.getByText('1 chamado').closest('p')!
    const tabela = screen.getByRole('table')
    const ordem = [barra, etiquetas, contagem, tabela]
    for (let i = 1; i < ordem.length; i++) expect(ordem[i - 1].compareDocumentPosition(ordem[i]) & Node.DOCUMENT_POSITION_FOLLOWING, String(i)).toBeTruthy()
    expect(within(barra as HTMLElement).getByRole('searchbox')).toBeInTheDocument()
    expect(within(barra as HTMLElement).getByRole('button', { name: /^Filtros/ })).toBeInTheDocument()
    expect(within(barra as HTMLElement).getByRole('button', { name: 'Exportar CSV' })).toBeInTheDocument()
    expect(within(etiquetas as HTMLElement).getByRole('button', { name: 'Limpar filtros' })).toBeInTheDocument()
    expect(contagem).toHaveTextContent(/^1 chamado · Mais antigos primeiro$/)
  })

  it('contagem diz a ordem', async () => {
    abrirFila('?ordem=recentes', { [`GET ${FILA}&ordem=recentes`]: () => [200, pagina([item(1), item(2)])] })
    await screen.findByText('ESC-000002')
    expect(screen.getByText('2 chamados').closest('p')).toHaveTextContent(/^2 chamados · Mais recentes primeiro$/)
  })

  it('ordem dos campos do painel', async () => {
    abrirFila('', { [`GET ${FILA}`]: () => [200, pagina([item(1)])] })
    await screen.findByText('ESC-000001')
    const painel = await abrirPainel()
    const corpo = painel.getByRole('group', { name: 'Estado' }).parentElement!
    const titulos = [...corpo.children].map((c) => (c.tagName === 'FIELDSET' ? c.querySelector('legend')!.textContent : c.querySelector('label')!.textContent))
    expect(titulos).toEqual(['Estado', 'Setor', 'Categoria', 'Responsável', 'Período de abertura', 'Ordem'])
  })
  it('painel aplica em lote', async () => {
    const { chamadas } = abrirFila('', { [`GET ${FILA}`]: () => [200, pagina([item(1)])] })
    await screen.findByText('ESC-000001')
    const painel = await abrirPainel()
    const estados = within(painel.getByRole('group', { name: 'Estado' })).getAllByRole('checkbox')
    expect(estados.map((c) => c.closest('label')!.textContent)).toEqual(['Aberto', 'Em atendimento', 'Aguardando aluno', 'Resolvido', 'Fechado'])
    expect(painel.getByRole('combobox', { name: 'Setor' })).toBeInTheDocument()
    expect(within(painel.getByRole('combobox', { name: 'Categoria' })).getAllByRole('option').map((o) => o.textContent)).toEqual(['Todas', 'Boleto'])
    expect(within(painel.getByRole('combobox', { name: 'Responsável' })).getAllByRole('option').map((o) => o.textContent)).toEqual(['Qualquer', 'Eu', 'Sem responsável'])
    expect(within(painel.getByRole('combobox', { name: 'Ordem' })).getAllByRole('option').map((o) => o.textContent)).toEqual(['Mais antigos primeiro', 'Mais recentes primeiro'])
    expect(painel.getByRole('group', { name: 'Período de abertura' })).toBeInTheDocument()
    expect(painel.getByLabelText('De')).toHaveAttribute('type', 'date')
    expect(painel.getByLabelText('Até')).toHaveAttribute('type', 'date')
    const antes = chamadas.filter((c) => c.url.startsWith('/api/chamados/busca')).length
    await userEvent.click(painel.getByRole('checkbox', { name: 'Fechado' }))
    await new Promise((r) => setTimeout(r, 50))
    expect(chamadas.filter((c) => c.url.startsWith('/api/chamados/busca')).length).toBe(antes)
    expect(painel.getByRole('checkbox', { name: 'Fechado' })).toBeChecked()
  })

  it('aplicar grava na url', async () => {
    const filtrada = `${FILA}&estado=FECHADO&setorId=2&categoriaId=5&responsavel=eu&de=2026-11-01&ate=2026-11-30&ordem=recentes`
    const { chamadas, roteador } = abrirFila('', { [`GET ${FILA}`]: () => [200, pagina([item(1)])], [`GET ${filtrada}`]: () => [200, pagina([item(9)])] })
    await screen.findByText('ESC-000001')
    const painel = await abrirPainel()
    await userEvent.click(painel.getByRole('checkbox', { name: 'Fechado' }))
    await userEvent.selectOptions(painel.getByRole('combobox', { name: 'Setor' }), 'Financeiro')
    await userEvent.selectOptions(painel.getByRole('combobox', { name: 'Categoria' }), 'Boleto')
    await userEvent.selectOptions(painel.getByRole('combobox', { name: 'Responsável' }), 'Eu')
    fireEvent.change(painel.getByLabelText('De'), { target: { value: '2026-11-01' } })
    fireEvent.change(painel.getByLabelText('Até'), { target: { value: '2026-11-30' } })
    await userEvent.selectOptions(painel.getByRole('combobox', { name: 'Ordem' }), 'Mais recentes primeiro')
    await userEvent.click(painel.getByRole('button', { name: 'Aplicar' }))

    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Filtros' })).not.toBeInTheDocument())
    await screen.findByText('ESC-000009')
    const p = params(roteador)
    expect([p.getAll('estado'), p.get('setor'), p.get('categoria'), p.get('responsavel'), p.get('de'), p.get('ate'), p.get('ordem')]).toEqual([['FECHADO'], '2', '5', 'eu', '2026-11-01', '2026-11-30', 'recentes'])
    expect(buscas(chamadas, filtrada)).toBe(1)
  })

  it('url inicial preenche', async () => {
    const url = `${FILA}&q=boleto&estado=FECHADO&responsavel=ninguem&ordem=recentes`
    const { chamadas } = abrirFila('?q=boleto&estado=FECHADO&responsavel=ninguem&ordem=recentes', { [`GET ${url}`]: () => [200, pagina([item(3, 'boleto')])] })
    await screen.findByText('ESC-000003')
    expect(buscas(chamadas, url)).toBe(1)
    expect(await campo()).toHaveValue('boleto')
    const painel = await abrirPainel()
    expect(painel.getByRole('checkbox', { name: 'Fechado' })).toBeChecked()
    expect(painel.getByRole('checkbox', { name: 'Aberto' })).not.toBeChecked()
    expect(painel.getByRole('combobox', { name: 'Responsável' })).toHaveDisplayValue('Sem responsável')
    expect(painel.getByRole('combobox', { name: 'Ordem' })).toHaveDisplayValue('Mais recentes primeiro')
  })

  it('etiquetas dos filtros', async () => {
    const { chamadas, roteador } = abrirFila('?estado=FECHADO&responsavel=eu', {
      [`GET ${FILA}&estado=FECHADO&responsavel=eu`]: () => [200, pagina([item(1)])],
      [`GET ${FILA}&responsavel=eu`]: () => [200, pagina([item(2)])],
      [`GET ${FILA}`]: () => [200, pagina([item(3)])],
    })
    await screen.findByText('ESC-000001')
    expect(screen.getByRole('button', { name: 'Remover filtro Estado' })).toHaveTextContent('Estado: Fechado')
    expect(screen.getByRole('button', { name: 'Remover filtro Responsável' })).toHaveTextContent('Responsável: Eu')
    expect(screen.getByRole('button', { name: 'Filtros (2)' })).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Remover filtro Estado' }))
    await screen.findByText('ESC-000002')
    expect(params(roteador).getAll('estado')).toEqual([])
    expect(params(roteador).get('responsavel')).toBe('eu')
    expect(buscas(chamadas, `${FILA}&responsavel=eu`)).toBe(1)
    expect(screen.getByRole('button', { name: 'Filtros (1)' })).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Limpar filtros' }))
    await screen.findByText('ESC-000003')
    expect(params(roteador).toString()).toBe('')
    expect(screen.queryByRole('button', { name: /^Remover filtro/ })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Filtros' })).toBeInTheDocument()
  })

  it('contagem do total', async () => {
    for (const [total, texto] of [
      [73, '73 chamados'],
      [1, '1 chamado'],
      [0, '0 chamados'],
    ] as const) {
      const { unmount } = (() => {
        api({ 'GET /api/auth/me': () => [200, atendente], [`GET ${FILA}`]: () => [200, pagina(total ? [item(1)] : [], total)] })
        return abrir('/univag/fila')
      })()
      expect(await screen.findByText(texto)).toBeInTheDocument()
      unmount()
    }
  })

  it('vazio com e sem filtro', async () => {
    const { roteador } = abrirFila('?estado=FECHADO', { [`GET ${FILA}&estado=FECHADO`]: () => [200, pagina([])], [`GET ${FILA}`]: () => [200, pagina([])] })
    const celula = (await screen.findByText('Nenhum chamado com esses filtros.')).closest('td') as HTMLElement
    await userEvent.click(within(celula).getByRole('button', { name: 'Limpar filtros' }))
    expect(await screen.findByText('Nenhum chamado na fila dos seus setores.')).toBeInTheDocument()
    expect(screen.queryByText('Nenhum chamado com esses filtros.')).not.toBeInTheDocument()
    expect(params(roteador).toString()).toBe('')
  })

  it('mostrar mais pede a pagina seguinte', async () => {
    const primeira = Array.from({ length: 50 }, (_, i) => item(i + 1))
    const segunda = Array.from({ length: 11 }, (_, i) => item(i + 50))
    const { chamadas } = abrirFila('', { [`GET ${FILA}`]: () => [200, pagina(primeira, 60)], [`GET ${FILA}&pagina=2`]: () => [200, pagina(segunda, 60, 2)] })
    await screen.findByText('ESC-000050')
    expect(linhasDoCorpo()).toHaveLength(50)
    await userEvent.click(screen.getByRole('button', { name: 'Mostrar mais 50' }))
    await screen.findByText('ESC-000060')
    expect(buscas(chamadas, `${FILA}&pagina=2`)).toBe(1)
    expect(linhasDoCorpo()).toHaveLength(60)
    expect(screen.getAllByText('ESC-000050')).toHaveLength(1)
    expect(screen.queryByRole('button', { name: 'Mostrar mais 50' })).not.toBeInTheDocument()
  })

  it('erro com tentar de novo', async () => {
    let falhas = 1
    const url = `${FILA}&estado=FECHADO`
    const { chamadas } = abrirFila('?estado=FECHADO', { [`GET ${url}`]: () => (falhas-- > 0 ? [500, { status: 500, detail: 'Falha.' }] : [200, pagina([item(4)])]) })
    const alerta = await screen.findByRole('alert')
    expect(alerta).toHaveTextContent('Não foi possível carregar a fila. Verifique sua conexão.')
    await userEvent.click(within(alerta).getByRole('button', { name: 'Tentar de novo' }))
    await screen.findByText('ESC-000004')
    expect(buscas(chamadas, url)).toBe(2)
  })

  it('mantem a lista enquanto carrega', async () => {
    let soltar: (v: [number, unknown]) => void = () => {}
    abrirFila('', { [`GET ${FILA}`]: () => [200, pagina([item(1)])], [`GET ${FILA}&q=xyz`]: () => new Promise((r) => (soltar = r)) })
    await screen.findByText('ESC-000001')
    expect(screen.getByRole('table')).not.toHaveAttribute('aria-busy')
    const busca = await campo()
    fireEvent.change(busca, { target: { value: 'xyz' } })
    fireEvent.submit(busca.closest('form')!)
    await waitFor(() => expect(screen.getByRole('table')).toHaveAttribute('aria-busy', 'true'))
    expect(screen.getByText('ESC-000001')).toBeInTheDocument()
    await act(async () => soltar([200, pagina([item(7)])]))
    await screen.findByText('ESC-000007')
    expect(screen.getByRole('table')).not.toHaveAttribute('aria-busy')
  })

  it('teclado busca na hora', async () => {
    const { chamadas } = abrirFila('', { [`GET ${FILA}`]: () => [200, pagina([item(1)])], [`GET ${FILA}&q=boleto`]: () => [200, pagina([item(2)])] })
    const busca = await campo()
    await screen.findByText('ESC-000001')
    expect(busca).toHaveAttribute('type', 'search')
    expect(busca).toHaveAttribute('enterkeyhint', 'search')
    await userEvent.type(busca, 'boleto{Enter}', { delay: null })
    await waitFor(() => expect(buscas(chamadas, `${FILA}&q=boleto`)).toBe(1), { timeout: 200 })
    expect(document.activeElement).not.toBe(busca)
  })
})
