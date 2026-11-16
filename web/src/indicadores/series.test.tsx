import { fireEvent, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { abrir } from '../test/util'
import { prepararIndicadores, secaoDe } from '../test/indicadores'
import type { Detalhe, Ponto } from './tipos'

const pontos: Ponto[] = [
  { inicio: '2026-11-10', conversas: 2, chamadosAbertos: 3, chamadosResolvidos: 1 },
  { inicio: '2026-11-11', conversas: 12, chamadosAbertos: 1, chamadosResolvidos: 4 },
  { inicio: '2026-11-12', conversas: 5, chamadosAbertos: 2, chamadosResolvidos: 2 },
]
const abrirTela = (extra: Partial<Detalhe> = {}) => {
  prepararIndicadores({ pontos, granularidade: 'dia', ...extra })
  return abrir('/univag/indicadores')
}
const evolucao = async (extra: Partial<Detalhe> = {}) => (abrirTela(extra), secaoDe('Quanto chega e como evolui?'))
const figura = (s: Awaited<ReturnType<typeof evolucao>>, titulo: string) => within(s.getByText(titulo).closest('figure') as HTMLElement)

describe('séries', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('colunas de conversas', async () => {
    const s = await evolucao()
    const f = figura(s, 'Conversas por dia')

    expect(document.querySelectorAll('[data-coluna]')).toHaveLength(3)
    expect(f.getByText('Maior volume: 12 em 11/11')).toBeInTheDocument()
    expect(f.getByText('12', { selector: 'span' })).toBeInTheDocument()
    expect(f.getByText('0', { selector: 'span' })).toBeInTheDocument()
    const alturas = [...document.querySelectorAll('[data-coluna]')].map((c) => Number(c.getAttribute('height')))
    expect(alturas).toEqual([(2 / 12) * 100, 100, (5 / 12) * 100])
    expect(f.getByRole('img').getAttribute('aria-label')).toBe('Gráfico de colunas. Conversas por dia, de 10/11 a 12/11. Total 19. Pico de 12 em 11/11.')
  })

  it('colunas sem conversas lidas não aparecem', async () => {
    const s = await evolucao({ pontos: pontos.map((p) => ({ ...p, conversas: null })) })

    expect(s.queryByText('Conversas por dia')).not.toBeInTheDocument()
    expect(s.getByText('Chamados abertos e resolvidos por dia')).toBeInTheDocument()
  })

  it('duas linhas', async () => {
    const s = await evolucao()

    const [resolvidos, abertos] = [document.querySelector('[data-linha=resolvidos]')!, document.querySelector('[data-linha=abertos]')!]
    expect(resolvidos.getAttribute('stroke-dasharray')).toBeNull()
    expect(abertos.getAttribute('stroke-dasharray')).toBe('6 4')
    expect(s.getByText('Abertos · 2')).toBeInTheDocument()
    expect(s.getByText('Resolvidos · 2')).toBeInTheDocument()
  })

  it('duas linhas por semana', async () => {
    const s = await evolucao({ granularidade: 'semana' })

    expect(s.getByText('Chamados abertos e resolvidos por semana')).toBeInTheDocument()
    expect(s.getByText('Conversas por semana')).toBeInTheDocument()
  })

  it('ver como tabela', async () => {
    const s = await evolucao()
    const f = figura(s, 'Chamados abertos e resolvidos por dia')
    expect(f.queryByRole('table')).not.toBeInTheDocument()

    await userEvent.click(f.getByRole('button', { name: /Ver como tabela/ }))

    const tabela = f.getByRole('table')
    expect(within(tabela).getAllByRole('columnheader').map((h) => h.textContent)).toEqual(['Dia', 'Abertos', 'Resolvidos'])
    expect(within(tabela).getAllByRole('row').slice(1).map((l) => within(l).getAllByRole('cell').map((c) => c.textContent))).toEqual([
      ['10/11', '3', '1'],
      ['11/11', '1', '4'],
      ['12/11', '2', '2'],
    ])
    expect(f.getByRole('img').getAttribute('aria-label')).toContain('Total 6 abertos e 7 resolvidos')
  })

  it('valor do ponto', async () => {
    const s = await evolucao()
    const grupo = s.getByRole('group', { name: /Conversas por dia/ })

    expect(screen.queryByText(/^\d\d\/\d\d: /)).not.toBeInTheDocument()
    grupo.focus()
    expect(await screen.findByText('11/11: 12 conversas')).toBeInTheDocument()
    await userEvent.keyboard('{ArrowLeft}')
    expect(await screen.findByText('10/11: 2 conversas')).toBeInTheDocument()
    await userEvent.keyboard('{ArrowLeft}')
    expect(screen.getByText('10/11: 2 conversas')).toBeInTheDocument()
    await userEvent.keyboard('{ArrowRight}{ArrowRight}{ArrowRight}')
    expect(screen.getByText('12/11: 5 conversas')).toHaveClass('text-sm')
  })

  it('toque no gráfico escolhe o ponto pela posição', async () => {
    const s = await evolucao()
    const grupo = s.getByRole('group', { name: /Conversas por dia/ })
    grupo.getBoundingClientRect = () => ({ left: 100, width: 300, top: 0, right: 400, bottom: 100, height: 100, x: 100, y: 0, toJSON: () => ({}) })

    fireEvent.pointerDown(grupo, { clientX: 130 })
    expect(await screen.findByText('10/11: 2 conversas')).toBeInTheDocument()
    fireEvent.pointerDown(grupo, { clientX: 390 })
    expect(await screen.findByText('12/11: 5 conversas')).toBeInTheDocument()
  })
  it('valor do ponto nas duas linhas', async () => {
    const s = await evolucao({ granularidade: 'semana' })

    s.getByRole('group', { name: /Chamados abertos e resolvidos/ }).focus()

    expect(await screen.findByText('semana de 10/11: 3 abertos, 1 resolvido')).toBeInTheDocument()
  })
})
