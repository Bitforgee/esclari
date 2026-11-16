import { Estados } from '../chamado'
import { comUnidade, numero, plural } from '../formatos'
import { BarraEmpilhada } from './Figura'
import type { Detalhe, Tempos } from './tipos'

function Tempo({ rotulo, mediana, p90, verbo }: { rotulo: string; mediana: number | null; p90: number | null; verbo: string }) {
  return (
    <div className="flex flex-col gap-0.5 border-b py-3">
      <dt className="text-sm text-muted-foreground">{rotulo}</dt>
      <dd className="m-0 text-2xl font-semibold tracking-tight tabular-nums">{mediana === null ? '—' : `mediana ${comUnidade(mediana, 'h')}`}</dd>
      {p90 !== null && <dd className="m-0 text-sm text-muted-foreground">{`9 em 10 foram ${verbo} em até ${comUnidade(p90, 'h')}`}</dd>}
    </div>
  )
}

export function TemposDeAtendimento({ tempos }: { tempos: Tempos }) {
  return (
    <dl className="m-0 grid grid-cols-1 @3xl/main:grid-cols-2 @3xl/main:gap-x-10">
      <Tempo rotulo="Tempo até a 1ª resposta" mediana={tempos.primeiraRespostaMedianaHoras} p90={tempos.primeiraRespostaP90Horas} verbo="atendidos" />
      <Tempo rotulo="Tempo de resolução" mediana={tempos.resolucaoMedianaHoras} p90={tempos.resolucaoP90Horas} verbo="resolvidos" />
    </dl>
  )
}

const Tons = { ABERTO: 3, EM_ATENDIMENTO: 1, AGUARDANDO_ALUNO: 2, RESOLVIDO: 4 } as const

export function FilaPorEstado({ detalhe }: { detalhe: Detalhe }) {
  const segmentos = (Object.keys(Tons) as (keyof typeof Tons)[]).map((e) => ({ nome: Estados[e], valor: detalhe.chamadosPorEstado[e] ?? 0, tom: Tons[e] }))
  const total = segmentos.reduce((soma, s) => soma + s.valor, 0)
  if (total === 0) return <p className="m-0 text-sm text-muted-foreground">Nenhum chamado aberto agora.</p>
  const resumo = `Barra empilhada dos chamados abertos agora, por estado. ${segmentos.map((s) => `${s.nome}: ${s.valor}`).join('. ')}.`
  return (
    <div className="flex flex-col gap-3">
      <p className="m-0 text-sm text-muted-foreground">{`Chamados abertos agora: ${numero(total)}`}</p>
      <BarraEmpilhada resumo={resumo} segmentos={segmentos} base={total} />
      {detalhe.maisAntigoDias !== null && <p className="m-0 text-sm font-medium">{`O mais antigo espera há ${plural(detalhe.maisAntigoDias, 'dia', 'dias')}`}</p>}
    </div>
  )
}
