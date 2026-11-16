import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { abrir, api, chamado, me } from '../test/util'

describe('marca nos chamados', () => {
  it('placeholder do original usa o protocolo ESC-000123', async () => {
    const c = chamado({ numero: 'ESC-000123', solicitante: 'Ana', eventos: [], setores: [], categorias: [], acoes: ['duplicado'] })
    api({ 'GET /api/auth/me': () => [200, me({ papeis: ['ATENDENTE'], matriculas: [] })], 'GET /api/chamados/ESC-000123': () => [200, c] })
    abrir('/univag/chamados/ESC-000123')

    expect(await screen.findByPlaceholderText('Ex.: ESC-000123…')).toBeInTheDocument()
  })
})
