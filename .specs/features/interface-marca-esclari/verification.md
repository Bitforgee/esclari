# interface-marca-esclari verification

**Verdict**: FAIL
**Profile**: ui
**Diff range**: 49cba64..45b1193
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

Resumo: 19 dos 21 checks têm prova verde com asserção localizada; C20 e C21 falham como estão escritos. Fora dos checks, duas suítes E2E que passam na base `49cba64` quebram em `HEAD` por causa desta feature, e nenhuma prova da feature as roda. 13 faltas injetadas, 13 mortas (uma delas morta só por C5, ver a tabela). A árvore real terminou igual à linha de base (`?? .tmp-capturas/`, que já existia antes desta verificação).

## Binding sources

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `plan.md` Landing door 1 (valores literais) | yes - comparado token a token com `web/src/index.css` em `HEAD` | none - os 20 valores do claro, os 15 do escuro (mais `--sidebar-primary` Papel exigido pelo AC 2) e `--primary-subtle` do preto batem literalmente; o `.form-resposta` usa `0.59 0.012 94` | - |
| `plan.md` Landing door 2 (fallback) | yes - `index.css` tem 27 `color-mix(in oklch, var(--marca, X) N%, Y)` com `X == Y` e 30 `var(--marca, ` (27 + `--primary`, `--sidebar-primary`, `--ring` do `:root`), contados com regex própria | none | - |
| `plan.md` Landing door 3 (fundo fora do CSS) | yes - `index.html` `#f7f5ee`/`#0e0e18`, `corDaBarra.json`, `vite.config.ts` `theme_color`/`background_color` `#f7f5ee`, `ManifestoInstituicao.FundoDoApp` `#F7F5EE`, margem dos ícones `any` | none | - |
| `pesquisa.md` | yes - Tinta `oklch(0.216 0.073 280.7)` e Papel `oklch(0.970 0.010 93.6)` arredondados para 281 e 94 no plano e no CSS; erro claro `0.55` pelo par erro/fundo; Origem fora de controle | none | - |
| AD-063 (`STATE.md`) | yes - ação Tinta/Papel, neutros quentes 94 e Tinta 281, preto `#000`, fallback na própria base, erro `0.55 0.22 27.3`, `#F7F5EE` e `#0E0E18`, tipografia/raios/gráficos/preset Azul intactos | none | - |
| AD-062 e `redesign-marca-esclari-final/guia-de-marca.md` | yes - `guia-de-marca.md:33` ainda diz que o azul `#224fa7` "continua sendo a cor de ação padrão da interface" | none - a cláusula é superada de forma explícita pela AD-063; o guia ficou desatualizado (achado de documentação para o dono) | - |
| AD-027, AD-032, AD-034, AD-037, AD-060 | yes - AD-032 (`--marca-clara` 55% sobre branco) mantida e provada em C5; AD-060 (`--grafico-1..4`) em C10 | none | - |
| invariante herdado "cor de ação só em botão principal, item ativo, link e foco" (`estrutura-visual/plan.md:132` AC 22, provado por `web/e2e/ux.spec.ts:1250`) | yes | door 1 põe `--foreground` = `--primary` = Tinta no claro; sem cor da instituição, todo texto passa a ter a cor de ação, e a varredura de `ux.spec.ts:1250` acusa 7 elementos de texto na tela Raiz (passa na base) | o plano não registra a mudança desse invariante nem lista essa suíte; nenhum check cobre |
| capturas (`capturas/*.png`, 28) | yes - abertas `assistente-claro-1280`, `fila-escuro-1280`, `login-claro-360`, `indicadores-escuro-1280` | none - fundo Papel com cartões claros e botão/item ativo/monograma em Tinta no claro; superfícies com matiz Tinta, item ativo e botão em Papel no escuro; sem Origem em controle; gráficos com as cores do AD-060; arranjo (barra lateral à esquerda, cabeçalho, conteúdo, compositor no rodapé, formulário do login em coluna) igual ao das telas existentes | - |

Espaçamento e peso tipográfico ficam fora do alcance dos checks por definição do plano (só cor muda); a cor está coberta por C1, C3, C4 e C6 a C8 e o arranjo pelas suítes de C20.

## Checks

Comandos rodados em `HEAD` `45b1193`. Vitest numa só chamada: `npm --prefix D:\esclari\web test -- src/test/interface.test.ts src/aparencia.test.tsx src/ux.test.tsx src/administracao/aparencia/guidelinesCores.test.tsx src/guidelinesGlobal.test.ts src/test/icones.test.ts -t "<16 nomes em alternância>" --reporter=verbose`: exit 0, `Test Files 6 passed`, `Tests 17 passed | 123 skipped`, cada um dos 16 nomes aparece com ✓ (o 17º é `contraste dos tokens da barra`, casado pelo prefixo).

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | 31 tokens da door 1 por tema, porcentagens de hoje | Vitest acima: ✓ `tokens da paleta`, ✓ `tokens de Visual` | `web/src/test/interface.test.ts:302-304` - `expect(claro[token]).toBe(valorClaro)` / `escuro` / `preto` sobre a tabela `Tokens` (`:129-161`, 31 linhas, valores literais conferidos contra a door 1); `:334` - `expect(t[seletor][linha[0]]).toBe(esperado)` com a cascata `.dark.black` | PASS |
| C2 | sem azul antigo; 27 fallbacks iguais à base | ✓ `fallback de cada neutro e a propria base` | `interface.test.ts:268` - `not.toContain(oklch(0.45 0.15 262)...)`; `:270` - `toHaveLength(27)`; `:271` - `expect(reserva).toBe(base)`; `:272` - `toHaveLength(27 + 3)` | PASS |
| C3 | sem cor: botão, item ativo e link Tinta no claro e Papel no escuro/preto, tolerância 2 | `npm --prefix D:\esclari\web run e2e -- interface-marca.spec.ts -g "sem cor da instituicao a acao e Tinta ou Papel\|com cor da instituicao a acao segue a cor"`: exit 0, `✓ 1 ... sem cor da instituicao a acao e Tinta ou Papel`, `2 passed` | `web/e2e/interface-marca.spec.ts:34` - `toBeLessThanOrEqual(2)` por canal; `:54-56` - `perto(botao/ativo/link, esperada)` sobre `[light, Tinta], [dark, Papel], [black, Papel]` (`:51`). Gap de precisão: a "borda de 3 px" do claim não tem asserção de largura, só de cor | PASS |
| C4 | com `#8a1538`: botão e item ativo Vinho, fundo tingido | mesmo comando: `✓ 2 ... com cor da instituicao a acao segue a cor` | `interface-marca.spec.ts:64-65` - `perto(botao, Vinho)`, `perto(ativo, Vinho)`; `:67` - `expect(fundo).not.toEqual(Papel)`. Gap de precisão: desigualdade exata, um desvio de 1 unidade já passa | PASS |
| C5 | derivações de `--marca`; 14 neutros em `color-mix` 2-6% | ✓ `tokens derivam de marca nos três temas`, ✓ `neutros tingidos pela marca nos três temas` | `web/src/aparencia.test.tsx:98-99` - `toMatch(/^var\(--marca[\w-]*, .+\)$/)` e `toContain('var(--marca)')`; `:101` - `toBe('color-mix(in oklch, var(--marca) 55%, white)')`; `:102-103` - `/^var\(--marca-clara, /` e `toBe('oklch(0.216 0.073 281)')`; `web/src/ux.test.tsx:134-139` - formato, `>= 2`, `<= 6`, `total` 14 | PASS |
| C6 | 8 pares de texto ≥ 4,5:1 nos 3 temas | ✓ `contraste dos tokens` | `interface.test.ts:242` - `toBeGreaterThanOrEqual(4.5)` sobre `ParesDeTexto` (`:109-118`, os 8 pares do claim) × 3 temas | PASS |
| C7 | bordas e foco ≥ 3:1, `.form-resposta` ≥ 3:1 | ✓ `contraste das bordas e do foco` | `interface.test.ts:249` - `toHaveLength(10)`; `:250` - `toBeGreaterThanOrEqual(3)` | PASS |
| C8 | 8 cores prontas × 11 pares × 3 temas = 264 | ✓ `contraste com as oito cores prontas` | `interface.test.ts:255` - nomes lidos de `CoresProntas.tsx` iguais aos 8; `:261` - `toBeGreaterThanOrEqual(minimo)`; `:264` - `toBe(264)` | PASS |
| C9 | sem `C83A23`/`F16A4B` nem matiz 30-36 com C > 0,1 | ✓ `Origem fora da interface` | `interface.test.ts:277-278` - `not.toMatch(/c83a23\|f16a4b/i)` no CSS e nos `.ts/.tsx` fora de teste; `:280` - `quentes` igual a `[]`. Gap de precisão: a varredura de matiz só lê o `index.css`; para `.ts/.tsx` a garantia vem de outro teste (`interface.test.ts:309`, nenhum literal `oklch(` ou hex em `src`), que não é a prova nomeada | PASS |
| C10 | fontes, raios 4/6/12/16, gráficos intactos | ✓ `tipografia raios e graficos intactos` | `interface.test.ts:285-290` - `toBe`/`toEqual` com os literais do claim; `git diff 49cba64..HEAD -- web/src/index.css` não toca nenhuma linha de `--radius`, `--font-` ou `--grafico` | PASS |
| C11 | `.form-resposta` com base `0.59 0.012 94` e 6% | ✓ `regra da borda do form da resposta` | `interface.test.ts:322` - um só seletor `.form-resposta:not(.dark *)`; `:325` - `toBe('--input: ' + tingido(6, 0.59 ...))` | PASS |
| C12 | preset Azul `#224fa7`; `--primary` cai em Tinta | ✓ `preset Azul mantem o valor sem o sobrenome Esclari` | `web/src/administracao/aparencia/guidelinesCores.test.tsx:55-57` - `toContain('--primary: var(--marca, oklch(0.216 0.073 281))')`, `not.toContain(oklch(0.45 0.15 262))`, `toContain("['Azul', '224fa7']")` | PASS |
| C13 | `index.html` e `corDaBarra.json` | ✓ `C7 index.html traz theme-color por tema` | `web/src/guidelinesGlobal.test.ts:200` - `toEqual(['#f7f5ee', '#0e0e18'])`; `:201` - `CorDaBarra` `toEqual({ light, dark, black: '#000000' })` | PASS |
| C14 | Escuro, Claro, Preto gravam `#0e0e18`, `#f7f5ee`, `#000000` | ✓ `C7 trocar o tema atualiza o theme-color sem media` | `guidelinesGlobal.test.ts:209` - literais; `:213` - `expect(cor(), rotulo).toBe(esperado)` para Escuro, Claro, Preto, Claro | PASS |
| C15 | `theme_color` e `background_color` `#f7f5ee` | ✓ `C8 manifest com id, scope, ícone maskable ...` | `guidelinesGlobal.test.ts:264` - `toEqual(['#f7f5ee', '#f7f5ee'])` | PASS |
| C16 | manifesto da API: fundo `#F7F5EE`, tema `#8a1538` / `#C83A23` | `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests"`: exit 0, `Total de testes: 7`, `Aprovados: 7`, incluindo `Manifesto_da_instituicao`, `Manifesto_padrao_sem_aparencia`, `Manifesto_declara_o_fundo_do_app` | `Esclari.Api.Tests/ManifestoTests.cs:26` - tupla com `"#8a1538", "#F7F5EE"` (com aparência); `:49` - `Assert.Equal("#C83A23", theme_color)`; `:129` - `Assert.Equal("#F7F5EE", background_color)` (sem aparência), pela rota HTTP | PASS |
| C17 | cantos dos `any` Papel ±3; maskable e apple iguais à base | ✓ `margem dos icones any em Papel`; `git diff --exit-code 49cba64 -- web/public/icons/icon-maskable-512.png web/public/icons/apple-touch-icon.png`: exit 0 | `web/src/test/icones.test.ts:177` - `parecida(imagem.pixel(x, y), Papel)` nos 4 cantos de `icon-192` e `icon-512`; `:67` - tolerância `<= 3`. O teste regrava os ícones e `git status` continuou limpo (bytes idênticos) | PASS |
| C18 | 28 capturas `<tela>-<tema>-<largura>.png` nos tamanhos certos | `python .specs/features/interface-marca-esclari/provar_interface.py -v`: `test_C18_capturas ... ok`, exit 0 | `.specs/features/interface-marca-esclari/provar_interface.py:40` - 28 nomes; `:42` - conjunto presente igual ao esperado; `:44` - dimensões 360×780 e 1280×800 | PASS |
| C19 | só os 7 arquivos de teste da lista mudam | mesmo comando: `test_C19_testes_tocados ... ok` | `provar_interface.py:50` - `sorted(tocados - TESTES_PERMITIDOS) == []`; conferido à mão com `git diff --name-only 49cba64..HEAD` (os 7, nada mais) | PASS |
| C20 | arranjo, celular, casca-celular e guidelines passam | `npm --prefix D:\esclari\web run e2e -- arranjo.spec.ts celular.spec.ts casca-celular.spec.ts guidelines.spec.ts`: **exit 1**, `95 passed`, `4 failed`: `arranjo.spec.ts:227 arranjo Indicadores`, `:458 raiz com a marca e o texto centralizados`, `:938 quebras medidas dos dois lados`, `:1188 valores de tipo e bordas` | as 4 falham também na base `49cba64` (worktree da base, servidor Vite, mesmas mensagens: `strict mode violation: getByLabel('De') resolved to 7 elements`, timeout, `Expected "24px" Received "16px"`, timeout), então não são regressão desta feature; mas o claim "passam" é falso em `HEAD` | FAIL |
| C21 | builds, Vitest, crapper < 10 e dryer sem candidato a 0,8 saem 0 | `npm --prefix web run build` exit 0; `dotnet build Esclari.slnx` exit 0 (0 avisos, 0 erros); `npm --prefix web test -- --maxWorkers=4` exit 0, `115 passed`, `1074 passed`; `npm --prefix web run coverage` exit 0; `crapper --source-root src --use-existing-coverage --threshold 10` exit 0, maior nota 9.8 (`Sidebar`); `dryer --threshold 0.8` exit 0 com **745 candidatos** impressos (`DUPLICATE score=1.00 src/ui/alert.tsx:36-47 × src/ui/alert.tsx:49-63` ...) | os 745 estão todos em `src/ui/` (código copiado do shadcn, sem mudança no diff) e a base `49cba64` imprime os mesmos 745; o mesmo `dryer` restrito aos 76 `.ts/.tsx` de `src` fora de `ui/`, `test/`, `ilustracoes/` e `.test.` imprime `No duplicate candidates found.` (o recorte que `sessao-erro-transitorio/verification.md:27` usou). Mas o claim escrito diz "sem candidato" e a prova escrita mostra 745; e o exit code do `dryer` não decide nada (`dryer --help`: "0 the report was written"). O relato do construtor (só `popover × separator` a 0,81) é incompleto | FAIL |

## Coverage

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| portas do Landing (3) | `plan.md` Landing | door 1 C1 · door 2 C2, C5 · door 3 C13-C17 | - |
| tokens da door 1 (claro 20 + `.form-resposta`, escuro 15 + `--sidebar-primary`, preto 1) | `plan.md` door 1, comparado com `index.css` | todos na tabela `Tokens` de C1 com o mesmo literal; `.form-resposta` C11 | - |
| `color-mix` com fallback (27) | regex própria sobre `index.css`: 27 `color-mix(in oklch, var(--marca, `, 30 `var(--marca, ` | C2, contagem e igualdade nos 27 | - |
| cores prontas (8) | `CoresProntas.tsx:5-12` (Azul, Verde, Vinho, Roxo, Laranja, Grafite, Petróleo, Marrom) | C8 lê o arquivo e mede os 8 | - |
| lugares do fundo do app (8) | `git grep -i "fafbfd\|101113"` fora de `.specs`: zero sobras; lugares = `index.html` ×2, `corDaBarra.json`, troca de tema, `vite.config.ts` ×2, `FundoDoApp`, margem dos `any` | C13 ×3 · C14 · C15 ×2 · C16 · C17 | - |
| ícones do PWA (4) | `web/public/icons/` (4 PNGs) | `icon-192`, `icon-512` C17 teste · `icon-maskable-512`, `apple-touch-icon` C17 `git diff` | - |
| elementos de ação × temas (9) | AC 2 | C3, 3 elementos × 3 temas | - |
| pares 4,5:1 (24) e 3:1 (10) | AC 4 | C6 (8 × 3), C7 (3 × 3 + 1) | - |
| capturas (28) | AC 11: 7 telas × 2 temas × 2 larguras | C18 | - |
| invariantes (4) | AC 6 + Impact | tipografia, raios, gráficos C10 · preset Azul C12 | - |
| gates do AC 13 (6) | AC 13 | build web, build .NET, Vitest, crapper C21 · `.NET` tocados C16 · dryer C21 (FAIL) | dryer - ver C21 |
| testes existentes que fixam a paleta antiga ou o papel da cor de ação (sem linha no `checks.md`) | `git grep` em `web/e2e` por `oklch(0.45 0.15 262)` e pelas varreduras de cor; rodado `aparencia.spec.ts` + `ux.spec.ts` em `HEAD` e na base | `web/e2e/aparencia.spec.ts:80` `cor da instituição nos três temas` falha em `HEAD` (`Expected "oklch(0.45 0.15 262)" Received "oklch(0.216 0.073 281)"`, linhas 86-87 ainda esperam o azul) e passa na base; `web/e2e/ux.spec.ts:1250` `cor de ação só em botão principal, item ativo, link e foco` falha em `HEAD` (Raiz: 7 elementos de texto na cor de ação) e passa na base | `aparencia.spec.ts:80`, `ux.spec.ts:1250` - regressões sem check; o Impact do plano lista os testes tocados e não os cita |

Outras specs simuladas rodadas por precaução em `HEAD` (`ajustes`, `anexos`, `casca`, `chat`, `editor-fluxo`, `fluxos`, `identidade`, `instituicoes`, `integracoes`, `marca`, `plataforma`, `push`): 140 passed, 12 failed; as 12 falham também na base `49cba64` (mesmas mensagens), então não entram como regressão.

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| `index.css` decide (fallback, 3 temas) | `web/src/index.css` | estática (C1, C2, C5-C11) e E2E (C3, C4) | yes - cada token da door 1 por tema, cada fallback, pares por tema e por cor pronta |
| `ManifestoInstituicao` constante | `Esclari.Api/Instituicoes/Carga/ManifestoInstituicao.cs` | uma na rota HTTP | yes - `ManifestoTests.cs:26` com aparência, `:129` sem |
| `gerar-icones.mjs` lê `theme_color` | `web/scripts/gerar-icones.mjs` | uma sobre o PNG gerado | yes - `icones.test.ts:177` nos dois `any`; maskable e apple pelo `git diff` |

## Faults injected

Todas num worktree separado (`git worktree add --detach D:\esclari-vhead HEAD`, `node_modules` por junção), revertidas com `git -C D:\esclari-vhead checkout -- <arquivo>` depois de cada uma; a árvore real ficou com o porcelain da linha de base (`?? .tmp-capturas/`). As E2E de falta rodaram contra um Vite do worktree, com controle sem falta antes (`2 passed`).

| Mutation | Location | Killed |
| --- | --- | --- |
| `--muted-foreground` claro `0.48` -> `0.50` | `web/src/index.css` `:root` | yes - `tokens da paleta` e `tokens de Visual` (`expected '0.50 0.012 281' to be '0.48 0.012 281'`) |
| fallback do `--border` claro trocado por Tinta | `index.css` `:root --border` | yes - `fallback de cada neutro e a propria base` (`expected 'oklch(0.216 0.073 281)' to be 'oklch(0.89 0.012 94)'`) |
| `--marca-sutil-escuro` 40% -> 90% (só afeta quem tem cor) | `index.css:46` | yes - só `contraste com as oito cores prontas` (`Azul .dark --muted-foreground/--primary-subtle: 3.377 >= 4.5`); C1 e C6 passaram, como esperado |
| `--marca-clara` 55% -> 20% | `index.css:45` | yes - `tokens derivam de marca nos três temas` (C5). A prova de contraste C8 não a mata, e está certa: 20% de cor sobre branco clareia o botão e aumenta o contraste com o texto Tinta, então a mutação não viola C8 |
| `corDaBarra.json` dark `#0e0e18` -> `#101113` | `web/src/corDaBarra.json` | yes - C13 e C14 (`Escuro: expected '#101113' to be '#0e0e18'`) |
| `background_color` `#f7f5ee` -> `#fafbfd` | `web/vite.config.ts` | yes - C15 (`expected '#fafbfd' to be '#f7f5ee'`) |
| `theme-color` claro do `index.html` -> `#fafbfd` | `web/index.html` | yes - C13 |
| `FundoDoApp` -> `#fafbfd` | `ManifestoInstituicao.cs` | yes - `Manifesto_declara_o_fundo_do_app` e `Manifesto_da_instituicao` com falha (`Expected "#F7F5EE" Actual "#fafbfd"`) |
| margem dos ícones `any` pintada `#fafbfd` em vez do `theme_color` | `web/scripts/gerar-icones.mjs:44` | yes - `margem dos icones any em Papel` (`icon-192.png 0,0`); os PNGs regravados no worktree foram revertidos |
| preset novo `['Origem', 'c83a23']` | `web/src/administracao/aparencia/CoresProntas.tsx` | yes - `Origem fora da interface` |
| `--radius-md` 12px -> 10px | `index.css` `@theme inline` | yes - `tipografia raios e graficos intactos` |
| E2E: `--sidebar-primary` escuro com fallback Tinta | `index.css` `.dark` | yes - C3 (`dark item ativo: 21,19,58 contra 247,245,238`) |
| E2E: `--primary` claro sem `var(--marca, ...)` | `index.css` `:root` | yes - C4 (`botão: 21,19,58 contra 138,21,56`) |

## Findings for the owner

1. **Regressão sem check, `web/e2e/ux.spec.ts:1250`** (`cor de ação só em botão principal, item ativo, link e foco`, invariante do AC 22 de `estrutura-visual`): a door 1 aprovada faz `--foreground` = `--primary` = Tinta, então sem cor da instituição o texto comum tem a cor de ação. Passa na base, falha em `HEAD`. Decidir: registrar em AD que o invariante muda (texto Tinta é aceito) e emendar a varredura, ou separar o texto da cor de ação (outro valor de `--foreground`), o que reabre a door 1.
2. **Regressão sem check, `web/e2e/aparencia.spec.ts:80`**: as linhas 86-87 ainda esperam `oklch(0.45 0.15 262)` e `oklch(0.72 0.12 262)` para "sem cor". Atualizar exige emendar a lista fechada do C19 e o Impact do plano.
3. **C20 FAIL**: as 4 falhas de `arranjo.spec.ts` são anteriores à feature (falham igual na base). Fechar pede emenda do C20 (excluir as 4 com motivo) ou um AD de risco aceito.
4. **C21 FAIL (dryer)**: a prova escrita imprime 745 candidatos, todos em `src/ui/` e iguais aos da base; o exit code do `dryer` é sempre 0. Fechar pede emenda do C21 com o recorte das features anteriores (fatias e raiz, sem `ui/`, `test/`, `ilustracoes/` e `.test.`) e uma prova que leia a saída (`No duplicate candidates found.`), ou um AD.
5. **Documentação**: `redesign-marca-esclari-final/guia-de-marca.md:33` ainda diz que o azul é a cor de ação padrão; a AD-063 superou isso.
6. **Gaps de precisão (não reprovam)**: C3 não mede a largura de 3 px; C4 usa desigualdade exata contra Papel; a parte de matiz do C9 para `.ts/.tsx` depende de `interface.test.ts:309`, que não é a prova nomeada.
7. **Ambiente**: o filtro de arquivo do Playwright é regex, então `marca.spec.ts` também casa `interface-marca.spec.ts` e roda `capturas das sete telas`, que regrava as 28 capturas do commit. Isso aconteceu numa execução desta verificação; as capturas foram restauradas com `git checkout` e a árvore voltou à linha de base.

## Gate

- Vitest focado: 17 passed, 0 failed. Vitest inteiro: 1074 passed, 0 failed (115 arquivos). Cobertura: exit 0.
- `ManifestoTests`: 7 passed, 0 failed.
- E2E C3/C4: 2 passed. E2E C20: 95 passed, 4 failed (pré-existentes). E2E `aparencia.spec.ts` + `ux.spec.ts`: 59 passed, 2 failed (regressões).
- `provar_interface.py`: 2 passed. `git diff --exit-code` do C17: exit 0.
- Builds web e .NET: exit 0. `crapper`: exit 0, máximo 9.8. `dryer`: exit 0 com 745 candidatos em `src/ui/`.
- `mutator`: suspenso por decisão do usuário (AD-025), não rodou.
