# Logo e marca do Esclari verification

**Verdict**: FAIL
**Profile**: ui
**Diff range**: e39d84d..HEAD
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

Os 25 checks passam e têm evidência localizada. O veredito é FAIL por um mutante que sobreviveu e por um elemento que a direção A desenha e nenhum check cobre: a variante do logo sobre fundo escuro (`dark:brightness-0 dark:invert` em `web/src/acesso/Acesso.tsx:55`).

## Binding sources

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `direcoes.png` (direção A) | yes - lida e comparada com `simbolo.svg`, `favicon.svg`, `logo-horizontal.svg` e os 4 PNG, renderizados num Chromium e vistos como imagem | none: balão com dois cortes à direita formando o E, cauda embaixo à esquerda, palavra "Esclari" em azul à direita do símbolo, placa azul arredondada nos ícones, apple-touch e maskable com placa cheia | A faixa escura da direção A (símbolo e logo em branco sobre preto) não tem nenhum check. O código a implementa em `web/src/acesso/Acesso.tsx:55` (`dark:brightness-0 dark:invert`) e a fault F5 que remove essa classe passa em todos os testes |
| `pesquisa.md` | yes | none (maskable em círculo de 40%, apple-touch opaco 180 px sem cantos, favicon só com o símbolo, cor única: todos conferem com C5, C8, C9, C3) | - |
| `plan.md` Surface e Landing | yes | none (quatro rotas de Surface em C18, quatro portas em C1/C10/C2/C3) | Landing porta 3 fixa "Public Sans 700, entreletras -0.02em": C2 só prova "em curvas, sem `<text>`"; a fonte e o entreletras não têm check (julgado impreciso e visível no render, não selecionável por seletor) |
| `AGENTS.md` | yes | none | Gates `crapper` e `dryer` do `web/`: NÃO foram rodados, não estão disponíveis nesta máquina. `mutator` está suspenso (AD-025) e também não foi rodado |

## Checks

Provas web: `npm --prefix web test -- --reporter=verbose src/test/logo.test.ts src/test/icones.test.ts src/test/guia.test.ts src/acesso/logo-esclari.test.tsx src/aparencia.test.tsx src/acesso/textos.test.tsx` - exit 0, 40 passed, cada teste nomeado apareceu individualmente. `git status --porcelain` depois: só `.tmp-capturas/` (os PNG regravados são idênticos byte a byte).

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | simbolo.svg: viewBox 64, um path currentColor, sem mask/filter/gradient/text/hex | vitest `simbolo.svg tem um unico path com currentColor` ✓ | `web/src/test/logo.test.ts:17-21` - `expect(contar(svg, /<path\b/g)).toBe(1)`; `expect(svg).not.toMatch(/#[0-9a-fA-F]{3,8}\b/)` | PASS |
| C2 | logo-horizontal: razão ≥ 3,5, ≥ 2 paths, sem text/font-family/image | vitest `logo-horizontal.svg traz simbolo e palavra em curvas` ✓ | `logo.test.ts:27-29` - `expect(largura / altura).toBeGreaterThanOrEqual(3.5)`; `expect(contar(svg, /<path\b/g)).toBeGreaterThanOrEqual(2)` (viewBox real 225.8×64 = 3,53) | PASS |
| C3 | favicon `#224fa7` + `#ffffff`, `link rel=icon svg` antes do png | vitest `favicon.svg e declarado antes do png` ✓ | `logo.test.ts:34-39` - `expect(svg).toContain(\`fill="${Marca}"\`)`; `expect(iconePng).toBeGreaterThan(iconeSvg)` | PASS |
| C4 | marca só usa `#224fa7`, branco ou currentColor | vitest `a marca so usa 224fa7 e branco` ✓ | `logo.test.ts:44,48` - `expect(arquivos.sort()).toEqual(['favicon.svg','logo-horizontal.svg','simbolo.svg'])`; `expect([Marca, Branco, 'currentcolor'], ...).toContain(cor)` | PASS |
| C5 | PNG 192/512/512/180 | vitest `dimensoes dos quatro icones` ✓ | `web/src/test/icones.test.ts:128` - `expect([nome, imagem.largura, imagem.altura]).toEqual([nome, lado, lado])` (lista inclui `['apple-touch-icon.png', 180]` na linha 125) | PASS |
| C6 | script sai 0 e regrava os 4 PNG | vitest `gera os quatro icones com exit 0` ✓ | `icones.test.ts:149-150` - `expect(r.status, r.stderr).toBe(0)`; `expect(r.stdout).toContain(\`icones: gravado ${nome}\`)` | PASS |
| C7 | sem simbolo.svg: exit 1, mensagem cita o arquivo, PNG iguais | vitest `sem simbolo.svg` ✓ | `icones.test.ts:163-166` - `expect(r.status).toBe(1)`; `expect(r.stderr).toContain('simbolo.svg')`; `expect(nomes.map(hash)).toEqual(antes)`; ramo no código `web/scripts/gerar-icones.mjs:16-19` | PASS |
| C8 | maskable e apple com alfa 255 em todo pixel | vitest `maskable e apple sao opacos` ✓ | `icones.test.ts:136` - `expect(imagem.alfa(x, y), ...).toBe(255)` em loop 512² e 180² | PASS |
| C9 | pixel branco do maskable a ≤ 205 px do centro, ≥ 1 branco | vitest `maskable dentro da zona segura` ✓ | `icones.test.ts:88,91` - `expect(Math.hypot(x + 0.5 - 256, y + 0.5 - 256), ...).toBeLessThanOrEqual(raio)` (raio = 410/2 = 205); `expect(brancos).toBeGreaterThan(0)` | PASS |
| C10 | apple-touch-icon existe e está declarado | vitest `apple-touch-icon declarado` ✓ | `icones.test.ts:141` - `toContain('<link rel="apple-touch-icon" href="/icons/apple-touch-icon.png" />')`; `:143-144` opaco azul nos cantos | PASS |
| C11 | favicon em 16 px: 15%-45% de pixels com luminância > 200 | vitest `favicon em 16 px` ✓ | `logo.test.ts:78-79` - `expect(proporcao).toBeGreaterThanOrEqual(0.15)`; `expect(proporcao).toBeLessThanOrEqual(0.45)` | PASS |
| C12 | manifesto do vite lista os 3 ícones com os mesmos nomes e `purpose` | vitest `manifesto lista os tres icones` ✓ | `icones.test.ts:173-177` - `expect(icones).toEqual([['/icons/icon-192.png','any'],['/icons/icon-512.png','any'],['/icons/icon-maskable-512.png','maskable']])` | PASS |
| C13 | `/` mostra img `alt="Esclari"` dentro do h1 de nome "Esclari" | vitest `raiz mostra o logo dentro do h1` ✓ | `web/src/acesso/logo-esclari.test.tsx:33,35-36` - `findByRole('heading', { level: 1, name: 'Esclari' })`; `expect(logo).toHaveAttribute('src', '/marca/logo-horizontal.svg')` | PASS |
| C14 | login da instituição sem `/marca/`; só `Acesso.tsx` e `aparencia.tsx` citam `/marca/` em `web/src`; `Esclari.Api` não cita | vitest `marca do Esclari so aparece na raiz` ✓ | `logo-esclari.test.tsx:43,47,49` - `expect(imagem.getAttribute('src')).not.toMatch(/^\/marca\//)`; `expect(citam.sort()).toEqual(['Acesso.tsx', 'aparencia.tsx'])`; `expect(api_.filter(...)).toEqual([])` (ver julgamento abaixo) | PASS |
| C15 | manifesto padrão devolve os 3 caminhos e tamanhos | `dotnet test Esclari.Api.Tests --filter "...ManifestoTests.Manifesto_padrao_sem_aparencia\|...PushAgendadorTests.Payload_exato_sem_dado_pessoal"` exit 0, 2 passed | `Esclari.Api.Tests/ManifestoTests.cs:52-53` - `Assert.Equal(["/icons/icon-192.png", "/icons/icon-512.png", "/icons/icon-maskable-512.png"], ...)`; `Assert.Equal(["192x192", "512x512", "512x512"], ...)`. Teste não tocado pelo diff (`git diff --stat` de `Esclari.Api*` vazio) | PASS |
| C16 | push sem ícone leva `/icons/icon-192.png` | mesma invocação dotnet | `Esclari.Api.Tests/PushAgendadorTests.cs:89` - `Assert.Equal("""{"titulo":"Univag",...,"icone":"/icons/icon-192.png"}""", payload)` | PASS |
| C17 | `sw.js` gerado precacheia os 3 SVG de `marca/` | `npm --prefix web run build` + one-liner `node -e` do checks.md, exit 0; `sw.js` contém `marca/simbolo.svg,marca/logo-horizontal.svg,marca/favicon.svg`; `globPatterns` em `web/vite.config.ts:104` cobre `svg` | one-liner: `if(!s.includes('marca/'+f+'.svg'))process.exit(1)` → exit 0 | PASS |
| C18 | 3 SVG e o apple-touch respondem 200 com o tipo certo | `E2E_URL=http://localhost:5173 npx playwright test e2e/marca.spec.ts` (dev server) - 3 passed | `web/e2e/marca.spec.ts:13-14` - `expect(resposta.status(), caminho).toBe(200)`; `expect(resposta.headers()['content-type'], caminho).toContain(tipo)`. Observação: prova contra o servidor de dev do Vite, não contra o `Esclari.Api` servindo `wwwroot` (o container em :8080 estava desligado); é o que o check pede | PASS |
| C19 | guia tem os 9 títulos | vitest `tem os nove titulos` ✓ | `web/src/test/guia.test.ts:11` - `expect(titulos).toEqual(['Nome','Símbolo','Logo horizontal','Cor','Área de respeito','Tamanho mínimo','Usos proibidos','Tipografia','Voz'])` | PASS |
| C20 | guia: metade da altura, 16 px, 96 px de largura, `#224fa7` | vitest `traz as medidas e a cor` ✓ | `guia.test.ts:15-18` - `toContain('metade da altura do símbolo')`; `toMatch(/símbolo[^.]*16 px/)`; `toMatch(/logo horizontal[^.]*96 px de largura/)`; `toContain('#' + '224fa7')` | PASS |
| C21 | ≥ 5 usos proibidos, cada um com "porque" | vitest `usos proibidos sao 5 com razao` ✓ | `guia.test.ts:23-24` - `expect(itens.length).toBeGreaterThanOrEqual(5)`; `expect(item, item).toContain('porque')` (o guia tem 7 itens) | PASS |
| C22 | nome certo, `ESC-NNNNNN`, `esclari.com.br`, sem "ESCLARI" | vitest `escreve o nome do jeito certo` ✓ | `guia.test.ts:29-32` - `toContain('ESC-NNNNNN')`; `toContain('esclari.com.br')`; `expect(guia).not.toContain('ESCLARI')` | PASS |
| C23 | 360×780: logo com 40 px de altura, sem scroll horizontal | playwright `raiz em celular 360x780` ✓ | `web/e2e/marca.spec.ts:24-25` - `expect(Math.round(caixa.height)).toBe(40)`; `expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(360)` | PASS |
| C24 | 844×390: logo e dois parágrafos, `scrollHeight` ≤ 390 | playwright `raiz deitada 844x390` ✓ | `marca.spec.ts:31-34` - `getByText('Dúvidas, ...')`/`getByText('Abra o endereço ...')` `toBeVisible()`; `expect(await page.evaluate(() => document.documentElement.scrollHeight)).toBeLessThanOrEqual(390)` | PASS |
| C25 | com ícone da instituição o SVG sai e o PNG aponta para `imagens/icone?v=2`; sem ícone voltam `/icons/icon-192.png` e `/marca/favicon.svg` | vitest `favicon da marca some com icone da instituicao e volta sem ele` ✓ | `logo-esclari.test.tsx:56-57,62-63` - `toHaveAttribute('href', iconeDaInstituicao)`; `expect(noHead('link[rel="icon"][type="image/svg+xml"]')).toBeNull()`; `toHaveAttribute('href', '/marca/favicon.svg')` | PASS |

Julgamentos pedidos:

- **Testes existentes editados.** `web/src/aparencia.test.tsx:149-150`: o apple-touch padrão agora é `/icons/apple-touch-icon.png` (AC 8, trocado, não removido) e o PNG da aba é achado por `link[rel="icon"][type="image/png"]` porque o `index.html` ganhou um segundo `link rel=icon`. O `toBe` do valor segue exato e a linha `:148` (`href` igual à base) ficou igual. Não enfraquece. `web/src/acesso/textos.test.tsx:54-57`: o texto "Esclari" saiu da lista de `estados([...])` (a `img` não é texto) e entrou `expect(within(container).getByRole('heading', { level: 1, name: 'Esclari' })).toContainElement(within(container).getByRole('img', { name: 'Esclari' }))`; `t.conferir` e `t.todos()` seguem valendo para os outros dois textos. Mesma cobertura do nome, mais a estrutura. Não enfraquece.
- **Scan do C14.** Pegaria: um novo `.ts/.tsx` de `web/src` que cite `/marca/`, e um `.cs` de `Esclari.Api` que o cite. Não pegaria: citação em arquivo não `.cs` da API (só `CodeMetricsConfig.txt` existe fora de `.cs`/`bin`/`obj`/`wwwroot`, sem templates), caminho montado sem a string literal `/marca/`, nem uma nova `img` do Esclari dentro de `Acesso.tsx`/`aparencia.tsx` em tela que não seja o login da Univag com identidade (a checagem de `img` só renderiza `/univag`; sidebar e assistente ficam só pelo scan textual). Cobertura razoável para a regressão realista (alguém importar o logo em outro arquivo), fraca para uma regressão dentro de `Acesso.tsx`.
- **Aviso do `validate_checks.py`** sobre a linha de Coverage sem tamanho `(N)`: confirmado como cosmético; os conjuntos reais foram recomputados abaixo.
- **Gates do AGENTS.md**: `crapper` e `dryer` não foram rodados (indisponíveis nesta máquina).

## Coverage

Recomputado a partir de `plan.md` (Surface, Landing, Criteria 1-20), `direcoes.png` e dos arquivos reais, não da tabela do autor.

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| Rotas de Surface (4) | `plan.md` Surface | `/marca/simbolo.svg` C18 · `/marca/logo-horizontal.svg` C18 · `/marca/favicon.svg` C18 · `/icons/apple-touch-icon.png` C18 (todas 200; `marca.spec.ts:13`) | - |
| Exit codes de `gerar-icones.mjs` (2) | `gerar-icones.mjs:13,19,22` (`process.exit(1)` por `theme_color`, simbolo ausente, path ausente) | 0 C6 · 1 simbolo ausente C7 | ramo `<path d>` ausente (`gerar-icones.mjs:20-23`) sem teste; o plano só exige o ramo do arquivo ausente (AC 5), então registrado como observação, não como FAIL |
| PNG gerados (4) | `ICONES` em `gerar-icones.mjs` | `icon-192` C5 · `icon-512` C5 · `icon-maskable-512` C5, C8, C9 · `apple-touch-icon` C5, C8, C10 | - |
| Portas de Landing (4) | `plan.md` Landing | 1 símbolo C1 · 2 nomes C10, C12, C15, C16 · 3 palavra em curvas C2 · 4 cor C3, C4, C20 | - |
| Arquivos de `marca/` (3) | `ls web/public/marca` | `simbolo.svg` C1, C4 · `logo-horizontal.svg` C2, C4 · `favicon.svg` C3, C4, C11 | - |
| Critérios AC 1-20 (20) | `plan.md` Criteria | AC1 C1 · AC2 C2 · AC3 C3, C25 · AC4 C5, C6 · AC5 C7 · AC6 C8 · AC7 C9 · AC8 C10 · AC9 C11 · AC10 C13 · AC11 C14, C25 · AC12 C12, C15 · AC13 C16 · AC14 C17 · AC15 C19 · AC16 C20 · AC17 C21 · AC18 C22 · AC19 C23 · AC20 C24 | - |
| Guia, seções (9) | `guia-de-marca.md` | os 9 títulos em C19 `guia.test.ts:11` | - |
| Guia, medidas (4) | `plan.md` AC 16 | respeito, 16 px, 96 px, cor em C20 `guia.test.ts:15-18` | - |
| Identidade da instituição prevalece (6) | `plan.md` Observable / AC 11 | login C14 · aba C25 · sidebar C14 (só scan textual) · e-mail C14 (só scan textual de `.cs`) · manifesto C15 · push C16 | - |
| Variantes do logo que `direcoes.png` A desenha (3: sobre claro, sobre escuro, placa do app) | `direcoes.png` seção A (faixa clara e faixa escura) | sobre claro C13 · placa do app C3, C5 · **sobre escuro: nenhum** | **variante sobre fundo escuro (`dark:brightness-0 dark:invert`, `Acesso.tsx:55`)**: sem check; a fault F5 sobreviveu |
| Observable da raiz `/` (4 com critério) | `plan.md` Observable | offline C17 · celular C23 · deitado C24 · acessibilidade C13 | - |

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| `gerar-icones.mjs` decide: um no próprio nível; saídas 0 (C6) e 1 por arquivo ausente (C7) | `web/scripts/gerar-icones.mjs` | spawn do script | yes - `icones.test.ts:149-150` (exit 0) e `:163-166` (exit 1, mensagem, PNG intactos); fault F2 matou C9 |
| `Raiz` instrumentação: um no nível da tela; logo, `alt` e nome do `h1` (C13) | `web/src/acesso/Acesso.tsx` | teste de tela | yes - `logo-esclari.test.tsx:33-36` |
| Arquivos estáticos de `marca/` e `icons/`: um na fronteira, status e tipo de cada (C18) | `web/public/marca/*.svg`, `web/public/icons/apple-touch-icon.png` | servidor | yes - `marca.spec.ts:13-14` nos 4 arquivos, contra o servidor de dev do Vite |

## Faults injected

Worktree `git worktree add %TEMP%\ver-wt HEAD` (com junção para `web/node_modules`), removida ao fim; `git worktree prune`; `git status --porcelain` do tree real igual ao baseline (só `.tmp-capturas/`). Sem `git stash`.

| Mutation | Location | Killed |
| --- | --- | --- |
| segundo `<path>` acrescentado ao símbolo | `web/public/marca/simbolo.svg` | yes - `logo.test.ts > simbolo.svg tem um unico path com currentColor` |
| escala do maskable 0.68 → 0.9, ícones regenerados com o script | `web/scripts/gerar-icones.mjs` (`ESCALA`) | yes - `icones.test.ts > maskable dentro da zona segura` |
| remoção do `<link>` do SVG quando a instituição tem ícone (`atual?.remove()` apagado) | `web/src/aparencia.tsx:93-96` (`faviconDaMarca`) | yes - `logo-esclari.test.tsx > favicon da marca some com icone da instituicao e volta sem ele` e `aparencia.test.tsx > ícone na aba, na barra recolhida e no topo` |
| mínimo do logo horizontal 96 px → 80 px | `guia-de-marca.md` (Tamanho mínimo) | yes - `guia.test.ts > traz as medidas e a cor` |
| `dark:brightness-0 dark:invert` removido do logo da raiz | `web/src/acesso/Acesso.tsx:55` | **no** - `logo-esclari.test.tsx` e `textos.test.tsx`: 10 passed; nada afirma a variante escura |

## Gate

`npm --prefix web test -- ... (6 arquivos)` - 40 passed, 0 failed · `dotnet test` (C15, C16) - 2 passed, 0 failed · `playwright test e2e/marca.spec.ts` - 3 passed, 0 failed · `npm --prefix web run build` + one-liner do C17 - exit 0.

Não rodados: `crapper` e `dryer` (indisponíveis nesta máquina), `mutator` (suspenso, AD-025).

Gaps, em ordem:

1. Mutante sobrevivente F5 e variante escura da direção A sem check (`Acesso.tsx:55`). Correção: um check novo (por exemplo, a `img` da raiz tem as classes `dark:brightness-0 dark:invert`, ou um e2e com `colorScheme: 'dark'` que mede pixels claros no logo). Como checks aprovados não mudam, isto é decisão para quem aprovou o plano (ou aceitar o risco com um AD).
2. Ramo "`simbolo.svg` sem `<path d>`" (`gerar-icones.mjs:20-23`) sem teste. Observação, não exigido pelo plano.
3. C18 prova contra o servidor de dev do Vite, não contra o `Esclari.Api` servindo `wwwroot` (container :8080 desligado). Observação.
