import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { abrir, abrirComRoteador, api, me } from '../test/util'

const admin = me({ papeis: ['ADMINISTRADOR'], matriculas: [] })
const categorias = [{ id: 3, nome: 'Estágio', ativa: true }]

const artigo = (id: number, titulo: string, validoAte: string, validade: string, estado = 'ATIVO') => ({
  id, estado, indexacao: null, categoriaId: 3, categoria: 'Estágio', visivelDe: null, visivelAte: null, validoAte, validade,
  versao: { id, numero: 1, titulo, perguntas: [], resposta: 'r', links: [] },
})
const documento = (id: number, titulo: string, validoAte: string, validade: string) => ({
  id, titulo, categoriaId: 3, categoria: 'Estágio', estado: 'ATIVO', indexacao: 'INDEXADO', validoAte, validade,
  versao: { id: id * 10, numero: 1, nomeArquivo: 'edital.pdf', paginas: 2, criadaEm: '2026-11-02T12:00:00Z' },
})

const tresArtigos = [artigo(1, 'Faltas', '2027-11-17', 'VIGENTE'), artigo(2, 'Trancamento', '2026-11-29', 'VENCE_EM_BREVE'), artigo(3, 'Rematrícula', '2026-11-10', 'VENCIDO')]
const tresDocumentos = [documento(1, 'Edital', '2027-11-17', 'VIGENTE'), documento(2, 'Calendário', '2026-11-29', 'VENCE_EM_BREVE'), documento(3, 'Regulamento', '2026-11-10', 'VENCIDO')]

const linhaDe = (titulo: string) => screen.getByText(titulo).closest('tr')!
const servir = (lista: string, itens: unknown[], extra: Record<string, () => [number, unknown]> = {}) =>
  api({ 'GET /api/auth/me': () => [200, admin], [`GET /api/admin/${lista}`]: () => [200, itens], 'GET /api/admin/categorias': () => [200, categorias], ...extra })

describe('validade nas listas', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 17, 10, 0))
  })
  afterEach(() => {
    vi.useRealTimers()
  })

  it('etiqueta de validade', async () => {
    for (const [lista, itens] of [['artigos', tresArtigos], ['documentos', tresDocumentos]] as const) {
      servir(lista, itens)
      const montada = abrir(`/univag/${lista}`)
      const [vigente, emBreve, vencido] = lista === 'artigos' ? ['Faltas', 'Trancamento', 'Rematrícula'] : ['Edital', 'Calendário', 'Regulamento']
      await screen.findByText(vigente)

      expect(within(linhaDe(vigente)).getByText('Vence em 17/11/2027')).toBeInTheDocument()
      expect(within(linhaDe(emBreve)).getByText('Vence em 12 dias')).toBeInTheDocument()
      const etiqueta = within(linhaDe(vencido)).getByText('Vencido em 10/11/2026')
      expect(etiqueta.querySelector('svg')).not.toBeNull()
      expect(etiqueta).toHaveTextContent(/^Vencido em 10\/11\/2026$/)
      expect(within(linhaDe(vigente)).queryByRole('button', { name: /^Renovar/ })).toBeNull()
      expect(within(linhaDe(emBreve)).getByRole('button', { name: `Renovar ${emBreve}` })).toBeInTheDocument()
      expect(within(linhaDe(vencido)).getByRole('button', { name: `Renovar ${vencido}` })).toBeInTheDocument()
      montada.unmount()
    }
  })

  it('etiqueta de validade some do arquivado e diz vence hoje e em 1 dia', async () => {
    servir('artigos', [artigo(1, 'Hoje', '2026-11-17', 'VENCE_EM_BREVE'), artigo(2, 'Amanhã', '2026-11-18', 'VENCE_EM_BREVE'), artigo(3, 'Guardado', '2026-11-10', 'VENCIDO', 'ARQUIVADO')])
    abrir('/univag/artigos')
    await screen.findByText('Hoje')
    expect(within(linhaDe('Hoje')).getByText('Vence hoje')).toBeInTheDocument()
    expect(within(linhaDe('Amanhã')).getByText('Vence em 1 dia')).toBeInTheDocument()
    expect(within(linhaDe('Guardado')).getByText('Vencido em 10/11/2026')).toBeInTheDocument()
    expect(within(linhaDe('Guardado')).queryByRole('button', { name: /^Renovar/ })).toBeNull()
  })

  it('filtro de validade', async () => {
    servir('artigos', tresArtigos)
    const { roteador } = abrirComRoteador('/univag/artigos')
    await screen.findByText('Faltas')
    const filtro = screen.getByLabelText('Validade', { selector: 'select' })
    expect([...filtro.querySelectorAll('option')].map((o) => o.textContent)).toEqual(['Todos', 'Vencem em 30 dias', 'Vencidos'])

    await userEvent.selectOptions(filtro, 'em-30-dias')
    expect(screen.queryByText('Faltas')).toBeNull()
    expect(screen.getByText('Trancamento')).toBeInTheDocument()
    expect(screen.queryByText('Rematrícula')).toBeNull()
    expect(roteador.state.location.search).toBe('?validade=em-30-dias')

    await userEvent.selectOptions(filtro, 'vencidos')
    expect(screen.getByText('Rematrícula')).toBeInTheDocument()
    expect(screen.queryByText('Trancamento')).toBeNull()

    await userEvent.selectOptions(filtro, '')
    expect(screen.getByText('Faltas')).toBeInTheDocument()
    expect(roteador.state.location.search).toBe('')
  })

  it('filtro de validade vem da rota do e-mail', async () => {
    for (const [lista, vencido, vigente] of [['artigos', 'Rematrícula', 'Faltas'], ['documentos', 'Regulamento', 'Edital']] as const) {
      servir(lista, lista === 'artigos' ? tresArtigos : tresDocumentos)
      const montada = abrir(`/univag/${lista}?validade=vencidos`)
      await screen.findByText(vencido)
      expect(screen.queryByText(vigente)).toBeNull()
      expect(screen.getByLabelText('Validade', { selector: 'select' })).toHaveValue('vencidos')
      montada.unmount()
    }
    servir('artigos', [artigo(1, 'Faltas', '2027-11-17', 'VIGENTE')])
    abrir('/univag/artigos?validade=vencidos')
    expect(await screen.findByText('Nenhum artigo neste filtro de validade.')).toBeInTheDocument()
  })

  it('renovar sem confirmacao', async () => {
    let renovado = false
    const chamadas = servir('artigos', [], {
      'GET /api/admin/artigos': () => [200, [renovado ? artigo(3, 'Rematrícula', '2027-11-17', 'VIGENTE') : artigo(3, 'Rematrícula', '2026-11-10', 'VENCIDO')]],
      'POST /api/admin/artigos/3/renovar': () => {
        renovado = true
        return [200, artigo(3, 'Rematrícula', '2027-11-17', 'VIGENTE')]
      },
    })
    abrir('/univag/artigos')
    await screen.findByText('Vencido em 10/11/2026')

    await userEvent.click(screen.getByRole('button', { name: 'Renovar Rematrícula' }))

    expect(screen.queryByRole('dialog')).toBeNull()
    await waitFor(() => expect(screen.getByRole('region', { name: /^Notificações/ })).toHaveTextContent('Renovado até 17/11/2027'))
    expect(await within(linhaDe('Rematrícula')).findByText('Vence em 17/11/2027')).toBeInTheDocument()
    expect(screen.queryByText('Vencido em 10/11/2026')).toBeNull()
    expect(chamadas.filter((c) => c.metodo === 'POST').map((c) => c.url)).toEqual(['/api/admin/artigos/3/renovar'])
  })

  it('renovar documento sem confirmacao', async () => {
    let renovado = false
    const chamadas = servir('documentos', [], {
      'GET /api/admin/documentos': () => [200, [renovado ? documento(3, 'Regulamento', '2027-11-17', 'VIGENTE') : documento(3, 'Regulamento', '2026-11-29', 'VENCE_EM_BREVE')]],
      'POST /api/admin/documentos/3/renovar': () => {
        renovado = true
        return [200, documento(3, 'Regulamento', '2027-11-17', 'VIGENTE')]
      },
    })
    abrir('/univag/documentos')
    await screen.findByText('Vence em 12 dias')

    await userEvent.click(screen.getByRole('button', { name: 'Renovar Regulamento' }))

    expect(screen.queryByRole('dialog')).toBeNull()
    await waitFor(() => expect(screen.getByRole('region', { name: /^Notificações/ })).toHaveTextContent('Renovado até 17/11/2027'))
    expect(await screen.findByText('Vence em 17/11/2027')).toBeInTheDocument()
    expect(chamadas.filter((c) => c.metodo === 'POST').map((c) => c.url)).toEqual(['/api/admin/documentos/3/renovar'])
  })
})
