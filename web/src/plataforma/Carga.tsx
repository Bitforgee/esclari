import { useMutation } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router'
import { api, ErroApi } from '../api'
import { CampoArquivo } from '../CampoArquivo'
import { Editor } from '../Editor'
import { ErroDe } from '../ErroDe'
import { plural } from '../formatos'
import { Literal } from '../Literal'
import { Principal, Tabela } from '../Tabela'
import { Alert } from '../ui/alert'
import { Button } from '../ui/button'
import { TableCell, TableRow } from '../ui/table'

export type Contagens = { setores: number; cursos: number; categorias: number; regras: number; artigos: number }
export type Alvo = { slug: string; nome: string }
export type Criada = Alvo & { email: string }

const errosDoArquivo = (erro: unknown): string[] => (erro instanceof ErroApi ? (erro.corpo?.errors?.arquivo ?? []) : [])

export const textoDasContagens = (r: Contagens) =>
  `${plural(r.setores, 'setor', 'setores')}, ${plural(r.cursos, 'curso', 'cursos')}, ${plural(r.categorias, 'categoria', 'categorias')}, ${plural(r.regras, 'regra', 'regras')}, ${plural(r.artigos, 'artigo', 'artigos')}`

export function ResultadoDaCarga({ r, erro, prefixo = 'Importados' }: { r?: Contagens; erro: Error | null; prefixo?: string }) {
  const erros = errosDoArquivo(erro)
  if (r) return <Alert>{`${prefixo}: ${textoDasContagens(r)}.`}</Alert>
  if (erros.length > 0)
    return (
      <Tabela colunas={[{ titulo: 'Erro' }]} itens={erros}>
        {erros.map((e, i) => (
          <TableRow key={i}>
            <TableCell className={Principal}>{e}</TableCell>
          </TableRow>
        ))}
      </Tabela>
    )
  return <ErroDe erro={erro} />
}

export function ImportarCarga({ alvo, fechar }: { alvo: Alvo; fechar: () => void }) {
  const [arquivo, setArquivo] = useState<File>()
  const importacao = useMutation({ mutationFn: (f: FormData) => api<Contagens>(`/api/plataforma/instituicoes/${alvo.slug}/carga`, f) })

  function importar() {
    const f = new FormData()
    f.set('arquivo', arquivo!)
    importacao.mutate(f)
  }

  return (
    <Editor
      titulo={`Importar carga: ${alvo.nome}`}
      aberto
      sujo={!!arquivo}
      fechar={fechar}
      salvar={importar}
      botao="Importar"
      pendente={importacao.isPending}
      desabilitado={!arquivo}
      alerta={<ResultadoDaCarga r={importacao.data} erro={importacao.error} />}
    >
      <CampoArquivo id="carga" rotulo="Arquivo de carga (JSON)" accept=".json,application/json" multiple={false} arquivos={arquivo ? [arquivo] : []} setArquivos={([a]) => setArquivo(a)} />
    </Editor>
  )
}

export const AvisoDeCriacao = ({ criada, importar }: { criada: Criada; importar: () => void }) => (
  <Alert>
    <div className="flex flex-col items-start gap-2">
      <p className="m-0">
        Instituição {criada.nome} criada. Enviamos o link de primeiro acesso para <Literal>{criada.email}</Literal>. <Link to={`/${criada.slug}`} translate="no">/{criada.slug}</Link>
      </p>
      <Button variant="outline" onClick={importar}>
        Importar carga
      </Button>
    </div>
  </Alert>
)
