import { Link, useParams } from 'react-router'
import { comUnidade, numero, plural, porcentagem } from '../formatos'
import { useSessao } from '../sessao'
import { Button } from '../ui/button'
import { Skeleton } from '../ui/skeleton'
import { VerTabela } from './Figura'
import type { Prazos } from './tipos'
import { Medida, textoDaVariacao } from './Variacao'

type Medicao = { rotulo: string; meta: number; atual: number | null; anterior: number | null }

export const CoresDosPrazos = { anterior: 'var(--grafico-2)', atual: 'var(--grafico-1)' }

const pct = (v: number | null) => (v === null ? '—' : porcentagem(v))
const dentro = (v: number | null) => (v === null ? '—' : `${porcentagem(v)} dentro da meta`)
const variacao = (m: Medicao) => (m.atual === null || m.anterior === null ? undefined : textoDaVariacao(m.atual, m.anterior, 'subir'))

function medicoes(p: Prazos, anterior?: Prazos): Medicao[] {
  return [
    { rotulo: 'Primeira resposta', meta: p.primeiraRespostaMetaHoras, atual: p.primeiraRespostaNoPrazo, anterior: anterior?.primeiraRespostaNoPrazo ?? null },
    { rotulo: 'Resolução', meta: p.resolucaoMetaHoras, atual: p.resolucaoNoPrazo, anterior: anterior?.resolucaoNoPrazo ?? null },
  ]
}

function Barra({ rotulo, valor, cor }: { rotulo: string; valor: number | null; cor: string }) {
  return (
    <div className="flex items-center gap-2 text-xs">
      <span className="w-28 shrink-0 text-muted-foreground">{rotulo}</span>
      <div className="h-3 min-w-0 flex-1">
        <div data-barra="" className="h-3 rounded-xs" style={{ width: `${(valor ?? 0) * 100}%`, background: cor }} />
      </div>
      <span className="w-16 shrink-0 text-end tabular-nums">{pct(valor)}</span>
    </div>
  )
}

function GraficoDosPrazos({ lista }: { lista: Medicao[] }) {
  const titulo = 'Dentro da meta, por período'
  const resumo = `Gráfico de barras. ${titulo}. ${lista.map((m) => `${m.rotulo}: período anterior ${pct(m.anterior)}, este período ${pct(m.atual)}`).join('. ')}.`
  const tabela = { colunas: ['Medida', 'Meta', 'Período anterior', 'Este período'], linhas: lista.map((m) => [m.rotulo, comUnidade(m.meta, 'h'), pct(m.anterior), pct(m.atual)]) }
  return (
    <figure className="m-0 flex min-w-0 flex-col gap-3">
      <figcaption className="text-sm font-medium">{titulo}</figcaption>
      <div role="img" aria-label={resumo} className="flex flex-col gap-3">
        {lista.map((m) => (
          <div key={m.rotulo} className="flex flex-col gap-1">
            <span className="text-sm">{m.rotulo}</span>
            <Barra rotulo="Período anterior" valor={m.anterior} cor={CoresDosPrazos.anterior} />
            <Barra rotulo="Este período" valor={m.atual} cor={CoresDosPrazos.atual} />
          </div>
        ))}
      </div>
      <VerTabela tabela={tabela} titulo={titulo} />
    </figure>
  )
}

function EmAtraso({ p }: { p: Prazos }) {
  const { slug } = useParams()
  const { me } = useSessao()
  if (p.emAtrasoAgora === 0) return <p className="m-0 text-sm text-muted-foreground">Nenhum chamado em atraso</p>
  const lista = me.papeis.includes('ADMINISTRADOR') ? 'chamados' : 'fila'
  return (
    <div className="flex flex-col items-start gap-2">
      <p className="m-0 text-sm font-medium">{`Em atraso agora: ${numero(p.emAtrasoAgora)}`}</p>
      {p.maisAntigoEmAtrasoDias !== null && <p className="m-0 text-sm">{`O mais antigo está atrasado há ${plural(p.maisAntigoEmAtrasoDias, 'dia', 'dias')}`}</p>}
      <Button asChild variant="outline" size="sm">
        <Link to={`/${slug}/${lista}?atraso=true`}>Ver chamados em atraso</Link>
      </Button>
    </div>
  )
}

export function PrazosDaEquipe({ p, anterior }: { p: Prazos; anterior?: Prazos }) {
  const lista = medicoes(p, anterior)
  return (
    <div className="flex flex-col gap-4" data-secao="prazos">
      <p className="m-0 text-sm">{`Meta: primeira resposta em ${comUnidade(p.primeiraRespostaMetaHoras, 'h')}; resolução em ${comUnidade(p.resolucaoMetaHoras, 'h')}`}</p>
      <EmAtraso p={p} />
      <dl className="m-0 grid grid-cols-1 @3xl/main:grid-cols-2 @3xl/main:gap-x-10">
        {lista.map((m) => (
          <Medida key={m.rotulo} rotulo={m.rotulo} valor={dentro(m.atual)} variacao={variacao(m)} />
        ))}
      </dl>
      <GraficoDosPrazos lista={lista} />
    </div>
  )
}

export function EsqueletoDosPrazos() {
  return (
    <div role="status" aria-label="Carregando" data-esqueleto="prazos" className="flex flex-col gap-3">
      <Skeleton className="h-5 w-3/4" />
      <Skeleton className="h-16 w-full" />
      <Skeleton className="h-24 w-full" />
    </div>
  )
}
