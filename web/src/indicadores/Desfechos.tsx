import { plural } from '../formatos'
import { BarraEmpilhada } from './Figura'
import type { Desfechos } from './tipos'

const Pequena = 10

export const comDesfecho = (d: Desfechos) => d.resolvidas + d.transferidas + d.abandonadas + d.encerradas

export function DesfechosDoAssistente({ desfechos }: { desfechos: Desfechos | null }) {
  if (desfechos === null) return <p className="m-0 text-sm text-muted-foreground">Desfechos do assistente aparecem na visão geral, sem filtro de setor.</p>
  const base = comDesfecho(desfechos)
  const segmentos = [
    { nome: 'Resolvida pelo assistente', valor: desfechos.resolvidas, tom: 1 as const },
    { nome: 'Transferida a um atendente', valor: desfechos.transferidas, tom: 2 as const },
    { nome: 'Abandonada', valor: desfechos.abandonadas, tom: 3 as const },
    { nome: 'Encerrada pelo aluno', valor: desfechos.encerradas, tom: 4 as const },
  ]
  const resumo = `Barra empilhada de desfechos das conversas. ${segmentos.map((s) => `${s.nome}: ${s.valor}`).join('. ')}. Total ${base}.`
  return (
    <div className="flex flex-col gap-3">
      <BarraEmpilhada titulo="Desfechos das conversas" resumo={resumo} segmentos={segmentos} base={base} />
      <p className="m-0 text-sm text-muted-foreground">
        {`de ${plural(base, 'conversa', 'conversas')} com desfecho`}
        {desfechos.semDesfecho > 0 && ` · ${desfechos.semDesfecho} ainda sem desfecho`}
      </p>
      {base < Pequena && <p className="m-0 text-sm font-medium">Amostra pequena: menos de 10 conversas</p>}
    </div>
  )
}
