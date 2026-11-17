import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { estados } from '../test/textos'
import { abrir, api, me, type Rota } from '../test/util'

const Indicadores = [
  'Indicadores', 'De', 'Até', 'Consultar', 'Sem atendimentos neste período.',
  'Resumo', 'Indicador principal', 'Resolvidas sem reabertura, sobre as conversas com desfecho.', 'de 30 conversas com desfecho',
  'O assistente está resolvendo?', 'Resolvida pelo assistente · 20 · 66,7\u00a0%', 'Transferida a um atendente · 5 · 16,7\u00a0%', 'Abandonada · 3 · 10\u00a0%', 'Encerrada pelo aluno · 2 · 6,7\u00a0%',
  ' · 1 ainda sem desfecho',
  'Autoatendimento', 'Escalonamento', 'Abandono', 'Perguntas sem artigo',
  'Resolvidas pelo assistente, sobre as conversas com desfecho.', 'Transferidas a um atendente, sobre as conversas com desfecho.', 'Conversas abandonadas, sobre as conversas com desfecho.', 'Conversas em que o assistente não achou fonte publicada.',
  'Quanto chega e como evolui?', 'Alunos ativos', 'Conversas', 'Chamados resolvidos',
  'Conversas por dia', 'Maior volume: 4 em 02/11', 'Chamados abertos e resolvidos por dia', 'Abertos · 2', 'Resolvidos · 1',
  'No período: 3 abertos e 1 resolvidos. Se a linha tracejada fica acima da contínua, a fila cresce.',
  '01/11', '02/11', 'Ver como tabela',
  'A equipe responde a tempo?', 'Meta: primeira resposta em 24\u00a0h; resolução em 120\u00a0h', 'Nenhum chamado em atraso', 'Primeira resposta', 'Resolução',
  'Dentro da meta, por período', 'Período anterior', 'Este período', 'Tempo até a 1ª resposta', 'Tempo de resolução', 'mediana 1,5\u00a0h', '9 em 10 foram atendidos em até 4\u00a0h', '—',
  'Chamados abertos agora: 4', 'Aberto · 1 · 25\u00a0%', 'Em atendimento · 1 · 25\u00a0%', 'Aguardando aluno · 1 · 25\u00a0%', 'Resolvido · 1 · 25\u00a0%', 'O mais antigo espera há 9 dias',
  'Qualidade do atendimento', 'Roteamento correto', 'Reabertura', 'Resolvidos sem encaminhamento entre setores.', 'Resolvidos que o aluno reabriu.',
  'O que mais pesa', 'Categorias por conversas', 'Categorias por chamados', 'Categoria', 'Total', 'Boleto3',
  'A base de conhecimento está em dia?', 'Nenhum conteúdo publicado ainda',
  '12,3\u00a0%', '50\u00a0%', '20\u00a0%', '10\u00a0%', '5', '12', '0', '2', '4',
]

const Inicial = 'GET /api/indicadores?de=2026-11-01&ate=2026-11-10'
const Detalhe = 'GET /api/indicadores/detalhe?de=2026-11-01&ate=2026-11-10'
const base = {
  semDados: false, alunosAtivos: 12, conversas: 5, taxaAutoatendimento: 0.5, taxaEscalonamento: 0.2, taxaAbandono: 0.1, taxaPerguntasSemArtigo: 0.1,
  chamadosAbertos: 4, chamadosResolvidos: 2, tempoPrimeiraRespostaHoras: 1.5, tempoResolucaoHoras: null, roteamentoCorreto: 0.5, taxaReabertura: 0.1,
  principaisCategorias: { porConversas: [{ categoria: 'Boleto', total: 3 }], porChamados: [] }, indicadorPrincipal: 0.123,
}
const detalhe = {
  granularidade: 'dia',
  pontos: [
    { inicio: '2026-11-01', conversas: 1, chamadosAbertos: 1, chamadosResolvidos: 0 },
    { inicio: '2026-11-02', conversas: 4, chamadosAbertos: 2, chamadosResolvidos: 1 },
  ],
  desfechos: { resolvidas: 20, transferidas: 5, abandonadas: 3, encerradas: 2, semDesfecho: 1 },
  chamadosPorEstado: { ABERTO: 1, EM_ATENDIMENTO: 1, AGUARDANDO_ALUNO: 1, RESOLVIDO: 1 },
  maisAntigoDias: 9,
  tempos: { primeiraRespostaMedianaHoras: 1.5, primeiraRespostaP90Horas: 4, resolucaoMedianaHoras: null, resolucaoP90Horas: null },
  prazos: { primeiraRespostaMetaHoras: 24, resolucaoMetaHoras: 120, primeiraRespostaNoPrazo: null, resolucaoNoPrazo: null, emAtrasoAgora: 0, maisAntigoEmAtrasoDias: null },
}
const tela = () => screen.getByRole('main')

function indicadores(rota: Rota, rotaDetalhe: Rota = () => [200, detalhe]) {
  api({ 'GET /api/auth/me': () => [200, me({ papeis: ['ADMINISTRADOR'], matriculas: [] })], [Inicial]: rota, [Detalhe]: rotaDetalhe })
  return abrir('/univag/indicadores')
}
describe('textos', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 10, 10, 0))
  })
  afterEach(() => {
    vi.useRealTimers()
  })

  it('textos Indicadores', async () => {
    const t = estados(Indicadores)

    let montada = indicadores(() => [200, { ...base, semDados: true }])
    await screen.findByText('Sem atendimentos neste período.')
    t.conferir(tela())
    montada.unmount()

    montada = indicadores(() => [200, base])
    await screen.findByText('de 30 conversas com desfecho')
    t.conferir(tela(), ['0'])

    montada.unmount()

    montada = indicadores(() => [500, { status: 500, detail: 'Falha ao calcular.' }])
    expect(await screen.findByRole('alert')).toHaveTextContent('Não foi possível carregar os indicadores. Verifique sua conexão.')
    t.conferir(tela(), ['Não foi possível carregar os indicadores. Verifique sua conexão.', 'Tentar de novo'])
    montada.unmount()

    t.todos()
  })
})
