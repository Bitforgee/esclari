import { useRef } from 'react'
import { Alert } from '../../ui/alert'
import { Button } from '../../ui/button'

export function CampoImagem({ rotulo, dica, extra, padrao = { legenda: 'Marca do Esclari (padrão)' }, miniatura, escolher, remover, erro }: {
  rotulo: string
  dica: string
  extra?: string
  padrao?: { src?: string; legenda: string }
  miniatura?: string
  escolher: (arquivo: File) => void
  remover: () => void
  erro?: string
}) {
  const entrada = useRef<HTMLInputElement>(null)
  return (
    <div className="flex flex-col gap-2">
      <p className="m-0 text-sm font-medium">{rotulo}</p>
      <div className="flex items-center gap-3">
        <div className="flex size-16 shrink-0 items-center justify-center overflow-hidden rounded-sm border bg-muted">
          {miniatura ? (
            <img src={miniatura} alt={`Miniatura: ${rotulo}`} width={64} height={64} className="size-full object-contain" />
          ) : (
            padrao.src && <img src={padrao.src} alt="" width={64} height={64} className="size-full object-contain p-1" />
          )}
        </div>
        <div className="flex flex-wrap gap-2">
          <Button type="button" variant="outline" aria-label={`Enviar imagem: ${rotulo}`} onClick={() => entrada.current?.click()}>
            Enviar imagem
          </Button>
          {miniatura && (
            <Button type="button" variant="outline" aria-label={`Usar a marca do Esclari: ${rotulo}`} onClick={remover}>
              Usar a marca do Esclari
            </Button>
          )}
        </div>
        <input
          ref={entrada}
          type="file"
          accept="image/png,image/jpeg"
          aria-label={`Arquivo: ${rotulo}`}
          tabIndex={-1}
          className="sr-only"
          onChange={(e) => {
            const arquivo = e.target.files?.[0]
            if (arquivo) escolher(arquivo)
            e.target.value = ''
          }}
        />
      </div>
      <p className="m-0 text-xs font-medium">{miniatura ? 'Enviada por você' : padrao.legenda}</p>
      <p className="m-0 text-xs text-muted-foreground">{dica}</p>
      {extra && <p className="m-0 text-xs text-muted-foreground">{extra}</p>}
      {erro && <Alert variant="destructive">{erro}</Alert>}
    </div>
  )
}
