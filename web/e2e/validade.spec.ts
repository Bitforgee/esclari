import { expect, test, type Page } from '@playwright/test'
import { simularApi } from './simulada'

test.use({ serviceWorkers: 'block', hasTouch: true, isMobile: true })

const TituloLongo = 'Como pedir o abono de faltas por atestado médico durante o período de provas do semestre'

const artigo = (id: number, titulo: string, validoAte: string, validade: string) => ({
  id, estado: 'ATIVO', indexacao: 'INDEXADO', categoriaId: 1, categoria: 'Acadêmico > Frequência', visivelDe: null, visivelAte: null, validoAte, validade,
  versao: { id, numero: 3, titulo, perguntas: ['abono?'], resposta: 'x', links: [] },
})

const Artigos = [
  artigo(1, TituloLongo, '2026-11-10', 'VENCIDO'),
  artigo(2, 'Trancamento de matrícula', '2026-11-29', 'VENCE_EM_BREVE'),
  artigo(3, 'Rematrícula', '2027-11-17', 'VIGENTE'),
]

async function abrir(page: Page, largura: number, altura: number, caminho: string) {
  await page.setViewportSize({ width: largura, height: altura })
  await page.clock.setFixedTime(new Date('2026-11-17T12:00:00-04:00'))
  await simularApi(page, ['ADMINISTRADOR'])
  await page.route('**/api/admin/artigos', (rota) => rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(Artigos) }))
  await page.goto(caminho)
}

async function semRolagemHorizontal(page: Page, largura: number) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(largura)
}

async function etiquetaInteira(page: Page, texto: string, largura: number) {
  const etiqueta = page.getByText(texto, { exact: true })
  await expect(etiqueta).toBeVisible()
  const caixa = (await etiqueta.boundingBox())!
  expect(caixa.x).toBeGreaterThanOrEqual(0)
  expect(caixa.x + caixa.width).toBeLessThanOrEqual(largura)
  expect(await etiqueta.evaluate((e) => e.scrollWidth <= e.clientWidth + 1)).toBe(true)
}

for (const [largura, altura] of [[360, 780], [844, 390]] as const) {
  test(`${largura}x${altura} lista de artigos com validade`, async ({ page }) => {
    await abrir(page, largura, altura, '/univag/artigos')
    await etiquetaInteira(page, 'Vencido em 10/11/2026', largura)
    await etiquetaInteira(page, 'Vence em 12 dias', largura)
    await etiquetaInteira(page, 'Vence em 17/11/2027', largura)
    await semRolagemHorizontal(page, largura)

    for (const nome of [`Renovar ${TituloLongo}`, 'Renovar Trancamento de matrícula']) {
      const botao = page.getByRole('button', { name: nome })
      await botao.scrollIntoViewIfNeeded()
      await expect(botao).toBeInViewport()
      expect((await botao.boundingBox())!.height).toBeGreaterThanOrEqual(44)
    }
  })

  test(`${largura}x${altura} seção da base no painel`, async ({ page }) => {
    await abrir(page, largura, altura, '/univag/indicadores')
    const secao = page.getByRole('region', { name: 'A base de conhecimento está em dia?' })
    await secao.scrollIntoViewIfNeeded()
    await expect(secao.getByText('Vencidos', { exact: true })).toBeVisible()
    await expect(secao).toContainText('160')
    await semRolagemHorizontal(page, largura)
    const link = secao.getByRole('link', { name: 'Ver vencidos' })
    expect((await link.boundingBox())!.height).toBeGreaterThanOrEqual(44)
  })
}
