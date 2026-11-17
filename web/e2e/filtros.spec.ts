import { expect, test, type Locator, type Page } from '@playwright/test'
import { simularApi } from './simulada'

test.use({ serviceWorkers: 'block' })

const caixa = async (l: Locator) => (await l.boundingBox())!
const busca = (page: Page) => page.getByRole('searchbox', { name: 'Buscar por número, título ou aluno' })
const painel = (page: Page) => page.getByRole('dialog', { name: 'Filtros' })

async function abrir(page: Page, largura: number, altura: number, caminho = '/univag/fila') {
  await page.setViewportSize({ width: largura, height: altura })
  await simularApi(page, ['ATENDENTE'])
  await page.goto(caminho)
  await page.getByText('ESC-000123').first().waitFor()
}

async function abrirPainel(page: Page) {
  await page.getByRole('button', { name: /^Filtros/ }).click()
  await painel(page).waitFor()
  await page.waitForTimeout(600)
}

async function rodapeVisivel(page: Page, altura: number) {
  for (const nome of ['Limpar', 'Aplicar']) {
    const c = await caixa(painel(page).getByRole('button', { name: nome, exact: true }))
    expect(c.y, nome).toBeGreaterThanOrEqual(0)
    expect(c.y + c.height, nome).toBeLessThanOrEqual(altura + 0.5)
  }
}

test('desktop painel à direita e barra numa linha', async ({ page }) => {
  await abrir(page, 1280, 800)
  const campo = await caixa(busca(page))
  for (const nome of [/^Filtros/, /^Exportar CSV$/]) {
    const c = await caixa(page.getByRole('button', { name: nome }))
    expect(c.y + c.height / 2).toBeCloseTo(campo.y + campo.height / 2, 0)
  }
  await abrirPainel(page)
  const p = await caixa(painel(page))
  expect(p.x + p.width).toBeCloseTo(1280, 0)
  expect(p.x).toBeGreaterThan(640)
  expect(p.y).toBeCloseTo(0, 0)
  expect(p.height).toBeCloseTo(800, 0)
})
test.describe('filtros da fila no celular', () => {
  test.use({ hasTouch: true, isMobile: true })

  test('360 busca na largura toda', async ({ page }) => {
    await abrir(page, 360, 780)
    const barra = await caixa(page.locator('[data-barra-da-busca]'))
    const campo = await caixa(busca(page))
    const filtros = await caixa(page.getByRole('button', { name: 'Filtros' }))
    const exportar = await caixa(page.getByRole('button', { name: 'Exportar CSV' }))
    expect(campo.width).toBeCloseTo(barra.width, 0)
    expect(filtros.y).toBeGreaterThanOrEqual(campo.y + campo.height)
    expect(exportar.y).toBeCloseTo(filtros.y, 0)
    expect(exportar.x).toBeGreaterThan(filtros.x)
    expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(360)
  })

  test('360 painel de baixo com rodape fixo', async ({ page }) => {
    await abrir(page, 360, 780)
    await abrirPainel(page)
    const p = await caixa(painel(page))
    expect(p.y + p.height).toBeCloseTo(780, 0)
    const corpo = painel(page).locator('[data-painel=corpo]')
    await corpo.evaluate((e) => (e.scrollTop = e.scrollHeight))
    await rodapeVisivel(page, 780)
    await corpo.evaluate((e) => (e.scrollTop = 0))
    await rodapeVisivel(page, 780)

    const pedido = page.waitForRequest((r) => r.url().includes('/api/chamados/busca') && r.url().includes('estado=FECHADO'))
    await painel(page).getByText('Fechado', { exact: true }).click()
    await painel(page).getByRole('button', { name: 'Aplicar', exact: true }).click()
    await pedido
    await expect(painel(page)).toHaveCount(0)
    expect(new URL(page.url()).searchParams.getAll('estado')).toEqual(['FECHADO'])
    await expect(page.getByRole('button', { name: 'Remover filtro Estado' })).toBeVisible()
    await expect(page.getByText('ESC-000123').first()).toBeVisible()
  })

  test('deitado o painel rola', async ({ page }) => {
    await abrir(page, 844, 390)
    await abrirPainel(page)
    const corpo = painel(page).locator('[data-painel=corpo]')
    const [rola, visivel] = await corpo.evaluate((e) => [e.scrollHeight, e.clientHeight])
    expect(rola).toBeGreaterThan(visivel)
    for (const fim of [0.5, 1]) {
      await corpo.evaluate((e, f) => (e.scrollTop = (e.scrollHeight - e.clientHeight) * f), fim)
      await rodapeVisivel(page, 390)
    }
  })

  test('toque de 44 px', async ({ page }) => {
    await abrir(page, 360, 780, '/univag/fila?estado=FECHADO')
    const alvos: Locator[] = [busca(page), page.getByRole('button', { name: /^Filtros/ }), page.getByRole('button', { name: 'Exportar CSV' }), page.getByRole('button', { name: 'Remover filtro Estado' }), page.getByRole('button', { name: 'Limpar filtros' })]
    for (const alvo of alvos) expect((await caixa(alvo)).height, await alvo.evaluate((e) => e.getAttribute('aria-label') ?? e.textContent ?? e.tagName)).toBeGreaterThanOrEqual(44)
    await abrirPainel(page)
    const p = painel(page)
    const doPainel: Locator[] = [
      ...(await p.locator('fieldset').first().locator('label').all()),
      ...(await p.getByRole('combobox').all()),
      p.getByLabel('De', { exact: true }),
      p.getByLabel('Até', { exact: true }),
      p.getByRole('button', { name: 'Limpar', exact: true }),
      p.getByRole('button', { name: 'Aplicar', exact: true }),
    ]
    expect(doPainel.length).toBe(5 + 4 + 4)
    for (const alvo of doPainel) {
      await alvo.scrollIntoViewIfNeeded()
      expect((await caixa(alvo)).height, await alvo.evaluate((e) => e.textContent || e.id)).toBeGreaterThanOrEqual(44)
    }
  })
})
