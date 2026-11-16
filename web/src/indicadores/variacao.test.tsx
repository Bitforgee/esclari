import { screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { abrir, api, me } from '../test/util'
import type { Indicadores } from './tipos'
import { MinimoDeCasos, Taxas, textoDaVariacao, variacaoDaTaxa } from './Variacao'

const base: Indicadores = {
  semDados: false, alunosAtivos: 12, conversas: 50, taxaAutoatendimento: 0.5, taxaEscalonamento: 0.2, taxaAbandono: 0.1, taxaPerguntasSemArtigo: 0.1,
  chamadosAbertos: 4, chamadosResolvidos: 20, tempoPrimeiraRespostaHoras: 1.5, tempoResolucaoHoras: 3, roteamentoCorreto: 0.5, taxaReabertura: 0.1,
  principaisCategorias: { porConversas: null, porChamados: null }, indicadorPrincipal: 0.5,
}
const principal = Taxas[0]
const principalNoPeriodo = (extra: Partial<Indicadores>) => ({ ...base, ...extra })

describe('variação', () => {
  it('direcao de cada taxa', () => {
    const sobe = Taxas.map((t) => [t.rotulo, textoDaVariacao(0.6, 0.5, t.melhorSe)])
    const desce = Taxas.map((t) => [t.rotulo, textoDaVariacao(0.5, 0.6, t.melhorSe)])

    expect(sobe).toEqual([
      ['Indicador principal', '▲ 10,0 pts · melhor'],
      ['Autoatendimento', '▲ 10,0 pts · melhor'],
      ['Escalonamento', '▲ 10,0 pts · pior'],
      ['Abandono', '▲ 10,0 pts · pior'],
      ['Perguntas sem artigo', '▲ 10,0 pts · pior'],
      ['Roteamento correto', '▲ 10,0 pts · melhor'],
      ['Reabertura', '▲ 10,0 pts · pior'],
    ])
    expect(desce.map(([, t]) => t.replace('melhor', 'X').replace('pior', 'melhor').replace('X', 'pior'))).toEqual(sobe.map(([, t]) => t.replace('▲', '▼')))
  })

  it('omite a variacao', () => {
    const atual = principalNoPeriodo({ indicadorPrincipal: 0.6 })
    const casos: [string, Indicadores | undefined, boolean][] = [
      ['sem período anterior', undefined, false],
      ['período anterior sem dados', principalNoPeriodo({ semDados: true }), false],
      ['taxa nula no anterior', principalNoPeriodo({ indicadorPrincipal: null }), false],
      ['9 casos', principalNoPeriodo({ conversas: MinimoDeCasos - 1 }), false],
      ['10 casos', principalNoPeriodo({ conversas: MinimoDeCasos }), true],
    ]
    for (const [nome, anterior, mostra] of casos) expect(variacaoDaTaxa(principal, atual, anterior) !== undefined, nome).toBe(mostra)
    expect(variacaoDaTaxa(principal, principalNoPeriodo({ indicadorPrincipal: null }), base)).toBeUndefined()
    expect(variacaoDaTaxa(principal, principalNoPeriodo({ semDados: true }), base)).toBeUndefined()
    expect(variacaoDaTaxa(Taxas[5], atual, principalNoPeriodo({ chamadosResolvidos: 9 }))).toBeUndefined()
    expect(variacaoDaTaxa(Taxas[5], atual, principalNoPeriodo({ chamadosResolvidos: 10, roteamentoCorreto: 0.4 }))).toBe('▲ 10,0 pts · melhor')
  })

  describe('na tela', () => {
    const atual = { ...base, indicadorPrincipal: 0.7, taxaAutoatendimento: 0.6, taxaEscalonamento: 0.2, taxaAbandono: 0.1, taxaPerguntasSemArtigo: 0.15, roteamentoCorreto: 0.8, taxaReabertura: 0.1 }
    const anterior = { ...base, indicadorPrincipal: 0.6, taxaAutoatendimento: 0.5, taxaEscalonamento: 0.3, taxaAbandono: 0.1, taxaPerguntasSemArtigo: 0.1, roteamentoCorreto: 0.7, taxaReabertura: 0.2 }

    beforeEach(() => {
      vi.useFakeTimers({ toFake: ['Date'] })
      vi.setSystemTime(new Date(2026, 10, 15, 10, 0))
    })
    afterEach(() => {
      vi.useRealTimers()
    })

    async function abrirTela() {
      api({
        'GET /api/auth/me': () => [200, me({ papeis: ['ADMINISTRADOR'], matriculas: [] })],
        'GET /api/indicadores?de=2026-11-01&ate=2026-11-15': () => [200, atual],
        'GET /api/indicadores?de=2026-10-17&ate=2026-10-31': () => [200, anterior],
      })
      abrir('/univag/indicadores')
      await screen.findByText('Indicador principal')
      await screen.findAllByText('vs. período anterior')
    }

    const variacaoDe = (rotulo: string) => {
      const dd = screen.getByText(rotulo, { selector: 'dt' }).nextElementSibling!
      return within(dd as HTMLElement).queryByText('vs. período anterior')?.parentElement?.firstChild?.textContent?.trim()
    }

    it('variacao nas sete taxas', async () => {
      await abrirTela()

      expect(
        Taxas.map((t) => [t.rotulo, variacaoDe(t.rotulo)]),
      ).toEqual([
        ['Indicador principal', '▲ 10,0 pts · melhor'],
        ['Autoatendimento', '▲ 10,0 pts · melhor'],
        ['Escalonamento', '▼ 10,0 pts · melhor'],
        ['Abandono', 'sem variação'],
        ['Perguntas sem artigo', '▲ 5,0 pts · pior'],
        ['Roteamento correto', '▲ 10,0 pts · melhor'],
        ['Reabertura', '▼ 10,0 pts · melhor'],
      ])
    })

    it('nunca so por cor', async () => {
      await abrirTela()

      const textos = [...document.querySelectorAll('main dd span.tabular-nums')].filter((s) => s.textContent!.includes('vs. período anterior')).map((s) => s.firstChild!.textContent!.trim())
      expect(textos).toHaveLength(7)
      for (const t of textos) expect(t).toMatch(/^(▲|▼) [\d,]+ pts? · (melhor|pior)$|^sem variação$/)
    })
  })
})
