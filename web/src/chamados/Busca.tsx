import { useMutation } from '@tanstack/react-query'
import { Download, SlidersHorizontal, X } from 'lucide-react'
import { useEffect, useRef, useState, type FormEvent } from 'react'
import { toast } from 'sonner'
import { BotaoAcao } from '../acao'
import { ErroApi } from '../api'
import { Estados } from '../chamado'
import { Marca } from '../Marca'
import { useEstreito } from '../Tabela'
import { Button } from '../ui/button'
import { Input } from '../ui/input'
import { Label } from '../ui/label'
import { NativeSelect, NativeSelectOption } from '../ui/native-select'
import { Sheet, SheetContent, SheetDescription, SheetFooter, SheetHeader, SheetTitle } from '../ui/sheet'

export type Escopo = 'fila' | 'todos'
export type Filtros = { q: string; estado: string[]; setor: string; categoria: string; responsavel: string; de: string; ate: string; ordem: string; atraso: string }
export type Opcao = { id: number; nome: string }
export type Opcoes = { setores: Opcao[]; categorias: Opcao[] }

export const SemFiltros: Filtros = { q: '', estado: [], setor: '', categoria: '', responsavel: '', de: '', ate: '', ordem: '', atraso: '' }

const NaApi: [keyof Filtros, string][] = [
  ['q', 'q'],
  ['estado', 'estado'],
  ['setor', 'setorId'],
  ['categoria', 'categoriaId'],
  ['responsavel', 'responsavel'],
  ['de', 'de'],
  ['ate', 'ate'],
  ['ordem', 'ordem'],
  ['atraso', 'atraso'],
]

export function lerFiltros(p: URLSearchParams): Filtros {
  const um = (k: string) => p.get(k) ?? ''
  return { q: um('q'), estado: p.getAll('estado'), setor: um('setor'), categoria: um('categoria'), responsavel: um('responsavel'), de: um('de'), ate: um('ate'), ordem: um('ordem'), atraso: um('atraso') === 'true' ? 'true' : '' }
}

const pares = (f: Filtros, naApi: boolean) => NaApi.flatMap(([k, api]) => [f[k]].flat().filter(Boolean).map((v) => [naApi ? api : k, v]))

export const consultaDaApi = (escopo: Escopo, f: Filtros) => new URLSearchParams([['escopo', escopo], ...pares(f, true)]).toString()
export const paraUrl = (f: Filtros) => new URLSearchParams(pares(f, false))

const Responsaveis: Record<string, string> = { eu: 'Eu', ninguem: 'Sem responsável' }
const Ordens: Record<string, string> = { '': 'Mais antigos primeiro', recentes: 'Mais recentes primeiro' }
export const rotuloDaOrdem = (ordem: string) => Ordens[ordem] ?? Ordens['']

const nomeDe = (lista: Opcao[] | undefined, id: string) => lista?.find((o) => String(o.id) === id)?.nome ?? `#${id}`
const dataBr = (iso: string) => iso.split('-').reverse().join('/')
function periodo(de: string, ate: string) {
  if (de && ate) return `${dataBr(de)} a ${dataBr(ate)}`
  return de ? `a partir de ${dataBr(de)}` : `até ${dataBr(ate)}`
}

type Definicao = { rotulo: string; chaves: (keyof Filtros)[]; valor: (f: Filtros, o?: Opcoes) => string }
const Definicoes: Definicao[] = [
  { rotulo: 'Busca', chaves: ['q'], valor: (f) => f.q },
  { rotulo: 'Estado', chaves: ['estado'], valor: (f) => f.estado.map((e) => Estados[e] ?? e).join(', ') },
  { rotulo: 'Setor', chaves: ['setor'], valor: (f, o) => nomeDe(o?.setores, f.setor) },
  { rotulo: 'Categoria', chaves: ['categoria'], valor: (f, o) => nomeDe(o?.categorias, f.categoria) },
  { rotulo: 'Responsável', chaves: ['responsavel'], valor: (f) => Responsaveis[f.responsavel] ?? f.responsavel },
  { rotulo: 'Período', chaves: ['de', 'ate'], valor: (f) => periodo(f.de, f.ate) },
  { rotulo: 'Atraso', chaves: ['atraso'], valor: () => 'só atrasados' },
]

export type Etiqueta = { rotulo: string; valor: string; chaves: (keyof Filtros)[] }
export const etiquetasDe = (f: Filtros, o?: Opcoes): Etiqueta[] =>
  Definicoes.filter((d) => d.chaves.some((k) => f[k].length > 0)).map((d) => ({ rotulo: d.rotulo, valor: d.valor(f, o), chaves: d.chaves }))

export const semAs = (f: Filtros, chaves: (keyof Filtros)[]): Filtros => ({ ...f, ...Object.fromEntries(chaves.map((k) => [k, SemFiltros[k]])) })

export function CampoDeBusca({ q, buscar }: { q: string; buscar: (q: string) => void }) {
  const [texto, setTexto] = useState(q)
  const ultima = useRef(buscar)
  ultima.current = buscar
  useEffect(() => setTexto((t) => (t.trim() === q ? t : q)), [q])
  useEffect(() => {
    if (texto.trim() === q) return
    const espera = setTimeout(() => ultima.current(texto.trim()), 300)
    return () => clearTimeout(espera)
  }, [texto, q])
  function enviar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    ultima.current(texto.trim())
    e.currentTarget.querySelector('input')?.blur()
  }
  return (
    <form role="search" onSubmit={enviar} className="min-w-0 flex-1 md:max-w-sm">
      <Label htmlFor="busca-chamados" className="sr-only">
        Buscar por número, título ou aluno
      </Label>
      <Input id="busca-chamados" type="search" enterKeyHint="search" autoComplete="off" placeholder="Buscar por número, título ou aluno" value={texto} onChange={(e) => setTexto(e.target.value)} />
    </form>
  )
}

function Escolha({ id, rotulo, valor, opcoes, mudar }: { id: string; rotulo: string; valor: string; opcoes: [string, string][]; mudar: (v: string) => void }) {
  return (
    <div className="flex flex-col gap-2 [&>[data-slot=native-select-wrapper]]:w-full">
      <Label htmlFor={id}>{rotulo}</Label>
      <NativeSelect id={id} value={valor} onChange={(e) => mudar(e.target.value)} className="w-full">
        {opcoes.map(([v, r]) => (
          <NativeSelectOption key={v} value={v}>
            {r}
          </NativeSelectOption>
        ))}
      </NativeSelect>
    </div>
  )
}

const comVazio = (vazio: string, lista?: Opcao[]): [string, string][] => [['', vazio], ...(lista ?? []).map((o): [string, string] => [String(o.id), o.nome])]

function CamposDoPainel({ r, mudar, opcoes }: { r: Filtros; mudar: (p: Partial<Filtros>) => void; opcoes?: Opcoes }) {
  const alternar = (estado: string, marcado: boolean) => mudar({ estado: marcado ? [...r.estado, estado] : r.estado.filter((e) => e !== estado) })
  return (
    <>
      <fieldset className="flex flex-col gap-1">
        <legend className="mb-1 text-sm font-medium">Estado</legend>
        {Object.entries(Estados).map(([valor, rotulo]) => (
          <Marca key={valor} id={`filtro-estado-${valor}`} rotulo={rotulo} marcado={r.estado.includes(valor)} mudar={(v) => alternar(valor, v)} />
        ))}
      </fieldset>
      <Escolha id="filtro-setor" rotulo="Setor" valor={r.setor} opcoes={comVazio('Todos', opcoes?.setores)} mudar={(setor) => mudar({ setor })} />
      <Escolha id="filtro-categoria" rotulo="Categoria" valor={r.categoria} opcoes={comVazio('Todas', opcoes?.categorias)} mudar={(categoria) => mudar({ categoria })} />
      <Escolha id="filtro-responsavel" rotulo="Responsável" valor={r.responsavel} opcoes={[['', 'Qualquer'], ['eu', 'Eu'], ['ninguem', 'Sem responsável']]} mudar={(responsavel) => mudar({ responsavel })} />
      <fieldset className="flex flex-col gap-2">
        <legend className="mb-1 text-sm font-medium">Período de abertura</legend>
        <Label htmlFor="filtro-de">De</Label>
        <Input id="filtro-de" type="date" value={r.de} onChange={(e) => mudar({ de: e.target.value })} />
        <Label htmlFor="filtro-ate">Até</Label>
        <Input id="filtro-ate" type="date" value={r.ate} onChange={(e) => mudar({ ate: e.target.value })} />
      </fieldset>
      <Escolha id="filtro-ordem" rotulo="Ordem" valor={r.ordem} opcoes={[['', Ordens['']], ['recentes', Ordens.recentes]]} mudar={(ordem) => mudar({ ordem })} />
    </>
  )
}

export function PainelDeFiltros({ aberto, setAberto, filtros, opcoes, aplicar }: { aberto: boolean; setAberto: (v: boolean) => void; filtros: Filtros; opcoes?: Opcoes; aplicar: (f: Filtros) => void }) {
  const estreito = useEstreito()
  const [rascunho, setRascunho] = useState(filtros)
  const [abriu, setAbriu] = useState(aberto)
  if (aberto !== abriu) {
    setAbriu(aberto)
    if (aberto) setRascunho(filtros)
  }
  return (
    <Sheet open={aberto} onOpenChange={setAberto}>
      <SheetContent side={estreito ? 'bottom' : 'right'} className={`gap-0 ${estreito ? 'max-h-[85dvh]' : ''}`} data-painel="filtros">
        <SheetHeader>
          <SheetTitle>Filtros</SheetTitle>
          <SheetDescription className="sr-only">Escolha os filtros e toque em Aplicar.</SheetDescription>
        </SheetHeader>
        <div data-painel="corpo" className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto px-4 pb-4">
          <CamposDoPainel r={rascunho} mudar={(p) => setRascunho((r) => ({ ...r, ...p }))} opcoes={opcoes} />
        </div>
        <SheetFooter data-painel="rodape" className="mt-0 flex-row border-t">
          <Button type="button" variant="outline" className="flex-1" onClick={() => setRascunho({ ...SemFiltros, q: rascunho.q })}>
            Limpar
          </Button>
          <Button type="button" className="flex-1" onClick={() => aplicar(rascunho)}>
            Aplicar
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  )
}

export function Etiquetas({ etiquetas, tirar, limpar }: { etiquetas: Etiqueta[]; tirar: (e: Etiqueta) => void; limpar: () => void }) {
  if (etiquetas.length === 0) return null
  return (
    <div className="flex flex-wrap items-center gap-2" data-etiquetas="">
      {etiquetas.map((e) => (
        <Button key={e.rotulo} type="button" variant="outline" size="sm" aria-label={`Remover filtro ${e.rotulo}`} onClick={() => tirar(e)}>
          <span>{`${e.rotulo}: ${e.valor}`}</span>
          <X aria-hidden="true" />
        </Button>
      ))}
      <Button type="button" variant="ghost" size="sm" onClick={limpar}>
        Limpar filtros
      </Button>
    </div>
  )
}

async function baixar(url: string) {
  const r = await fetch(url, { credentials: 'same-origin' })
  if (!r.ok) {
    const corpo = await r.json().catch(() => undefined)
    throw new ErroApi(r.status, corpo?.detail ?? 'Não foi possível exportar os chamados.', corpo)
  }
  const nome = /filename="?([^";]+)"?/.exec(r.headers.get('Content-Disposition') ?? '')?.[1] ?? 'chamados.csv'
  const endereco = URL.createObjectURL(await r.blob())
  const link = Object.assign(document.createElement('a'), { href: endereco, download: nome })
  document.body.append(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(endereco)
}

export function BotoesDaBusca({ ativos, abrir, exportar }: { ativos: number; abrir: () => void; exportar: string }) {
  const exportacao = useMutation({ mutationFn: () => baixar(exportar), onError: (e) => toast.error(e.message) })
  return (
    <div className="flex gap-2 max-md:[&>*]:flex-1">
      <Button type="button" variant="outline" onClick={abrir}>
        <SlidersHorizontal aria-hidden="true" />
        {ativos > 0 ? `Filtros (${ativos})` : 'Filtros'}
      </Button>
      <BotaoAcao type="button" variant="outline" pendente={exportacao.isPending} onClick={() => exportacao.mutate()}>
        {!exportacao.isPending && <Download aria-hidden="true" />}
        Exportar CSV
      </BotaoAcao>
    </div>
  )
}
