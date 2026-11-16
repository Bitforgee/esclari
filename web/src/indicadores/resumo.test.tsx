import { screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { abrir, api, detalheVazio, me } from '../test/util'

const dados = {
  semDados: false, alunosAtivos: 12, conversas: 50, taxaAutoatendimento: 0.5, taxaEscalonamento: 0.2, taxaAbandono: 0.1, taxaPerguntasSemArtigo: 0.1,
  chamadosAbertos: 4, chamadosResolvidos: 20, tempoPrimeiraRespostaHoras: 1.5, tempoResolucaoHoras: 3, roteamentoCorreto: 0.5, taxaReabertura: 0.1,
  principaisCategorias: { porConversas: null, porChamados: null }, indicadorPrincipal: 0.712,
}

describe('resumo', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 15, 10, 0))
  })
  afterEach(() => {
    vi.useRealTimers()
  })

  it('resumo com denominador', async () => {
    api({
      'GET /api/auth/me': () => [200, me({ papeis: ['ADMINISTRADOR'], matriculas: [] })],
      'GET /api/indicadores?de=2026-11-01&ate=2026-11-15': () => [200, dados],
      'GET /api/indicadores?de=2026-10-17&ate=2026-10-31': () => [200, { ...dados, indicadorPrincipal: 0.67 }],
      'GET /api/indicadores/detalhe?de=2026-11-01&ate=2026-11-15': () => [200, { ...detalheVazio, desfechos: { resolvidas: 80, transferidas: 25, abandonadas: 10, encerradas: 0, semDesfecho: 5 } }],
    })
    abrir('/univag/indicadores')

    const resumo = within(await screen.findByRole('region', { name: 'Resumo' }))
    expect(await resumo.findByText('de 115 conversas com desfecho')).toBeInTheDocument()
    expect(resumo.getByText('71,2 %')).toBeInTheDocument()
    expect(await resumo.findByText('▲ 4,2 pts · melhor')).toBeInTheDocument()
    expect(resumo.getByText('Resolvidas sem reabertura, sobre as conversas com desfecho.')).toBeInTheDocument()
  })
})
