import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

const guia = readFileSync(resolve(process.cwd(), '../.specs/features/marca-esclari/guia-de-marca.md'), 'utf8')
const titulos = [...guia.matchAll(/^## (.+)$/gm)].map((m) => m[1].trim())
const secao = (nome: string) => guia.split(new RegExp(`^## ${nome}\\s*$`, 'm'))[1]?.split(/^## /m)[0] ?? ''

describe('guia de marca', () => {
  it('tem os nove titulos', () => {
    expect(titulos).toEqual(['Nome', 'Símbolo', 'Logo horizontal', 'Cor', 'Área de respeito', 'Tamanho mínimo', 'Usos proibidos', 'Tipografia', 'Voz'])
  })

  it('traz as medidas e a cor', () => {
    expect(secao('Área de respeito')).toContain('metade da altura do símbolo')
    expect(secao('Tamanho mínimo')).toMatch(/símbolo[^.]*16 px/)
    expect(secao('Tamanho mínimo')).toMatch(/logo horizontal[^.]*96 px de largura/)
    expect(secao('Cor')).toContain('#' + '2438d6')
  })

  it('usos proibidos sao 5 com razao', () => {
    const itens = secao('Usos proibidos').split('\n').filter((l) => l.startsWith('- '))
    expect(itens.length).toBeGreaterThanOrEqual(5)
    for (const item of itens) expect(item, item).toContain('porque')
  })

  it('escreve o nome do jeito certo', () => {
    expect(guia).toContain('Esclari')
    expect(guia).toContain('ESC-NNNNNN')
    expect(guia).toContain('esclari.com.br')
    expect(guia).not.toContain('ESCLARI')
    for (const titulo of titulos) expect(titulo).not.toMatch(/^o Esclari/i)
  })
})
