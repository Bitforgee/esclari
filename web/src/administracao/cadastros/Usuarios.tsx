import { Plus, Users } from 'lucide-react'
import { useState, type Dispatch, type SetStateAction } from 'react'
import { useGravacao, useLeituras } from '../../acao'
import { api, gravar } from '../../api'
import { Cabecalho } from '../../Cabecalho'
import { Campo, ErroDoCampo, useFocoNoPrimeiroErro } from '../../Campo'
import { Confirmar } from '../../Confirmar'
import { Editor, useSujo } from '../../Editor'
import { ErroDe, errosDeCampo } from '../../ErroDe'
import { Literal } from '../../Literal'
import { Marca } from '../../Marca'
import { CelulaLargo, Detalhe, MenuAcoes, Principal, Tabela, useMostrarMais } from '../../Tabela'
import { Badge } from '../../ui/badge'
import { DropdownMenuItem, DropdownMenuSeparator } from '../../ui/dropdown-menu'
import { Input } from '../../ui/input'
import { Label } from '../../ui/label'
import { NativeSelect } from '../../ui/native-select'
import { TableCell, TableRow } from '../../ui/table'
import { useSessao } from '../../sessao'
import { useBuscaNaUrl, useEditarNaUrl, useTipoNaUrl } from '../../urlDaTela'
import { cpfFormatado, type Setor, type TipoDeUsuario, type Tipos, type Usuario } from '../comum'

type Ficha = Pick<Usuario, 'cpf' | 'nome' | 'email' | 'setores'> & { id?: number; tipoId: number | null; senhaTemporaria: string }
type Edicao = { editando: Ficha; setEditando: Dispatch<SetStateAction<Ficha | undefined>>; setores: Setor[]; tipos: TipoDeUsuario[]; propria?: boolean; erros: Record<string, string> }
const Novo: Ficha = { cpf: '', nome: '', email: '', tipoId: null, setores: [], senhaTemporaria: '' }
const ficha = (u: Usuario): Ficha => ({ id: u.id, cpf: u.cpf, nome: u.nome, email: u.email, tipoId: u.tipo.id, setores: u.setores, senhaTemporaria: '' })
const exigeSetor = (tipos: TipoDeUsuario[], tipoId: number | null) => {
  const tipo = tipos.find((t) => t.id === tipoId)
  return !!tipo && tipo.sistema !== 'GESTOR' && tipo.funcoes.includes('fila.atender')
}
const corpo = (u: Ficha, tipos: TipoDeUsuario[]) => ({ cpf: u.cpf, nome: u.nome, email: u.email, tipoId: u.tipoId, setores: exigeSetor(tipos, u.tipoId) ? u.setores : [], senhaTemporaria: u.senhaTemporaria || undefined })

function alternar<T>(lista: T[], valor: T) {
  return lista.includes(valor) ? lista.filter((x) => x !== valor) : [...lista, valor]
}

const Colunas = [{ titulo: 'Nome' }, { titulo: 'CPF', largo: true }, { titulo: 'E-mail', largo: true }, { titulo: 'Tipo', largo: true }, { titulo: 'Situação' }, { titulo: '' }]

function LinhaUsuario({ u, setores, editar, desativar, pendente, propria }: { u: Usuario; setores: Map<number, string>; editar: () => void; desativar: () => void; pendente: boolean; propria: boolean }) {
  const perfil = u.tipo.nome + (u.setores.length > 0 ? ` · ${u.setores.map((s) => setores.get(s)).join(', ')}` : '')
  return (
    <TableRow>
      <TableCell className={`font-medium ${Principal}`}>
        {u.nome}
        <Detalhe>
          <Literal>{cpfFormatado(u.cpf)}</Literal> · <Literal>{u.email}</Literal> · {perfil}
        </Detalhe>
      </TableCell>
      <CelulaLargo>
        <Literal>{cpfFormatado(u.cpf)}</Literal>
      </CelulaLargo>
      <CelulaLargo>
        <Literal>{u.email}</Literal>
      </CelulaLargo>
      <CelulaLargo>{perfil}</CelulaLargo>
      <TableCell data-celula="selo">{!u.ativo && <Badge variant="secondary">Desativado</Badge>}</TableCell>
      <TableCell data-celula="acoes" className="text-end">
        {u.ativo && (
          <MenuAcoes nome={u.nome} pendente={pendente}>
            <DropdownMenuItem onSelect={editar}>Editar</DropdownMenuItem>
            {!propria && (
              <>
                <DropdownMenuSeparator />
                <DropdownMenuItem variant="destructive" onSelect={desativar}>
                  Desativar
                </DropdownMenuItem>
              </>
            )}
          </MenuAcoes>
        )}
      </TableCell>
    </TableRow>
  )
}

function EscolhaDoTipo({ editando, setEditando, tipos, propria, erros }: Edicao) {
  if (propria) return <p className="text-sm text-muted-foreground">Você não pode mudar o próprio tipo. Peça a outro gestor.</p>
  const escolher = (valor: string) => setEditando((f) => f && { ...f, tipoId: valor === '' ? null : Number(valor) })
  return (
    <div className="flex min-w-0 flex-col gap-2">
      <Label htmlFor="tipo">Tipo</Label>
      <NativeSelect id="tipo" name="tipo" className="w-full" aria-invalid={!!erros.tipoId} value={editando.tipoId ?? ''} onChange={(e) => escolher(e.target.value)}>
        <option value="">Escolha o tipo</option>
        {tipos.map((t) => (
          <option key={t.id} value={t.id}>
            {t.nome}
          </option>
        ))}
      </NativeSelect>
      <ErroDoCampo id="tipo" erro={erros.tipoId} />
    </div>
  )
}

function TipoESetores(props: Edicao) {
  const { editando, setEditando, setores, tipos, erros } = props
  const marcar = (id: number) => setEditando((f) => f && { ...f, setores: alternar(f.setores, id) })
  return (
    <>
      <EscolhaDoTipo {...props} />
      {exigeSetor(tipos, editando.tipoId) && (
        <div className="flex flex-col">
          <Label>Setores de quem atende</Label>
          {setores
            .filter((s) => s.ativo)
            .map((s) => (
              <Marca key={s.id} id={`setor-${s.id}`} rotulo={s.nome} marcado={editando.setores.includes(s.id)} mudar={() => marcar(s.id)} />
            ))}
          <ErroDoCampo id="setores" erro={erros.setores} />
        </div>
      )}
    </>
  )
}

function CamposDoUsuario(props: Edicao) {
  const { editando, setEditando, erros } = props
  const muda = (chave: 'cpf' | 'nome' | 'email' | 'senhaTemporaria') => (e: { target: { value: string } }) => setEditando((f) => f && { ...f, [chave]: e.target.value })
  return (
    <>
      <Campo id="cpf" rotulo="CPF" inputMode="numeric" autoComplete="off" spellCheck={false} placeholder="Ex.: 000.000.000-00…" value={editando.cpf} erro={erros.cpf} onChange={muda('cpf')} />
      <Campo id="nome" rotulo="Nome" value={editando.nome} erro={erros.nome} onChange={muda('nome')} />
      <Campo id="email" rotulo="E-mail" type="email" autoComplete="off" spellCheck={false} value={editando.email} erro={erros.email} onChange={muda('email')} />
      <TipoESetores {...props} />
      <div className="flex flex-col gap-2">
        <Campo id="senha" rotulo="Senha temporária (opcional)" type="password" autoComplete="new-password" value={editando.senhaTemporaria} erro={erros.senhaTemporaria} onChange={muda('senhaTemporaria')} />
        <p className="text-sm text-muted-foreground">Com senha temporária, a pessoa troca a senha no primeiro login. Sem ela, entra pelo primeiro acesso por e-mail.</p>
      </div>
    </>
  )
}

const CamposComErro = ['cpf', 'nome', 'email', 'senhaTemporaria', 'tipoId', 'setores']
const semAcento = (t: string) => t.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase()

function casa(u: Usuario, busca: string) {
  const t = semAcento(busca.trim())
  const digitos = t.replace(/\D/g, '')
  return !t || semAcento(u.nome).includes(t) || semAcento(u.email).includes(t) || cpfFormatado(u.cpf).includes(t) || (digitos !== '' && u.cpf.includes(digitos))
}

const tituloDaJanela = (f?: Ficha) => (f?.id ? 'Editar usuário' : 'Novo usuário')
const vazioDaBusca = (busca: string, tipoId: string) =>
  busca.trim() || tipoId ? { Icone: Users, titulo: 'Nenhum resultado', frase: 'Nenhum usuário combina com a busca.' } : { Icone: Users, titulo: 'Nenhum usuário', frase: 'Cadastre a equipe ou importe as matrículas.' }

function FiltroDeTipo({ tipos, valor, mudar }: { tipos: TipoDeUsuario[]; valor: string; mudar: (v: string) => void }) {
  return (
    <NativeSelect aria-label="Tipo" name="tipoId" className="w-full min-w-0" value={valor} onChange={(e) => mudar(e.target.value)}>
      <option value="">Todos os tipos</option>
      {tipos.map((t) => (
        <option key={t.id} value={t.id}>
          {t.nome}
        </option>
      ))}
    </NativeSelect>
  )
}

function ConfirmarDesativacao({ usuario, fechar, confirmar }: { usuario?: Usuario; fechar: () => void; confirmar: () => void }) {
  return (
    <Confirmar aberto={!!usuario} fechar={fechar} titulo={`Desativar ${usuario?.nome}?`} botao="Desativar" destrutivo confirmar={confirmar}>
      A pessoa perde o acesso na próxima requisição. Conversas, chamados e histórico ficam guardados; os chamados em que ela era responsável voltam para a fila do setor.
    </Confirmar>
  )
}

function useDadosDeUsuarios(tipoId: string) {
  const leituras = useLeituras<{ usuarios: Usuario[]; setores: Setor[]; tipos: Tipos }>({ usuarios: tipoId ? `/api/admin/usuarios?tipoId=${tipoId}` : '/api/admin/usuarios', setores: '/api/admin/setores', tipos: '/api/admin/tipos' })
  return { ...leituras, setores: leituras.dados?.setores ?? [], tipos: leituras.dados?.tipos.tipos ?? [] }
}

export function Usuarios() {
  const { me } = useSessao()
  const [tipoId, setTipoId] = useTipoNaUrl()
  const { dados, erro, recarregar, setores, tipos } = useDadosDeUsuarios(tipoId)
  const [desativar, setDesativar] = useState<Usuario>()
  const [busca, setBusca] = useBuscaNaUrl()
  const acao = useGravacao(recarregar)
  const achar = (id: number) => dados?.usuarios.filter((u) => u.ativo && u.id === id).map(ficha)[0]
  const { editando, setEditando, abrir, fechar } = useEditarNaUrl(achar, Novo, !!dados)
  const sujo = useSujo(editando)
  useFocoNoPrimeiroErro(acao.error)
  const nomes = new Map(setores.map((s) => [s.id, s.nome]))
  const usuarios = dados?.usuarios.filter((u) => casa(u, busca))
  const { visiveis, mais } = useMostrarMais(usuarios)

  const salvar = () => acao.mutate({ chamar: () => gravar('/api/admin/usuarios', editando!.id, corpo(editando!, tipos)), aviso: 'Usuário salvo.', depois: fechar })

  function confirmarDesativar() {
    const u = desativar!
    setDesativar(undefined)
    acao.mutate({ chamar: () => api(`/api/admin/usuarios/${u.id}/desativar`, {}), aviso: 'Usuário desativado.' })
  }

  return (
    <div className="flex flex-col gap-2">
      <Cabecalho botao="Novo usuário" Icone={Plus} aoClicar={() => abrir({ ...Novo })} />
      <ErroDe erro={erro} oque="os usuários" tentar={recarregar} />
      <ErroDe erro={acao.error} oculto={!!editando} />
      <div className="flex flex-col gap-2 md:flex-row">
        <Input type="search" name="busca" autoComplete="off" spellCheck={false} aria-label="Buscar por nome, CPF ou e-mail" placeholder="Buscar por nome, CPF ou e-mail…" value={busca} onChange={(e) => setBusca(e.target.value)} />
        <FiltroDeTipo tipos={tipos} valor={tipoId} mudar={setTipoId} />
      </div>
      <Tabela
        colunas={Colunas}
        itens={usuarios}
        erro={erro}
        vazio={vazioDaBusca(busca, tipoId)}
      >
        {visiveis?.map((u) => (
          <LinhaUsuario key={u.id} u={u} setores={nomes} editar={() => abrir(ficha(u))} desativar={() => setDesativar(u)} pendente={acao.isPending} propria={u.id === me.id} />
        ))}
      </Tabela>
      {mais}

      <Editor sujo={sujo} titulo={tituloDaJanela(editando)} aberto={!!editando} fechar={fechar} salvar={salvar} pendente={acao.isPending} alerta={<ErroDe erro={acao.error} campos={CamposComErro} />} botao="Salvar usuário">
        {editando && <CamposDoUsuario editando={editando} setEditando={setEditando} setores={setores} tipos={tipos} erros={errosDeCampo(acao.error)} propria={editando.id === me.id} />}
      </Editor>

      <ConfirmarDesativacao usuario={desativar} fechar={() => setDesativar(undefined)} confirmar={confirmarDesativar} />
    </div>
  )
}
