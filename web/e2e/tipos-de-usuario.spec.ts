import { expect, test, type Locator, type Page } from '@playwright/test'
import { simularApi } from './simulada'

test.use({ serviceWorkers: 'block', hasTouch: true, isMobile: true })

const titulo = (page: Page) => page.locator('header h1')
const caixa = async (l: Locator) => (await l.boundingBox())!
const semRolagemHorizontal = (page: Page) => page.evaluate(() => document.documentElement.scrollWidth === document.documentElement.clientWidth)
const alcancavel = (l: Locator) =>
  l.evaluate((el) => {
    const r = el.getBoundingClientRect()
    const topo = document.elementFromPoint(r.x + r.width / 2, r.y + r.height / 2)
    return !!topo && (el === topo || el.contains(topo)) && r.top >= 0 && r.bottom <= innerHeight && r.left >= 0 && r.right <= innerWidth
  })
const dentroDaJanela = (page: Page) => (c: { x: number; y: number; width: number; height: number }) => {
  const tela = page.viewportSize()!
  return c.y >= 0 && c.y + c.height <= tela.height && c.x >= 0 && c.x + c.width <= tela.width
}
const sobrepoe = (a: { x: number; y: number; width: number; height: number }, b: { x: number; y: number; width: number; height: number }) =>
  a.x < b.x + b.width && b.x < a.x + a.width && a.y < b.y + b.height && b.y < a.y + a.height

async function abrir(page: Page, largura: number, altura: number, caminho = '/univag/tipos') {
  await page.setViewportSize({ width: largura, height: altura })
  await simularApi(page, ['ADMINISTRADOR'])
  await page.goto(caminho)
  await titulo(page).waitFor()
}

async function novoTipo(page: Page) {
  await page.getByRole('button', { name: 'Novo tipo', exact: true }).click()
  const dialogo = page.getByRole('dialog', { name: 'Novo tipo' })
  await dialogo.getByLabel('Nome').waitFor()
  return dialogo
}

test.describe('tipos de usuário no celular', () => {
  test('celular 360×780: uma coluna, funções na vertical e sem rolagem horizontal', async ({ page }) => {
    await abrir(page, 360, 780)
    await expect(page.getByText('Coordenador de curso').first()).toBeVisible()
    expect(await semRolagemHorizontal(page), 'lista').toBe(true)
    const lista = page.locator('[role=list]')
    await expect(lista).toHaveCount(1)
    const cartoes = await lista.locator('> *').all()
    expect(cartoes.length).toBeGreaterThanOrEqual(4)
    const colunas = new Set<number>()
    for (const c of cartoes) colunas.add(Math.round((await caixa(c)).x))
    expect(colunas.size, 'cartões na mesma coluna').toBe(1)

    const dialogo = await novoTipo(page)
    expect(await semRolagemHorizontal(page), 'formulário').toBe(true)
    const caixas = await dialogo.getByRole('checkbox').all()
    expect(caixas).toHaveLength(12)
    const posicoes = []
    for (const c of caixas) posicoes.push(await caixa(c))
    expect(new Set(posicoes.map((p) => Math.round(p.x))).size, 'funções na mesma coluna').toBe(1)
    for (let i = 1; i < posicoes.length; i++) expect(posicoes[i].y, `função ${i + 1} abaixo da anterior`).toBeGreaterThan(posicoes[i - 1].y)
    for (const p of posicoes) expect(p.x + p.width).toBeLessThanOrEqual(360)
    const nome = await caixa(dialogo.getByLabel('Nome'))
    expect(nome.x + nome.width).toBeLessThanOrEqual(360)
    expect(posicoes[0].y).toBeGreaterThan(nome.y + nome.height - 1)
  })

  test('celular deitado 844×390: o formulário rola e o Salvar fica visível sem cobrir campo', async ({ page }) => {
    await abrir(page, 844, 390)
    const dialogo = await novoTipo(page)
    const salvar = dialogo.getByRole('button', { name: 'Salvar', exact: true })
    await salvar.scrollIntoViewIfNeeded()
    expect(await alcancavel(salvar), 'Salvar alcançável').toBe(true)
    expect(dentroDaJanela(page)(await caixa(salvar))).toBe(true)
    const botao = await caixa(salvar)
    const campos = [dialogo.getByLabel('Nome'), ...(await dialogo.getByRole('checkbox').all())]
    for (const campo of campos) expect(sobrepoe(botao, await caixa(campo)), 'Salvar não cobre campo').toBe(false)
    const ultima = dialogo.getByRole('checkbox', { name: 'Usar o acesso por MCP' })
    await ultima.scrollIntoViewIfNeeded()
    expect(dentroDaJanela(page)(await caixa(ultima))).toBe(true)
    expect(await semRolagemHorizontal(page)).toBe(true)
  })

  test('teclado aberto 360×400: o Nome continua visível e o Salvar alcançável', async ({ page }) => {
    await abrir(page, 360, 400)
    const dialogo = await novoTipo(page)
    const nome = dialogo.getByLabel('Nome')
    await nome.focus()
    await page.keyboard.type('Coordenação de curso')
    await expect(nome).toHaveValue('Coordenação de curso')
    expect(await alcancavel(nome), 'Nome visível ao digitar').toBe(true)
    const salvar = dialogo.getByRole('button', { name: 'Salvar', exact: true })
    await salvar.scrollIntoViewIfNeeded()
    expect(await alcancavel(salvar), 'Salvar alcançável').toBe(true)
    expect(sobrepoe(await caixa(salvar), await caixa(nome)), 'Salvar não cobre o Nome').toBe(false)
    expect(await semRolagemHorizontal(page)).toBe(true)
  })

  test('alvos de toque de 44 px e nada depende de passar o mouse', async ({ page }) => {
    await abrir(page, 360, 780)
    expect(await page.evaluate(() => matchMedia('(pointer: coarse)').matches)).toBe(true)
    const grande = async (l: Locator, nome: string) => {
      const c = await caixa(l)
      expect(c.width, `${nome} largura`).toBeGreaterThanOrEqual(44)
      expect(c.height, `${nome} altura`).toBeGreaterThanOrEqual(44)
    }
    await grande(page.getByRole('button', { name: 'Novo tipo', exact: true }), 'Novo tipo')

    const dialogo = await novoTipo(page)
    for (const c of await dialogo.getByRole('checkbox').all()) await grande(c.locator('xpath=ancestor::label[1]'), `caixa de função ${await c.getAttribute('id')}`)
    await grande(dialogo.getByRole('button', { name: 'Salvar', exact: true }), 'Salvar')
    await dialogo.getByRole('button', { name: 'Cancelar', exact: true }).click()
    await expect(dialogo).toHaveCount(0)

    await page.getByRole('button', { name: 'Ações de Coordenador de curso' }).click()
    await grande(page.getByRole('menuitem', { name: 'Apagar' }), 'Apagar no menu')
    await page.getByRole('menuitem', { name: 'Apagar' }).click()
    const confirmacao = page.getByRole('dialog', { name: 'Apagar o tipo Coordenador de curso?' })
    await grande(confirmacao.getByRole('button', { name: 'Apagar', exact: true }), 'Apagar na confirmação')
    await confirmacao.getByRole('button', { name: 'Voltar', exact: true }).click()

    await page.goto('/univag/usuarios')
    await titulo(page).waitFor()
    await page.getByRole('button', { name: 'Novo usuário', exact: true }).click()
    const usuario = page.getByRole('dialog', { name: 'Novo usuário' })
    await grande(usuario.getByLabel('Tipo'), 'seletor Tipo')
  })
})
