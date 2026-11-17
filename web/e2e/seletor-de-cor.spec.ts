import { expect, test } from '@playwright/test'
import { simularApi } from './simulada'

test.use({ serviceWorkers: 'block' })

test('seletor ao lado do campo', async ({ page }) => {
  await simularApi(page)
  for (const [largura, altura] of [[360, 780], [1280, 800]] as const) {
    await page.setViewportSize({ width: largura, height: altura })
    await page.goto('/univag/aparencia')
    const campo = page.getByLabel('Cor principal')
    await campo.waitFor()
    const rotulo = page.locator('label', { hasText: 'Escolher cor' })
    const [c, r] = [(await campo.boundingBox())!, (await rotulo.boundingBox())!]
    expect(Math.abs(c.y + c.height / 2 - (r.y + r.height / 2)), `${largura}: mesma linha`).toBeLessThanOrEqual(2)
    expect(r.x + r.width, `${largura}: dentro da página`).toBeLessThanOrEqual(largura)
    expect(await page.evaluate(() => document.documentElement.scrollWidth), `${largura}: sem rolagem lateral`).toBeLessThanOrEqual(largura)
    if (largura === 360) expect(r.height, 'alvo de toque').toBeGreaterThanOrEqual(44)
    await rotulo.getByText('Escolher cor').click()
    await expect(page.getByLabel('Escolher cor')).toBeFocused()
  }
})
