import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import type { Chamado, MensagemChamado } from '../chamado'
import { abrir, api, chamado, me, type Rota } from '../test/util'

const Botao = 'Criar artigo a partir desta resposta'
const Aviso = 'Rascunho criado. O administrador precisa ativar antes de o assistente usar.'
const equipe = me({ papeis: ['ATENDENTE'], matriculas: [] })
const admin = me({ papeis: ['ADMINISTRADOR'], matriculas: [] })

const mensagem = (id: number, tipo: 'PUBLICA' | 'NOTA', texto: string, autor: string, doAluno = false): MensagemChamado => ({
  id, tipo, texto, autor, doAluno, criadaEm: `2026-11-02T12:0${id}:00Z`, anexos: [],
})

function resolvido(parcial: Partial<Chamado> = {}): Chamado {
  return chamado({
    estado: 'RESOLVIDO',
    solicitante: 'Ana',
    resumo: 'Aluna pede o boleto.',
    eventos: [],
    acoes: [],
    artigoDaResposta: 'PODE_CRIAR',
    mensagens: [
      mensagem(1, 'PUBLICA', 'Primeira resposta da equipe.', 'Carla'),
      mensagem(2, 'PUBLICA', 'Boleto reemitido no portal.', 'Carla'),
      mensagem(3, 'NOTA', 'Aluna ligou.', 'Bruno'),
      mensagem(4, 'PUBLICA', 'Obrigada!', 'Ana', true),
      mensagem(5, 'PUBLICA', 'Encerrado por falta de resposta.', 'Sistema'),
    ],
    ...parcial,
  })
}

function detalhe(c: Chamado, quem = equipe, rotas: Record<string, Rota> = {}) {
  const chamadas = api({ 'GET /api/auth/me': () => [200, quem], 'GET /api/chamados/ESC-000001': () => [200, c], ...rotas })
  const { unmount } = abrir('/univag/chamados/ESC-000001')
  return { chamadas, unmount }
}

async function abrirFormulario() {
  await userEvent.click(await screen.findByRole('button', { name: Botao }))
  return screen.findByRole('dialog', { name: Botao })
}

const avisos = () => screen.getByRole('region', { name: /^Notificações/ })

describe('artigo a partir da resposta', () => {
  it('botao so no chamado resolvido', async () => {
    for (const estado of ['RESOLVIDO', 'FECHADO']) {
      const { unmount } = detalhe(resolvido({ estado }))
      expect(await screen.findByRole('button', { name: Botao })).toBeInTheDocument()
      unmount()
    }

    const casos: [Chamado, ReturnType<typeof me>][] = [
      [resolvido({ estado: 'EM_ATENDIMENTO', artigoDaResposta: undefined, acoes: ['responder', 'nota', 'resolver'] }), equipe],
      [resolvido({ eventos: null, solicitante: null, artigoDaResposta: undefined, acoes: ['responder', 'rejeitar', 'confirmar'] }), me()],
      [resolvido({ artigoDaResposta: undefined }), admin],
    ]
    for (const [c, quem] of casos) {
      const { unmount } = detalhe(c, quem)
      await screen.findByRole('heading', { level: 2, name: 'preciso do boleto' })
      expect(screen.queryByRole('button', { name: Botao })).toBeNull()
      expect(screen.queryByText(/^Artigo criado/)).toBeNull()
      unmount()
    }
  })

  it('formulario preenchido com a ultima resposta publica', async () => {
    detalhe(resolvido())
    const dialogo = await abrirFormulario()

    const titulo = within(dialogo).getByLabelText('Título')
    expect(titulo).toHaveValue('')
    expect(within(dialogo).getByLabelText('Perguntas relacionadas (uma por linha)')).toHaveValue('')
    expect(within(dialogo).getByLabelText('Resposta')).toHaveValue('Boleto reemitido no portal.')
    const faixa = within(dialogo).getByText('Remova nomes, CPF e matrícula antes de salvar')
    expect(faixa.compareDocumentPosition(titulo) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it('salvar cria o rascunho', async () => {
    const { chamadas } = detalhe(resolvido(), equipe, { 'POST /api/chamados/ESC-000001/artigo': () => [201, { id: 9, estado: 'RASCUNHO', origemChamado: 'ESC-000001' }] })
    const dialogo = await abrirFormulario()
    await userEvent.type(within(dialogo).getByLabelText('Título'), 'Segunda via do boleto')
    await userEvent.type(within(dialogo).getByLabelText('Perguntas relacionadas (uma por linha)'), 'como tiro a segunda via?{enter}{enter} boleto vencido? ')
    await userEvent.type(within(dialogo).getByLabelText('Links (um por linha, com https://)'), 'https://portal.exemplo/boleto')

    await userEvent.click(within(dialogo).getByRole('button', { name: 'Salvar rascunho' }))

    await waitFor(() => expect(within(avisos()).getByText(Aviso)).toBeInTheDocument())
    expect(chamadas.find((c) => c.metodo === 'POST')).toEqual({
      metodo: 'POST',
      url: '/api/chamados/ESC-000001/artigo',
      corpo: { titulo: 'Segunda via do boleto', perguntas: ['como tiro a segunda via?', 'boleto vencido?'], resposta: 'Boleto reemitido no portal.', links: ['https://portal.exemplo/boleto'] },
    })
    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull())
    expect(screen.queryByRole('button', { name: Botao })).toBeNull()
    expect(screen.getByText('Artigo criado: ESC-000001')).toBeInTheDocument()
  })

  it('chamado que ja originou artigo', async () => {
    detalhe(resolvido({ artigoDaResposta: 'CRIADO' }))

    expect(await screen.findByText('Artigo criado: ESC-000001')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: Botao })).toBeNull()
  })

  it('falha mantem o formulario', async () => {
    const respostas: [number, unknown][] = [
      [400, { status: 400, detail: 'Informe o título.', errors: { titulo: ['Informe um título de 3 a 120 caracteres.'] } }],
      [409, { status: 409, detail: 'Este chamado já originou o artigo “Boleto”.' }],
    ]
    detalhe(resolvido(), equipe, { 'POST /api/chamados/ESC-000001/artigo': () => respostas.shift()! })
    const dialogo = await abrirFormulario()
    await userEvent.type(within(dialogo).getByLabelText('Título'), 'Bo')
    await userEvent.type(within(dialogo).getByLabelText('Perguntas relacionadas (uma por linha)'), 'como tiro o boleto?')

    await userEvent.click(within(dialogo).getByRole('button', { name: 'Salvar rascunho' }))

    expect(await within(dialogo).findByText('Informe um título de 3 a 120 caracteres.')).toBeInTheDocument()
    expect(screen.getByRole('dialog', { name: Botao })).toBe(dialogo)
    expect(within(dialogo).getByLabelText('Título')).toHaveValue('Bo')
    expect(within(dialogo).getByLabelText('Perguntas relacionadas (uma por linha)')).toHaveValue('como tiro o boleto?')
    expect(within(dialogo).getByLabelText('Resposta')).toHaveValue('Boleto reemitido no portal.')

    await userEvent.click(within(dialogo).getByRole('button', { name: 'Salvar rascunho' }))

    expect(await within(dialogo).findByText('Este chamado já originou o artigo “Boleto”.')).toBeInTheDocument()
    expect(within(dialogo).getByLabelText('Título')).toHaveValue('Bo')
    expect(screen.queryByText(/^Artigo criado/)).toBeNull()
  })

  it('salvar desabilitado durante a criacao', async () => {
    detalhe(resolvido(), equipe, { 'POST /api/chamados/ESC-000001/artigo': () => new Promise(() => {}) })
    const dialogo = await abrirFormulario()
    await userEvent.type(within(dialogo).getByLabelText('Título'), 'Segunda via do boleto')
    await userEvent.type(within(dialogo).getByLabelText('Perguntas relacionadas (uma por linha)'), 'como tiro a segunda via?')

    await userEvent.click(within(dialogo).getByRole('button', { name: 'Salvar rascunho' }))

    const salvando = await within(dialogo).findByRole('button', { name: 'Salvando…' })
    expect(salvando).toBeDisabled()
    expect(salvando).toHaveAttribute('aria-busy', 'true')
  })
})
