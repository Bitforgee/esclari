import { cleanup, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { abrir, api } from '../test/util'

const instituicao = { 'GET /api/instituicoes/univag': () => [200, { nome: 'Univag' }] as [number, unknown] }

describe('máscara do CPF no acesso', () => {
  afterEach(() => cleanup())

  it('cpf do login e do primeiro acesso', async () => {
    const chamadas = api({ ...instituicao, 'POST /api/auth/login': () => [401, { status: 401, detail: 'CPF ou senha inválidos.' }] })
    let montada = abrir('/univag')
    const cpf = await screen.findByLabelText('CPF')
    expect(cpf).toHaveAttribute('inputmode', 'numeric')
    expect(cpf).toHaveAttribute('maxlength', '14')
    await userEvent.type(cpf, '04647333167')
    expect(cpf).toHaveValue('046.473.331-67')
    await userEvent.type(screen.getByLabelText('Senha'), 'senha-boa-123')
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))
    await screen.findByText('CPF ou senha inválidos.')
    expect(chamadas.filter((c) => c.metodo === 'POST').map((c) => c.corpo)).toEqual([{ instituicao: 'univag', cpf: '04647333167', senha: 'senha-boa-123' }])
    montada.unmount()

    const outras = api({ ...instituicao, 'POST /api/auth/primeiro-acesso': () => [200, { mensagem: 'Enviamos.' }] })
    montada = abrir('/univag/primeiro-acesso')
    const doPrimeiro = await screen.findByLabelText('CPF')
    expect(doPrimeiro).toHaveAttribute('inputmode', 'numeric')
    expect(doPrimeiro).toHaveAttribute('maxlength', '14')
    await userEvent.type(doPrimeiro, '0464733316a7')
    expect(doPrimeiro).toHaveValue('046.473.331-67')
    await userEvent.click(screen.getByRole('button', { name: 'Enviar link' }))
    await waitFor(() => expect(outras.filter((c) => c.metodo === 'POST').map((c) => c.corpo)).toEqual([{ instituicao: 'univag', cpf: '04647333167' }]))
    montada.unmount()
  })
})
