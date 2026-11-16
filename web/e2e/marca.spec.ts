import { expect, test } from '@playwright/test'

const arquivos: [string, string][] = [
  ['/marca/simbolo.svg', 'image/svg+xml'],
  ['/marca/logo-horizontal.svg', 'image/svg+xml'],
  ['/marca/favicon.svg', 'image/svg+xml'],
  ['/icons/apple-touch-icon.png', 'image/png'],
]

test('arquivos da marca respondem 200', async ({ request }) => {
  for (const [caminho, tipo] of arquivos) {
    const resposta = await request.get(caminho)
    expect(resposta.status(), caminho).toBe(200)
    expect(resposta.headers()['content-type'], caminho).toContain(tipo)
  }
})

test('raiz em celular 360x780', async ({ page }) => {
  await page.setViewportSize({ width: 360, height: 780 })
  await page.goto('/')
  const logo = page.getByRole('heading', { level: 1, name: 'Esclari' }).getByRole('img', { name: 'Esclari' })
  await expect(logo).toBeVisible()
  const caixa = (await logo.boundingBox())!
  expect(Math.round(caixa.height)).toBe(40)
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(360)
})

test('raiz deitada 844x390', async ({ page }) => {
  await page.setViewportSize({ width: 844, height: 390 })
  await page.goto('/')
  await expect(page.getByRole('heading', { level: 1, name: 'Esclari' }).getByRole('img', { name: 'Esclari' })).toBeVisible()
  await expect(page.getByText('Dúvidas, solicitações e respostas da sua instituição em um só lugar.')).toBeVisible()
  await expect(page.getByText('Abra o endereço da sua instituição, por exemplo /univag.')).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollHeight)).toBeLessThanOrEqual(390)
})
