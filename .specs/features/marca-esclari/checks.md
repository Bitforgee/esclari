# Logo e marca do Esclari checks

Profile: ui
Plan: `.specs/features/marca-esclari/plan.md`

26 checks em 4 slices · 5 portas de mão única · 0 perguntas abertas.

Comandos (todos rodam em `D:\esclari`): testes da web com `npm --prefix web test -- <arquivo> -t "<nome>"`; testes da API com `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~<Classe>.<Teste>"`; e2e da marca com o servidor de dev (`npm --prefix web run dev`, porta 5173) e `$env:E2E_URL='http://localhost:5173'; npm --prefix web exec playwright test e2e/marca.spec.ts -g "<nome>"`.

## Checks

### S1 - Arquivos da marca · ~12 arquivos · ~45 KB · ~14k

**C1** - `web/public/marca/simbolo.svg` tem `viewBox="0 0 64 64"`, exatamente um `<path fill="currentColor">` e um `<rect>` com `fill` marca-texto (`#ffd43b`), nenhum `mask`, `filter`, `gradient`, `<text>`, `<image>`, `<style>` e nenhuma outra cor em hexadecimal (AC 1, door 5)
Proof: `npm --prefix web test -- src/test/logo.test.ts -t "simbolo.svg tem um unico path com currentColor"`

**C2** - `web/public/marca/logo-horizontal.svg` e `logo-horizontal-escuro.svg` têm `viewBox` com largura/altura de pelo menos 3,5, ao menos 2 elementos `<path>` (símbolo e palavra) e nenhum `<text>`, `font-family` ou `<image>`; o claro usa letras `#0e1330` e o escuro letras `#ffffff` (AC 2, door 3, door 5)
Proof: `npm --prefix web test -- src/test/logo.test.ts -t "logo-horizontal.svg traz simbolo e palavra em curvas"`

**C3** - `favicon.svg` tem placa `#2438d6` e símbolo `#ffffff` com o braço marca-texto, e `web/index.html` declara `<link rel="icon" type="image/svg+xml" href="/marca/favicon.svg">` antes do `<link rel="icon" type="image/png">` de reserva (AC 3)
Proof: `npm --prefix web test -- src/test/logo.test.ts -t "favicon.svg e declarado antes do png"`

**C4** - Os quatro arquivos de `marca/` usam apenas as cores `#2438d6`, `#ffffff`, `#ffd43b`, `#0e1330` (ou `currentColor`) (door 5)
Proof: `npm --prefix web test -- src/test/logo.test.ts -t "a marca so usa as quatro cores"`

**C5** - `icon-192.png` é 192×192, `icon-512.png` e `icon-maskable-512.png` são 512×512 e `apple-touch-icon.png` é 180×180 (AC 4)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "dimensoes dos quatro icones"`

**C6** - `node scripts/gerar-icones.mjs` termina com código 0 e regrava os quatro PNG (AC 4)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "gera os quatro icones com exit 0"`

**C7** - Sem `web/public/marca/simbolo.svg`, o script termina com código 1, a mensagem cita `simbolo.svg` e os quatro PNG ficam byte a byte iguais (AC 5)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "sem simbolo.svg"`

**C8** - `icon-maskable-512.png` e `apple-touch-icon.png` têm alfa 255 em todos os pixels (AC 6)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "maskable e apple sao opacos"`

**C9** - Todo pixel de `icon-maskable-512.png` que não é a cor da placa (`#2438d6`) fica a no máximo 205 px do centro (raio de 40% de 512 = 204,8) e há pelo menos 1 pixel branco e 1 pixel marca-texto (AC 7)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "maskable dentro da zona segura"`

**C10** - `/icons/apple-touch-icon.png` existe em `web/public/icons/` e `web/index.html` tem `<link rel="apple-touch-icon" href="/icons/apple-touch-icon.png">` (AC 8, door 2)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "apple-touch-icon declarado"`

**C11** - Renderizado em 16×16, `favicon.svg` tem entre 15% e 45% de pixels com luminância acima de 200 (AC 9)
Proof: `npm --prefix web test -- src/test/logo.test.ts -t "favicon em 16 px"`

**C12** - Os três ícones que o manifesto e o push já usam continuam com os mesmos nomes: `/icons/icon-192.png` (`any`), `/icons/icon-512.png` (`any`), `/icons/icon-maskable-512.png` (`maskable`) no `vite.config.ts` (AC 12, door 2)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "manifesto lista os tres icones"`

### S2 - A marca onde só o Esclari aparece · ~6 arquivos · ~20 KB · ~7k

**C13** - A raiz `/` mostra, dentro do `h1`, uma `img` com `alt="Esclari"` e `src="/marca/logo-horizontal.svg"` no tema claro, e o nome acessível do `h1` é "Esclari" (AC 10)
Proof: `npm --prefix web test -- src/acesso/logo-esclari.test.tsx -t "raiz mostra o logo dentro do h1"`

**C14** - Com logo e ícone da instituição configurados, o login da Univag não mostra nenhuma `img` com `src` iniciado em `/marca/`; e nenhum arquivo de `web/src` (fora dos testes, de `Acesso.tsx` e de `aparencia.tsx`, que só põe o favicon da aba) nem de `Esclari.Api` (e-mail, push, manifesto) cita `/marca/` (AC 11)
Proof: `npm --prefix web test -- src/acesso/logo-esclari.test.tsx -t "marca do Esclari so aparece na raiz"`

**C15** - Sem ícone da instituição, `GET /api/instituicoes/{slug}/manifest.webmanifest` devolve `/icons/icon-192.png`, `/icons/icon-512.png` e `/icons/icon-maskable-512.png`, com tamanhos `192x192`, `512x512`, `512x512` (AC 12)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests.Manifesto_padrao_sem_aparencia"`

**C16** - Sem ícone da instituição, o push leva `"icone":"/icons/icon-192.png"` (AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PushAgendadorTests.Payload_exato_sem_dado_pessoal"`

**C17** - Depois de `npm --prefix web run build`, o `sw.js` gerado lista `marca/simbolo.svg`, `marca/logo-horizontal.svg`, `marca/logo-horizontal-escuro.svg` e `marca/favicon.svg` no precache (AC 14)
Proof: `npm --prefix web run build; node -e "const s=require('fs').readFileSync('Esclari.Api/wwwroot/sw.js','utf8');for(const f of ['simbolo','logo-horizontal','logo-horizontal-escuro','favicon'])if(!s.includes('marca/'+f+'.svg'))process.exit(1)"`

**C18** - `GET /marca/simbolo.svg`, `/marca/logo-horizontal.svg`, `/marca/logo-horizontal-escuro.svg` e `/marca/favicon.svg` respondem 200 com `image/svg+xml`, e `GET /icons/apple-touch-icon.png` responde 200 com `image/png` (Surface)
Proof: `$env:E2E_URL='http://localhost:5173'; npm --prefix web exec playwright test e2e/marca.spec.ts -g "arquivos da marca respondem 200"`

**C25** - Com ícone da instituição, o `<link rel="icon" type="image/svg+xml">` da marca sai de `<head>` e o PNG aponta para `/api/instituicoes/univag/imagens/icone?v=2`; sem ícone, o PNG volta a `/icons/icon-192.png` e o SVG volta a `/marca/favicon.svg` (AC 3, AC 11)
Proof: `npm --prefix web test -- src/acesso/logo-esclari.test.tsx -t "favicon da marca some com icone da instituicao e volta sem ele"`

**C26** - O `src` do logo da raiz acompanha o tema: `light` usa `/marca/logo-horizontal.svg`; `dark` e `black` usam `/marca/logo-horizontal-escuro.svg` (lacuna achada na rodada 1 do Verifier; AC 10, porta 5)
Proof: `npm --prefix web test -- src/acesso/logo-esclari.test.tsx -t "raiz troca o logo no tema escuro"`

### S3 - Guia de marca · 1 arquivo · ~6 KB · ~3k

**C19** - `guia-de-marca.md` tem os nove títulos "Nome", "Símbolo", "Logo horizontal", "Cor", "Área de respeito", "Tamanho mínimo", "Usos proibidos", "Tipografia" e "Voz" (AC 15)
Proof: `npm --prefix web test -- src/test/guia.test.ts -t "tem os nove titulos"`

**C20** - O guia diz área de respeito de "metade da altura do símbolo", tamanho mínimo "16 px" para o símbolo e "96 px" de largura para o logo horizontal, e a cor `#2438d6` (AC 16)
Proof: `npm --prefix web test -- src/test/guia.test.ts -t "traz as medidas e a cor"`

**C21** - A seção "Usos proibidos" tem pelo menos 5 itens de lista, cada um com a palavra "porque" (a razão) (AC 17)
Proof: `npm --prefix web test -- src/test/guia.test.ts -t "usos proibidos sao 5 com razao"`

**C22** - O guia contém "Esclari", `ESC-NNNNNN` e `esclari.com.br`, e não contém "ESCLARI" nem "o Esclari" nos títulos (AC 18)
Proof: `npm --prefix web test -- src/test/guia.test.ts -t "escreve o nome do jeito certo"`

### S4 - Celular e toque · 1 arquivo · ~3 KB · ~2k

**C23** - Em 360×780, a raiz `/` mostra o logo com 40 px de altura e `document.documentElement.scrollWidth` não passa de 360 (AC 19)
Proof: `$env:E2E_URL='http://localhost:5173'; npm --prefix web exec playwright test e2e/marca.spec.ts -g "raiz em celular 360x780"`

**C24** - Em 844×390, a raiz `/` mostra o logo e os dois parágrafos com `document.documentElement.scrollHeight` de no máximo 390 (AC 20)
Proof: `$env:E2E_URL='http://localhost:5173'; npm --prefix web exec playwright test e2e/marca.spec.ts -g "raiz deitada 844x390"`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| `GET /marca/simbolo.svg` statuses (1) | 200 C18 | - |
| `GET /marca/logo-horizontal.svg` statuses (1) | 200 C18 | - |
| `GET /marca/favicon.svg` statuses (1) | 200 C18 | - |
| `GET /icons/apple-touch-icon.png` statuses (1) | 200 C18 | - |
| `gerar-icones.mjs` exit codes (2) | 0 C6 · 1 C7 | - |
| PNG gerados (4) | `icon-192` C5 · `icon-512` C5 · `icon-maskable-512` C5, C8, C9 · `apple-touch-icon` C5, C8, C10 | - |
| Landing doors (5) | 1 símbolo (superseded) C1 · 2 nomes dos ícones C10, C12, C15, C16 · 3 palavra-marca em curvas C2 · 4 cor (superseded) C4 · 5 símbolo v2 e cores C1, C3, C4, C9, C11, C20 | - |
| Relations entities (0) | `None - no stored-data shape change` | - |
| Observable: raiz `/`, itens com critério (4; vazio, carregando, erro, confirmação, teclado e toque são `n/a` no plano) | offline C17 · celular C23 · deitado C24 · acessibilidade do logo C13 | - |
| Guia, seções (9) | Nome C19 · Símbolo C19 · Logo horizontal C19 · Cor C19 · Área de respeito C19 · Tamanho mínimo C19 · Usos proibidos C19 · Tipografia C19 · Voz C19 | - |
| Guia, medidas (4) | respeito C20 · símbolo 16 px C20 · logo 96 px C20 · cor C20 | - |
| Identidade da instituição prevalece (6) | login C14 · aba do navegador C25 · sidebar C14 · e-mail C14 · manifesto C15 · push C16 | - |

- Checks que citam status, rota ou forma de resposta: C15, C16, C18 - cada um cruza a fronteira (API real ou servidor de dev)
- Nenhum outro check afirma mais do que o caso que a prova exercita

## Test policy

| Code | Required proofs | Coverage expectation |
| --- | --- | --- |
| `web/scripts/gerar-icones.mjs` (decide: entrada ausente, escrita atômica) | um no próprio nível (spawn do script) | cada saída: 0 (C6) e 1 por arquivo ausente (C7) |
| `Raiz` em `Acesso.tsx` (decide o arquivo do logo pelo tema) | um no nível da tela | o logo, o `alt`, o nome do `h1` (C13) e uma linha por tema (C26) |
| Arquivos estáticos de `marca/` e `icons/` (sem código) | um na fronteira (servidor) | status e tipo de cada arquivo (C18) |

Evidence:

- `gerar-icones.mjs`: dois ramos (`theme_color` ausente, Chromium ausente) hoje; ganha o ramo `simbolo.svg` ausente (3 pontos de decisão) -> decide
- `Acesso.tsx` `Raiz`: um ponto de decisão (o arquivo do logo conforme o tema, três valores de tema: `light`, `dark`, `black`) -> decide, provado em C26 no próprio nível
- mais próximo no repositório: `web/src/test/icones.test.ts`, mesma forma (spawn do script e PNG), já prova o script com 5 casos

Cost: 3 provas no nível próprio em 2 arquivos. Estes rows não vão para as diretrizes do repositório; vale só para esta feature.

## Swept

- validation: C1, C2, C3, C4, C8, C9 (formato do SVG, cor, opacidade e zona segura)
- failure modes: C7 (símbolo ausente deixa os PNG como estavam); logo que não carrega cai no `alt` do C13
- idempotency: n/a - o script regrava os mesmos bytes a partir do mesmo SVG; nada acumula
- authorization: n/a - arquivos estáticos públicos, sem sessão (plano, `## Observable`)
- concurrency: n/a - sem estado compartilhado; o script usa `renameSync` para trocar cada PNG de uma vez (existing)
- data lifecycle: n/a - sem dado armazenado
- dependency failure: existing - o Chromium ausente já sai com 1 e deixa os ícones (`icones.test.ts`, "falha do chromium")
- state transitions: n/a - nenhuma máquina de estados
- observability: n/a - sem requisito de log; o script já imprime "icones: gravado <arquivo>" por PNG (C6)

## Out of scope

Herdado do plano: INPI, rodapé "Atendimento por Esclari", Open Graph, logo animado, nova paleta, iniciais da instituição, unificar `#224fa7`, avatar e nome do assistente.

## Handoff

- S1 ~14k, S2 ~7k, S3 ~3k, S4 ~2k = ~26k de tokens, soma abaixo do orçamento de 150k, em uma fatia da web (`web/public`, `web/scripts`, `web/src/test`, `web/src/acesso`, `web/e2e`) mais um guia
- Mechanism: one builder (a estimativa cabe no orçamento)
- **Settled mid-build:** `web/src/aparencia.test.tsx` (existente) ganhou duas linhas de expectativa trocadas, sem enfraquecer nada: o `apple-touch-icon` padrão passa a ser `/icons/apple-touch-icon.png` (AC 8) e o PNG da aba é achado por `link[rel="icon"][type="image/png"]` porque o `index.html` agora tem dois `link rel=icon`. `aparencia.tsx` ganhou `faviconDaMarca` para o SVG não sobrar quando a instituição tem ícone (C25). `web/src/acesso/textos.test.tsx` perdeu o texto "Esclari" da raiz e ganhou a checagem do `h1` com a `img`.

## Amendment 1 (17/11)

O usuário achou a marca fraca e aprovou a proposta v2 (`proposta-v4.png`). Isso troca o símbolo, a cor e a palavra-marca (porta 5 em `plan.md`) e muda estes checks, antes de qualquer código: C1, C2, C3, C4, C9, C13, C17, C18, C20 e C26 (o texto acima já é o novo). Os números e as provas seguem os mesmos, exceto o nome do teste de C4 e de C26. As provas dos demais não mudam. Dois pontos que o usuário aceitou junto: o azul padrão da interface (`web/src/index.css`) e o preset "Azul Esclari" da tela Aparência continuam `#224fa7` até um passo separado, e o logo claro e o escuro viram dois arquivos porque o marca-texto não sobrevive a inverter cores com CSS; a raiz escolhe o arquivo pelo tema com `useTema` (em `aparencia.tsx`), e não por classes CSS, para o `h1` ter uma só `img`.
