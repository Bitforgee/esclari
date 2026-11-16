import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { abrir, api, chamado, instituicao, me } from '../test/util'

const aluno = { 'GET /api/auth/me': () => [200, me()] as [number, unknown], 'GET /api/instituicoes/univag': () => [200, instituicao()] as [number, unknown] }
const Antigo = /(?<![A-Za-z])One(?![a-z])/

afterEach(() => {
  window.innerWidth = 1024
  localStorage.clear()
  document.documentElement.className = ''
})

describe('marca no cabeçalho', () => {
  it('protocolo sem traducao: o cabeçalho com ESC-000123 leva translate no', async () => {
    const c = chamado({ numero: 'ESC-000123', solicitante: 'Ana', eventos: [], setores: [], categorias: [], acoes: [] })
    api({ 'GET /api/auth/me': () => [200, me({ papeis: ['ATENDENTE'], matriculas: [] })], 'GET /api/chamados/ESC-000123': () => [200, c] })
    abrir('/univag/chamados/ESC-000123')

    const protocolo = await screen.findAllByRole('heading', { name: 'ESC-000123', level: 1 })
    expect(protocolo[0]).toHaveAttribute('translate', 'no')
  })

  it('protocolo sem traducao: um título comum não leva translate', async () => {
    api({ 'GET /api/auth/me': () => [200, me({ papeis: ['ATENDENTE'], matriculas: [] })], })
    abrir('/univag/conta')

    const comum = await screen.findAllByRole('heading', { level: 1 })
    expect(comum.some((h) => h.hasAttribute('translate'))).toBe(false)
  })
})

describe('marca Esclari', () => {
  it('titulo da tela de acesso é Esclari e nenhum texto One aparece', () => {
    api({})
    abrir('/')
    expect(screen.getByRole('heading', { level: 1, name: 'Esclari' })).toBeInTheDocument()
    expect(document.body.textContent).not.toMatch(Antigo)
  })

  it('marca no shell: barra aberta e menu do celular de 360 px mostram Esclari', async () => {
    api(aluno)
    const aberta = abrir('/univag/conta')
    await screen.findByRole('button', { name: 'Alternar barra lateral' })
    const barra = document.querySelector('[data-sidebar=sidebar]') as HTMLElement
    expect(within(barra).getByText('Esclari')).toBeInTheDocument()
    aberta.unmount()

    window.innerWidth = 360
    api(aluno)
    abrir('/univag/trocar-senha')
    await userEvent.click(await screen.findByRole('button', { name: 'Alternar barra lateral' }))
    const menu = await screen.findByRole('dialog', { name: 'Menu' })
    expect(within(menu).getByText('Esclari')).toBeInTheDocument()
    expect(menu.textContent).not.toMatch(Antigo)
  })

  it('chaves esclari: tema e barra gravam esclari-tema e esclari-barra e nenhuma one-*', async () => {
    api(aluno)
    abrir('/univag/conta')
    await userEvent.click(await screen.findByRole('button', { name: 'Tema' }))
    await userEvent.click(await screen.findByRole('button', { name: 'Escuro' }))
    expect(localStorage.getItem('esclari-tema')).toBe('dark')
    await userEvent.keyboard('{Escape}')
    await userEvent.click(await screen.findByRole('button', { name: 'Alternar barra lateral' }))
    expect(localStorage.getItem('esclari-barra')).toBe('recolhida')
    const chaves = Object.keys(localStorage)
    expect(chaves.filter((c) => c.startsWith('one-'))).toEqual([])
    expect(chaves).toEqual(expect.arrayContaining(['esclari-tema', 'esclari-barra']))
  })
})
