import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

const ler = (arquivo: string) => readFileSync(resolve(process.cwd(), arquivo), 'utf8')

describe('marca no manifesto e no título', () => {
  it('manifesto e titulo: vite.config.ts e index.html dizem Esclari', () => {
    const vite = ler('vite.config.ts')
    expect(vite).toMatch(/\bname:\s*'Esclari'/)
    expect(vite).toMatch(/short_name:\s*'Esclari'/)
    expect(ler('index.html')).toMatch(/<title>Esclari<\/title>/)
  })
})
