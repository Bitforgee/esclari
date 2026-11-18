import { cleanup, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { abrir, api, type Rota } from '../test/util'

const operador = { id: 1, nome: 'Marina Souza', email: 'marina@esclari.com.br' }
const naoAutenticado = { status: 401, detail: 'Entre para continuar.' }
const auditoriaVazia = { itens: [], total: 0 }

function tela(rotas: Record<string, Rota> = {}) {
  let entrou = false
  const chamadas = api({
    'GET /api/plataforma/eu': () => (entrou ? [200, operador] : [401, naoAutenticado]),
    'POST /api/plataforma/auth/codigo': () => {
      entrou = true
      return [204, undefined]
    },
    'GET /api/plataforma/auditoria?pagina=1': () => [200, auditoriaVazia],
    ...rotas,
  })
  abrir('/plataforma/auditoria')
  return chamadas
}

describe('entrada no console', () => {
  afterEach(() => cleanup())

  it('sem sessão mostra e-mail e senha, sem a casca', async () => {
    tela()
    expect(await screen.findByLabelText('E-mail')).toBeInTheDocument()
    expect(screen.getByLabelText('Senha')).toHaveAttribute('type', 'password')
    expect(screen.getByRole('img', { name: 'Esclari' })).toHaveAttribute('src', '/marca/logo-horizontal.svg')
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument()
  })

  it('com 2FA já cadastrado pede os 6 dígitos e entra', async () => {
    const chamadas = tela({ 'POST /api/plataforma/auth/entrar': () => [200, { etapa: 'CODIGO', segredo: null, uri: null }] })
    await userEvent.type(await screen.findByLabelText('E-mail'), 'marina@esclari.com.br')
    await userEvent.type(screen.getByLabelText('Senha'), 'uma senha longa')
    await userEvent.click(screen.getByRole('button', { name: 'Continuar' }))

    const codigo = await screen.findByLabelText('Código de 6 dígitos')
    expect(codigo).toHaveAttribute('inputmode', 'numeric')
    expect(codigo).toHaveAttribute('autocomplete', 'one-time-code')
    expect(screen.queryByRole('link', { name: 'Abrir no aplicativo' })).not.toBeInTheDocument()
    await userEvent.type(codigo, '12a3456')
    expect(codigo).toHaveValue('123456')
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('navigation', { name: 'Navegação' })).toBeInTheDocument()
    expect(chamadas.filter((c) => c.metodo === 'POST').map((c) => [c.url, c.corpo])).toEqual([
      ['/api/plataforma/auth/entrar', { email: 'marina@esclari.com.br', senha: 'uma senha longa' }],
      ['/api/plataforma/auth/codigo', { codigo: '123456' }],
    ])
  })

  it('sem 2FA mostra a chave em grupos de 4 e o link para o aplicativo', async () => {
    const uri = 'otpauth://totp/Esclari:marina@esclari.com.br?secret=JBSWY3DPEHPK3PXP&issuer=Esclari'
    tela({ 'POST /api/plataforma/auth/entrar': () => [200, { etapa: 'CADASTRAR', segredo: 'JBSWY3DPEHPK3PXP', uri }] })
    await userEvent.type(await screen.findByLabelText('E-mail'), 'marina@esclari.com.br')
    await userEvent.type(screen.getByLabelText('Senha'), 'uma senha longa')
    await userEvent.click(screen.getByRole('button', { name: 'Continuar' }))

    expect(await screen.findByText('JBSW Y3DP EHPK 3PXP')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Abrir no aplicativo' })).toHaveAttribute('href', uri)
    expect(screen.getByLabelText('Código de 6 dígitos')).toBeInTheDocument()
  })

  it('credenciais erradas mostram a mensagem e mantêm o e-mail', async () => {
    tela({ 'POST /api/plataforma/auth/entrar': () => [401, { status: 401, detail: 'E-mail ou senha incorretos.' }] })
    await userEvent.type(await screen.findByLabelText('E-mail'), 'marina@esclari.com.br')
    await userEvent.type(screen.getByLabelText('Senha'), 'errada')
    await userEvent.click(screen.getByRole('button', { name: 'Continuar' }))
    expect(await screen.findByText('E-mail ou senha incorretos.')).toBeInTheDocument()
    expect(screen.getByLabelText('E-mail')).toHaveValue('marina@esclari.com.br')
  })

  it('código errado fica na mesma etapa com a mensagem', async () => {
    tela({
      'POST /api/plataforma/auth/entrar': () => [200, { etapa: 'CODIGO', segredo: null, uri: null }],
      'POST /api/plataforma/auth/codigo': () => [401, { status: 401, detail: 'Código inválido.' }],
    })
    await userEvent.type(await screen.findByLabelText('E-mail'), 'marina@esclari.com.br')
    await userEvent.type(screen.getByLabelText('Senha'), 'uma senha longa')
    await userEvent.click(screen.getByRole('button', { name: 'Continuar' }))
    await userEvent.type(await screen.findByLabelText('Código de 6 dígitos'), '000000')
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))
    expect(await screen.findByText('Código inválido.')).toBeInTheDocument()
    expect(screen.getByLabelText('Código de 6 dígitos')).toHaveValue('000000')
  })
})
