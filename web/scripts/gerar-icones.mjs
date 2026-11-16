import { readFileSync, mkdirSync, writeFileSync, renameSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { chromium } from '@playwright/test'

const raiz = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const destino = join(raiz, 'public', 'icons')
const MARCA = '#224fa7'
const fundo = readFileSync(join(raiz, 'vite.config.ts'), 'utf8').match(/theme_color:\s*'(#[0-9a-fA-F]{6})'/)?.[1]
if (!fundo) {
  console.error('icones: não achei theme_color em web/vite.config.ts.')
  process.exit(1)
}

const ICONES = [
  ['icon-192.png', 192, false],
  ['icon-512.png', 512, false],
  ['icon-maskable-512.png', 512, true],
]

function svg(tamanho, mascaravel) {
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
  const retangulos = barras.map(([bx, by, bl, ba]) => `<rect x="${bx}" y="${by}" width="${bl}" height="${ba}" fill="#ffffff" shape-rendering="crispEdges"/>`).join('')
  const margem = mascaravel ? 0 : tamanho * 0.06
  const placa = mascaravel
    ? `<rect width="${tamanho}" height="${tamanho}" fill="${MARCA}"/>`
    : `<rect width="${tamanho}" height="${tamanho}" fill="${fundo}"/><rect x="${margem}" y="${margem}" width="${tamanho - 2 * margem}" height="${tamanho - 2 * margem}" rx="${tamanho * 0.18}" fill="${MARCA}"/>`
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${tamanho}" height="${tamanho}" viewBox="0 0 ${tamanho} ${tamanho}">${placa}${retangulos}</svg>`
}

async function renderizar() {
  const navegador = await chromium.launch()
  try {
    const imagens = []
    for (const [nome, tamanho, mascaravel] of ICONES) {
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
      }, [svg(tamanho, mascaravel), tamanho])
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



