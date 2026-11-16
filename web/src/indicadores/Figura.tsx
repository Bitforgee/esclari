import { useState, type KeyboardEvent, type PointerEvent, type ReactNode } from 'react'
import { numero, porcentagem } from '../formatos'
import { Button } from '../ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../ui/table'

export const diaMes = (iso: string) => `${iso.slice(8, 10)}/${iso.slice(5, 7)}`

export type Segmento = { nome: string; valor: number; tom: 1 | 2 | 3 | 4 }

const tom = (n: number) => `var(--grafico-${n})`

export function BarraEmpilhada({ titulo, resumo, segmentos, base }: { titulo: string; resumo: string; segmentos: Segmento[]; base: number }) {
  const parte = (v: number) => (base > 0 ? v / base : 0)
  const tabela = { colunas: ['Item', 'Quantidade', 'Percentual'], linhas: segmentos.map((s) => [s.nome, numero(s.valor), porcentagem(parte(s.valor))]) }
  return (
    <div className="flex flex-col gap-3">
      <div role="img" aria-label={resumo} className="flex h-6 gap-0.5 overflow-hidden rounded-xs bg-background">
        {segmentos
          .filter((s) => s.valor > 0)
          .map((s) => (
            <div key={s.nome} data-segmento={s.nome} style={{ width: `${parte(s.valor) * 100}%`, background: tom(s.tom) }} />
          ))}
      </div>
      <ul role="list" className="m-0 flex list-none flex-col gap-1 p-0 text-sm">
        {segmentos.map((s) => (
          <li key={s.nome} className="flex items-center gap-2">
            <span aria-hidden="true" className="size-3 shrink-0 rounded-xs" style={{ background: tom(s.tom) }} />
            <span>{`${s.nome} · ${numero(s.valor)} · ${porcentagem(parte(s.valor))}`}</span>
          </li>
        ))}
      </ul>
      <VerTabela tabela={tabela} titulo={titulo} />
    </div>
  )
}

export function useAtivo(quantos: number, inicial: number) {
  const [ativo, setAtivo] = useState<number | null>(null)
  const ir = (i: number) => setAtivo(Math.min(quantos - 1, Math.max(0, i)))
  const teclas: Record<string, number> = { ArrowRight: 1, ArrowLeft: -1 }
  const foco = {
    tabIndex: 0,
    onFocus: () => setAtivo((a) => a ?? inicial),
    onBlur: () => setAtivo(null),
    onKeyDown: (e: KeyboardEvent) => {
      if (!(e.key in teclas)) return
      e.preventDefault()
      ir((ativo ?? inicial) + teclas[e.key])
    },
    onPointerDown: (e: PointerEvent<HTMLElement>) => {
      const caixa = e.currentTarget.getBoundingClientRect()
      if (caixa.width > 0) ir(Math.floor(((e.clientX - caixa.left) / caixa.width) * quantos))
    },
  }
  return { ativo, foco }
}

export type TabelaDoGrafico = { colunas: string[]; linhas: string[][] }

export function VerTabela({ tabela, titulo }: { tabela: TabelaDoGrafico; titulo: string }) {
  const [aberta, setAberta] = useState(false)
  return (
    <div className="flex flex-col gap-2">
      <Button type="button" variant="outline" size="xs" className="self-start" aria-expanded={aberta} aria-label={`${aberta ? 'Ocultar' : 'Ver'} como tabela: ${titulo}`} onClick={() => setAberta(!aberta)}>
        {aberta ? 'Ocultar tabela' : 'Ver como tabela'}
      </Button>
      {aberta && (
        <Table aria-label={titulo}>
          <TableHeader>
            <TableRow>
              {tabela.colunas.map((c) => (
                <TableHead key={c}>{c}</TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {tabela.linhas.map((linha) => (
              <TableRow key={linha[0]}>
                {linha.map((celula, i) => (
                  <TableCell key={tabela.colunas[i]} className="tabular-nums">
                    {celula}
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  )
}

export function Quadro({ titulo, resumo, destaque, leitura, tabela, foco, children }: {
  titulo: string
  resumo: string
  destaque: string
  leitura: string
  tabela: TabelaDoGrafico
  foco: ReturnType<typeof useAtivo>['foco']
  children: ReactNode
}) {
  return (
    <figure className="m-0 flex min-w-0 flex-col gap-2">
      <figcaption className="flex flex-col gap-0.5">
        <span className="text-sm font-medium">{titulo}</span>
        <span className="text-sm text-muted-foreground">{destaque}</span>
      </figcaption>
      <div role="group" aria-label={`${titulo}. Use as setas para ver cada ponto.`} className="rounded-sm" {...foco}>
        <div role="img" aria-label={resumo}>
          {children}
        </div>
      </div>
      <p aria-live="polite" className="m-0 min-h-5 text-sm tabular-nums">
        {leitura}
      </p>
      <VerTabela tabela={tabela} titulo={titulo} />
    </figure>
  )
}
