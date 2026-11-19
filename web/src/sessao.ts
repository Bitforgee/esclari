import { useQueryClient } from '@tanstack/react-query'
import { useNavigate, useOutletContext, useParams } from 'react-router'
import { api, type Me } from './api'

export type Sessao = { me: Me; setMe: (me: Me) => void }
export const useSessao = () => useOutletContext<Sessao>()

const Entrada: [string, string][] = [
  ['assistente.usar', 'assistente'],
  ['fila.atender', 'fila'],
  ['chamados.ver_todos', 'chamados'],
  ['indicadores.ver', 'indicadores'],
  ['conhecimento.gerenciar', 'artigos'],
  ['usuarios.gerenciar', 'usuarios'],
  ['tipos.gerenciar', 'tipos'],
  ['matriculas.importar', 'matriculas'],
  ['cadastros.gerenciar', 'cadastros'],
  ['aparencia.gerenciar', 'aparencia'],
  ['integracoes.gerenciar', 'integracoes'],
]

export function telaInicial(u: { permissoes: string[]; deveTrocarSenha: boolean }): string {
  if (u.deveTrocarSenha) return 'trocar-senha'
  return Entrada.find(([funcao]) => u.permissoes.includes(funcao))?.[1] ?? 'conta'
}

export function useSair() {
  const { slug } = useParams()
  const navigate = useNavigate()
  const cliente = useQueryClient()
  return async () => {
    await api('/api/auth/logout', {}).catch(() => undefined)
    navigate(`/${slug}`)
    cliente.removeQueries({ queryKey: ['me'] })
  }
}
