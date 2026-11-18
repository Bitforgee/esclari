import { useMutation, useQuery } from '@tanstack/react-query'
import { CircleCheck, CircleDashed } from 'lucide-react'
import { useState } from 'react'
import { BotaoAcao } from '../acao'
import { api } from '../api'
import { CampoArquivo } from '../CampoArquivo'
import { Button } from '../ui/button'
import { NativeSelect } from '../ui/native-select'
import { Label } from '../ui/label'
import { type Contagens, ResultadoDaCarga } from './Carga'

export type Prontidao = { itens: { codigo: string; ok: boolean }[]; pronta: boolean }
type Modelo = { id: string; nome: string; descricao: string; setores: number; categorias: number; regras: number }

const Itens: Record<string, { rotulo: string; passo?: 'carga' | 'equipe'; dica?: string }> = {
  ADMINISTRADOR_COM_SENHA: { rotulo: 'Um administrador com senha criada', passo: 'equipe' },
  ATENDENTE_NO_SETOR: { rotulo: 'Um atendente ligado a um setor', passo: 'equipe' },
  CATEGORIA_COM_REGRA: { rotulo: 'Uma categoria com regra de roteamento', passo: 'carga' },
  ARTIGO_ATIVO: { rotulo: 'Um artigo ativo e dentro da validade', dica: 'O administrador da instituição cadastra os artigos.' },
}

const irPara = (id: string) => document.getElementById(id)?.focus()

const Passos = { carga: 'Ir para a carga inicial', equipe: 'Ir para a equipe inicial' }

function Pendencia({ passo, dica }: { passo?: 'carga' | 'equipe'; dica?: string }) {
  if (!passo) return <span className="ms-auto text-sm text-muted-foreground">{dica}</span>
  return (
    <Button variant="outline" size="sm" className="ms-auto pointer-coarse:h-11" onClick={() => irPara(passo)}>
      {Passos[passo]}
    </Button>
  )
}

function ItemDaLista({ codigo, ok }: { codigo: string; ok: boolean }) {
  const item = Itens[codigo] ?? { rotulo: codigo }
  const [Icone, cor, estado] = ok ? [CircleCheck, 'text-primary', 'Feito'] : [CircleDashed, 'text-muted-foreground', 'Pendente']
  return (
    <li className="flex min-h-11 flex-wrap items-center gap-2">
      <Icone aria-hidden="true" className={`size-5 ${cor}`} />
      <span>{item.rotulo}</span>
      <span className="text-sm text-muted-foreground">{estado}</span>
      {!ok && <Pendencia passo={item.passo} dica={item.dica} />}
    </li>
  )
}

function Lista({ prontidao }: { prontidao: Prontidao }) {
  return (
    <section aria-labelledby="prontidao-titulo" className="flex flex-col gap-2">
      <h3 id="prontidao-titulo" className="m-0 text-base font-semibold">
        {prontidao.pronta ? 'Pronta para atender' : 'O que falta para atender'}
      </h3>
      <ul className="m-0 flex list-none flex-col gap-1 p-0">
        {prontidao.itens.map((i) => (
          <ItemDaLista key={i.codigo} {...i} />
        ))}
      </ul>
    </section>
  )
}

type Envio = { simular: boolean; modelo?: Modelo; arquivo?: File }

function enviar(slug: string, { simular, modelo, arquivo }: Envio): Promise<Contagens> {
  if (modelo && simular) return Promise.resolve({ setores: modelo.setores, cursos: 0, categorias: modelo.categorias, regras: modelo.regras, artigos: 0 })
  if (modelo) return api<Contagens>(`/api/plataforma/instituicoes/${slug}/carga/modelo/${modelo.id}`, {})
  const f = new FormData()
  f.set('arquivo', arquivo!)
  return api<Contagens>(`/api/plataforma/instituicoes/${slug}/carga${simular ? '?simular=true' : ''}`, f)
}

function Origem({ modelos, origem, mudar }: { modelos: Modelo[]; origem: string; mudar: (o: string) => void }) {
  const modelo = modelos.find((m) => m.id === origem)
  return (
    <div className="flex flex-col gap-2">
      <Label htmlFor="origem">Origem da carga</Label>
      <NativeSelect id="origem" className="w-full" value={origem} onChange={(e) => mudar(e.target.value)}>
        <option value="">Escolha um modelo ou um arquivo</option>
        {modelos.map((m) => (
          <option key={m.id} value={m.id}>
            Modelo {m.nome}
          </option>
        ))}
        <option value="arquivo">Arquivo JSON</option>
      </NativeSelect>
      {modelo && <p className="m-0 text-sm text-muted-foreground">{modelo.descricao}</p>}
    </div>
  )
}

type EnvioDaCarga = { isPending: boolean; variables?: Envio; data?: Contagens; error: Error | null }

function Botoes({ envio, pronto, agir }: { envio: EnvioDaCarga; pronto: boolean; agir: (simular: boolean) => void }) {
  const enviando = (simular: boolean) => envio.isPending && envio.variables?.simular === simular
  return (
    <div className="flex flex-wrap gap-2">
      <BotaoAcao variant="outline" pendente={enviando(true)} disabled={!pronto || envio.isPending} onClick={() => agir(true)}>
        Simular
      </BotaoAcao>
      <BotaoAcao pendente={enviando(false)} disabled={!pronto || envio.isPending} onClick={() => agir(false)}>
        Importar
      </BotaoAcao>
    </div>
  )
}

function Resultado({ envio }: { envio: EnvioDaCarga }) {
  if (!envio.data && !envio.error) return null
  return <ResultadoDaCarga r={envio.data} erro={envio.error} prefixo={envio.variables?.simular ? 'A importação criaria' : 'Importados'} />
}

function CargaInicial({ slug, recarregar }: { slug: string; recarregar: () => Promise<unknown> }) {
  const { data: modelos = [] } = useQuery({ queryKey: ['modelos-de-carga'], queryFn: () => api<Modelo[]>('/api/plataforma/modelos-de-carga') })
  const [origem, setOrigem] = useState('')
  const [arquivo, setArquivo] = useState<File>()
  const envio = useMutation({ mutationFn: (e: Envio) => enviar(slug, e), onSuccess: async (_, e) => void (e.simular || (await recarregar())) })
  const modelo = modelos.find((m) => m.id === origem)
  const pronto = !!modelo || (origem === 'arquivo' && !!arquivo)
  const agir = (simular: boolean) => envio.mutate({ simular, modelo, arquivo })
  const mudar = (o: string) => {
    setOrigem(o)
    envio.reset()
  }
  return (
    <section id="carga" tabIndex={-1} aria-labelledby="carga-titulo" className="flex flex-col gap-3 outline-none">
      <h3 id="carga-titulo" className="m-0 text-base font-semibold">
        Carga inicial
      </h3>
      <Origem modelos={modelos} origem={origem} mudar={mudar} />
      {origem === 'arquivo' && <CampoArquivo id="arquivo-carga" rotulo="Arquivo de carga (JSON)" accept=".json,application/json" multiple={false} arquivos={arquivo ? [arquivo] : []} setArquivos={([a]) => setArquivo(a)} />}
      <Botoes envio={envio} pronto={pronto} agir={agir} />
      <Resultado envio={envio} />
    </section>
  )
}

export function Implantacao({ slug, prontidao, recarregar }: { slug: string; prontidao: Prontidao; recarregar: () => Promise<unknown> }) {
  return (
    <>
      <Lista prontidao={prontidao} />
      <CargaInicial slug={slug} recarregar={recarregar} />
    </>
  )
}
