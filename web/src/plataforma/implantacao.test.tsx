import { cleanup, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { abrir, api, type Rota } from '../test/util'

const operador = { id: 1, nome: 'Marina Souza', email: 'marina@esclari.com.br' }
const Base = '/api/plataforma/instituicoes/demo'
const pendente = { itens: [{ codigo: 'ADMINISTRADOR_COM_SENHA', ok: true }, { codigo: 'ATENDENTE_NO_SETOR', ok: false }, { codigo: 'CATEGORIA_COM_REGRA', ok: false }, { codigo: 'ARTIGO_ATIVO', ok: false }], pronta: false }
const detalhe = { slug: 'demo', nome: 'Faculdade Demo', situacao: 'CONFIGURANDO', prontidao: pendente }
const modelo = { id: 'padrao-esclari', nome: 'Padrão Esclari', descricao: 'Três setores, seis categorias e uma regra por categoria.', setores: 3, categorias: 6, regras: 6 }
const admin = { id: 10, nome: 'Dona Admin', emailMascarado: 'd***@demo.edu.br', cpfMascarado: '***.456.789-**', papeis: ['ADMINISTRADOR'], ativo: true, primeiroAcessoPendente: false }
const carla = { id: 11, nome: 'Carla Atendente', emailMascarado: 'c***@demo.edu.br', cpfMascarado: '***.111.222-**', papeis: ['ATENDENTE'], ativo: true, primeiroAcessoPendente: true }

function tela(rotas: Record<string, Rota> = {}, equipe: unknown[] = [admin]) {
  const chamadas = api({
    'GET /api/plataforma/eu': () => [200, operador],
    [`GET ${Base}`]: () => [200, detalhe],
    'GET /api/plataforma/modelos-de-carga': () => [200, [modelo]],
    [`GET ${Base}/equipe`]: () => [200, equipe],
    [`GET ${Base}/setores`]: () => [200, [{ id: 1, nome: 'Triagem' }, { id: 2, nome: 'Financeiro' }]],
    ...rotas,
  })
  abrir('/plataforma/instituicoes/demo')
  return chamadas
}

const gravacoes = (chamadas: { metodo: string; url: string; corpo: any }[]) => chamadas.filter((c) => c.metodo !== 'GET').map((c) => [c.url, c.corpo])

describe('implantação da instituição', () => {
  afterEach(() => cleanup())

  it('criar leva a implantacao com os quatro itens, o estado e o passo de cada pendente', async () => {
    api({
      'GET /api/plataforma/eu': () => [200, operador],
      'GET /api/plataforma/instituicoes': () => [200, []],
      'POST /api/plataforma/instituicoes': () => [201, detalhe],
      [`GET ${Base}`]: () => [200, detalhe],
      'GET /api/plataforma/modelos-de-carga': () => [200, [modelo]],
      [`GET ${Base}/equipe`]: () => [200, [admin]],
      [`GET ${Base}/setores`]: () => [200, []],
    })
    abrir('/plataforma/instituicoes')
    await userEvent.click(await screen.findByRole('button', { name: 'Nova instituição' }))
    const d = within(await screen.findByRole('dialog'))
    const [nome, nomeDoAdmin] = d.getAllByLabelText('Nome')
    await userEvent.type(nome, 'Demo')
    await userEvent.type(nomeDoAdmin, 'Dona Admin')
    await userEvent.type(d.getByLabelText('CPF'), '123.456.789-09')
    await userEvent.type(d.getByLabelText('E-mail'), 'admin@demo.edu.br')
    await userEvent.click(d.getByRole('button', { name: 'Criar' }))

    expect(await screen.findByRole('tab', { name: 'Implantação', selected: true })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Instituição', level: 1 })).toBeInTheDocument()
    const lista = within(await screen.findByRole('list'))
    const itens = lista.getAllByRole('listitem')
    expect(itens.map((i) => i.textContent)).toEqual([
      'Um administrador com senha criadaFeito',
      'Um atendente ligado a um setorPendenteIr para a equipe inicial',
      'Uma categoria com regra de roteamentoPendenteIr para a carga inicial',
      'Um artigo ativo e dentro da validadePendenteO administrador da instituição cadastra os artigos.',
    ])
    await userEvent.click(lista.getByRole('button', { name: 'Ir para a carga inicial' }))
    expect(document.activeElement).toBe(document.getElementById('carga'))
    await userEvent.click(lista.getByRole('button', { name: 'Ir para a equipe inicial' }))
    expect(document.activeElement).toBe(document.getElementById('equipe'))
  })

  it('carga inicial simula antes de importar e mostra as contagens ou os erros', async () => {
    let lidas = 0
    const chamadas = tela({
      [`GET ${Base}`]: () => [200, { ...detalhe, prontidao: ++lidas > 1 ? { ...pendente, itens: pendente.itens.map((i) => (i.codigo === 'CATEGORIA_COM_REGRA' ? { ...i, ok: true } : i)) } : pendente }],
      [`POST ${Base}/carga/modelo/padrao-esclari`]: () => [200, { setores: 3, cursos: 0, categorias: 6, regras: 6, artigos: 0 }],
      [`POST ${Base}/carga?simular=true`]: () => [400, { status: 400, detail: 'O arquivo de carga tem erros.', errors: { arquivo: ['setores[1].nome: "A" repetido', 'regras[0].categoria: categoria "X" não existe'] } }],
    })
    const secao = within(await screen.findByRole('region', { name: 'Carga inicial' }))
    const [simular, importar] = [secao.getByRole('button', { name: 'Simular' }), secao.getByRole('button', { name: 'Importar' })]
    expect(simular.compareDocumentPosition(importar) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(simular).toBeDisabled()

    await secao.findByRole('option', { name: 'Modelo Padrão Esclari' })
    await userEvent.selectOptions(secao.getByLabelText('Origem da carga'), 'padrao-esclari')
    await userEvent.click(simular)
    expect(await secao.findByText('A importação criaria: 3 setores, 0 cursos, 6 categorias, 6 regras, 0 artigos.')).toBeInTheDocument()
    expect(gravacoes(chamadas)).toEqual([])
    await userEvent.click(importar)
    expect(await secao.findByText('Importados: 3 setores, 0 cursos, 6 categorias, 6 regras, 0 artigos.')).toBeInTheDocument()
    expect(gravacoes(chamadas)).toEqual([[`${Base}/carga/modelo/padrao-esclari`, {}]])
    await waitFor(() => expect(screen.getAllByRole('listitem')[2]).toHaveTextContent('Feito'))

    await userEvent.selectOptions(secao.getByLabelText('Origem da carga'), 'arquivo')
    await userEvent.upload(secao.getByLabelText(/Arquivo de carga/), new File(['{}'], 'carga.json', { type: 'application/json' }))
    await userEvent.click(secao.getByRole('button', { name: 'Simular' }))
    const erro = await secao.findByText('setores[1].nome: "A" repetido')
    expect(erro.closest('tr')).not.toBe(secao.getByText('regras[0].categoria: categoria "X" não existe').closest('tr'))
    expect(chamadas.at(-1)!.url).toBe(`${Base}/carga?simular=true`)
  })

  it('equipe inicial vazia e desativar com confirmacao do nome', async () => {
    tela()
    const secao = within(await screen.findByRole('region', { name: /Equipe inicial/ }))
    expect(await secao.findByText('Nenhum administrador ou atendente além do primeiro. Crie a equipe para a instituição atender.')).toBeInTheDocument()
    expect(secao.getByRole('button', { name: 'Novo usuário' })).toBeInTheDocument()
    cleanup()

    let desativado = false
    const chamadas = tela({ [`POST ${Base}/equipe/11/desativar`]: () => ((desativado = true), [204, undefined]) }, [admin, carla])
    const equipe = within(await screen.findByRole('region', { name: /Equipe inicial/ }))
    await userEvent.click(await equipe.findByRole('button', { name: 'Ações de Carla Atendente' }))
    await userEvent.click(await screen.findByRole('menuitem', { name: 'Desativar' }))
    const d = within(await screen.findByRole('dialog'))
    expect(d.getByText('Desativar Carla Atendente? Ele deixa de entrar na instituição até você reativar.')).toBeInTheDocument()
    await userEvent.click(d.getByRole('button', { name: 'Voltar' }))
    expect(gravacoes(chamadas)).toEqual([])
    await userEvent.click(equipe.getByRole('button', { name: 'Ações de Carla Atendente' }))
    await userEvent.click(await screen.findByRole('menuitem', { name: 'Desativar' }))
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Desativar' }))
    await waitFor(() => expect(desativado).toBe(true))
    expect(gravacoes(chamadas)).toEqual([[`${Base}/equipe/11/desativar`, {}]])
  })

  it('novo usuário convida com papéis e setores, sem senha', async () => {
    const chamadas = tela({
      [`POST ${Base}/equipe`]: (corpo) => (corpo.setores.length === 0 ? [400, { status: 400, detail: 'Escolha ao menos um setor para o atendente.', errors: { setores: ['Escolha ao menos um setor para o atendente.'] } }] : [201, { id: 12 }]),
    })
    const secao = within(await screen.findByRole('region', { name: /Equipe inicial/ }))
    await userEvent.click(secao.getByRole('button', { name: 'Novo usuário' }))
    const d = within(await screen.findByRole('dialog', { name: 'Novo usuário' }))
    await userEvent.type(d.getByLabelText('Nome'), 'Carla')
    await userEvent.type(d.getByLabelText('CPF'), '123.456.789-09')
    await userEvent.type(d.getByLabelText('E-mail'), 'carla@demo.edu.br')
    await userEvent.click(d.getByLabelText('Atendente'))
    await userEvent.click(d.getByRole('button', { name: 'Convidar' }))
    expect(await d.findByText('Escolha ao menos um setor para o atendente.')).toBeInTheDocument()
    await userEvent.click(d.getByLabelText('Financeiro'))
    await userEvent.click(d.getByRole('button', { name: 'Convidar' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(gravacoes(chamadas).at(-1)).toEqual([`${Base}/equipe`, { nome: 'Carla', cpf: '123.456.789-09', email: 'carla@demo.edu.br', papeis: ['ATENDENTE'], setores: [2] }])
    expect(d.queryByLabelText(/senha/i)).toBeNull()
  })
})
