import { readFileSync } from 'node:fs'
import { describe, expect, it } from 'vitest'

type Lab = [number, number, number]

const css = readFileSync('src/index.css', 'utf8')
const bloco = (seletor: string) => {
  const inicio = css.search(new RegExp(`^${seletor.replace('.', '\\.')} \\{`, 'm'))
  return css.slice(inicio, css.indexOf('\n}', inicio))
}
const declaracoes = (texto: string) => new Map([...texto.matchAll(/^\s*(--[\w-]+):\s*(.+);\s*$/gm)].map((m) => [m[1], m[2]]))
const blocos = { claro: declaracoes(bloco(':root')), escuro: declaracoes(bloco('.dark')), preto: declaracoes(bloco('.black')) }

const tema = (nome: 'claro' | 'escuro' | 'preto') => (token: string) =>
  (nome === 'preto' ? blocos.preto.get(token) ?? blocos.escuro.get(token) : nome === 'escuro' ? blocos.escuro.get(token) : undefined) ?? blocos.claro.get(token)!

function dividir(texto: string) {
  const partes: string[] = []
  let nivel = 0
  let atual = ''
  for (const c of texto) {
    if (c === ',' && nivel === 0) (partes.push(atual.trim()), (atual = ''))
    else ((nivel += c === '(' ? 1 : c === ')' ? -1 : 0), (atual += c))
  }
  return [...partes, atual.trim()]
}

function oklchParaLab(l: number, c: number, h: number): Lab {
  const r = (h * Math.PI) / 180
  return [l, c * Math.cos(r), c * Math.sin(r)]
}

function ler(valor: string, marca: Lab): Lab {
  const v = valor.trim()
  const dentro = v.slice(v.indexOf('(') + 1, v.lastIndexOf(')'))
  if (/^oklch\s*\(/.test(v)) {
    const [l, c, h] = dentro.split(/\s+/).map((x) => (x === 'none' ? 0 : Number(x)))
    return oklchParaLab(l, c, h)
  }
  if (v.startsWith('var(')) {
    const [, ...resto] = dividir(dentro)
    return resto.length ? ler(resto.join(','), marca) : marca
  }
  const [, a, b] = dividir(dentro)
  const [cor, pct] = [a.replace(/\s+[\d.]+%$/, ''), Number(a.match(/([\d.]+)%$/)![1]) / 100]
  const [x, y] = [ler(cor, marca), ler(b, marca)]
  return [0, 1, 2].map((i) => x[i] * pct + y[i] * (1 - pct)) as Lab
}

function luminancia([L, a, b]: Lab) {
  const l = (L + 0.3963377774 * a + 0.2158037573 * b) ** 3
  const m = (L - 0.1055613458 * a - 0.0638541728 * b) ** 3
  const s = (L - 0.0894841775 * a - 1.291485548 * b) ** 3
  const lin = [4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s, -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s, -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s]
  const [r, g, bl] = lin.map((x) => Math.min(1, Math.max(0, x)))
  return 0.2126 * r + 0.7152 * g + 0.0722 * bl
}

const contraste = (a: Lab, b: Lab) => {
  const [x, y] = [luminancia(a), luminancia(b)].sort((p, q) => q - p)
  return (x + 0.05) / (y + 0.05)
}

const marcaPadrao = oklchParaLab(0.45, 0.15, 262)
const marcaNoLimite = oklchParaLab(0.58, 0.14, 245)

const cenarios = [
  { nome: 'claro', tema: tema('claro'), marca: marcaPadrao },
  { nome: 'escuro', tema: tema('escuro'), marca: marcaPadrao },
  { nome: 'preto', tema: tema('preto'), marca: marcaPadrao },
  { nome: 'claro com marca no limite', tema: tema('claro'), marca: marcaNoLimite },
]

describe('contraste dos gráficos', () => {
  it('contraste de graficos', () => {
    expect(cenarios).toHaveLength(4)
    for (const { nome, tema: valor, marca } of cenarios) {
      const fundo = ler(valor('--background'), marca)
      for (const token of ['--grafico-1', '--grafico-2', '--grafico-3', '--grafico-4'])
        expect(contraste(ler(valor(token), marca), fundo), `${nome} ${token}`).toBeGreaterThanOrEqual(3)
      for (const token of ['--foreground', '--muted-foreground'])
        expect(contraste(ler(valor(token), marca), fundo), `${nome} ${token}`).toBeGreaterThanOrEqual(4.5)
    }
  })

  it('tons de cinza distintos', () => {
    for (const nome of ['claro', 'escuro'] as const) {
      const luz = ['--grafico-1', '--grafico-2', '--grafico-3', '--grafico-4'].map((t) => ler(tema(nome)(t), marcaPadrao)[0] * 100).sort((a, b) => a - b)
      for (let i = 1; i < luz.length; i++) expect(luz[i] - luz[i - 1], `${nome} ${luz.join(' ')}`).toBeGreaterThanOrEqual(10)
    }
  })
})
