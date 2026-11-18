import { useMutation } from '@tanstack/react-query'
import { useState } from 'react'
import { BotaoAcao, useLeituras } from '../acao'
import { api } from '../api'
import { Campo } from '../Campo'
import { ErroDe, errosDeCampo } from '../ErroDe'
import { Alert } from '../ui/alert'
import { Button } from '../ui/button'

type Parametro = { valor: number | null; padrao: number; efetivo: number }
type Chave = 'abandonoMinutos' | 'confirmacaoDias' | 'lembreteAguardandoDias' | 'resolverAguardandoDias' | 'validadeConhecimentoMeses' | 'primeiraRespostaHoras' | 'resolucaoHoras'
type Parametros = Record<Chave, Parametro>
type Ficha = Record<Chave, string>

const Campos: [Chave, string, string][] = [
  ['abandonoMinutos', 'Abandono da conversa', 'min'],
  ['confirmacaoDias', 'Prazo para o aluno confirmar a resolução', 'dias'],
  ['lembreteAguardandoDias', 'Lembrete ao aluno que não respondeu', 'dias'],
  ['resolverAguardandoDias', 'Resolução automática sem resposta do aluno', 'dias'],
  ['validadeConhecimentoMeses', 'Validade de artigos e documentos', 'meses'],
  ['primeiraRespostaHoras', 'Meta de primeira resposta', 'h'],
  ['resolucaoHoras', 'Meta de resolução', 'h'],
]

const fichaDe = (p: Parametros) => Object.fromEntries(Campos.map(([c]) => [c, p[c].valor?.toString() ?? ''])) as Ficha
const corpoDe = (f: Ficha) => Object.fromEntries(Campos.map(([c]) => [c, f[c] === '' ? null : Number(f[c])]))

function Linha({ chave, rotulo, unidade, padrao, valor, erro, mudar }: { chave: Chave; rotulo: string; unidade: string; padrao: number; valor: string; erro?: string; mudar: (v: string) => void }) {
  return (
    <div className="flex flex-wrap items-end gap-2">
      <div className="min-w-0 flex-1 basis-60">
        <Campo id={chave} rotulo={`${rotulo} (${unidade})`} type="number" inputMode="numeric" autoComplete="off" dica={`Padrão: ${padrao} ${unidade}. Vazio usa o padrão.`} value={valor} erro={erro} onChange={(e) => mudar(e.target.value)} />
      </div>
      <Button type="button" variant="outline" className="h-11" disabled={valor === ''} onClick={() => mudar('')} aria-label={`Usar padrão em ${rotulo}`}>
        Usar padrão
      </Button>
    </div>
  )
}

function Formulario({ slug, atuais, recarregar }: { slug: string; atuais: Parametros; recarregar: () => Promise<unknown> }) {
  const [ficha, setFicha] = useState(() => fichaDe(atuais))
  const salvar = useMutation({
    mutationFn: (f: Ficha) => api<Parametros>(`/api/plataforma/instituicoes/${slug}/parametros`, corpoDe(f), 'PUT'),
    onSuccess: async (novos) => {
      setFicha(fichaDe(novos))
      await recarregar()
    },
  })
  const erros = errosDeCampo(salvar.error)
  return (
    <form
      className="flex max-w-2xl flex-col gap-4"
      onSubmit={(e) => {
        e.preventDefault()
        salvar.mutate(ficha)
      }}
    >
      {Campos.map(([chave, rotulo, unidade]) => (
        <Linha key={chave} chave={chave} rotulo={rotulo} unidade={unidade} padrao={atuais[chave].padrao} valor={ficha[chave]} erro={erros[chave]} mudar={(v) => setFicha((f) => ({ ...f, [chave]: v }))} />
      ))}
      <ErroDe erro={salvar.error} campos={Campos.map(([c]) => c)} />
      {salvar.isSuccess && <Alert role="status">Parâmetros salvos.</Alert>}
      <BotaoAcao type="submit" pendente={salvar.isPending} className="h-11 self-start">
        Salvar
      </BotaoAcao>
    </form>
  )
}

export function AbaDeParametros({ slug }: { slug: string }) {
  const { dados, erro, recarregar } = useLeituras<{ parametros: Parametros }>({ parametros: `/api/plataforma/instituicoes/${slug}/parametros` })
  if (erro) return <ErroDe erro={erro} oque="os parâmetros" tentar={recarregar} />
  return dados ? <Formulario slug={slug} atuais={dados.parametros} recarregar={recarregar} /> : null
}
