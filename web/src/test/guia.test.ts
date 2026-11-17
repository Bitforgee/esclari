import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

const guia = readFileSync(resolve(process.cwd(), '../.specs/features/redesign-marca-esclari-final/guia-de-marca.md'), 'utf8')
const titulos = [...guia.matchAll(/^## (.+)$/gm)].map((m) => m[1].trim())
const secao = (nome: string) => guia.split(new RegExp(`^## ${nome}\\s*$`, 'm'))[1]?.split(/^## /m)[0] ?? ''

describe('guia de marca', () => {
  it('guia final cobre entrega uso e impressao', () => {
    expect(titulos).toEqual(['Ideia', 'Arquivos', 'Paleta', 'Contraste', 'Área de respeito', 'Tamanho mínimo', 'Fundos', 'Co-branding', 'Usos proibidos', 'Tipografia', 'Impressão'])
    for (const cor of ['Origem', 'Origem clara', 'Tinta', 'Papel']) {
      expect(secao('Paleta')).toMatch(new RegExp(`${cor}[^\\n]+#[0-9A-F]{6}[^\\n]+RGB[^\\n]+CMYK`, 'i'))
    }
    expect(secao('Impressão')).toContain('prova física')
    expect(secao('Impressão')).toContain('Pantone')
    expect(secao('Usos proibidos').split('\n').filter((l) => l.startsWith('- ')).length).toBeGreaterThanOrEqual(5)
    for (const item of secao('Usos proibidos').split('\n').filter((l) => l.startsWith('- '))) expect(item).toContain('porque')
  })

  it('guia final fixa quatro tamanhos minimos', () => {
    expect(secao('Área de respeito')).toContain('metade da largura expandida do Fio')
    expect(secao('Tamanho mínimo')).toMatch(/corte pequeno[^.]*16 px/i)
    expect(secao('Tamanho mínimo')).toMatch(/símbolo master[^.]*24 px/i)
    expect(secao('Tamanho mínimo')).toMatch(/logo horizontal[^.]*96 px/i)
    expect(secao('Tamanho mínimo')).toMatch(/logo vertical[^.]*72 px/i)
  })

  it('escreve o nome e os contratos do produto', () => {
    expect(guia).toContain('Esclari')
    expect(guia).toContain('ESC-NNNNNN')
    expect(guia).toContain('esclari.com.br')
    expect(guia).not.toContain('ESCLARI')
    for (const titulo of titulos) expect(titulo).not.toMatch(/^o Esclari/i)
  })
})
