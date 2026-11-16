import type { ReactNode } from 'react'
import { numero, plural } from '../formatos'
import { diaMes, Quadro, useAtivo } from './Figura'
import type { Detalhe, Ponto } from './tipos'

type Granularidade = Detalhe['granularidade']

const rotulo = (g: Granularidade, iso: string) => (g === 'dia' ? diaMes(iso) : `semana de ${diaMes(iso)}`)
const faixa = (pontos: Ponto[]) => `de ${diaMes(pontos[0].inicio)} a ${diaMes(pontos[pontos.length - 1].inicio)}`
const soma = (valores: number[]) => valores.reduce((a, b) => a + b, 0)
const indiceDoMaior = (valores: number[]) => valores.indexOf(Math.max(...valores))

function Eixo({ maximo, pontos, children }: { maximo: number; pontos: Ponto[]; children: ReactNode }) {
  return (
    <div className="grid min-w-0 flex-1 grid-cols-[auto_1fr] gap-x-2">
      <div className="flex flex-col justify-between text-xs text-muted-foreground tabular-nums">
        <span>{numero(maximo)}</span>
        <span>0</span>
      </div>
      {children}
      <span />
      <div className="flex justify-between text-xs text-muted-foreground">
        <span>{diaMes(pontos[0].inicio)}</span>
        <span>{diaMes(pontos[pontos.length - 1].inicio)}</span>
      </div>
    </div>
  )
}

export function ConversasPorPeriodo({ pontos, granularidade }: { pontos: Ponto[]; granularidade: Granularidade }) {
  const valores = pontos.map((p) => p.conversas ?? 0)
  const pico = indiceDoMaior(valores)
  const { ativo, foco } = useAtivo(pontos.length, pico)
  if (pontos.every((p) => p.conversas === null)) return null
  const maximo = Math.max(...valores, 1)
  const titulo = `Conversas por ${granularidade}`
  const picoTexto = valores[pico] === 0 ? 'Nenhuma conversa no período.' : `Maior volume: ${numero(valores[pico])} em ${rotulo(granularidade, pontos[pico].inicio)}`
  const resumo = `Gráfico de colunas. ${titulo}, ${faixa(pontos)}. Total ${numero(soma(valores))}. Pico de ${numero(valores[pico])} em ${rotulo(granularidade, pontos[pico].inicio)}.`
  const tabela = { colunas: [granularidade === 'dia' ? 'Dia' : 'Semana', 'Conversas'], linhas: pontos.map((p, i) => [rotulo(granularidade, p.inicio), numero(valores[i])]) }
  return (
    <Quadro titulo={titulo} resumo={resumo} destaque={picoTexto} foco={foco} tabela={tabela}
      leitura={ativo === null ? '' : `${rotulo(granularidade, pontos[ativo].inicio)}: ${plural(valores[ativo], 'conversa', 'conversas')}`}>
      <Eixo maximo={maximo} pontos={pontos}>
        <svg viewBox={`0 0 ${pontos.length} 100`} preserveAspectRatio="none" className="h-40 w-full" aria-hidden="true">
          {valores.map((v, i) => (
            <rect key={pontos[i].inicio} data-coluna x={i + 0.15} width={0.7} y={100 - (v / maximo) * 100} height={(v / maximo) * 100} fill="var(--grafico-1)"
              stroke={ativo === i ? 'var(--foreground)' : 'none'} strokeWidth={2} vectorEffect="non-scaling-stroke" />
          ))}
        </svg>
      </Eixo>
    </Quadro>
  )
}

const Traco = ({ tracejada }: { tracejada?: boolean }) => (
  <svg viewBox="0 0 24 4" className="h-1 w-6 shrink-0" aria-hidden="true">
    <line x1="0" x2="24" y1="2" y2="2" stroke={tracejada ? 'var(--grafico-2)' : 'var(--grafico-1)'} strokeWidth="3" strokeDasharray={tracejada ? '6 4' : undefined} />
  </svg>
)

const coordenadas = (valores: number[], maximo: number) =>
  (valores.length === 1 ? [0.25, 0.75].map((x) => [x, valores[0]]) : valores.map((v, i) => [i + 0.5, v]))
    .map(([x, v]) => `${x},${100 - (v / maximo) * 100}`)
    .join(' ')

export function ChamadosPorPeriodo({ pontos, granularidade }: { pontos: Ponto[]; granularidade: Granularidade }) {
  const [abertos, resolvidos] = [pontos.map((p) => p.chamadosAbertos), pontos.map((p) => p.chamadosResolvidos)]
  const pico = indiceDoMaior(abertos)
  const { ativo, foco } = useAtivo(pontos.length, pico)
  const maximo = Math.max(...abertos, ...resolvidos, 1)
  const titulo = `Chamados abertos e resolvidos por ${granularidade}`
  const resumo = `Gráfico de linhas. ${titulo}, ${faixa(pontos)}. Total ${numero(soma(abertos))} abertos e ${numero(soma(resolvidos))} resolvidos. Pico de abertos: ${numero(abertos[pico])} em ${rotulo(granularidade, pontos[pico].inicio)}.`
  const tabela = { colunas: [granularidade === 'dia' ? 'Dia' : 'Semana', 'Abertos', 'Resolvidos'], linhas: pontos.map((p, i) => [rotulo(granularidade, p.inicio), numero(abertos[i]), numero(resolvidos[i])]) }
  const ultimo = pontos.length - 1
  return (
    <Quadro titulo={titulo} resumo={resumo} foco={foco} tabela={tabela}
      destaque={`No período: ${numero(soma(abertos))} abertos e ${numero(soma(resolvidos))} resolvidos. Se a linha tracejada fica acima da contínua, a fila cresce.`}
      leitura={ativo === null ? '' : `${rotulo(granularidade, pontos[ativo].inicio)}: ${plural(abertos[ativo], 'aberto', 'abertos')}, ${plural(resolvidos[ativo], 'resolvido', 'resolvidos')}`}>
      <div className="flex flex-col gap-2 @2xl/main:flex-row @2xl/main:items-center">
        <Eixo maximo={maximo} pontos={pontos}>
          <svg viewBox={`0 0 ${pontos.length} 100`} preserveAspectRatio="none" className="h-40 w-full" aria-hidden="true">
            <polyline data-linha="resolvidos" points={coordenadas(resolvidos, maximo)} fill="none" stroke="var(--grafico-1)" strokeWidth={2} vectorEffect="non-scaling-stroke" />
            <polyline data-linha="abertos" points={coordenadas(abertos, maximo)} fill="none" stroke="var(--grafico-2)" strokeWidth={2} strokeDasharray="6 4" vectorEffect="non-scaling-stroke" />
          </svg>
        </Eixo>
        <ul role="list" className="m-0 flex list-none flex-col gap-1 p-0 text-sm">
          <li className="flex items-center gap-2"><Traco tracejada />{`Abertos · ${numero(abertos[ultimo])}`}</li>
          <li className="flex items-center gap-2"><Traco />{`Resolvidos · ${numero(resolvidos[ultimo])}`}</li>
        </ul>
      </div>
    </Quadro>
  )
}
