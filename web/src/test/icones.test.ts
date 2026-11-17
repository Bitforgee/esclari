import { spawnSync } from 'node:child_process'
import { createHash } from 'node:crypto'
import { copyFileSync, existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join, resolve } from 'node:path'
import { inflateSync } from 'node:zlib'
import { describe, expect, it } from 'vitest'

const raiz = process.cwd()
const caminho = (nome: string) => resolve(raiz, 'public/icons', nome)
const lerBytes = (nome: string) => readFileSync(caminho(nome))
const vite = readFileSync(resolve(raiz, 'vite.config.ts'), 'utf8')
const themeColor = vite.match(/theme_color:\s*'#([0-9a-fA-F]{6})'/)![1]
const corDoManifesto = [0, 2, 4].map((i) => parseInt(themeColor.slice(i, i + 2), 16))
const corDaMarca = [0xc8, 0x3a, 0x23]

type Imagem = { largura: number; altura: number; pixel: (x: number, y: number) => number[]; alfa: (x: number, y: number) => number }

function decodificar(bytes: Buffer): Imagem {
  let posicao = 8
  let largura = 0
  let altura = 0
  let canais = 3
  const dados: Buffer[] = []
  while (posicao < bytes.length) {
    const tamanho = bytes.readUInt32BE(posicao)
    const tipo = bytes.toString('ascii', posicao + 4, posicao + 8)
    const corpo = bytes.subarray(posicao + 8, posicao + 8 + tamanho)
    if (tipo === 'IHDR') {
      largura = corpo.readUInt32BE(0)
      altura = corpo.readUInt32BE(4)
      canais = corpo[9] === 6 ? 4 : 3
    }
    if (tipo === 'IDAT') dados.push(corpo)
    posicao += 12 + tamanho
  }
  const bruto = inflateSync(Buffer.concat(dados))
  const linha = largura * canais
  const saida = Buffer.alloc(altura * linha)
  for (let y = 0; y < altura; y++) {
    const filtro = bruto[y * (linha + 1)]
    for (let i = 0; i < linha; i++) {
      const atual = bruto[y * (linha + 1) + 1 + i]
      const esquerda = i >= canais ? saida[y * linha + i - canais] : 0
      const cima = y > 0 ? saida[(y - 1) * linha + i] : 0
      const diagonal = y > 0 && i >= canais ? saida[(y - 1) * linha + i - canais] : 0
      const paeth = () => {
        const p = esquerda + cima - diagonal
        const pa = Math.abs(p - esquerda)
        const pb = Math.abs(p - cima)
        const pc = Math.abs(p - diagonal)
        return pa <= pb && pa <= pc ? esquerda : pb <= pc ? cima : diagonal
      }
      const previsto = [0, esquerda, cima, (esquerda + cima) >> 1, paeth()][filtro]
      saida[y * linha + i] = (atual + previsto) & 255
    }
  }
  return {
    largura,
    altura,
    pixel: (x, y) => [...saida.subarray(y * linha + x * canais, y * linha + x * canais + 3)],
    alfa: (x, y) => (canais === 4 ? saida[y * linha + x * canais + 3] : 255),
  }
}

const ehBranco = ([r, g, b]: number[]) => r >= 240 && g >= 240 && b >= 240
const parecida = (p: number[], cor: number[]) => p.every((v, i) => Math.abs(v - cor[i]) <= 3)
const ehCorDaMarca = (p: number[]) => parecida(p, corDaMarca)

describe('icones do PWA', () => {
  it('dimensoes dos tres icones', () => {
    const esperado: [string, number][] = [['icon-192.png', 192], ['icon-512.png', 512], ['icon-maskable-512.png', 512]]
    for (const [nome, lado] of esperado) {
      const imagem = decodificar(lerBytes(nome))
      expect([nome, imagem.largura, imagem.altura]).toEqual([nome, lado, lado])
    }
  })

  it('maskable dentro da zona segura', () => {
    const imagem = decodificar(lerBytes('icon-maskable-512.png'))
    const raio = 410 / 2
    let brancos = 0
    for (let y = 0; y < 512; y++)
      for (let x = 0; x < 512; x++) {
        const p = imagem.pixel(x, y)
        if (ehBranco(p)) {
          brancos++
          expect(Math.hypot(x + 0.5 - 256, y + 0.5 - 256), `${x},${y}`).toBeLessThanOrEqual(raio)
        } else if (Math.hypot(x + 0.5 - 256, y + 0.5 - 256) > raio) expect(ehCorDaMarca(p), `${x},${y}`).toBe(true)
      }
    expect(brancos).toBeGreaterThan(0)
    for (const [x, y] of [[0, 0], [511, 0], [0, 511], [511, 511]]) expect(ehCorDaMarca(imagem.pixel(x, y))).toBe(true)
  })

  it('icone any branco sobre a cor do manifesto', () => {
    const imagem = decodificar(lerBytes('icon-192.png'))
    expect(parecida(imagem.pixel(0, 0), corDoManifesto)).toBe(true)
    expect(ehCorDaMarca(imagem.pixel(96, 20))).toBe(true)
    let brancos = 0
    let simetrico = true
    for (let y = 0; y < 192; y++)
      for (let x = 0; x < 192; x++) {
        const p = imagem.pixel(x, y)
        if (ehBranco(p)) brancos++
        const oposto = imagem.pixel(191 - x, 191 - y)
        if (p.some((v, i) => Math.abs(v - oposto[i]) > 3)) simetrico = false
      }
    expect(brancos).toBeGreaterThan(0)
    expect(simetrico).toBe(false)
  })

  it('falha do chromium', () => {
    const nomes = ['icon-192.png', 'icon-512.png', 'icon-maskable-512.png']
    const hash = (nome: string) => createHash('sha256').update(lerBytes(nome)).digest('hex')
    const antes = nomes.map(hash)
    const vazia = mkdtempSync(join(tmpdir(), 'sem-chromium-'))
    const r = spawnSync(process.execPath, ['scripts/gerar-icones.mjs'], { cwd: raiz, encoding: 'utf8', env: { ...process.env, PLAYWRIGHT_BROWSERS_PATH: vazia } })
    expect(r.status).toBe(1)
    expect(r.stderr).toMatch(/Chromium/)
    expect(r.stderr).toMatch(/não consegui/)
    expect(nomes.map(hash)).toEqual(antes)
  }, 60_000)

  it('dimensoes dos quatro icones', () => {
    const esperado: [string, number][] = [['icon-192.png', 192], ['icon-512.png', 512], ['icon-maskable-512.png', 512], ['apple-touch-icon.png', 180]]
    for (const [nome, lado] of esperado) {
      const imagem = decodificar(lerBytes(nome))
      expect([nome, imagem.largura, imagem.altura]).toEqual([nome, lado, lado])
    }
  })

  it('maskable e apple sao opacos', () => {
    for (const nome of ['icon-maskable-512.png', 'apple-touch-icon.png']) {
      const imagem = decodificar(lerBytes(nome))
      for (let y = 0; y < imagem.altura; y++)
        for (let x = 0; x < imagem.largura; x++) expect(imagem.alfa(x, y), `${nome} ${x},${y}`).toBe(255)
    }
  })

  it('apple-touch-icon declarado', () => {
    expect(readFileSync(resolve(raiz, 'index.html'), 'utf8')).toContain('<link rel="apple-touch-icon" href="/icons/apple-touch-icon.png" />')
    const imagem = decodificar(lerBytes('apple-touch-icon.png'))
    expect(ehCorDaMarca(imagem.pixel(0, 0))).toBe(true)
    expect(ehCorDaMarca(imagem.pixel(179, 179))).toBe(true)
  })

  it('gera os quatro icones com exit 0', () => {
    const r = spawnSync(process.execPath, ['scripts/gerar-icones.mjs'], { cwd: raiz, encoding: 'utf8' })
    expect(r.status, r.stderr).toBe(0)
    for (const nome of ['icon-192.png', 'icon-512.png', 'icon-maskable-512.png', 'apple-touch-icon.png']) expect(r.stdout).toContain(`icones: gravado ${nome}`)
  }, 120_000)

  it('sem simbolo-pequeno.svg', () => {
    const nomes = ['icon-192.png', 'icon-512.png', 'icon-maskable-512.png', 'apple-touch-icon.png']
    const hash = (nome: string) => createHash('sha256').update(lerBytes(nome)).digest('hex')
    const antes = nomes.map(hash)
    const copia = mkdtempSync(join(raiz, '.tmp-sem-simbolo-'))
    try {
      mkdirSync(join(copia, 'scripts'))
      copyFileSync(resolve(raiz, 'scripts/gerar-icones.mjs'), join(copia, 'scripts/gerar-icones.mjs'))
      copyFileSync(resolve(raiz, 'vite.config.ts'), join(copia, 'vite.config.ts'))
      const r = spawnSync(process.execPath, [join(copia, 'scripts/gerar-icones.mjs')], { cwd: copia, encoding: 'utf8' })
      expect(r.status).toBe(1)
      expect(r.stderr).toContain('simbolo-pequeno.svg')
      expect(existsSync(join(copia, 'public/icons'))).toBe(false)
      expect(nomes.map(hash)).toEqual(antes)
    } finally {
      rmSync(copia, { recursive: true, force: true })
    }
  })
  it('margem dos icones any em Papel', () => {
    const r = spawnSync(process.execPath, ['scripts/gerar-icones.mjs'], { cwd: raiz, encoding: 'utf8' })
    expect(r.status, r.stderr).toBe(0)
    const Papel = [0xf7, 0xf5, 0xee]
    for (const [nome, lado] of [['icon-192.png', 192], ['icon-512.png', 512]] as const) {
      const imagem = decodificar(lerBytes(nome))
      for (const [x, y] of [[0, 0], [lado - 1, 0], [0, lado - 1], [lado - 1, lado - 1]]) expect(parecida(imagem.pixel(x, y), Papel), `${nome} ${x},${y}`).toBe(true)
    }
  }, 120_000)
  it('manifesto lista os tres icones', () => {
    const icones = [...vite.matchAll(/\{\s*src:\s*'([^']+)',\s*sizes:\s*'([^']+)',\s*type:\s*'image\/png'(?:,\s*purpose:\s*'([^']+)')?\s*\}/g)].map(([, src, , purpose]) => [src, purpose])
    expect(icones).toEqual([
      ['/icons/icon-192.png', 'any'],
      ['/icons/icon-512.png', 'any'],
      ['/icons/icon-maskable-512.png', 'maskable'],
    ])
  })

  it('gera quatro icones deterministas com o fio pequeno', () => {
    const nomes = ['icon-192.png', 'icon-512.png', 'icon-maskable-512.png', 'apple-touch-icon.png']
    const hash = (nome: string) => createHash('sha256').update(lerBytes(nome)).digest('hex')
    const primeira = spawnSync(process.execPath, ['scripts/gerar-icones.mjs'], { cwd: raiz, encoding: 'utf8' })
    expect(primeira.status, primeira.stderr).toBe(0)
    const hashes = nomes.map(hash)
    const segunda = spawnSync(process.execPath, ['scripts/gerar-icones.mjs'], { cwd: raiz, encoding: 'utf8' })
    expect(segunda.status, segunda.stderr).toBe(0)
    expect(nomes.map(hash)).toEqual(hashes)
    for (const [nome, lado] of [['icon-192.png', 192], ['icon-512.png', 512], ['icon-maskable-512.png', 512], ['apple-touch-icon.png', 180]] as const) {
      const imagem = decodificar(lerBytes(nome))
      expect([imagem.largura, imagem.altura], nome).toEqual([lado, lado])
      const cantoEsperado = nome.startsWith('icon-') && !nome.includes('maskable') ? corDoManifesto : corDaMarca
      expect(parecida(imagem.pixel(0, 0), cantoEsperado), nome).toBe(true)
      expect(ehCorDaMarca(imagem.pixel(Math.floor(lado / 2), Math.floor(lado * 0.1))), nome).toBe(true)
      let brancos = 0
      for (let y = 0; y < lado; y++) for (let x = 0; x < lado; x++) if (ehBranco(imagem.pixel(x, y))) brancos++
      expect(brancos, nome).toBeGreaterThan(0)
    }
  }, 120_000)

  it('icone gerado fica opaco e dentro da zona segura', () => {
    const r = spawnSync(process.execPath, ['scripts/gerar-icones.mjs'], { cwd: raiz, encoding: 'utf8' })
    expect(r.status, r.stderr).toBe(0)
    const maskable = decodificar(lerBytes('icon-maskable-512.png'))
    for (let y = 0; y < 512; y++)
      for (let x = 0; x < 512; x++) {
        expect(maskable.alfa(x, y), `maskable ${x},${y}`).toBe(255)
        if (ehBranco(maskable.pixel(x, y))) expect(Math.hypot(x + 0.5 - 256, y + 0.5 - 256), `${x},${y}`).toBeLessThanOrEqual(204.8)
      }
    const apple = decodificar(lerBytes('apple-touch-icon.png'))
    for (let y = 0; y < 180; y++) for (let x = 0; x < 180; x++) expect(apple.alfa(x, y), `apple ${x},${y}`).toBe(255)
  }, 120_000)
})
