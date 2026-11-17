import { keepPreviousData, useInfiniteQuery, useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router'
import { api } from '../api'
import { Estados } from '../chamado'
import { ErroDe } from '../ErroDe'
import { Badge } from '../ui/badge'
import { TableCell, TableRow } from '../ui/table'
import { AlarmClock, Inbox, MessageSquare, SearchX } from 'lucide-react'
import { BotaoAcao } from '../acao'
import { plural } from '../formatos'
import { BotoesDaBusca, CampoDeBusca, consultaDaApi, Etiquetas, etiquetasDe, lerFiltros, PainelDeFiltros, paraUrl, rotuloDaOrdem, semAs, SemFiltros, type Escopo, type Filtros, type Opcoes } from './Busca'
import { Button } from '../ui/button'
import { abertoHa } from '../datas'
import { Literal } from '../Literal'
import { Marca } from '../Marca'
import { useSessao } from '../sessao'
import { CelulaLargo, Detalhe, Principal, Tabela, useEstreito, useMostrarMais, type Vazio } from '../Tabela'
import { ChaveDaBusca, useFilaAberta } from '../tempo-real'

type ItemMeu = { numero: string; titulo: string; estado: string; criadoEm: string }
type ItemFila = ItemMeu & { categoria: string; responsavel: string | null; setor?: string; atraso?: string | null }

const CoresDoEstado: Record<string, string> = {
  ABERTO: 'bg-accent text-foreground',
  EM_ATENDIMENTO: 'bg-accent text-foreground',
  AGUARDANDO_ALUNO: 'bg-warning-subtle text-foreground',
  RESOLVIDO: 'bg-success/15 text-foreground',
  FECHADO: 'bg-success/15 text-foreground',
}
export const SeloDoChamado = ({ estado }: { estado: string }) => <Badge className={CoresDoEstado[estado]}>{Estados[estado]}</Badge>

const RotuloDoAtraso: Record<string, string> = { PRIMEIRA_RESPOSTA: 'Atrasado: 1ª resposta', RESOLUCAO: 'Atrasado: resolução' }

export function SeloDoAtraso({ atraso, className }: { atraso?: string | null; className?: string }) {
  if (!atraso) return null
  return (
    <div className={className} data-atraso={atraso}>
      <Badge variant="outline">
        <AlarmClock aria-hidden="true" />
        {RotuloDoAtraso[atraso] ?? atraso}
      </Badge>
    </div>
  )
}

export const ProximoPasso: Record<string, string> = {
  ABERTO: 'A equipe vai responder',
  EM_ATENDIMENTO: 'A equipe vai responder',
  AGUARDANDO_ALUNO: 'Responda para continuar',
  RESOLVIDO: 'Resolvida',
  FECHADO: 'Encerrada',
}

type Meta = { aberto: string; categoria?: string; responsavel?: string; proximo?: string }

const metaDa = (c: ItemMeu, aberto: string, fila?: boolean): Meta => (fila ? { aberto, categoria: (c as ItemFila).categoria, responsavel: responsavel((c as ItemFila).responsavel) } : { aberto, proximo: ProximoPasso[c.estado] })

function LinhaCompacta({ c, caminho, meta }: { c: ItemMeu; caminho: string; meta: Meta }) {
  return (
    <div className="flex min-w-0 flex-col gap-1">
      <p className="m-0 [overflow-wrap:anywhere]" data-linha="assunto">
        <Link to={caminho} className="block text-foreground no-underline hover:underline" onClick={(e) => e.stopPropagation()}>
          <span className="line-clamp-2">
            <Literal className="font-medium">{c.numero}</Literal> <span>{c.titulo}</span>
          </span>
        </Link>
      </p>
      <div className="flex items-start gap-2 text-xs text-muted-foreground" data-linha="meta">
        <SeloDoChamado estado={c.estado} />
        <SeloDoAtraso atraso={(c as ItemFila).atraso} />
        <span className="min-w-0 flex-1 leading-[22px]">{[meta.aberto, meta.categoria, meta.responsavel].filter(Boolean).join(' · ')}</span>
      </div>
      {meta.proximo && (
        <p className="m-0 text-xs text-muted-foreground" data-linha="proximo">
          {meta.proximo}
        </p>
      )}
    </div>
  )
}

type Larga<T> = { titulo: string; valor: (c: T, aberto: string) => string; celula?: (c: T, aberto: string) => string; uma?: boolean }

function LinhaDaTabela<T extends ItemMeu>({ c, caminho, aberto, largas, fila }: { c: T; caminho: string; aberto: string; largas?: Larga<T>[]; fila?: boolean }) {
  return (
    <>
      <TableCell className="font-medium">
        <Link to={caminho} className="text-foreground no-underline hover:underline" onClick={(e) => e.stopPropagation()}>
          <Literal>{c.numero}</Literal>
        </Link>
      </TableCell>
      <TableCell className={Principal}>
        {c.titulo}
        {largas && <Detalhe>{largas.map((l) => l.valor(c, aberto)).join(' · ')}</Detalhe>}
      </TableCell>
      {largas?.map((l) => (
        <CelulaLargo key={l.titulo} className={l.uma ? 'whitespace-nowrap' : undefined}>
          {(l.celula ?? l.valor)(c, aberto)}
        </CelulaLargo>
      ))}
      <TableCell data-celula="selo" className="whitespace-normal">
        <SeloDoChamado estado={c.estado} />
        <SeloDoAtraso atraso={(c as ItemMeu & Partial<ItemFila>).atraso} className="mt-1" />
        {!fila && (
          <div className="mt-1 flex flex-col text-xs text-muted-foreground">
            <span>{aberto}</span>
            <span>{ProximoPasso[c.estado]}</span>
          </div>
        )}
      </TableCell>
    </>
  )
}

function ListaDeChamados({ url, vazio }: { url: string; vazio: Vazio }) {
  const { slug } = useParams()
  const { me } = useSessao()
  const navegar = useNavigate()
  const estreito = useEstreito()
  const { data: itens, error, refetch } = useQuery({ queryKey: [url], queryFn: () => api<ItemMeu[]>(url) })
  const { visiveis, mais } = useMostrarMais(itens)
  const colunas = [{ titulo: 'Número' }, { titulo: 'Título' }, { titulo: 'Estado' }]
  return (
    <div className="flex flex-col gap-2">
      <ErroDe erro={error} oque="suas solicitações" tentar={() => void refetch()} />
      <Tabela colunas={colunas} itens={itens} vazio={vazio} erro={error} densa>
        {visiveis?.map((c) => {
          const caminho = `/${slug}/chamados/${c.numero}`
          const aberto = abertoHa(c.criadoEm, me.instituicao.fuso)
          const meta = metaDa(c, aberto)
          return (
            <TableRow key={c.numero} className="cursor-pointer max-md:px-0 max-md:py-3" onClick={() => navegar(caminho)}>
              {estreito ? <LinhaCompacta c={c} caminho={caminho} meta={meta} /> : <LinhaDaTabela c={c} caminho={caminho} aberto={aberto} />}
            </TableRow>
          )
        })}
      </Tabela>
      {mais}
    </div>
  )
}

export const responsavel = (nome: string | null) => (nome === null ? 'sem responsável' : nome)

type Pagina = { itens: ItemFila[]; total: number; pagina: number; porPagina: number }

const proxima = (p: Pagina) => (p.pagina * p.porPagina < p.total ? p.pagina + 1 : undefined)
const unicos = (paginas: Pagina[]) => [...new Map(paginas.flatMap((p) => p.itens).map((c) => [c.numero, c])).values()]

function useBusca(escopo: Escopo, filtros: Filtros) {
  const consulta = consultaDaApi(escopo, filtros)
  const busca = useInfiniteQuery({
    queryKey: [...ChaveDaBusca, consulta],
    queryFn: ({ pageParam }) => api<Pagina>(`/api/chamados/busca?${consulta}${pageParam > 1 ? `&pagina=${pageParam}` : ''}`),
    initialPageParam: 1,
    getNextPageParam: proxima,
    placeholderData: keepPreviousData,
  })
  const opcoes = useQuery({ queryKey: ['filtros-da-busca', escopo], queryFn: () => api<Opcoes>(`/api/chamados/filtros?escopo=${escopo}`) })
  return { busca, opcoes: opcoes.data, consulta, itens: busca.data && unicos(busca.data.pages), total: busca.data?.pages[0]?.total }
}

function useFiltrosDaUrl() {
  const [params, setParams] = useSearchParams()
  const filtros = lerFiltros(params)
  return { filtros, mudar: (f: Filtros, trocar = false) => setParams(paraUrl(f), { replace: trocar }) }
}

function Contagem({ total, ordem }: { total?: number; ordem: string }) {
  if (total === undefined) return null
  return (
    <p className="m-0 text-sm text-muted-foreground">
      <span>{plural(total, 'chamado', 'chamados')}</span> · <span>{rotuloDaOrdem(ordem)}</span>
    </p>
  )
}

type Lista = { escopo: Escopo; oque: string; vazio: Vazio; largas: Larga<ItemFila>[] }

function ListaBuscada({ escopo, oque, vazio, largas }: Lista) {
  const { slug } = useParams()
  const { me } = useSessao()
  const navegar = useNavigate()
  const estreito = useEstreito()
  const [painel, setPainel] = useState(false)
  const { filtros, mudar } = useFiltrosDaUrl()
  const { busca, opcoes, consulta, itens, total } = useBusca(escopo, filtros)
  const etiquetas = etiquetasDe(filtros, opcoes)
  const limpar = () => mudar({ ...SemFiltros, ordem: filtros.ordem })
  const semResultado = { Icone: SearchX, frase: 'Nenhum chamado com esses filtros.', acao: <Button variant="outline" onClick={limpar}>Limpar filtros</Button> }
  const colunas = [{ titulo: 'Número' }, { titulo: 'Título' }, ...largas.map(({ titulo }) => ({ titulo, largo: true })), { titulo: 'Estado' }]
  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col gap-2 md:flex-row md:items-center" data-barra-da-busca="">
        <CampoDeBusca q={filtros.q} buscar={(q) => mudar({ ...filtros, q }, true)} />
        <Marca id="filtro-atraso" rotulo="Só atrasados" marcado={filtros.atraso === 'true'} mudar={(v) => mudar({ ...filtros, atraso: v ? 'true' : '' })} />
        <BotoesDaBusca ativos={etiquetas.filter((e) => e.rotulo !== 'Busca').length} abrir={() => setPainel(true)} exportar={`/api/chamados/exportar?${consulta}`} />
      </div>
      <Etiquetas etiquetas={etiquetas} tirar={(e) => mudar(semAs(filtros, e.chaves))} limpar={limpar} />
      <PainelDeFiltros aberto={painel} setAberto={setPainel} filtros={filtros} opcoes={opcoes} aplicar={(f) => { setPainel(false); mudar(f) }} />
      <ErroDe erro={busca.error} oque={oque} tentar={() => void busca.refetch()} />
      <Contagem total={total} ordem={filtros.ordem} />
      <Tabela colunas={colunas} itens={itens} vazio={etiquetas.length > 0 ? semResultado : vazio} erro={busca.error} densa ocupada={busca.isPlaceholderData}>
        {itens?.map((c) => {
          const caminho = `/${slug}/chamados/${c.numero}`
          const aberto = abertoHa(c.criadoEm, me.instituicao.fuso)
          return (
            <TableRow key={c.numero} className="cursor-pointer max-md:px-0 max-md:py-3" onClick={() => navegar(caminho)}>
              {estreito ? <LinhaCompacta c={c} caminho={caminho} meta={metaDa(c, aberto, true)} /> : <LinhaDaTabela c={c} caminho={caminho} aberto={aberto} largas={largas} fila />}
            </TableRow>
          )
        })}
      </Tabela>
      {busca.hasNextPage && (
        <BotaoAcao type="button" variant="outline" className="self-center" pendente={busca.isFetchingNextPage} onClick={() => void busca.fetchNextPage()}>
          Mostrar mais 50
        </BotaoAcao>
      )}
    </div>
  )
}

const Aberto: Larga<ItemFila> = { titulo: 'Aberto', valor: (_, aberto) => aberto, celula: (_, aberto) => aberto.replace(/^aberto /, ''), uma: true }
const Categoria: Larga<ItemFila> = { titulo: 'Categoria', valor: (c) => c.categoria }
const Responsavel: Larga<ItemFila> = { titulo: 'Responsável', valor: (c) => responsavel(c.responsavel) }

export function Fila() {
  useFilaAberta()
  return <ListaBuscada escopo="fila" oque="a fila" vazio={{ Icone: Inbox, titulo: 'Fila em dia', frase: 'Nenhum chamado na fila dos seus setores.' }} largas={[Categoria, Responsavel, Aberto]} />
}

export function TodosOsChamados() {
  return (
    <ListaBuscada
      escopo="todos"
      oque="os chamados"
      vazio={{ Icone: Inbox, titulo: 'Nenhum chamado', frase: 'Nenhum chamado ainda.' }}
      largas={[{ titulo: 'Setor', valor: (c) => c.setor ?? '' }, Categoria, Responsavel, Aberto]}
    />
  )
}

export function Solicitacoes() {
  const { slug } = useParams()
  return (
    <ListaDeChamados
      url="/api/chamados?escopo=meus"
      vazio={{
        Icone: MessageSquare,
        titulo: 'Nenhuma solicitação',
        frase: 'Você ainda não tem solicitações.',
        acao: (
          <Button asChild>
            <Link to={`/${slug}/assistente`}>Falar com o assistente</Link>
          </Button>
        ),
      }}
    />
  )
}
