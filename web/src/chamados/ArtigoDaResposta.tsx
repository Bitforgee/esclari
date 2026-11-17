import { useMutation } from '@tanstack/react-query'
import { BookCheck } from 'lucide-react'
import { useState, type Dispatch, type SetStateAction } from 'react'
import { toast } from 'sonner'
import { api } from '../api'
import { Campo, ErroDoCampo, useFocoNoPrimeiroErro } from '../Campo'
import type { Chamado } from '../chamado'
import { Editor, useSujo } from '../Editor'
import { ErroDe, errosDeCampo } from '../ErroDe'
import { Button } from '../ui/button'
import { Label } from '../ui/label'
import { Textarea } from '../ui/textarea'

type Rascunho = { titulo: string; perguntas: string; resposta: string; links: string }

const Titulo = 'Criar artigo a partir desta resposta'
const Aviso = 'Rascunho criado. O administrador precisa ativar antes de o assistente usar.'
const CamposComErro = ['titulo', 'perguntas', 'resposta', 'links']
const linhas = (texto: string) => texto.split('\n').map((l) => l.trim()).filter(Boolean)

export const ultimaRespostaDaEquipe = (c: Chamado) =>
  c.mensagens.filter((m) => m.tipo === 'PUBLICA' && !m.doAluno && m.autor !== 'Sistema').at(-1)?.texto ?? ''

const corpo = (r: Rascunho) => ({ titulo: r.titulo, perguntas: linhas(r.perguntas), resposta: r.resposta, links: linhas(r.links) })

function AreaDeTexto({ id, rotulo, linhas: altura, valor, erro, mudar }: { id: string; rotulo: string; linhas: number; valor: string; erro?: string; mudar: (v: string) => void }) {
  return (
    <div className="flex flex-col gap-2">
      <Label htmlFor={id}>{rotulo}</Label>
      <Textarea id={id} rows={altura} value={valor} aria-invalid={!!erro} aria-describedby={erro ? `${id}-erro` : undefined} onChange={(e) => mudar(e.target.value)} />
      <ErroDoCampo id={id} erro={erro} />
    </div>
  )
}

function CamposDoRascunho({ r, setR, erros }: { r: Rascunho; setR: Dispatch<SetStateAction<Rascunho | undefined>>; erros: Record<string, string> }) {
  const muda = (chave: keyof Rascunho) => (valor: string) => setR((atual) => atual && { ...atual, [chave]: valor })
  return (
    <>
      <p className="m-0 rounded-md bg-warning-subtle px-3 py-2 text-sm">Remova nomes, CPF e matrícula antes de salvar</p>
      <Campo id="artigo-titulo" rotulo="Título" autoComplete="off" maxLength={200} value={r.titulo} erro={erros.titulo} onChange={(e) => muda('titulo')(e.target.value)} />
      <AreaDeTexto id="artigo-perguntas" rotulo="Perguntas relacionadas (uma por linha)" linhas={3} valor={r.perguntas} erro={erros.perguntas} mudar={muda('perguntas')} />
      <AreaDeTexto id="artigo-resposta" rotulo="Resposta" linhas={6} valor={r.resposta} erro={erros.resposta} mudar={muda('resposta')} />
      <AreaDeTexto id="artigo-links" rotulo="Links (um por linha, com https://)" linhas={2} valor={r.links} erro={erros.links} mudar={muda('links')} />
    </>
  )
}

export function ArtigoDaResposta({ c, setChamado }: { c: Chamado; setChamado: (c: Chamado) => void }) {
  const [rascunho, setRascunho] = useState<Rascunho>()
  const criar = useMutation({
    mutationFn: (r: Rascunho) => api(`/api/chamados/${c.numero}/artigo`, corpo(r)),
    onSuccess: () => {
      setRascunho(undefined)
      toast.success(Aviso)
      setChamado({ ...c, artigoDaResposta: 'CRIADO' })
    },
  })
  useFocoNoPrimeiroErro(criar.error)
  const sujo = useSujo(rascunho)
  if (c.artigoDaResposta === 'CRIADO')
    return (
      <p className="m-0 inline-flex items-center gap-2 text-sm text-muted-foreground">
        <BookCheck aria-hidden="true" className="size-4" />
        {`Artigo criado: ${c.numero}`}
      </p>
    )
  if (c.artigoDaResposta !== 'PODE_CRIAR') return null
  const fechar = () => {
    setRascunho(undefined)
    criar.reset()
  }
  return (
    <>
      <Button variant="outline" className="h-11 whitespace-normal" onClick={() => setRascunho({ titulo: '', perguntas: '', resposta: ultimaRespostaDaEquipe(c), links: '' })}>
        {Titulo}
      </Button>
      <Editor sujo={sujo} titulo={Titulo} aberto={!!rascunho} fechar={fechar} salvar={() => criar.mutate(rascunho!)} pendente={criar.isPending}
        alerta={<ErroDe erro={criar.error} campos={CamposComErro} />} botao="Salvar rascunho">
        {rascunho && <CamposDoRascunho r={rascunho} setR={setRascunho} erros={errosDeCampo(criar.error)} />}
      </Editor>
    </>
  )
}
