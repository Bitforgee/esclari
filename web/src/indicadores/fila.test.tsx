import { screen } from '@testing-library/react'
import { within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { abrir } from '../test/util'
import { prepararIndicadores, secaoDe } from '../test/indicadores'
import type { Detalhe } from './tipos'

const abrirTela = (extra: Partial<Detalhe> = {}) => {
  prepararIndicadores(extra)
  return abrir('/univag/indicadores')
}
const fila = async (extra: Partial<Detalhe> = {}) => (abrirTela(extra), secaoDe('A equipe responde a tempo?'))
const Zerada = { ABERTO: 0, EM_ATENDIMENTO: 0, AGUARDANDO_ALUNO: 0, RESOLVIDO: 0 }

describe('fila e tempos', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('mediana e p90', async () => {
    const s = await fila({ tempos: { primeiraRespostaMedianaHoras: 2.5, primeiraRespostaP90Horas: 10, resolucaoMedianaHoras: 20, resolucaoP90Horas: 70 } })

    const dd = (rotulo: string) => [...s.getByText(rotulo, { selector: 'dt' }).parentElement!.querySelectorAll('dd')].map((d) => d.textContent)
    expect(dd('Tempo até a 1ª resposta')).toEqual(['mediana 2,5\u00a0h', '9 em 10 foram atendidos em até 10\u00a0h'])
    expect(dd('Tempo de resolução')).toEqual(['mediana 20\u00a0h', '9 em 10 foram resolvidos em até 70\u00a0h'])
    expect(s.queryByText(/média/i)).not.toBeInTheDocument()
  })

  it('tempos sem chamados mostram traço e nenhuma frase de p90', async () => {
    const s = await fila()

    const tempos = within(s.getByText('Tempo até a 1ª resposta', { selector: 'dt' }).closest('dl')!)
    expect(tempos.getAllByText('—')).toHaveLength(2)
    expect(s.queryByText(/9 em 10/)).not.toBeInTheDocument()
  })

  it('fila por estado', async () => {
    const s = await fila({ chamadosPorEstado: { ABERTO: 3, EM_ATENDIMENTO: 6, AGUARDANDO_ALUNO: 2, RESOLVIDO: 4 }, maisAntigoDias: 9 })

    const barra = s.getByRole('img', { name: /^Barra empilhada/ })
    expect(within(barra.parentElement!).getAllByRole('listitem').map((i) => i.textContent)).toEqual([
      'Aberto · 3 · 20\u00a0%',
      'Em atendimento · 6 · 40\u00a0%',
      'Aguardando aluno · 2 · 13,3\u00a0%',
      'Resolvido · 4 · 26,7\u00a0%',
    ])
    expect(s.getByText('Chamados abertos agora: 15')).toBeInTheDocument()
    expect(s.getByText('O mais antigo espera há 9 dias')).toBeInTheDocument()
    expect(barra.getAttribute('aria-label')).toContain('Em atendimento: 6. Aguardando aluno: 2. Resolvido: 4. Total 15.')
    await userEvent.click(s.getByRole('button', { name: 'Ver como tabela: Chamados abertos por estado' }))
    expect(s.getAllByRole('columnheader').map((h) => h.textContent)).toEqual(['Item', 'Quantidade', 'Percentual'])
    expect(s.getAllByRole('row').slice(1).map((l) => within(l).getAllByRole('cell').map((c) => c.textContent))).toEqual([
      ['Aberto', '3', '20\u00a0%'],
      ['Em atendimento', '6', '40\u00a0%'],
      ['Aguardando aluno', '2', '13,3\u00a0%'],
      ['Resolvido', '4', '26,7\u00a0%'],
    ])
  })

  it('o mais antigo no singular e sem ele', async () => {
    const { unmount } = abrirTela({ chamadosPorEstado: { ...Zerada, ABERTO: 1 }, maisAntigoDias: 1 })
    expect(await screen.findByText('O mais antigo espera há 1 dia')).toBeInTheDocument()
    unmount()

    const s = await fila({ chamadosPorEstado: { ...Zerada, AGUARDANDO_ALUNO: 1 }, maisAntigoDias: null })
    expect(s.queryByText(/O mais antigo/)).not.toBeInTheDocument()
  })

  it('nenhum chamado aberto', async () => {
    const s = await fila()

    expect(s.getByText('Nenhum chamado aberto agora.')).toBeInTheDocument()
    expect(s.queryByRole('img', { name: /^Barra empilhada/ })).not.toBeInTheDocument()
  })
})
