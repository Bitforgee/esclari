import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { abrir, api, instituicao, me } from '../../test/util'

const admin = me({ papeis: ['ADMINISTRADOR'], matriculas: [] })
const { nome: _, ...aparencia } = instituicao()

describe('máscara do telefone na aparência', () => {
  it('telefone do contato', async () => {
    let enviado: any
    api({
      'GET /api/auth/me': () => [200, admin],
      'GET /api/admin/aparencia': () => [200, aparencia],
      'GET /api/instituicoes/univag': () => [200, instituicao()],
      'PUT /api/admin/aparencia': (corpo) => {
        enviado = corpo
        return [200, aparencia]
      },
    })
    abrir('/univag/aparencia')
    await userEvent.click(await screen.findByRole('tab', { name: /^Contato/ }))
    const telefone = await screen.findByLabelText('Telefone')
    await userEvent.type(telefone, '65999991234')
    expect(telefone).toHaveValue('(65) 99999-1234')
    await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))
    await waitFor(() => expect(enviado).toBeDefined())
    expect(enviado.contato.telefone).toBe('(65) 99999-1234')
  })
})
