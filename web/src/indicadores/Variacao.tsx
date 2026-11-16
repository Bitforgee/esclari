import { decimalFixo } from '../formatos'
import type { Indicadores } from './tipos'

export const MinimoDeCasos = 10

type Taxa = { rotulo: string; chave: keyof Indicadores; melhorSe: 'subir' | 'descer'; casos: 'conversas' | 'chamadosResolvidos' }

export const Taxas: Taxa[] = [
  { rotulo: 'Indicador principal', chave: 'indicadorPrincipal', melhorSe: 'subir', casos: 'conversas' },
  { rotulo: 'Autoatendimento', chave: 'taxaAutoatendimento', melhorSe: 'subir', casos: 'conversas' },
  { rotulo: 'Escalonamento', chave: 'taxaEscalonamento', melhorSe: 'descer', casos: 'conversas' },
  { rotulo: 'Abandono', chave: 'taxaAbandono', melhorSe: 'descer', casos: 'conversas' },
  { rotulo: 'Perguntas sem artigo', chave: 'taxaPerguntasSemArtigo', melhorSe: 'descer', casos: 'conversas' },
  { rotulo: 'Roteamento correto', chave: 'roteamentoCorreto', melhorSe: 'subir', casos: 'chamadosResolvidos' },
  { rotulo: 'Reabertura', chave: 'taxaReabertura', melhorSe: 'descer', casos: 'chamadosResolvidos' },
]

export function textoDaVariacao(atual: number, anterior: number, melhorSe: Taxa['melhorSe']) {
  const pontos = Math.round((atual - anterior) * 1000) / 10
  if (pontos === 0) return 'sem variação'
  const melhor = pontos > 0 === (melhorSe === 'subir')
  return `${pontos > 0 ? '▲' : '▼'} ${decimalFixo(Math.abs(pontos))} ${Math.abs(pontos) < 2 ? 'pt' : 'pts'} · ${melhor ? 'melhor' : 'pior'}`
}

export function variacaoDaTaxa(taxa: Taxa, atual: Indicadores, anterior?: Indicadores) {
  if (!anterior || anterior.semDados || atual.semDados) return undefined
  const [a, b] = [atual[taxa.chave], anterior[taxa.chave]] as (number | null)[]
  const casos = Math.min(atual[taxa.casos] ?? 0, anterior[taxa.casos] ?? 0)
  return a === null || b === null || casos < MinimoDeCasos ? undefined : textoDaVariacao(a, b, taxa.melhorSe)
}

export const DeAnterior = ({ texto }: { texto: string }) => (
  <span className="text-sm font-medium tabular-nums">
    {texto} <span className="font-normal text-muted-foreground">vs. período anterior</span>
  </span>
)

export function Medida({ rotulo, valor, variacao, nota }: { rotulo: string; valor: string; variacao?: string; nota?: string }) {
  return (
    <div className="flex flex-col gap-0.5 border-b py-3">
      <dt className="text-sm text-muted-foreground">{rotulo}</dt>
      <dd className="m-0 flex flex-wrap items-baseline gap-x-3">
        <span className="text-2xl font-semibold tracking-tight tabular-nums">{valor}</span>
        {variacao && <DeAnterior texto={variacao} />}
      </dd>
      {nota && <dd className="m-0 text-sm text-muted-foreground">{nota}</dd>}
    </div>
  )
}
