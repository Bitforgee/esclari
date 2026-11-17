import { existsSync, readFileSync, mkdirSync, writeFileSync, renameSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { chromium } from '@playwright/test'

const raiz = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const destino = join(raiz, 'public', 'icons')
const MARCA = '#C83A23'
const fundo = readFileSync(join(raiz, 'vite.config.ts'), 'utf8').match(/theme_color:\s*'(#[0-9a-fA-F]{6})'/)?.[1]
if (!fundo) {
  console.error('icones: não achei theme_color em web/vite.config.ts.')
  process.exit(1)
}

const arquivoDoSimbolo = join(raiz, 'public', 'marca', 'simbolo-pequeno.svg')
if (!existsSync(arquivoDoSimbolo)) {
  console.error('icones: não achei web/public/marca/simbolo-pequeno.svg. Os ícones atuais não foram alterados.')
  process.exit(1)
}
const fonteDoSimbolo = readFileSync(arquivoDoSimbolo, 'utf8')
const desenho = fonteDoSimbolo.match(/<svg[^>]*>([\s\S]*)<\/svg>/)?.[1]
const caixa = fonteDoSimbolo.match(/viewBox=["']([\d. -]+)["']/)?.[1].split(/\s+/).map(Number)
if (!desenho?.includes('<path') || !caixa || caixa.length !== 4 || caixa[2] !== caixa[3]) {
  console.error('icones: web/public/marca/simbolo-pequeno.svg não tem SVG quadrado com <path>. Os ícones atuais não foram alterados.')
  process.exit(1)
}
const [x, y, lado] = caixa

const ICONES = [
  ['icon-192.png', 192, 'any'],
  ['icon-512.png', 512, 'any'],
  ['icon-maskable-512.png', 512, 'maskable'],
  ['apple-touch-icon.png', 180, 'apple'],
]
const ESCALA = { any: 0.72, apple: 0.72, maskable: 0.68 }

function svg(tamanho, modo) {
  const u = tamanho / lado
  const centro = lado / 2
  const simbolo = `<g transform="translate(${tamanho / 2} ${tamanho / 2}) scale(${u * ESCALA[modo]}) translate(${-x - centro} ${-y - centro})">${desenho.replaceAll('currentColor', '#ffffff')}</g>`
  const margem = tamanho * 0.06
  const placa =
    modo === 'any'
      ? `<rect width="${tamanho}" height="${tamanho}" fill="${fundo}"/><rect x="${margem}" y="${margem}" width="${tamanho - 2 * margem}" height="${tamanho - 2 * margem}" rx="${tamanho * 0.18}" fill="${MARCA}"/>`
      : `<rect width="${tamanho}" height="${tamanho}" fill="${MARCA}"/>`
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${tamanho}" height="${tamanho}" viewBox="0 0 ${tamanho} ${tamanho}">${placa}${simbolo}</svg>`
}

async function renderizar() {
  const navegador = await chromium.launch()
  try {
    const imagens = []
    for (const [nome, tamanho, modo] of ICONES) {
      const pagina = await navegador.newPage()
      const dados = await pagina.evaluate(async ([conteudo, lado]) => {
        const imagem = new Image()
        imagem.src = `data:image/svg+xml;charset=utf-8,${encodeURIComponent(conteudo)}`
        await imagem.decode()
        const tela = document.createElement('canvas')
        tela.width = lado
        tela.height = lado
        tela.getContext('2d').drawImage(imagem, 0, 0, lado, lado)
        return tela.toDataURL('image/png')
      }, [svg(tamanho, modo), tamanho])
      imagens.push([nome, Buffer.from(dados.split(',')[1], 'base64')])
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



