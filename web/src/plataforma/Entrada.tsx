import { useMutation } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { BotaoAcao } from '../acao'
import { api } from '../api'
import { useTema } from '../aparencia'
import { Campo, descricaoDoCampo } from '../Campo'
import { ErroDe, errosDeCampo } from '../ErroDe'
import { Alert } from '../ui/alert'

type Etapa = { etapa: 'CODIGO' | 'CADASTRAR'; segredo: string | null; uri: string | null }

const emGrupos = (segredo: string) => segredo.match(/.{1,4}/g)?.join(' ') ?? segredo

function Moldura({ aviso, children }: { aviso?: string; children: ReactNode }) {
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

function Cadastro({ etapa }: { etapa: Etapa }) {
  if (etapa.etapa !== 'CADASTRAR' || !etapa.segredo) return <p className="m-0 text-sm text-muted-foreground">Digite os 6 dígitos do aplicativo autenticador.</p>
  return (
    <div className="flex flex-col gap-3 text-sm">
      <p className="m-0 text-muted-foreground">Cadastre esta conta no aplicativo autenticador. Digite a chave abaixo ou abra o link no celular.</p>
      <p translate="no" className="m-0 font-mono text-base tracking-wider break-all" aria-label="Chave do aplicativo">
        {emGrupos(etapa.segredo)}
      </p>
      {etapa.uri && (
        <a href={etapa.uri} className="inline-flex min-h-11 items-center underline">
          Abrir no aplicativo
        </a>
      )}
    </div>
  )
}

function Codigo({ etapa, entrou }: { etapa: Etapa; entrou: () => void }) {
  const [codigo, setCodigo] = useState('')
  const confirmar = useMutation({ mutationFn: () => api('/api/plataforma/auth/codigo', { codigo }), onSuccess: entrou })
  const erro = errosDeCampo(confirmar.error).codigo
  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(e) => {
        e.preventDefault()
        confirmar.mutate()
      }}
    >
      <Cadastro etapa={etapa} />
      <Campo
        id="codigo"
        rotulo="Código de 6 dígitos"
        inputMode="numeric"
        autoComplete="one-time-code"
        maxLength={6}
        spellCheck={false}
        value={codigo}
        erro={erro}
        aria-describedby={descricaoDoCampo('codigo', undefined, erro)}
        onChange={(e) => setCodigo(e.target.value.replace(/\D/g, ''))}
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
