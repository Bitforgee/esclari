import { screen, within } from '@testing-library/react'
import { onlineManager } from '@tanstack/react-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { abrir, api, cabecalhos, esperaLinhaVazia, FILA, me, rotaDaLista, type Rota } from '../test/util'

const atendente = me({ papeis: ['ALUNO', 'ATENDENTE'] })

function lista(escopo: string, rota: Rota) {
  api({ 'GET /api/auth/me': () => [200, atendente], ...rotaDaLista(escopo, rota) })
}

describe('listas de chamados', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('Fila mostra cada chamado com categoria, responsável e dias', async () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date('2026-11-05T15:00:00Z'))
    lista('fila', () => [
      200,
      [
        { numero: 'ESC-000001', titulo: 'boleto', categoria: 'Boleto', estado: 'ABERTO', responsavel: null, criadoEm: '2026-11-02T12:00:00Z', dias: 1 },
        { numero: 'ESC-000002', titulo: 'matrícula', categoria: 'Matrícula', estado: 'EM_ATENDIMENTO', responsavel: 'Carla', criadoEm: '2026-10-30T12:00:00Z', dias: 3 },
      ],
    ])
    abrir('/univag/fila')
    const primeiro = (await screen.findByText('ESC-000001')).closest('tr') as HTMLElement
    expect(within(primeiro).getByText('ESC-000001').closest('a')).toHaveAttribute('href', '/univag/chamados/ESC-000001')
    expect(within(primeiro).getByText('Aberto')).toBeInTheDocument()
    expect(primeiro).toHaveTextContent('Boleto · sem responsável · aberto há 3 dias')
    const segundo = screen.getByText('ESC-000002').closest('tr') as HTMLElement
    expect(within(segundo).getByText('Em atendimento')).toBeInTheDocument()
    expect(segundo).toHaveTextContent('Matrícula · Carla · aberto há 6 dias')
    expect(screen.queryByText('Nenhum chamado na fila dos seus setores.')).not.toBeInTheDocument()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('Minhas solicitações lista os chamados do aluno sem a linha da equipe', async () => {
    lista('meus', () => [200, [{ numero: 'ESC-000003', titulo: 'histórico', estado: 'RESOLVIDO', criadoEm: '2026-11-02T12:00:00Z' }]])
    abrir('/univag/solicitacoes')
    const item = (await screen.findByText('ESC-000003')).closest('tr') as HTMLElement
    expect(within(item).getByText('ESC-000003').closest('a')).toHaveAttribute('href', '/univag/chamados/ESC-000003')
    expect(item).toHaveTextContent('ESC-000003históricoResolvido')
    expect(screen.queryByText(/Você ainda não tem solicitações/)).not.toBeInTheDocument()
  })

  it('colunas das listas de chamados', async () => {
    lista('fila', () => [200, [{ numero: 'ESC-000001', titulo: 'boleto', categoria: 'Boleto', estado: 'ABERTO', responsavel: null, criadoEm: '2026-11-02T12:00:00Z', dias: 1 }]])
    const montada = abrir('/univag/fila')
    const linha = (await screen.findByText('ESC-000001')).closest('tr') as HTMLElement
    expect(cabecalhos()).toEqual(['Número', 'Título', 'Categoria', 'Responsável', 'Aberto', 'Estado'])
    const celulas = within(linha).getAllByRole('cell')
    expect(celulas[0]).toHaveTextContent(/^ESC-000001$/)
    expect(celulas[1]).toHaveTextContent(/^boleto.*aberto há \d+ dias$/)
    montada.unmount()

    lista('meus', () => [200, [{ numero: 'ESC-000003', titulo: 'histórico', estado: 'RESOLVIDO', criadoEm: '2026-11-02T12:00:00Z' }]])
    abrir('/univag/solicitacoes')
    const item = (await screen.findByText('ESC-000003')).closest('tr') as HTMLElement
    expect(cabecalhos()).toEqual(['Número', 'Título', 'Estado'])
    expect(within(item).getAllByRole('cell')[0]).toHaveTextContent(/^ESC-000003$/)
    expect(within(item).getAllByRole('cell')[1]).toHaveTextContent(/^histórico$/)
  })

  it('lista vazia numa linha', async () => {
    lista('fila', () => [200, []])
    const montada = abrir('/univag/fila')
    await screen.findByText('Nenhum chamado na fila dos seus setores.')
    esperaLinhaVazia('Nenhum chamado na fila dos seus setores.', 6)
    montada.unmount()

    lista('meus', () => [200, []])
    abrir('/univag/solicitacoes')
    await screen.findByText(/Você ainda não tem solicitações/)
    esperaLinhaVazia('Você ainda não tem solicitações.', 3)
  })

  it('lista carregando mostra 3 esqueletos e nada mais', async () => {
    lista('fila', () => new Promise(() => {}))
    abrir('/univag/fila')
    expect(await screen.findByRole('heading', { name: 'Fila' })).toBeInTheDocument()
    expect(document.querySelectorAll('[data-slot="skeleton"]')).toHaveLength(3)
    expect(screen.getByRole('status')).toHaveTextContent('Carregando…')
    expect(screen.queryByText('Nenhum chamado na fila dos seus setores.')).not.toBeInTheDocument()
  })

  it('lista com erro mostra o detail sem o Spinner', async () => {
    lista('meus', () => [500, { status: 500, detail: 'Falha ao listar.' }])
    abrir('/univag/solicitacoes')
    expect(await screen.findByRole('alert')).toHaveTextContent('Não foi possível carregar suas solicitações. Verifique sua conexão.')
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
    expect(screen.queryByText(/Você ainda não tem solicitações/)).not.toBeInTheDocument()
  })

  it('sem rede a lista mostra o erro', async () => {
    const fetch = vi.fn(async (url: string) => {
      if (url === '/api/auth/me') return new Response(JSON.stringify(atendente), { headers: { 'Content-Type': 'application/json' } })
      throw new TypeError('Failed to fetch')
    })
    vi.stubGlobal('fetch', fetch)
    onlineManager.setOnline(false)
    try {
      abrir('/univag/fila')
      expect(await screen.findByRole('alert')).toHaveTextContent('Não foi possível carregar a fila. Verifique sua conexão.')
      expect(screen.queryByRole('status', { name: 'Carregando' })).not.toBeInTheDocument()
      expect(fetch.mock.calls.filter(([url]) => url === FILA)).toHaveLength(1)
    } finally {
      onlineManager.setOnline(true)
    }
  })
})
