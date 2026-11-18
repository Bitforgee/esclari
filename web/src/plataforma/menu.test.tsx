import { cleanup, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { abrir, api, me } from '../test/util'

const operador = { id: 1, nome: 'Marina Souza', email: 'marina@esclari.com.br' }
const auditoria = {
  itens: [{ acao: 'INSTITUICAO_DESATIVADA', operador: 'Marina Souza', instituicao: 'demo', resumo: 'Contrato encerrado', criadaEm: '2026-11-17T15:30:00Z' }],
  total: 1,
}

describe('menu do console e das instituições', () => {
  afterEach(() => cleanup())

  it('administrador de instituição não vê grupo Plataforma nem Instituições', async () => {
    for (const papeis of [['ADMINISTRADOR'], ['ATENDENTE'], ['ALUNO']]) {
      api({ 'GET /api/auth/me': () => [200, me({ papeis })] })
      abrir('/univag/trocar-senha')
      const barra = await screen.findByRole('navigation')
      expect(within(barra).queryByRole('group', { name: 'Plataforma' }), papeis.join()).not.toBeInTheDocument()
      expect(within(barra).queryByRole('link', { name: 'Instituições' }), papeis.join()).not.toBeInTheDocument()
      cleanup()
    }
  })

  it('casca do console tem a marca do Esclari e só Instituições e Auditoria', async () => {
    api({ 'GET /api/plataforma/eu': () => [200, operador], 'GET /api/plataforma/auditoria?pagina=1': () => [200, auditoria] })
    abrir('/plataforma/auditoria')
    const nav = await screen.findByRole('navigation', { name: 'Navegação' })
    expect(within(nav).getAllByRole('link').map((l) => [l.textContent, l.getAttribute('href')])).toEqual([
      ['Instituições', '/plataforma/instituicoes'],
      ['Auditoria', '/plataforma/auditoria'],
    ])
    expect(screen.getByRole('img', { name: 'Esclari' })).toHaveAttribute('src', '/marca/logo-horizontal.svg')
    expect(screen.getByRole('heading', { name: 'Auditoria', level: 1 })).toBeInTheDocument()
    expect(screen.getByText('Marina Souza', { selector: 'span' })).toBeInTheDocument()
  })

  it('auditoria mostra a ação em português e o estado vazio', async () => {
    api({ 'GET /api/plataforma/eu': () => [200, operador], 'GET /api/plataforma/auditoria?pagina=1': () => [200, auditoria] })
    abrir('/plataforma/auditoria')
    const linha = (await screen.findByText('Instituição desativada')).closest('tr') as HTMLElement
    expect(within(linha).getAllByRole('cell')[0]).toHaveTextContent('17/11/2026')
    expect(within(linha).getByText('Contrato encerrado')).toBeInTheDocument()
    expect(within(linha).getByText('/demo')).toBeInTheDocument()
    cleanup()

    api({ 'GET /api/plataforma/eu': () => [200, operador], 'GET /api/plataforma/auditoria?pagina=1': () => [200, { itens: [], total: 0 }] })
    abrir('/plataforma/auditoria')
    expect(await screen.findByText('Ainda não há ações registradas.')).toBeInTheDocument()
  })

  it('auditoria com mais de 50 registros pagina para os mais antigos e volta', async () => {
    const pagina = (n: number) => ({ itens: [{ ...auditoria.itens[0], resumo: `Registro da página ${n}` }], total: 120 })
    const chamadas = api({
      'GET /api/plataforma/eu': () => [200, operador],
      'GET /api/plataforma/auditoria?pagina=1': () => [200, pagina(1)],
      'GET /api/plataforma/auditoria?pagina=2': () => [200, pagina(2)],
    })
    abrir('/plataforma/auditoria')
    expect((await screen.findAllByText('Registro da página 1')).length).toBeGreaterThan(0)
    expect(screen.getByRole('button', { name: 'Mais recentes' })).toBeDisabled()
    await userEvent.click(screen.getByRole('button', { name: 'Mais antigos' }))
    expect((await screen.findAllByText('Registro da página 2')).length).toBeGreaterThan(0)
    await userEvent.click(screen.getByRole('button', { name: 'Mais recentes' }))
    expect((await screen.findAllByText('Registro da página 1')).length).toBeGreaterThan(0)
    expect(chamadas.filter((c) => c.url.includes('auditoria')).map((c) => c.url.at(-1))).toEqual(['1', '2', '1'])
  })

  it('/plataforma sem caminho abre as instituições', async () => {
    api({ 'GET /api/plataforma/eu': () => [200, operador], 'GET /api/plataforma/instituicoes': () => [200, []] })
    abrir('/plataforma')
    expect(await screen.findByRole('heading', { name: 'Instituições', level: 1 })).toBeInTheDocument()
  })
})
