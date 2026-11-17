import { spawnSync } from 'node:child_process'
import { readFileSync, readdirSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

const raiz = process.cwd()
const marca = (nome: string) => readFileSync(resolve(raiz, 'public/marca', nome), 'utf8')
const indexHtml = readFileSync(resolve(raiz, 'index.html'), 'utf8')
const Origem = '#' + 'C83A23'
const OrigemClara = '#' + 'F16A4B'
const Tinta = '#' + '15133A'
const Papel = '#' + 'F7F5EE'
const Preto = '#' + '000000'
const Branco = '#' + 'FFFFFF'
const arquivos = ['simbolo.svg', 'simbolo-pequeno.svg', 'palavra-marca.svg', 'logo-horizontal.svg', 'logo-horizontal-escuro.svg', 'logo-vertical.svg', 'logo-preto.svg', 'logo-branco.svg', 'favicon.svg']
const masters = arquivos.filter((a) => a !== 'favicon.svg')
const contar = (texto: string, padrao: RegExp) => (texto.match(padrao) ?? []).length
const cores = (svg: string) => [...svg.matchAll(/(?:fill|stroke|stop-color)=["']([^"']+)["']/gi)].map((m) => m[1])
const proibidos = ['<text', '<image', 'font-family', '<mask', '<clipPath', '<filter', 'gradient', 'url(']

function viewBox(svg: string) {
  const valores = svg.match(/viewBox=["']([\d. -]+)["']/)![1].split(/\s+/).map(Number)
  return { x: valores[0], y: valores[1], largura: valores[2], altura: valores[3] }
}

function luminancia(hex: string) {
  const canais = hex.match(/[0-9a-f]{2}/gi)!.map((v) => parseInt(v, 16) / 255).map((v) => v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4)
  return 0.2126 * canais[0] + 0.7152 * canais[1] + 0.0722 * canais[2]
}

function contraste(a: string, b: string) {
  const [clara, escura] = [luminancia(a), luminancia(b)].sort((x, y) => y - x)
  return (clara + 0.05) / (escura + 0.05)
}

function navegador(roteiro: string, ...argumentos: string[]) {
  const r = spawnSync(process.execPath, ['--input-type=module', '-e', roteiro, ...argumentos], { cwd: raiz, encoding: 'utf8' })
  expect(r.status, r.stderr).toBe(0)
  return JSON.parse(r.stdout.trim())
}

const roteiroImagem = `
  import { chromium } from '@playwright/test'
  const [svg, ladoTexto] = process.argv.slice(1)
  const lado = Number(ladoTexto)
  const navegador = await chromium.launch()
  const pagina = await navegador.newPage({ viewport: { width: lado, height: lado } })
  const resultado = await pagina.evaluate(async ({ svg, lado }) => {
    const imagem = new Image()
    imagem.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svg)
    await imagem.decode()
    const tela = document.createElement('canvas')
    tela.width = lado
    tela.height = lado
    const contexto = tela.getContext('2d')
    contexto.drawImage(imagem, 0, 0, lado, lado)
    const dados = contexto.getImageData(0, 0, lado, lado).data
    let area = 0, minX = lado, minY = lado, maxX = -1, maxY = -1
    const claros = []
    for (let y = 0; y < lado; y++) for (let x = 0; x < lado; x++) {
      const i = (y * lado + x) * 4
      const l = .2126 * dados[i] + .7152 * dados[i + 1] + .0722 * dados[i + 2]
      if (dados[i + 3] && l < 80) { area++; minX = Math.min(minX, x); minY = Math.min(minY, y); maxX = Math.max(maxX, x); maxY = Math.max(maxY, y) }
      if (l > 220) claros.push([x, y])
    }
    return { area, minX, minY, maxX, maxY, claros }
  }, { svg, lado })
  await navegador.close()
  console.log(JSON.stringify(resultado))
`

describe('marca final em SVG', () => {
  it('simbolo final e um contorno preenchido', () => {
    const svg = marca('simbolo.svg')
    expect(viewBox(svg)).toEqual({ x: 0, y: 0, largura: 256, altura: 256 })
    expect(contar(svg, /<path\b/g)).toBe(1)
    expect(svg).toMatch(/<path[^>]*fill=["']currentColor["']/)
    for (const proibido of [...proibidos, 'stroke=', 'transform=']) expect(svg.toLowerCase()).not.toContain(proibido.toLowerCase())
    expect(svg).not.toMatch(/#[0-9a-f]{3,8}\b/i)
    const caixa = navegador(roteiroImagem, svg, '256')
    expect(caixa.minX).toBeGreaterThanOrEqual(20)
    expect(caixa.minX).toBeLessThanOrEqual(24)
    expect(caixa.maxX).toBeGreaterThanOrEqual(240)
    expect(caixa.maxX).toBeLessThanOrEqual(244)
    expect(caixa.minY).toBeGreaterThanOrEqual(16)
    expect(caixa.minY).toBeLessThanOrEqual(20)
    expect(caixa.maxY).toBeGreaterThanOrEqual(236)
    expect(caixa.maxY).toBeLessThanOrEqual(240)
  }, 60_000)

  it('corte pequeno reforca peso e encurta pontas', () => {
    const master = marca('simbolo.svg')
    const pequeno = marca('simbolo-pequeno.svg')
    expect(viewBox(pequeno)).toEqual({ x: 0, y: 0, largura: 256, altura: 256 })
    expect(contar(pequeno, /<path\b/g)).toBe(1)
    expect(pequeno).toMatch(/<path[^>]*fill=["']currentColor["']/)
    for (const proibido of [...proibidos, 'stroke=', 'transform=']) expect(pequeno.toLowerCase()).not.toContain(proibido.toLowerCase())
    const a = navegador(roteiroImagem, master, '256')
    const b = navegador(roteiroImagem, pequeno, '256')
    expect(b.area).toBeGreaterThan(a.area)
    expect(b.maxX - b.minX).toBeLessThan(a.maxX - a.minX)
    expect(b.maxY - b.minY).toBeLessThan(a.maxY - a.minY)
    expect(pequeno).toContain('data-min-detail="8"')
  }, 120_000)

  it('palavra-marca e propria e separa r de i', () => {
    const svg = marca('palavra-marca.svg')
    for (const proibido of proibidos) expect(svg.toLowerCase()).not.toContain(proibido.toLowerCase())
    expect([...svg.matchAll(/id=["']letra-([esclari])["']/g)].map((m) => m[1])).toEqual(['e', 's', 'c', 'l', 'a', 'r', 'i'])
    for (const letra of ['e', 'a', 'r', 'i']) expect(svg).toMatch(new RegExp(`id=["']letra-${letra}["'][^>]*data-custom=["']fio["']`))
    expect(svg).toMatch(/id=["']pingo-i["'][^>]*fill=["']currentColor["']/)
    const roteiro = `
      import { chromium } from '@playwright/test'
      const svg = process.argv[1]
      const navegador = await chromium.launch()
      const pagina = await navegador.newPage()
      const gap = await pagina.evaluate((svg) => {
        document.body.innerHTML = svg
        const r = document.querySelector('#letra-r').getBBox()
        const i = document.querySelector('#letra-i').getBBox()
        const vb = document.querySelector('svg').viewBox.baseVal
        return (i.x - r.x - r.width) * 24 / vb.height
      }, svg)
      await navegador.close()
      console.log(JSON.stringify(gap))
    `
    expect(navegador(roteiro, svg)).toBeGreaterThanOrEqual(1)
  }, 60_000)

  it('marca usa somente a paleta e os papeis aprovados', () => {
    expect(cores(marca('logo-horizontal.svg')).sort()).toEqual([Tinta, Origem].sort())
    expect(cores(marca('logo-horizontal-escuro.svg')).sort()).toEqual([OrigemClara, Papel].sort())
    expect(cores(marca('logo-vertical.svg')).sort()).toEqual([Tinta, Origem].sort())
    expect(cores(marca('favicon.svg')).sort()).toEqual([Origem, Branco].sort())
    const permitidas = [Origem, OrigemClara, Tinta, Papel, Preto, Branco, 'currentColor']
    for (const arquivo of arquivos) for (const cor of cores(marca(arquivo))) expect(permitidas, `${arquivo}: ${cor}`).toContain(cor)
    for (const arquivo of arquivos) expect(marca(arquivo).toLowerCase()).not.toContain('gradient')
  })

  it('lockups compartilham simbolo palavra e espacamento', () => {
    for (const arquivo of ['logo-horizontal.svg', 'logo-horizontal-escuro.svg', 'logo-vertical.svg']) {
      const svg = marca(arquivo)
      expect(svg, arquivo).toContain('data-componente="simbolo"')
      expect(svg, arquivo).toContain('data-componente="palavra-marca"')
      expect(svg, arquivo).toContain('data-gap="28"')
      expect(svg, arquivo).toContain('data-alinhamento="optico"')
    }
  })

  it('logos preto e branco preservam o lockup', () => {
    const desenhos = (svg: string) => [...svg.matchAll(/<path[^>]*d=["']([^"']+)["']/g)].map((m) => m[1])
    expect(new Set(cores(marca('logo-preto.svg')))).toEqual(new Set([Preto]))
    expect(new Set(cores(marca('logo-branco.svg')))).toEqual(new Set([Branco]))
    expect(desenhos(marca('logo-preto.svg'))).toEqual(desenhos(marca('logo-horizontal.svg')))
    expect(desenhos(marca('logo-branco.svg'))).toEqual(desenhos(marca('logo-horizontal.svg')))
  })

  it('paleta cumpre os quatro contrastes', () => {
    expect(contraste('C83A23', 'FFFFFF')).toBeCloseTo(5.14, 2)
    expect(contraste('F16A4B', '15133A')).toBeCloseTo(5.82, 2)
    expect(contraste('15133A', 'FFFFFF')).toBeCloseTo(17.7, 1)
    expect(contraste('15133A', 'F7F5EE')).toBeCloseTo(16.23, 2)
    for (const [a, b] of [['C83A23', 'FFFFFF'], ['F16A4B', '15133A'], ['15133A', 'FFFFFF'], ['15133A', 'F7F5EE']]) expect(contraste(a, b)).toBeGreaterThanOrEqual(4.5)
  })

  it('favicon preserva volta e extremidades em 16 px', () => {
    const svg = marca('favicon.svg')
    expect(svg).toContain(`fill="${Origem}"`)
    expect(svg).toContain(`fill="${Branco}"`)
    const dados = navegador(roteiroImagem, svg, '16')
    const claros = new Set<string>(dados.claros.map(([x, y]: number[]) => `${x},${y}`))
    expect(dados.claros.length).toBeGreaterThan(20)
    expect([...claros].some((p) => p.startsWith('3,'))).toBe(true)
    const direita = dados.claros.filter(([x]: number[]) => x >= 11)
    expect(direita.length).toBeGreaterThan(1)
    expect(Math.min(...direita.map(([, y]: number[]) => y))).toBeLessThanOrEqual(6)
    expect(Math.max(...direita.map(([, y]: number[]) => y))).toBeGreaterThanOrEqual(8)
    const svgIndice = indexHtml.indexOf('<link rel="icon" type="image/svg+xml" href="/marca/favicon.svg"')
    expect(svgIndice).toBeGreaterThan(-1)
    expect(indexHtml.indexOf('<link rel="icon" type="image/png"')).toBeGreaterThan(svgIndice)
  }, 60_000)

  it('pasta publica contem somente os nove SVG aprovados', () => {
    expect(readdirSync(resolve(raiz, 'public/marca')).filter((a) => a.endsWith('.svg')).sort()).toEqual([...arquivos].sort())
  })
})
