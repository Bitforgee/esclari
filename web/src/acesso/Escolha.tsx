import { useQuery } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { api, ErroApi } from '../api'
import { useTema } from '../aparencia'
import { Input } from '../ui/input'
import { Label } from '../ui/label'

type Item = { slug: string; nome: string; nomeCurto: string | null }

function useAtraso(valor: string, ms: number) {
  const [atual, setAtual] = useState(valor)
  useEffect(() => {
    const espera = setTimeout(() => setAtual(valor), ms)
    return () => clearTimeout(espera)
  }, [valor, ms])
  return atual
}

function aviso(erro: unknown) {
  return erro instanceof ErroApi && erro.status === 429 ? 'Muitas buscas agora. Aguarde um minuto e tente de novo.' : 'Não foi possível buscar agora. Verifique a conexão e tente de novo.'
}

function Resultados({ itens }: { itens: Item[] }) {
  return (
    <ul className="m-0 flex list-none flex-col p-0">
      {itens.map((i) => (
        <li key={i.slug}>
          <Link to={`/${i.slug}`} className="flex min-h-11 items-center gap-2 rounded-xs border-b px-1 hover:bg-muted">
            <span translate="no">{i.nome}</span>
            {i.nomeCurto && <span className="text-sm text-muted-foreground">{i.nomeCurto}</span>}
          </Link>
        </li>
      ))}
    </ul>
  )
}

function Avisos({ ativa, buscando, erro, itens, busca }: { ativa: boolean; buscando: boolean; erro: unknown; itens?: Item[]; busca: string }) {
  const texto = !ativa ? null : buscando ? 'Buscando…' : erro ? aviso(erro) : itens?.length === 0 ? `Nenhuma instituição encontrada para “${busca}”. Confira o nome ou peça o endereço à sua instituição.` : null
  return (
    <div aria-live="polite" className="flex flex-col gap-2">
      {texto && <p className={`m-0 text-sm ${erro && !buscando ? 'text-destructive' : 'text-muted-foreground'}`}>{texto}</p>}
    </div>
  )
}

export function Escolha() {
  const tema = useTema()
  const [texto, setTexto] = useState('')
  const busca = useAtraso(texto.trim(), 300)
  const ativa = busca.length >= 3 && texto.trim().length >= 3
  const { data, error, isFetching } = useQuery({
    queryKey: ['busca-de-instituicao', busca],
    queryFn: () => api<{ itens: Item[] }>(`/api/instituicoes?busca=${encodeURIComponent(busca)}`),
    enabled: ativa,
    retry: false,
  })
  const itens = ativa ? data?.itens : undefined
  return (
    <div className="flex min-h-svh justify-center p-6 pt-[12svh] md:p-10 md:pt-[12svh]">
      <div className="flex w-full max-w-sm flex-col gap-6">
        <h1 translate="no" className="m-0 flex justify-center font-heading text-[28px] leading-tight font-semibold tracking-tight">
          <img src={tema === 'light' ? '/marca/logo-horizontal.svg' : '/marca/logo-horizontal-escuro.svg'} alt="Esclari" width={174} height={40} className="h-10 w-auto" />
        </h1>
        <div className="flex flex-col gap-2">
          <Label htmlFor="instituicao">Sua instituição</Label>
          <Input id="instituicao" value={texto} maxLength={60} autoComplete="off" aria-describedby="instituicao-dica" onChange={(e) => setTexto(e.target.value)} />
          <p id="instituicao-dica" className="m-0 text-sm text-muted-foreground">
            Digite o nome ou a sigla da sua instituição.
          </p>
        </div>
        <Avisos ativa={ativa} buscando={isFetching} erro={error} itens={itens} busca={busca} />
        {!!itens?.length && <Resultados itens={itens} />}
      </div>
    </div>
  )
}
