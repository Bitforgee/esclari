import { Plus, UserCog } from 'lucide-react'
import { useState, type Dispatch, type SetStateAction } from 'react'
import { Link, useParams } from 'react-router'
import { useGravacao, useLeituras } from '../../acao'
import { api, gravar } from '../../api'
import { Cabecalho } from '../../Cabecalho'
import { Campo, ErroDoCampo, useFocoNoPrimeiroErro } from '../../Campo'
import { Confirmar } from '../../Confirmar'
import { Editor, useSujo } from '../../Editor'
import { ErroDe, errosDeCampo } from '../../ErroDe'
import { CelulaLargo, Detalhe, MenuAcoes, Principal, Tabela } from '../../Tabela'
import { useEditarNaUrl } from '../../urlDaTela'
import { Badge } from '../../ui/badge'
import { DropdownMenuItem, DropdownMenuSeparator } from '../../ui/dropdown-menu'
import { Label } from '../../ui/label'
import { TableCell, TableRow } from '../../ui/table'
import type { Funcao, TipoDeUsuario, Tipos as TiposDoServidor } from '../comum'

type Ficha = { id?: number; nome: string; funcoes: string[] }
type Edicao = { editando: Ficha; setEditando: Dispatch<SetStateAction<Ficha | undefined>>; catalogo: Funcao[]; erros: Record<string, string> }

const Novo: Ficha = { nome: '', funcoes: [] }
const ficha = (t: TipoDeUsuario): Ficha => ({ id: t.id, nome: t.nome, funcoes: t.funcoes })
const Colunas = [{ titulo: 'Tipo' }, { titulo: 'Usuários' }, { titulo: 'Funções', largo: true }, { titulo: '' }]
const CamposComErro = ['nome', 'funcoes']

const ordenar = (tipos: TipoDeUsuario[]) => [...tipos.filter((t) => t.sistema !== null), ...tipos.filter((t) => t.sistema === null).sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'))]
const editavel = (t: TipoDeUsuario) => t.sistema === null || t.sistema === 'ATENDENTE'

function Contagem({ tipo }: { tipo: TipoDeUsuario }) {
  const { slug } = useParams()
  if (tipo.usuarios === 0) return <span>0</span>
  return (
    <Link to={`/${slug}/usuarios?tipoId=${tipo.id}`} className="inline-flex min-h-11 min-w-11 items-center justify-center underline underline-offset-4 md:min-h-0 md:min-w-0">
      {tipo.usuarios}
    </Link>
  )
}

function LinhaDoTipo({ tipo, rotulos, editar, apagar, pendente }: { tipo: TipoDeUsuario; rotulos: Map<string, string>; editar: () => void; apagar: () => void; pendente: boolean }) {
  const funcoes = tipo.funcoes.map((f) => rotulos.get(f) ?? f).join(', ') || 'Nenhuma função'
  return (
    <TableRow>
      <TableCell className={`font-medium ${Principal}`}>
        {tipo.nome} {tipo.sistema && <Badge variant="secondary">Tipo de sistema</Badge>}
        <Detalhe>{funcoes}</Detalhe>
      </TableCell>
      <TableCell>
        <Contagem tipo={tipo} />
      </TableCell>
      <CelulaLargo>{funcoes}</CelulaLargo>
      <TableCell data-celula="acoes" className="text-end">
        {editavel(tipo) && (
          <MenuAcoes nome={tipo.nome} pendente={pendente}>
            <DropdownMenuItem onSelect={editar}>Editar</DropdownMenuItem>
            {tipo.sistema === null && (
              <>
                <DropdownMenuSeparator />
                <DropdownMenuItem variant="destructive" onSelect={apagar}>
                  Apagar
                </DropdownMenuItem>
              </>
            )}
          </MenuAcoes>
        )}
      </TableCell>
    </TableRow>
  )
}

function CaixaDeFuncao({ funcao, marcada, mudar }: { funcao: Funcao; marcada: boolean; mudar: () => void }) {
  const id = `funcao-${funcao.codigo}`
  return (
    <Label htmlFor={id} className="min-h-11 w-full cursor-pointer items-start gap-3 py-2">
      <input type="checkbox" id={id} aria-labelledby={`${id}-rotulo`} aria-describedby={`${id}-detalhe`} className="mt-0.5 size-5 shrink-0 accent-primary" checked={marcada} onChange={mudar} />
      <span className="flex min-w-0 flex-col gap-1">
        <span id={`${id}-rotulo`} className="font-medium">
          {funcao.rotulo}
        </span>
        <span id={`${id}-detalhe`} className="flex flex-col gap-1 text-sm font-normal text-muted-foreground">
          <span>{funcao.descricao}</span>
          {funcao.deAluno && <Badge variant="secondary">Função de aluno</Badge>}
          {funcao.expoeDadosPessoais && <span className="text-destructive">Mostra dados pessoais dos alunos.</span>}
        </span>
      </span>
    </Label>
  )
}

function alternar(lista: string[], valor: string) {
  return lista.includes(valor) ? lista.filter((x) => x !== valor) : [...lista, valor]
}

function CamposDoTipo({ editando, setEditando, catalogo, erros }: Edicao) {
  const marcar = (codigo: string) => setEditando((f) => f && { ...f, funcoes: alternar(f.funcoes, codigo) })
  return (
    <>
      <Campo id="nome" rotulo="Nome" value={editando.nome} erro={erros.nome} onChange={(e) => setEditando((f) => f && { ...f, nome: e.target.value })} />
      <fieldset className="m-0 flex min-w-0 flex-col border-0 p-0" aria-describedby={erros.funcoes ? 'funcoes-erro' : undefined}>
        <legend className="mb-1 p-0 text-sm font-medium">Funções</legend>
        {catalogo.map((f) => (
          <CaixaDeFuncao key={f.codigo} funcao={f} marcada={editando.funcoes.includes(f.codigo)} mudar={() => marcar(f.codigo)} />
        ))}
        <ErroDoCampo id="funcoes" erro={erros.funcoes} />
      </fieldset>
    </>
  )
}

function useTipos() {
  const { dados, erro, recarregar } = useLeituras<{ tipos: TiposDoServidor }>({ tipos: '/api/admin/tipos' })
  const catalogo = dados?.tipos.catalogo ?? []
  const achar = (id: number) => dados?.tipos.tipos.filter((t) => t.id === id && editavel(t)).map(ficha)[0]
  return { pronto: !!dados, erro, recarregar, catalogo, achar, rotulos: new Map(catalogo.map((f) => [f.codigo, f.rotulo])), tipos: dados && ordenar(dados.tipos.tipos) }
}

export function Tipos() {
  const { pronto, erro, recarregar, catalogo, achar, rotulos, tipos } = useTipos()
  const [apagar, setApagar] = useState<TipoDeUsuario>()
  const acao = useGravacao(recarregar)
  const { editando, setEditando, abrir, fechar } = useEditarNaUrl(achar, Novo, pronto)
  const sujo = useSujo(editando)
  useFocoNoPrimeiroErro(acao.error)

  const salvar = () => acao.mutate({ chamar: () => gravar('/api/admin/tipos', editando!.id, { nome: editando!.nome, funcoes: editando!.funcoes }), aviso: 'Tipo salvo.', depois: fechar })

  function confirmarExclusao() {
    const t = apagar!
    setApagar(undefined)
    acao.mutate({ chamar: () => api(`/api/admin/tipos/${t.id}`, undefined, 'DELETE'), aviso: 'Tipo apagado.' })
  }

  return (
    <div className="flex flex-col gap-2">
      <Cabecalho botao={erro ? undefined : 'Novo tipo'} Icone={Plus} aoClicar={() => abrir({ ...Novo })} />
      <ErroDe erro={erro} oque="os tipos de usuário" tentar={recarregar} />
      <ErroDe erro={acao.error} oculto={!!editando} />
      <Tabela colunas={Colunas} itens={tipos} erro={erro} vazio={{ Icone: UserCog, titulo: 'Nenhum tipo', frase: 'Crie um tipo para dar funções à equipe.' }}>
        {tipos?.map((t) => (
          <LinhaDoTipo key={t.id} tipo={t} rotulos={rotulos} editar={() => abrir(ficha(t))} apagar={() => setApagar(t)} pendente={acao.isPending} />
        ))}
      </Tabela>

      <Editor sujo={sujo} titulo={editando?.id ? 'Editar tipo' : 'Novo tipo'} aberto={!!editando} fechar={fechar} salvar={salvar} pendente={acao.isPending} alerta={<ErroDe erro={acao.error} campos={CamposComErro} />} botao="Salvar">
        {editando && <CamposDoTipo editando={editando} setEditando={setEditando} catalogo={catalogo} erros={errosDeCampo(acao.error)} />}
      </Editor>

      <Confirmar aberto={!!apagar} fechar={() => setApagar(undefined)} titulo={`Apagar o tipo ${apagar?.nome}?`} botao="Apagar" destrutivo confirmar={confirmarExclusao}>
        Só é possível apagar um tipo que nenhum usuário usa.
      </Confirmar>
    </div>
  )
}
