import { expect, test, type Page } from '@playwright/test'
import { simularApi } from './simulada'

const campo = (page: Page) => page.getByLabel('Sua instituição')

async function abrirComCinco(page: Page, largura: number, altura: number) {
  await simularApi(page)
  await page.setViewportSize({ width: largura, height: altura })
  await page.goto('/')
  await campo(page).fill('uni')
  await page.getByRole('link', { name: /Univag/ }).waitFor()
  await expect(page.locator('ul li a')).toHaveCount(5)
}

const caixa = async (l: ReturnType<Page['locator']>) => (await l.boundingBox())!

test('celular 360x780: campo e cinco resultados inteiros sem rolagem', async ({ page }) => {
  await abrirComCinco(page, 360, 780)
  expect(await page.evaluate(() => document.documentElement.scrollHeight)).toBeLessThanOrEqual(780)
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(360)
  const ultimo = await caixa(page.locator('ul li a').last())
  expect(ultimo.y + ultimo.height).toBeLessThanOrEqual(780)
})

test('teclado aberto 360x420: campo e primeiro resultado dentro da área visível', async ({ page }) => {
  await abrirComCinco(page, 360, 420)
  await campo(page).focus()
  const c = await caixa(campo(page))
  const primeiro = await caixa(page.locator('ul li a').first())
  expect(c.y).toBeGreaterThanOrEqual(0)
  expect(primeiro.y + primeiro.height).toBeLessThanOrEqual(420)
})

test('celular deitado 844x390: campo sem rolagem e sem rolagem horizontal', async ({ page }) => {
  await simularApi(page)
  await page.setViewportSize({ width: 844, height: 390 })
  await page.goto('/')
  await campo(page).waitFor()
  expect(await page.evaluate(() => document.documentElement.scrollHeight)).toBeLessThanOrEqual(390)
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(844)
})

test('toque: resultados e links de troca com 44 px ou mais', async ({ page }) => {
  await abrirComCinco(page, 360, 780)
  for (const link of await page.locator('ul li a').all()) expect((await caixa(link)).height).toBeGreaterThanOrEqual(44)
  await page.getByRole('link', { name: /Univag/ }).click()
  const trocar = page.getByRole('link', { name: 'Trocar de instituição' })
  await trocar.waitFor()
  expect((await caixa(trocar)).height).toBeGreaterThanOrEqual(44)
})
