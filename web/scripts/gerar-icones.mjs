import { readFileSync, mkdirSync, writeFileSync, renameSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { chromium } from '@playwright/test'

const raiz = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const destino = join(raiz, 'public', 'icons')
const cor = readFileSync(join(raiz, 'vite.config.ts'), 'utf8').match(/theme_color:\s*'(#[0-9a-fA-F]{6})'/)?.[1]
if (!cor) {
  console.error('icones: não achei theme_color em web/vite.config.ts.')
  process.exit(1)
}

const ICONES = [
  ['icon-192.png', 192],
  ['icon-512.png', 512],
  ['icon-maskable-512.png', 512],
]

function svg(tamanho) {
  const x = tamanho * 0.3
  const y = tamanho * 0.25
  const largura = tamanho * 0.4
  const altura = tamanho * 0.5
  const traco = tamanho * 0.1
  const barras = [
    [x, y, traco, altura],
    [x, y, largura, traco],
    [x, y + altura / 2 - traco / 2, largura * 0.85, traco],
    [x, y + altura - traco, largura, traco],
  ]
  const retangulos = barras.map(([bx, by, bl, ba]) => `<rect x="${bx}" y="${by}" width="${bl}" height="${ba}" fill="#ffffff"/>`).join('')
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${tamanho}" height="${tamanho}" viewBox="0 0 ${tamanho} ${tamanho}"><rect width="${tamanho}" height="${tamanho}" fill="${cor}"/>${retangulos}</svg>`
}

async function renderizar() {
  const navegador = await chromium.launch()
  try {
    const imagens = []
    for (const [nome, tamanho] of ICONES) {
      const pagina = await navegador.newPage({ viewport: { width: tamanho, height: tamanho }, deviceScaleFactor: 1 })
      await pagina.setContent(`<!doctype html><html><body style="margin:0;background:${cor}">${svg(tamanho)}</body></html>`)
      imagens.push([nome, await pagina.screenshot({ type: 'png', clip: { x: 0, y: 0, width: tamanho, height: tamanho } })])
      await pagina.close()
    }
    return imagens
  } finally {
    await navegador.close()
  }
}

let imagens
try {
  imagens = await renderizar()
} catch (erro) {
  console.error(`icones: não consegui abrir o Chromium do Playwright (${String(erro.message).split('\n')[0]}). Rode "npx playwright install chromium" e tente de novo. Os ícones atuais não foram alterados.`)
  process.exit(1)
}

mkdirSync(destino, { recursive: true })
for (const [nome, png] of imagens) {
  const temporario = join(destino, `${nome}.tmp`)
  writeFileSync(temporario, png)
  renameSync(temporario, join(destino, nome))
  console.log(`icones: gravado ${nome}`)
}
