import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { abrir, api, me } from '../test/util'

const admin = me({ papeis: ['ADMINISTRADOR'], matriculas: [] })
const artigo = (id: number, titulo: string, estado: string, origemChamado: string | null) => ({
  id, estado, indexacao: null, categoriaId: 3, categoria: 'Financeiro', visivelDe: null, visivelAte: null, validoAte: '2027-11-17', validade: 'VIGENTE', origemChamado,
  versao: { id, numero: 1, titulo, perguntas: ['como tiro o boleto?'], resposta: 'r', links: [] },
})
const linhaDe = (titulo: string) => screen.getByText(titulo).closest('tr')!

describe('origem do artigo', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 17, 10, 0))
  })
  afterEach(() => {
    vi.useRealTimers()
  })

  it('etiqueta de origem', async () => {
    api({
      'GET /api/auth/me': () => [200, admin],
      'GET /api/admin/artigos': () => [200, [artigo(1, 'Segunda via do boleto', 'RASCUNHO', 'ESC-000123'), artigo(2, 'Abono de faltas', 'ATIVO', null)]],
      'GET /api/admin/categorias': () => [200, [{ id: 3, nome: 'Financeiro', ativa: true }]],
    })
    abrir('/univag/artigos')
    await screen.findByText('Segunda via do boleto')

    const doChamado = linhaDe('Segunda via do boleto')
    const titulo = doChamado.querySelector('td')!
    expect(within(titulo).getByText('Origem: ESC-000123')).toBeInTheDocument()
    expect(within(titulo).getByText('Vence em 17/11/2027')).toBeInTheDocument()
    expect(within(doChamado).getByText('Rascunho')).toBeInTheDocument()
    expect(within(linhaDe('Abono de faltas')).queryByText(/^Origem:/)).toBeNull()

    await userEvent.click(within(doChamado).getByRole('button', { name: 'Ações de Segunda via do boleto' }))
    expect(await screen.findByRole('menuitem', { name: 'Ativar' })).toBeInTheDocument()
  })

  it('filtro de situacao mostra os rascunhos', async () => {
    api({
      'GET /api/auth/me': () => [200, admin],
      'GET /api/admin/artigos': () => [200, [artigo(1, 'Segunda via do boleto', 'RASCUNHO', 'ESC-000123'), artigo(2, 'Abono de faltas', 'ATIVO', null), artigo(3, 'Antigo', 'ARQUIVADO', null)]],
      'GET /api/admin/categorias': () => [200, [{ id: 3, nome: 'Financeiro', ativa: true }]],
    })
    const montada = abrir('/univag/artigos')
    await screen.findByText('Segunda via do boleto')
    const filtro = screen.getByLabelText('Situação', { selector: 'select' })
    expect([...filtro.querySelectorAll('option')].map((o) => o.textContent)).toEqual(['Todos', 'Rascunhos', 'Ativos', 'Arquivados'])

    await userEvent.selectOptions(filtro, 'rascunho')
    expect(screen.getByText('Segunda via do boleto')).toBeInTheDocument()
    expect(screen.queryByText('Abono de faltas')).toBeNull()
    expect(screen.queryByText('Antigo')).toBeNull()

    await userEvent.selectOptions(screen.getByLabelText('Validade', { selector: 'select' }), 'vencidos')
    expect(screen.getByText('Nenhum artigo neste filtro de validade.')).toBeInTheDocument()
    expect(screen.getByLabelText('Situação', { selector: 'select' })).toHaveValue('rascunho')

    await userEvent.selectOptions(screen.getByLabelText('Validade', { selector: 'select' }), '')
    await userEvent.selectOptions(screen.getByLabelText('Situação', { selector: 'select' }), 'arquivado')
    expect(screen.getByText('Antigo')).toBeInTheDocument()
    expect(screen.queryByText('Segunda via do boleto')).toBeNull()
    montada.unmount()

    abrir('/univag/artigos?estado=rascunho')
    await screen.findByText('Segunda via do boleto')
    expect(screen.queryByText('Abono de faltas')).toBeNull()
  })
})
