import { expect, test, type Page } from '@playwright/test'
import { chamadoSimulado, simularApi } from './simulada'

test.use({ serviceWorkers: 'block', hasTouch: true, isMobile: true })

const TituloLongo = 'Segunda via do boleto de rematrícula do segundo semestre com desconto de pontualidade'

const item = (n: number, atraso: string | null) => ({
  numero: `ESC-00012${n}`, titulo: TituloLongo, setor: 'Coordenação de Análise e Desenvolvimento de Sistemas', categoria: 'Financeiro > Boletos e mensalidades',
  estado: 'EM_ATENDIMENTO', responsavel: 'Carla Regina de Souza', criadoEm: '2026-11-10T12:00:00Z', dias: 7, atraso,
})

async function abrir(page: Page, largura: number, altura: number, papeis: string[], caminho: string) {
  await page.setViewportSize({ width: largura, height: altura })
  await page.clock.setFixedTime(new Date('2026-11-17T12:00:00-04:00'))
  await simularApi(page, papeis)
  await page.route('**/api/chamados/busca?**', (rota) => {
    const itens = [item(1, 'PRIMEIRA_RESPOSTA'), item(2, 'RESOLUCAO'), item(3, null)]
    return rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ itens, total: itens.length, pagina: 1, porPagina: 50 }) })
  })
  await page.route('**/api/chamados/ESC-000555', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ ...chamadoSimulado('ABERTO'), numero: 'ESC-000555', titulo: TituloLongo, solicitante: null, resumo: null, eventos: null, acoes: ['responder', 'cancelar'], prazoPrimeiraRespostaEm: '2026-11-18T12:00:00Z' }) }))
  await page.goto(caminho)
}

async function semRolagemHorizontal(page: Page, largura: number) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(largura)
}

async function inteiro(page: Page, texto: string, largura: number) {
  const alvo = page.getByText(texto, { exact: true }).first()
  await alvo.scrollIntoViewIfNeeded()
  await expect(alvo).toBeVisible()
  const caixa = (await alvo.boundingBox())!
  expect(caixa.x).toBeGreaterThanOrEqual(0)
  expect(caixa.x + caixa.width).toBeLessThanOrEqual(largura)
  expect(await alvo.evaluate((e) => e.scrollWidth <= e.clientWidth + 1)).toBe(true)
}

for (const [largura, altura] of [[360, 780], [844, 390]] as const) {
  test(`${largura}x${altura} fila com etiqueta e chave de atraso`, async ({ page }) => {
    await abrir(page, largura, altura, ['ATENDENTE'], '/univag/fila')
    await inteiro(page, 'Atrasado: 1ª resposta', largura)
    await inteiro(page, 'Atrasado: resolução', largura)
    await inteiro(page, 'ESC-000121', largura)
    await semRolagemHorizontal(page, largura)
    const chave = page.locator('label[for="filtro-atraso"]')
    await expect(chave).toBeVisible()
    expect((await chave.boundingBox())!.height).toBeGreaterThanOrEqual(44)
  })

  test(`${largura}x${altura} chamado do aluno com previsão`, async ({ page }) => {
    await abrir(page, largura, altura, ['ALUNO'], '/univag/chamados/ESC-000555')
    await inteiro(page, 'Resposta prevista até 18/11 às 08:00', largura)
    await semRolagemHorizontal(page, largura)
  })

  test(`${largura}x${altura} seção de prazos no painel`, async ({ page }) => {
    await abrir(page, largura, altura, ['ADMINISTRADOR'], '/univag/indicadores')
    const secao = page.getByRole('region', { name: 'A equipe responde a tempo?' })
    await secao.scrollIntoViewIfNeeded()
    await expect(secao.getByText('Em atraso agora: 3')).toBeVisible()
    await inteiro(page, 'Meta: primeira resposta em 24 h; resolução em 120 h', largura)
    await inteiro(page, 'O mais antigo está atrasado há 5 dias', largura)
    await semRolagemHorizontal(page, largura)
    for (const alvo of [secao.getByRole('link', { name: 'Ver chamados em atraso' }), secao.getByRole('button', { name: 'Ver como tabela: Dentro da meta, por período' })]) {
      await alvo.scrollIntoViewIfNeeded()
      expect((await alvo.boundingBox())!.height).toBeGreaterThanOrEqual(44)
    }
  })
}
