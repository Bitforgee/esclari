# Interface com a identidade do Esclari checks

Profile: ui
Plan: `.specs/features/interface-marca-esclari/plan.md`
Base: `49cba64`

21 checks em 3 slices · 3 portas de mão única · 0 perguntas abertas.

Plano aprovado pelo dono em 17/11/2026 ("aprovo o plano"), com as duas recomendações: neutros quentes para todas as instituições e Origem fora do item ativo da barra lateral.

Comandos rodam em `D:\esclari`. Testes web: `npm --prefix web test -- <arquivo> -t "<nome>"` (PowerShell em `C:` maiúsculo, `--maxWorkers=4` na suíte inteira). E2E: `docker compose up -d --build` e `npm --prefix web run e2e -- <spec>` contra :8080. Provas do repositório: `python .specs/features/interface-marca-esclari/provar_interface.py -k <nome>`.

Tinta = `#15133A` = `oklch(0.216 0.073 281)`; Papel = `#F7F5EE` = `oklch(0.970 0.010 94)`; Origem = `#C83A23`; Origem clara = `#F16A4B`. Hexadecimais são comparados sem diferenciar caixa: o `web/` grava em minúsculas (`#f7f5ee`, `#0e0e18`), como o `#fafbfd` de hoje, e a API em maiúsculas, como o `CorPadrao`.

## Checks

### S1 - Paleta da interface · 9 arquivos · ~150 KB · ~38k

**C1** - `web/src/index.css` declara em `:root`, `.dark` e `.black` exatamente os valores da door 1 nos 31 tokens da tabela `Tokens` de `interface.test.ts` (os 22 de hoje mais `--card-foreground`, `--popover-foreground`, `--sidebar-primary`, `--sidebar-foreground`, `--sidebar-primary-foreground`, `--sidebar-accent-foreground`, `--secondary`, `--secondary-foreground` e `--accent-foreground`), com as porcentagens de tingimento de hoje (claro: background 2, sidebar 4, sidebar-accent 5, sidebar-border 6, muted 4, accent 5, border 6, input 6; escuro: 3, 3, 6, 5, 5, 6, 5, 5; preto: sidebar-accent 6, sidebar-border 5, muted 4, accent 6, border 5, input 5); os tokens que a door 1 não lista (`--secondary*`, `--accent-foreground`, `--sidebar-foreground`, `--sidebar-primary-foreground`, `--sidebar-accent-foreground`, `--warning-subtle`, `--success`, `--destructive` escuro, `--ring` escuro, textos do escuro e do preto fora de `--foreground`) mantêm o valor da base (AC 1, door 1)
Proof: `npm --prefix web test -- src/test/interface.test.ts -t "tokens da paleta"`
Proof: `npm --prefix web test -- src/test/interface.test.ts -t "tokens de Visual"`

**C2** - `index.css` não contém `oklch(0.45 0.15 262)`, `oklch(0.72 0.12 262)` nem `oklch(0.96 0.02 262)`, e em todo `color-mix(in oklch, var(--marca, X) N%, Y)` o fallback `X` é igual à base `Y` (27 declarações: 10 em `:root`, 10 em `.dark`, 6 em `.black`, 1 no `.form-resposta`) (AC 1, door 2)
Proof: `npm --prefix web test -- src/test/interface.test.ts -t "fallback de cada neutro e a propria base"`

**C3** - Sem cor da instituição, num Chromium real, o fundo do botão primário, a borda de 3 px do item ativo da barra lateral e a cor do link `text-primary` saem `#15133A` no tema Claro e `#F7F5EE` nos temas Escuro e Preto, com tolerância de 2 por canal (AC 2)
Proof: `npm --prefix web run e2e -- interface-marca.spec.ts -g "sem cor da instituicao a acao e Tinta ou Papel"`

**C4** - Com cor da instituição `#8a1538`, num Chromium real, o fundo do botão primário e a borda do item ativo saem `#8a1538` no Claro, e o fundo do app sai diferente de `#F7F5EE` (tingido) (AC 3)
Proof: `npm --prefix web run e2e -- interface-marca.spec.ts -g "com cor da instituicao a acao segue a cor"`

**C5** - `--primary`, `--sidebar-primary`, `--primary-subtle` e `--ring` de `:root` e `.dark` e `--primary-subtle` de `.black` derivam de `var(--marca)`; `--marca-clara` é `color-mix(in oklch, var(--marca) 55%, white)`; `--primary` escuro começa por `var(--marca-clara, `; `--primary-foreground` escuro é `oklch(0.216 0.073 281)`; os 14 neutros tingidos usam o formato `color-mix` com 2 a 6% (AC 3, door 2)
Proof: `npm --prefix web test -- src/aparencia.test.tsx -t "tokens derivam de marca nos três temas"`
Proof: `npm --prefix web test -- src/ux.test.tsx -t "neutros tingidos pela marca nos três temas"`

**C6** - Sem cor da instituição, os 8 pares de texto (texto/fundo, texto/cartão, secundário/fundo, secundário/muted, secundário/primary-subtle, secundário/warning-subtle, texto do botão/botão, erro/fundo) dão pelo menos 4,5:1 em Claro, Escuro e Preto (AC 4)
Proof: `npm --prefix web test -- src/test/interface.test.ts -t "contraste dos tokens"`

**C7** - Sem cor da instituição, borda do campo/fundo, borda do campo/muted e foco/fundo dão pelo menos 3:1 em Claro, Escuro e Preto, e a borda do `.form-resposta` sobre muted dá pelo menos 3:1 no Claro (AC 4)
Proof: `npm --prefix web test -- src/test/interface.test.ts -t "contraste das bordas e do foco"`

**C8** - Com cada uma das 8 cores prontas da Aparência como `--marca` (lidas de `CoresProntas.tsx`), os 11 pares de C6 e C7 cumprem 4,5:1 e 3:1 em Claro, Escuro e Preto: 264 medições (AC 4)
Proof: `npm --prefix web test -- src/test/interface.test.ts -t "contraste com as oito cores prontas"`

**C9** - Nem `index.css` nem qualquer `.ts`/`.tsx` de `web/src` fora de testes contém `C83A23`, `F16A4B` (sem diferenciar caixa) ou matiz OKLCH de 30 a 36 com croma acima de 0,1 (AC 5)
Proof: `npm --prefix web test -- src/test/interface.test.ts -t "Origem fora da interface"`

**C10** - `--font-sans` é `'Public Sans Variable', ui-sans-serif, system-ui, sans-serif`, `--font-heading` é `'Source Serif 4 Variable', ui-serif, Georgia, serif`, os raios são 4/6/12/16 px e `--grafico-1..4` são `0.42 0.15 262`/`0.64 0.12 75`/`0.53 0.17 20`/`0.31 0 0` no claro e `0.64 0.14 262`/`0.88 0.12 85`/`0.76 0.14 20`/`0.52 0 0` no escuro (AC 6)
Proof: `npm --prefix web test -- src/test/interface.test.ts -t "tipografia raios e graficos intactos"`

**C11** - A regra `.form-resposta:not(.dark *)` declara `--input` com base `oklch(0.59 0.012 94)` e o mesmo tingimento de 6% (door 1)
Proof: `npm --prefix web test -- src/test/interface.test.ts -t "regra da borda do form da resposta"`

**C12** - O preset `Azul` da Aparência continua `#224fa7` e o `--primary` de `:root` cai em Tinta, não no azul (Impact, preset "Azul")
Proof: `npm --prefix web test -- src/administracao/aparencia/guidelinesCores.test.tsx -t "preset Azul mantem o valor sem o sobrenome Esclari"`

### S2 - Fundo do app fora do CSS · 8 arquivos · ~70 KB · ~18k

**C13** - `web/index.html` declara `theme-color` `#F7F5EE` para `(prefers-color-scheme: light)` e `#0E0E18` para `dark`, e `corDaBarra.json` é `{ light: #F7F5EE, dark: #0E0E18, black: #000000 }` (AC 7, door 3)
Proof: `npm --prefix web test -- src/guidelinesGlobal.test.ts -t "C7 index.html traz theme-color por tema"`

**C14** - Escolher Escuro, Claro e Preto no menu Tema grava `#0E0E18`, `#F7F5EE` e `#000000` no `theme-color` sem `media` (AC 7, door 3)
Proof: `npm --prefix web test -- src/guidelinesGlobal.test.ts -t "C7 trocar o tema atualiza o theme-color sem media"`

**C15** - O manifesto do PWA em `vite.config.ts` tem `theme_color` `#F7F5EE` e `background_color` `#F7F5EE` (AC 8, door 3)
Proof: `npm --prefix web test -- src/guidelinesGlobal.test.ts -t "C8 manifest com id, scope"`

**C16** - `GET /api/instituicoes/{slug}/manifest.webmanifest` devolve `background_color` `#F7F5EE` com e sem aparência, `theme_color` `#8a1538` com cor e `#C83A23` sem cor (AC 9, door 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests"`

**C17** - Depois de `npm --prefix web run icones`, os cantos de `icon-192.png` e `icon-512.png` são `#F7F5EE` (tolerância 3), e `icon-maskable-512.png` e `apple-touch-icon.png` ficam byte a byte iguais aos da base (AC 10, door 3)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "margem dos icones any em Papel"`
Proof: `git diff --exit-code 49cba64 -- web/public/icons/icon-maskable-512.png web/public/icons/apple-touch-icon.png`

### S3 - Telas e invariantes · 4 arquivos · ~40 KB · ~10k

**C18** - Existem 28 capturas PNG em `.specs/features/interface-marca-esclari/capturas/`, nomeadas `<tela>-<tema>-<largura>.png` para as telas `login`, `assistente`, `solicitacoes`, `chamado`, `fila`, `indicadores`, `aparencia`, temas `claro` e `escuro` e larguras 360 (360×780) e 1280 (1280×800), sem cor da instituição; a inspeção fica registrada no `## Handoff` (AC 11)
Proof: `python .specs/features/interface-marca-esclari/provar_interface.py -k test_C18_capturas`

**C19** - Entre `49cba64` e `HEAD`, os únicos arquivos de teste alterados em `web/` e `Esclari.Api.Tests/` são os desta lista fechada: `web/src/test/interface.test.ts`, `web/src/test/icones.test.ts`, `web/src/guidelinesGlobal.test.ts`, `web/src/aparencia.test.tsx`, `web/src/administracao/aparencia/guidelinesCores.test.tsx`, `web/e2e/interface-marca.spec.ts` (novo) e `Esclari.Api.Tests/ManifestoTests.cs`; nenhuma suíte de arranjo, celular, toque ou teclado muda (AC 12)
Proof: `python .specs/features/interface-marca-esclari/provar_interface.py -k test_C19_testes_tocados`

**C20** - As suítes E2E de arranjo, celular e diretrizes passam (AC 12)
Proof: `npm --prefix web run e2e -- arranjo.spec.ts celular.spec.ts casca-celular.spec.ts guidelines.spec.ts`

**C21** - Build web, build .NET, suíte Vitest inteira, `crapper` com maior nota abaixo de 10 em `web/src` e `dryer` sem candidato a 0.8 saem com 0; mutação fica suspensa (AD-025) (AC 13)
Proof: `npm --prefix web run build`
Proof: `dotnet build Esclari.slnx`
Proof: `npm --prefix web test -- --maxWorkers=4`
Proof: `npm --prefix web run coverage`
Proof: `Push-Location web; crapper --source-root src --use-existing-coverage --threshold 10; $c = $LASTEXITCODE; Pop-Location; exit $c`
Proof: `Push-Location web; dryer --threshold 0.8; $c = $LASTEXITCODE; Pop-Location; exit $c`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| portas do Landing (3) | door 1 C1 · door 2 C2, C5 · door 3 C13, C14, C15, C16, C17 | - |
| temas com tokens (3) | `:root` C1 · `.dark` C1 · `.black` C1 | - |
| elementos de ação sem cor (9) | C3, table-driven sobre os 9 casos (botão, item ativo e link × Claro, Escuro e Preto) | - |
| pares de texto 4,5:1 (24) | C6, table-driven sobre os 24 casos | - |
| pares de borda e foco 3:1 (10) | C7, table-driven sobre os 10 casos | - |
| cores prontas (8) | Azul · Verde · Vinho · Roxo · Laranja · Grafite · Petróleo · Marrom: C8, table-driven sobre 8 × 11 × 3 = 264 | - |
| lugares do fundo do app (8) | `index.html` claro C13 · `index.html` escuro C13 · `corDaBarra.json` C13 · troca de tema C14 · `vite.config.ts` theme_color C15 · `vite.config.ts` background_color C15 · `ManifestoInstituicao.FundoDoApp` C16 · margem dos ícones `any` C17 | - |
| ícones do PWA (4) | `icon-192` C17 · `icon-512` C17 · `icon-maskable-512` C17 · `apple-touch-icon` C17 | - |
| invariantes (4) | tipografia C10 · raios C10 · gráficos C10 · preset Azul C12 | - |
| telas capturadas (28) | C18, table-driven sobre os 28 arquivos | - |
| gates do AC 13 (6) | build web C21 · build .NET C21 · testes .NET tocados C16 · Vitest C21 · crapper C21 · dryer C21 | - |

- `GET /api/instituicoes/{slug}/manifest.webmanifest`: nenhum status muda (Surface `None`); o valor de `background_color` cruza a rota HTTP em C16.
- C3 e C4 afirmam cor renderizada e por isso usam um Chromium real; o jsdom não resolve `var()` nem `color-mix`.

## Test policy

O repositório já responde às duas perguntas para esta mudança: tokens de cor são provados por leitura estática do `index.css` com cálculo de contraste (`web/src/test/interface.test.ts`, `web/src/indicadores/contraste.test.ts`), e cor renderizada é provada em E2E com a API simulada (`web/e2e/simulada.ts`). Nenhuma linha nova de política.

| Code | Required proofs | Coverage expectation |
| --- | --- | --- |
| `index.css` (decide: fallback com ou sem `--marca`, 3 temas) | estática na própria camada **e** E2E no navegador | cada token da door 1 por tema; cada fallback; pares de contraste por tema e por cor pronta |
| `ManifestoInstituicao` (constante) | uma na rota HTTP | valor com e sem aparência |
| `gerar-icones.mjs` (lê `theme_color`) | uma sobre o PNG gerado | margem dos dois `any`; maskable e apple inalterados |

Evidence:

- `index.css`: 27 declarações `color-mix` com fallback, 3 blocos de tema que se sobrepõem (`.dark.black`) -> decide
- `ManifestoInstituicao.FundoDoApp`: constante sem condicional -> instrumentação, provada pela rota
- análogo: `interface.test.ts` "contraste dos tokens" já prova os pares por tema

## Swept

- validation: n/a - nenhuma entrada nova; a cor da instituição continua validada pela Aparência (AD-032)
- failure modes: C2 - sem `--marca`, o fallback de cada `color-mix` é a própria base
- idempotency: C17 - regravar os ícones produz os mesmos bytes no maskable e no apple
- authorization: n/a - nenhuma rota nem permissão muda
- concurrency: n/a - só valores estáticos de CSS e de manifesto
- data lifecycle: n/a - nada armazenado muda (Relations `None`)
- dependency failure: existing - `icones.test.ts` "falha do chromium" cobre o gerador sem Chromium
- state transitions: C14 - troca de tema Claro, Escuro e Preto
- observability: n/a - nenhuma exigência de log

## Handoff

- S1 ≈ 38k (index.css 12 KB, interface.test.ts 20 KB, aparencia.test.tsx, ux.test.tsx, guidelinesCores, contraste, CoresProntas, e2e novo e simulada.ts); S2 entra em 56k (index.html, vite.config.ts, corDaBarra.json, guidelinesGlobal 16 KB, icones.test.ts 10 KB, gerar-icones.mjs, ManifestoInstituicao, ManifestoTests); S3 fecha em 66k, abaixo do budget de 150k - um builder
- Mecanismo: um builder
- **Boundary:** C1-C21 construídos em `040ae1a..62d7209`. Provas do autor: Vitest 1074/1074 em 115 arquivos; cobertura exit 0; `crapper --threshold 10` exit 0; `dryer --threshold 0.8` exit 0; build web e build .NET exit 0; `ManifestoTests` 7/7; E2E `interface-marca.spec.ts` 3/3 contra :8080; `provar_interface.py` C18 e C19 OK; `git diff --exit-code` do C17 exit 0. Injeção do autor: `--marca-clara` a 95% derruba "contraste com as oito cores prontas" (Azul `.dark` botão 2,62:1).
- **Inspeção das capturas (C18):** as 28 capturas mostram fundo Papel com cartões brancos, botões e item ativo em Tinta no Claro, superfícies com matiz Tinta e ação em Papel no Escuro, sem Origem em controle nenhum; arranjo igual ao de antes.
- **Settled mid-build:** `--chart-1`/`--chart-2` (sem uso em tela, só em `@theme inline`) seguem a door 2: o fallback passa a ser a própria base cinza. A tabela `Tokens` ganhou 9 linhas para provar também os tokens que não mudam. Hexadecimais do `web/` ficam em minúsculas, como antes. A API de `redesign-marca-esclari-final` (fallback Origem em e-mail e manifesto) foi commitada antes da base (`49cba64`) depois dos 11 testes pendentes passarem com o Docker; AD-063 registra que esta feature substitui a metade de fallback do AC 14 daquela feature.
- **Pré-existente, fora do diff:** C20 tem 4 falhas em `arranjo.spec.ts` (`arranjo Indicadores`, `raiz com a marca e o texto centralizados`, `quebras medidas dos dois lados`, `valores de tipo e bordas`); as mesmas 4 falham na base `49cba64` (servidor Vite da base, mesmo filtro), porque esperam textos e rótulos de antes de `indicadores-claros` e da palavra-marca em imagem. As outras 95 passam. O `dryer` sai 0 mas lista `src/ui/popover.tsx` × `src/ui/separator.tsx` a 0,81, dois arquivos sem mudança desde a base.
- **Abandoned:** medir a cor pelo `getComputedStyle` cru (o Chromium devolve `oklch(...)`); o spec pinta a cor num canvas para obter sRGB. A chave do tema é `esclari-tema`, não a `vite-ui-theme` do shadcn.
