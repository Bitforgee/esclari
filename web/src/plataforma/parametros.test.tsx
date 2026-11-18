import { cleanup, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { abrir, api, type Rota } from '../test/util'

const Base = '/api/plataforma/instituicoes/demo'
const p = (padrao: number, valor: number | null = null) => ({ valor, padrao, efetivo: valor ?? padrao })
const atuais = {
  abandonoMinutos: p(30, 45),
  confirmacaoDias: p(7),
  lembreteAguardandoDias: p(3),
  resolverAguardandoDias: p(7),
  validadeConhecimentoMeses: p(12),
  primeiraRespostaHoras: p(24),
  resolucaoHoras: p(120),
}
const detalhe = { slug: 'demo', nome: 'Faculdade Demo', situacao: 'ATIVA', prontidao: { itens: [], pronta: true } }

function tela(rotas: Record<string, Rota>) {
  const chamadas = api({
    'GET /api/plataforma/eu': () => [200, { id: 1, nome: 'Marina', email: 'marina@esclari.com.br' }],
    [`GET ${Base}`]: () => [200, detalhe],
    'GET /api/plataforma/modelos-de-carga': () => [200, []],
    [`GET ${Base}/equipe`]: () => [200, []],
    [`GET ${Base}/setores`]: () => [200, []],
    [`GET ${Base}/parametros`]: () => [200, atuais],
    ...rotas,
  })
  abrir('/plataforma/instituicoes/demo')
  return chamadas
}

const puts = (chamadas: { metodo: string; url: string; corpo: any }[]) => chamadas.filter((c) => c.metodo === 'PUT').map((c) => [c.url, c.corpo])

async function abrirAba() {
  await userEvent.click(await screen.findByRole('tab', { name: 'Parâmetros' }))
  return within(await screen.findByRole('tabpanel'))
}

describe('parâmetros da instituição', () => {
  afterEach(() => cleanup())

  it('aba parametros mostra o padrao e salva os sete', async () => {
    const chamadas = tela({ [`PUT ${Base}/parametros`]: (corpo) => [200, { ...atuais, confirmacaoDias: p(7, corpo.confirmacaoDias) }] })
    const aba = await abrirAba()

    const abandono = await aba.findByLabelText('Abandono da conversa (min)')
    expect(abandono).toHaveValue(45)
    expect(abandono).toHaveAccessibleDescription('Padrão: 30 min. Vazio usa o padrão.')
    const confirmacao = aba.getByLabelText('Prazo para o aluno confirmar a resolução (dias)')
    expect(confirmacao).toHaveValue(null)
    expect(confirmacao).toHaveAccessibleDescription('Padrão: 7 dias. Vazio usa o padrão.')
    expect(aba.getAllByRole('spinbutton')).toHaveLength(7)
    expect(aba.getByLabelText('Meta de resolução (h)')).toHaveAccessibleDescription('Padrão: 120 h. Vazio usa o padrão.')

    await userEvent.type(confirmacao, '2')
    await userEvent.click(aba.getByRole('button', { name: 'Salvar' }))

    expect(await aba.findByText('Parâmetros salvos.')).toBeInTheDocument()
    expect(puts(chamadas)).toEqual([
      [`${Base}/parametros`, { abandonoMinutos: 45, confirmacaoDias: 2, lembreteAguardandoDias: null, resolverAguardandoDias: null, validadeConhecimentoMeses: null, primeiraRespostaHoras: null, resolucaoHoras: null }],
    ])
    expect(confirmacao).toHaveValue(2)
  })

  it('erro no campo e usar padrao so ao salvar', async () => {
    let tentativa = 0
    const chamadas = tela({
      [`PUT ${Base}/parametros`]: () =>
        ++tentativa === 1
          ? [400, { status: 400, detail: 'O abandono deve ficar entre 5 e 1440.', errors: { abandonoMinutos: ['O abandono deve ficar entre 5 e 1440.'] } }]
          : [200, { ...atuais, abandonoMinutos: p(30) }],
    })
    const aba = await abrirAba()
    const abandono = await aba.findByLabelText('Abandono da conversa (min)')
    await userEvent.clear(abandono)
    await userEvent.type(abandono, '2')
    await userEvent.click(aba.getByRole('button', { name: 'Salvar' }))
    await waitFor(() => expect(abandono).toHaveAccessibleDescription('Padrão: 30 min. Vazio usa o padrão. O abandono deve ficar entre 5 e 1440.'))
    expect(aba.queryByText('Parâmetros salvos.')).not.toBeInTheDocument()

    await userEvent.click(aba.getByRole('button', { name: 'Usar padrão em Abandono da conversa' }))
    expect(abandono).toHaveValue(null)
    expect(puts(chamadas)).toHaveLength(1)
    await userEvent.click(aba.getByRole('button', { name: 'Salvar' }))
    expect(await aba.findByText('Parâmetros salvos.')).toBeInTheDocument()
    expect(puts(chamadas)[1][1]).toMatchObject({ abandonoMinutos: null })
  })
})
