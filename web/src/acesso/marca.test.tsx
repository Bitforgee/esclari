import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { abrir, api, chamado, me } from '../test/util'

describe('marca no cabeçalho', () => {
  it('protocolo sem traducao: o cabeçalho com ESC-000123 leva translate no', async () => {
    const c = chamado({ numero: 'ESC-000123', solicitante: 'Ana', eventos: [], setores: [], categorias: [], acoes: [] })
    api({ 'GET /api/auth/me': () => [200, me({ papeis: ['ATENDENTE'], matriculas: [] })], 'GET /api/chamados/ESC-000123': () => [200, c] })
    abrir('/univag/chamados/ESC-000123')

    const protocolo = await screen.findAllByRole('heading', { name: 'ESC-000123', level: 1 })
    expect(protocolo[0]).toHaveAttribute('translate', 'no')
  })

  it('protocolo sem traducao: um título comum não leva translate', async () => {
    api({ 'GET /api/auth/me': () => [200, me({ papeis: ['ATENDENTE'], matriculas: [] })], })
    abrir('/univag/conta')

    const comum = await screen.findAllByRole('heading', { level: 1 })
    expect(comum.some((h) => h.hasAttribute('translate'))).toBe(false)
  })
})
