import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { abrir, api, chamado, me } from '../test/util'

describe('prazo do aluno', () => {
  it('previsao no fuso da instituicao', async () => {
    api({
      'GET /api/auth/me': () => [200, me({ instituicao: { slug: 'univag', nome: 'Univag', fuso: 'America/Cuiaba' } })],
      'GET /api/chamados/ESC-000001': () => [200, chamado({ estado: 'ABERTO', prazoPrimeiraRespostaEm: '2026-11-18T12:00:00Z' })],
    })
    abrir('/univag/chamados/ESC-000001')

    const previsao = await screen.findByText('Resposta prevista até 18/11 às 08:00')
    expect(previsao).toHaveAttribute('data-linha', 'previsao')
    expect(previsao.previousElementSibling).toContainElement(screen.getAllByText('Aberto')[0])
  })

  it('sem previsao', async () => {
    api({
      'GET /api/auth/me': () => [200, me()],
      'GET /api/chamados/ESC-000001': () => [200, chamado({ estado: 'EM_ATENDIMENTO', prazoPrimeiraRespostaEm: null })],
    })
    abrir('/univag/chamados/ESC-000001')

    await screen.findByRole('heading', { name: 'preciso do boleto' })
    expect(screen.queryByText(/Resposta prevista/)).not.toBeInTheDocument()
  })
})
