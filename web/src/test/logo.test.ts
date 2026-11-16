import { readFileSync, readdirSync } from 'node:fs'
import { resolve } from 'node:path'
import { spawnSync } from 'node:child_process'
import { describe, expect, it } from 'vitest'

const raiz = process.cwd()
const marca = (nome: string) => readFileSync(resolve(raiz, 'public/marca', nome), 'utf8')
const indexHtml = readFileSync(resolve(raiz, 'index.html'), 'utf8')
const hex = (digitos: string) => '#' + digitos
const Marca = hex('224fa7')
const Branco = hex('ffffff')
const contar = (texto: string, padrao: RegExp) => (texto.match(padrao) ?? []).length

describe('marca em SVG', () => {
  it('simbolo.svg tem um unico path com currentColor', () => {
    const svg = marca('simbolo.svg')
    expect(svg).toMatch(/viewBox="0 0 64 64"/)
    expect(contar(svg, /<path\b/g)).toBe(1)
    expect(svg).toMatch(/<path[^>]*fill="currentColor"/)
    for (const proibido of ['<mask', '<filter', 'Gradient', '<text', '<image', '<style']) expect(svg, proibido).not.toContain(proibido)
    expect(svg).not.toMatch(/#[0-9a-fA-F]{3,8}\b/)
  })

  it('logo-horizontal.svg traz simbolo e palavra em curvas', () => {
    const svg = marca('logo-horizontal.svg')
    const [, largura, altura] = svg.match(/viewBox="0 0 ([\d.]+) ([\d.]+)"/)!.map(Number)
    expect(largura / altura).toBeGreaterThanOrEqual(3.5)
    expect(contar(svg, /<path\b/g)).toBeGreaterThanOrEqual(2)
    for (const proibido of ['<text', 'font-family', '<image']) expect(svg, proibido).not.toContain(proibido)
  })

  it('favicon.svg e declarado antes do png', () => {
    const svg = marca('favicon.svg')
    expect(svg).toContain(`fill="${Marca}"`)
    expect(svg).toContain(`fill="${Branco}"`)
    const iconeSvg = indexHtml.indexOf('<link rel="icon" type="image/svg+xml" href="/marca/favicon.svg"')
    const iconePng = indexHtml.indexOf('<link rel="icon" type="image/png"')
    expect(iconeSvg).toBeGreaterThan(-1)
    expect(iconePng).toBeGreaterThan(iconeSvg)
  })

  it('a marca so usa 224fa7 e branco', () => {
    const arquivos = readdirSync(resolve(raiz, 'public/marca')).filter((a) => a.endsWith('.svg'))
    expect(arquivos.sort()).toEqual(['favicon.svg', 'logo-horizontal.svg', 'simbolo.svg'])
    for (const arquivo of arquivos) {
      const cores = [...marca(arquivo).matchAll(/(?:fill|stroke|stop-color)="([^"]+)"/g)].map((m) => m[1].toLowerCase())
      expect(cores.length, arquivo).toBeGreaterThan(0)
      for (const cor of cores) expect([Marca, Branco, 'currentcolor'], `${arquivo} ${cor}`).toContain(cor)
    }
  })

  it('favicon em 16 px', () => {
    const roteiro = `
      import { chromium } from '@playwright/test'
      const svg = process.argv[1]
      const navegador = await chromium.launch()
      const pagina = await navegador.newPage()
      const proporcao = await pagina.evaluate(async (conteudo) => {
        const imagem = new Image()
        imagem.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(conteudo)
        await imagem.decode()
        const tela = document.createElement('canvas')
        tela.width = 16
        tela.height = 16
        const contexto = tela.getContext('2d')
        contexto.drawImage(imagem, 0, 0, 16, 16)
        const dados = contexto.getImageData(0, 0, 16, 16).data
        let claros = 0
        for (let i = 0; i < dados.length; i += 4) if (0.2126 * dados[i] + 0.7152 * dados[i + 1] + 0.0722 * dados[i + 2] > 200) claros++
        return claros / 256
      }, svg)
      await navegador.close()
      console.log(proporcao)
    `
    const r = spawnSync(process.execPath, ['--input-type=module', '-e', roteiro, marca('favicon.svg')], { cwd: raiz, encoding: 'utf8' })
    expect(r.status, r.stderr).toBe(0)
    const proporcao = Number(r.stdout.trim())
    expect(proporcao).toBeGreaterThanOrEqual(0.15)
    expect(proporcao).toBeLessThanOrEqual(0.45)
  }, 60_000)
})