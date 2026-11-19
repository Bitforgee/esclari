import { useQuery, useQueryClient } from '@tanstack/react-query'
import { BookText, ChartColumn, ClipboardList, ChevronLeft, CircleAlert, CircleUser, FileText, Inbox, LayoutGrid, ListChecks, LogOut, MessageCircle, Menu as IconeMenu, Monitor, Moon, MoonStar, Palette, Phone, Plug, Settings, Sun, Upload, UserCog, Users } from 'lucide-react'
import { useEffect, useId, useLayoutEffect, useRef, useState } from 'react'
import { Link as RouterLink, Navigate, NavLink, Outlet, useLocation, useMatch, useNavigate, useParams } from 'react-router'
import { api, ErroApi, type Me } from './api'
import { Carregando } from './Carregando'
import { ErroDe } from './ErroDe'
import { DialogoDeContato, iniciais, normalizar, Sigla, temContato, urlDaImagem, useInstituicao, usePlaca, useRota, type InstituicaoPublica } from './aparencia'
import { AvisosPush } from './AvisosPush'
import { telaInicial, useSair, type Sessao } from './sessao'
import { useTempoReal } from './tempo-real'
import { Alert } from './ui/alert'
import { Button } from './ui/button'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from './ui/dropdown-menu'
import { Popover, PopoverContent, PopoverTrigger } from './ui/popover'
import { Sidebar, SidebarContent, SidebarFooter, SidebarGroup, SidebarGroupLabel, SidebarHeader, SidebarInset, SidebarMenu, SidebarMenuButton, SidebarMenuItem, SidebarProvider, SidebarTrigger, useSidebar } from './ui/sidebar'
import { useTheme } from './ui/theme-provider'
import { Celular, Deitado, useConsulta } from './ui/use-mobile'

const Menu = [
  { grupo: 'Atendimento', funcao: 'assistente.usar', caminho: 'assistente', rotulo: 'Assistente', Icone: MessageCircle },
  { grupo: 'Atendimento', funcao: 'assistente.usar', caminho: 'solicitacoes', rotulo: 'Minhas solicitações', abrev: 'Solicitações', Icone: ListChecks },
  { grupo: 'Atendimento', funcao: 'fila.atender', caminho: 'fila', rotulo: 'Fila', Icone: Inbox },
  { grupo: 'Atendimento', funcao: 'chamados.ver_todos', caminho: 'chamados', rotulo: 'Chamados', Icone: ClipboardList },
  { grupo: 'Atendimento', funcao: 'indicadores.ver', caminho: 'indicadores', rotulo: 'Indicadores', Icone: ChartColumn },
  { grupo: 'Conhecimento', funcao: 'conhecimento.gerenciar', caminho: 'artigos', rotulo: 'Artigos', Icone: BookText },
  { grupo: 'Conhecimento', funcao: 'conhecimento.gerenciar', caminho: 'documentos', rotulo: 'Documentos', Icone: FileText },
  { grupo: 'Administração', funcao: 'usuarios.gerenciar', caminho: 'usuarios', rotulo: 'Usuários', Icone: Users },
  { grupo: 'Administração', funcao: 'tipos.gerenciar', caminho: 'tipos', rotulo: 'Tipos de usuário', Icone: UserCog },
  { grupo: 'Administração', funcao: 'matriculas.importar', caminho: 'matriculas', rotulo: 'Matrículas', Icone: Upload },
  { grupo: 'Administração', funcao: 'cadastros.gerenciar', caminho: 'cadastros', rotulo: 'Cadastros', Icone: Settings },
  { grupo: 'Administração', funcao: 'aparencia.gerenciar', caminho: 'aparencia', rotulo: 'Aparência', Icone: Palette },
  { grupo: 'Administração', funcao: 'integracoes.gerenciar', caminho: 'integracoes', rotulo: 'Integrações', Icone: Plug },
]
export const Grupos = ['Atendimento', 'Conhecimento', 'Administração']
const LimiteDeAbas = 5

export { iniciais }

export type Item = (typeof Menu)[number] & { abrev?: string }

export const itensDo = (permissoes: string[]): Item[] => Menu.filter((m) => permissoes.includes(m.funcao))

export function distribuir(permissoes: string[]) {
  const itens = itensDo(permissoes)
  const cabe = (n: number) => n + (itens.length > n ? 1 : 0) + 1 <= LimiteDeAbas
  let fixas = itens.filter((i) => i.grupo === 'Atendimento')
  while (!cabe(fixas.length)) fixas = fixas.slice(0, -1)
  return { fixas, gerir: itens.filter((i) => !fixas.includes(i)) }
}

export const destinosDaGerir = (permissoes: string[]) => distribuir(permissoes).gerir

const Temas: ['light' | 'dark' | 'black' | 'system', string, typeof Sun][] = [
  ['light', 'Claro', Sun],
  ['dark', 'Escuro', Moon],
  ['black', 'Preto', MoonStar],
  ['system', 'Sistema', Monitor],
]

export function OpcoesDeTema({ aoEscolher }: { aoEscolher: () => void }) {
  const { theme, setTheme } = useTheme()
  return (
    <>
      {Temas.map(([valor, rotulo, Icone]) => (
    <Button
      key={valor}
      variant="ghost"
      className="justify-start"
      aria-pressed={theme === valor}
      onClick={() => {
        setTheme(valor)
        aoEscolher()
      }}
    >
      <Icone aria-hidden="true" />
      {rotulo}
    </Button>
      ))}
    </>
  )
}

function Tema() {
  const { theme } = useTheme()
  const { isMobile } = useSidebar()
  const [aberto, setAberto] = useState(false)
  const Atual = Temas.find(([valor]) => valor === theme)![2]
  return (
    <SidebarMenuItem>
      <Popover open={aberto} onOpenChange={setAberto}>
        <PopoverTrigger asChild>
          <SidebarMenuButton tooltip="Tema">
            <Atual aria-hidden="true" />
            <span className="group-data-[collapsible=icon]:sr-only">Tema</span>
          </SidebarMenuButton>
        </PopoverTrigger>
        <PopoverContent side={isMobile ? 'top' : 'right'} align="end" className="flex w-40 flex-col p-1">
          <OpcoesDeTema aoEscolher={() => setAberto(false)} />
        </PopoverContent>
      </Popover>
    </SidebarMenuItem>
  )
}

export function tituloDaRota(pathname: string, numero?: string) {
  const caminho = pathname.split('/')[2]
  if (numero) return numero
  if (caminho === 'trocar-senha') return 'Trocar senha'
  if (caminho === 'conta') return 'Conta'
  if (caminho === 'gerir') return 'Gerir'
  return Menu.find((m) => m.caminho === caminho)?.rotulo ?? ''
}

function Link({ item: { caminho, rotulo, Icone }, slug }: { item: Item; slug?: string }) {
  const ativo = !!useMatch({ path: `/${slug}/${caminho}`, end: false })
  const { setOpenMobile } = useSidebar()
  return (
    <SidebarMenuItem>
      <SidebarMenuButton asChild isActive={ativo} tooltip={rotulo}>
        <NavLink to={`/${slug}/${caminho}`} onClick={() => setOpenMobile(false)}>
          <Icone aria-hidden="true" />
          <span className="group-data-[collapsible=icon]:sr-only">{rotulo}</span>
        </NavLink>
      </SidebarMenuButton>
    </SidebarMenuItem>
  )
}

function Grupo({ nome, itens, slug }: { nome: string; itens: Item[]; slug?: string }) {
  const id = useId()
  return (
    <SidebarGroup role="group" aria-labelledby={id}>
      <SidebarGroupLabel id={id} className="group-data-[collapsible=icon]:sr-only">
        {nome}
      </SidebarGroupLabel>
      <SidebarMenu>
        {itens.map((item) => (
          <Link key={item.caminho} item={item} slug={slug} />
        ))}
      </SidebarMenu>
    </SidebarGroup>
  )
}

function MenuUsuario({ me, sair }: { me: Me; sair: () => void }) {
  const { isMobile, state } = useSidebar()
  const dados = (
    <div className="grid min-w-0 flex-1 text-start text-sm leading-tight">
      <span className="truncate font-medium">{me.nome}</span>
      <span className="truncate text-xs text-muted-foreground">{me.tipo.nome}</span>
    </div>
  )
  return (
    <SidebarMenuItem>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <SidebarMenuButton size="lg" className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground">
            {state === 'collapsed' && !isMobile && (
              <span aria-hidden="true" className="flex size-8 shrink-0 items-center justify-center text-xs font-semibold">
                {iniciais(me.nome)}
              </span>
            )}
            <div className="grid min-w-0 flex-1 text-start text-sm leading-tight group-data-[collapsible=icon]:sr-only">
              <span className="truncate font-medium">{me.nome}</span>
              <span className="truncate text-xs text-muted-foreground">{me.tipo.nome}</span>
            </div>
          </SidebarMenuButton>
        </DropdownMenuTrigger>
        <DropdownMenuContent className="w-(--radix-dropdown-menu-trigger-width) min-w-56 rounded-lg" side={isMobile ? 'top' : 'right'} align="end" sideOffset={4}>
          <DropdownMenuLabel className="flex gap-2 p-1.5 font-normal">{dados}</DropdownMenuLabel>
          <DropdownMenuSeparator />
          <DropdownMenuItem onSelect={sair}>
            <LogOut aria-hidden="true" />
            Sair
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </SidebarMenuItem>
  )
}

function ContatoDoMenu({ contato }: { contato: InstituicaoPublica['contato'] }) {
  const [aberto, setAberto] = useState(false)
  return (
    <SidebarMenuItem>
      <SidebarMenuButton tooltip="Contato" onClick={() => setAberto(true)}>
        <Phone aria-hidden="true" />
        <span className="group-data-[collapsible=icon]:sr-only">Contato</span>
      </SidebarMenuButton>
      <DialogoDeContato contato={contato} aberto={aberto} aoMudar={setAberto} />
    </SidebarMenuItem>
  )
}

function CabecalhoDaBarra({ me, slug, ap }: { me: Me; slug?: string; ap: InstituicaoPublica }) {
  const placa = usePlaca()
  const logo = ap.imagens.logo
  return (
    <div className="flex items-center gap-2 p-1.5">
      {logo && slug && <img src={urlDaImagem(slug, 'logo', logo)} alt={ap.nome} width={160} height={32} className={`h-8 w-auto max-w-40 object-contain group-data-[collapsible=icon]:hidden ${placa}`} />}
      <span className={logo ? 'hidden group-data-[collapsible=icon]:flex' : 'flex'}>
        <Sigla slug={slug} icone={ap.imagens.icone} />
      </span>
      {!logo && (
        <div className="grid min-w-0 text-sm leading-tight">
          <span translate="no" className="font-semibold group-data-[collapsible=icon]:sr-only" data-testid="instituicao">
            {me.instituicao.nome}
          </span>
          <span translate="no" className="text-xs text-muted-foreground group-data-[collapsible=icon]:sr-only">Esclari</span>
        </div>
      )}
    </div>
  )
}

function Barra({ me, itens, slug, sair, ap }: { me: Me; itens: Item[]; slug?: string; sair: () => void; ap: InstituicaoPublica }) {
  return (
    <Sidebar collapsible="icon" variant="sidebar">
      <SidebarHeader className="border-b">
        <CabecalhoDaBarra me={me} slug={slug} ap={ap} />
      </SidebarHeader>
      <SidebarContent role="navigation">
        {Grupos.map((nome) => ({ nome, doGrupo: itens.filter((i) => i.grupo === nome) }))
          .filter(({ doGrupo }) => doGrupo.length > 0)
          .map(({ nome, doGrupo }) => (
            <Grupo key={nome} nome={nome} itens={doGrupo} slug={slug} />
          ))}
      </SidebarContent>
      <SidebarFooter>
        <SidebarMenu>
          {temContato(ap.contato) && <ContatoDoMenu contato={ap.contato} />}
          <Tema />
          <MenuUsuario me={me} sair={sair} />
        </SidebarMenu>
        <SidebarTrigger className="self-end max-md:hidden pointer-coarse:size-11 group-data-[collapsible=icon]:self-center" />
      </SidebarFooter>
    </Sidebar>
  )
}

function Topo({ titulo, aluno, abas, slug, icone, nome, voltar }: { titulo: string; aluno: boolean; abas: boolean; slug?: string; icone: number | null; nome: string; voltar?: () => void }) {
  const { isMobile, toggleSidebar } = useSidebar()
  return (
    <header className="sticky top-0 z-10 flex h-[calc(49px+env(safe-area-inset-top))] shrink-0 items-center gap-2 border-b bg-background pt-[env(safe-area-inset-top)]">
      <div className={`flex w-full min-w-0 items-center px-4 lg:px-6 ${abas ? 'gap-2' : 'gap-1 lg:gap-2'}`}>
        {!abas && isMobile && (
          <Button variant="ghost" size="icon" className="-ms-1" onClick={toggleSidebar}>
            <IconeMenu aria-hidden="true" />
            <span className="sr-only">Alternar barra lateral</span>
          </Button>
        )}
        {voltar && (
          <Button variant="ghost" size="icon" className="-ms-1" onClick={voltar}>
            <ChevronLeft aria-hidden="true" />
            <span className="sr-only">Voltar</span>
          </Button>
        )}
        {!voltar && <Sigla slug={slug} icone={icone} className="size-6 md:hidden" />}        <h1 translate={/^ESC-\d+$/.test(titulo) ? 'no' : undefined} className="truncate text-base font-medium">{titulo}</h1>
        <div className="ms-auto flex shrink-0 items-center gap-1">{aluno && <AvisosPush />}</div>
      </div>
    </header>
  )
}

type Aba = { caminho: string; rotulo: string; Icone: typeof Sun; ativas: string[] }

function abasDe(permissoes: string[]): Aba[] {
  const { fixas, gerir } = distribuir(permissoes)
  const doMenu = fixas.map(({ caminho, rotulo, abrev, Icone }) => ({ caminho, rotulo: abrev ?? rotulo, Icone, ativas: caminho === 'solicitacoes' ? [caminho, 'chamados'] : [caminho] }))
  const deGerir = gerir.length > 0 ? [{ caminho: 'gerir', rotulo: 'Gerir', Icone: LayoutGrid, ativas: ['gerir', ...gerir.map((g) => g.caminho)] }] : []
  return [...doMenu, ...deGerir, { caminho: 'conta', rotulo: 'Conta', Icone: CircleUser, ativas: ['conta'] }]
}

const AlturaDaBarra = 'calc(3.5rem + 1px + env(safe-area-inset-bottom))'
const AlturaDaBarraDeitada = 'calc(3rem + 1px + env(safe-area-inset-bottom))'

function BarraDeAbas({ slug, permissoes, deitado }: { slug?: string; permissoes: string[]; deitado: boolean }) {
  const segmento = useLocation().pathname.split('/')[2]
  const abas = abasDe(permissoes)
  return (
    <nav aria-label="Navegação" style={{ gridTemplateColumns: `repeat(${abas.length}, minmax(0, 1fr))` }} className="fixed inset-x-0 bottom-0 z-20 grid border-t bg-background pb-[env(safe-area-inset-bottom)]">
      {abas.map(({ caminho, rotulo, Icone, ativas }) => {
        const ativo = ativas.includes(segmento)
        return (
          <RouterLink
            key={caminho}
            to={`/${slug}/${caminho}`}
            aria-current={ativo ? 'page' : undefined}
            className={`flex items-center justify-center focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring ${deitado ? 'h-12 flex-row gap-2 text-sm' : 'h-14 flex-col gap-0.5 text-xs'} ${ativo ? 'font-semibold text-primary' : 'text-muted-foreground'}`}
          >
            <Icone aria-hidden="true" className="size-5" />
            <span>{rotulo}</span>
          </RouterLink>
        )
      })}
    </nav>
  )
}

function PularParaOConteudo() {
  return (
    <a
      href="#conteudo"
      onClick={(e) => {
        e.preventDefault()
        document.getElementById('conteudo')?.focus()
      }}
      className="sr-only focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:z-50 focus:rounded-sm focus:bg-background focus:px-4 focus:py-3 focus:text-sm focus:font-medium focus:shadow-md focus:ring-2 focus:ring-ring"
    >
      Pular para o conteúdo
    </a>
  )
}

const rotaPermitida = (permissoes: string[], caminho: string) => {
  if (caminho === 'gerir') return destinosDaGerir(permissoes).length > 0
  const item = Menu.find((m) => m.caminho === caminho)
  return !item || permissoes.includes(item.funcao)
}

function useBarra() {
  const [aberta, setAberta] = useState(() => localStorage.getItem('esclari-barra') !== 'recolhida')
  return {
    aberta,
    aoMudar(valor: boolean) {
      localStorage.setItem('esclari-barra', valor ? 'aberta' : 'recolhida')
      setAberta(valor)
    },
  }
}

const semSessao = (erro: Error | null) => erro instanceof ErroApi && erro.status === 401

const textoDaSessao = (status: number) => {
  if (status === 403) return 'Você não tem permissão para esta ação.'
  if (status === 429) return 'Muitas tentativas. Aguarde um minuto e tente de novo.'
  return 'O serviço está indisponível no momento.'
}

function ErroDaSessao({ erro, tentar }: { erro: Error; tentar: () => void }) {
  if (!(erro instanceof ErroApi) || (erro.status !== 403 && erro.status !== 429 && erro.status < 500)) return <ErroDe erro={erro} oque="sua sessão" tentar={tentar} />
  return (
    <Alert variant="destructive">
      <CircleAlert aria-hidden="true" />
      <div className="flex flex-col items-start gap-2">
        <p className="m-0">Não foi possível carregar sua sessão. {textoDaSessao(erro.status)}</p>
        {erro.status !== 403 && (
          <Button variant="outline" size="sm" onClick={tentar}>
            Tentar de novo
          </Button>
        )}
      </div>
    </Alert>
  )
}

function podeAbrir(permissoes: string[], pathname: string, inicial: string) {
  const [, , caminho, numero] = pathname.split('/')
  return (caminho === 'chamados' && !!numero) || rotaPermitida(permissoes, caminho) || caminho === inicial
}

function desvio({ me, erro, slug, pathname }: { me?: Me; erro: Error | null; slug?: string; pathname: string }) {
  if (semSessao(erro)) return `/${slug}`
  if (!me) return undefined
  if (me.instituicao.slug !== slug) return `/${me.instituicao.slug}`
  if (me.deveTrocarSenha && !pathname.endsWith('/trocar-senha')) return `/${slug}/trocar-senha`
  const inicial = telaInicial(me)
  return podeAbrir(me.permissoes, pathname, inicial) ? undefined : `/${slug}/${inicial}`
}

export function Autenticado() {
  const { slug } = useParams()
  const { pathname } = useLocation()
  const cliente = useQueryClient()
  const { data: me, error, refetch } = useQuery({ queryKey: ['me'], queryFn: () => api<Me>('/api/auth/me') })
  const { data: instituicao } = useInstituicao(slug)
  useTempoReal(cliente, !!me && !me.deveTrocarSenha && me.instituicao.slug === slug)

  const destino = desvio({ me, erro: error, slug, pathname })
  if (destino) return <Navigate to={destino} replace />
  if (error)
    return (
      <div className="mx-auto flex min-h-svh max-w-sm items-center p-6">
        <ErroDaSessao erro={error} tentar={() => void refetch()} />
      </div>
    )
  if (!me)
    return (
      <Carregando className="flex justify-center py-12" />
    )
  return <Casca me={me} slug={slug} ap={instituicao ?? normalizar({ nome: me.instituicao.nome })} />
}

function perfilDoModo(me: Me) {
  return {
    soAluno: me.permissoes.length === 1 && me.permissoes[0] === 'assistente.usar',
    equipe: me.permissoes.some((p) => p === 'fila.atender' || p === 'chamados.ver_todos'),
    lista: me.permissoes.includes('fila.atender') ? 'fila' : 'solicitacoes',
  }
}

function useVoltar(slug: string | undefined, lista: string) {
  const { key } = useLocation()
  const navegar = useNavigate()
  return () => (key === 'default' ? navegar(`/${slug}/${lista}`, { replace: true }) : navegar(-1))
}

function useModo(me: Me, slug?: string) {
  const { pathname } = useLocation()
  const celular = useConsulta(Celular)
  const deitado = useConsulta(Deitado)
  const { soAluno, equipe, lista } = perfilDoModo(me)
  const voltarAoChamado = useVoltar(slug, lista)
  const noChamado = /^\/[^/]+\/chamados\/[^/]+/.test(pathname)
  const abas = celular && (soAluno || !me.deveTrocarSenha)
  return {
    abas,
    deitado,
    comBarra: abas && !(noChamado && equipe),
    voltar: celular && noChamado ? voltarAoChamado : undefined,
  }
}

function useFocoNaRota(pathname: string) {
  const anterior = useRef(pathname)
  useEffect(() => {
    if (anterior.current === pathname) return
    anterior.current = pathname
    document.getElementById('conteudo')?.focus({ preventScroll: true })
  }, [pathname])
}

function useAlturaDaBarra(altura?: string) {
  useLayoutEffect(() => {
    if (!altura) return
    const raiz = document.documentElement.style
    raiz.setProperty('--barra', altura)
    return () => void raiz.removeProperty('--barra')
  }, [altura])
}

function Casca({ me, slug, ap }: { me: Me; slug?: string; ap: InstituicaoPublica }) {
  const { numero } = useParams()
  const { pathname } = useLocation()
  const titulo = tituloDaRota(pathname, numero)
  useRota(titulo, ap)
  useFocoNaRota(pathname)
  const cliente = useQueryClient()
  const barra = useBarra()
  const sair = useSair()
  const { abas, comBarra, deitado, voltar } = useModo(me, slug)
  const setMe = (novo: Me) => cliente.setQueryData(['me'], novo)
  const altura = deitado ? AlturaDaBarraDeitada : AlturaDaBarra
  useAlturaDaBarra(comBarra ? altura : undefined)

  return (
    <SidebarProvider open={barra.aberta} onOpenChange={barra.aoMudar}>
      <PularParaOConteudo />
      {!abas && <Barra me={me} itens={itensDo(me.permissoes)} slug={slug} sair={sair} ap={ap} />}
      <SidebarInset id="conteudo" tabIndex={-1} className="min-w-0 outline-none">
        <Topo titulo={titulo} aluno={me.permissoes.includes('assistente.usar')} abas={abas} slug={slug} icone={ap.imagens.icone} nome={me.instituicao.nome} voltar={voltar} />
        <div className={`@container/main flex-1 px-4 py-4 md:py-6 lg:px-6 ${comBarra ? 'pb-[calc(var(--barra)+1rem)]' : ''}`}>
          <Outlet context={{ me, setMe } satisfies Sessao} />
        </div>
      </SidebarInset>
      {comBarra && <BarraDeAbas slug={slug} permissoes={me.permissoes} deitado={deitado} />}
    </SidebarProvider>
  )
}
