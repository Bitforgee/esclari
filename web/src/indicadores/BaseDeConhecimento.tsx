import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router'
import { api } from '../api'
import { Carregando } from '../Carregando'
import { ErroDe } from '../ErroDe'
import { numero, plural } from '../formatos'
import { Skeleton } from '../ui/skeleton'
import { Medida } from './Variacao'

type Contagem = { vigentes: number; venceEm30Dias: number; vencidos: number }
export type ResumoDaValidade = { artigos: Contagem; documentos: Contagem }

const Grupos = [
  ['Vigentes', 'vigentes'],
  ['Vencem em 30 dias', 'venceEm30Dias'],
  ['Vencidos', 'vencidos'],
] as const

const nota = (r: ResumoDaValidade, chave: keyof Contagem) => `${plural(r.artigos[chave], 'artigo', 'artigos')} e ${plural(r.documentos[chave], 'documento', 'documentos')}`

const vazia = (r: ResumoDaValidade) => Grupos.every(([, chave]) => r.artigos[chave] + r.documentos[chave] === 0)

const Atalho = 'inline-flex min-h-11 items-center text-sm underline underline-offset-4'

export function BaseDeConhecimento() {
  const { slug } = useParams()
  const { data, error, refetch } = useQuery({ queryKey: ['validade-do-conhecimento'], queryFn: () => api<ResumoDaValidade>('/api/conhecimento/validade') })
  if (error) return <ErroDe erro={error} oque="a validade da base de conhecimento" tentar={() => void refetch()} />
  if (!data)
    return (
      <Carregando className="flex flex-col gap-2">
        {[0, 1, 2].map((i) => (
          <Skeleton key={i} className="h-14 w-full" />
        ))}
      </Carregando>
    )
  if (vazia(data)) return <p className="m-0 text-sm text-muted-foreground">Nenhum conteúdo publicado ainda</p>
  return (
    <>
      <dl className="m-0 grid grid-cols-1 @3xl/main:grid-cols-3 @3xl/main:gap-x-10">
        {Grupos.map(([rotulo, chave]) => (
          <Medida key={chave} rotulo={rotulo} valor={numero(data.artigos[chave] + data.documentos[chave])} nota={nota(data, chave)} />
        ))}
      </dl>
      <div className="flex flex-wrap gap-x-6">
        <Link className={Atalho} to={`/${slug}/artigos?validade=vencidos`}>
          Ver vencidos
        </Link>
        <Link className={Atalho} to={`/${slug}/documentos?validade=vencidos`}>
          Ver documentos vencidos
        </Link>
      </div>
    </>
  )
}
