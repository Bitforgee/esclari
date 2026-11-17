import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { abrir, api, instituicao, me } from '../../test/util'

const admin = me({ papeis: ['ADMINISTRADOR'], matriculas: [] })
const Azul = '#' + '224fa7'
const Nomes = ['Azul', 'Verde', 'Vinho', 'Roxo', 'Laranja', 'Grafite', 'Petróleo', 'Marrom']

async function grupo() {
  const { nome: _, ...aparencia } = instituicao()
  api({ 'GET /api/auth/me': () => [200, admin], 'GET /api/admin/aparencia': () => [200, aparencia], 'GET /api/instituicoes/univag': () => [200, instituicao()] })
  abrir('/univag/aparencia')
  return within(await screen.findByRole('radiogroup', { name: 'Cores prontas' }))
}

type Grupo = { getAllByRole: (papel: 'radio') => HTMLElement[] }
const paradas = (g: Grupo) => g.getAllByRole('radio').filter((r) => r.tabIndex === 0).map((r) => r.getAttribute('aria-label'))
const marcada = (g: Grupo) => g.getAllByRole('radio').filter((r) => r.getAttribute('aria-checked') === 'true').map((r) => r.getAttribute('aria-label'))

describe('CoresProntas por teclado', () => {
  it('C25 só uma cor entra na ordem de Tab: a marcada, ou a primeira sem marcada', async () => {
    const g = await grupo()
    expect(paradas(g)).toEqual(['Azul'])
    expect(g.getAllByRole('radio').filter((r) => r.tabIndex === -1)).toHaveLength(7)
    await userEvent.click(g.getByRole('radio', { name: 'Vinho' }))
    expect(paradas(g)).toEqual(['Vinho'])
  })

  it('C25 ArrowRight e ArrowLeft movem a seleção e o foco, com volta ao fim do grupo', async () => {
    const g = await grupo()
    g.getByRole('radio', { name: 'Azul' }).focus()
    await userEvent.keyboard('{ArrowRight}')
    expect(marcada(g)).toEqual(['Verde'])
    expect(g.getByRole('radio', { name: 'Verde' })).toHaveFocus()
    await userEvent.keyboard('{ArrowRight}{ArrowLeft}{ArrowLeft}')
    expect(marcada(g)).toEqual(['Azul'])
    await userEvent.keyboard('{ArrowLeft}')
    expect(marcada(g)).toEqual([Nomes[7]])
    expect(g.getByRole('radio', { name: 'Marrom' })).toHaveFocus()
    expect(paradas(g)).toEqual(['Marrom'])
    await userEvent.keyboard('{ArrowRight}')
    expect(marcada(g)).toEqual(['Azul'])
  })

  it('preset Azul mantem o valor sem o sobrenome Esclari', async () => {
    const g = await grupo()
    expect(g.getByRole('radio', { name: 'Azul' })).toBeInTheDocument()
    expect(g.queryByRole('radio', { name: 'Azul Esclari' })).not.toBeInTheDocument()
    await userEvent.click(g.getByRole('radio', { name: 'Azul' }))
    expect(screen.getByLabelText('Cor principal')).toHaveValue(Azul)
    const css = readFileSync(resolve(process.cwd(), 'src/index.css'), 'utf8')
    expect(css).toContain('--primary: var(--marca, ' + 'ok' + 'lch(0.45 0.15 262))')
    expect(readFileSync(resolve(process.cwd(), 'src/administracao/aparencia/CoresProntas.tsx'), 'utf8')).toContain("['Azul', '224fa7']")
  })

  it('C25 Tab atravessa o grupo numa só parada', async () => {
    const g = await grupo()
    await userEvent.click(g.getByRole('radio', { name: 'Roxo' }))
    await userEvent.tab({ shift: true })
    await userEvent.tab()
    expect(g.getByRole('radio', { name: 'Roxo' })).toHaveFocus()
    await userEvent.tab()
    expect(screen.getByLabelText('Escolher cor')).toHaveFocus()
  })
})
