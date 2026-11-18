import { cleanup, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { abrir, acoesDe, api, cabecalhos, comTexto, escolher, type Rota } from '../test/util'

const operador = { id: 1, nome: 'Marina Souza', email: 'marina@esclari.com.br' }
const saude = { ultimaAtividadeEm: '2026-11-16T12:00:00Z', semBaseTaxa30Dias: 0.125 }
const demo = { slug: 'demo', nome: 'Faculdade Demo', fuso: 'America/Cuiaba', ativa: true, criadaEm: '2026-11-02T12:00:00Z', administradores: 2, situacao: 'ATIVA', saude, atencao: [] as string[] }
const antiga = { slug: 'antiga', nome: 'Faculdade Antiga', fuso: 'America/Sao_Paulo', ativa: false, criadaEm: '2026-09-15T12:00:00Z', administradores: 0, situacao: 'DESATIVADA', saude: { ultimaAtividadeEm: null, semBaseTaxa30Dias: null }, atencao: [] as string[] }
const Lista = '/api/plataforma/instituicoes'

function tela(rotas: Record<string, Rota> = {}, lista: unknown[] = [demo, antiga]) {
  const chamadas = api({ 'GET /api/plataforma/eu': () => [200, operador], [`GET ${Lista}`]: () => [200, lista], ...rotas })
  abrir('/plataforma/instituicoes')
  return chamadas
}

const linha = async (nome: string) => (await screen.findByText(nome)).closest('tr') as HTMLElement
const gravacoes = (chamadas: { metodo: string; url: string; corpo: any }[]) => chamadas.filter((c) => c.metodo !== 'GET').map((c) => [c.metodo, c.url, c.corpo])
const avisos = () => screen.getByRole('region', { name: /^Notificações/ })

describe('instituições da plataforma', () => {
  afterEach(() => {
    cleanup()
    localStorage.clear()
  })

  it('lista com saude e etiquetas: situação, taxa e ações', async () => {
    tela()
    const ativa = await linha('Faculdade Demo')
    expect(cabecalhos()).toEqual(['Nome', 'Situação', 'Última atividade', 'Não encontrei', 'Administradores', 'Criada em', 'Ações'])
    const celulas = within(ativa).getAllByRole('cell')
    expect(celulas[0]).toHaveTextContent('Faculdade Demo/demo')
    expect(within(celulas[0]).getByText('/demo')).toHaveClass('text-xs')
    expect(celulas[1]).toHaveTextContent(/^Ativa$/)
    expect(celulas[2]).toHaveTextContent(/^16\/11\/2026$/)
    expect(celulas[3]).toHaveTextContent(/^12,5\s%$/)
    expect(celulas[4]).toHaveTextContent(/^2$/)
    expect(celulas[5]).toHaveTextContent(/^02\/11\/2026$/)
    const inativa = await linha('Faculdade Antiga')
    const dela = within(inativa).getAllByRole('cell')
    expect(dela[1]).toHaveTextContent(/^Desativada$/)
    expect(dela[2]).toHaveTextContent(/^Sem atividade$/)
    expect(dela[3]).toHaveTextContent(/^-$/)

    expect(await acoesDe('Faculdade Demo')).toEqual(['Importar carga', 'Reenviar acesso do admin', 'Desativar!'])
    expect(await acoesDe('Faculdade Antiga')).toEqual(['Importar carga', 'Reenviar acesso do admin', 'Reativar'])
    expect(screen.getByRole('button', { name: 'Nova instituição' })).toBeInTheDocument()
  })

  it('lista com saude e etiquetas: as cinco de atenção e o primeiro acesso', async () => {
    const etiquetas = ['PARADA', 'MUITOS_SEM_BASE', 'EMAILS_FALHANDO', 'CONTEUDO_VENCIDO', 'FILA_ANTIGA']
    tela({}, [{ ...demo, atencao: etiquetas }, { ...antiga, ativa: true, situacao: 'AGUARDANDO_PRIMEIRO_ACESSO' }])
    const celula = within(await linha('Faculdade Demo')).getAllByRole('cell')[1]
    expect(within(celula).getAllByText(/./).map((e) => e.textContent)).toEqual(['Ativa', 'Parada', "Muitos 'não encontrei'", 'E-mails falhando', 'Conteúdo vencido', 'Fila antiga'])
    expect(within(within(await linha('Faculdade Antiga')).getAllByRole('cell')[1]).getByText('Aguardando primeiro acesso')).toBeInTheDocument()
  })

  it('estados vazios: instituições mostra a frase e o botão de criar', async () => {
    tela({}, [])
    expect(await screen.findByText('Ainda não há instituição cadastrada. Crie a primeira.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Nova instituição' })).toBeInTheDocument()
  })

  it('carregando mostra 3 esqueletos', async () => {
    api({ 'GET /api/plataforma/eu': () => [200, operador], [`GET ${Lista}`]: () => new Promise(() => {}) })
    abrir('/plataforma/instituicoes')
    expect(await screen.findByRole('heading', { name: 'Instituições', level: 1 })).toBeInTheDocument()
    await waitFor(() => expect(document.querySelectorAll('[data-slot="skeleton"]')).toHaveLength(3))
  })

  async function abrirNova() {
    await userEvent.click(await screen.findByRole('button', { name: 'Nova instituição' }))
    const d = within(await screen.findByRole('dialog'))
    return { d, admin: within(d.getByRole('group', { name: 'Primeiro administrador' })) }
  }

  it('endereço segue o nome até ser editado', async () => {
    tela()
    const { d } = await abrirNova()
    const [nome, endereco] = [d.getAllByLabelText('Nome')[0], d.getByLabelText('Endereço')]

    await userEvent.type(nome, 'Centro Universitário São José & Cia')
    expect(endereco).toHaveValue('centro-universitario-sao-jose-cia')

    await userEvent.clear(endereco)
    await userEvent.type(endereco, 'csj')
    await userEvent.type(nome, ' Ltda')
    expect(nome).toHaveValue('Centro Universitário São José & Cia Ltda')
    expect(endereco).toHaveValue('csj')

    const fuso = d.getByLabelText('Fuso horário') as HTMLSelectElement
    expect(fuso).toHaveValue('America/Sao_Paulo')
    const valores = [...fuso.options].map((o) => o.value)
    expect(valores[0]).toBe('America/Sao_Paulo')
    expect(valores.every((v) => v.startsWith('America/'))).toBe(true)
    expect([...valores].sort()).toEqual(
      ['Araguaina', 'Bahia', 'Belem', 'Boa_Vista', 'Campo_Grande', 'Cuiaba', 'Eirunepe', 'Fortaleza', 'Maceio', 'Manaus', 'Noronha', 'Porto_Velho', 'Recife', 'Rio_Branco', 'Santarem', 'Sao_Paulo'].map((z) => `America/${z}`),
    )
  })

  async function preencher({ d, admin }: Awaited<ReturnType<typeof abrirNova>>) {
    await userEvent.type(d.getAllByLabelText('Nome')[0], 'Faculdade Demo')
    await userEvent.selectOptions(d.getByLabelText('Fuso horário'), 'America/Cuiaba')
    await userEvent.type(admin.getByLabelText('Nome'), 'Dona Admin')
    await userEvent.type(admin.getByLabelText('CPF'), '123.456.789-09')
    await userEvent.type(admin.getByLabelText('E-mail'), 'admin@demo.edu.br')
  }

  it('criar fecha, avisa e recarrega a lista', async () => {
    let lidas = 0
    const chamadas = tela({
      [`GET ${Lista}`]: () => [200, ++lidas === 1 ? [antiga] : [demo, antiga]],
      [`POST ${Lista}`]: () => [201, demo],
    })
    await preencher(await abrirNova())
    await userEvent.click(screen.getByRole('button', { name: 'Criar' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(gravacoes(chamadas)).toEqual([
      ['POST', Lista, { slug: 'faculdade-demo', nome: 'Faculdade Demo', fuso: 'America/Cuiaba', admin: { cpf: '123.456.789-09', nome: 'Dona Admin', email: 'admin@demo.edu.br' } }],
    ])
    expect(await screen.findByText(comTexto('Instituição Faculdade Demo criada. Enviamos o link de primeiro acesso para admin@demo.edu.br. /faculdade-demo'))).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '/faculdade-demo' })).toHaveAttribute('href', '/faculdade-demo')
    expect(await linha('Faculdade Demo')).toBeInTheDocument()
    expect(lidas).toBe(2)
  })

  it('erro da API no campo sem perder o formulário', async () => {
    let tentativa = 0
    tela({
      [`POST ${Lista}`]: () =>
        ++tentativa === 1
          ? [400, { status: 400, detail: 'E-mail inválido.', errors: { 'admin.email': ['E-mail inválido.'] } }]
          : tentativa === 2
            ? [409, { status: 409, detail: 'Esse endereço já está em uso.' }]
            : [500, { status: 500, detail: 'Ocorreu um erro inesperado. Tente de novo.' }],
    })
    const abertura = await abrirNova()
    const { d, admin } = abertura
    await preencher(abertura)
    const conferirCampos = () => {
      expect(d.getAllByLabelText('Nome')[0]).toHaveValue('Faculdade Demo')
      expect(d.getByLabelText('Endereço')).toHaveValue('faculdade-demo')
      expect(d.getByLabelText('Fuso horário')).toHaveValue('America/Cuiaba')
      expect(admin.getByLabelText('Nome')).toHaveValue('Dona Admin')
      expect(admin.getByLabelText('CPF')).toHaveValue('123.456.789-09')
      expect(admin.getByLabelText('E-mail')).toHaveValue('admin@demo.edu.br')
    }

    await userEvent.click(d.getByRole('button', { name: 'Criar' }))
    expect(await screen.findByText('E-mail inválido.')).toBeInTheDocument()
    expect(admin.getByLabelText('E-mail')).toHaveAccessibleDescription('E-mail inválido.')
    conferirCampos()

    await userEvent.click(d.getByRole('button', { name: 'Criar' }))
    await waitFor(() => expect(d.getByLabelText('Endereço')).toHaveAccessibleDescription('Esse endereço já está em uso.'))
    expect(screen.queryByText('E-mail inválido.')).not.toBeInTheDocument()
    conferirCampos()
    expect(screen.getByRole('dialog')).toBeInTheDocument()

    await userEvent.click(d.getByRole('button', { name: 'Criar' }))
    expect(await d.findByText('Ocorreu um erro inesperado. Tente de novo.')).toBeInTheDocument()
    expect(d.getByLabelText('Endereço')).not.toHaveAccessibleDescription()
    conferirCampos()
  })

  it('importar carga mostra o resumo ou os erros', async () => {
    let tentativa = 0
    const chamadas = tela({
      [`POST ${Lista}/demo/carga`]: () =>
        ++tentativa === 1
          ? [200, { setores: 2, cursos: 1, categorias: 3, regras: 2, artigos: 5 }]
          : tentativa === 2
            ? [400, { status: 400, detail: 'O arquivo de carga tem erros.', errors: { arquivo: ['regras[0].categoria: categoria "Nenhuma" não existe', 'setores[1].nome: "A" repetido'] } }]
            : [413, { status: 413, detail: 'O arquivo passa de 2 MB.' }],
    })
    await escolher('Faculdade Demo', 'Importar carga')
    const d = within(await screen.findByRole('dialog', { name: /Importar carga/ }))
    const campo = d.getByLabelText(/Arquivo de carga/) as HTMLInputElement
    expect(campo).toHaveAttribute('type', 'file')
    expect(campo.accept).toContain('.json')
    expect(d.getByRole('button', { name: 'Importar' })).toBeDisabled()

    const arquivo = new File(['{}'], 'carga.json', { type: 'application/json' })
    await userEvent.upload(campo, arquivo)
    await userEvent.click(d.getByRole('button', { name: 'Importar' }))
    expect(await d.findByText('Importados: 2 setores, 1 curso, 3 categorias, 2 regras, 5 artigos.')).toBeInTheDocument()
    expect(gravacoes(chamadas)).toEqual([['POST', `${Lista}/demo/carga`, { arquivo }]])

    await userEvent.click(d.getByRole('button', { name: 'Importar' }))
    const primeiro = await d.findByText('regras[0].categoria: categoria "Nenhuma" não existe')
    const segundo = d.getByText('setores[1].nome: "A" repetido')
    expect(primeiro.closest('tr')).not.toBe(segundo.closest('tr'))
    expect(primeiro.closest('tr')).toBeTruthy()
    expect(d.queryByText(/^Importados:/)).not.toBeInTheDocument()

    await userEvent.click(d.getByRole('button', { name: 'Importar' }))
    expect(await d.findByText('O arquivo passa de 2 MB.')).toBeInTheDocument()
    expect(d.queryByText('setores[1].nome: "A" repetido')).not.toBeInTheDocument()
  })

  it('desativar pede confirmacao e motivo, reativar e reenviar agem direto', async () => {
    let desativada = false
    const chamadas = tela({
      [`GET ${Lista}`]: () => [200, [{ ...demo, ativa: !desativada, situacao: desativada ? 'DESATIVADA' : 'ATIVA' }, antiga]],
      [`POST ${Lista}/demo/desativar`]: () => {
        desativada = true
        return [200, { slug: 'demo', ativa: false }]
      },
      [`POST ${Lista}/antiga/reativar`]: () => [200, { slug: 'antiga', ativa: true }],
      [`POST ${Lista}/demo/reenviar-acesso`]: () => [202, { enviados: 2 }],
    })

    await escolher('Faculdade Demo', 'Desativar')
    const d = within(await screen.findByRole('dialog'))
    expect(d.getByText('Desativar Faculdade Demo? Ninguém dessa instituição consegue entrar até você reativar.')).toBeInTheDocument()
    expect(d.getByRole('button', { name: 'Desativar' })).toBeDisabled()
    await userEvent.type(d.getByLabelText('Motivo'), '  ')
    expect(d.getByRole('button', { name: 'Desativar' })).toBeDisabled()
    await userEvent.click(d.getByRole('button', { name: 'Voltar' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(gravacoes(chamadas)).toEqual([])

    await escolher('Faculdade Demo', 'Desativar')
    await userEvent.keyboard('{Escape}')
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())

    await escolher('Faculdade Demo', 'Desativar')
    const segunda = within(await screen.findByRole('dialog'))
    expect(segunda.getByLabelText('Motivo')).toHaveValue('')
    await userEvent.type(segunda.getByLabelText('Motivo'), 'Contrato encerrado')
    await userEvent.click(segunda.getByRole('button', { name: 'Desativar' }))
    await waitFor(() => expect(gravacoes(chamadas)).toEqual([['POST', `${Lista}/demo/desativar`, { motivo: 'Contrato encerrado' }]]))
    await waitFor(async () => expect(within(await linha('Faculdade Demo')).getAllByRole('cell')[1]).toHaveTextContent(/^Desativada$/))

    await escolher('Faculdade Antiga', 'Reativar')
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(await within(avisos()).findByText('Instituição reativada.')).toBeInTheDocument()

    await escolher('Faculdade Demo', 'Reenviar acesso do admin')
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(await within(avisos()).findByText('Enviamos o link para 2 administrador(es).')).toBeInTheDocument()
    expect(gravacoes(chamadas).map(([, url]) => url)).toEqual([`${Lista}/demo/desativar`, `${Lista}/antiga/reativar`, `${Lista}/demo/reenviar-acesso`])
  })

  it('estados vazios: auditoria mostra a frase sem botão de criar', async () => {
    api({ 'GET /api/plataforma/eu': () => [200, operador], 'GET /api/plataforma/auditoria?pagina=1': () => [200, { itens: [], total: 0 }] })
    abrir('/plataforma/auditoria')
    expect(await screen.findByText('Ainda não há ações registradas.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Nova instituição' })).not.toBeInTheDocument()
  })

  it('casca da plataforma com a marca do Esclari e dois itens', async () => {
    api({ 'GET /api/plataforma/eu': () => [200, operador], [`GET ${Lista}`]: () => [200, []] })
    abrir('/plataforma/instituicoes')
    const nav = await screen.findByRole('navigation', { name: 'Navegação' })
    expect(within(nav).getAllByRole('link').map((l) => [l.textContent, l.getAttribute('href')])).toEqual([
      ['Instituições', '/plataforma/instituicoes'],
      ['Auditoria', '/plataforma/auditoria'],
    ])
    expect(screen.getByRole('img', { name: 'Esclari' })).toHaveAttribute('src', '/marca/logo-horizontal.svg')
    expect(screen.getByText('Marina Souza', { selector: 'span' })).toBeInTheDocument()
  })

  it('criar leva a importar a carga na mesma tela', async () => {
    const chamadas = tela({
      [`POST ${Lista}`]: () => [201, demo],
      [`POST ${Lista}/faculdade-demo/carga`]: () => [200, { setores: 2, cursos: 1, categorias: 3, regras: 2, artigos: 5 }],
    })
    await preencher(await abrirNova())
    await userEvent.click(screen.getByRole('button', { name: 'Criar' }))
    await userEvent.click(await screen.findByRole('button', { name: 'Importar carga' }))
    const d = within(await screen.findByRole('dialog', { name: 'Importar carga: Faculdade Demo' }))
    const arquivo = new File(['{}'], 'carga.json', { type: 'application/json' })
    await userEvent.upload(d.getByLabelText(/Arquivo de carga/), arquivo)
    await userEvent.click(d.getByRole('button', { name: 'Importar' }))
    expect(await d.findByText('Importados: 2 setores, 1 curso, 3 categorias, 2 regras, 5 artigos.')).toBeInTheDocument()
    expect(gravacoes(chamadas).map(([m, u]) => [m, u])).toEqual([['POST', Lista], ['POST', `${Lista}/faculdade-demo/carga`]])
  })
})
