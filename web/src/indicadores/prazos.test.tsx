import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { abrir, api, detalheVazio, me, type Rota } from '../test/util'
import type { Prazos } from './tipos'

const admin = me({ papeis: ['ADMINISTRADOR'], matriculas: [] })
const dados = {
  semDados: false, alunosAtivos: 12, conversas: 20, taxaAutoatendimento: 0.5, taxaEscalonamento: 0.2, taxaAbandono: 0.1, taxaPerguntasSemArtigo: 0.1,
  chamadosAbertos: 4, chamadosResolvidos: 12, tempoPrimeiraRespostaHoras: 3, tempoResolucaoHoras: 30, roteamentoCorreto: 0.9, taxaReabertura: 0.1,
  principaisCategorias: { porConversas: [], porChamados: [] }, indicadorPrincipal: 0.5,
}
const Atual = 'GET /api/indicadores/detalhe?de=2026-11-01&ate=2026-11-15'
const Anterior = 'GET /api/indicadores/detalhe?de=2026-10-17&ate=2026-10-31'
const prazos = (p: Partial<Prazos>) => ({ ...detalheVazio, prazos: { ...detalheVazio.prazos, ...p } })

function abrirPainel(rotas: Record<string, Rota>) {
  api({ 'GET /api/auth/me': () => [200, admin], 'GET /api/indicadores?de=2026-11-01&ate=2026-11-15': () => [200, dados], ...rotas })
  return abrir('/univag/indicadores')
}

const secao = async () => (await screen.findByRole('heading', { name: 'A equipe responde a tempo?' })).closest('section') as HTMLElement

describe('prazos no painel', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 15, 10, 0))
  })
  afterEach(() => {
    vi.useRealTimers()
  })

  it('meta e percentual', async () => {
    const melhor = abrirPainel({ [Atual]: () => [200, prazos({ primeiraRespostaNoPrazo: 0.82, resolucaoNoPrazo: null })], [Anterior]: () => [200, prazos({ primeiraRespostaNoPrazo: 0.75, resolucaoNoPrazo: 0.5 })] })
    const s = await secao()

    expect(await within(s).findByText('Meta: primeira resposta em 24 h; resolução em 120 h')).toBeInTheDocument()
    const primeira = (await within(s).findByText('82 % dentro da meta')).closest('dd')!
    expect(await within(primeira).findByText(/melhor/)).toHaveTextContent('▲ 7,0 pts · melhor')
    const resolucao = within(s).getByText('Resolução', { selector: 'dt' }).nextElementSibling!
    expect(resolucao).toHaveTextContent('—')
    expect(within(s).getAllByText(/dentro da meta/)).toHaveLength(1)
    melhor.unmount()

    abrirPainel({ [Atual]: () => [200, prazos({ primeiraRespostaNoPrazo: 0.82 })], [Anterior]: () => [200, prazos({ primeiraRespostaNoPrazo: 0.9 })] })
    const outra = await secao()
    const pior = (await within(outra).findByText('82 % dentro da meta')).closest('dd')!
    expect(await within(pior).findByText(/pior/)).toHaveTextContent('▼ 8,0 pts · pior')
  })

  it('em atraso agora', async () => {
    const com = abrirPainel({ [Atual]: () => [200, prazos({ emAtrasoAgora: 3, maisAntigoEmAtrasoDias: 5 })] })
    const s = await secao()

    expect(await within(s).findByText('Em atraso agora: 3')).toBeInTheDocument()
    expect(within(s).getByText('O mais antigo está atrasado há 5 dias')).toBeInTheDocument()
    expect(within(s).getByRole('link', { name: 'Ver chamados em atraso' })).toHaveAttribute('href', '/univag/chamados?atraso=true')
    expect(within(s).queryByText('Nenhum chamado em atraso')).not.toBeInTheDocument()
    com.unmount()

    abrirPainel({ [Atual]: () => [200, prazos({ emAtrasoAgora: 0, maisAntigoEmAtrasoDias: null })] })
    const sem = await secao()
    expect(await within(sem).findByText('Nenhum chamado em atraso')).toBeInTheDocument()
    expect(within(sem).queryByText(/Em atraso agora/)).not.toBeInTheDocument()
    expect(within(sem).queryByRole('link', { name: 'Ver chamados em atraso' })).not.toBeInTheDocument()
  })

  it('carregando e erro', async () => {
    const carregando = abrirPainel({ [Atual]: () => new Promise(() => {}) })
    const s = await secao()
    expect(s.querySelector('[data-esqueleto="prazos"]')).toBeInTheDocument()
    expect(within(s).queryByText(/^Meta:/)).not.toBeInTheDocument()
    carregando.unmount()

    let vez = 0
    abrirPainel({ [Atual]: () => (vez++ === 0 ? [500, { status: 500, detail: 'falhou' }] : [200, prazos({ emAtrasoAgora: 2, maisAntigoEmAtrasoDias: 1 })]) })
    const comErro = await secao()
    const tentar = await within(comErro).findByRole('button', { name: 'Tentar de novo' })
    expect(within(comErro).getByText(/Não foi possível carregar/)).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Qualidade do atendimento' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Resumo' })).toBeInTheDocument()
    await userEvent.click(tentar)
    expect(await within(comErro).findByText('Em atraso agora: 2')).toBeInTheDocument()
  })

  it('ver como tabela', async () => {
    abrirPainel({ [Atual]: () => [200, prazos({ primeiraRespostaNoPrazo: 0.82, resolucaoNoPrazo: 0.74 })], [Anterior]: () => [200, prazos({ primeiraRespostaNoPrazo: 0.75, resolucaoNoPrazo: null })] })
    const s = await secao()
    await within(s).findByText('82 % dentro da meta')

    await userEvent.click(within(s).getByRole('button', { name: 'Ver como tabela: Dentro da meta, por período' }))

    const tabela = within(s).getByRole('table', { name: 'Dentro da meta, por período' })
    const linhas = within(tabela).getAllByRole('row').map((r) => [...r.querySelectorAll('th, td')].map((c) => c.textContent))
    expect(linhas).toEqual([
      ['Medida', 'Meta', 'Período anterior', 'Este período'],
      ['Primeira resposta', '24\u00a0h', '75\u00a0%', '82\u00a0%'],
      ['Resolução', '120\u00a0h', '—', '74\u00a0%'],
    ])
    expect(within(s).getByRole('img', { name: /Gráfico de barras\. Dentro da meta, por período\./ })).toBeInTheDocument()
  })
})
