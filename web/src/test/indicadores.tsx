import { screen, within } from '@testing-library/react'
import { expect, vi } from 'vitest'
import { api, detalheVazio, me } from './util'

export const principalBase = {
  semDados: false, alunosAtivos: 12, conversas: 50, taxaAutoatendimento: 0.5, taxaEscalonamento: 0.2, taxaAbandono: 0.1, taxaPerguntasSemArtigo: 0.1,
  chamadosAbertos: 4, chamadosResolvidos: 20, tempoPrimeiraRespostaHoras: 1.5, tempoResolucaoHoras: 3, roteamentoCorreto: 0.5, taxaReabertura: 0.1,
  principaisCategorias: { porConversas: null, porChamados: null }, indicadorPrincipal: 0.5,
}

export function prepararIndicadores(detalhe: object = {}, principal: object = {}) {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(new Date(2026, 10, 15, 10, 0))
  api({
    'GET /api/auth/me': () => [200, me({ papeis: ['ADMINISTRADOR'], matriculas: [] })],
    'GET /api/indicadores?de=2026-11-01&ate=2026-11-15': () => [200, { ...principalBase, ...principal }],
    'GET /api/indicadores/detalhe?de=2026-11-01&ate=2026-11-15': () => [200, { ...detalheVazio, ...detalhe }],
  })
}

export async function secaoDe(nome: string) {
  const regiao = await screen.findByRole('region', { name: nome })
  await vi.waitFor(() => expect(within(regiao).queryByText('Carregando…')).not.toBeInTheDocument())
  return within(regiao)
}
