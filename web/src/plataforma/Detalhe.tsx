import { useState } from 'react'
import { useLocation, useParams } from 'react-router'
import { useLeituras } from '../acao'
import { ErroDe } from '../ErroDe'
import { Carregando } from '../Carregando'
import { Badge } from '../ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs'
import { AvisoDeCriacao, type Criada, ImportarCarga } from './Carga'
import { Equipe } from './Equipe'
import { Implantacao, type Prontidao } from './Implantacao'
import { AbaDeParametros } from './Parametros'

type Detalhe = { slug: string; nome: string; situacao: string; prontidao: Prontidao }

const Situacoes: Record<string, string> = { ATIVA: 'Ativa', DESATIVADA: 'Desativada', AGUARDANDO_PRIMEIRO_ACESSO: 'Aguardando primeiro acesso', CONFIGURANDO: 'Configurando' }

function Cabeca({ d }: { d?: Detalhe }) {
  if (!d) return <Carregando className="flex py-4" />
  return (
    <div className="flex flex-wrap items-center gap-2">
      <h2 className="m-0 text-lg font-semibold">{d.nome}</h2>
      <span translate="no" className="text-sm text-muted-foreground">
        /{d.slug}
      </span>
      <Badge variant="secondary">{Situacoes[d.situacao] ?? d.situacao}</Badge>
    </div>
  )
}

function Abas({ slug, d, recarregar }: { slug: string; d?: Detalhe; recarregar: () => Promise<unknown> }) {
  const [aba, setAba] = useState('implantacao')
  return (
    <Tabs value={aba} onValueChange={setAba}>
      <TabsList>
        <TabsTrigger value="implantacao">Implantação</TabsTrigger>
        <TabsTrigger value="parametros">Parâmetros</TabsTrigger>
      </TabsList>
      <TabsContent value="implantacao" className="flex flex-col gap-6 pt-2">
        {d && <Implantacao slug={slug} prontidao={d.prontidao} recarregar={recarregar} abrirParametros={() => setAba('parametros')} />}
        {d && <Equipe slug={slug} nome={d.nome} recarregar={recarregar} />}
      </TabsContent>
      <TabsContent value="parametros" className="pt-2">
        <AbaDeParametros slug={slug} />
      </TabsContent>
    </Tabs>
  )
}

export function DetalheDaInstituicao() {
  const { slug = '' } = useParams()
  const criada = (useLocation().state as { criada?: Criada } | null)?.criada
  const [carga, setCarga] = useState(false)
  const { dados, erro, recarregar } = useLeituras<{ detalhe: Detalhe }>({ detalhe: `/api/plataforma/instituicoes/${slug}` })
  const fechar = () => {
    setCarga(false)
    void recarregar()
  }
  return (
    <div className="flex flex-col gap-4">
      {criada && <AvisoDeCriacao criada={criada} importar={() => setCarga(true)} />}
      <ErroDe erro={erro} oque="a instituição" tentar={recarregar} />
      {!erro && <Cabeca d={dados?.detalhe} />}
      <Abas slug={slug} d={dados?.detalhe} recarregar={recarregar} />
      {carga && criada && <ImportarCarga alvo={criada} fechar={fechar} />}
    </div>
  )
}
