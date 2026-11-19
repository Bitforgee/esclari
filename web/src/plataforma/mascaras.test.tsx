import { cleanup, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { abrir, api } from '../test/util'

const operador = { id: 1, nome: 'Marina Souza', email: 'marina@esclari.com.br' }
const Base = '/api/plataforma/instituicoes/demo'
const detalhe = { slug: 'demo', nome: 'Faculdade Demo', situacao: 'CONFIGURANDO', prontidao: { itens: [], pronta: false } }
const admin = { id: 10, nome: 'Dona Admin', emailMascarado: 'd***@demo.edu.br', cpfMascarado: '***.456.789-**', tipo: { id: 1, nome: 'Gestor', sistema: 'GESTOR' }, ativo: true, primeiroAcessoPendente: false }
const gravacoes = (chamadas: { metodo: string; url: string; corpo: any }[]) => chamadas.filter((c) => c.metodo !== 'GET').map((c) => [c.url, c.corpo])

async function abrirNova() {
  await userEvent.click(await screen.findByRole('button', { name: 'Nova instituição' }))
  const d = within(await screen.findByRole('dialog'))
  return { d, primeiro: within(d.getByRole('group', { name: 'Primeiro administrador' })) }
}

describe('máscaras na plataforma', () => {
  afterEach(() => cleanup())

  it('cpf da equipe e da nova instituição', async () => {
    const chamadas = api({
      'GET /api/plataforma/eu': () => [200, operador],
      [`GET ${Base}`]: () => [200, detalhe],
      'GET /api/plataforma/modelos-de-carga': () => [200, []],
      [`GET ${Base}/equipe`]: () => [200, [admin]],
      [`GET ${Base}/setores`]: () => [200, []],
      [`POST ${Base}/equipe`]: () => [201, { id: 12 }],
    })
    let montada = abrir('/plataforma/instituicoes/demo')
    const secao = within(await screen.findByRole('region', { name: /Equipe inicial/ }))
    await userEvent.click(secao.getByRole('button', { name: 'Novo usuário' }))
    const d = within(await screen.findByRole('dialog', { name: 'Novo usuário' }))
    const cpf = d.getByLabelText('CPF')
    expect(cpf).toHaveAttribute('inputmode', 'numeric')
    expect(cpf).toHaveAttribute('maxlength', '14')
    await userEvent.type(d.getByLabelText('Nome'), 'Gil')
    await userEvent.type(cpf, '04647333167')
    expect(cpf).toHaveValue('046.473.331-67')
    await userEvent.type(d.getByLabelText('E-mail'), 'gil@demo.edu.br')
    await userEvent.selectOptions(d.getByLabelText('Tipo'), 'Gestor')
    await userEvent.click(d.getByRole('button', { name: 'Convidar' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(gravacoes(chamadas).at(-1)).toEqual([`${Base}/equipe`, { nome: 'Gil', cpf: '04647333167', email: 'gil@demo.edu.br', tipo: 'GESTOR', setores: [] }])
    montada.unmount()

    const criacao = api({
      'GET /api/plataforma/eu': () => [200, operador],
      'GET /api/plataforma/instituicoes': () => [200, []],
      'POST /api/plataforma/instituicoes': () => [400, { status: 400, detail: 'Dados inválidos.' }],
    })
    montada = abrir('/plataforma/instituicoes')
    const nova = await abrirNova()
    const doAdmin = nova.primeiro.getByLabelText('CPF')
    expect(doAdmin).toHaveAttribute('inputmode', 'numeric')
    expect(doAdmin).toHaveAttribute('maxlength', '14')
    await userEvent.type(nova.d.getAllByLabelText('Nome')[0], 'Faculdade Demo')
    await userEvent.type(nova.primeiro.getByLabelText('Nome'), 'Dona Admin')
    await userEvent.type(doAdmin, '046.473.331-67')
    expect(doAdmin).toHaveValue('046.473.331-67')
    await userEvent.type(nova.primeiro.getByLabelText('E-mail'), 'admin@demo.edu.br')
    await userEvent.click(screen.getByRole('button', { name: 'Criar' }))
    await waitFor(() => expect(gravacoes(criacao)).toHaveLength(1))
    expect(gravacoes(criacao)[0][1].admin.cpf).toBe('04647333167')
    montada.unmount()
  })

  it('endereço da nova instituição', async () => {
    api({ 'GET /api/plataforma/eu': () => [200, operador], 'GET /api/plataforma/instituicoes': () => [200, []] })
    abrir('/plataforma/instituicoes')
    const { d } = await abrirNova()
    const endereco = d.getByLabelText('Endereço')
    await userEvent.type(endereco, 'Univag Sul')
    expect(endereco).toHaveValue('univag-sul')
    await userEvent.clear(endereco)
    await userEvent.type(endereco, 'São_João!')
    expect(endereco).toHaveValue('saojoao')
  })
})
