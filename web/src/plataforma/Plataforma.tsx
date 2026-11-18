import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Building2, LogOut, ScrollText, type LucideIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Navigate, NavLink, Outlet, useLocation, useNavigate } from 'react-router'
import { api, ErroApi } from '../api'
import { useTema } from '../aparencia'
import { Carregando } from '../Carregando'
import { ErroDe } from '../ErroDe'
import { Button } from '../ui/button'
import { Entrada } from './Entrada'

type Operador = { id: number; nome: string; email: string }

const Itens: [string, string, LucideIcon][] = [
  ['instituicoes', 'Instituições', Building2],
  ['auditoria', 'Auditoria', ScrollText],
]

const Encerrada = 'Sua sessão terminou. Entre de novo.'
const semSessao = (erro: unknown) => erro instanceof ErroApi && erro.status === 401

function useSessaoEncerrada() {
  const cliente = useQueryClient()
  const [encerrada, setEncerrada] = useState(false)
  useEffect(() => {
    const ouvir = (erro: unknown, chave?: readonly unknown[]) => {
      if (semSessao(erro) && chave?.[0] !== 'plataforma-eu') setEncerrada(true)
    }
    const leituras = cliente.getQueryCache().subscribe((e) => {
      if (e.type === 'updated' && e.action.type === 'error') ouvir(e.action.error, e.query.queryKey)
    })
    const gravacoes = cliente.getMutationCache().subscribe((e) => {
      if (e.type === 'updated' && e.action.type === 'error') ouvir(e.action.error)
    })
    return () => {
      leituras()
      gravacoes()
    }
  }, [cliente])
  return [encerrada, setEncerrada] as const
}

function Barra({ operador, sair }: { operador: Operador; sair: () => void }) {
  const tema = useTema()
  return (
    <header className="sticky top-0 z-10 flex flex-wrap items-center gap-x-4 gap-y-1 border-b bg-background px-4 py-2 pt-[calc(0.5rem+env(safe-area-inset-top))] lg:px-6">
      <img src={tema === 'light' ? '/marca/logo-horizontal.svg' : '/marca/logo-horizontal-escuro.svg'} alt="Esclari" width={130} height={30} className="h-7 w-auto" />
      <nav aria-label="Navegação" className="order-last flex w-full gap-1 sm:order-none sm:w-auto">
        {Itens.map(([caminho, rotulo, Icone]) => (
          <NavLink
            key={caminho}
            to={`/plataforma/${caminho}`}
            className={({ isActive }) => `inline-flex min-h-11 items-center gap-2 rounded-md px-3 text-sm focus-visible:ring-2 focus-visible:ring-ring ${isActive ? 'bg-accent font-semibold' : 'text-muted-foreground'}`}
          >
            <Icone aria-hidden="true" className="size-4" />
            {rotulo}
          </NavLink>
        ))}
      </nav>
      <div className="ms-auto flex items-center gap-2">
        <span className="truncate text-sm text-muted-foreground">{operador.nome}</span>
        <Button variant="ghost" onClick={sair}>
          <LogOut aria-hidden="true" />
          Sair
        </Button>
      </div>
    </header>
  )
}

function Casca({ operador, sair }: { operador: Operador; sair: () => void }) {
  const { pathname } = useLocation()
  const titulo = pathname.startsWith('/plataforma/instituicoes/') ? 'Instituição' : (Itens.find(([c]) => pathname.endsWith(`/${c}`))?.[1] ?? 'Console da plataforma')
  useEffect(() => {
    document.title = `${titulo} · Esclari`
  }, [titulo])
  return (
    <>
      <Barra operador={operador} sair={sair} />
      <main id="conteudo" tabIndex={-1} className="@container/main flex flex-col gap-3 px-4 py-4 outline-none md:py-6 lg:px-6">
        <h1 className="font-heading text-[28px] leading-tight font-semibold tracking-tight">{titulo}</h1>
        <Outlet />
      </main>
    </>
  )
}

export function Plataforma() {
  const cliente = useQueryClient()
  const navegar = useNavigate()
  const { pathname } = useLocation()
  const [encerrada, setEncerrada] = useSessaoEncerrada()
  const { data: operador, error, refetch } = useQuery({ queryKey: ['plataforma-eu'], queryFn: () => api<Operador>('/api/plataforma/eu'), retry: false })

  const entrou = () => {
    setEncerrada(false)
    void refetch()
  }
  const sair = async () => {
    await api('/api/plataforma/auth/sair', {}).catch(() => undefined)
    cliente.removeQueries()
    navegar('/plataforma', { replace: true })
    void refetch()
  }

  useEffect(() => {
    if (!encerrada) return
    cliente.removeQueries({ predicate: (q) => q.queryKey[0] !== 'plataforma-eu' })
    void refetch()
    navegar('/plataforma', { replace: true })
  }, [encerrada, cliente, navegar, refetch])

  if (encerrada || semSessao(error)) return <Entrada aviso={encerrada ? Encerrada : undefined} entrou={entrou} />
  if (error) return <div className="mx-auto max-w-sm p-6"><ErroDe erro={error} oque="o console" tentar={() => void refetch()} /></div>
  if (!operador) return <Carregando className="flex justify-center py-12" />
  if (pathname.replace(/\/$/, '') === '/plataforma') return <Navigate to="/plataforma/instituicoes" replace />
  return <Casca operador={operador} sair={sair} />
}
