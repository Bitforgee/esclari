import { screen, within } from '@testing-library/react'
import { existsSync, readdirSync, readFileSync } from 'node:fs'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import componentes from '../../components.json'
import html from '../../index.html?raw'
import pacote from '../../package.json'
import tsconfig from '../../tsconfig.json'
import vite from '../../vite.config.ts?raw'
import barra from '../corDaBarra.json'
import { abrir, api, chamado, me } from './util'

const fontes = import.meta.glob('/src/**/*.{ts,tsx}', { query: '?raw', import: 'default', eager: true }) as Record<string, string>
const arquivos = Object.keys(fontes).map((c) => c.replace('/src/', ''))
const texto = (arquivo: string) => fontes[`/src/${arquivo}`]
const css = readFileSync('src/index.css', 'utf8')
const todas = { ...pacote.dependencies, ...pacote.devDependencies } as Record<string, string>
const comBootstrap = () => arquivos.some((a) => /from 'react-bootstrap'/.test(texto(a)))
const semEspacos = (t: string) => t.replace(/\s+/g, '')

function bloco(seletor: string): Record<string, string> {
  const corpo = css.slice(css.indexOf(`${seletor} {`)).split('}')[0]
  const valores: Record<string, string> = {}
  for (const [, nome, valor] of corpo.matchAll(/(--[\w-]+):\s*([^;]+);/g)) valores[nome] = valor.trim().replace(/^var\(--[\w-]+, (.*)\)$/, '$1').replace(/^oklch\((.*)\)$/, '$1')
  return valores
}

type Cor = { L: number; C: number; h?: number }
type Tema = Record<string, string>

function partes(texto: string) {
  const saida: string[] = []
  let [nivel, atual] = [0, '']
  for (const c of texto) {
    nivel += c === '(' ? 1 : c === ')' ? -1 : 0
    if (c === ',' && nivel === 0) (saida.push(atual.trim()), (atual = ''))
    else atual += c
  }
  return [...saida, atual.trim()]
}

function deHex(hex: string): Cor {
  const [r, g, b] = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255).map((c) => (c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4))
  const [l, m, s] = [0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b, 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b, 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b].map(Math.cbrt)
  const [L, A, B] = [0.2104542553 * l + 0.793617785 * m - 0.0040720468 * s, 1.9779984951 * l - 2.428592205 * m + 0.4505937099 * s, 0.0259040371 * l + 0.7827717662 * m - 0.808675766 * s]
  return { L, C: Math.hypot(A, B), h: ((Math.atan2(B, A) * 180) / Math.PI + 360) % 360 }
}

function matiz(a: Cor, b: Cor, p: number) {
  if (a.h === undefined || b.h === undefined) return a.h ?? b.h
  const d = ((b.h - a.h + 540) % 360) - 180
  return (a.h + d * (1 - p) + 360) % 360
}
const misturar = (a: Cor, b: Cor, p: number): Cor => ({ L: a.L * p + b.L * (1 - p), C: a.C * p + b.C * (1 - p), h: matiz(a, b, p) })

function lerOklch(dentro: string): Cor {
  const [L, C, h] = dentro.trim().split(/\s+/)
  return { L: Number(L), C: Number(C), h: h === 'none' ? undefined : Number(h) }
}

function avaliarVar(dentro: string, tema: Tema, marca?: Cor): Cor {
  const [nome, reserva] = partes(dentro)
  if (nome === '--marca') return marca ?? avaliar(reserva, tema)
  if (nome.startsWith('--marca-') && !marca) return avaliar(reserva, tema)
  return avaliar(tema[nome], tema, marca)
}

function avaliarMistura(dentro: string, tema: Tema, marca?: Cor): Cor {
  const [, primeira, segunda] = partes(dentro)
  const [, cor, pct] = primeira.match(/^(.*)\s+([\d.]+)%$/)!
  return misturar(avaliar(cor, tema, marca), avaliar(segunda, tema, marca), Number(pct) / 100)
}

function avaliar(valor: string, tema: Tema, marca?: Cor): Cor {
  const v = valor.trim()
  if (v === 'white') return { L: 1, C: 0 }
  if (v.startsWith('#')) return deHex(v)
  const [, funcao, dentro] = v.match(/^([\w-]+)\((.*)\)$/) ?? ['', 'oklch', v]
  if (funcao === 'var') return avaliarVar(dentro, tema, marca)
  return funcao === 'color-mix' ? avaliarMistura(dentro, tema, marca) : lerOklch(dentro)
}

function luminancia({ L, C, h = 0 }: Cor) {
  const a = C * Math.cos((h * Math.PI) / 180)
  const b = C * Math.sin((h * Math.PI) / 180)
  const [l3, m3, s3] = [L + 0.3963377774 * a + 0.2158037573 * b, L - 0.1055613458 * a - 0.0638541728 * b, L - 0.0894841775 * a - 1.291485548 * b].map((x) => x ** 3)
  const [r, g, bl] = [
    4.0767416621 * l3 - 3.3077115913 * m3 + 0.2309699292 * s3,
    -1.2684380046 * l3 + 2.6097574011 * m3 - 0.3413193965 * s3,
    -0.0041960863 * l3 - 0.7034186147 * m3 + 1.707614701 * s3,
  ].map((x) => Math.min(1, Math.max(0, x)))
  return 0.2126 * r + 0.7152 * g + 0.0722 * bl
}
const razao = (x: Cor, y: Cor) => {
  const [claro, escuro] = [luminancia(x), luminancia(y)].sort((p, q) => q - p)
  return (claro + 0.05) / (escuro + 0.05)
}

function crus(seletor: string): Tema {
  const corpo = css.slice(css.indexOf(`${seletor} {`)).split('}')[0]
  return Object.fromEntries([...corpo.matchAll(/(--[\w-]+):\s*([^;]+);/g)].map(([, nome, valor]) => [nome, valor.trim()]))
}
const temasCrus = () => {
  const [raiz, escuro, preto] = [crus(':root'), crus('.dark'), crus('.black')]
  return { ':root': raiz, '.dark': { ...raiz, ...escuro }, '.dark.black': { ...raiz, ...escuro, ...preto } }
}
const contraste = (tema: Tema, frente: string, fundo: string, marca?: Cor) => razao(avaliar(tema[frente], tema, marca), avaliar(tema[fundo], tema, marca))

const ParesDeTexto = [
  ['--foreground', '--background'],
  ['--foreground', '--card'],
  ['--muted-foreground', '--background'],
  ['--muted-foreground', '--muted'],
  ['--muted-foreground', '--primary-subtle'],
  ['--muted-foreground', '--warning-subtle'],
  ['--primary-foreground', '--primary'],
  ['--destructive', '--background'],
]
const ParesDeBorda = [
  ['--input', '--background'],
  ['--input', '--muted'],
  ['--ring', '--background'],
]
const Tinta = '0.216 0.073 281'
const Papel = '0.970 0.010 94'

const tingido = (n: number, base: string) => `color-mix(in oklch, var(--marca, oklch(${base})) ${n}%, oklch(${base}))`

const Tokens: [string, string, string, string][] = [
  ['--background', tingido(2, Papel), tingido(3, '0.17 0.02 281'), '0 0 0'],
  ['--foreground', Tinta, '0.95 0.008 94', '0.92 0 0'],
  ['--card', '1 0 0', '0.205 0.02 281', '0.15 0 0'],
  ['--card-foreground', Tinta, '0.95 0 0', '0.92 0 0'],
  ['--popover', '1 0 0', '0.205 0.02 281', '0.15 0 0'],
  ['--popover-foreground', Tinta, '0.95 0 0', '0.92 0 0'],
  ['--sidebar', tingido(4, '0.955 0.012 94'), tingido(3, '0.15 0.02 281'), '0 0 0'],
  ['--sidebar-foreground', '0.145 0 0', '0.95 0 0', '0.92 0 0'],
  ['--sidebar-primary', Tinta, Papel, '='],
  ['--sidebar-primary-foreground', '0.985 0 0', '0.17 0 0', '='],
  ['--sidebar-accent', tingido(5, '0.92 0.014 94'), tingido(6, '0.27 0.025 281'), tingido(6, '0.22 0 none')],
  ['--sidebar-accent-foreground', '0.205 0 0', '0.985 0 0', '='],
  ['--sidebar-border', tingido(6, '0.88 0.014 94'), tingido(5, '0.27 0.02 281'), tingido(5, '0.26 0 none')],
  ['--sidebar-ring', 'var(--ring)', 'var(--ring)', '='],
  ['--ring', '0.5 0 0', '0.556 0 0', '='],
  ['--primary', Tinta, Papel, '='],
  ['--primary-foreground', '0.985 0 0', Tinta, '='],
  ['--primary-subtle', '0.94 0.02 281', '0.27 0.04 281', '0.24 0.04 281'],
  ['--secondary', '0.97 0 0', '0.269 0 0', '='],
  ['--secondary-foreground', '0.205 0 0', '0.985 0 0', '='],
  ['--muted', tingido(4, '0.945 0.012 94'), tingido(5, '0.22 0.02 281'), tingido(4, '0.19 0 none')],
  ['--accent', tingido(5, '0.935 0.013 94'), tingido(6, '0.25 0.022 281'), tingido(6, '0.24 0 none')],
  ['--accent-foreground', '0.205 0 0', '0.985 0 0', '='],
  ['--muted-foreground', '0.48 0.012 281', '0.71 0.01 281', '0.70 0 0'],
  ['--warning-subtle', '0.97 0.04 90', '0.30 0.05 85', '0.27 0.05 85'],
  ['--success', '0.52 0.13 155', '0.72 0.14 155', '='],
  ['--destructive', '0.55 0.22 27.3', '0.704 0.191 22.216', '='],
  ['--border', tingido(6, '0.89 0.012 94'), tingido(5, '0.27 0.02 281'), tingido(5, '0.26 0 none')],
  ['--input', tingido(6, '0.62 0.012 94'), tingido(5, '0.58 0.015 281'), tingido(5, '0.58 0 none')],
  ['--chart-1', tingido(45, '0.6 0 none'), tingido(45, '0.8 0 none'), '='],
  ['--chart-2', tingido(45, '0.6 0 none'), tingido(45, '0.8 0 none'), '='],
]
const temas = () => ({ ':root': bloco(':root'), '.dark': bloco('.dark'), '.dark.black': { ...bloco('.dark'), ...bloco('.black') } })
const formResposta = () => css.replace(/\/\*[\s\S]*?\*\//g, '').match(/\.form-resposta:not\(\.dark \*\)\s*\{\s*--input:\s*([^;]+);/)![1].trim()
const Prontas = () => [...texto('administracao/aparencia/CoresProntas.tsx').matchAll(/\['([^']+)', '([0-9a-f]{6})'\]/g)].map(([, nome, hex]) => [nome, deHex('#' + hex)] as const)

afterEach(() => {
  document.documentElement.className = ''
})

describe('interface nova', () => {
  it('versões exatas e pacotes proibidos', () => {
    expect(Object.entries(todas).filter(([, versao]) => !/^\d+\.\d+\.\d+$/.test(versao))).toEqual([])
    for (const proibido of ['ai', '@ai-sdk/react', 'streamdown', 'next-themes']) expect(todas, proibido).not.toHaveProperty([proibido])
  })

  it('foco sem classes de auréola', () => {
    const proibidas = ['ring-ring' + '/50', 'outline-ring' + '/50', 'focus-visible:ring-' + '[3px]']
    const lidos = arquivos.filter((a) => a !== 'test/interface.test.ts')
    expect(arquivos.filter((a) => a.startsWith('ui/') && !a.includes('.test.')).length).toBeGreaterThanOrEqual(8)
    for (const p of proibidas) expect([p, lidos.filter((a) => texto(a).includes(p))]).toEqual([p, []])
    expect(css).not.toContain('outline-ring' + '/50')
  })

  it('sem biblioteca de gráfico', () => {
    const lock = JSON.parse(readFileSync('package-lock.json', 'utf8'))
    expect(todas).not.toHaveProperty(['recharts'])
    expect(lock.packages[''].dependencies).not.toHaveProperty(['recharts'])
    expect(arquivos).not.toContain('indicadores/Grafico.tsx')
    const assets = '../Esclari.Api/wwwroot/assets'
    expect(existsSync(assets) ? readdirSync(assets).filter((f) => f.startsWith('Grafico-')) : []).toEqual([])
  })

  it('SignalR exato e só no web', () => {
    expect(pacote.dependencies['@microsoft/signalr']).toMatch(/^\d+\.\d+\.\d+$/)
    expect(pacote.devDependencies).not.toHaveProperty(['@microsoft/signalr'])
  })

  it('proxy do dev com WebSocket e PWA sem API em cache', () => {
    expect(semEspacos(vite)).toContain("proxy:{'/api':{target:'http://localhost:5226',ws:true}}")
    const sw = readFileSync('src/sw.ts', 'utf8')
    expect(sw).toContain('denylist: Negados')
    expect(sw).toContain('export const Negados = [/^\\/api\\//]')
    expect(vite).toContain("globPatterns: ['**/*.{js,css,html,png,svg,woff,woff2}']")
    expect(vite).not.toContain('runtimeCaching')
  })

  it('pacotes do Tiptap', () => {
    for (const nome of ['@tiptap/react', '@tiptap/pm', '@tiptap/extension-document', '@tiptap/extension-paragraph', '@tiptap/extension-text', '@tiptap/extensions'])
      expect(todas[nome], nome).toMatch(/^\d+\.\d+\.\d+$/)
    expect(todas).not.toHaveProperty(['@tiptap/starter-kit'])
  })

  it('configuração do Tailwind e do shadcn', () => {
    expect(semEspacos(vite)).toContain('plugins:[react(),tailwindcss(),VitePWA(')
    expect(vite).toMatch(/alias:\s*\{\s*'@':\s*path\.resolve\(__dirname,\s*'\.\/src'\)\s*\}/)
    expect(componentes.style).toBe('new-york')
    expect(componentes.tailwind).toMatchObject({ config: '', css: 'src/index.css', cssVariables: true })
    expect(componentes.aliases).toEqual({ components: '@/ui', ui: '@/ui', utils: '@/ui/utils', lib: '@/ui', hooks: '@/ui' })
    expect(tsconfig.compilerOptions.paths).toEqual({ '@/*': ['./src/*'] })
  })

  it('sem Bootstrap nos arquivos', () => {
    for (const pacote of ['react-' + 'bootstrap', 'bootstrap-' + 'icons']) expect(arquivos.filter((a) => !a.includes('.test.') && texto(a).includes(pacote)), pacote).toEqual([])
    expect(css).not.toContain('bootstrap')
  })

  it('camada do Bootstrap', () => {
    const Css = 'bootstrap/dist/css/bootstrap.min.css'
    if (comBootstrap()) {
      expect(css.startsWith('@layer theme, base, bootstrap, components, utilities;')).toBe(true)
      expect(css.split(Css)).toHaveLength(2)
      expect(css).toContain(`@import "${Css}" layer(bootstrap);`)
      expect(arquivos.filter((a) => texto(a).includes(Css))).toEqual([])
    } else {
      expect(css).not.toContain('bootstrap')
      for (const nome of ['bootstrap', 'react-bootstrap', 'bootstrap-icons']) expect(todas, nome).not.toHaveProperty([nome])
    }
  })

  it('contraste dos tokens', () => {
    for (const [seletor, t] of Object.entries(temasCrus()))
      for (const [frente, fundo] of ParesDeTexto) expect(contraste(t, frente, fundo), `${seletor} ${frente}/${fundo}`).toBeGreaterThanOrEqual(4.5)
  })

  it('contraste das bordas e do foco', () => {
    const casos = Object.entries(temasCrus()).flatMap(([seletor, t]) => ParesDeBorda.map(([frente, fundo]) => [`${seletor} ${frente}/${fundo}`, contraste(t, frente, fundo)] as const))
    const raiz = temasCrus()[':root']
    casos.push(['.form-resposta --input/--muted', razao(avaliar(formResposta(), raiz), avaliar(raiz['--muted'], raiz))])
    expect(casos).toHaveLength(10)
    for (const [nome, valor] of casos) expect(valor, nome).toBeGreaterThanOrEqual(3)
  })

  it('contraste com as oito cores prontas', () => {
    const prontas = Prontas()
    expect(prontas.map(([nome]) => nome)).toEqual(['Azul', 'Verde', 'Vinho', 'Roxo', 'Laranja', 'Grafite', 'Petróleo', 'Marrom'])
    let medidas = 0
    for (const [nome, marca] of prontas)
      for (const [seletor, t] of Object.entries(temasCrus()))
        for (const [pares, minimo] of [[ParesDeTexto, 4.5], [ParesDeBorda, 3]] as const)
          for (const [frente, fundo] of pares) {
            expect(contraste(t, frente, fundo, marca), `${nome} ${seletor} ${frente}/${fundo}`).toBeGreaterThanOrEqual(minimo)
            medidas++
          }
    expect(medidas).toBe(264)
  })

  it('fallback de cada neutro e a propria base', () => {
    for (const antigo of ['0.45 0.15 262', '0.72 0.12 262', '0.96 0.02 262']) expect(css).not.toContain(`oklch(${antigo})`)
    const misturas = [...css.matchAll(/color-mix\(in oklch, var\(--marca, (oklch\([^)]+\))\) \d+%, (oklch\([^)]+\))\)/g)]
    expect(misturas).toHaveLength(27)
    for (const [declaracao, reserva, base] of misturas) expect(reserva, declaracao).toBe(base)
    expect(css.match(/var\(--marca, /g)).toHaveLength(27 + 3)
  })

  it('Origem fora da interface', () => {
    const Origem = /c83a23|f16a4b/i
    expect(css).not.toMatch(Origem)
    expect(arquivos.filter((a) => !a.includes('.test.') && !a.startsWith('test/') && Origem.test(texto(a)))).toEqual([])
    const quentes = [...css.matchAll(/oklch\(([\d.]+) ([\d.]+) ([\d.]+)\)/g)].filter(([, , c, h]) => Number(c) > 0.1 && Number(h) >= 30 && Number(h) <= 36)
    expect(quentes.map(([valor]) => valor)).toEqual([])
  })

  it('links text-primary sublinhados', () => {
    const links = arquivos.filter((a) => a.endsWith('.tsx') && !a.includes('.test.')).flatMap((a) => [...texto(a).matchAll(/<a\b[^>]*className="([^"]*\btext-primary\b[^"]*)"/g)].map(([, classes]) => [a, classes]))
    expect(links.length).toBeGreaterThanOrEqual(4)
    for (const [arquivo, classes] of links) expect(classes.split(' '), arquivo).toContain('underline')
  })

  it('tipografia raios e graficos intactos', () => {
    const [claro, escuro, tema] = [bloco(':root'), bloco('.dark'), bloco('@theme inline')]
    expect(tema['--font-sans']).toBe("'Public Sans Variable', ui-sans-serif, system-ui, sans-serif")
    expect(tema['--font-heading']).toBe("'Source Serif 4 Variable', ui-serif, Georgia, serif")
    expect(['--radius-xs', '--radius-sm', '--radius-md', '--radius-lg'].map((r) => tema[r])).toEqual(['4px', '6px', '12px', '16px'])
    const graficos = ['--grafico-1', '--grafico-2', '--grafico-3', '--grafico-4']
    expect(graficos.map((g) => claro[g])).toEqual(['0.42 0.15 262', '0.64 0.12 75', '0.53 0.17 20', '0.31 0 0'])
    expect(graficos.map((g) => escuro[g])).toEqual(['0.64 0.14 262', '0.88 0.12 85', '0.76 0.14 20', '0.52 0 0'])
  })

  it('contraste do painel de entrada', () => {
    for (const [seletor, t] of Object.entries(temasCrus()))
      for (const frente of ['--foreground', '--muted-foreground']) expect(contraste(t, frente, '--muted'), `${seletor} ${frente}`).toBeGreaterThanOrEqual(4.5)
  })

  it('tokens da paleta', () => {
    const [claro, escuro, preto, tema] = [bloco(':root'), bloco('.dark'), bloco('.black'), bloco('@theme inline')]
    expect(css.indexOf('.black {')).toBeGreaterThan(css.indexOf('.dark {'))
    for (const [token, valorClaro, valorEscuro, valorPreto] of Tokens) {
      expect(claro[token], `:root ${token}`).toBe(valorClaro)
      expect(escuro[token], `.dark ${token}`).toBe(valorEscuro)
      expect(preto[token], `.black ${token}`).toBe(valorPreto === '=' ? undefined : valorPreto)
      expect(Object.values(tema), `@theme inline ${token}`).toContain(`var(${token})`)
    }
    expect(tema['--font-sans']).toBe("'Public Sans Variable', ui-sans-serif, system-ui, sans-serif")
    const CorLiteral = /#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{3})(?![0-9a-fA-F])|\b(?:oklch|rgb|hsl)\(/
    expect(arquivos.filter((a) => CorLiteral.test(texto(a)))).toEqual([])
  })

  it('títulos de página', () => {
    const titulos = arquivos.filter((a) => !a.includes('.test.')).flatMap((a) => [...texto(a).matchAll(/<h1[^>]*>/g)].map((m) => [a, m[0]]))
    expect(titulos.map(([arquivo]) => arquivo).sort()).toEqual(['Autenticado.tsx', 'acesso/Acesso.tsx', 'plataforma/Entrada.tsx', 'plataforma/Plataforma.tsx'])
    for (const [arquivo, h1] of titulos)
      expect(h1, arquivo).toMatch(arquivo === 'Autenticado.tsx' ? /className="(?:[^"]* )?text-base font-medium[ "]/ : /className="(?:[^"]* )?font-heading text-\[28px\][ "]/)
  })

  it('regra da borda do form da resposta', () => {
    expect(css.match(/form-resposta/g)).toHaveLength(1)
    const regras = [...css.replace(/\/\*[\s\S]*?\*\//g, '').matchAll(/([^{}]*form-resposta[^{}]*)\{([^}]*)\}/g)]
    expect(regras.map((r) => r[1].trim())).toEqual(['.form-resposta:not(.dark *)'])
    const token = Tokens.find((l) => l[0] === '--input')![1]
    expect(token).toContain('0.62 0.012 94')
    expect(regras[0][2].trim()).toBe(`--input: ${token.replaceAll('0.62 0.012 94', '0.59 0.012 94')};`)
  })

  it('tokens de Visual', () => {
    const t = temas()
    const colunas = { ':root': 1, '.dark': 2, '.dark.black': 3 } as const
    for (const [seletor, coluna] of Object.entries(colunas))
      for (const linha of Tokens) {
        const esperado = linha[coluna] === '=' ? linha[2] : linha[coluna]
        expect(t[seletor as keyof typeof t][linha[0]], `${seletor} ${linha[0]}`).toBe(esperado)
      }
  })

  it('contraste dos tokens da barra', () => {
    for (const [seletor, t] of Object.entries(temasCrus()))
      for (const [frente, fundo] of [
        ['--sidebar-foreground', '--sidebar'],
        ['--sidebar-accent-foreground', '--sidebar-accent'],
      ])
        expect(contraste(t, frente, fundo), `${seletor} ${frente}/${fundo}`).toBeGreaterThanOrEqual(4.5)
  })

  it('sem gaveta Vaul', () => {
    expect(arquivos.filter((a) => a === 'ui/drawer.tsx')).toEqual([])
    expect(arquivos.filter((a) => /ui\/drawer['"]|from 'vaul'/.test(texto(a)) && !a.includes('interface.test'))).toEqual([])
    expect(todas).not.toHaveProperty(['vaul'])
  })

  it('provedores dentro do App', () => {
    for (const provedor of ['<QueryClientProvider', '<ThemeProvider', '<MotionConfig', '<Toaster']) {
      expect(texto('App.tsx'), provedor).toContain(provedor)
      expect(texto('main.tsx'), provedor).not.toContain(provedor)
    }
    expect(texto('main.tsx')).toContain('<App />')
    expect(texto('test/util.tsx')).toMatch(/export function abrir[\s\S]*<App \/>/)
    expect(texto('test/util.tsx')).toContain("import { App } from '../App'")
  })

  it('tema claro enquanto houver react-bootstrap', async () => {
    for (const escuro of [false, true]) {
      vi.stubGlobal('matchMedia', (media: string) => ({ media, matches: escuro, addEventListener() {}, removeEventListener() {}, addListener() {}, removeListener() {} }))
      api({ 'GET /api/auth/me': () => [200, me()] })
      const { unmount } = abrir('/univag/trocar-senha')
      await screen.findByRole('heading', { name: 'Trocar senha' })
      if (comBootstrap()) {
        expect(document.documentElement, `escuro ${escuro}`).toHaveClass('light')
        expect(document.documentElement, `escuro ${escuro}`).not.toHaveClass('dark')
        expect(screen.queryByRole('button', { name: 'Tema' })).not.toBeInTheDocument()
      } else {
        if (!escuro) expect(screen.getByRole('button', { name: 'Tema' })).toBeInTheDocument()
        else expect(screen.queryByRole('button', { name: 'Tema' })).not.toBeInTheDocument()
      }
      unmount()
    }
  })

  it('AI Elements só visual', () => {
    expect(arquivos).toEqual(expect.arrayContaining(['ui/conversation.tsx', 'ui/message.tsx']))
    const exportados = [...texto('ui/message.tsx').matchAll(/export (?:const|function) (\w+)/g)].map(([, nome]) => nome)
    expect(exportados).toEqual(['Message', 'MessageContent'])
    for (const proibido of ['Conversation' + 'ScrollButton', 'Message' + 'Response']) expect(arquivos.filter((a) => texto(a).includes(proibido)), proibido).toEqual([])
  })

  it('TanStack Table só nas 3 tabelas', () => {
    const usam = arquivos.filter((a) => texto(a).includes('useReact' + 'Table('))
    expect(['indicadores/Indicadores.tsx', 'administracao/cadastros/Matriculas.tsx']).toEqual(expect.arrayContaining(usam))
    expect(usam).toContain('indicadores/Indicadores.tsx')
    for (const recurso of ['getSorted' + 'RowModel', 'getFiltered' + 'RowModel', 'getPagination' + 'RowModel']) expect(arquivos.filter((a) => texto(a).includes(recurso)), recurso).toEqual([])
  })

  it('sem Select do Radix nem AlertDialog', () => {
    const Proibido = /from ['"](?:@\/ui\/|(?:\.\.?\/)+ui\/)(?:select|alert-dialog|date-picker)['"]|from ['"]@radix-ui\/react-(?:select|alert-dialog)['"]|import \{[^}]*\b(?:Select|AlertDialog)\b[^}]*\} from ['"]radix-ui['"]/
    expect(arquivos.filter((a) => Proibido.test(texto(a)))).toEqual([])
    for (const nome of ['select', 'alert-dialog', 'date-picker']) expect(arquivos).not.toContain(`ui/${nome}.tsx`)
  })

  it('controles nativos', async () => {
    const admin = me({ papeis: ['ADMINISTRADOR', 'ATENDENTE'], matriculas: [] })
    const tipo = (rotulo: string, raiz: HTMLElement = document.body) => within(raiz).getByLabelText(rotulo).getAttribute('type')
    const telas: [string, Record<string, () => [number, unknown]>, () => Promise<void>][] = [
      ['/univag/indicadores', {}, async () => {
        await screen.findByLabelText('De')
        expect([tipo('De'), tipo('Até')]).toEqual(['date', 'date'])
      }],
      ['/univag/artigos', { 'GET /api/admin/artigos': () => [200, []], 'GET /api/admin/categorias': () => [200, []] }, async () => {
        await screen.findByText('Nenhum artigo')
        await userEvent.click(await screen.findByRole('button', { name: 'Novo artigo' }))
        const dialogo = await screen.findByRole('dialog')
        expect([tipo('Válido de (opcional)', dialogo), tipo('Válido até (opcional)', dialogo)]).toEqual(['date', 'date'])
      }],
      ['/univag/documentos', { 'GET /api/admin/documentos': () => [200, []], 'GET /api/admin/categorias': () => [200, []] }, async () => {
        await screen.findByText('Nenhum documento')
        await userEvent.click(await screen.findByRole('button', { name: 'Novo documento' }))
        expect(tipo('Arquivo PDF', await screen.findByRole('dialog'))).toBe('file')
      }],
      ['/univag/matriculas', {}, async () => {
        await screen.findByLabelText('Arquivo CSV')
        expect(tipo('Arquivo CSV')).toBe('file')
      }],
      ['/univag/chamados/ESC-000001', { 'GET /api/chamados/ESC-000001': () => [200, chamado({ acoes: ['responder'] })] }, async () => {
        await screen.findByLabelText('Anexos (PDF, JPG ou PNG, até 10 MB cada)')
        expect(tipo('Anexos (PDF, JPG ou PNG, até 10 MB cada)')).toBe('file')
      }],
    ]
    for (const [caminho, rotas, conferir] of telas) {
      api({ 'GET /api/auth/me': () => [200, admin], ...rotas })
      const { unmount } = abrir(caminho)
      await conferir()
      unmount()
    }
  })

  it('theme-color segue o tema claro no manifest e no html', () => {
    expect(vite.match(/theme_color:\s*'([^']+)'/)![1]).toBe(barra.light)
    expect(html.match(/<meta name="theme-color" content="([^"]+)" media="\(prefers-color-scheme: light\)"/)![1]).toBe(barra.light)
    expect(html.match(/<meta name="theme-color" content="([^"]+)" media="\(prefers-color-scheme: dark\)"/)![1]).toBe(barra.dark)
    expect(barra.light).not.toBe('#' + '0d6efd')
  })

  it('estado das telas só pelo TanStack Query', () => {
    for (const nome of ['use' + 'Acao', 'use' + 'Lista', 'use' + 'Base', 'use' + 'Listas']) expect(arquivos.filter((a) => !a.includes('.test.') && texto(a).includes(nome)), nome).toEqual([])
    expect(texto('chamado.ts')).not.toContain('set' + 'Interval')
    expect(texto('chamado.ts')).toContain('refetchInterval')
  })
})
