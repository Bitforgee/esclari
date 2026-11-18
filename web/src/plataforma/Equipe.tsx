import { useMutation } from '@tanstack/react-query'
import { UserPlus, Users } from 'lucide-react'
import { useState } from 'react'
import { useGravacao, useLeituras } from '../acao'
import { api } from '../api'
import { Campo } from '../Campo'
import { Confirmar } from '../Confirmar'
import { Editor, useSujo } from '../Editor'
import { ErroDe, errosDeCampo } from '../ErroDe'
import { Marca } from '../Marca'
import { CelulaLargo, Detalhe, MenuAcoes, Principal, Tabela } from '../Tabela'
import { Button } from '../ui/button'
import { DropdownMenuItem } from '../ui/dropdown-menu'
import { TableCell, TableRow } from '../ui/table'

type Membro = { id: number; nome: string; emailMascarado: string; cpfMascarado: string; papeis: string[]; ativo: boolean; primeiroAcessoPendente: boolean }
type Setor = { id: number; nome: string }
type Ficha = { nome: string; cpf: string; email: string; papeis: string[]; setores: number[] }

const Vazia: Ficha = { nome: '', cpf: '', email: '', papeis: [], setores: [] }
const Papeis: [string, string][] = [
  ['ADMINISTRADOR', 'Administrador'],
  ['ATENDENTE', 'Atendente'],
]
const nomesDosPapeis = (papeis: string[]) => papeis.map((p) => Papeis.find(([v]) => v === p)?.[1] ?? p).join(', ')
const alternar = <T,>(lista: T[], valor: T, marcado: boolean) => (marcado ? [...lista, valor] : lista.filter((v) => v !== valor))
const situacao = (m: Membro) => (!m.ativo ? 'Desativado' : m.primeiroAcessoPendente ? 'Convite enviado' : 'Ativo')

const Colunas = [{ titulo: 'Nome' }, { titulo: 'Papéis' }, { titulo: 'E-mail', largo: true }, { titulo: 'Situação', largo: true }, { titulo: '' }]
const SemEquipe = { Icone: Users, titulo: 'Equipe inicial', frase: 'Nenhum administrador ou atendente além do primeiro. Crie a equipe para a instituição atender.' }

function Formulario({ ficha, mudar, setores, erros }: { ficha: Ficha; mudar: (f: Partial<Ficha>) => void; setores: Setor[]; erros: Record<string, string> }) {
  return (
    <>
      <Campo id="membro-nome" rotulo="Nome" autoComplete="off" value={ficha.nome} erro={erros.nome} onChange={(e) => mudar({ nome: e.target.value })} />
      <Campo id="membro-cpf" rotulo="CPF" inputMode="numeric" autoComplete="off" spellCheck={false} placeholder="Ex.: 000.000.000-00…" value={ficha.cpf} erro={erros.cpf} onChange={(e) => mudar({ cpf: e.target.value })} />
      <Campo id="membro-email" rotulo="E-mail" type="email" autoComplete="off" spellCheck={false} value={ficha.email} erro={erros.email} onChange={(e) => mudar({ email: e.target.value })} />
      <fieldset className="flex flex-col gap-1">
        <legend className="mb-1 text-sm font-medium">Papéis</legend>
        {Papeis.map(([valor, rotulo]) => (
          <Marca key={valor} id={`papel-${valor}`} rotulo={rotulo} marcado={ficha.papeis.includes(valor)} mudar={(v) => mudar({ papeis: alternar(ficha.papeis, valor, v) })} />
        ))}
        {erros.papeis && <p className="m-0 text-sm text-destructive">{erros.papeis}</p>}
      </fieldset>
      {ficha.papeis.includes('ATENDENTE') && (
        <fieldset className="flex flex-col gap-1">
          <legend className="mb-1 text-sm font-medium">Setores do atendente</legend>
          {setores.map((s) => (
            <Marca key={s.id} id={`setor-${s.id}`} rotulo={s.nome} marcado={ficha.setores.includes(s.id)} mudar={(v) => mudar({ setores: alternar(ficha.setores, s.id, v) })} />
          ))}
          {erros.setores && <p className="m-0 text-sm text-destructive">{erros.setores}</p>}
        </fieldset>
      )}
    </>
  )
}

function Linha({ m, agir, desativar, pendente }: { m: Membro; agir: (aviso: string, acao: string) => void; desativar: () => void; pendente: boolean }) {
  return (
    <TableRow>
      <TableCell className={`font-medium ${Principal}`}>
        {m.nome}
        <Detalhe>
          {m.emailMascarado} · {situacao(m)}
        </Detalhe>
      </TableCell>
      <TableCell className={Principal}>{nomesDosPapeis(m.papeis)}</TableCell>
      <CelulaLargo>{m.emailMascarado}</CelulaLargo>
      <CelulaLargo>{situacao(m)}</CelulaLargo>
      <TableCell data-celula="acoes" className="text-end">
        <MenuAcoes nome={m.nome} pendente={pendente}>
          {m.ativo && m.primeiroAcessoPendente && <DropdownMenuItem onSelect={() => agir('Convite reenviado.', `${m.id}/convite`)}>Reenviar convite</DropdownMenuItem>}
          {m.ativo ? (
            <DropdownMenuItem variant="destructive" onSelect={desativar}>
              Desativar
            </DropdownMenuItem>
          ) : (
            <DropdownMenuItem onSelect={() => agir('Usuário reativado.', `${m.id}/reativar`)}>Reativar</DropdownMenuItem>
          )}
        </MenuAcoes>
      </TableCell>
    </TableRow>
  )
}

function NovoUsuario({ novo, setores }: { novo: ReturnType<typeof useNovo>; setores: Setor[] }) {
  const sujo = useSujo(novo.ficha)
  return (
    <Editor sujo={sujo} titulo="Novo usuário" aberto={!!novo.ficha} fechar={() => novo.setFicha(undefined)} salvar={() => novo.mutacao.mutate(novo.ficha!)} botao="Convidar" pendente={novo.mutacao.isPending} alerta={<ErroDe erro={novo.mutacao.error} campos={['nome', 'cpf', 'email', 'papeis', 'setores']} />}>
      {novo.ficha && <Formulario ficha={novo.ficha} mudar={(p) => novo.setFicha((f) => f && { ...f, ...p })} setores={setores} erros={errosDeCampo(novo.mutacao.error)} />}
    </Editor>
  )
}

const alemDoPrimeiro = (equipe?: Membro[]) => equipe && (equipe.length > 1 ? equipe : [])

function useNovo(base: string, recarregar: () => Promise<unknown>) {
  const [ficha, setFicha] = useState<Ficha>()
  const mutacao = useMutation({
    mutationFn: (f: Ficha) => api(base, f),
    onSuccess: async () => {
      setFicha(undefined)
      await recarregar()
    },
  })
  const abrir = () => {
    mutacao.reset()
    setFicha({ ...Vazia })
  }
  return { ficha, setFicha, mutacao, abrir }
}

export function Equipe({ slug, nome, recarregar }: { slug: string; nome: string; recarregar: () => Promise<unknown> }) {
  const base = `/api/plataforma/instituicoes/${slug}/equipe`
  const { dados, erro, recarregar: reler } = useLeituras<{ equipe: Membro[]; setores: Setor[] }>({ equipe: base, setores: `/api/plataforma/instituicoes/${slug}/setores` })
  const tudo = () => Promise.all([reler(), recarregar()])
  const novo = useNovo(base, tudo)
  const acao = useGravacao(tudo)
  const [alvo, setAlvo] = useState<Membro>()
  const agir = (aviso: string, url: string) => acao.mutate({ chamar: () => api(`${base}/${url}`, {}), aviso })
  const membros = alemDoPrimeiro(dados?.equipe)
  const confirmar = () => {
    const m = alvo!
    setAlvo(undefined)
    agir(`${m.nome} foi desativado.`, `${m.id}/desativar`)
  }

  return (
    <section id="equipe" tabIndex={-1} aria-labelledby="equipe-titulo" className="flex flex-col gap-3 outline-none">
      <div className="flex flex-wrap items-center gap-2">
        <h3 id="equipe-titulo" className="m-0 text-base font-semibold">
          Equipe inicial de {nome}
        </h3>
        <Button className="ms-auto" onClick={novo.abrir}>
          <UserPlus aria-hidden="true" />
          Novo usuário
        </Button>
      </div>
      <ErroDe erro={erro} oque="a equipe" tentar={reler} />
      <ErroDe erro={acao.error} />
      <Tabela colunas={Colunas} itens={membros} erro={erro} vazio={SemEquipe}>
        {membros?.map((m) => <Linha key={m.id} m={m} agir={agir} desativar={() => setAlvo(m)} pendente={acao.isPending} />)}
      </Tabela>
      <NovoUsuario novo={novo} setores={dados?.setores ?? []} />
      <Confirmar aberto={!!alvo} fechar={() => setAlvo(undefined)} titulo="Desativar usuário" botao="Desativar" destrutivo confirmar={confirmar}>
        Desativar {alvo?.nome}? Ele deixa de entrar na instituição até você reativar.
      </Confirmar>
    </section>
  )
}
