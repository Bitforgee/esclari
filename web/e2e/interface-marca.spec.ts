import { expect, test, type Page } from '@playwright/test'
import { mkdirSync } from 'node:fs'
import { resolve } from 'node:path'
import { abrirTela, comAparencia, instalar, semAparencia, simularApi } from './simulada'

test.use({ serviceWorkers: 'block' })

const Pasta = resolve(process.cwd(), '../.specs/features/interface-marca-esclari/capturas')
const Tinta = [0x15, 0x13, 0x3a]
const Papel = [0xf7, 0xf5, 0xee]
const Vinho = [0x8a, 0x15, 0x38]
const Telas: [string, string][] = [
  ['login', 'Login'],
  ['assistente', 'Assistente'],
  ['solicitacoes', 'Minhas solicitações'],
  ['chamado', 'Detalhe do chamado'],
  ['fila', 'Fila'],
  ['indicadores', 'Indicadores'],
  ['aparencia', 'Aparência'],
]
const Tamanhos = [[360, 780], [1280, 800]] as const

async function preparar(page: Page, tema: string, aparencia: object = semAparencia) {
  await page.emulateMedia({ reducedMotion: 'reduce' })
  await page.addInitScript((t) => localStorage.setItem('esclari-tema', t), tema)
  await instalar(page)
  await simularApi(page, undefined, false, aparencia)
}

const pintada = (page: Page, seletor: string, propriedade: string, pseudo = '') =>
  page.locator(seletor).first().evaluate((e, [p, ps]) => window.__ux.pintar(['#fff', getComputedStyle(e, ps || null).getPropertyValue(p)]), [propriedade, pseudo] as const)

const perto = (cor: number[], esperada: number[], rotulo: string) =>
  expect(Math.max(...cor.map((v, i) => Math.abs(v - esperada[i]))), `${rotulo}: ${cor} contra ${esperada}`).toBeLessThanOrEqual(2)

async function acoes(page: Page, comMarca = false) {
  await page.setViewportSize({ width: 1280, height: 800 })
  await page.goto('/univag/assistente')
  await page.getByLabel('Mensagem').fill('segunda via do boleto')
  await page.waitForFunction((tema) => document.documentElement.classList.contains(tema), await page.evaluate(() => localStorage.getItem('esclari-tema')!))
  if (comMarca) await page.waitForFunction(() => document.documentElement.style.getPropertyValue('--marca') !== '')
  const botao = await pintada(page, 'button.bg-primary:visible', 'background-color')
  const ativo = await pintada(page, '[data-sidebar=menu-button][data-active=true]', 'border-left-color', '::before')
  await page.setViewportSize({ width: 360, height: 780 })
  await page.locator('a[aria-current=page].text-primary').first().waitFor()
  const link = await pintada(page, 'a[aria-current=page].text-primary', 'color')
  return { botao, ativo, link }
}

test('sem cor da instituicao a acao e Tinta ou Papel', async ({ page }) => {
  for (const [tema, esperada] of [['light', Tinta], ['dark', Papel], ['black', Papel]] as const) {
    await preparar(page, tema)
    const { botao, ativo, link } = await acoes(page)
    perto(botao, esperada, `${tema} botão`)
    perto(ativo, esperada, `${tema} item ativo`)
    perto(link, esperada, `${tema} link`)
    await page.unroute('**/api/**')
  }
})

test('com cor da instituicao a acao segue a cor', async ({ page }) => {
  await preparar(page, 'light', comAparencia)
  const { botao, ativo } = await acoes(page, true)
  perto(botao, Vinho, 'botão')
  perto(ativo, Vinho, 'item ativo')
  const fundo = await page.evaluate(() => window.__ux.pintar(['#fff', getComputedStyle(document.body).backgroundColor]))
  expect(fundo, 'fundo tingido').not.toEqual(Papel)
})

test('capturas das sete telas', async ({ page }) => {
  test.setTimeout(240_000)
  mkdirSync(Pasta, { recursive: true })
  for (const [tema, rotulo] of [['light', 'claro'], ['dark', 'escuro']] as const) {
    await preparar(page, tema)
    for (const [largura, altura] of Tamanhos)
      for (const [arquivo, nome] of Telas) {
        await page.setViewportSize({ width: largura, height: altura })
        await abrirTela(page, nome)
        await page.screenshot({ path: resolve(Pasta, `${arquivo}-${rotulo}-${largura}.png`), animations: 'disabled' })
      }
    await page.unroute('**/api/**')
  }
})
