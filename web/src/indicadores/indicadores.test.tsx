import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { abrir, api, detalheVazio, me } from '../test/util'

const admin = me({ papeis: ['ADMINISTRADOR'], matriculas: [] })

describe('indicadores', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('Sem dados no período', async () => {
    const vazio = {
      semDados: true, alunosAtivos: 0, conversas: 0, taxaAutoatendimento: null, taxaEscalonamento: null, taxaAbandono: null, taxaPerguntasSemArtigo: null,
      chamadosAbertos: 0, chamadosResolvidos: 0, tempoPrimeiraRespostaHoras: null, tempoResolucaoHoras: null, roteamentoCorreto: null, taxaReabertura: null,
      principaisCategorias: { porConversas: [], porChamados: [] }, indicadorPrincipal: null,
    }
    api({ 'GET /api/auth/me': () => [200, admin], 'GET /api/indicadores?de=2026-10-01&ate=2026-10-31': () => [200, vazio] })
    abrir('/univag/indicadores')
    await userEvent.clear(await screen.findByLabelText('De'))
    await userEvent.type(screen.getByLabelText('De'), '2026-10-01')
    await userEvent.clear(screen.getByLabelText('Até'))
    await userEvent.type(screen.getByLabelText('Até'), '2026-10-31')
    await userEvent.click(screen.getByRole('button', { name: 'Consultar' }))

    expect(await screen.findByText('Sem atendimentos neste período.')).toBeInTheDocument()
    expect(screen.queryByText('Autoatendimento')).not.toBeInTheDocument()
  })

  it('Consultar mostra o Spinner enquanto busca', async () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 15, 10, 0))
    let responder!: () => void
    const vazio = {
      semDados: true, alunosAtivos: 0, conversas: 0, taxaAutoatendimento: null, taxaEscalonamento: null, taxaAbandono: null, taxaPerguntasSemArtigo: null,
      chamadosAbertos: 0, chamadosResolvidos: 0, tempoPrimeiraRespostaHoras: null, tempoResolucaoHoras: null, roteamentoCorreto: null, taxaReabertura: null,
      principaisCategorias: { porConversas: [], porChamados: [] }, indicadorPrincipal: null,
    }
    let vez = 0
    api({
      'GET /api/auth/me': () => [200, admin],
      'GET /api/indicadores?de=2026-11-01&ate=2026-11-15': () => (vez++ === 0 ? [200, vazio] : new Promise((resolve) => (responder = () => resolve([200, vazio])))),
    })
    abrir('/univag/indicadores')
    await screen.findByText('Sem atendimentos neste período.')
    const consultar = screen.getByRole('button', { name: 'Consultar' })
    expect(consultar).toBeEnabled()
    expect(within(consultar).queryByRole('status', { hidden: true })).not.toBeInTheDocument()

    await userEvent.click(consultar)
    await waitFor(() => expect(consultar).toBeDisabled())
    expect(within(consultar).getByRole('status', { hidden: true })).toBeInTheDocument()
    responder()
    await waitFor(() => expect(consultar).toBeEnabled())
    expect(within(consultar).queryByRole('status', { hidden: true })).not.toBeInTheDocument()
  })

  const Detalhe = 'GET /api/indicadores/detalhe?de=2026-11-01&ate=2026-11-15'
  const detalheComTempo = { ...detalheVazio, tempos: { primeiraRespostaMedianaHoras: 1.5, primeiraRespostaP90Horas: 4, resolucaoMedianaHoras: null, resolucaoP90Horas: null } }
  const valorDe = async (rotulo: string) => (await screen.findByText(rotulo, { selector: 'dt' })).nextElementSibling!.textContent

  it('as medidas formatam taxa, horas e número, e o ranking vazio mostra traço', async () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 15, 10, 0))
    const dados = {
      semDados: false, alunosAtivos: 12, conversas: null, taxaAutoatendimento: 0.456, taxaEscalonamento: null, taxaAbandono: 0, taxaPerguntasSemArtigo: 1,
      chamadosAbertos: 4, chamadosResolvidos: 0, tempoPrimeiraRespostaHoras: 1.5, tempoResolucaoHoras: null, roteamentoCorreto: 0.5, taxaReabertura: null,
      principaisCategorias: { porConversas: [{ categoria: 'Boleto', total: 3 }], porChamados: [] }, indicadorPrincipal: 0.123,
    }
    const chamadas = api({ 'GET /api/auth/me': () => [200, admin], 'GET /api/indicadores?de=2026-11-01&ate=2026-11-15': () => [200, dados], [Detalhe]: () => [200, detalheComTempo] })
    abrir('/univag/indicadores')

    const esperado: [string, string][] = [
      ['Indicador principal', '12,3\u00a0%'], ['Alunos ativos', '12'], ['Conversas', '—'], ['Autoatendimento', '45,6\u00a0%'], ['Escalonamento', '—'], ['Abandono', '0\u00a0%'],
      ['Perguntas sem artigo', '100\u00a0%'], ['Chamados resolvidos', '0'], ['Tempo até a 1ª resposta', 'mediana 1,5\u00a0h'], ['Tempo de resolução', '—'],
      ['Roteamento correto', '50\u00a0%'], ['Reabertura', '—'],
    ]
    for (const [rotulo, valor] of esperado) expect(await valorDe(rotulo), rotulo).toBe(valor)
    const conversas = screen.getByText('Categorias por conversas').closest('section') as HTMLElement
    expect(Array.from(conversas.querySelectorAll('tbody tr')).map((li) => li.textContent)).toEqual(['Boleto3'])
    const porChamados = screen.getByText('Categorias por chamados').closest('section') as HTMLElement
    expect(Array.from(porChamados.querySelectorAll('tbody tr')).map((li) => li.textContent)).toEqual(['—'])
    expect(screen.queryByText('Sem atendimentos neste período.')).not.toBeInTheDocument()
    expect(chamadas.filter((c) => c.url.startsWith('/api/indicadores')).map((c) => c.url.split('?')[0]).sort()).toEqual(['/api/indicadores', '/api/indicadores', '/api/indicadores/detalhe'])
    expect(conversas.parentElement).toHaveClass('grid-cols-1', '@4xl/main:grid-cols-2')
  })

  const dadosCheios = {
    semDados: false, alunosAtivos: 12, conversas: 5, taxaAutoatendimento: 0.456, taxaEscalonamento: 0.2, taxaAbandono: 0.1, taxaPerguntasSemArtigo: 0.1,
    chamadosAbertos: 4, chamadosResolvidos: 2, tempoPrimeiraRespostaHoras: 1.5, tempoResolucaoHoras: 3, roteamentoCorreto: 0.5, taxaReabertura: 0.1,
    principaisCategorias: { porConversas: null, porChamados: null }, indicadorPrincipal: 0.123,
  }

  it('as seções vêm na ordem das perguntas do gestor e cada medida na sua seção', async () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 15, 10, 0))
    api({ 'GET /api/auth/me': () => [200, admin], 'GET /api/indicadores?de=2026-11-01&ate=2026-11-15': () => [200, dadosCheios] })
    abrir('/univag/indicadores')
    await screen.findByText('Indicador principal')

    const secoes = screen.getAllByRole('region').filter((r) => r.querySelector(':scope > h2'))
    const termos = secoes.map((s) => [s.querySelector(':scope > h2')!.textContent, [...s.querySelectorAll('dt')].map((d) => d.textContent)])
    expect(termos).toEqual([
      ['Resumo', ['Indicador principal']],
      ['O assistente está resolvendo?', ['Autoatendimento', 'Escalonamento', 'Abandono', 'Perguntas sem artigo']],
      ['Quanto chega e como evolui?', ['Alunos ativos', 'Conversas', 'Chamados resolvidos']],
      ['A equipe responde a tempo?', ['Tempo até a 1ª resposta', 'Tempo de resolução']],
      ['Qualidade do atendimento', ['Roteamento correto', 'Reabertura']],
      ['O que mais pesa', []],
      ['A base de conhecimento está em dia?', []],
    ])
    expect(secoes[0].closest('form')).toBeNull()
    expect(screen.getByLabelText('De').closest('form')!.compareDocumentPosition(secoes[0]) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it('ranking que a API não manda não aparece', async () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 0, 9, 10, 0))
    const dados = {
      semDados: false, alunosAtivos: null, conversas: null, taxaAutoatendimento: null, taxaEscalonamento: null, taxaAbandono: null, taxaPerguntasSemArtigo: null,
      chamadosAbertos: 0, chamadosResolvidos: 0, tempoPrimeiraRespostaHoras: null, tempoResolucaoHoras: null, roteamentoCorreto: null, taxaReabertura: null,
      principaisCategorias: { porConversas: null, porChamados: [{ categoria: 'Boleto', total: 1 }] }, indicadorPrincipal: null,
    }
    api({ 'GET /api/auth/me': () => [200, admin], 'GET /api/indicadores?de=2026-01-01&ate=2026-01-09': () => [200, dados] })
    abrir('/univag/indicadores')
    expect(await screen.findByText('Categorias por chamados')).toBeInTheDocument()
    expect(screen.queryByText('Categorias por conversas')).not.toBeInTheDocument()
  })

  describe('rankings e período', () => {
    const Inicial = 'GET /api/indicadores?de=2026-11-01&ate=2026-11-10'
    const dados = {
      semDados: false, alunosAtivos: 12, conversas: 5, taxaAutoatendimento: 0.5, taxaEscalonamento: 0.2, taxaAbandono: 0.1, taxaPerguntasSemArtigo: 0.1,
      chamadosAbertos: 4, chamadosResolvidos: 2, tempoPrimeiraRespostaHoras: 1.5, tempoResolucaoHoras: 3, roteamentoCorreto: 0.5, taxaReabertura: 0.1,
      principaisCategorias: { porConversas: [{ categoria: 'Boleto', total: 3 }, { categoria: 'Matrícula', total: 1 }], porChamados: [] }, indicadorPrincipal: 0.123,
    }

    function indicadores() {
      vi.useFakeTimers({ toFake: ['Date'] })
      vi.setSystemTime(new Date(2026, 10, 10, 10, 0))
      const chamadas = api({ 'GET /api/auth/me': () => [200, admin], [Inicial]: () => [200, dados], 'GET /api/indicadores?de=2026-11-03&ate=2026-11-17': () => [200, dados] })
      abrir('/univag/indicadores')
      return chamadas
    }
    const cartao = async (titulo: string) => (await screen.findByText(titulo)).closest('section') as HTMLElement
    const barras = (c: HTMLElement) => c.querySelectorAll('[data-barra]')

    it('ranking com barras na própria tabela', async () => {
      indicadores()
      const conversas = await cartao('Categorias por conversas')
      expect(within(conversas).getByRole('table')).toBeInTheDocument()
      expect(barras(conversas)).toHaveLength(2)
      expect(conversas.querySelector('.recharts-responsive-container')).toBeNull()
    })

    it('ranking vazio sem barras', async () => {
      indicadores()
      await cartao('Categorias por conversas')
      const chamados = await cartao('Categorias por chamados')
      expect(within(chamados).getByText('—')).toBeInTheDocument()
      expect(barras(chamados)).toHaveLength(0)
    })

    it('rankings em tabela Categoria e Total', async () => {
      indicadores()
      for (const titulo of ['Categorias por conversas', 'Categorias por chamados']) {
        const tabela = within(await cartao(titulo)).getByRole('table')
        expect(within(tabela).getAllByRole('columnheader').map((h) => h.textContent)).toEqual(['Categoria', 'Total'])
      }
      const linhas = within(within(await cartao('Categorias por conversas')).getByRole('table')).getAllByRole('row').slice(1)
      expect(linhas.map((l) => within(l).getAllByRole('cell').map((c) => c.textContent))).toEqual([
        ['Boleto', '3'],
        ['Matrícula', '1'],
      ])
    })

    it('período pelos campos de data só consulta no Consultar', async () => {
      const chamadas = indicadores()
      await screen.findByText('Indicador principal')
      for (const [rotulo, valor] of [['De', '2026-11-03'], ['Até', '2026-11-17']]) {
        await userEvent.clear(screen.getByLabelText(rotulo))
        await userEvent.type(screen.getByLabelText(rotulo), valor)
      }
      const consultas = () => chamadas.filter((c) => c.url.startsWith('/api/indicadores?')).map((c) => c.url)
      expect(consultas()).toEqual(['/api/indicadores?de=2026-11-01&ate=2026-11-10', '/api/indicadores?de=2026-10-22&ate=2026-10-31'])
      await userEvent.click(screen.getByRole('button', { name: 'Consultar' }))
      await waitFor(() =>
        expect(consultas()).toEqual([
          '/api/indicadores?de=2026-11-01&ate=2026-11-10',
          '/api/indicadores?de=2026-10-22&ate=2026-10-31',
          '/api/indicadores?de=2026-11-03&ate=2026-11-17',
          '/api/indicadores?de=2026-10-19&ate=2026-11-02',
        ]),
      )
    })
  })
})
