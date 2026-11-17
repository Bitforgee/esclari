import { ChartColumn } from 'lucide-react'
import { keepPreviousData, useQuery, type UseQueryResult } from '@tanstack/react-query'
import { flexRender, getCoreRowModel, useReactTable, type ColumnDef } from '@tanstack/react-table'
import { useId, useState, type ReactNode } from 'react'
import { useSearchParams } from 'react-router'
import { BotaoAcao } from '../acao'
import { api } from '../api'
import { ErroDe } from '../ErroDe'
import { numero, plural, porcentagem } from '../formatos'
import { dia } from '../Periodo'
import { Input } from '../ui/input'
import { Label } from '../ui/label'
import { BlocoVazio } from '../Tabela'
import { useSessao } from '../sessao'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../ui/table'
import { BaseDeConhecimento } from './BaseDeConhecimento'
import { comDesfecho, DesfechosDoAssistente } from './Desfechos'
import { FilaPorEstado, TemposDeAtendimento } from './Fila'
import { ChamadosPorPeriodo, ConversasPorPeriodo } from './Series'
import type { Detalhe, Indicadores, Linha } from './tipos'
import { DeAnterior, Medida, Taxas, variacaoDaTaxa } from './Variacao'

const ou = (v: number | null, formatar: (v: number) => string) => (v === null ? '—' : formatar(v))
const pct = (v: number | null) => ou(v, porcentagem)
const num = (v: number | null) => ou(v, numero)

const Colunas: ColumnDef<Linha>[] = [
  { accessorKey: 'categoria', header: 'Categoria' },
  { accessorKey: 'total', header: 'Total' },
]

function TabelaRanking({ linhas }: { linhas: Linha[] }) {
  const tabela = useReactTable({ data: linhas, columns: Colunas, getCoreRowModel: getCoreRowModel() })
  const maior = Math.max(...linhas.map((l) => l.total))
  return (
    <Table>
      <TableHeader>
        {tabela.getHeaderGroups().map((grupo) => (
          <TableRow key={grupo.id}>
            {grupo.headers.map((h) => (
              <TableHead key={h.id} className="last:text-end">
                {flexRender(h.column.columnDef.header, h.getContext())}
              </TableHead>
            ))}
          </TableRow>
        ))}
      </TableHeader>
      <TableBody>
        {linhas.length === 0 && (
          <TableRow>
            <TableCell colSpan={2}>—</TableCell>
          </TableRow>
        )}
        {tabela.getRowModel().rows.map((linha) => (
          <TableRow key={linha.id}>
            {linha.getVisibleCells().map((celula) => (
              <TableCell key={celula.id} className="first:w-full first:whitespace-normal first:[overflow-wrap:anywhere] last:text-end last:tabular-nums">
                {flexRender(celula.column.columnDef.cell, celula.getContext())}
                {celula.column.id === 'categoria' && (
                  <div data-barra aria-hidden="true" className="mt-1 h-1.5 rounded-xs bg-grafico-1" style={{ width: maior > 0 ? `${(linha.original.total / maior) * 100}%` : '0%' }} />
                )}
              </TableCell>
            ))}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}

export function Ranking({ titulo, linhas }: { titulo: string; linhas: Linha[] }) {
  return (
    <section className="flex min-w-0 flex-col gap-2">
      <h3 className="m-0 text-sm font-medium">{titulo}</h3>
      <TabelaRanking linhas={linhas} />
    </section>
  )
}

function Secao({ titulo, children }: { titulo: string; children: ReactNode }) {
  const id = useId()
  return (
    <section aria-labelledby={id} className="flex min-w-0 flex-col gap-4">
      <h2 id={id} className="m-0 text-base font-semibold">
        {titulo}
      </h2>
      {children}
    </section>
  )
}

function DoDetalhe({ consulta, children }: { consulta: UseQueryResult<Detalhe>; children: (d: Detalhe) => ReactNode }) {
  if (consulta.error) return <ErroDe erro={consulta.error} oque="os gráficos" tentar={() => void consulta.refetch()} />
  if (!consulta.data) return <div className="min-h-40 text-sm text-muted-foreground">Carregando…</div>
  return children(consulta.data)
}

type Contexto = { dados: Indicadores; anterior?: Indicadores; detalhe: UseQueryResult<Detalhe> }

const Notas: Record<string, string> = {
  Autoatendimento: 'Resolvidas pelo assistente, sobre as conversas com desfecho.',
  Escalonamento: 'Transferidas a um atendente, sobre as conversas com desfecho.',
  Abandono: 'Conversas abandonadas, sobre as conversas com desfecho.',
  'Perguntas sem artigo': 'Conversas em que o assistente não achou fonte publicada.',
  'Roteamento correto': 'Resolvidos sem encaminhamento entre setores.',
  Reabertura: 'Resolvidos que o aluno reabriu.',
}

function Taxa({ rotulo, c }: { rotulo: string; c: Contexto }) {
  const taxa = Taxas.find((t) => t.rotulo === rotulo)!
  return <Medida rotulo={rotulo} valor={pct(c.dados[taxa.chave] as number | null)} variacao={variacaoDaTaxa(taxa, c.dados, c.anterior)} nota={Notas[rotulo]} />
}

function Resumo({ c }: { c: Contexto }) {
  const desfechos = c.detalhe.data?.desfechos
  const variacao = variacaoDaTaxa(Taxas[0], c.dados, c.anterior)
  return (
    <Secao titulo="Resumo">
      <dl className="m-0 flex flex-col gap-1">
        <dt className="text-sm text-muted-foreground">Indicador principal</dt>
        <dd className="m-0 flex flex-wrap items-baseline gap-x-4">
          <div className="font-heading text-[40px] leading-tight font-semibold tabular-nums">{pct(c.dados.indicadorPrincipal)}</div>
          {variacao && <DeAnterior texto={variacao} />}
        </dd>
        <dd className="m-0 text-sm text-muted-foreground">Resolvidas sem reabertura, sobre as conversas com desfecho.</dd>
        {desfechos && <dd className="m-0 text-sm text-muted-foreground">{`de ${plural(comDesfecho(desfechos), 'conversa', 'conversas')} com desfecho`}</dd>}
      </dl>
    </Secao>
  )
}

function Painel({ c }: { c: Contexto }) {
  const { dados } = c
  const rankings = [
    ['Categorias por conversas', dados.principaisCategorias.porConversas],
    ['Categorias por chamados', dados.principaisCategorias.porChamados],
  ] as const
  return (
    <div className="flex flex-col gap-10">
      <Resumo c={c} />
      <Secao titulo="O assistente está resolvendo?">
        <DoDetalhe consulta={c.detalhe}>{(d) => <DesfechosDoAssistente desfechos={d.desfechos} />}</DoDetalhe>
        <dl className="m-0 grid grid-cols-1 @3xl/main:grid-cols-2 @3xl/main:gap-x-10">
          {['Autoatendimento', 'Escalonamento', 'Abandono', 'Perguntas sem artigo'].map((r) => (
            <Taxa key={r} rotulo={r} c={c} />
          ))}
        </dl>
      </Secao>
      <Secao titulo="Quanto chega e como evolui?">
        <dl className="m-0 grid grid-cols-1 @3xl/main:grid-cols-3 @3xl/main:gap-x-10">
          <Medida rotulo="Alunos ativos" valor={num(dados.alunosAtivos)} />
          <Medida rotulo="Conversas" valor={num(dados.conversas)} />
          <Medida rotulo="Chamados resolvidos" valor={num(dados.chamadosResolvidos)} />
        </dl>
        <DoDetalhe consulta={c.detalhe}>
          {(d) => (
            <div className="grid grid-cols-1 gap-8 @3xl/main:grid-cols-2">
              <ConversasPorPeriodo pontos={d.pontos} granularidade={d.granularidade} />
              <ChamadosPorPeriodo pontos={d.pontos} granularidade={d.granularidade} />
            </div>
          )}
        </DoDetalhe>
      </Secao>
      <Secao titulo="A equipe responde a tempo?">
        <DoDetalhe consulta={c.detalhe}>
          {(d) => (
            <>
              <TemposDeAtendimento tempos={d.tempos} />
              <FilaPorEstado detalhe={d} />
            </>
          )}
        </DoDetalhe>
      </Secao>
      <Secao titulo="Qualidade do atendimento">
        <dl className="m-0 grid grid-cols-1 @3xl/main:grid-cols-2 @3xl/main:gap-x-10">
          <Taxa rotulo="Roteamento correto" c={c} />
          <Taxa rotulo="Reabertura" c={c} />
        </dl>
      </Secao>
      <Secao titulo="O que mais pesa">
        <div className="grid grid-cols-1 gap-8 @4xl/main:grid-cols-2">
          {rankings
            .filter(([, r]) => r !== null)
            .map(([titulo, r]) => (
              <Ranking key={titulo} titulo={titulo} linhas={r!} />
            ))}
        </div>
      </Secao>
    </div>
  )
}

export function periodoAnterior(de: string, ate: string) {
  if (!de || !ate) return null
  const [d, a] = [de, ate].map((v) => v.split('-').map(Number))
  const dias = Math.round((Date.UTC(a[0], a[1] - 1, a[2]) - Date.UTC(d[0], d[1] - 1, d[2])) / 86_400_000) + 1
  const antes = (k: number) => dia(new Date(d[0], d[1] - 1, d[2] - k))
  return dias < 1 ? null : { de: antes(dias), ate: antes(1) }
}

function useAnterior(consulta: { de: string; ate: string }, provisorio: boolean) {
  const antes = periodoAnterior(consulta.de, consulta.ate)
  const { data } = useQuery({
    queryKey: ['indicadores-anterior', consulta],
    queryFn: () => api<Indicadores>(`/api/indicadores?de=${antes!.de}&ate=${antes!.ate}`),
    enabled: !!antes,
  })
  return provisorio ? undefined : data
}

const dataValida = (v: string | null): v is string => !!v && /^\d{4}-\d{2}-\d{2}$/.test(v) && dia(new Date(`${v}T00:00:00`)) === v

function periodoDaUrl(params: URLSearchParams, padrao: { de: string; ate: string }) {
  const [de, ate] = [params.get('de'), params.get('ate')]
  return dataValida(de) && dataValida(ate) && de <= ate ? { de, ate } : padrao
}

export function Indicadores() {
  const { me } = useSessao()
  const hoje = new Date()
  const [params, setParams] = useSearchParams()
  const [inicial] = useState(() => periodoDaUrl(params, { de: dia(new Date(hoje.getFullYear(), hoje.getMonth(), 1)), ate: dia(hoje) }))
  const [de, setDe] = useState(inicial.de)
  const [ate, setAte] = useState(inicial.ate)
  const [consulta, setConsulta] = useState({ ...inicial, vez: 0 })
  const { data: dados, error, isFetching, isPlaceholderData, refetch } = useQuery({
    queryKey: ['indicadores', consulta],
    queryFn: () => api<Indicadores>(`/api/indicadores?de=${consulta.de}&ate=${consulta.ate}`),
    placeholderData: keepPreviousData,
  })
  const detalhe = useQuery({
    queryKey: ['indicadores-detalhe', consulta],
    queryFn: () => api<Detalhe>(`/api/indicadores/detalhe?de=${consulta.de}&ate=${consulta.ate}`),
    placeholderData: keepPreviousData,
  })
  const anterior = useAnterior(consulta, isPlaceholderData)

  return (
    <div className="flex flex-col gap-6">
      <form
        className="grid grid-cols-2 gap-2 @3xl/main:flex @3xl/main:items-end"
        onSubmit={(e) => {
          e.preventDefault()
          setConsulta((c) => ({ de, ate, vez: c.vez + 1 }))
          setParams({ de, ate }, { replace: true })
        }}
      >
        <div className="flex flex-col gap-2">
          <Label htmlFor="de">De</Label>
          <Input id="de" name="de" autoComplete="off" type="date" value={de} onChange={(e) => setDe(e.target.value)} />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="ate">Até</Label>
          <Input id="ate" name="ate" autoComplete="off" type="date" value={ate} onChange={(e) => setAte(e.target.value)} />
        </div>
        <BotaoAcao type="submit" className="col-span-2 @3xl/main:col-span-1" pendente={isFetching}>
          Consultar
        </BotaoAcao>
      </form>
      <ErroDe erro={error} oque="os indicadores" tentar={() => void refetch()} />
      {dados?.semDados && <BlocoVazio Icone={ChartColumn} frase="Sem atendimentos neste período." />}
      {dados && !dados.semDados && <Painel c={{ dados, anterior: anterior?.semDados ? undefined : anterior, detalhe }} />}
      {me.papeis.includes('ADMINISTRADOR') && (
        <Secao titulo="A base de conhecimento está em dia?">
          <BaseDeConhecimento />
        </Secao>
      )}
    </div>
  )
}
