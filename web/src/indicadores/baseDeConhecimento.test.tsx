import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { principalBase } from '../test/indicadores'
import { abrir, api, detalheVazio, me, validadeVazia, type Rota } from '../test/util'

const Titulo = 'A base de conhecimento está em dia?'

function servir(validade: Rota, papeis = ['ADMINISTRADOR']) {
  return api({
    'GET /api/auth/me': () => [200, me({ papeis, matriculas: [] })],
    'GET /api/indicadores?de=2026-11-01&ate=2026-11-15': () => [200, principalBase],
    'GET /api/indicadores/detalhe?de=2026-11-01&ate=2026-11-15': () => [200, detalheVazio],
    'GET /api/conhecimento/validade': validade,
  })
}

const resumo = { artigos: { vigentes: 8, venceEm30Dias: 2, vencidos: 1 }, documentos: { vigentes: 4, venceEm30Dias: 0, vencidos: 0 } }

describe('base de conhecimento no painel', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 15, 10, 0))
  })
  afterEach(() => {
    vi.useRealTimers()
  })

  it('secao com totais e link', async () => {
    servir(() => [200, resumo])
    abrir('/univag/indicadores')

    const secao = within(await screen.findByRole('region', { name: Titulo }))
    const medida = async (rotulo: string) => (await secao.findByText(rotulo)).parentElement!
    expect(await medida('Vigentes')).toHaveTextContent('12')
    expect(await medida('Vigentes')).toHaveTextContent('8 artigos e 4 documentos')
    expect(await medida('Vencem em 30 dias')).toHaveTextContent('2')
    expect(await medida('Vencidos')).toHaveTextContent('1')
    expect(await medida('Vencidos')).toHaveTextContent('1 artigo e 0 documentos')
    expect(secao.getByRole('link', { name: 'Ver vencidos' })).toHaveAttribute('href', '/univag/artigos?validade=vencidos')
    expect(secao.getByRole('link', { name: 'Ver documentos vencidos' })).toHaveAttribute('href', '/univag/documentos?validade=vencidos')
  })

  it('secao com totais e link fica fora para quem não é administrador', async () => {
    const chamadas = servir(() => [200, resumo], ['ATENDENTE'])
    abrir('/univag/indicadores')
    await screen.findByRole('region', { name: 'Resumo' })
    expect(screen.queryByRole('region', { name: Titulo })).toBeNull()
    expect(chamadas.map((c) => c.url)).not.toContain('/api/conhecimento/validade')
  })

  it('vazio', async () => {
    servir(() => [200, validadeVazia])
    abrir('/univag/indicadores')

    const regiao = await screen.findByRole('region', { name: Titulo })
    expect(await within(regiao).findByText('Nenhum conteúdo publicado ainda')).toBeInTheDocument()
    expect(regiao).not.toHaveTextContent(/\d/)
    expect(within(regiao).queryByRole('link')).toBeNull()
  })

  it('erro sem esconder o painel', async () => {
    let falhar = true
    servir(() => (falhar ? [500, { title: 'Erro', status: 500, detail: 'Erro interno.' }] : [200, resumo]))
    abrir('/univag/indicadores')

    const secao = within(await screen.findByRole('region', { name: Titulo }))
    expect(await secao.findByText(/Não foi possível carregar a validade da base de conhecimento/)).toBeInTheDocument()
    expect(screen.getByRole('region', { name: 'Resumo' })).toBeInTheDocument()
    expect(screen.getByRole('region', { name: 'O assistente está resolvendo?' })).toBeInTheDocument()

    falhar = false
    await userEvent.click(secao.getByRole('button', { name: 'Tentar de novo' }))
    expect(await secao.findByText('Vigentes')).toBeInTheDocument()
  })

  it('carregando', async () => {
    let responder!: () => void
    servir(() => new Promise((r) => (responder = () => r([200, resumo]))))
    abrir('/univag/indicadores')

    const regiao = await screen.findByRole('region', { name: Titulo })
    const status = within(regiao).getByRole('status', { name: 'Carregando' })
    expect(status.querySelectorAll('[data-slot="skeleton"]')).toHaveLength(3)
    expect(within(regiao).queryByText('Vigentes')).toBeNull()
    expect(regiao).not.toHaveTextContent(/\d/)

    responder()
    expect(await within(regiao).findByText('Vigentes')).toBeInTheDocument()
    expect(within(regiao).queryByRole('status')).toBeNull()
  })
})
