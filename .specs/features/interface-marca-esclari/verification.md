# interface-marca-esclari verification

**Verdict**: PASS
**Profile**: ui
**Diff range**: 49cba64..671d093
**Round**: 3 - scoped (proofs re-run in full at `671d093`; step 1, coverage, faults and citations redone on what `3a98b3b..671d093` touched: AD-065/E4, E5, `provar_interface.py`, and the `seletor-de-cor` commit `e695042`, which changes `CoresProntas.tsx`, `Aparencia.tsx` and `guidelinesCores.test.tsx`; the rest is marked carried from `3a98b3b`)
**Verifier**: independent sub-agent (author != verifier), round 3

Resumo: as 24 provas (C1-C24, com E1-E5 no lugar do texto que nomeiam) saem verdes em `671d093`, cada uma com asserção localizada. O único achado da rodada 2 (aba ativa da barra inferior fora da AD-064) foi resolvido por decisão do dono: a AD-065 tira o item de navegação ativo (`aria-current=page`) da regra de sublinhado, e o código bate com isso (`web/src/Autenticado.tsx:295-296`: `text-primary` só quando `ativo`, o mesmo ramo que põe `aria-current="page"` e `font-semibold`). Nenhum outro `<a>` renderiza `text-primary` sem `underline`. 23 faltas injetadas num worktree separado, 23 mortas; a árvore real terminou com o porcelain da linha de base (`?? .tmp-capturas/`).

## Rounds 1-2 (carried note)

- Rodada 1 em `45b1193`: FAIL. C20 e C21 falhavam (4 testes pré-existentes de `arranjo.spec.ts`; `dryer` com 745 candidatos em `src/ui/`), e `ux.spec.ts:1250` e `aparencia.spec.ts:80` quebravam sem check. 13 faltas, 13 mortas. Resolvido por `55c4df2` (E1-E3, AD-064) e `3a98b3b`.
- Rodada 2 em `3a98b3b`: FAIL só por step 1: a aba ativa da barra inferior (`Autenticado.tsx:292-296`) era um `<a>` com `text-primary` sem `underline`, contra a AD-064. 28 faltas, 28 mortas. O dono escolheu a opção (a): AD-065 e E4 (`671d093`), sem mudança de código.
- Entre `3a98b3b` e `671d093` entrou também `e695042` (feature `seletor-de-cor`, com Verify próprio). Esta rodada só olha o efeito dele sobre as provas desta feature: C8 e C12 leem `CoresProntas.tsx`, C19 conta os testes do diff (E5) e C22 varre a tela Aparência.

## Binding sources

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `plan.md` Landing door 1 | carried from `3a98b3b` - `git diff --stat 3a98b3b..671d093` não lista `web/src/index.css`; a comparação token a token da rodada 2 (`index.css:6-114`) vale | none | - |
| `plan.md` Landing door 2 | carried from `3a98b3b` - mesmo motivo; 27 `color-mix` com fallback igual à base | none | - |
| `plan.md` Landing door 3 | carried from `45b1193` - `index.html`, `vite.config.ts`, `corDaBarra.json`, `ManifestoInstituicao.cs`, `gerar-icones.mjs` e os ícones não mudaram desde então | none | - |
| `pesquisa.md` | yes - reaberto: Tinta `0.216 0.073 280.7`, Papel `0.970 0.010 93.6`, erro claro `0.55`, Origem fora de controle | none | - |
| AD-063 (`STATE.md:69`) | yes | none | - |
| AD-064 (`STATE.md:70`) com AD-065 (`STATE.md:71`) | yes - comparadas com C22, C24/E4 e com todo `text-primary` de `web/src` fora de testes (`git grep -n "text-primary" -- web/src ':!*.test.*'`) | none - os `<a>` literais com `text-primary` são 4 e todos têm `underline` (`aparencia.tsx:171,176` com `underline-offset-4`, `chamados/Conversa.tsx:64`, `LinkDeAnexo.tsx:8`); a aba da barra inferior só ganha `text-primary` no ramo `ativo`, que é o mesmo que põe `aria-current="page"` (`Autenticado.tsx:295`) e `font-semibold` (`:296`), exatamente o recorte da AD-065; as variantes `link` de `ui/button.tsx:20` e `ui/badge.tsx:19` (sem `underline` em repouso) não têm uso (`git grep -n -E "variant=.link." -- web/src` sem resultado); os demais `text-primary` são `span`/ícone, não `<a>` | - |
| `redesign-marca-esclari-final/guia-de-marca.md` | carried from `3a98b3b` - não mudou | none | - |
| AD-027, AD-032, AD-034, AD-037, AD-060, AD-062 | carried from `45b1193` | none | - |
| invariante "cor de ação só em botão principal, item ativo, link e foco" (`ux.spec.ts:1250`) sobre a tela Aparência depois de `e695042` | yes - o seletor novo (`CoresProntas.tsx:60`, `label` com `bg-background`, `border`, `outline-ring` no foco) usa só tokens; a amostra com a cor da instituição é isenta por `data-amostra` (`web/e2e/ux.spec.ts:1272`) | none - C22 passa na varredura de 19 telas, Aparência incluída (`web/e2e/simulada.ts:162`) | - |
| capturas (`capturas/*.png`, 28) | carried from `45b1193` - não mudaram | none para esta feature; ver Findings 3 | - |

Tela tocada desde a rodada 2: só Aparência, pelo `seletor-de-cor` (botão "Escolher cor" ao lado do campo "Cor principal"). Arranjo e texto dessa mudança são obrigações do `checks.md` de `seletor-de-cor`, não deste; aqui confirmei só cor: nenhum valor novo fora dos tokens, nenhum `C83A23`/`F16A4B` (C9 verde) e a regra da cor de ação continua valendo na tela (C22 verde). Espaçamento e peso tipográfico continuam fora do alcance dos checks.

## Checks

Comandos rodados em `HEAD` `671d093` (o container `api` em :8080 serve esse `HEAD`: o bundle `/assets/index-DwJGFAXN.js` contém `data-amostra`, que só existe a partir de `e695042`; `671d093` só muda documentos).

- Vitest numa chamada (cwd `C:\`): `npm --prefix D:\esclari\web test -- src/test/interface.test.ts src/aparencia.test.tsx src/ux.test.tsx src/administracao/aparencia/guidelinesCores.test.tsx src/guidelinesGlobal.test.ts src/test/icones.test.ts -t "<17 nomes em alternância>" --reporter=verbose`: exit 0, `Test Files 6 passed (6)`, `Tests 18 passed | 123 skipped (141)`; cada um dos 17 nomes aparece com ✓ (o 18º é `contraste dos tokens da barra`, casado pelo prefixo).
- E2E numa chamada contra :8080: `npm --prefix D:\esclari\web run e2e -- e2e/interface-marca.spec.ts e2e/ux.spec.ts e2e/aparencia.spec.ts -g "sem cor da instituicao a acao e Tinta ou Papel|com cor da instituicao a acao segue a cor|cor de ação só em botão principal, item ativo, link e foco|cor da instituição nos três temas"`: exit 0, `5 passed` (`aparencia.spec.ts:80`, `interface-marca.spec.ts:50`, `:61`, `ux.spec.ts:1250`, `:1287` casado pelo prefixo).
- `python .specs/features/interface-marca-esclari/provar_interface.py -v` (`PYTHONUTF8=1`): `test_C18_capturas ok`, `test_C19_testes_tocados ok`, `test_C21_dryer ok`, exit 0.

Citações: `guidelinesCores.test.tsx` e `provar_interface.py` refeitas em `671d093`; o resto carried from `3a98b3b` (`git diff --name-only 3a98b3b..671d093` não lista os demais arquivos de teste).

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | 31 tokens da door 1 por tema, porcentagens de hoje | Vitest acima: ✓ `tokens da paleta`, ✓ `tokens de Visual` | `web/src/test/interface.test.ts:308-310` - `expect(claro[token]).toBe(valorClaro)` / `escuro` / `preto` sobre a tabela `Tokens` (`:129-161`); `:340` - `expect(t[seletor][linha[0]]).toBe(esperado)` | PASS |
| C2 | sem azul antigo; 27 fallbacks iguais à base | ✓ `fallback de cada neutro e a propria base` | `interface.test.ts:268` - `not.toContain(oklch(0.45 0.15 262) ...)`; `:270` - `toHaveLength(27)`; `:271` - `expect(reserva).toBe(base)` | PASS |
| C3 | sem cor: botão, item ativo e link Tinta no claro, Papel no escuro e no preto, tolerância 2 | E2E acima: ✓ `sem cor da instituicao a acao e Tinta ou Papel` | `web/e2e/interface-marca.spec.ts:34` - `toBeLessThanOrEqual(2)` por canal; `:54-56` - `perto(botao/ativo/link, esperada)` sobre `[light, Tinta], [dark, Papel], [black, Papel]` (`:51`) | PASS |
| C4 | com `#8a1538`: botão e item ativo Vinho, fundo tingido | ✓ `com cor da instituicao a acao segue a cor` | `interface-marca.spec.ts:64-65` - `perto(botao, Vinho)`, `perto(ativo, Vinho)`; `:67` - `expect(fundo).not.toEqual(Papel)` | PASS |
| C5 | derivações de `--marca`; 14 neutros em `color-mix` 2-6% | ✓ `tokens derivam de marca nos três temas`, ✓ `neutros tingidos pela marca nos três temas` | `web/src/aparencia.test.tsx:98-99` - `toMatch(/^var\(--marca[\w-]*, .+\)$/)`; `:101` - `toBe('color-mix(in oklch, var(--marca) 55%, white)')`; `:102-103`; `web/src/ux.test.tsx:134-139` - formato, `>= 2`, `<= 6`, total 14 | PASS |
| C6 | 8 pares de texto ≥ 4,5:1 nos 3 temas | ✓ `contraste dos tokens` | `interface.test.ts:242` - `toBeGreaterThanOrEqual(4.5)` sobre `ParesDeTexto` × 3 temas | PASS |
| C7 | bordas e foco ≥ 3:1, `.form-resposta` ≥ 3:1 | ✓ `contraste das bordas e do foco` | `interface.test.ts:249` - `toHaveLength(10)`; `:250` - `toBeGreaterThanOrEqual(3)` | PASS |
| C8 | 8 cores prontas (lidas de `CoresProntas.tsx`) × 11 pares × 3 temas = 264 | ✓ `contraste com as oito cores prontas` | `interface.test.ts:261` - `toBeGreaterThanOrEqual(minimo)`; `:264` - `toBe(264)`; a lista lida continua em `web/src/administracao/aparencia/CoresProntas.tsx:5-12`, intacta depois de `e695042` | PASS |
| C9 | sem `C83A23`/`F16A4B` nem matiz 30-36 com C > 0,1 | ✓ `Origem fora da interface` | `interface.test.ts:277-278` - `not.toMatch(...)` com `c83a23` e `f16a4b` sem caixa no CSS e nos `.ts/.tsx` fora de teste; `:280` - `quentes` igual a `[]` | PASS |
| C10 | fontes, raios 4/6/12/16, gráficos intactos | ✓ `tipografia raios e graficos intactos` | `interface.test.ts:291-293` - raios `toEqual(['4px', '6px', '12px', '16px'])`; `:295-296` - os 8 valores de `--grafico-1..4` | PASS |
| C11 | `.form-resposta` com base `0.59 0.012 94` e 6% | ✓ `regra da borda do form da resposta` | `interface.test.ts:328` - um só seletor; `:331` - `toBe('--input: ' + token com 0.59 0.012 94)` | PASS |
| C12 | preset Azul `#224fa7`; `--primary` cai em Tinta | ✓ `preset Azul mantem o valor sem o sobrenome Esclari` | `web/src/administracao/aparencia/guidelinesCores.test.tsx:53` - `toHaveValue(Azul)` com `Azul = '#' + '224fa7'` (`:9`); `:55` - `toContain('--primary: var(--marca, ' + 'ok' + 'lch(0.216 0.073 281))')`; `:56` - sem `0.45 0.15 262`; `:57` - `toContain("['Azul', '224fa7']")`. `e695042` só acrescentou um passo de Tab noutro teste do arquivo (`:67`); nenhuma asserção de C12 mudou | PASS |
| C13 | `index.html` e `corDaBarra.json` | ✓ `C7 index.html traz theme-color por tema` | `web/src/guidelinesGlobal.test.ts:200` - `toEqual(['#f7f5ee', '#0e0e18'])`; `:201` - `CorDaBarra` `toEqual({ light, dark, black: '#000000' })` | PASS |
| C14 | Escuro, Claro, Preto gravam `#0e0e18`, `#f7f5ee`, `#000000` | ✓ `C7 trocar o tema atualiza o theme-color sem media` | `guidelinesGlobal.test.ts:209` - literais; `:213` - `expect(cor(), rotulo).toBe(esperado)` | PASS |
| C15 | `theme_color` e `background_color` `#f7f5ee` | ✓ `C8 manifest com id, scope, ícone maskable e background igual ao theme_color claro` | `guidelinesGlobal.test.ts:264` - `toEqual(['#f7f5ee', '#f7f5ee'])` | PASS |
| C16 | manifesto da API: fundo `#F7F5EE`, tema `#8a1538` / `#C83A23` | `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests"`: exit 0, `Total de testes: 7`, `Aprovados: 7` (`Manifesto_da_instituicao`, `Manifesto_padrao_sem_aparencia`, `Manifesto_declara_o_fundo_do_app` entre eles) | `Esclari.Api.Tests/ManifestoTests.cs:26` - `"#8a1538", "#F7F5EE"` com aparência; `:49` - `Assert.Equal("#C83A23", theme_color)`; `:129` - `Assert.Equal("#F7F5EE", background_color)`, pela rota HTTP | PASS |
| C17 | cantos dos `any` Papel ±3; maskable e apple iguais à base | ✓ `margem dos icones any em Papel`; `git diff --exit-code 49cba64 -- web/public/icons/icon-maskable-512.png web/public/icons/apple-touch-icon.png`: exit 0 | `web/src/test/icones.test.ts:177` - `parecida(imagem.pixel(x, y), Papel)` nos 4 cantos; `:67` - tolerância `<= 3`; `git status` limpo depois do teste | PASS |
| C18 | 28 capturas `<tela>-<tema>-<largura>.png` nos tamanhos certos | `provar_interface.py -v`: `test_C18_capturas ... ok` | `.specs/features/interface-marca-esclari/provar_interface.py:44` - 28 nomes; `:46` - conjunto presente igual ao esperado; `:48` - 360×780 e 1280×800 | PASS |
| C19 (E1, E5) | só os testes da lista (7 originais + `ux.spec.ts`, `aparencia.spec.ts`) mudam, sem contar os 2 de `seletor-de-cor` | `provar_interface.py -v`: `test_C19_testes_tocados ... ok` | `provar_interface.py:54` - `sorted(tocados - TESTES_PERMITIDOS) == []`, lista em `:14-26` (os 2 de E5 em `:24-25`); `git diff --name-only 49cba64..HEAD -- web Esclari.Api.Tests` dá 11 testes: os 9 da lista mais `aparenciaAbas.test.tsx` e `seletor-de-cor.spec.ts`; nenhuma suíte de arranjo, celular, toque ou teclado | PASS |
| C20 (E2) | arranjo, celular, casca-celular e guidelines passam, exceto os 4 de `arranjo.spec.ts` pré-existentes | `npm --prefix D:\esclari\web run e2e -- e2e/arranjo.spec.ts e2e/celular.spec.ts e2e/casca-celular.spec.ts e2e/guidelines.spec.ts --grep-invert` com os 4 nomes de E2: exit 0, `95 passed (1.0m)` | as mesmas 95 da rodada 2, em `web/e2e/arranjo.spec.ts`, `celular.spec.ts`, `casca-celular.spec.ts`, `guidelines.spec.ts:160`, `:188` (últimas linhas da saída) | PASS |
| C21 (E3) | build web, build .NET, Vitest inteiro, crapper < 10 saem 0; `dryer` sobre as 6 fatias e a raiz imprime "No duplicate candidates found." | `npm --prefix web run build` exit 0; `dotnet build Esclari.slnx` exit 0 (`0 Aviso(s)`, `0 Erro(s)`); `npm --prefix web test -- --maxWorkers=4` exit 0, `115 passed`, `1077 passed` (os 1075 da rodada 2 mais 2 testes de `seletor-de-cor`); `npm --prefix web run coverage` exit 0; `crapper --source-root src --use-existing-coverage --threshold 10` exit 0, maior nota 9.8 (`Sidebar`); `provar_interface.py -k test_C21_dryer` ok | `provar_interface.py:62` - `returncode == 0`; `:63` - `assertIn("No duplicate candidates found.", r.stdout)`; `:64` - `assertNotIn("DUPLICATE", r.stdout)`. Mutação suspensa (AD-025), não rodou | PASS |
| C22 | com cor, cor de ação só em botão principal, item ativo, link e foco; quando `--primary` pinta igual a `--foreground`, a regra de texto sai e fundo e borda ficam | E2E acima: ✓ `ux.spec.ts:1250` | `web/e2e/ux.spec.ts:1258` - lê `--primary` e `--foreground`; `:1280` - `primaria.join() === texto.join()`; `:1275` - regra de texto condicionada; `:1269-1273` fundo e `:1277` borda sem a condição; `:1281` - `expect(indevidos).toEqual([])` | PASS |
| C23 | botão Entrar: `#8a1538` no claro e `color-mix(... 55%, white)` no escuro e no preto com cor; Tinta no claro e Papel no escuro sem cor | E2E acima: ✓ `aparencia.spec.ts:80` | `web/e2e/aparencia.spec.ts:83-87` - os 5 casos com os literais do claim; `:95` - `expect(fundo).toBe(esperado)` | PASS |
| C24 (E4) | todo `<a>` literal com `text-primary` fora de testes tem `underline` (pelo menos 4); o item de navegação ativo (`aria-current=page`) fica fora | Vitest acima: ✓ `links text-primary sublinhados` | `interface.test.ts:284` - `matchAll(/<a\b[^>]*className="([^"]*\btext-primary\b[^"]*)"/g)` sobre os `.tsx` fora de `.test.`; `:285` - `toBeGreaterThanOrEqual(4)`; `:286` - `toContain('underline')`. Os 4 que casam: `aparencia.tsx:171,176`, `Conversa.tsx:64`, `LinkDeAnexo.tsx:8`. O único outro `<a>` com `text-primary` é a aba ativa, `Autenticado.tsx:295-296`, excluída pela E4 | PASS |

## Coverage

Recomputado em `671d093` para os conjuntos cuja autoridade a rodada tocou; o resto carried from `3a98b3b`, com as autoridades inalteradas.

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| portas do Landing (3) | `plan.md` Landing | door 1 C1 · door 2 C2, C5 · door 3 C13-C17 | - |
| tokens da door 1 (claro 20 + `.form-resposta`, escuro 15 + `--sidebar-primary`, preto 1) | carried from `3a98b3b` (`index.css` sem mudança) | C1, `.form-resposta` C11 | - |
| `color-mix` com fallback (27) | carried from `3a98b3b` | C2 | - |
| cores prontas (8) | `CoresProntas.tsx:5-12` em `671d093` (Azul, Verde, Vinho, Roxo, Laranja, Grafite, Petróleo, Marrom; `e695042` não mexeu na lista) | C8 lê o arquivo e mede os 8; preset Azul C12 | - |
| lugares do fundo do app (8) | carried from `45b1193` | C13 ×3 · C14 · C15 ×2 · C16 · C17 | - |
| ícones do PWA (4) | `web/public/icons/` | `icon-192`, `icon-512` C17 teste · `icon-maskable-512`, `apple-touch-icon` C17 `git diff` | - |
| elementos de ação × temas (9) | AC 2 | C3, 3 × 3 | - |
| pares 4,5:1 (24) e 3:1 (10) | AC 4 | C6 (8 × 3), C7 (3 × 3 + 1) | - |
| capturas (28) | AC 11 | C18 | - |
| invariantes (4) | AC 6 + Impact | tipografia, raios, gráficos C10 · preset Azul C12 | - |
| gates do AC 13 (6) | AC 13 + E3 | build web, build .NET, Vitest, crapper C21 · `.NET` tocados C16 · dryer C21 | - |
| testes alterados (11 no diff; 9 contados) | `git diff --name-only 49cba64..671d093 -- web Esclari.Api.Tests` filtrado por teste, com E1 e E5 | 9 da lista C19 · 2 de `seletor-de-cor` excluídos por E5 | - |
| regra da cor de ação da AD-064 (3 ramos) | `STATE.md:70` | com cor, só botão/item ativo/link/foco C22 · sem cor, regra de texto desligada C22 · sem cor, fundo e borda seguem C22 | - |
| `<a>` que renderizam `text-primary` (5) | `git grep -n "text-primary" -- web/src ':!*.test.*'` mais as variantes `link` de `ui/` (sem uso) | 4 `<a>` literais C24 · aba ativa `Autenticado.tsx:295-296` excluída pela AD-065/E4 | - |

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| `index.css` decide (fallback, 3 temas) | `web/src/index.css` | estática (C1, C2, C5-C11) e E2E (C3, C4, C23) | yes - cada token da door 1 por tema, cada fallback, pares por tema e por cor pronta; carried from `3a98b3b`, arquivo sem mudança |
| `ManifestoInstituicao` constante | `Esclari.Api/Instituicoes/Carga/ManifestoInstituicao.cs` | uma na rota HTTP | yes - `ManifestoTests.cs:26` com aparência, `:129` sem |
| `gerar-icones.mjs` lê `theme_color` | `web/scripts/gerar-icones.mjs` | uma sobre o PNG gerado | yes - `icones.test.ts:177`; maskable e apple pelo `git diff` |

## Faults injected

Todas num worktree separado (`git worktree add --detach D:\esclari-v3 HEAD`, `web\node_modules` por junção `mklink /J`), cada uma revertida com `git -C D:\esclari-v3 checkout -- <arquivo>` antes da seguinte e o porcelain do worktree conferido vazio. Vitest e Python rodaram no worktree; `dotnet test` no projeto de testes do worktree; as E2E contra um Vite do worktree na porta 5181 (`createServer` do `vite`, Playwright por `process.execPath`, `E2E_URL=http://localhost:5181`, `shell: false`). Controles sem falta antes: Vitest `18 passed` no worktree; E2E `5 passed` contra o Vite do worktree. As duas faltas de C19 foram commits só no worktree destacado. Depois: junção removida com `cmd /c rmdir`, `git worktree remove --force D:\esclari-v3`, porcelain da árvore real igual à linha de base (`?? .tmp-capturas/`). Nada foi feito em :8080 nem nos containers.

| Mutation | Location | Killed |
| --- | --- | --- |
| F1 `--muted-foreground` claro `0.48` -> `0.50` | `web/src/index.css:28` | yes - C1 (`:root --muted-foreground: expected '0.50 0.012 281' to be '0.48 0.012 281'`) |
| F2 fallback do `--border` claro trocado por Tinta | `index.css:34` | yes - C2 (`expected 'oklch(0.216 0.073 281)' to be 'oklch(0.89 0.012 94)'`) |
| F3 `--marca-clara` 55% -> 20% | `index.css:45` | yes - C5 (`expected 'color-mix(in oklch, var(--marca) 20%,…' to be '… 55%,…'`) |
| F4 `--destructive` claro `0.55` -> `0.60` | `index.css:33` | yes - C6 (`:root --destructive/--background: expected 4.036 to be greater than or equal to 4.5`) |
| F5 `--input` escuro `0.58` -> `0.40` | `index.css:79` | yes - C7 (`.dark --input/--background: expected 2.074 … >= 3`) |
| F6 (superfície de `e695042`) preset Vinho `8a1538` -> `f5d0e0` | `web/src/administracao/aparencia/CoresProntas.tsx:7` | yes - C8 (`Vinho :root --primary-foreground/--primary: expected 1.343 … >= 4.5`) |
| F7 (superfície de `e695042`) preset Azul `224fa7` -> `224fa8` | `CoresProntas.tsx:5` | yes - C12 (`expect(element).toHaveValue(#224fa7)`, `guidelinesCores.test.tsx:53`) |
| F8 (código novo de `e695042`) cor de reserva do `SeletorDeCor` `000000` -> `c83a23` | `CoresProntas.tsx`, `comHash('000000')` do `SeletorDeCor` | yes - C9 (`expected [ Array(1) ] to deeply equal []`) |
| F9 `--radius-md` 12px -> 10px | `index.css:129` | yes - C10 (`[ '4px', '6px', '10px', '16px' ]`) |
| F10 base do `.form-resposta` `0.59` -> `0.62` | `index.css:113` | yes - C11 (`expected '--input: color-mix(…' to be '--input: color-mix(… 0.59 0.012 94 …'`) |
| F11 `theme-color` claro -> `#fafbfd` | `web/index.html:6` | yes - C13 (`expected '#fafbfd' to be '#f7f5ee'`) |
| F12 `corDaBarra.json` dark `#0e0e18` -> `#101113` | `web/src/corDaBarra.json:1` | yes - C14 (`Escuro: expected '#101113' to be '#0e0e18'`) |
| F13 `background_color` -> `#fafbfd` | `web/vite.config.ts:27` | yes - C15 (`expected '#fafbfd' to be '#f7f5ee'`) |
| F14 `underline` tirado do link da fonte da conversa | `web/src/chamados/Conversa.tsx:64` | yes - C24 (`chamados/Conversa.tsx: expected [ 'text-primary' ] to include 'underline'`) |
| F15 margem dos ícones `any` pintada `#fafbfd` | `web/scripts/gerar-icones.mjs:44` | yes - C17 (`icon-192.png 0,0: expected false to be true`); PNGs regravados revertidos |
| F16 uma captura apagada (`fila-escuro-360.png`) | `capturas/` | yes - C18 (`Lists differ … 'fila-escuro-360.png'`) |
| F17 cópia de `Chamados.tsx` numa fatia | `web/src/chamados/CopiaDaFalta.tsx` | yes - C21 E3 (`'No duplicate candidates found.' not found in 'DUPLICATE score=1.00 src/chamados/Chamados.tsx:40-61 …'`) |
| F18 `FundoDoApp` -> `#FAFBFD` | `Esclari.Api/Instituicoes/Carga/ManifestoInstituicao.cs:11` | yes - C16 (`Manifesto_da_instituicao`, `Manifesto_declara_o_fundo_do_app`: `Expected "#F7F5EE" Actual "#FAFBFD"`; 2 com falha, 5 aprovados) |
| F19 (superfície de E5) teste fora da lista alterado e commitado | `web/e2e/arranjo.spec.ts` | yes - C19 (`['web/e2e/arranjo.spec.ts'] != []`) |
| E1 `--sidebar-primary` escuro com fallback Tinta | `index.css:60` | yes - C3 (`dark item ativo: 21,19,58 contra 247,245,238`, recebido 226 contra `<= 2`) |
| E2 `--primary` claro sem `var(--marca, ...)` | `index.css:22` | yes - C4 (`botão: 21,19,58 contra 138,21,56`) |
| E3 (código novo de `e695042`) rótulo "Escolher cor" em `text-primary` | `CoresProntas.tsx:60` | yes - C22 (`Aparência: cor de ação fora de botão principal, item ativo, link e foco`, 5 achados com a cor da instituição) |
| E4 `--primary` escuro com fallback `0.95 0.008 94` | `index.css:66` | yes - C23 (`dark sem cor: Expected "oklch(0.97 0.01 94)" Received "oklch(0.95 0.008 94)"`) |

Controle negativo de E5, antes de F19: um commit que só altera `web/e2e/seletor-de-cor.spec.ts` deixa `test_C19_testes_tocados` em `OK`, como E5 manda; o commit seguinte em `arranjo.spec.ts` derruba a prova (F19), então a exclusão cobre só os dois arquivos nomeados.

E4 (AD-065) é emenda de texto com prova inalterada: não há asserção nova para fazer falhar. A superfície da prova de C24 foi feita falhar em F14.

## Findings for the owner

1. **Nenhum achado reprova esta rodada.** A AD-065 fecha o achado da rodada 2 e o código bate com ela: em `Autenticado.tsx:295-296` o `text-primary` da barra inferior só aparece junto com `aria-current="page"` e `font-semibold`; não existe outro `<a>` com `text-primary` sem `underline`.
2. **Gaps de precisão (não reprovam, carried).** C3 chama de "link" a aba ativa da barra inferior (`interface-marca.spec.ts:45-46`); nenhum E2E mede a cor de um link de conteúdo, só C24 por leitura estática. A regex de C24 só vê `<a className="...">` literal: um `RouterLink`, `cn(...)` ou `className={...}` futuro com `text-primary` passaria sem ser visto. C4 usa desigualdade contra Papel. A matiz de C9 só é lida no `index.css`. O reconhecimento da aba ativa "pelo peso 600" que a AD-065 descreve não tem check; peso tipográfico está na isenção do perfil `ui`, então fica registrado e não reprova.
3. **Capturas de Aparência anteriores ao `seletor-de-cor`.** As 4 capturas `aparencia-*.png` de C18 são de `62d7209`, antes de `e695042` mover o seletor para o lado do campo "Cor principal". C18 só prova nome e tamanho, e o arranjo do seletor é obrigação do `checks.md` de `seletor-de-cor`; se as capturas servirem de referência visual da tela, vale regravá-las.
4. **Ambiente.** O Vite do worktree usou o `node_modules` da árvore real por junção (o cache `web/node_modules/.vite` foi regravado; pasta ignorada). O `dryer` regrava `web/.metrics/dry.edn` (ignorado). O build de teste do worktree mostrou avisos CS8604 em `AparenciaAdminTests.cs`, `InstituicaoDominioTests.cs` e `PermissoesChamadoTests.cs`, que esta feature não tocou; o `dotnet build Esclari.slnx` da árvore real saiu com 0 avisos. Ficam em `C:\Users\...\Temp` os worktrees `seletor-wt`, `v2-wt-api`, `v2-wt-web` e `verif-m1`, que não são desta rodada e não foram tocados.

## Lessons

Esta rodada é um PASS limpo e não traz lição nova. A lição da rodada 2 continua para quem tem a skill registrar com `lessons.py`: uma prova estática sobre `<a className="...">` literal não vê componentes que renderizam `<a>` (`RouterLink`, `NavLink`, `asChild`) nem `className={...}`; uma regra sobre todo `<a>` renderizado pede varredura no navegador ou a lista dos componentes que renderizam âncora.

## Gate

- Vitest focado: 18 passed, 0 failed. Vitest inteiro: 1077 passed, 0 failed (115 arquivos). Cobertura: exit 0, 1077 passed.
- `ManifestoTests`: 7 passed, 0 failed.
- E2E C3, C4, C22, C23: 5 passed. E2E C20 (E2): 95 passed, 0 failed.
- `provar_interface.py`: 3 passed (C18, C19, C21 dryer). `git diff --exit-code` do C17: exit 0.
- Builds web e .NET: exit 0 (.NET com 0 avisos). `crapper`: exit 0, máximo 9.8.
- Faltas: 23 injetadas, 23 mortas.
- `mutator`: suspenso por decisão do usuário (AD-025), não rodou.
