import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { abrir, api, instituicao, type Rota } from '../test/util'

const univag = { slug: 'univag', nome: 'Centro Universitário Várzea Grande', nomeCurto: 'Univag' }
const buscas = (chamadas: { url: string }[]) => chamadas.filter((c) => c.url.startsWith('/api/instituicoes?'))
const achou = (itens: unknown[]): Rota => () => [200, { itens }]

describe('escolha da instituição', () => {
  it('estado inicial', async () => {
    const chamadas = api({})
    abrir('/')
    expect(screen.getByRole('heading', { level: 1, name: 'Esclari' })).toBeInTheDocument()
    const campo = screen.getByLabelText('Sua instituição')
    expect(screen.getByText('Digite o nome ou a sigla da sua instituição.')).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /Univag/ })).not.toBeInTheDocument()
    await userEvent.type(campo, 'un')
    await new Promise((r) => setTimeout(r, 400))
    expect(buscas(chamadas)).toHaveLength(0)
    expect(screen.queryByText('Buscando…')).not.toBeInTheDocument()
  })

  it('busca com espera', async () => {
    const chamadas = api({ 'GET /api/instituicoes?busca=univ': achou([univag, { slug: 'outra', nome: 'Faculdade Outra', nomeCurto: null }]) })
    abrir('/')
    await userEvent.type(screen.getByLabelText('Sua instituição'), 'univ')
    expect(buscas(chamadas)).toHaveLength(0)
    const link = await screen.findByRole('link', { name: /Centro Universitário Várzea Grande/ })
    expect(link).toHaveAttribute('href', '/univag')
    expect(link).toHaveTextContent('Univag')
    expect(screen.getByRole('link', { name: 'Faculdade Outra' })).toHaveAttribute('href', '/outra')
    expect(buscas(chamadas).map((c) => c.url)).toEqual(['/api/instituicoes?busca=univ'])
  })

  it('mostra Buscando em região educada enquanto espera a resposta', async () => {
    let liberar: () => void = () => {}
    api({ 'GET /api/instituicoes?busca=univ': () => new Promise((r) => (liberar = () => r([200, { itens: [univag] }]))) })
    abrir('/')
    await userEvent.type(screen.getByLabelText('Sua instituição'), 'univ')
    const aviso = await screen.findByText('Buscando…')
    expect(aviso.closest('[aria-live]')).toHaveAttribute('aria-live', 'polite')
    liberar()
    await screen.findByRole('link', { name: /Univag/ })
    expect(screen.queryByText('Buscando…')).not.toBeInTheDocument()
  })

  it('estados da busca', async () => {
    const chamadas = api({
      'GET /api/instituicoes?busca=zzz': achou([]),
      'GET /api/instituicoes?busca=rede': () => [503, { status: 503, detail: 'fora' }],
      'GET /api/instituicoes?busca=redes': achou([univag]),
      'GET /api/instituicoes?busca=many': () => [429, { status: 429, detail: 'Muitas tentativas.' }],
    })
    abrir('/')
    const campo = screen.getByLabelText('Sua instituição')
    await userEvent.type(campo, 'zzz')
    expect(await screen.findByText('Nenhuma instituição encontrada para “zzz”. Confira o nome ou peça o endereço à sua instituição.')).toBeInTheDocument()

    await userEvent.clear(campo)
    await userEvent.type(campo, 'rede')
    expect(await screen.findByText('Não foi possível buscar agora. Verifique a conexão e tente de novo.')).toBeInTheDocument()
    await userEvent.type(campo, 's')
    await screen.findByRole('link', { name: /Univag/ })
    expect(buscas(chamadas).map((c) => c.url)).toContain('/api/instituicoes?busca=redes')

    await userEvent.clear(campo)
    await userEvent.type(campo, 'many')
    expect(await screen.findByText('Muitas buscas agora. Aguarde um minuto e tente de novo.')).toBeInTheDocument()
  })

  it('só a resposta do último texto aparece', async () => {
    api({ 'GET /api/instituicoes?busca=abc': achou([{ slug: 'a', nome: 'Alfa', nomeCurto: null }]), 'GET /api/instituicoes?busca=abcd': achou([{ slug: 'b', nome: 'Beta', nomeCurto: null }]) })
    abrir('/')
    const campo = screen.getByLabelText('Sua instituição')
    await userEvent.type(campo, 'abc')
    await screen.findByRole('link', { name: 'Alfa' })
    await userEvent.type(campo, 'd')
    await waitFor(() => expect(screen.getByRole('link', { name: 'Beta' })).toBeInTheDocument())
    expect(screen.queryByRole('link', { name: 'Alfa' })).not.toBeInTheDocument()
  })

  it('tocar no resultado abre o Login da instituição e a volta lembra dela', async () => {
    api({ 'GET /api/instituicoes/univag': () => [200, instituicao()], 'GET /api/instituicoes?busca=univ': achou([univag]) })
    abrir('/univag')
    await userEvent.click(await screen.findByRole('link', { name: 'Trocar de instituição' }))
    await userEvent.type(await screen.findByLabelText('Sua instituição'), 'univ')
    await userEvent.click(await screen.findByRole('link', { name: /Univag/ }))
    expect(await screen.findByLabelText('CPF')).toBeInTheDocument()
    expect(localStorage.getItem('esclari-instituicao')).toBe('univag')
  })
})
