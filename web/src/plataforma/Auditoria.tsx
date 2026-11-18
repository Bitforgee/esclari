import { ScrollText } from 'lucide-react'
import { useState } from 'react'
import { useLeituras } from '../acao'
import { ErroDe } from '../ErroDe'
import { formatarData } from '../datas'
import { Button } from '../ui/button'
import { CelulaLargo, Detalhe, Principal, Tabela } from '../Tabela'
import { TableCell, TableRow } from '../ui/table'

type Item = { acao: string; operador: string; instituicao: string | null; resumo: string; criadaEm: string }
type Pagina = { itens: Item[]; total: number }

const PorPagina = 50
const Fuso = 'America/Sao_Paulo'

const Acoes: Record<string, string> = {
  OPERADOR_ENTROU: 'Entrou no console',
  INSTITUICAO_CRIADA: 'Instituição criada',
  INSTITUICAO_ALTERADA: 'Instituição alterada',
  INSTITUICAO_DESATIVADA: 'Instituição desativada',
  INSTITUICAO_REATIVADA: 'Instituição reativada',
  ACESSO_REENVIADO: 'Acesso reenviado',
  CARGA_IMPORTADA: 'Carga importada',
  OPERADOR_ALTERADO: 'Operador alterado',
}

const Colunas = [{ titulo: 'Quando' }, { titulo: 'Ação' }, { titulo: 'Operador', largo: true }, { titulo: 'Instituição', largo: true }, { titulo: 'Resumo', largo: true }]
const SemRegistros = { Icone: ScrollText, titulo: 'Nenhum registro', frase: 'Ainda não há ações registradas.' }

function Linha({ a }: { a: Item }) {
  const alvo = a.instituicao ? `/${a.instituicao}` : '-'
  return (
    <TableRow>
      <TableCell className={Principal}>
        {formatarData(a.criadaEm, Fuso)}
        <Detalhe>
          {a.operador} · {alvo} · {a.resumo}
        </Detalhe>
      </TableCell>
      <TableCell className={Principal}>{Acoes[a.acao] ?? a.acao}</TableCell>
      <CelulaLargo>{a.operador}</CelulaLargo>
      <CelulaLargo>
        <span translate="no">{alvo}</span>
      </CelulaLargo>
      <CelulaLargo>{a.resumo}</CelulaLargo>
    </TableRow>
  )
}

function Paginacao({ pagina, total, ir }: { pagina: number; total: number; ir: (p: number) => void }) {
  if (total <= PorPagina) return null
  return (
    <div className="flex items-center justify-between gap-2">
      <Button variant="outline" disabled={pagina === 1} onClick={() => ir(pagina - 1)}>
        Mais recentes
      </Button>
      <Button variant="outline" disabled={pagina * PorPagina >= total} onClick={() => ir(pagina + 1)}>
        Mais antigos
      </Button>
    </div>
  )
}

export function Auditoria() {
  const [pagina, setPagina] = useState(1)
  const { dados, erro, recarregar } = useLeituras<{ lista: Pagina }>({ lista: `/api/plataforma/auditoria?pagina=${pagina}` })
  return (
    <div className="flex flex-col gap-2">
      <ErroDe erro={erro} oque="a auditoria" tentar={recarregar} />
      <Tabela colunas={Colunas} itens={dados?.lista.itens} erro={erro} vazio={SemRegistros}>
        {dados?.lista.itens.map((a, i) => (
          <Linha key={`${a.criadaEm}-${i}`} a={a} />
        ))}
      </Tabela>
      <Paginacao pagina={pagina} total={dados?.lista.total ?? 0} ir={setPagina} />
    </div>
  )
}