import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { abrir, api, instituicao, me } from '../test/util'

const admin = me({ papeis: ['ADMINISTRADOR'], matriculas: [] })
const urlMcp = 'https://esclari.teste/api/mcp/univag'
const Antigo = /(?<![A-Za-z])One(?![a-z])/
const moodleVazio = { configurada: false, ativa: false, emissor: '', clientId: '', deploymentId: '', authUrl: '', jwksUrl: '', urlLogin: '', urlLancamento: '', urlJwks: '' }
const integracoes = () =>
  api({
    'GET /api/auth/me': () => [200, admin],
    'GET /api/instituicoes/univag': () => [200, instituicao()],
    'GET /api/admin/moodle': () => [200, moodleVazio],
    'GET /api/admin/mcp': () => [200, { url: urlMcp, tokens: [] }],
  })

describe('marca Esclari nas telas de administração', () => {
  it('integracoes mostra a atividade Esclari do Moodle e o servidor esclari do MCP, sem One', async () => {
    integracoes()
    const moodle = abrir('/univag/integracoes')
    await screen.findByLabelText('Emissor (iss)')
    expect(document.body.textContent).toContain('atividade Esclari')
    expect(document.body.textContent).not.toMatch(Antigo)
    moodle.unmount()

    integracoes()
    abrir('/univag/integracoes?aba=mcp')
    expect(await screen.findByText(urlMcp)).toBeInTheDocument()
    expect(document.body.textContent).toContain('indicadores do Esclari')
    expect(document.body.textContent).not.toMatch(Antigo)
  })

  it('integracoes mostra o comando claude mcp add com esclari e a chave esclari no JSON', async () => {
    integracoes()
    abrir('/univag/integracoes?aba=mcp')
    await screen.findByRole('heading', { name: 'Como conectar', level: 2 })
    expect(screen.getByText(`claude mcp add --transport http esclari ${urlMcp} --header "Authorization: Bearer <seu-token>"`)).toBeInTheDocument()
    expect(screen.getByText(`{"mcpServers":{"esclari":{"url":"${urlMcp}","headers":{"Authorization":"Bearer <seu-token>"}}}}`)).toBeInTheDocument()
    expect(document.body.textContent).not.toMatch(/http one |"one":/)
  })

  it('azul é o nome da cor padrão e fica na parada inicial', async () => {
    const { nome: _, ...aparencia } = instituicao()
    api({ 'GET /api/auth/me': () => [200, admin], 'GET /api/admin/aparencia': () => [200, aparencia], 'GET /api/instituicoes/univag': () => [200, instituicao()] })
    abrir('/univag/aparencia')
    const grupo = within(await screen.findByRole('radiogroup', { name: 'Cores prontas' }))
    const azul = grupo.getByRole('radio', { name: 'Azul' })
    expect(grupo.getAllByRole('radio').filter((r) => r.tabIndex === 0)).toEqual([azul])
    expect(grupo.queryByRole('radio', { name: 'Azul Esclari' })).toBeNull()
    expect(grupo.queryByRole('radio', { name: 'Azul One' })).toBeNull()
  })
})
