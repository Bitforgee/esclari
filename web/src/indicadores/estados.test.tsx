import { screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { abrir, api, me, type Rota } from '../test/util'

const dados = {
  semDados: false, alunosAtivos: 12, conversas: 50, taxaAutoatendimento: 0.5, taxaEscalonamento: 0.2, taxaAbandono: 0.1, taxaPerguntasSemArtigo: 0.1,
  chamadosAbertos: 4, chamadosResolvidos: 20, tempoPrimeiraRespostaHoras: 1.5, tempoResolucaoHoras: 3, roteamentoCorreto: 0.5, taxaReabertura: 0.1,
  principaisCategorias: { porConversas: [{ categoria: 'Boleto', total: 3 }], porChamados: [] }, indicadorPrincipal: 0.712,
}
const Principal = 'GET /api/indicadores?de=2026-11-01&ate=2026-11-15'
const Detalhe = 'GET /api/indicadores/detalhe?de=2026-11-01&ate=2026-11-15'

describe('estados da tela', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 15, 10, 0))
  })
  afterEach(() => {
    vi.useRealTimers()
  })

  const abrirCom = (principal: object, detalhe?: Rota) => {
    api({ 'GET /api/auth/me': () => [200, me({ papeis: ['ADMINISTRADOR'], matriculas: [] })], [Principal]: () => [200, principal], ...(detalhe ? { [Detalhe]: detalhe } : {}) })
    abrir('/univag/indicadores')
  }

  it('sem atendimentos', async () => {
    abrirCom({ ...dados, semDados: true })

    expect(await screen.findByText('Sem atendimentos neste período.')).toBeInTheDocument()
    for (const titulo of ['Resumo', 'O assistente está resolvendo?', 'Quanto chega e como evolui?', 'A equipe responde a tempo?', 'Qualidade do atendimento', 'O que mais pesa'])
      expect(screen.queryByRole('region', { name: titulo }), titulo).not.toBeInTheDocument()
  })

  it('carregando', async () => {
    abrirCom(dados, () => new Promise(() => {}))

    await screen.findByRole('region', { name: 'Resumo' })
    for (const titulo of ['O assistente está resolvendo?', 'Quanto chega e como evolui?']) {
      const carregando = within(screen.getByRole('region', { name: titulo })).getByText('Carregando…')
      expect(carregando, titulo).toHaveClass('min-h-40')
    }
    const prazos = screen.getByRole('region', { name: 'A equipe responde a tempo?' })
    expect(prazos.querySelector('[data-esqueleto="prazos"]')).toBeInTheDocument()
    expect(within(prazos).getByRole('status', { name: 'Carregando' })).toBeInTheDocument()
  })

  it('erro so no detalhe', async () => {
    abrirCom(dados, () => [500, { status: 500, detail: 'Falha.' }])

    await waitFor(() => expect(screen.getAllByRole('alert')).toHaveLength(3))
    for (const alerta of screen.getAllByRole('alert')) {
      expect(alerta).toHaveTextContent('Não foi possível carregar os gráficos. Verifique sua conexão.')
      expect(within(alerta.parentElement as HTMLElement).getByRole('button', { name: 'Tentar de novo' })).toBeInTheDocument()
    }
    expect(within(screen.getByRole('region', { name: 'Resumo' })).getByText('71,2 %')).toBeInTheDocument()
    expect(within(screen.getByRole('region', { name: 'Qualidade do atendimento' })).getByText('Roteamento correto')).toBeInTheDocument()
    expect(within(screen.getByRole('region', { name: 'O que mais pesa' })).getByText('Categorias por conversas')).toBeInTheDocument()
  })
})
