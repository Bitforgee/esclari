import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { abrir, api, chamado, comTexto, me } from '../test/util'

async function transferirComPrazo(horas: number) {
  api({
    'GET /api/auth/me': () => [200, me()],
    'POST /api/conversas': () => [201, { conversaId: 7, resposta: { tipo: 'sem_base', texto: 'Não encontrei.' } }],
    'POST /api/conversas/7/transferir': () => [201, { numero: 'ESC-000001', estado: 'ABERTO', setor: 'Financeiro', prazoPrimeiraRespostaHoras: horas }],
    'GET /api/chamados/ESC-000001': () => [200, chamado()],
  })
  abrir('/univag/assistente')

  await userEvent.type(await screen.findByLabelText('Mensagem'), 'preciso do boleto')
  await userEvent.click(screen.getByRole('button', { name: /Enviar/ }))
  await screen.findByText('Não encontrei.')
  await userEvent.click(screen.getByRole('button', { name: /Falar com atendente/ }))
  await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Falar com atendente' }))

  await screen.findByText(comTexto('Sua solicitação ESC-000001 foi para Financeiro'))
}

describe('prazo na transferência', () => {
  it('confirmacao com prazo', async () => {
    await transferirComPrazo(24)
    expect(screen.getByText('A equipe responde em até 24 horas')).toBeInTheDocument()
  })

  it('confirmacao com prazo configurado de 8 horas', async () => {
    await transferirComPrazo(8)
    expect(screen.getByText('A equipe responde em até 8 horas')).toBeInTheDocument()
    expect(screen.queryByText('A equipe responde em até 24 horas')).not.toBeInTheDocument()
  })
})
