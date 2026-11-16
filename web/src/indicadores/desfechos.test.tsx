import { screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { abrir } from '../test/util'
import { prepararIndicadores, secaoDe } from '../test/indicadores'
import { detalheVazio } from '../test/util'
import type { Desfechos } from './tipos'

const desfechos = (extra: Partial<Desfechos> = {}): Desfechos => ({ resolvidas: 80, transferidas: 25, abandonadas: 10, encerradas: 0, semDesfecho: 5, ...extra })
const abrirTela = (d: Desfechos | null) => {
  prepararIndicadores({ desfechos: d })
  return abrir('/univag/indicadores')
}
const secao = async (d: Desfechos | null) => (abrirTela(d), secaoDe('O assistente está resolvendo?'))

describe('desfechos do assistente', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('barra com rotulo direto', async () => {
    const s = await secao(desfechos())

    const itens = s.getAllByRole('listitem').map((i) => i.textContent)
    expect(itens).toEqual([
      'Resolvida pelo assistente · 80 · 69,6\u00a0%',
      'Transferida a um atendente · 25 · 21,7\u00a0%',
      'Abandonada · 10 · 8,7\u00a0%',
      'Encerrada pelo aluno · 0 · 0\u00a0%',
    ])
    expect(s.getByRole('img').getAttribute('aria-label')).toContain('Resolvida pelo assistente: 80')
  })

  it('denominador', async () => {
    const { unmount } = abrirTela(desfechos())
    const s = await screen.findByText('de 115 conversas com desfecho · 5 ainda sem desfecho')
    expect(s).toBeInTheDocument()
    const larguras = [...document.querySelectorAll<HTMLElement>('[data-segmento]')].map((x) => parseFloat(x.style.width))
    expect(larguras).toHaveLength(3)
    expect(larguras.reduce((a, b) => a + b, 0)).toBeCloseTo(100, 5)
    unmount()

    const sem = await secao(desfechos({ semDesfecho: 0 }))
    expect(sem.getByText('de 115 conversas com desfecho')).toBeInTheDocument()
    expect(sem.queryByText(/ainda sem desfecho/)).not.toBeInTheDocument()
  })

  it('amostra pequena', async () => {
    const { unmount } = abrirTela(desfechos({ resolvidas: 5, transferidas: 2, abandonadas: 2, encerradas: 0 }))
    expect(await screen.findByText('Amostra pequena: menos de 10 conversas')).toBeInTheDocument()
    unmount()

    const s = await secao(desfechos({ resolvidas: 5, transferidas: 2, abandonadas: 2, encerradas: 1 }))
    expect(s.queryByText('Amostra pequena: menos de 10 conversas')).not.toBeInTheDocument()
  })

  it('sem filtro de setor', async () => {
    const s = await secao(null)

    expect(s.getByText('Desfechos do assistente aparecem na visão geral, sem filtro de setor.')).toBeInTheDocument()
    expect(s.queryByRole('img')).not.toBeInTheDocument()
  })

  it('separador de 2 px', async () => {
    const s = await secao(desfechos())

    const barra = s.getByRole('img')
    expect(barra).toHaveClass('gap-0.5', 'bg-background')
    expect([...barra.children].map((x) => (x as HTMLElement).style.background)).toEqual(['var(--grafico-1)', 'var(--grafico-2)', 'var(--grafico-3)'])
    expect(detalheVazio.desfechos.semDesfecho).toBe(0)
  })
})
