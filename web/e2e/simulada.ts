import type { Page } from '@playwright/test'

const TituloLongo = 'Boleto'.repeat(20)
const EmailLongo = 'e'.repeat(46) + '@univag.edu.br'

const longo = 'Preciso da segunda via do boleto de novembro, que veio com o valor da rematrícula do semestre anterior'

export const chamadoSimulado = (estado = 'EM_ATENDIMENTO') => ({
  numero: 'ESC-000123', titulo: longo, estado, motivoFechamento: null, setor: 'Coordenação de Análise e Desenvolvimento de Sistemas',
  categoria: 'Financeiro > Boletos e mensalidades', responsavel: 'Carla Regina de Souza', criadoEm: '2026-11-02T12:00:00Z', resolvidoEm: null,
  matricula: { numero: '2000567890', curso: 'Análise e Desenvolvimento de Sistemas', semestre: 3 }, solicitante: 'Ana Beatriz', origemCategoria: 'IA',
  resumo: 'Aluna pede a segunda via do boleto de novembro com valor corrigido.',
  transcricao: [
    { autor: 'ALUNO', texto: longo, criadaEm: '2026-11-02T12:00:00Z' },
    { autor: 'ASSISTENTE', texto: 'O boleto fica em https://portal.univag.edu.br/financeiro/boletos/segunda-via', criadaEm: '2026-11-02T12:00:05Z',
      fontes: [{ versaoArtigoId: 1, titulo: 'Como emitir a segunda via do boleto do semestre', trechos: ['https://portal.univag.edu.br/financeiro/boletos/segunda-via'], atualizado: true }] },
  ],
  mensagens: [{ id: 1, tipo: 'NOTA', texto: 'Conferir com a tesouraria antes de responder.', autor: 'Carla Regina de Souza', doAluno: false, criadaEm: '2026-11-02T12:10:00Z' }],
  eventos: [{ tipo: 'TRANSICAO', ator: 'Carla Regina de Souza', doAluno: false, de: 'AGUARDANDO_ALUNO', para: 'EM_ATENDIMENTO', detalhe: null, criadoEm: '2026-11-02T12:10:00Z' }],
  acoes: ['responder', 'nota', 'resolver', 'assumir', 'confirmar', 'encaminhar', 'cancelar', 'reclassificar'],
})

export const semAparencia = {
  nomeCurto: null, cor: null, login: { titulo: null, frase: null, itens: [] }, assistente: { nome: null, boasVindas: null, perguntas: [] },
  contato: { horario: null, telefone: null, email: null }, imagens: { logo: null, icone: null, login: null, assistente: null },
}
export const comAparencia = {
  nomeCurto: 'Univag', cor: '#8a1538', login: { titulo: 'Fale com a Univag', frase: 'Aqui você resolve tudo sobre a sua vida acadêmica.', itens: ['Boletos', 'Matrícula', 'Documentos'] },
  assistente: { nome: 'Vagner', boasVindas: 'Oi! Posso ajudar com boletos e matrícula.', perguntas: ['Como pago a mensalidade?', 'Quando abre a rematrícula?', 'Onde vejo minhas notas?', 'Como peço o histórico?'] },
  contato: { horario: 'Seg a sex, 8h às 18h', telefone: '(65) 3688-6000', email: 'atendimento@univag.edu.br' }, imagens: { logo: 3, icone: 2, login: 1, assistente: 4 },
}
const Png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGNgYGD4DwABBAEAwS2OUAAAAABJRU5ErkJggg==', 'base64')

export const InstituicoesSimuladas = [
  { slug: 'univag', nome: 'Centro Universitário de Várzea Grande', fuso: 'America/Cuiaba', ativa: true, criadaEm: '2026-11-02T12:00:00Z', administradores: 2, situacao: 'ATIVA', saude: { ultimaAtividadeEm: '2026-11-16T12:00:00Z', semBaseTaxa30Dias: 0.1 }, atencao: [] },
  { slug: 'faculdade-antiga', nome: 'Faculdade Antiga', fuso: 'America/Sao_Paulo', ativa: false, criadaEm: '2026-09-15T12:00:00Z', administradores: 0, situacao: 'DESATIVADA', saude: { ultimaAtividadeEm: null, semBaseTaxa30Dias: null }, atencao: [] },
]

const FuncoesDoPapel: Record<string, string[]> = {
  ALUNO: ['assistente.usar'],
  ATENDENTE: ['fila.atender', 'indicadores.ver', 'mcp.usar'],
  ADMINISTRADOR: ['chamados.ver_todos', 'indicadores.ver', 'conhecimento.gerenciar', 'cadastros.gerenciar', 'usuarios.gerenciar', 'matriculas.importar', 'tipos.gerenciar', 'aparencia.gerenciar', 'integracoes.gerenciar', 'mcp.usar'],
}
const NomeDoPapel: [string, string][] = [
  ['ADMINISTRADOR', 'Gestor'],
  ['ATENDENTE', 'Atendente'],
  ['ALUNO', 'Aluno'],
]

export const comoUsuario = (papeis: string[]) => ({
  tipo: { id: 1, nome: NomeDoPapel.find(([p]) => papeis.includes(p))![1] },
  permissoes: [...new Set(papeis.flatMap((p) => FuncoesDoPapel[p]))].sort(),
})

export const TiposSimulados = {
  tipos: [
    { id: 3, nome: 'Aluno', sistema: 'ALUNO', funcoes: FuncoesDoPapel.ALUNO, usuarios: 120 },
    { id: 1, nome: 'Gestor', sistema: 'GESTOR', funcoes: FuncoesDoPapel.ADMINISTRADOR, usuarios: 2 },
    { id: 2, nome: 'Atendente', sistema: 'ATENDENTE', funcoes: FuncoesDoPapel.ATENDENTE, usuarios: 8 },
    { id: 4, nome: 'Coordenador de curso', sistema: null, funcoes: ['indicadores.ver', 'chamados.ver_todos', 'usuarios.gerenciar'], usuarios: 3 },
  ],
  catalogo: [
    { codigo: 'assistente.usar', rotulo: 'Usar o assistente', descricao: 'Conversar com o assistente, pedir atendimento direto e acompanhar as próprias solicitações. Exige matrícula ativa.', expoeDadosPessoais: false, deAluno: true },
    { codigo: 'fila.atender', rotulo: 'Atender a fila', descricao: 'Ver e atender os chamados dos setores em que a pessoa está vinculada. Exige ao menos um setor.', expoeDadosPessoais: true, deAluno: false },
    { codigo: 'chamados.ver_todos', rotulo: 'Ver os chamados de todos os setores', descricao: 'Ler os chamados de qualquer setor, exportar a lista e receber o aviso de chamados atrasados.', expoeDadosPessoais: true, deAluno: false },
    { codigo: 'indicadores.ver', rotulo: 'Ver os indicadores', descricao: 'Abrir os indicadores de atendimento dos setores a que a pessoa tem acesso.', expoeDadosPessoais: false, deAluno: false },
    { codigo: 'conhecimento.gerenciar', rotulo: 'Gerenciar a base de conhecimento', descricao: 'Criar e editar artigos e documentos e receber o aviso de conteúdo perto de vencer.', expoeDadosPessoais: false, deAluno: false },
    { codigo: 'cadastros.gerenciar', rotulo: 'Gerenciar cadastros', descricao: 'Manter setores, categorias, cursos, respostas prontas e fluxos de atendimento.', expoeDadosPessoais: false, deAluno: false },
    { codigo: 'usuarios.gerenciar', rotulo: 'Gerenciar usuários', descricao: 'Criar, editar e desativar usuários, ver o CPF deles e escolher o tipo de cada um.', expoeDadosPessoais: true, deAluno: false },
    { codigo: 'matriculas.importar', rotulo: 'Importar matrículas', descricao: 'Enviar o arquivo CSV de matrículas, que traz CPF, nome e e-mail dos alunos.', expoeDadosPessoais: true, deAluno: false },
    { codigo: 'tipos.gerenciar', rotulo: 'Gerenciar tipos de usuário', descricao: 'Criar, editar e apagar tipos de usuário e escolher as funções de cada um.', expoeDadosPessoais: false, deAluno: false },
    { codigo: 'aparencia.gerenciar', rotulo: 'Gerenciar a aparência', descricao: 'Mudar o nome, as cores e as imagens da instituição.', expoeDadosPessoais: false, deAluno: false },
    { codigo: 'integracoes.gerenciar', rotulo: 'Gerenciar integrações', descricao: 'Configurar o Moodle e criar ou revogar tokens de acesso ao MCP.', expoeDadosPessoais: false, deAluno: false },
    { codigo: 'mcp.usar', rotulo: 'Usar o acesso por MCP', descricao: 'Consultar a instituição por um token MCP, com os mesmos dados que a pessoa vê no sistema.', expoeDadosPessoais: true, deAluno: false },
  ],
}

export async function trocaObrigatoria(page: Page, papeis: string[]) {
  await page.route('**/api/auth/me', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: 1,
        nome: 'Ana',
        cpfMascarado: '***.456.789-**',
        ...comoUsuario(papeis),
        deveTrocarSenha: true,
        instituicao: { slug: 'univag', nome: 'Centro Universitário de Várzea Grande', fuso: 'America/Cuiaba' },
        matriculas: [],
      }),
    }),
  )
}

export const InstituicoesDaBusca = [
  { slug: 'univag', nome: 'Centro Universitário de Várzea Grande', nomeCurto: 'Univag' },
  { slug: 'unic', nome: 'Universidade de Cuiabá', nomeCurto: 'Unic' },
  { slug: 'unemat', nome: 'Universidade do Estado de Mato Grosso', nomeCurto: 'Unemat' },
  { slug: 'ufmt', nome: 'Universidade Federal de Mato Grosso', nomeCurto: null },
  { slug: 'unirondon', nome: 'Centro Universitário Cândido Rondon', nomeCurto: 'Unirondon' },
]

export async function simularApi(page: Page, papeis = ['ALUNO', 'ATENDENTE', 'ADMINISTRADOR'], extremos = false, aparencia: object = semAparencia) {
  const mais = <T,>(linhas: T[], extra: T) => (extremos ? [...linhas, extra] : linhas)
  await page.route('**/api/**', (rota) => {
    const caminho = new URL(rota.request().url()).pathname
    const json = (status: number, corpo: unknown) => rota.fulfill({ status, contentType: 'application/json', body: JSON.stringify(corpo) })
    if (caminho === '/api/instituicoes') return json(200, { itens: InstituicoesDaBusca })
    if (caminho === '/api/instituicoes/univag') return json(200, { nome: 'Centro Universitário de Várzea Grande', ...aparencia })
    if (/^\/api\/instituicoes\/univag\/imagens\/\w+$/.test(caminho)) return rota.fulfill({ status: 200, contentType: 'image/png', body: Png })
    if (caminho === '/api/admin/aparencia') return json(200, aparencia)
    if (caminho.startsWith('/api/admin/aparencia/imagens/')) return json(200, { tipo: caminho.split('/').pop(), versao: 4 })
    if (caminho === '/api/plataforma/eu') return json(200, { id: 1, nome: 'Marina Souza', email: 'marina@esclari.com.br' })
    if (caminho === '/api/auth/me')
      return json(200, {
        id: 1,
        nome: 'Ana',
        cpfMascarado: '***.456.789-**',
        ...comoUsuario(papeis),
        deveTrocarSenha: false,
        instituicao: { slug: 'univag', nome: 'Centro Universitário de Várzea Grande', fuso: 'America/Cuiaba' },
        matriculas: [],
      })
    if (caminho === '/api/conversas')
      return json(201, { conversaId: 1, resposta: { tipo: 'resposta', texto: 'O boleto do semestre fica no Portal do Aluno: https://portal.univag.edu.br/financeiro/boletos/segunda-via' } })
    if (caminho === '/api/chamados/busca')
      {
      const fila = { numero: 'ESC-000123', titulo: longo, setor: 'Coordenação de Análise e Desenvolvimento de Sistemas', categoria: 'Financeiro > Boletos e mensalidades', estado: 'AGUARDANDO_ALUNO', responsavel: 'Carla Regina de Souza', criadoEm: '2026-11-02T12:00:00Z', dias: 12, atraso: null }
      const itens = mais([fila], { ...fila, numero: 'ESC-000124', titulo: TituloLongo })
      return json(200, { itens, total: itens.length, pagina: 1, porPagina: 50 })
    }
    if (caminho === '/api/chamados/filtros') return json(200, { setores: [{ id: 1, nome: 'Coordenação de Análise e Desenvolvimento de Sistemas' }], categorias: [{ id: 1, nome: 'Financeiro > Boletos e mensalidades' }] })
    if (caminho === '/api/chamados' && rota.request().url().includes('escopo=fila'))
      {
      const fila = { numero: 'ESC-000123', titulo: longo, categoria: 'Financeiro > Boletos e mensalidades', estado: 'AGUARDANDO_ALUNO', responsavel: 'Carla Regina de Souza', criadoEm: '2026-11-02T12:00:00Z', dias: 12 }
      return json(200, mais([fila], { ...fila, numero: 'ESC-000124', titulo: TituloLongo }))
    }
    if (caminho === '/api/chamados') return json(200, [{ numero: 'ESC-000123', titulo: longo, estado: 'EM_ATENDIMENTO', criadoEm: '2026-11-02T12:00:00Z' }])
    if (caminho === '/api/chamados/ESC-000123') return json(200, chamadoSimulado())
    if (caminho === '/api/admin/tipos') return json(200, TiposSimulados)
    if (caminho === '/api/admin/usuarios')
      {
      const usuario = { id: 1, cpf: '12345678909', nome: 'Carla Regina de Souza Albuquerque', email: 'carla.regina.albuquerque@univag.edu.br', ativo: true, deveTrocarSenha: false, tipo: { id: 1, nome: 'Gestor' }, setores: [1] }
      return json(200, mais([usuario], { ...usuario, id: 2, nome: 'Bruno', email: EmailLongo }))
    }
    if (caminho === '/api/admin/setores') return json(200, [{ id: 1, nome: 'Coordenação de Análise e Desenvolvimento de Sistemas', triagem: false, ativo: true }])
    if (['/api/admin/cursos', '/api/admin/fluxos', '/api/admin/respostas-prontas'].includes(caminho)) return json(200, [])
    if (caminho.startsWith('/api/admin/fluxos/')) return json(200, { numero: 1, etapas: [] })
    if (caminho === '/api/chamados/ESC-000123/subir') return json(200, chamadoSimulado())
    if (/^\/api\/conversas\/\d+\/destino$/.test(caminho)) return json(200, { lideranca: false })
    if (caminho === '/api/admin/categorias') return json(200, [{ id: 1, nome: 'Financeiro > Boletos e mensalidades', ativa: true }])
    if (caminho === '/api/admin/artigos')
{
      const artigo = { id: 1, estado: 'ARQUIVADO', indexacao: 'INDEXADO', categoriaId: 1, categoria: 'Financeiro > Boletos e mensalidades',
        versao: { id: 1, numero: 12, titulo: 'Como emitir a segunda via do boleto do semestre com o valor corrigido', perguntas: ['boleto?'], resposta: 'x', links: [] } }
      return json(200, mais([artigo], { ...artigo, id: 2, versao: { ...artigo.versao, id: 2, titulo: TituloLongo } }))
    }
    if (caminho === '/api/conhecimento/validade') return json(200, { artigos: { vigentes: 120, venceEm30Dias: 14, vencidos: 3 }, documentos: { vigentes: 40, venceEm30Dias: 2, vencidos: 1 } })
    if (caminho === '/api/indicadores/detalhe')
      return json(200, {
        granularidade: 'dia',
        pontos: Array.from({ length: 30 }, (_, i) => ({ inicio: `2026-11-${String(i + 1).padStart(2, '0')}`, conversas: 20 + ((i * 7) % 23), chamadosAbertos: 5 + ((i * 3) % 9), chamadosResolvidos: 4 + ((i * 5) % 9) })),
        desfechos: { resolvidas: 3700, transferidas: 1200, abandonadas: 400, encerradas: 300, semDesfecho: 78 },
        chamadosPorEstado: { ABERTO: 41, EM_ATENDIMENTO: 180, AGUARDANDO_ALUNO: 70, RESOLVIDO: 30 },
        maisAntigoDias: 17,
        tempos: { primeiraRespostaMedianaHoras: 3.5, primeiraRespostaP90Horas: 28.25, resolucaoMedianaHoras: 40.5, resolucaoP90Horas: 190.75 },
        prazos: { primeiraRespostaMetaHoras: 24, resolucaoMetaHoras: 120, primeiraRespostaNoPrazo: 0.8234, resolucaoNoPrazo: 0.7412, emAtrasoAgora: 3, maisAntigoEmAtrasoDias: 5 },
      })
    if (caminho === '/api/indicadores')
      return json(200, {
        semDados: false, alunosAtivos: 1234, conversas: 5678, taxaAutoatendimento: 0.6543, taxaEscalonamento: 0.2, taxaAbandono: 0.1457, taxaPerguntasSemArtigo: 0.12,
        chamadosAbertos: 321, chamadosResolvidos: 1200, tempoPrimeiraRespostaHoras: 12.75, tempoResolucaoHoras: 123.25, roteamentoCorreto: 0.91, taxaReabertura: 0.05,
        principaisCategorias: { porConversas: [{ categoria: extremos ? 'Financeiro > Boletos e mensalidades 2026' : 'Financeiro > Boletos e mensalidades', total: 1000 }], porChamados: [{ categoria: 'Acadêmico > Notas e frequência', total: 300 }] },
        indicadorPrincipal: 0.7123,
      })
    if (caminho === '/api/admin/documentos')
{
      const documento = { id: 1, titulo: 'Regulamento do estágio supervisionado obrigatório dos cursos de graduação', categoriaId: 1, categoria: 'Financeiro > Boletos e mensalidades', estado: 'ATIVO', indexacao: 'INDEXADO',
        versao: { id: 1, numero: 3, nomeArquivo: 'regulamento-do-estagio-supervisionado-obrigatorio-2026.pdf', paginas: 42, criadaEm: '2026-11-02T12:00:00Z' } }
      return json(200, mais([documento], { ...documento, id: 2, titulo: TituloLongo }))
    }
    if (caminho === '/api/plataforma/instituicoes') return json(rota.request().method() === 'GET' ? 200 : 201, rota.request().method() === 'GET' ? InstituicoesSimuladas : InstituicoesSimuladas[0])
    if (caminho.startsWith('/api/plataforma/instituicoes/')) return json(200, { slug: caminho.split('/')[4], ativa: true, enviados: 2, setores: 2, cursos: 1, categorias: 3, regras: 2, artigos: 5 })
    return json(404, { status: 404, detail: 'Recurso não encontrado.' })
  })
}

export const Telas: [string, string, (p: Page) => Promise<void>][] = [
  ['Login', '/univag', async (p) => void (await p.getByLabel('CPF').waitFor())],
  [
    'Assistente',
    '/univag/assistente',
    async (p) => {
      await p.getByLabel('Mensagem').fill('preciso do boleto do semestre, onde encontro a segunda via?')
      await p.getByRole('button', { name: /Enviar/ }).click()
      await p.getByText('Isso resolveu?').waitFor()
    },
  ],
  ['Minhas solicitações', '/univag/solicitacoes', async (p) => void (await p.getByText('ESC-000123').waitFor())],
  ['Fila', '/univag/fila', async (p) => void (await p.getByText('ESC-000123').waitFor())],
  ['Detalhe do chamado', '/univag/chamados/ESC-000123', async (p) => void (await p.getByText('Resumo gerado por IA').waitFor())],
  ['Artigos', '/univag/artigos', async (p) => void (await p.getByText('ARQUIVADO').first().waitFor())],
  ['Indicadores', '/univag/indicadores', async (p) => void (await p.getByText('Indicador principal').waitFor())],
  ['Usuários', '/univag/usuarios', async (p) => void (await p.getByText('Carla Regina de Souza Albuquerque').waitFor())],
  [
    'Matrículas',
    '/univag/matriculas',
    async (p) => {
      await p.route('**/api/admin/matriculas/importacao', (r) =>
        r.fulfill({ status: 400, contentType: 'application/json', body: JSON.stringify({ status: 400, detail: 'Há erros no arquivo.', erros: [{ linha: 2, motivo: 'O CPF informado nesta linha não confere com nenhum aluno cadastrado' }] }) }),
      )
      await p.locator('input[type=file]').setInputFiles({ name: 'matriculas.csv', mimeType: 'text/csv', buffer: Buffer.from('cpf;nome') })
      await p.getByRole('button', { name: 'Importar' }).click()
      await p.getByRole('dialog').getByRole('button', { name: 'Importar' }).click()
      await p.getByText(/não confere/).waitFor()
    },
  ],
  ['Cadastros', '/univag/cadastros', async (p) => void (await p.getByText('Coordenação de Análise e Desenvolvimento de Sistemas').waitFor())],
  [
    'Raiz',
    '/',
    async (p) => {
      await p.evaluate(() => localStorage.removeItem('esclari-instituicao'))
      await p.goto('/')
      await p.getByLabel('Sua instituição').waitFor()
    },
  ],
  ['Aparência', '/univag/aparencia', async (p) => void (await p.getByLabel('Cor principal').waitFor())],
  ['Primeiro acesso', '/univag/primeiro-acesso', async (p) => void (await p.getByLabel('CPF').waitFor())],
  ['Criar senha', '/univag/definir-senha?token=x', async (p) => void (await p.getByLabel('Nova senha').waitFor())],
  ['Trocar senha', '/univag/trocar-senha', async (p) => void (await p.getByLabel('Senha atual').waitFor())],
  ['Documentos', '/univag/documentos', async (p) => void (await p.getByText('Regulamento do estágio supervisionado obrigatório dos cursos de graduação').waitFor())],
]

declare global {
  interface Window {
    __ux: { pintar: (pilha: string[]) => number[]; pilha: (el: Element) => string[] }
  }
}

export async function instalar(page: Page) {
  await page.addInitScript(() => {
    const ctx = document.createElement('canvas').getContext('2d', { willReadFrequently: true })!
    const pintar = (pilha: string[]) => {
      ctx.clearRect(0, 0, 300, 150)
      for (const cor of pilha) {
        ctx.fillStyle = cor
        ctx.fillRect(0, 0, 300, 150)
      }
      return [...ctx.getImageData(0, 0, 1, 1).data].slice(0, 3)
    }
    window.__ux = {
      pintar,
      pilha(el) {
        const camadas: string[] = []
        for (let n: Element | null = el; n; n = n.parentElement) {
          const cor = getComputedStyle(n).backgroundColor
          const [sobrePreto, sobreBranco] = [pintar(['#000', cor]), pintar(['#fff', cor])]
          if (sobrePreto.join() === '0,0,0' && sobreBranco.join() === '255,255,255') continue
          camadas.unshift(cor)
          if (sobrePreto.join() === sobreBranco.join()) return camadas
        }
        return ['#fff', ...camadas]
      },
    }
  })
}

export async function referencia(page: Page, token: string) {
  return page.evaluate((t) => {
    const ref = document.createElement('div')
    ref.style.color = `var(${t})`
    document.body.append(ref)
    const cor = getComputedStyle(ref).color
    ref.remove()
    return window.__ux.pintar(['#fff', cor])
  }, token)
}

export async function abrirTela(page: Page, nome: string) {
  const [, caminho, esperar] = Telas.find(([n]) => n === nome)!
  await page.goto(caminho)
  await esperar(page)
}

export async function varrer(page: Page, aoAbrir: (nome: string) => Promise<void>, aparencia: object = semAparencia) {
  const telas: [string, () => Promise<void>][] = [
    ...Telas.map(([nome]) => [nome, () => abrirTela(page, nome)] as [string, () => Promise<void>]),
    ['Conta', async () => { await page.goto('/univag/conta'); await page.getByText('***.456.789-**').waitFor() }],
    ['Instituições', async () => { await page.goto('/plataforma/instituicoes'); await page.getByText('Faculdade Antiga').waitFor() }],
  ]
  for (const [nome, abrirDaTela] of telas) {
    await page.unroute('**/api/**')
    await simularApi(page, nome === 'Instituições' ? ['ADMINISTRADOR'] : nome === 'Conta' ? ['ALUNO'] : undefined, false, aparencia)
    await abrirDaTela()
    await aoAbrir(nome)
  }
}
