import { CalendarCheck, CircleAlert, Clock } from 'lucide-react'
import { useSearchParams } from 'react-router'
import { plural } from '../formatos'
import type { Vazio } from '../Tabela'
import { Badge } from '../ui/badge'
import { Button } from '../ui/button'
import { Label } from '../ui/label'
import { NativeSelect } from '../ui/native-select'

export type Situacao = 'VIGENTE' | 'VENCE_EM_BREVE' | 'VENCIDO'
export type ComValidade = { validoAte?: string; validade?: Situacao }

export const dataBr = (iso: string) => iso.split('-').reverse().join('/')

function diasAte(iso: string) {
  const [a, m, d] = iso.split('-').map(Number)
  const hoje = new Date()
  return Math.round((Date.UTC(a, m - 1, d) - Date.UTC(hoje.getFullYear(), hoje.getMonth(), hoje.getDate())) / 86_400_000)
}

function textoEmBreve(iso: string) {
  const dias = Math.max(0, diasAte(iso))
  return dias === 0 ? 'Vence hoje' : `Vence em ${plural(dias, 'dia', 'dias')}`
}

export function EtiquetaValidade({ validoAte, validade }: ComValidade) {
  if (!validoAte || !validade) return null
  if (validade === 'VENCIDO')
    return (
      <Badge variant="destructive" data-validade={validade}>
        <CircleAlert aria-hidden="true" />
        {`Vencido em ${dataBr(validoAte)}`}
      </Badge>
    )
  if (validade === 'VENCE_EM_BREVE')
    return (
      <Badge className="bg-warning-subtle text-foreground" data-validade={validade}>
        <Clock aria-hidden="true" />
        {textoEmBreve(validoAte)}
      </Badge>
    )
  return (
    <span data-validade={validade} className="inline-flex items-center gap-1 text-xs text-muted-foreground">
      <CalendarCheck aria-hidden="true" className="size-3" />
      {`Vence em ${dataBr(validoAte)}`}
    </span>
  )
}

const Filtros = { 'em-30-dias': 'VENCE_EM_BREVE', vencidos: 'VENCIDO' } as const
type Filtro = keyof typeof Filtros

export function useFiltroValidade<T extends ComValidade>(itens: T[] | undefined, tipo: string) {
  const [params, setParams] = useSearchParams()
  const pedido = params.get('validade')
  const filtro: Filtro | '' = pedido && pedido in Filtros ? (pedido as Filtro) : ''
  const escolher = (valor: string) =>
    setParams(
      (anterior) => {
        const proximo = new URLSearchParams(anterior)
        if (valor) proximo.set('validade', valor)
        else proximo.delete('validade')
        return proximo
      },
      { replace: true },
    )
  const filtrados = filtro ? itens?.filter((i) => i.validade === Filtros[filtro]) : itens
  const seletor = itens?.length ? <FiltroValidade filtro={filtro} escolher={escolher} /> : null
  const vazio = (padrao: Vazio): Vazio => (filtro ? { Icone: CalendarCheck, frase: `Nenhum ${tipo} neste filtro de validade.` } : padrao)
  return { filtrados, seletor, vazio }
}

function FiltroValidade({ filtro, escolher }: { filtro: string; escolher: (valor: string) => void }) {
  return (
    <div className="flex flex-col gap-2 self-start">
      <Label htmlFor="filtro-validade">Validade</Label>
      <NativeSelect id="filtro-validade" value={filtro} onChange={(e) => escolher(e.target.value)}>
        <option value="">Todos</option>
        <option value="em-30-dias">Vencem em 30 dias</option>
        <option value="vencidos">Vencidos</option>
      </NativeSelect>
    </div>
  )
}

export const precisaRenovar = (i: ComValidade) => i.validade === 'VENCE_EM_BREVE' || i.validade === 'VENCIDO'

export const avisoRenovado = (r: unknown) => `Renovado até ${dataBr((r as { validoAte: string }).validoAte)}`

export function BotaoRenovar({ nome, pendente, renovar }: { nome: string; pendente: boolean; renovar: () => void }) {
  return (
    <Button type="button" size="sm" variant="outline" aria-label={`Renovar ${nome}`} disabled={pendente} onClick={renovar}>
      Renovar
    </Button>
  )
}

export function ValidadeDaLinha({ item, nome, pendente, renovar }: { item: ComValidade; nome: string; pendente: boolean; renovar?: () => void }) {
  if (!item.validoAte) return null
  return (
    <div className="mt-1 flex flex-wrap items-center gap-2">
      <EtiquetaValidade validoAte={item.validoAte} validade={item.validade} />
      {renovar && precisaRenovar(item) && <BotaoRenovar nome={nome} pendente={pendente} renovar={renovar} />}
    </div>
  )
}
