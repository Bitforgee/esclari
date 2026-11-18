# Logo e marca do Esclari verification

**Verdict**: PASS (rodada 3: o unico reprovado, C3, foi superado pelo redesign AD-062; ver `## Rodada 3`)
**Profile**: ui
**Diff range**: e39d84d..HEAD (HEAD = eddecf9)
**Round**: 2 - full
**Verifier**: independent sub-agent (author != verifier)

A rodada 1 foi FAIL na variante escura do logo (logo claro no tema escuro); a lacuna virou o C26, e depois o usuario aprovou o redesenho (porta 5, `Amendment 1`), que esta rodada verifica inteiro. Esta rodada e FAIL por um mutante sobrevivente no C3 (o braco marca-texto do `favicon.svg` nao tem assercao). Os 26 checks tem prova verde em `HEAD`.

## Binding sources

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `plan.md` (Surface, Landing porta 5, criterios) | yes - lido inteiro | none contra os checks. Nota: os criterios AC 1, 3, 16 e a Surface ainda dizem `#224fa7`, "balao" e "um `<path>`"; a porta 5 e o Amendment 1 os supersedem e os checks seguem a porta 5. O plano nao foi emendado nos criterios (lacuna de precisao, minor) | - |
| `proposta-v4.png` (E com coluna e dois bracos Ultramar, braco do meio marca-texto solto; "esclari" em Sora SemiBold minuscula, pingo do "i" amarelo; claro, escuro, placa 64/40/24, logo na raiz) | yes - imagem aberta e comparada com a renderizacao de `web/public/marca/*.svg` e `web/public/icons/*.png` | none: E, braco amarelo em pilula, pingo amarelo, variante escura (E e letras brancas, amarelo igual), placa azul com E branco batem | favicon: o braco marca-texto de `favicon.svg` nao tem check que o exija (ver C3) |
| `direcoes-2.png`, `direcoes-3.png` (alternativas rejeitadas) | yes - listadas na pasta; a escolha esta registrada na porta 5 | none | - |
| `pesquisa.md` | yes - lido | none (tamanho minimo 16 px, zona segura 40%, apple-touch 180 batem com C5, C9, C20) | - |
| `AGENTS.md` | yes - regras injetadas lidas | none: textos em pt-BR, sem comentario no codigo, testes em portugues; `crapper`/`dryer` nao rodados (indisponiveis nesta maquina); `mutator` suspenso (AD-025) | - |

Julgamento visual (renderizado em Chromium, comparado com `proposta-v4.png`):

- Minor (cosmetico): em `logo-horizontal.svg` e `logo-horizontal-escuro.svg` o terminal do "r" encosta no tronco do "i" (ampliado a 300 px de altura: os dois se fundem no topo). A `proposta-v4.png` mostra o "r" e o "i" muito proximos; no 24 px do logo da raiz ler "ri" funciona, mas o espaco entre as letras e menor que o das outras. Nenhum check cobre o espacamento.
- Sem defeito: E, braco amarelo, pingo amarelo, variante escura, `icon-192`/`icon-512` (placa arredondada), `icon-maskable-512` e `apple-touch-icon` (placa cheia, E centrado). `simbolo.svg` aberto em `<img>` sai preto porque `currentColor` nao herda em `<img>`; e o contrato da porta 5, nao defeito.

## Checks

Todas as provas rodadas em `HEAD`: Vitest `src/test/logo.test.ts src/test/icones.test.ts src/test/guia.test.ts src/acesso/logo-esclari.test.tsx src/acesso/textos.test.tsx src/aparencia.test.tsx` (6 arquivos, 41 testes, 0 falhas; os 23 testes dos arquivos novos listados um a um no reporter JSON); `dotnet test` (5 aprovados: `Manifesto_padrao_sem_aparencia`, `Payload_exato_sem_dado_pessoal`, tres `EmailMarcaTests`); `npm --prefix web run build` + one-liner do C17 (exit 0); Playwright `e2e/marca.spec.ts` contra o dev server na 5173 (3 passed). `git status --porcelain` depois: so `.tmp-capturas/`.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | `simbolo.svg`: viewBox 64, 1 path currentColor, 1 rect `#ffd43b`, sem mask/filter/etc., so uma cor hex | vitest `logo.test.ts` "simbolo.svg tem um unico path com currentColor" exit 0 | `web/src/test/logo.test.ts:24` `expect(contar(svg, /<path\b/g)).toBe(1)`; `:26` `contar(svg, /<rect\b/g)).toBe(1)`; `:30` `expect(cores).toEqual([MarcaTexto])` | PASS |
| C2 | os dois logos: razao >= 3,5, >= 2 paths, sem text/font-family/image; letras `#0e1330` claro e `#ffffff` escuro | vitest "logo-horizontal.svg traz simbolo e palavra em curvas" | `logo.test.ts:37` `expect(largura / altura, arquivo).toBeGreaterThanOrEqual(3.5)`; `:38` `contar(svg, /<path\b/g)...toBeGreaterThanOrEqual(2)`; `:40` `expect(svg, arquivo).toContain(\`fill='${letras}'\`)`; `:41` marca-texto | PASS |
| C3 | `favicon.svg` placa `#2438d6`, simbolo `#ffffff` **com o braco marca-texto**; `index.html` declara SVG antes do PNG | vitest "favicon.svg e declarado antes do png" | `logo.test.ts:47` `toContain(\`fill='${Ultramar}'\`)`; `:48` `toContain(\`fill='${Branco}'\`)`; `:52` `expect(iconePng).toBeGreaterThan(iconeSvg)`. Nenhuma assercao exige `#ffd43b` em `favicon.svg` (busca: so `:41` e `:27`, em outros arquivos). Mutante: remover o `<rect fill='#ffd43b'>` do favicon, 5/5 testes seguem verdes | FAIL |
| C4 | os 4 SVG so usam as quatro cores | vitest "a marca so usa as quatro cores" | `logo.test.ts:57` `expect(arquivos.sort()).toEqual([...4 nomes])`; `:61` `expect([Ultramar, Branco, MarcaTexto, Tinta, 'currentcolor'], ...).toContain(cor)` | PASS |
| C5 | PNG 192, 512, 512, 180 | vitest "dimensoes dos quatro icones" | `web/src/test/icones.test.ts:132` `expect([nome, imagem.largura, imagem.altura]).toEqual([nome, lado, lado])` com 192/512/512/180 em `:129` | PASS |
| C6 | script exit 0 e regrava os 4 PNG | vitest "gera os quatro icones com exit 0" | `icones.test.ts:153` `expect(r.status, r.stderr).toBe(0)`; `:154` `expect(r.stdout).toContain(\`icones: gravado ${nome}\`)` x4. `git status --porcelain` depois: nada rastreado mudou (bytes identicos) | PASS |
| C7 | sem `simbolo.svg`: exit 1, mensagem cita o arquivo, PNG iguais | vitest "sem simbolo.svg" | `icones.test.ts:167` `expect(r.status).toBe(1)`; `:168` `expect(r.stderr).toContain('simbolo.svg')`; `:170` `expect(nomes.map(hash)).toEqual(antes)` | PASS |
| C8 | maskable e apple com alfa 255 em todo pixel | vitest "maskable e apple sao opacos" | `icones.test.ts:140` `expect(imagem.alfa(x, y), ...).toBe(255)` em todos os pixels dos dois arquivos | PASS |
| C9 | fora da placa `#2438d6`, todo pixel a <= 205 px do centro; >= 1 branco e >= 1 amarelo | vitest "maskable dentro da zona segura" | `icones.test.ts:91` `...toBeLessThanOrEqual(raio)` (raio 205); `:92` `expect(ehCorDaMarca(p)).toBe(true)` fora do raio; `:94` `brancos > 0`; `:95` `expect(amarelos).toBeGreaterThan(0)` | PASS (ver Faults: le o PNG commitado, nao o gerador) |
| C10 | `apple-touch-icon` existe e `index.html` o declara | vitest "apple-touch-icon declarado" | `icones.test.ts:145` `toContain('<link rel="apple-touch-icon" href="/icons/apple-touch-icon.png" />')`; `:147` `expect(ehCorDaMarca(imagem.pixel(0, 0))).toBe(true)` | PASS |
| C11 | favicon a 16 px: 15% a 45% de pixels claros | vitest "favicon em 16 px" | `logo.test.ts:91` `expect(proporcao).toBeGreaterThanOrEqual(0.15)`; `:92` `toBeLessThanOrEqual(0.45)` | PASS |
| C12 | manifesto lista os 3 icones, mesmos nomes e `purpose` | vitest "manifesto lista os tres icones" | `icones.test.ts:177` `expect(icones).toEqual([['/icons/icon-192.png','any'],['/icons/icon-512.png','any'],['/icons/icon-maskable-512.png','maskable']])` | PASS |
| C13 | raiz: `h1` com `img` alt "Esclari", src claro, nome "Esclari" | vitest `logo-esclari.test.tsx` "raiz mostra o logo dentro do h1" | `web/src/acesso/logo-esclari.test.tsx:33` `findByRole('heading', { level: 1, name: 'Esclari' })`; `:35` `toHaveAttribute('src', '/marca/logo-horizontal.svg')`; `:36` `toHaveAttribute('alt', 'Esclari')`. Codigo: `Acesso.tsx:56` | PASS |
| C14 | login da Univag sem `img` `/marca/`; so `Acesso.tsx` e `aparencia.tsx` citam `/marca/`; API nao cita | vitest "marca do Esclari so aparece na raiz" | `logo-esclari.test.tsx:54` `expect(imagem.getAttribute('src')).not.toMatch(/^\/marca\//)`; `:58` `expect(citam.sort()).toEqual(['Acesso.tsx', 'aparencia.tsx'])`; `:60` `expect(api_.filter(...)).toEqual([])` | PASS |
| C15 | manifesto padrao: 3 caminhos e tamanhos 192/512/512 | `dotnet test --filter ManifestoTests.Manifesto_padrao_sem_aparencia` exit 0 (Aprovado) | `Esclari.Api.Tests/ManifestoTests.cs:52` `Assert.Equal(["/icons/icon-192.png", "/icons/icon-512.png", "/icons/icon-maskable-512.png"], ...)`; `:53` `Assert.Equal(["192x192", "512x512", "512x512"], ...)` | PASS |
| C16 | push padrao leva `"icone":"/icons/icon-192.png"` | `dotnet test --filter PushAgendadorTests.Payload_exato_sem_dado_pessoal` exit 0 (Aprovado) | `Esclari.Api.Tests/PushAgendadorTests.cs:89` `Assert.Equal("""{"titulo":"Univag",...,"icone":"/icons/icon-192.png"}""", payload)` | PASS |
| C17 | `sw.js` gerado lista os 4 SVG no precache | `npm --prefix web run build` + one-liner exit 0 | one-liner `process.exit(1)` se faltar `marca/<f>.svg`; saida 0; `sw.js` contem `marca/favicon.svg`, `logo-horizontal-escuro.svg`, `logo-horizontal.svg`, `simbolo.svg` (regex no arquivo gerado) | PASS |
| C18 | os 4 SVG e o apple-touch respondem 200 com o tipo certo | Playwright `marca.spec.ts` "arquivos da marca respondem 200" (3 passed) | `web/e2e/marca.spec.ts:14` `expect(resposta.status(), caminho).toBe(200)`; `:15` `expect(resposta.headers()['content-type'], caminho).toContain(tipo)`; lista em `:3-9` com 5 caminhos | PASS (servidor de dev, nao o Kestrel; ver nota) |
| C19 | nove titulos do guia | vitest `guia.test.ts` "tem os nove titulos" | `web/src/test/guia.test.ts:11` `expect(titulos).toEqual(['Nome','Simbolo',...,'Voz'])` (guia: `## ` nas linhas 5 a 51) | PASS |
| C20 | medidas e cor do guia | vitest "traz as medidas e a cor" | `guia.test.ts:15` `toContain('metade da altura do simbolo')`; `:16` `toMatch(/simbolo[^.]*16 px/)`; `:17` `toMatch(/logo horizontal[^.]*96 px de largura/)`; `:18` `toContain('#' + '2438d6')` | PASS |
| C21 | >= 5 usos proibidos, cada um com "porque" | vitest "usos proibidos sao 5 com razao" | `guia.test.ts:23` `expect(itens.length).toBeGreaterThanOrEqual(5)`; `:24` `expect(item, item).toContain('porque')` | PASS |
| C22 | nome certo, `ESC-NNNNNN`, `esclari.com.br`, sem "ESCLARI" | vitest "escreve o nome do jeito certo" | `guia.test.ts:29` `toContain('ESC-NNNNNN')`; `:30` `toContain('esclari.com.br')`; `:31` `expect(guia).not.toContain('ESCLARI')` | PASS |
| C23 | 360x780: logo com 40 px, sem rolagem horizontal | Playwright "raiz em celular 360x780" | `marca.spec.ts:25` `expect(Math.round(caixa.height)).toBe(40)`; `:26` `expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(360)` | PASS |
| C24 | 844x390: logo e dois paragrafos, `scrollHeight` <= 390 | Playwright "raiz deitada 844x390" | `marca.spec.ts:35` `expect(await page.evaluate(() => document.documentElement.scrollHeight)).toBeLessThanOrEqual(390)`; `:32-34` logo e dois textos visiveis | PASS |
| C25 | com icone da instituicao o SVG sai e o PNG aponta para o icone; sem icone volta | vitest "favicon da marca some com icone da instituicao e volta sem ele" | `logo-esclari.test.tsx:67` `toHaveAttribute('href', iconeDaInstituicao)`; `:68` `expect(noHead('link[rel="icon"][type="image/svg+xml"]')).toBeNull()`; `:73` `href '/icons/icon-192.png'`; `:74` `toHaveAttribute('href', '/marca/favicon.svg')` | PASS |
| C26 | `light` usa o logo claro; `dark` e `black` usam o escuro | vitest "raiz troca o logo no tema escuro" | `logo-esclari.test.tsx:44` `expect(titulo.querySelector('img'), tema).toHaveAttribute('src', '/marca/' + arquivo)` para light, dark, black (`:40`). Codigo: `Acesso.tsx:52` `useTema()`, `:56` ternario | PASS |

Nota C18: a prova roda contra o dev server do Vite (como o checks.md manda), nao contra o Kestrel com `wwwroot`; o `Content-Type` do Kestrel para `.svg` nao foi exercido. Registrado como precisao, nao como falha.

Edicoes em testes existentes (julgamento 1), todas sem enfraquecer:

- `web/src/aparencia.test.tsx:149-150`: `apple-touch-icon` `'/icons/icon-192.png'` -> `'/icons/apple-touch-icon.png'` (AC 8) e o seletor `link[rel="icon"]` -> `link[rel="icon"][type="image/png"]` com o mesmo valor `/icons/icon-192.png`. A linha `:148` (que compara o `link[rel="icon"]` com o do `base`) segue como estava. Mesma forca.
- `web/src/acesso/textos.test.tsx:54,57`: o texto "Esclari" saiu da lista de textos da raiz porque agora e uma `img`; entrou `getByRole('heading', { level: 1, name: 'Esclari' })` contendo `getByRole('img', { name: 'Esclari' })` (`:57`). Equivalente ou mais forte.
- `web/src/test/icones.test.ts`: `corDaMarca` `#224fa7` -> `#2438d6` (muda o valor esperado, acompanha a porta 5; as assercoes que a usam seguem); entrou `marcaTexto` e `amarelos > 0` (`:95`). Nenhuma assercao removida ou afrouxada.
- `Esclari.Api.Tests/EmailMarcaTests.cs:82` e `ManifestoTests.cs:49`: cor padrao esperada `#224fa7` -> `#2438d6`, acompanhando `LayoutEmail.CorPadrao` e `ManifestoInstituicao.CorPadrao`. Nao enfraquecem.

Julgamento 3 (cores padrao da API): `LayoutEmail.CorPadrao` e provada por `EmailMarcaTests.cs:82` (`Assert.Contains("background-color:#2438d6", simples)`) e `ManifestoInstituicao.CorPadrao` por `ManifestoTests.cs:49` (`Assert.Equal("#2438d6", ...theme_color)`); a mutacao 5 abaixo derrubou o segundo. Nenhum check C1..C26 as nomeia (a cor e a porta 5, mas os dois testes sao so de testes existentes editados); lacuna de rastreio, minor.

Julgamento 4: o azul padrao da interface (`web/src/index.css`) e o preset "Azul Esclari" continuam `#224fa7`; isso esta declarado no `Amendment 1` de `checks.md` ("continuam `#224fa7` ate um passo separado") e em `guia-de-marca.md:26`. Aceito pelo usuario; nao conta como achado.

Julgamento 2: `crapper` e `dryer` nao estao disponiveis nesta maquina: **nao rodados**. `mutator` suspenso (AD-025): nao rodado, check de mutacao fica aberto como "suspenso por decisao do usuario".

## Coverage

Recomputado a partir da Surface, Landing, criterios e guia do plano (nao da tabela do autor).

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| Surface, rotas e status (4 rotas, todas `200`; mais `logo-horizontal-escuro.svg`, adicionado pela porta 5 = 5 arquivos) | `plan.md` Surface + `marca.spec.ts:3-9` | simbolo, logo, logo-escuro, favicon, apple-touch: 200 e tipo em `marca.spec.ts:14-15` (C18) | - (o escuro nao esta na Surface do plano: lacuna de precisao do plano, nao do check) |
| `gerar-icones.mjs` saidas (3: exit 0; exit 1 sem `simbolo.svg`; exit 1 sem Chromium; o ramo `theme_color` ausente e `<path>` ausente existem no codigo) | `gerar-icones.mjs` | 0 -> C6; 1 simbolo -> C7; 1 chromium -> `icones.test.ts:122` (existente) | ramos `theme_color` ausente e "simbolo sem `<path>`" (`gerar-icones.mjs`) sem teste; nao sao check do plano (AC 5 so nomeia o arquivo ausente) - precisao, nao falha |
| PNG gerados (4) | `ICONES` em `gerar-icones.mjs` | `icon-192` C5; `icon-512` C5; `icon-maskable-512` C5, C8, C9; `apple-touch-icon` C5, C8, C10 | - |
| SVG da marca (4) | `web/public/marca/` | simbolo C1, C4; logo C2, C4, C18; logo-escuro C2, C4, C18, C26; favicon C3, C4, C11, C18, C25 | `favicon.svg`: cor marca-texto (C3) |
| Landing, portas (5) | `plan.md` Landing | 1 e 4 superseded; 2 -> C10, C12, C15, C16; 3 -> C2; 5 -> C1, C3, C4, C9, C11, C20 | porta 5, "braco marca-texto no favicon" (C3) |
| Cores da marca (4) | porta 5 | `#2438d6` C3/C4/C9; `#ffd43b` C1/C2/C4/C9; `#0e1330` C2/C4; `#ffffff` C2/C3/C4 | `#ffd43b` em `favicon.svg` |
| Temas do logo da raiz (3: light, dark, black) | `Acesso.tsx:56` e `useTema` | light, dark, black em C26 (`logo-esclari.test.tsx:40`) | - (o tema `system` passa por `useTema`, sem teste proprio; precisao) |
| Estados da raiz `/` (4 com criterio; os demais `n/a` com motivo no plano) | `plan.md` Observable | offline C17; 360x780 C23; 844x390 C24; acessibilidade C13 | - |
| Linhas do `Observable` com "teclado aberto" e "toque" | `plan.md` Observable | `n/a` com motivo (sem campo, sem controle) | - |
| Guia, secoes (9) e medidas (4) | `guia-de-marca.md` | secoes C19; respeito, 16 px, 96 px, cor C20; usos proibidos C21; nome C22 | - |
| Identidade da instituicao prevalece (6) | AC 11 | login C14; aba C25; sidebar C14; e-mail C14 e `EmailMarcaTests.cs:82`; manifesto C15; push C16 | - |

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| `gerar-icones.mjs` (decide) | `web/scripts/gerar-icones.mjs` | um no proprio nivel (spawn): C6 e C7 | yes - exit 0 (`icones.test.ts:153`) e exit 1 (`:167`) |
| `Raiz` em `Acesso.tsx` (decide o logo pelo tema) | `web/src/acesso/Acesso.tsx` | um no nivel da tela: C13 e uma linha por tema (C26) | yes - `logo-esclari.test.tsx:35` e `:44` (light, dark, black); a mutacao 2 abaixo foi morta |
| Arquivos estaticos de `marca/` e `icons/` | `web/public/marca/*`, `web/public/icons/*` | um na fronteira (servidor): C18 | yes - 5 arquivos com status e tipo (`marca.spec.ts:14-15`), contra o servidor de dev |

## Faults injected

Worktree em `%TEMP%\wt-marca` (e `wt-marca2`), `node_modules` por juncao, sem `git stash`. Descartado; `git worktree list` nao mostra os meus e `git status --porcelain` do arbol real: so `.tmp-capturas/` (igual ao inicio). Os outros worktrees listados (`v2-wt-api`, `v2-wt-web`, `verif-m1`) ja existiam e nao sao meus.

| Mutation | Location | Killed |
| --- | --- | --- |
| escala maskable `0.68` -> `0.95` | `web/scripts/gerar-icones.mjs` (`ESCALA`) | yes, so na segunda execucao: na 1a rodada de `icones.test.ts` 10/10 passam (o teste de zona segura le o PNG commitado antes de o teste "gera os quatro icones" regrava-lo); na 2a rodada `maskable dentro da zona segura` falha. Ver achado 2 |
| `Raiz` sempre usa o logo claro | `web/src/acesso/Acesso.tsx:56` | yes - `logo-esclari.test.tsx` "raiz troca o logo no tema escuro" falha (1 failed, 3 passed) |
| `faviconDaMarca` vira no-op (`remove()` -> `void atual`) | `web/src/aparencia.tsx` (`faviconDaMarca`) | yes - 2 falhas em `logo-esclari.test.tsx`/`aparencia.test.tsx` |
| tirar o `<rect>` marca-texto de `simbolo.svg` | `web/public/marca/simbolo.svg` | yes - `logo.test.ts` "simbolo.svg tem um unico path com currentColor" falha |
| `ManifestoInstituicao.CorPadrao` volta a `#224fa7` | `Esclari.Api/Instituicoes/Carga/ManifestoInstituicao.cs` | yes - `ManifestoTests.Manifesto_padrao_sem_aparencia` falha (1 falha, 9 aprovados) |
| (extra, tirada do cap para provar um achado) tirar o `<rect fill='#ffd43b'>` de `favicon.svg` | `web/public/marca/favicon.svg` | **no** - `logo.test.ts` 5/5 passam: nenhuma assercao exige o braco amarelo no favicon (C3, C4 so limita as cores permitidas, C11 mede luminancia e o amarelo `#ffd43b` tem luminancia ~210) |

## Gate

`npm --prefix web test -- <6 arquivos>` - 41 passed, 0 failed · `dotnet test Esclari.Api.Tests --filter "ManifestoTests.Manifesto_padrao_sem_aparencia|PushAgendadorTests.Payload_exato_sem_dado_pessoal|EmailMarcaTests"` - 5 passed, 0 failed · `npm --prefix web run build` + one-liner C17 - exit 0 · `playwright test e2e/marca.spec.ts` (E2E_URL 5173) - 3 passed. `crapper`/`dryer`: nao rodados (indisponiveis nesta maquina). `mutator`: suspenso (AD-025).

## Ranked gaps

1. **C3 / mutante sobrevivente** (FAIL): o braco marca-texto de `favicon.svg` (`#ffd43b`) nao e exigido por nenhuma assercao. `web/src/test/logo.test.ts:45-53` so confere `#2438d6` (`:47`) e `#ffffff` (`:48`); retirar o `<rect>` amarelo mantem os 5 testes verdes. O check C3 diz "simbolo `#ffffff` com o braco marca-texto". Correcao minima: acrescentar em `logo.test.ts` `expect(svg).toContain(\`fill='${MarcaTexto}'\`)` ao teste de C3.
2. **C9 / precisao** (minor): `maskable dentro da zona segura` le o PNG commitado; mudar a escala em `gerar-icones.mjs` so e pego depois que o teste do C6 regrava os PNG e outra rodada le o resultado (a 1a rodada fica verde). Nao ha prova que o PNG commitado e o que o script gera hoje (C6 nao compara bytes). Sugestao: o teste do C6 comparar o hash dos PNG regravados com o `git`/valor anterior, ou o do C9 gerar em pasta temporaria.
3. **Marca / cosmetico** (minor): o "r" encosta no "i" nos dois logos horizontais (`web/public/marca/logo-horizontal.svg`, `logo-horizontal-escuro.svg`); sem check de espacamento.
4. **Plano / precisao** (minor): `plan.md` AC 1, 3, 16 e a Surface seguem com `#224fa7`, balao e "um `<path>`"; a porta 5 e o `Amendment 1` os supersedem so nos checks. `logo-horizontal-escuro.svg` nao esta na Surface. As cores padrao da API (`LayoutEmail`, `ManifestoInstituicao`) so tem prova em testes existentes editados, sem check proprio.
5. **C18 / nivel** (minor): a prova roda no servidor de dev; o `Content-Type` do Kestrel para os SVG nao e exercido.

## Rodada 3 (escopada, fechamento)

Verificador independente, sem corrigir codigo nem testes. HEAD atual, depois de 108f95e, 643666e, 55eaed4, 49cba64.

| Check | Resultado | Evidencia |
| --- | --- | --- |
| C3 (braco marca-texto do avicon.svg) | SUPERADO | 643666e (AD-062, edesign-marca-esclari-final) trocou o favicon: hoje web/public/marca/favicon.svg e o Fio de procedencia, placa #C83A23 e simbolo #FFFFFF num unico path, sem braco amarelo. A exigencia de "braco marca-texto" nao existe mais. Antes disso, 108f95e ja tinha acrescentado a asserção do braco amarelo em logo.test.ts (o mutante da rodada 2 foi coberto antes de ser superado). |
| Marca atual (avicon.svg cores Origem+Branco, favicon em 16 px, <link rel=icon svg> antes do png) | PASS | 
px vitest run src/test/logo.test.ts src/acesso/logo-esclari.test.tsx: 2 arquivos, 13 testes, 0 falhas (inclui "favicon preserva volta e extremidades em 16 px"). |
| C26 (logo da raiz segue o tema) | PASS | os 4 testes de logo-esclari.test.tsx passam no HEAD. |
| C1, C2, C4 (simbolo/logo com #ffd43b, #2438d6, Ultramar) | SUPERADO | descrevem a marca da porta 5, substituida pelo AD-062 (cores Tinta/Origem). Nao rodados. |
| Demais checks (icones, guia, aparencia, .NET EmailMarcaTests, manifesto, e2e marca.spec.ts, C17 build) | NAO VERIFICADO | fora do escopo da rodada (pedido: sem suite completa, sem e2e, sem build, sem dotnet). Os que citam a marca antiga foram reescritos pelo AD-062/AD-063..065, cuja verificacao propria (interface-marca-esclari, PASS) os cobre. |

Fora desta rodada: Playwright, build, dotnet test, crapper/dryer. Nada reprovado e aplicavel ficou sem prova; o FAIL da rodada 2 virou PASS por superacao do C3.
