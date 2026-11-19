import { waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import html from '../index.html?raw'
import { abrir, api, instituicao, me } from './test/util'

const base = new DOMParser().parseFromString(html, 'text/html')
const favicon = () => document.head.querySelector('link[rel="icon"]:not([type="image/svg+xml"])')

beforeEach(() => {
  document.head.innerHTML = [...base.head.children].map((e) => e.outerHTML).join('')
})

describe('ícone do app em JPEG', () => {
  it('favicon com o tipo do ícone', async () => {
    const comIcone = instituicao({ imagens: { logo: null, icone: 4, login: null, assistente: null }, iconeTipo: 'image/jpeg' })
    api({ 'GET /api/instituicoes/univag': () => [200, comIcone], 'GET /api/auth/me': () => [200, me()] })
    let montada = abrir('/univag/trocar-senha')
    await waitFor(() => expect(favicon()).toHaveAttribute('href', '/api/instituicoes/univag/imagens/icone?v=4'))
    expect(favicon()).toHaveAttribute('type', 'image/jpeg')
    montada.unmount()

    api({ 'GET /api/instituicoes/univag': () => [200, instituicao()], 'GET /api/auth/me': () => [200, me()] })
    montada = abrir('/univag/trocar-senha')
    await waitFor(() => expect(favicon()).toHaveAttribute('href', '/icons/icon-192.png'))
    expect(favicon()).toHaveAttribute('type', 'image/png')
    montada.unmount()
  })
})
