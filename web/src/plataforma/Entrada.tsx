import { useMutation, useQuery } from '@tanstack/react-query'
import QRCode from 'qrcode'
import { useRef, useState, type ReactNode } from 'react'
import { BotaoAcao } from '../acao'
import { api } from '../api'
import { useTema } from '../aparencia'
import { Campo, descricaoDoCampo } from '../Campo'
import { ErroDe, errosDeCampo } from '../ErroDe'
import { Alert } from '../ui/alert'
import { useConsulta } from '../ui/use-mobile'

type Etapa = { etapa: 'CODIGO' | 'CADASTRAR'; segredo: string | null; uri: string | null }

const emGrupos = (segredo: string) => segredo.match(/.{1,4}/g)?.join(' ') ?? segredo

export function Moldura({ aviso, children }: { aviso?: string; children: ReactNode }) {
  const tema = useTema()
  return (
    <div className="flex min-h-svh w-full items-center justify-center p-6 md:p-10">
      <div className="flex w-full max-w-sm flex-col gap-6">
        <h1 translate="no" className="font-heading text-[28px] leading-tight font-semibold tracking-tight">
          <img src={tema === 'light' ? '/marca/logo-horizontal.svg' : '/marca/logo-horizontal-escuro.svg'} alt="Esclari" width={174} height={40} className="h-10 w-auto" />
        </h1>
        <h2 className="text-base font-semibold">Console da plataforma</h2>
        {aviso && <Alert role="status">{aviso}</Alert>}
        {children}
      </div>
    </div>
  )
}

function Credenciais({ avancar }: { avancar: (e: Etapa) => void }) {
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const entrar = useMutation({ mutationFn: () => api<Etapa>('/api/plataforma/auth/entrar', { email, senha }), onSuccess: avancar })
  const campos = errosDeCampo(entrar.error)
  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(e) => {
        e.preventDefault()
        entrar.mutate()
      }}
    >
      <Campo id="email" rotulo="E-mail" type="email" autoComplete="username" spellCheck={false} value={email} erro={campos.email} onChange={(e) => setEmail(e.target.value)} />
      <Campo id="senha" rotulo="Senha" type="password" autoComplete="current-password" value={senha} erro={campos.senha} onChange={(e) => setSenha(e.target.value)} />
      <ErroDe erro={entrar.error} campos={['email', 'senha']} />
      <BotaoAcao type="submit" pendente={entrar.isPending} className="h-11">
        Continuar
      </BotaoAcao>
    </form>
  )
}

const Toque = '(pointer: coarse)'

function QrCode({ uri }: { uri: string }) {
  const { data: svg } = useQuery({ queryKey: ['qr', uri], queryFn: () => QRCode.toString(uri, { type: 'svg', errorCorrectionLevel: 'H' }), staleTime: Infinity })
  if (!svg) return null
  return (
    <div className="relative size-50">
      <img src={'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svg)} alt="QR code para o aplicativo autenticador" width={200} height={200} className="size-50 rounded-md bg-white p-2" />
      <img src="/marca/simbolo-pequeno.svg" alt="" width={40} height={40} className="absolute top-1/2 left-1/2 size-10 -translate-x-1/2 -translate-y-1/2 rounded-sm bg-white p-1 text-black" />
    </div>
  )
}

function Cadastro({ etapa }: { etapa: Etapa }) {
  const toque = useConsulta(Toque)
  if (etapa.etapa !== 'CADASTRAR' || !etapa.segredo) return <p className="m-0 text-sm text-muted-foreground">Digite os 6 dígitos do aplicativo autenticador.</p>
  return (
    <div className="flex flex-col gap-3 text-sm">
      <p className="m-0 text-muted-foreground">Escaneie o QR code com o aplicativo autenticador. Se não der, digite a chave abaixo.</p>
      {etapa.uri && <QrCode uri={etapa.uri} />}
      <p translate="no" className="m-0 font-mono text-base tracking-wider break-all" aria-label="Chave do aplicativo">
        {emGrupos(etapa.segredo)}
      </p>
      {etapa.uri && toque && (
        <a href={etapa.uri} className="inline-flex min-h-11 items-center underline">
          Abrir no aplicativo
        </a>
      )}
    </div>
  )
}

const ClasseDoCodigo = 'h-14 max-md:h-14 pointer-coarse:h-14 pl-[calc(0.75rem+0.5em)] text-center font-mono text-2xl md:text-2xl pointer-coarse:text-2xl tracking-[0.5em]'

function Codigo({ etapa, entrou }: { etapa: Etapa; entrou: () => void }) {
  const [codigo, setCodigo] = useState('')
  const campo = useRef<HTMLInputElement>(null)
  const confirmar = useMutation({
    mutationFn: (digitos: string) => api('/api/plataforma/auth/codigo', { codigo: digitos }),
    onSuccess: entrou,
    onError: () => {
      setCodigo('')
      campo.current?.focus()
    },
  })
  const enviar = (digitos: string) => {
    if (!confirmar.isPending) confirmar.mutate(digitos)
  }
  const preencher = (texto: string) => {
    const digitos = texto.replace(/\D/g, '').slice(0, 6)
    setCodigo(digitos)
    if (digitos.length === 6) enviar(digitos)
  }
  const erro = errosDeCampo(confirmar.error).codigo
  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(e) => {
        e.preventDefault()
        enviar(codigo)
      }}
    >
      <Cadastro etapa={etapa} />
      <Campo
        ref={campo}
        id="codigo"
        rotulo="Código de 6 dígitos"
        inputMode="numeric"
        autoComplete="one-time-code"
        autoFocus
        maxLength={6}
        spellCheck={false}
        className={ClasseDoCodigo}
        value={codigo}
        erro={erro}
        aria-describedby={descricaoDoCampo('codigo', undefined, erro)}
        onChange={(e) => preencher(e.target.value)}
        onPaste={(e) => {
          e.preventDefault()
          preencher(e.clipboardData.getData('text'))
        }}
      />
      <ErroDe erro={confirmar.error} campos={['codigo']} />
      <BotaoAcao type="submit" pendente={confirmar.isPending} className="h-11">
        Entrar
      </BotaoAcao>
    </form>
  )
}
export function Entrada({ aviso, entrou }: { aviso?: string; entrou: () => void }) {
  const [etapa, setEtapa] = useState<Etapa>()
  return <Moldura aviso={aviso}>{etapa ? <Codigo etapa={etapa} entrou={entrou} /> : <Credenciais avancar={setEtapa} />}</Moldura>
}
