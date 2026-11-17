import { expect, test, type Page } from '@playwright/test'
import { chamadoSimulado, simularApi } from './simulada'

test.use({ serviceWorkers: 'block', hasTouch: true, isMobile: true })

const Botao = 'Criar artigo a partir desta resposta'
const Resposta = 'Reemitimos o boleto de novembro com o valor corrigido. Ele já está no Portal do Aluno, em Financeiro > Boletos e mensalidades > Segunda via, e vence no dia 30.'

async function abrir(page: Page, largura: number, altura: number) {
  await page.setViewportSize({ width: largura, height: altura })
  await simularApi(page, ['ATENDENTE'])
  await page.route('**/api/chamados/ESC-000777', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        ...chamadoSimulado('RESOLVIDO'), numero: 'ESC-000777', resolvidoEm: '2026-11-03T12:00:00Z', acoes: ['nota', 'reclassificar'], artigoDaResposta: 'PODE_CRIAR',
        categorias: [{ id: 1, nome: 'Financeiro > Boletos e mensalidades' }],
        mensagens: [{ id: 2, tipo: 'PUBLICA', texto: Resposta, autor: 'Carla Regina de Souza', doAluno: false, criadaEm: '2026-11-03T12:00:00Z', anexos: [] }],
      }),
    }))
  await page.goto('/univag/chamados/ESC-000777')
}

async function semRolagemHorizontal(page: Page, largura: number) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(largura)
}

async function altura44(alvo: ReturnType<Page['locator']>) {
  await alvo.scrollIntoViewIfNeeded()
  await expect(alvo).toBeVisible()
  expect((await alvo.boundingBox())!.height).toBeGreaterThanOrEqual(44)
}

async function abrirFormulario(page: Page) {
  const botao = page.getByRole('button', { name: Botao })
  await botao.scrollIntoViewIfNeeded()
  await botao.tap()
  const formulario = page.getByRole('dialog', { name: Botao })
  await expect(formulario).toBeVisible()
  return formulario
}

async function faixaAcimaDoTitulo(page: Page) {
  const faixa = (await page.getByText('Remova nomes, CPF e matrícula antes de salvar').boundingBox())!
  const titulo = (await page.getByRole('dialog').getByLabel('Título', { exact: true }).boundingBox())!
  expect(faixa.y + faixa.height).toBeLessThanOrEqual(titulo.y)
}

for (const [largura, altura] of [[360, 780], [844, 390]] as const) {
  test(`${largura}x${altura} botão do chamado resolvido e formulário do rascunho`, async ({ page }) => {
    await abrir(page, largura, altura)
    const botao = page.getByRole('button', { name: Botao })
    await altura44(botao)
    await semRolagemHorizontal(page, largura)

    const formulario = await abrirFormulario(page)
    await expect(formulario.getByLabel('Resposta', { exact: true })).toHaveValue(Resposta)
    await faixaAcimaDoTitulo(page)
    if (largura === 360) expect(Math.round((await formulario.boundingBox())!.width)).toBe(largura)
    await altura44(formulario.getByRole('button', { name: 'Salvar rascunho' }))
    await semRolagemHorizontal(page, largura)
  })
}

test('360x400 teclado aberto mantém o campo e o salvar alcançáveis', async ({ page }) => {
  await abrir(page, 360, 780)
  const formulario = await abrirFormulario(page)
  await page.setViewportSize({ width: 360, height: 400 })
  const campo = formulario.getByLabel('Resposta', { exact: true })
  await campo.focus()
  await campo.scrollIntoViewIfNeeded()

  const caixa = (await campo.boundingBox())!
  expect(caixa.y).toBeGreaterThanOrEqual(0)
  expect(caixa.y).toBeLessThan(400)
  await expect(campo).toBeFocused()
  const salvar = formulario.getByRole('button', { name: 'Salvar rascunho' })
  await salvar.scrollIntoViewIfNeeded()
  await expect(salvar).toBeInViewport()
  await semRolagemHorizontal(page, 360)
})
