import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { abrir, api, chamado, me } from '../test/util'

const atendente = me({ papeis: ['ATENDENTE'], matriculas: [] })

describe('máscara do protocolo', () => {
  it('protocolo do duplicado', async () => {
    const c = chamado({ solicitante: 'Ana', eventos: [], setores: [], categorias: [], acoes: ['duplicado'] })
    api({ 'GET /api/auth/me': () => [200, atendente], 'GET /api/chamados/ESC-000001': () => [200, c] })
    abrir('/univag/chamados/ESC-000001')
    const original = await screen.findByLabelText('Duplicado de outro pedido do mesmo aluno')
    await userEvent.type(original, 'esc12')
    expect(original).toHaveValue('ESC-12')
    await userEvent.type(original, '34567')
    expect(original).toHaveValue('ESC-123456')
  })
})
