import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { abrir, api, escolher, me, type Rota } from '../../test/util'

const admin = me({ id: 99, papeis: ['ADMINISTRADOR'], matriculas: [] })
const tipoGestor = { id: 1, nome: 'Gestor' }
const FuncoesDoGestor = ['fila.atender', 'chamados.ver_todos', 'indicadores.ver', 'conhecimento.gerenciar', 'cadastros.gerenciar', 'usuarios.gerenciar', 'matriculas.importar', 'tipos.gerenciar', 'aparencia.gerenciar', 'integracoes.gerenciar', 'mcp.usar']
const tipos = { tipos: [{ ...tipoGestor, sistema: 'GESTOR', funcoes: FuncoesDoGestor, usuarios: 1 }], catalogo: [] }
const davi = { id: 5, cpf: '11144477735', nome: 'Davi', email: 'davi@univag.edu.br', ativo: true, deveTrocarSenha: false, tipo: tipoGestor, setores: [] }

function usuarios(rotas: Record<string, Rota> = {}) {
  const chamadas = api({ 'GET /api/auth/me': () => [200, admin], 'GET /api/admin/usuarios': () => [200, [davi]], 'GET /api/admin/setores': () => [200, []], 'GET /api/admin/tipos': () => [200, tipos], ...rotas })
  abrir('/univag/usuarios')
  return chamadas
}

const gravacoes = (chamadas: { metodo: string; url: string; corpo: any }[]) => chamadas.filter((c) => c.metodo !== 'GET').map((c) => [c.metodo, c.url, c.corpo])

describe('máscara do CPF em usuários', () => {
  it('cpf do novo usuário', async () => {
    const chamadas = usuarios({ 'POST /api/admin/usuarios': () => [201, {}] })
    await userEvent.click(await screen.findByRole('button', { name: /Novo/ }))
    const d = within(await screen.findByRole('dialog'))
    const cpf = d.getByLabelText('CPF')
    expect(cpf).toHaveAttribute('inputmode', 'numeric')
    expect(cpf).toHaveAttribute('maxlength', '14')
    await userEvent.type(cpf, '04647333167')
    expect(cpf).toHaveValue('046.473.331-67')
    await userEvent.type(d.getByLabelText('Nome'), 'Eva')
    await userEvent.type(d.getByLabelText('E-mail'), 'eva@univag.edu.br')
    await userEvent.selectOptions(d.getByLabelText('Tipo'), 'Gestor')
    await userEvent.click(d.getByRole('button', { name: 'Salvar usuário' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(gravacoes(chamadas)).toEqual([['POST', '/api/admin/usuarios', { cpf: '04647333167', nome: 'Eva', email: 'eva@univag.edu.br', tipoId: 1, setores: [] }]])
  })

  it('cpf ao editar usuário', async () => {
    const chamadas = usuarios({ 'PUT /api/admin/usuarios/5': () => [200, {}] })
    await screen.findByText('Davi')
    await escolher('Davi', 'Editar')
    const d = within(await screen.findByRole('dialog'))
    expect(d.getByLabelText('CPF')).toHaveValue('111.444.777-35')
    await userEvent.click(d.getByRole('button', { name: 'Salvar usuário' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(gravacoes(chamadas)).toEqual([['PUT', '/api/admin/usuarios/5', { cpf: '11144477735', nome: 'Davi', email: 'davi@univag.edu.br', tipoId: 1, setores: [] }]])
  })
})
