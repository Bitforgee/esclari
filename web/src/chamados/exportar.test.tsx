import { act, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { abrir, me } from '../test/util'

const atendente = me({ papeis: ['ATENDENTE'], matriculas: [] })
const json = (corpo: unknown, status = 200) => new Response(JSON.stringify(corpo), { status, headers: { 'Content-Type': status >= 400 ? 'application/problem+json' : 'application/json' } })
const pagina = { itens: [{ numero: 'ESC-000001', titulo: 'boleto', estado: 'FECHADO', setor: 'Triagem', categoria: 'Boleto', responsavel: null, criadoEm: '2026-11-02T12:00:00Z', dias: 1 }], total: 1, pagina: 1, porPagina: 50 }
const Exportacao = '/api/chamados/exportar?escopo=fila&estado=FECHADO'

function servidor(exportar: () => Promise<Response>) {
  const urls: string[] = []
  vi.stubGlobal(
    'fetch',
    vi.fn(async (url: string) => {
      urls.push(url)
      if (url === '/api/auth/me') return json(atendente)
      if (url === '/api/chamados/busca?escopo=fila&estado=FECHADO') return json(pagina)
      if (url === '/api/chamados/filtros?escopo=fila') return json({ setores: [], categorias: [] })
      if (url === Exportacao) return exportar()
      return json({ status: 404, detail: 'Recurso não encontrado.' }, 404)
    }),
  )
  return urls
}

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('exportar CSV', () => {
  it('exporta com os filtros da tela', async () => {
    let soltar: (r: Response) => void = () => {}
    const urls = servidor(() => new Promise((r) => (soltar = r)))
    const criar = vi.fn(() => 'blob:csv')
    const revogar = vi.fn()
    Object.assign(URL, { createObjectURL: criar, revokeObjectURL: revogar })
    const baixados: string[] = []
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      baixados.push(`${this.download} ${this.getAttribute('href')}`)
    })
    abrir('/univag/fila?estado=FECHADO')
    await screen.findByText('ESC-000001')

    await userEvent.click(screen.getByRole('button', { name: 'Exportar CSV' }))
    const botao = await screen.findByRole('button', { name: /Exportar CSV/ })
    await waitFor(() => expect(botao).toBeDisabled())
    expect(botao).toHaveAttribute('aria-busy', 'true')
    expect(botao.querySelector('[data-slot=spinner], [role=status], svg.animate-spin')).not.toBeNull()
    expect(urls).toContain(Exportacao)

    await act(async () =>
      soltar(new Response('\uFEFFNúmero;Título\r\n', { status: 200, headers: { 'Content-Type': 'text/csv; charset=utf-8', 'Content-Disposition': 'attachment; filename="chamados-univag-2026-11-10.csv"' } })),
    )
    await waitFor(() => expect(baixados).toEqual(['chamados-univag-2026-11-10.csv blob:csv']))
    expect(revogar).toHaveBeenCalledWith('blob:csv')
    await waitFor(() => expect(screen.getByRole('button', { name: 'Exportar CSV' })).toBeEnabled())
  })

  it('recusa vira aviso', async () => {
    servidor(async () => json({ status: 400, detail: 'A exportação passa de 10.000 chamados. Refine os filtros.' }, 400))
    abrir('/univag/fila?estado=FECHADO')
    await screen.findByText('ESC-000001')

    await userEvent.click(screen.getByRole('button', { name: 'Exportar CSV' }))

    expect(await screen.findByText('A exportação passa de 10.000 chamados. Refine os filtros.')).toBeInTheDocument()
  })
})
