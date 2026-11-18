import { expect, test, type APIRequestContext } from '@playwright/test'
import { execFileSync } from 'node:child_process'
import { createHmac } from 'node:crypto'

const container = process.env.E2E_CONTAINER ?? 'esclari-api-1'
const mailpit = process.env.E2E_MAILPIT ?? 'http://localhost:8025'
const sufixo = Date.now().toString(36)
const daDemo = `demo-${sufixo}`
const email = `operadora-${sufixo}@e2e.esclari`
const senha = 'operador-e2e-plataforma-1'
const Alfabeto = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'

function cpfValido(semente: number) {
  const d = String(semente).padStart(9, '0').slice(-9).split('').map(Number)
  if (d.every((x) => x === d[0])) d[8] = (d[8] + 1) % 10
  for (const n of [9, 10]) {
    const soma = d.reduce((s, x, i) => s + x * (n + 1 - i), 0)
    d.push(((soma * 10) % 11) % 10)
  }
  return d.join('')
}

function codigoDe(segredo: string, passo: number) {
  let bits = 0
  let valor = 0
  const bytes: number[] = []
  for (const c of segredo.replace(/\s/g, '')) {
    valor = (valor << 5) | Alfabeto.indexOf(c)
    bits += 5
    if (bits >= 8) {
      bytes.push((valor >>> (bits - 8)) & 255)
      bits -= 8
    }
  }
  const contador = Buffer.alloc(8)
  contador.writeBigUInt64BE(BigInt(passo))
  const h = createHmac('sha1', Buffer.from(bytes)).update(contador).digest()
  const o = h[h.length - 1] & 15
  const binario = ((h[o] & 127) << 24) | (h[o + 1] << 16) | (h[o + 2] << 8) | h[o + 3]
  return String(binario % 1_000_000).padStart(6, '0')
}

const cli = (...args: string[]) => execFileSync('docker', ['exec', container, 'dotnet', 'Esclari.Api.dll', ...args], { encoding: 'utf8' })

test.describe.configure({ timeout: 240_000 })
async function linkDoEmail(request: APIRequestContext, para: string) {
  for (let i = 0; i < 200; i++) {
    const lista = await (await request.get(`${mailpit}/api/v1/search`, { params: { query: `to:${para}` } })).json()
    for (const m of lista.messages ?? []) {
      const corpo = (await (await request.get(`${mailpit}/api/v1/message/${m.ID}`)).json()).Text as string
      const achado = corpo.match(/(https?:\/\/\S+\/definir-senha\?token=\S+)/)
      if (achado) return achado[1]
    }
    await new Promise((r) => setTimeout(r, 500))
  }
  throw new Error(`nenhum e-mail de primeiro acesso para ${para}`)
}

test('operador da plataforma entra com 2FA, cria e desativa uma instituição', async ({ browser, playwright, baseURL }) => {
  const convite = cli('plataforma', 'operador', '--email', email, '--nome', 'Operadora E2E')
  const token = convite.match(/definir-senha\?token=(\S+)/)![1]
  const api = await playwright.request.newContext({ baseURL })
  expect((await api.post('/api/plataforma/auth/definir-senha', { data: { token, senha } })).ok()).toBeTruthy()

  const equipe = await browser.newContext({ baseURL })
  const painel = await equipe.newPage()
  await painel.goto('/plataforma')
  await painel.getByLabel('E-mail').fill(email)
  await painel.getByLabel('Senha').fill(senha)
  await painel.getByRole('button', { name: 'Continuar' }).click()
  const chave = (await painel.getByLabel('Chave do aplicativo').textContent())!
  await painel.getByLabel('Código de 6 dígitos').fill(codigoDe(chave, Math.floor(Date.now() / 30_000)))
  await painel.getByRole('button', { name: 'Entrar' }).click()

  await expect(painel.getByRole('navigation', { name: 'Navegação' }).getByRole('link', { name: 'Instituições' })).toBeVisible()
  await painel.getByRole('button', { name: 'Nova instituição' }).click()
  const dialogo = painel.getByRole('dialog')
  const emailDaDemo = `admin-${sufixo}@demo.e2e.esclari`
  await dialogo.getByLabel('Nome').first().fill('Faculdade Demo')
  await dialogo.getByLabel('Endereço').fill(daDemo)
  const admin = dialogo.getByRole('group', { name: 'Primeiro administrador' })
  await admin.getByLabel('Nome').fill('Dona Admin')
  await admin.getByLabel('CPF').fill(cpfValido(Date.now() % 1_000_000_000 + 7))
  await admin.getByLabel('E-mail').fill(emailDaDemo)
  await dialogo.getByRole('button', { name: 'Criar' }).click()
  await expect(painel.getByText(`Instituição Faculdade Demo criada. Enviamos o link de primeiro acesso para ${emailDaDemo}.`)).toBeVisible()
  await expect(painel.getByText(`/${daDemo}`, { exact: true }).first()).toBeVisible()

  const link = await linkDoEmail(api, emailDaDemo)
  expect(new URL(link).pathname).toBe(`/${daDemo}/definir-senha`)
  const visitante = await browser.newContext({ baseURL })
  const pagina = await visitante.newPage()
  await pagina.goto(link.replace(/^https?:\/\/[^/]+/, ''))
  await expect(pagina.getByLabel('Nova senha')).toBeVisible()

  const linha = painel.getByRole('row').filter({ hasText: `/${daDemo}` })
  await linha.getByRole('button', { name: 'Ações de Faculdade Demo' }).click()
  await painel.getByRole('menuitem', { name: 'Desativar' }).click()
  const confirmacao = painel.getByRole('dialog')
  await confirmacao.getByLabel('Motivo').fill('Fim do teste e2e')
  await confirmacao.getByRole('button', { name: 'Desativar' }).click()
  await expect(linha.getByText('Desativada')).toBeVisible()
  await pagina.goto(`/${daDemo}`)
  await expect(pagina.getByText('Instituição não encontrada.')).toBeVisible()

  await painel.getByRole('link', { name: 'Auditoria' }).click()
  await expect(painel.getByText('Instituição desativada').first()).toBeVisible()

  await equipe.close()
  await visitante.close()
})
