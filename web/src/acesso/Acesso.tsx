import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { Eye, EyeOff } from 'lucide-react'
import { useState, type ComponentProps, type ReactNode } from 'react'
import { useForm, type FieldValues, type Path, type Resolver, type UseFormReturn } from 'react-hook-form'
import { Link, Navigate, useNavigate, useParams, useSearchParams } from 'react-router'
import { z } from 'zod'
import { BotaoAcao } from '../acao'
import { api, ErroApi } from '../api'
import { Carregando } from '../Carregando'
import { ErroDe } from '../ErroDe'
import { descricaoDoCampo } from '../Campo'
import { cpfMascara, PropsDoCpf, soDigitos } from '../mascaras'
import { instituicaoLembrada, lembrarInstituicao, urlDaImagem, useInstituicao, usePlaca, useTema, type InstituicaoPublica } from '../aparencia'
import { telaInicial, useSessao } from '../sessao'
import { Alert } from '../ui/alert'
import { Escolha } from './Escolha'
import { Button } from '../ui/button'
import { Input } from '../ui/input'
import { Label } from '../ui/label'

export const LinkDeApoio = 'inline-flex min-h-11 items-center self-start text-sm text-muted-foreground underline underline-offset-4 hover:text-foreground'

const obrigatorio = z.string().min(1, 'Preencha este campo.')

export function Centro({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-svh w-full items-center justify-center p-6 md:p-10">
      <div className="w-full max-w-sm">{children}</div>
    </div>
  )
}

const TituloBase = 'Atendimento ao estudante'
const Frase = 'Dúvidas, solicitações e respostas da sua instituição em um só lugar.'
const Itens = ['Tire dúvidas com o assistente a qualquer hora', 'Acompanhe suas solicitações até a resposta', 'Fale com a equipe da instituição quando precisar']

const H1 = ({ children, translate }: { children: ReactNode; translate?: 'no' }) => <h1 translate={translate} className="font-heading text-[28px] leading-tight font-semibold tracking-tight">{children}</h1>

export function NaoEncontrada() {
  const { slug } = useParams()
  return (
    <Centro>
      <div className="flex flex-col items-start gap-3">
        <H1>Página não encontrada</H1>
        <p className="m-0 text-muted-foreground">O endereço que você abriu não existe.</p>
        <Button asChild className="h-11">
          <Link to={`/${slug}`}>Voltar ao início</Link>
        </Button>
      </div>
    </Centro>
  )
}

export function Raiz() {
  const lembrada = instituicaoLembrada()
  return lembrada ? <Navigate to={`/${lembrada}`} replace /> : <Escolha />
}

function Identidade({ slug, d }: { slug?: string; d?: InstituicaoPublica }) {
  const tema = useTema()
  const placa = usePlaca()
  const logo = slug && d?.imagens.logo
  return (
    <div className="mb-3 flex h-12 items-center">
      {logo ? <img src={urlDaImagem(slug, 'logo', logo)} alt={d.nome} width={192} height={48} className={`h-12 w-auto max-w-full object-contain ${placa}`} /> : d && <img src={tema === 'light' ? '/marca/logo-horizontal.svg' : '/marca/logo-horizontal-escuro.svg'} alt="Esclari" width={174} height={40} className="h-10 w-auto" />}
    </div>
  )
}

function Nome({ slug, d }: { slug?: string; d?: InstituicaoPublica }) {
  return (
    <div className="flex flex-col gap-1">
      <Identidade slug={slug} d={d} />
      {d && <H1 translate="no">{d.nome}</H1>}
    </div>
  )
}

function Textos({ d }: { d?: InstituicaoPublica }) {
  return (
    <div className="flex flex-col gap-1 text-muted-foreground">
      <h2 className="text-base font-medium">{d?.login.titulo ?? TituloBase}</h2>
      <p className="m-0">{d?.login.frase ?? Frase}</p>
    </div>
  )
}

function ListaDeItens({ d }: { d?: InstituicaoPublica }) {
  return (
    <ul className="m-0 flex list-disc flex-col gap-1 ps-5 text-sm text-muted-foreground">
      {(d?.login.itens.length ? d.login.itens : Itens).map((texto) => (
        <li key={texto}>{texto}</li>
      ))}
    </ul>
  )
}

function Foto({ foto, className, lateral }: { foto: string | null; className: string; lateral?: boolean }) {
  if (!foto) return null
  const img = <img src={foto} alt="" width={800} height={lateral ? 1200 : 160} className={className} />
  return lateral ? <div className="relative hidden lg:block">{img}</div> : img
}

function Entrada({ tela, children }: { tela?: string; children: ReactNode }) {
  const { slug } = useParams()
  const { data: d } = useInstituicao(slug)
  const foto = slug && d?.imagens.login ? urlDaImagem(slug, 'login', d.imagens.login) : null
  return (
    <div className={foto ? 'grid min-h-svh max-lg:grid-rows-[auto_1fr] lg:grid-cols-2' : 'flex min-h-svh'}>
      <Foto foto={foto} className="h-40 w-full object-cover lg:hidden" />
      <div className="flex w-full flex-col items-center justify-center p-6 md:p-10">
        <div className="flex w-full max-w-[400px] flex-col gap-8">
          <Nome slug={slug} d={d} />
          <Textos d={d} />
          {tela && <h2 className="text-base font-semibold">{tela}</h2>}
          <div className="flex flex-col gap-6">{children}</div>
          <ListaDeItens d={d} />
        </div>
      </div>
      <Foto foto={foto} className="absolute inset-0 size-full object-cover" lateral />
    </div>
  )
}

type Props<T extends FieldValues> = { f: UseFormReturn<T>; nome: Path<T>; rotulo: string; dica?: string; mascara?: (texto: string) => string } & ComponentProps<'input'>

function BotaoVerSenha({ visivel, alternar }: { visivel: boolean; alternar: () => void }) {
  return (
    <button
      type="button"
      aria-label={visivel ? 'Ocultar senha' : 'Mostrar senha'}
      aria-pressed={visivel}
      className="absolute inset-y-0 right-0 flex w-11 items-center justify-center rounded-md text-muted-foreground hover:text-foreground"
      onClick={alternar}
    >
      {visivel ? <EyeOff aria-hidden="true" className="size-4" /> : <Eye aria-hidden="true" className="size-4" />}
    </button>
  )
}

function DicaDoCampo({ nome, dica }: { nome: string; dica?: string }) {
  return dica ? (
    <p id={`${nome}-dica`} className="text-sm text-muted-foreground">
      {dica}
    </p>
  ) : null
}

function Campo<T extends FieldValues>({ f, nome, rotulo, dica, type, mascara, ...props }: Props<T>) {
  const [visivel, setVisivel] = useState(false)
  const registro = f.register(nome)
  const onChange: typeof registro.onChange = (e) => {
    if (mascara) e.target.value = mascara(e.target.value)
    return registro.onChange(e)
  }
  const senha = type === 'password'
  const erro = f.formState.errors[nome]?.message as string | undefined
  return (
    <div className="flex flex-col gap-2">
      <Label htmlFor={nome}>{rotulo}</Label>
      <div className="relative">
        <Input id={nome} type={senha && visivel ? 'text' : type} aria-invalid={!!f.formState.errors[nome]} aria-describedby={descricaoDoCampo(nome, dica, erro)} className={senha ? 'pr-12' : undefined} {...props} {...registro} onChange={onChange} />
        {senha && <BotaoVerSenha visivel={visivel} alternar={() => setVisivel((v) => !v)} />}
      </div>
      {erro && (
        <p id={`${nome}-erro`} role="alert" className="text-sm text-destructive">
          {erro}
        </p>
      )}
      <DicaDoCampo nome={nome} dica={dica} />
    </div>
  )
}

function Formulario<T extends FieldValues>({ schema, enviar, botao, className, children }: {
  schema: z.ZodType<T>
  enviar: (v: T) => Promise<unknown>
  botao: string
  className?: string
  children: (form: UseFormReturn<T>) => ReactNode
}) {
  const form = useForm<T>({ resolver: zodResolver(schema as never) as Resolver<T> })
  const m = useMutation({ mutationFn: enviar })
  return (
    <>
      <form noValidate onSubmit={form.handleSubmit((v) => m.mutate(v))} className="flex flex-col gap-6">
        {children(form)}
        <BotaoAcao type="submit" pendente={m.isPending} className={className}>
          {botao}
        </BotaoAcao>
      </form>
      {m.error && <Alert variant="destructive">{m.error.message}</Alert>}
    </>
  )
}

const schemaLogin = z.object({ cpf: obrigatorio, senha: obrigatorio })

const VoltaDoMoodle: Record<string, string> = {
  'sem-cadastro': 'Não encontramos seu cadastro no Esclari. Fale com a secretaria.',
  inativo: 'Seu acesso ao Esclari está desativado. Fale com a secretaria.',
  'use-senha': 'Equipe entra com CPF e senha.',
}

export function Login() {
  const { slug } = useParams()
  const navigate = useNavigate()
  const [busca] = useSearchParams()
  const { isError, isPending, error, refetch } = useInstituicao(slug)
  const motivo = Object.hasOwn(VoltaDoMoodle, busca.get('moodle') ?? '') ? VoltaDoMoodle[busca.get('moodle')!] : undefined

  if (isError)
    return (
      <Centro>
        {error instanceof ErroApi ? (
          <div className="relative">
            <Alert>Instituição não encontrada.</Alert>
            <Link to="/" className={`${LinkDeApoio} absolute top-full left-0`}>
              Escolher instituição
            </Link>
          </div>
        ) : (
          <ErroDe erro={error} oque="a instituição" tentar={() => void refetch()} />
        )}
      </Centro>
    )
  if (isPending)
    return (
      <Centro>
        <Carregando className="mx-auto flex size-8 justify-center" />
      </Centro>
    )

  return (
    <Entrada>
      {motivo && <Alert>{motivo}</Alert>}
      <Formulario
        schema={schemaLogin}
        botao="Entrar"
        className="w-full"
        enviar={async (v) => {
          const u = await api<{ permissoes: string[]; deveTrocarSenha: boolean }>('/api/auth/login', { instituicao: slug, ...v, cpf: soDigitos(v.cpf) })
          navigate(`/${slug}/${telaInicial(u)}`)
        }}
      >
        {(form) => (
          <>
            <p className="m-0 -mb-2 text-sm text-muted-foreground">Entre com seu CPF e sua senha.</p>
            <Campo f={form} nome="cpf" rotulo="CPF" {...PropsDoCpf} mascara={cpfMascara} autoComplete="username" />
            <Campo f={form} nome="senha" rotulo="Senha" type="password" autoComplete="current-password" />
          </>
        )}
      </Formulario>
      <Link to={`/${slug}/primeiro-acesso`} className={LinkDeApoio}>
        Primeiro acesso ou esqueci minha senha
      </Link>
      <Link to="/" onClick={() => lembrarInstituicao(null)} className={LinkDeApoio}>
        Trocar de instituição
      </Link>
    </Entrada>
  )
}

export function PrimeiroAcesso() {
  const { slug } = useParams()
  const [mensagem, setMensagem] = useState<string>()
  return (
    <Entrada tela="Primeiro acesso">
      {mensagem ? (
        <Alert>{mensagem}</Alert>
      ) : (
        <Formulario
          schema={z.object({ cpf: obrigatorio })}
          botao="Enviar link"
          className="w-full"
          enviar={async (v) => setMensagem((await api<{ mensagem: string }>('/api/auth/primeiro-acesso', { instituicao: slug, cpf: soDigitos(v.cpf) })).mensagem)}
        >
          {(form) => <Campo f={form} nome="cpf" rotulo="CPF" {...PropsDoCpf} mascara={cpfMascara} autoComplete="off" dica="Enviaremos um link para criar a senha no e-mail registrado na instituição." />}
        </Formulario>
      )}
      <Link to={`/${slug}`} className="inline-flex min-h-11 items-center self-start text-sm text-muted-foreground underline underline-offset-4 hover:text-foreground">
        Voltar para o login
      </Link>
    </Entrada>
  )
}

export function DefinirSenha() {
  const { slug } = useParams()
  const [busca] = useSearchParams()
  const [pronto, setPronto] = useState(false)
  return (
    <Entrada tela="Criar senha">
      {pronto ? (
        <>
          <Alert>Senha criada. Entre com o CPF e a nova senha.</Alert>
          <Button asChild className="h-11">
            <Link to={`/${slug}`}>Entrar</Link>
          </Button>
        </>
      ) : (
        <Formulario
          schema={z.object({ novaSenha: obrigatorio })}
          botao="Salvar senha"
          className="w-full"
          enviar={async (v) => {
            await api('/api/auth/definir-senha', { token: busca.get('token'), ...v })
            setPronto(true)
          }}
        >
          {(form) => <Campo f={form} nome="novaSenha" rotulo="Nova senha" type="password" autoComplete="new-password" dica="De 8 a 128 caracteres." />}
        </Formulario>
      )}
    </Entrada>
  )
}

export function TrocarSenha() {
  const { slug } = useParams()
  const navigate = useNavigate()
  const { me, setMe } = useSessao()
  return (
    <div className="mx-auto flex max-w-sm flex-col gap-6">
      {me.deveTrocarSenha && <Alert>Troque a senha temporária para continuar.</Alert>}
      <Formulario
        schema={z.object({ senhaAtual: obrigatorio, novaSenha: obrigatorio })}
        botao="Salvar nova senha"
        enviar={async (v) => {
          await api('/api/auth/senha', v)
          const atualizado = { ...me, deveTrocarSenha: false }
          setMe(atualizado)
          navigate(`/${slug}/${telaInicial(atualizado)}`)
        }}
      >
        {(form) => (
          <>
            <Campo f={form} nome="senhaAtual" rotulo="Senha atual" type="password" autoComplete="current-password" />
            <Campo f={form} nome="novaSenha" rotulo="Nova senha" type="password" autoComplete="new-password" dica="De 8 a 128 caracteres." />
          </>
        )}
      </Formulario>
    </div>
  )
}
