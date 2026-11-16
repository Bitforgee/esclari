import { screen, waitFor } from '@testing-library/react'
import { readFileSync, readdirSync } from 'node:fs'
import { join, resolve } from 'node:path'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import html from '../../index.html?raw'
import { abrir, api, instituicao, me } from '../test/util'

const base = new DOMParser().parseFromString(html, 'text/html')
const noHead = (seletor: string) => document.head.querySelector(seletor)
const iconeDaInstituicao = '/api/instituicoes/univag/imagens/icone?v=2'
const comIdentidade = instituicao({ nomeCurto: 'Univag', imagens: { logo: 3, icone: 2, login: null, assistente: null } })

beforeEach(() => {
  document.head.innerHTML = [...base.head.children].map((e) => e.outerHTML).join('')
})
afterEach(() => {
  vi.useRealTimers()
  document.documentElement.removeAttribute('style')
  delete document.documentElement.dataset.titulo
})

function arquivos(pasta: string, extensao: RegExp): string[] {
  return readdirSync(pasta, { withFileTypes: true }).flatMap((e) => {
    const caminho = join(pasta, e.name)
    if (e.isDirectory()) return ['bin', 'obj', 'wwwroot', 'node_modules'].includes(e.name) ? [] : arquivos(caminho, extensao)
    return extensao.test(e.name) ? [caminho] : []
  })
}

describe('marca do Esclari', () => {
  it('raiz mostra o logo dentro do h1', async () => {
    abrir('/')
    const titulo = await screen.findByRole('heading', { level: 1, name: 'Esclari' })
    const logo = titulo.querySelector('img')!
    expect(logo).toHaveAttribute('src', '/marca/logo-horizontal.svg')
    expect(logo).toHaveAttribute('alt', 'Esclari')
  })

  it('raiz troca o logo no tema escuro', async () => {
    for (const [tema, arquivo] of [['light', 'logo-horizontal.svg'], ['dark', 'logo-horizontal-escuro.svg'], ['black', 'logo-horizontal-escuro.svg']]) {
      localStorage.setItem('esclari-tema', tema)
      const { unmount } = abrir('/')
      const titulo = await screen.findByRole('heading', { level: 1, name: 'Esclari' })
      expect(titulo.querySelector('img'), tema).toHaveAttribute('src', '/marca/' + arquivo)
      unmount()
    }
    localStorage.clear()
  })

  it('marca do Esclari so aparece na raiz', async () => {
    api({ 'GET /api/instituicoes/univag': () => [200, comIdentidade] })
    abrir('/univag')
    await screen.findByLabelText('CPF')
    for (const imagem of document.querySelectorAll('img')) expect(imagem.getAttribute('src')).not.toMatch(/^\/marca\//)

    const web = arquivos(resolve(process.cwd(), 'src'), /\.tsx?$/).filter((a) => !/\.test\.tsx?$/.test(a) && !a.includes(join('src', 'test')))
    const citam = web.filter((a) => readFileSync(a, 'utf8').includes('/marca/')).map((a) => a.split(/[\\/]/).pop())
    expect(citam.sort()).toEqual(['Acesso.tsx', 'aparencia.tsx'])
    const api_ = arquivos(resolve(process.cwd(), '../Esclari.Api'), /\.cs$/)
    expect(api_.filter((a) => readFileSync(a, 'utf8').includes('/marca/'))).toEqual([])
  })

  it('favicon da marca some com icone da instituicao e volta sem ele', async () => {
    expect(noHead('link[rel="icon"][type="image/svg+xml"]')).not.toBeNull()
    api({ 'GET /api/instituicoes/univag': () => [200, comIdentidade], 'GET /api/auth/me': () => [200, me()] })
    const montada = abrir('/univag/assistente')
    await waitFor(() => expect(noHead('link[rel="icon"][type="image/png"]')).toHaveAttribute('href', iconeDaInstituicao))
    expect(noHead('link[rel="icon"][type="image/svg+xml"]')).toBeNull()
    montada.unmount()

    api({ 'GET /api/instituicoes/univag': () => [200, instituicao()], 'GET /api/auth/me': () => [200, me()] })
    abrir('/univag/assistente')
    await waitFor(() => expect(noHead('link[rel="icon"][type="image/png"]')).toHaveAttribute('href', '/icons/icon-192.png'))
    expect(noHead('link[rel="icon"][type="image/svg+xml"]')).toHaveAttribute('href', '/marca/favicon.svg')
  })
})
