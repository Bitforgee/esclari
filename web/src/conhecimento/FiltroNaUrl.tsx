import { useSearchParams } from 'react-router'
import { Label } from '../ui/label'
import { NativeSelect } from '../ui/native-select'

export type Opcao<T> = { valor: string; rotulo: string; aceita: (item: T) => boolean }

function Seletor({ chave, rotulo, valor, opcoes, escolher }: { chave: string; rotulo: string; valor: string; opcoes: { valor: string; rotulo: string }[]; escolher: (valor: string) => void }) {
  return (
    <div className="flex flex-col gap-2">
      <Label htmlFor={`filtro-${chave}`}>{rotulo}</Label>
      <NativeSelect id={`filtro-${chave}`} value={valor} onChange={(e) => escolher(e.target.value)}>
        <option value="">Todos</option>
        {opcoes.map((o) => (
          <option key={o.valor} value={o.valor}>
            {o.rotulo}
          </option>
        ))}
      </NativeSelect>
    </div>
  )
}

export function useFiltroNaUrl<T>(itens: T[] | undefined, chave: string, rotulo: string, opcoes: Opcao<T>[], visivel: boolean) {
  const [params, setParams] = useSearchParams()
  const escolhida = opcoes.find((o) => o.valor === params.get(chave))
  const escolher = (valor: string) =>
    setParams(
      (anterior) => {
        const proximo = new URLSearchParams(anterior)
        if (valor) proximo.set(chave, valor)
        else proximo.delete(chave)
        return proximo
      },
      { replace: true },
    )
  const filtrados = escolhida ? itens?.filter(escolhida.aceita) : itens
  const seletor = visivel ? <Seletor chave={chave} rotulo={rotulo} valor={escolhida?.valor ?? ''} opcoes={opcoes} escolher={escolher} /> : null
  return { filtrados, seletor, ativo: !!escolhida }
}
