import { screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { abrir, api, instituicao, me } from '../test/util'

const semMarca = instituicao()
const comIcone = instituicao({ imagens: { logo: null, icone: 2, login: null, assistente: null } })
const comLogo = instituicao({ imagens: { logo: 3, icone: null, login: null, assistente: null } })
const comFoto = instituicao({ imagens: { logo: null, icone: null, login: null, assistente: 4 } })
const Cor = '#' + '8a1538'
const comCor = instituicao({ cor: Cor })
const aluno = me()

afterEach(() => {
  localStorage.clear()
  document.documentElement.removeAttribute('style')
})

const imagens = () => [...document.querySelectorAll('img')].map((i) => i.getAttribute('src'))

describe('marca padrão do Esclari', () => {
  it('sem icone o cabecalho mostra o simbolo do Esclari', async () => {
    api({ 'GET /api/auth/me': () => [200, aluno], 'GET /api/instituicoes/univag': () => [200, semMarca] })
    abrir('/univag/trocar-senha')
    const nome = await screen.findByTestId('instituicao')
    const cabecalho = nome.closest('div')!.parentElement!
    expect(cabecalho.querySelector('img')).toHaveAttribute('src', '/marca/favicon.svg')
    expect(cabecalho.querySelector('img')).toHaveAttribute('alt', '')
    expect(cabecalho.textContent).not.toContain('U ')
    expect(screen.queryByText('U')).toBeNull()
  })

  it('com icone mostra o icone da instituicao', async () => {
    api({ 'GET /api/auth/me': () => [200, aluno], 'GET /api/instituicoes/univag': () => [200, comIcone] })
    abrir('/univag/trocar-senha')
    await screen.findByTestId('instituicao')
    await waitFor(() => expect(imagens()).toContain('/api/instituicoes/univag/imagens/icone?v=2'))
    expect(imagens()).not.toContain('/marca/favicon.svg')
  })

  it('entrada sem logo mostra a logo do Esclari', async () => {
    api({ 'GET /api/instituicoes/univag': () => [200, semMarca] })
    for (const [tema, arquivo] of [['light', 'logo-horizontal.svg'], ['dark', 'logo-horizontal-escuro.svg'], ['black', 'logo-horizontal-escuro.svg']]) {
      localStorage.setItem('esclari-tema', tema)
      const { unmount } = abrir('/univag')
      const titulo = await screen.findByRole('heading', { level: 1, name: 'Univag' })
      const logo = titulo.parentElement!.querySelector('img')!
      expect(logo, tema).toHaveAttribute('src', '/marca/' + arquivo)
      expect(logo, tema).toHaveAttribute('alt', 'Esclari')
      unmount()
    }
  })

  it('com logo mostra so a logo da instituicao', async () => {
    api({ 'GET /api/auth/me': () => [200, aluno], 'GET /api/instituicoes/univag': () => [200, comLogo] })
    const entrada = abrir('/univag')
    await screen.findByLabelText('CPF')
    expect(imagens()).toContain('/api/instituicoes/univag/imagens/logo?v=3')
    expect(imagens().filter((s) => s?.startsWith('/marca/'))).toEqual([])
    entrada.unmount()

    abrir('/univag/trocar-senha')
    await waitFor(() => expect(screen.getByAltText('Univag')).toHaveAttribute('src', '/api/instituicoes/univag/imagens/logo?v=3'))
    const visiveis = [...document.querySelectorAll('img')].filter((i) => !i.parentElement!.classList.contains('hidden') && !i.classList.contains('md:hidden'))
    expect(visiveis.map((i) => i.getAttribute('src')).filter((s) => s?.startsWith('/marca/'))).toEqual([])
  })

  it('logo propria no tema escuro fica sobre placa branca', async () => {
    api({ 'GET /api/instituicoes/univag': () => [200, comLogo] })
    for (const [tema, placa] of [['light', false], ['dark', true], ['black', true]] as const) {
      localStorage.setItem('esclari-tema', tema)
      const { unmount } = abrir('/univag')
      const logo = await screen.findByAltText('Univag')
      expect(logo.classList.contains('bg-white'), tema).toBe(placa)
      unmount()
    }
  })

  it('assistente sem foto usa o simbolo do Esclari', async () => {
    api({ 'GET /api/auth/me': () => [200, aluno], 'GET /api/instituicoes/univag': () => [200, semMarca] })
    const sem = abrir('/univag/assistente')
    await screen.findByText('Assistente virtual')
    await waitFor(() => expect(imagens()).toContain('/marca/favicon.svg'))
    sem.unmount()

    api({ 'GET /api/auth/me': () => [200, aluno], 'GET /api/instituicoes/univag': () => [200, comFoto] })
    abrir('/univag/assistente')
    await waitFor(() => expect(imagens()).toContain('/api/instituicoes/univag/imagens/assistente?v=4'))
  })

  it('cor propria nao troca a logo do Esclari', async () => {
    api({ 'GET /api/instituicoes/univag': () => [200, comCor] })
    abrir('/univag')
    const titulo = await screen.findByRole('heading', { level: 1, name: 'Univag' })
    expect(titulo.parentElement!.querySelector('img')).toHaveAttribute('src', '/marca/logo-horizontal.svg')
    await waitFor(() => expect(document.documentElement.style.getPropertyValue('--marca')).toBe(Cor))
  })

  it('iniciais so no avatar da pessoa', async () => {
    localStorage.setItem('esclari-barra', 'recolhida')
    api({ 'GET /api/auth/me': () => [200, me({ nome: 'Ana Souza' })], 'GET /api/instituicoes/univag': () => [200, semMarca] })
    abrir('/univag/trocar-senha')
    await screen.findByTestId('instituicao')
    const avatares = [...document.querySelectorAll('span[aria-hidden="true"]')].map((s) => s.textContent)
    expect(avatares).toContain('AS')
    expect(avatares).not.toContain('U')
  })
})
