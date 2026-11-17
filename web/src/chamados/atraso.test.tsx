import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { abrir, api, FILA, me, type Rota } from '../test/util'

const atendente = me({ papeis: ['ATENDENTE'], matriculas: [] })
const admin = me({ papeis: ['ADMINISTRADOR'], matriculas: [] })
const item = (n: number, atraso: string | null) => ({ numero: `ESC-${String(n).padStart(6, '0')}`, titulo: `pedido ${n}`, estado: 'ABERTO', setor: 'Triagem', categoria: 'Boleto', responsavel: null, criadoEm: '2026-11-02T12:00:00Z', dias: 1, atraso })
const pagina = (itens: unknown[]) => ({ itens, total: itens.length, pagina: 1, porPagina: 50 })

function linhaDe(numero: string) {
  return screen.getByText(numero).closest('tr')!
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('atraso nas listas', () => {
  it('etiqueta de atraso', async () => {
    api({ 'GET /api/auth/me': () => [200, atendente], [`GET ${FILA}`]: () => [200, pagina([item(1, 'PRIMEIRA_RESPOSTA'), item(2, 'RESOLUCAO'), item(3, null)])] })
    abrir('/univag/fila')

    await screen.findByText('ESC-000001')
    const primeira = within(linhaDe('ESC-000001')).getByText('Atrasado: 1ª resposta')
    const resolucao = within(linhaDe('ESC-000002')).getByText('Atrasado: resolução')
    for (const etiqueta of [primeira, resolucao]) expect(etiqueta.querySelector('svg')).toBeInTheDocument()
    expect(within(linhaDe('ESC-000003')).queryByText(/Atrasado/)).not.toBeInTheDocument()
    expect(linhaDe('ESC-000003').querySelector('[data-atraso]')).toBeNull()
  })

  it('filtro de atraso', async () => {
    const rotas: Record<string, Rota> = {
      'GET /api/auth/me': () => [200, atendente],
      [`GET ${FILA}`]: () => [200, pagina([item(1, 'PRIMEIRA_RESPOSTA'), item(3, null)])],
      [`GET ${FILA}&atraso=true`]: () => [200, pagina([item(1, 'PRIMEIRA_RESPOSTA')])],
    }
    const chamadas = api(rotas)
    const fila = abrir('/univag/fila')
    await screen.findByText('ESC-000003')
    expect(screen.getByRole('button', { name: 'Filtros' })).toBeInTheDocument()

    await userEvent.click(screen.getByRole('checkbox', { name: 'Só atrasados' }))

    expect(await screen.findByRole('button', { name: 'Filtros (1)' })).toBeInTheDocument()
    await screen.findByRole('button', { name: 'Remover filtro Atraso' })
    expect(screen.queryByText('ESC-000003')).not.toBeInTheDocument()
    expect(chamadas.some((c) => c.url === `${FILA}&atraso=true`)).toBe(true)
    expect(fila.roteador.state.location.search).toBe('?atraso=true')
    fila.unmount()

    const todos = api({ 'GET /api/auth/me': () => [200, admin], 'GET /api/chamados/busca?escopo=todos&atraso=true': () => [200, pagina([item(1, 'PRIMEIRA_RESPOSTA')])] })
    abrir('/univag/chamados?atraso=true')
    await screen.findByText('ESC-000001')
    expect(screen.getByRole('checkbox', { name: 'Só atrasados' })).toBeChecked()
    expect(screen.getByRole('button', { name: 'Filtros (1)' })).toBeInTheDocument()
    expect(todos.filter((c) => c.url.startsWith('/api/chamados/busca')).map((c) => c.url)).toEqual(['/api/chamados/busca?escopo=todos&atraso=true'])
  })
})
