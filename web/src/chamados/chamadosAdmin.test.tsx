import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { abrir, api, cabecalhos, chamado, me, type Chamada } from '../test/util'

const admin = me({ papeis: ['ADMINISTRADOR'], matriculas: [] })
const Todos = '/api/chamados/busca?escopo=todos'
const item = { numero: 'ESC-000001', titulo: 'boleto', estado: 'ABERTO', setor: 'Financeiro', categoria: 'Boleto', responsavel: null, criadoEm: '2026-11-02T12:00:00Z', dias: 1 }
const pagina = (itens: unknown[]) => ({ itens, total: itens.length, pagina: 1, porPagina: 50 })
const lido = (chamadas: Chamada[], url: string) => chamadas.some((c) => c.metodo === 'GET' && c.url === url)

describe('chamados do administrador', () => {
  it('menu do administrador', async () => {
    api({ 'GET /api/auth/me': () => [200, admin] })
    const { unmount } = abrir('/univag/trocar-senha')
    const grupo = within(await screen.findByRole('group', { name: 'Atendimento' }))
    expect(grupo.getByRole('link', { name: 'Chamados' })).toHaveAttribute('href', '/univag/chamados')
    unmount()

    api({ 'GET /api/auth/me': () => [200, me({ papeis: ['ATENDENTE'], matriculas: [] })] })
    abrir('/univag/trocar-senha')
    await screen.findByRole('group', { name: 'Atendimento' })
    expect(within(screen.getByRole('navigation')).queryByRole('link', { name: 'Chamados' })).not.toBeInTheDocument()
  })

  it('tela chamados', async () => {
    const vazia = api({ 'GET /api/auth/me': () => [200, admin], [`GET ${Todos}`]: () => [200, pagina([])], 'GET /api/chamados/filtros?escopo=todos': () => [200, { setores: [], categorias: [] }] })
    const { unmount } = abrir('/univag/chamados')
    expect(await screen.findByText('Nenhum chamado ainda.')).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1, name: 'Chamados' })).toBeInTheDocument()
    expect(lido(vazia, Todos)).toBe(true)
    expect(lido(vazia, '/api/chamados/filtros?escopo=todos')).toBe(true)
    unmount()

    api({ 'GET /api/auth/me': () => [200, admin], [`GET ${Todos}`]: () => [200, pagina([item])] })
    abrir('/univag/chamados')
    const linha = (await screen.findByText('ESC-000001')).closest('tr') as HTMLElement
    expect(cabecalhos()).toEqual(['Número', 'Título', 'Setor', 'Categoria', 'Responsável', 'Aberto', 'Estado'])
    expect(within(linha).getAllByRole('cell')[2]).toHaveTextContent(/^Financeiro$/)
  })

  it('detalhe em leitura', async () => {
    api({ 'GET /api/auth/me': () => [200, admin], 'GET /api/chamados/ESC-000001': () => [200, chamado({ estado: 'ABERTO', acoes: [], eventos: [], solicitante: 'Ana', resumo: 'x' })] })
    abrir('/univag/chamados/ESC-000001')
    await screen.findByRole('heading', { name: 'preciso do boleto' })
    expect(document.querySelector('textarea')).toBeNull()
    for (const nome of ['Enviar', 'Assumir', 'Resolver', 'Encaminhar', 'Mais ações']) expect(screen.queryByRole('button', { name: nome })).not.toBeInTheDocument()
  })
})
