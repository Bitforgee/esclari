import { readFileSync, mkdirSync, writeFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { chromium } from '@playwright/test'

const raiz = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const destino = resolve(raiz, '..', 'Esclari.Api', 'Instituicoes', 'Aparencia', 'logo-horizontal-email.png')
const logo = readFileSync(join(raiz, 'public', 'marca', 'logo-horizontal.svg'))

const navegador = await chromium.launch()
try {
  const pagina = await navegador.newPage({ viewport: { width: 174, height: 40 }, deviceScaleFactor: 2 })
  await pagina.setContent(
    `<body style="margin:0;background:#ffffff"><img width="174" height="40" style="display:block" src="data:image/svg+xml;base64,${logo.toString('base64')}"></body>`,
  )
  await pagina.waitForFunction(() => document.images[0].complete)
  const png = await pagina.screenshot({ type: 'png', clip: { x: 0, y: 0, width: 174, height: 40 } })
  mkdirSync(dirname(destino), { recursive: true })
  writeFileSync(destino, png)
  console.log(`logo-email: ${destino} (${png.length} bytes)`)
} finally {
  await navegador.close()
}
