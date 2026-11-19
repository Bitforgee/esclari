import { cleanup, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import QRCode from 'qrcode'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { abrir, api, escolher, type Rota } from '../test/util'

const operador = { id: 1, nome: 'Marina Souza', email: 'marina@esclari.com.br' }
const naoAutenticado = { status: 401, detail: 'Entre para continuar.' }
const auditoriaVazia = { itens: [], total: 0 }

const Uri = 'otpauth://totp/Esclari:marina@esclari.com.br?secret=JBSWY3DPEHPK3PXP&issuer=Esclari'

function ponteiro(toque: boolean) {
  vi.stubGlobal('matchMedia', (media: string) => ({ media, matches: toque && media === '(pointer: coarse)', onchange: null, addEventListener() {}, removeEventListener() {}, addListener() {}, removeListener() {}, dispatchEvent: () => false }))
}

async function ateACadastro(chamadas: { url: string }[] = []) {
  await userEvent.type(await screen.findByLabelText('E-mail'), 'marina@esclari.com.br')
  await userEvent.type(screen.getByLabelText('Senha'), 'uma senha longa')
  await userEvent.click(screen.getByRole('button', { name: 'Continuar' }))
  return chamadas
}

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

  it('entrada em tres etapas: sem sessão mostra e-mail e senha, sem a casca', async () => {
    tela()
    expect(await screen.findByLabelText('E-mail')).toBeInTheDocument()
    expect(screen.getByLabelText('Senha')).toHaveAttribute('type', 'password')
    expect(screen.getByRole('img', { name: 'Esclari' })).toHaveAttribute('src', '/marca/logo-horizontal.svg')
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument()
  })

  it('entrada em tres etapas: com 2FA já cadastrado pede os 6 dígitos e entra', async () => {
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

    expect(await screen.findByRole('navigation', { name: 'Navegação' })).toBeInTheDocument()
    expect(chamadas.filter((c) => c.metodo === 'POST').map((c) => [c.url, c.corpo])).toEqual([
      ['/api/plataforma/auth/entrar', { email: 'marina@esclari.com.br', senha: 'uma senha longa' }],
      ['/api/plataforma/auth/codigo', { codigo: '123456' }],
    ])
  })

  it('entrada em tres etapas: sem 2FA mostra a chave em grupos de 4 e o link para o aplicativo', async () => {
    const uri = Uri
    ponteiro(true)
    tela({ 'POST /api/plataforma/auth/entrar': () => [200, { etapa: 'CADASTRAR', segredo: 'JBSWY3DPEHPK3PXP', uri }] })
    await userEvent.type(await screen.findByLabelText('E-mail'), 'marina@esclari.com.br')
    await userEvent.type(screen.getByLabelText('Senha'), 'uma senha longa')
    await userEvent.click(screen.getByRole('button', { name: 'Continuar' }))

    expect(await screen.findByText('JBSW Y3DP EHPK 3PXP')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Abrir no aplicativo' })).toHaveAttribute('href', uri)
    expect(screen.getByLabelText('Código de 6 dígitos')).toBeInTheDocument()
  })

  it('qr code da uri e chave em grupos de 4', async () => {
    ponteiro(false)
    tela({ 'POST /api/plataforma/auth/entrar': () => [200, { etapa: 'CADASTRAR', segredo: 'JBSWY3DPEHPK3PXP', uri: Uri }] })
    await ateACadastro()

    const qr = await screen.findByRole('img', { name: 'QR code para o aplicativo autenticador' })
    const esperado = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(await QRCode.toString(Uri, { type: 'svg', errorCorrectionLevel: 'H' }))
    expect(qr).toHaveAttribute('src', esperado)
    expect(screen.getByText('JBSW Y3DP EHPK 3PXP')).toBeInTheDocument()
  })

  it('qr code sem chamada de rede', async () => {
    ponteiro(false)
    const chamadas = tela({ 'POST /api/plataforma/auth/entrar': () => [200, { etapa: 'CADASTRAR', segredo: 'JBSWY3DPEHPK3PXP', uri: Uri }] })
    await ateACadastro()
    await screen.findByRole('img', { name: 'QR code para o aplicativo autenticador' })
    expect(chamadas.length).toBeGreaterThan(0)
    expect(chamadas.filter((c) => !c.url.startsWith('/api/plataforma/'))).toEqual([])
  })

  it('link otpauth so em aparelho de toque', async () => {
    ponteiro(false)
    tela({ 'POST /api/plataforma/auth/entrar': () => [200, { etapa: 'CADASTRAR', segredo: 'JBSWY3DPEHPK3PXP', uri: Uri }] })
    await ateACadastro()
    await screen.findByRole('img', { name: 'QR code para o aplicativo autenticador' })
    expect(screen.queryByRole('link', { name: 'Abrir no aplicativo' })).not.toBeInTheDocument()
    cleanup()
    vi.unstubAllGlobals()

    ponteiro(true)
    tela({ 'POST /api/plataforma/auth/entrar': () => [200, { etapa: 'CADASTRAR', segredo: 'JBSWY3DPEHPK3PXP', uri: Uri }] })
    await ateACadastro()
    expect(await screen.findByRole('link', { name: 'Abrir no aplicativo' })).toHaveAttribute('href', Uri)
  })

  it('codigo sem qr code nem chave', async () => {
    tela({ 'POST /api/plataforma/auth/entrar': () => [200, { etapa: 'CODIGO', segredo: null, uri: null }] })
    await ateACadastro()
    expect(await screen.findByLabelText('Código de 6 dígitos')).toBeInTheDocument()
    expect(screen.queryByRole('img', { name: 'QR code para o aplicativo autenticador' })).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Chave do aplicativo')).not.toBeInTheDocument()
  })

  it('credenciais erradas mostram a mensagem e mantêm o e-mail', async () => {
    tela({ 'POST /api/plataforma/auth/entrar': () => [401, { status: 401, detail: 'E-mail ou senha incorretos.' }] })
    await userEvent.type(await screen.findByLabelText('E-mail'), 'marina@esclari.com.br')
    await userEvent.type(screen.getByLabelText('Senha'), 'errada')
    await userEvent.click(screen.getByRole('button', { name: 'Continuar' }))
    expect(await screen.findByText('E-mail ou senha incorretos.')).toBeInTheDocument()
    expect(screen.getByLabelText('E-mail')).toHaveValue('marina@esclari.com.br')
  })

  const paraOCodigo = async (rotas: Record<string, Rota> = {}) => {
    const chamadas = tela({ 'POST /api/plataforma/auth/entrar': () => [200, { etapa: 'CODIGO', segredo: null, uri: null }], ...rotas })
    await ateACadastro()
    return { chamadas, codigo: await screen.findByLabelText('Código de 6 dígitos') }
  }
  const codigosEnviados = (chamadas: { metodo: string; url: string; corpo: any }[]) => chamadas.filter((c) => c.url === '/api/plataforma/auth/codigo').map((c) => c.corpo)

  it('campo do codigo grande: um input mono, centralizado, 24px e com espaço entre os dígitos', async () => {
    const { codigo } = await paraOCodigo()
    expect(codigo.tagName).toBe('INPUT')
    expect(codigo).toHaveAttribute('inputmode', 'numeric')
    expect(codigo).toHaveAttribute('autocomplete', 'one-time-code')
    expect(codigo).toHaveAttribute('maxlength', '6')
    for (const classe of ['font-mono', 'text-center', 'text-2xl']) expect(codigo).toHaveClass(classe)
    expect(codigo.className).toMatch(/tracking-widest|tracking-\[0\.\d+em\]/)
    expect(document.querySelectorAll('input[name="codigo"]')).toHaveLength(1)
  })

  it('campo do codigo so aceita digitos: letras e símbolos caem e passa de 6 não entra', async () => {
    const { codigo } = await paraOCodigo({ 'POST /api/plataforma/auth/codigo': () => new Promise(() => {}) })
    await userEvent.type(codigo, '12a3')
    expect(codigo).toHaveValue('123')
    await userEvent.clear(codigo)
    await userEvent.click(codigo)
    await userEvent.paste('12-34 56789')
    expect(codigo).toHaveValue('123456')
  })

  it('envia sozinho ao completar 6 digitos, sem clicar em Entrar, uma vez só', async () => {
    const { chamadas, codigo } = await paraOCodigo()
    await userEvent.type(codigo, '123456')
    expect(await screen.findByRole('navigation', { name: 'Navegação' })).toBeInTheDocument()
    expect(codigosEnviados(chamadas)).toEqual([{ codigo: '123456' }])
  })

  it('nao envia de novo enquanto pendente: Enter e mais digitos nao geram segundo POST', async () => {
    let liberar: () => void = () => {}
    const espera = new Promise<void>((r) => (liberar = r))
    const { chamadas, codigo } = await paraOCodigo({
      'POST /api/plataforma/auth/codigo': async () => {
        await espera
        return [204, undefined]
      },
    })
    await userEvent.type(codigo, '123456')
    await userEvent.type(codigo, '7{Enter}{Enter}')
    expect(codigosEnviados(chamadas)).toEqual([{ codigo: '123456' }])
    liberar()
    await vi.waitFor(() => expect(screen.getByRole('button', { name: 'Entrar' })).toBeEnabled())
    expect(codigosEnviados(chamadas)).toEqual([{ codigo: '123456' }])
  })

  it('codigo errado limpa o campo e devolve o foco', async () => {
    const { codigo } = await paraOCodigo({ 'POST /api/plataforma/auth/codigo': () => [401, { status: 401, detail: 'Código inválido.' }] })
    await userEvent.type(codigo, '000000')
    const alerta = await screen.findByRole('alert')
    expect(alerta).toHaveTextContent('Código inválido.')
    expect(codigo).toHaveValue('')
    expect(codigo).toHaveFocus()
  })

  it('envia com menos de 6 digitos e mostra o erro da API: Enter e botao Entrar', async () => {
    const erro = { status: 400, detail: 'Informe os 6 dígitos.', errors: { codigo: ['Informe os 6 dígitos.'] } }
    const { chamadas, codigo } = await paraOCodigo({ 'POST /api/plataforma/auth/codigo': () => [400, erro] })
    await userEvent.type(codigo, '123{Enter}')
    expect(await screen.findByText('Informe os 6 dígitos.')).toBeInTheDocument()
    await userEvent.type(codigo, '45')
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))
    await vi.waitFor(() => expect(codigosEnviados(chamadas)).toEqual([{ codigo: '123' }, { codigo: '45' }]))
  })

  it('abre com o foco no codigo', async () => {
    const { codigo } = await paraOCodigo()
    expect(codigo).toHaveFocus()
  })

  it('qr code com o simbolo no centro e correcao H', async () => {
    ponteiro(false)
    tela({ 'POST /api/plataforma/auth/entrar': () => [200, { etapa: 'CADASTRAR', segredo: 'JBSWY3DPEHPK3PXP', uri: Uri }] })
    await ateACadastro()
    const qr = await screen.findByRole('img', { name: 'QR code para o aplicativo autenticador' })
    const simbolo = document.querySelector('img[src="/marca/simbolo-pequeno.svg"]')
    expect(simbolo).toBeInTheDocument()
    expect(simbolo).toHaveClass('bg-white')
    expect(qr.parentElement).toContainElement(simbolo as HTMLElement)
    expect(qr).toHaveAttribute('src', 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(await QRCode.toString(Uri, { type: 'svg', errorCorrectionLevel: 'H' })))
  })

  it('simbolo ocupa no maximo 20% do qr', async () => {
    ponteiro(false)
    tela({ 'POST /api/plataforma/auth/entrar': () => [200, { etapa: 'CADASTRAR', segredo: 'JBSWY3DPEHPK3PXP', uri: Uri }] })
    await ateACadastro()
    const qr = await screen.findByRole('img', { name: 'QR code para o aplicativo autenticador' })
    const simbolo = document.querySelector('img[src="/marca/simbolo-pequeno.svg"]') as HTMLElement
    const lado = Number(qr.getAttribute('width'))
    expect(lado).toBe(200)
    expect(Number(simbolo.getAttribute('width'))).toBeLessThanOrEqual(lado * 0.2)
    expect(Number(simbolo.getAttribute('height'))).toBeLessThanOrEqual(lado * 0.2)
  })
  it('401 leva a entrada com a frase de sessao encerrada', async () => {
    let lidas = 0
    const sessao = { id: 1, nome: 'Marina Souza', email: 'marina@esclari.com.br' }
    const item = { slug: 'demo', nome: 'Faculdade Demo', fuso: 'America/Cuiaba', ativa: true, criadaEm: '2026-11-02T12:00:00Z', administradores: 2, situacao: 'ATIVA', saude: { ultimaAtividadeEm: null, semBaseTaxa30Dias: null }, atencao: [] }
    api({
      'GET /api/plataforma/eu': () => [200, sessao],
      'GET /api/plataforma/instituicoes': () => (++lidas === 1 ? [200, [item]] : [401, { status: 401, detail: 'Sessão encerrada.' }]),
      'POST /api/plataforma/instituicoes/demo/reenviar-acesso': () => [401, { status: 401, detail: 'Sessão encerrada.' }],
    })
    abrir('/plataforma/instituicoes')
    await escolher('Faculdade Demo', 'Reenviar acesso do admin')
    expect(await screen.findByText('Sua sessão terminou. Entre de novo.')).toBeInTheDocument()
    expect(screen.getByLabelText('E-mail')).toBeInTheDocument()
    expect(screen.getByLabelText('Senha')).toBeInTheDocument()
  })

  it('plataforma nao e uma instituicao: /plataforma abre o console antes de qualquer /:slug', async () => {
    const chamadas = api({ 'GET /api/plataforma/eu': () => [401, naoAutenticado] })
    abrir('/plataforma')
    expect(await screen.findByLabelText('E-mail')).toBeInTheDocument()
    expect(screen.queryByText('Página não encontrada')).not.toBeInTheDocument()
    expect(chamadas.map((c) => c.url)).toEqual(['/api/plataforma/eu'])
  })
})

describe('criar a senha do operador', () => {
  afterEach(() => cleanup())

  it('definir senha da plataforma pelo link do convite e ir para a entrada', async () => {
    const chamadas = api({ 'POST /api/plataforma/auth/definir-senha': () => [204, undefined] })
    abrir('/plataforma/definir-senha?token=abc123')

    const nova = await screen.findByLabelText('Nova senha')
    expect(screen.queryByLabelText('E-mail')).not.toBeInTheDocument()
    await userEvent.type(nova, 'uma senha longa')
    await userEvent.type(screen.getByLabelText('Repita a senha'), 'uma senha diferente')
    expect(screen.getByText('As senhas não são iguais.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Criar senha' })).toBeDisabled()
    await userEvent.clear(screen.getByLabelText('Repita a senha'))
    await userEvent.type(screen.getByLabelText('Repita a senha'), 'uma senha longa')
    await userEvent.click(screen.getByRole('button', { name: 'Criar senha' }))

    expect(await screen.findByText('Senha criada. Agora entre no console e cadastre o aplicativo autenticador.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Entrar no console' })).toHaveAttribute('href', '/plataforma')
    expect(chamadas.filter((c) => c.metodo === 'POST').map((c) => [c.url, c.corpo])).toEqual([['/api/plataforma/auth/definir-senha', { token: 'abc123', senha: 'uma senha longa' }]])
  })

  it('link vencido mostra a mensagem da API', async () => {
    api({ 'POST /api/plataforma/auth/definir-senha': () => [400, { status: 400, detail: 'O link é inválido ou venceu. Peça um convite novo.', errors: { token: ['O link é inválido ou venceu. Peça um convite novo.'] } }] })
    abrir('/plataforma/definir-senha?token=velho')
    await userEvent.type(await screen.findByLabelText('Nova senha'), 'uma senha longa')
    await userEvent.type(screen.getByLabelText('Repita a senha'), 'uma senha longa')
    await userEvent.click(screen.getByRole('button', { name: 'Criar senha' }))
    expect(await screen.findByText('O link é inválido ou venceu. Peça um convite novo.')).toBeInTheDocument()
  })
})
