import { useMutation } from '@tanstack/react-query'
import { Building2, Plus } from 'lucide-react'
import { useId, useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { useGravacao, useLeituras } from '../acao'
import { api, ErroApi } from '../api'
import { Cabecalho } from '../Cabecalho'
import { Campo, ErroDoCampo, useFocoNoPrimeiroErro } from '../Campo'
import { Editor, useSujo } from '../Editor'
import { ErroDe, errosDeCampo } from '../ErroDe'
import { dataCurta, plural, porcentagem } from '../formatos'
import { CelulaLargo, Detalhe, MenuAcoes, Principal, Tabela } from '../Tabela'
import { Badge } from '../ui/badge'
import { Button } from '../ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../ui/dialog'
import { DropdownMenuItem, DropdownMenuSeparator } from '../ui/dropdown-menu'
import { Input } from '../ui/input'
import { Label } from '../ui/label'
import { NativeSelect } from '../ui/native-select'
import { TableCell, TableRow } from '../ui/table'
import { type Alvo, ImportarCarga } from './Carga'

type Saude = { ultimaAtividadeEm: string | null; semBaseTaxa30Dias: number | null }
type Item = { slug: string; nome: string; fuso: string; ativa: boolean; criadaEm: string; administradores: number; situacao: string; saude: Saude; atencao: string[] }

const Etiquetas: Record<string, string> = {
  PARADA: 'Parada',
  MUITOS_SEM_BASE: "Muitos 'não encontrei'",
  EMAILS_FALHANDO: 'E-mails falhando',
  CONTEUDO_VENCIDO: 'Conteúdo vencido',
  FILA_ANTIGA: 'Fila antiga',
}
const Situacoes: Record<string, string> = { ATIVA: 'Ativa', DESATIVADA: 'Desativada', AGUARDANDO_PRIMEIRO_ACESSO: 'Aguardando primeiro acesso' }

const Fusos = [
  'America/Sao_Paulo', 'America/Noronha', 'America/Belem', 'America/Fortaleza', 'America/Recife', 'America/Araguaina', 'America/Maceio', 'America/Bahia',
  'America/Campo_Grande', 'America/Cuiaba', 'America/Santarem', 'America/Porto_Velho', 'America/Boa_Vista', 'America/Manaus', 'America/Eirunepe', 'America/Rio_Branco',
]

export const enderecoDe = (nome: string) =>
  nome
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')

type Ficha = { nome: string; slug: string; slugEditado: boolean; fuso: string; adminNome: string; cpf: string; email: string }
const Nova: Ficha = { nome: '', slug: '', slugEditado: false, fuso: Fusos[0], adminNome: '', cpf: '', email: '' }
const corpo = (f: Ficha) => ({ slug: f.slug, nome: f.nome, fuso: f.fuso, admin: { cpf: f.cpf, nome: f.adminNome, email: f.email } })

function errosDaCriacao(erro: unknown): Record<string, string> {
  return erro instanceof ErroApi && erro.status === 409 ? { slug: erro.message } : errosDeCampo(erro)
}

function Formulario({ ficha, mudar, erros }: { ficha: Ficha; mudar: (parcial: Partial<Ficha>) => void; erros: Record<string, string> }) {
  const grupo = useId()
  const texto = (chave: 'adminNome' | 'cpf' | 'email') => (e: { target: { value: string } }) => mudar({ [chave]: e.target.value })
  return (
    <>
      <Campo id="nome" rotulo="Nome" autoComplete="off" value={ficha.nome} erro={erros.nome} onChange={(e) => mudar({ nome: e.target.value, ...(ficha.slugEditado ? {} : { slug: enderecoDe(e.target.value) }) })} />
      <div className="flex flex-col gap-2">
        <Label htmlFor="endereco">Endereço</Label>
        <div className="flex items-center gap-1">
          <span aria-hidden="true">/</span>
          <Input id="endereco" name="endereco" autoComplete="off" spellCheck={false} value={ficha.slug} aria-invalid={!!erros.slug} aria-describedby={erros.slug ? 'endereco-erro' : undefined} onChange={(e) => mudar({ slug: e.target.value, slugEditado: true })} />
        </div>
        <ErroDoCampo id="endereco" erro={erros.slug} />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="fuso">Fuso horário</Label>
        <NativeSelect id="fuso" className="w-full" value={ficha.fuso} onChange={(e) => mudar({ fuso: e.target.value })}>
          {Fusos.map((f) => (
            <option key={f} value={f}>
              {f}
            </option>
          ))}
        </NativeSelect>
        <ErroDoCampo id="fuso" erro={erros.fuso} />
      </div>
      <fieldset aria-labelledby={grupo} className="flex flex-col gap-3">
        <legend id={grupo} className="mb-2 text-sm font-medium">
          Primeiro administrador
        </legend>
        <Campo id="admin-nome" rotulo="Nome" autoComplete="off" value={ficha.adminNome} erro={erros['admin.nome']} onChange={texto('adminNome')} />
        <Campo id="admin-cpf" rotulo="CPF" inputMode="numeric" autoComplete="off" spellCheck={false} placeholder="Ex.: 000.000.000-00…" value={ficha.cpf} erro={erros['admin.cpf']} onChange={texto('cpf')} />
        <Campo id="admin-email" rotulo="E-mail" type="email" autoComplete="off" spellCheck={false} value={ficha.email} erro={erros['admin.email']} onChange={texto('email')} />
      </fieldset>
    </>
  )
}

const Colunas = [{ titulo: 'Nome' }, { titulo: 'Situação' }, { titulo: 'Última atividade', largo: true }, { titulo: 'Não encontrei', largo: true }, { titulo: 'Administradores', largo: true }, { titulo: 'Criada em', largo: true }, { titulo: '' }]

const ultima = (i: Item) => (i.saude.ultimaAtividadeEm ? dataCurta(i.saude.ultimaAtividadeEm, i.fuso) : 'Sem atividade')
const taxa = (i: Item) => (i.saude.semBaseTaxa30Dias === null ? '-' : porcentagem(i.saude.semBaseTaxa30Dias))

function Selo({ i }: { i: Item }) {
  return (
    <div className="flex flex-wrap gap-1">
      <Badge variant={i.ativa ? undefined : 'secondary'} className={i.ativa ? 'bg-accent text-foreground' : undefined}>
        {Situacoes[i.situacao] ?? i.situacao}
      </Badge>
      {i.atencao.map((a) => (
        <Badge key={a} variant="outline">
          {Etiquetas[a] ?? a}
        </Badge>
      ))}
    </div>
  )
}

type Agir = (aviso: string | ((r: unknown) => string), url: string, corpo?: object) => void

function Linha({ i, importar, desativar, agir, pendente }: { i: Item; importar: () => void; desativar: () => void; agir: Agir; pendente: boolean }) {
  const criada = dataCurta(i.criadaEm, i.fuso)
  return (
    <TableRow>
      <TableCell className={`font-medium ${Principal}`}>
        <Link to={`/plataforma/instituicoes/${i.slug}`} className="underline-offset-4 hover:underline">
          {i.nome}
        </Link>
        <div translate="no" className="text-xs font-normal text-muted-foreground">/{i.slug}</div>
        <Detalhe>
          {plural(i.administradores, 'administrador', 'administradores')} · criada em {criada} · {ultima(i)} · {taxa(i)}
        </Detalhe>
      </TableCell>
      <TableCell data-celula="selo">
        <Selo i={i} />
      </TableCell>
      <CelulaLargo>{ultima(i)}</CelulaLargo>
      <CelulaLargo>{taxa(i)}</CelulaLargo>
      <CelulaLargo>{i.administradores}</CelulaLargo>
      <CelulaLargo>{criada}</CelulaLargo>
      <TableCell data-celula="acoes" className="text-end">
        <MenuAcoes nome={i.nome} pendente={pendente}>
          <DropdownMenuItem onSelect={importar}>Importar carga</DropdownMenuItem>
          <DropdownMenuItem onSelect={() => agir((r) => `Enviamos o link para ${(r as { enviados: number }).enviados} administrador(es).`, `${i.slug}/reenviar-acesso`)}>Reenviar acesso do admin</DropdownMenuItem>
          {i.ativa ? (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuItem variant="destructive" onSelect={desativar}>
                Desativar
              </DropdownMenuItem>
            </>
          ) : (
            <DropdownMenuItem onSelect={() => agir('Instituição reativada.', `${i.slug}/reativar`)}>Reativar</DropdownMenuItem>
          )}
        </MenuAcoes>
      </TableCell>
    </TableRow>
  )
}

function Desativar({ alvo, fechar, enviar }: { alvo?: Item; fechar: () => void; enviar: (motivo: string) => void }) {
  const [motivo, setMotivo] = useState('')
  return (
    <Dialog open={!!alvo} onOpenChange={(a) => !a && fechar()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Desativar instituição</DialogTitle>
          <DialogDescription>Desativar {alvo?.nome}? Ninguém dessa instituição consegue entrar até você reativar.</DialogDescription>
        </DialogHeader>
        <Campo id="motivo" rotulo="Motivo" autoComplete="off" maxLength={200} value={motivo} onChange={(e) => setMotivo(e.target.value)} />
        <DialogFooter>
          <Button variant="outline" onClick={fechar}>
            Voltar
          </Button>
          <Button variant="destructive" disabled={motivo.trim().length < 3} onClick={() => enviar(motivo.trim())}>
            Desativar
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

function useCriacao(recarregar: () => Promise<unknown>) {
  const [nova, setNova] = useState<Ficha>()
  const navegar = useNavigate()
  const mutacao = useMutation({
    mutationFn: (f: Ficha) => api('/api/plataforma/instituicoes', corpo(f)),
    onSuccess: async (_, f) => {
      setNova(undefined)
      await recarregar()
      navegar(`/plataforma/instituicoes/${f.slug}`, { state: { criada: { slug: f.slug, nome: f.nome, email: f.email } } })
    },
  })
  const campos = errosDaCriacao(mutacao.error)
  const geral = Object.keys(campos).length === 0 ? mutacao.error : null
  const abrir = () => {
    mutacao.reset()
    setNova({ ...Nova })
  }
  return { nova, setNova, mutacao, campos, geral, abrir }
}

const SemInstituicoes = { Icone: Building2, titulo: 'Nenhuma instituição', frase: 'Ainda não há instituição cadastrada. Crie a primeira.' }

export function Instituicoes() {
  const { dados, erro, recarregar } = useLeituras<{ lista: Item[] }>({ lista: '/api/plataforma/instituicoes' })
  const criacao = useCriacao(recarregar)
  const sujo = useSujo(criacao.nova)
  useFocoNoPrimeiroErro(criacao.mutacao.error)
  const [carga, setCarga] = useState<Alvo>()
  const [desativar, setDesativar] = useState<Item>()
  const acao = useGravacao(recarregar)

  const agir: Agir = (aviso, url, corpo = {}) => acao.mutate({ chamar: () => api(`/api/plataforma/instituicoes/${url}`, corpo), aviso })
  const confirmarDesativar = (motivo: string) => {
    const i = desativar!
    setDesativar(undefined)
    agir(`Instituição ${i.nome} desativada.`, `${i.slug}/desativar`, { motivo })
  }

  return (
    <div className="flex flex-col gap-2">
      <Cabecalho botao="Nova instituição" Icone={Plus} aoClicar={criacao.abrir} />
      <ErroDe erro={erro} oque="as instituições" tentar={recarregar} />
      <ErroDe erro={acao.error} />
      <Tabela colunas={Colunas} itens={dados?.lista} erro={erro} vazio={SemInstituicoes}>
        {dados?.lista.map((i) => (
          <Linha key={i.slug} i={i} importar={() => setCarga(i)} desativar={() => setDesativar(i)} agir={agir} pendente={acao.isPending} />
        ))}
      </Tabela>

      <Editor
        sujo={sujo}
        titulo="Nova instituição"
        aberto={!!criacao.nova}
        fechar={() => criacao.setNova(undefined)}
        salvar={() => criacao.mutacao.mutate(criacao.nova!)}
        botao="Criar"
        pendente={criacao.mutacao.isPending}
        alerta={<ErroDe erro={criacao.geral} />}
      >
        {criacao.nova && <Formulario ficha={criacao.nova} mudar={(p) => criacao.setNova((f) => f && { ...f, ...p })} erros={criacao.campos} />}
      </Editor>

      {carga && <ImportarCarga key={carga.slug} alvo={carga} fechar={() => setCarga(undefined)} />}

      <Desativar key={desativar?.slug ?? ''} alvo={desativar} fechar={() => setDesativar(undefined)} enviar={confirmarDesativar} />
    </div>
  )
}
