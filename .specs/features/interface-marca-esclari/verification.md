# interface-marca-esclari verification

**Verdict**: FAIL
**Profile**: ui
**Diff range**: 49cba64..3a98b3b
**Round**: 2 - scoped (proofs re-run in full at `3a98b3b`; step 1, coverage, faults and citations redone on what `55c4df2` and `3a98b3b` touched; the rest is marked carried from `45b1193`)
**Verifier**: independent sub-agent (author != verifier), round 2

Resumo: as 24 provas (C1-C24, com E1-E3 no lugar do texto e da prova de C19-C21) saem verdes em `3a98b3b`, cada uma com asserção localizada. 28 faltas injetadas num worktree separado, 28 mortas. O veredito é FAIL por um único achado de step 1: a AD-064 diz que "every `<a>` with `text-primary` carries `underline`", e a aba ativa da barra inferior do celular (`web/src/Autenticado.tsx:292-296`, um `RouterLink` que renderiza `<a aria-current="page" class="... font-semibold text-primary">`) não tem `underline`. A prova de C24 lê só tags `<a className="...">` literais e não enxerga esse elemento. Correção barata nos dois sentidos (ver Findings 1). A árvore real terminou com o porcelain da linha de base (`?? .tmp-capturas/`).

## Round 1 (carried note)

Rodada 1 em `45b1193`: FAIL. C20 falhava (4 testes de `arranjo.spec.ts` que também falham na base), C21 falhava (`dryer` imprimia 745 candidatos em `src/ui/` e o código de saída não decidia nada), e duas suítes que passavam na base quebravam sem check: `web/e2e/ux.spec.ts:1250` (Raiz com texto na cor de ação) e `web/e2e/aparencia.spec.ts:80` (literais do azul antigo). Também: `guia-de-marca.md:33` desatualizado e três gaps de precisão (largura de 3 px em C3, desigualdade em C4, matiz de `.ts/.tsx` em C9). 13 faltas, 13 mortas.

O que mudou desde então: `55c4df2` (emendas E1-E3, AD-064, `test_C21_dryer` em `provar_interface.py`, guia corrigido) e `3a98b3b` (links do contato sublinhados em `web/src/aparencia.tsx`, teste novo "links text-primary sublinhados", `ux.spec.ts` com a condição da AD-064, `aparencia.spec.ts` com Tinta e Papel). Os achados 1 a 5 da rodada 1 estão resolvidos; os gaps de precisão seguem abaixo.

## Binding sources

Verificado em `3a98b3b`.

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `plan.md` Landing door 1 | yes - comparada token a token com `web/src/index.css:6-114` em `3a98b3b` | none - claro: os 20 valores (`--background` `:8`, `--card`/`--popover` `:10,12`, `--foreground` e textos de cartão/popover `:9,11,13`, `--primary` `:22`, `--primary-foreground` `:23`, `--primary-subtle` `:24`, `--sidebar` `:14`, `--sidebar-accent` `:18`, `--sidebar-border` `:20`, `--sidebar-primary` `:16`, `--muted` `:27`, `--accent` `:29`, `--border` `:34`, `--input` `:35`, `--muted-foreground` `:28`, `--destructive` `:33`, `--ring` `:36`) batem literalmente; `.form-resposta` `0.59 0.012 94` em `:113`. Escuro: os 15 (`:52-79`) batem, mais `--sidebar-primary` Papel em `:60`. Preto: `--primary-subtle` `0.24 0.04 281` em `:102`, fundo/cartão/texto inalterados. Porcentagens: claro 2/4/5/6/4/5/6/6, escuro 3/3/6/5/5/6/5/5, preto 6/5/4/6/5/5, iguais às de C1 | - |
| `plan.md` Landing door 2 | yes - contei 27 `color-mix(in oklch, var(--marca, X) N%, Y)` (10 em `:root`, 10 em `.dark`, 6 em `.black`, 1 no `.form-resposta`), todos com `X == Y`, mais `--primary`, `--sidebar-primary` e `--ring` do `:root` com `var(--marca, ` | none | - |
| `plan.md` Landing door 3 | carried from `45b1193` - nenhum arquivo da door 3 mudou em `45b1193..3a98b3b` | none | - |
| `pesquisa.md` | yes - Tinta `0.216 0.073 280.7` e Papel `0.970 0.010 93.6` arredondados para 281 e 94; erro claro `0.55` pelo par erro/fundo (4,37:1 com `0.577`); Origem fora de controle | none | - |
| AD-063 (`STATE.md:69`) | yes | none | - |
| AD-064 (`STATE.md:70`) | yes - comparada com C22, C23, C24 e com os `<a>` que o código renderiza (`git grep -n "text-primary" -- web/src ':!*.test.*'`) | none nos checks: C22 reproduz a regra (com cor: só botão, item ativo, link e foco; sem cor: regra de texto desligada, fundo e borda continuam); C24 cobre os 4 `<a>` literais, todos com `underline` (`aparencia.tsx:171,176`, `chamados/Conversa.tsx:64`, `LinkDeAnexo.tsx:8`); os links do contato têm `underline underline-offset-4` | `web/src/Autenticado.tsx:292-296`: a aba ativa da barra inferior é um `RouterLink` (renderiza `<a>`) com `aria-current="page"` e `font-semibold text-primary`, sem `underline`. A AD-064 diz "every `<a>` with `text-primary` carries `underline`" e lista o reconhecimento sem cor como "filled button, 3 px active bar, underline"; essa aba não tem nenhum dos três. A prova de C24 (`interface.test.ts:284`) só casa `<a\b[^>]*className="..."` literal, então não a vê. É o mesmo elemento que C3 mede como "link" (`interface-marca.spec.ts:45-46`, `a[aria-current=page].text-primary` a 360 px) |
| `redesign-marca-esclari-final/guia-de-marca.md` | yes - `:33` agora diz que, sem cor da instituição, a ação é Tinta no claro e Papel nos escuros (AD-063), Origem fora de controles e o azul só como cor pronta "Azul" | none - o achado 5 da rodada 1 foi resolvido em `55c4df2` | - |
| AD-027, AD-032, AD-034, AD-037, AD-060, AD-062 | carried from `45b1193` - nada dessas decisões foi tocado pelo fix | none | - |
| invariante "cor de ação só em botão principal, item ativo, link e foco" (`estrutura-visual` AC 22, `web/e2e/ux.spec.ts:1250`) | yes - agora sob a AD-064 e provado por C22 | none - `ux.spec.ts:1258` lê `--primary` e `--foreground`; `:1275` desliga só a regra de texto quando os dois pintam igual; `:1269-1273` (fundo) e `:1277` (borda) seguem | - |
| capturas (`capturas/*.png`, 28) | carried from `45b1193` - nenhuma captura mudou em `45b1193..3a98b3b`; a mudança visual do fix (sublinhado nos links do contato) não aparece nas 7 telas capturadas | none | - |

Telas tocadas pelo fix: só o bloco `Contato` (`aparencia.tsx:166-180`), que ganhou `underline underline-offset-4` nos dois links. Arranjo do bloco igual (coluna com horário, telefone e e-mail, `gap-1`); nada mais muda. Espaçamento e peso tipográfico continuam fora do alcance dos checks porque só cor e decoração mudam.

## Checks

Comandos rodados em `HEAD` `3a98b3b`.

- Vitest numa chamada (cwd `C:\`): `npm --prefix D:\esclari\web test -- src/test/interface.test.ts src/aparencia.test.tsx src/ux.test.tsx src/administracao/aparencia/guidelinesCores.test.tsx src/guidelinesGlobal.test.ts src/test/icones.test.ts -t "<17 nomes em alternância>" --reporter=verbose`: exit 0, `Test Files 6 passed`, `Tests 18 passed | 123 skipped`; cada um dos 17 nomes aparece com ✓ (o 18º é `contraste dos tokens da barra`, casado pelo prefixo).
- E2E numa chamada: `npm --prefix D:\esclari\web run e2e -- e2e/interface-marca.spec.ts e2e/ux.spec.ts e2e/aparencia.spec.ts -g "sem cor da instituicao a acao e Tinta ou Papel|com cor da instituicao a acao segue a cor|cor de ação só em botão principal, item ativo, link e foco|cor da instituição nos três temas"` contra :8080: exit 0, `5 passed` (`aparencia.spec.ts:80`, `interface-marca.spec.ts:50`, `:61`, `ux.spec.ts:1250`, `:1287` casado pelo prefixo).
- `python .specs/features/interface-marca-esclari/provar_interface.py -v`: `test_C18_capturas ok`, `test_C19_testes_tocados ok`, `test_C21_dryer ok`, exit 0.

Citações dos arquivos que o fix não tocou (`aparencia.test.tsx`, `ux.test.tsx`, `guidelinesCores.test.tsx`, `guidelinesGlobal.test.ts`, `icones.test.ts`, `ManifestoTests.cs`, `interface-marca.spec.ts`) carried from `45b1193`: `git diff --name-only 45b1193..3a98b3b` não os lista, então as linhas não mudaram. As de `interface.test.ts`, `ux.spec.ts`, `aparencia.spec.ts` e `provar_interface.py` foram refeitas em `3a98b3b`.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | 31 tokens da door 1 por tema, porcentagens de hoje | Vitest acima: ✓ `tokens da paleta`, ✓ `tokens de Visual` | `web/src/test/interface.test.ts:308-310` - `expect(claro[token]).toBe(valorClaro)` / `escuro` / `preto` sobre a tabela `Tokens` (`:129-161`); `:340` - `expect(t[seletor][linha[0]]).toBe(esperado)` com a cascata `.dark.black` | PASS |
| C2 | sem azul antigo; 27 fallbacks iguais à base | ✓ `fallback de cada neutro e a propria base` | `interface.test.ts:268` - `not.toContain(oklch(0.45 0.15 262) ...)`; `:270` - `toHaveLength(27)`; `:271` - `expect(reserva).toBe(base)`; `:272` - `toHaveLength(27 + 3)` | PASS |
| C3 | sem cor: botão, item ativo e "link" Tinta no claro, Papel no escuro e no preto, tolerância 2 | E2E acima: ✓ `sem cor da instituicao a acao e Tinta ou Papel` | `web/e2e/interface-marca.spec.ts:34` - `toBeLessThanOrEqual(2)` por canal; `:54-56` - `perto(botao/ativo/link, esperada)` sobre `[light, Tinta], [dark, Papel], [black, Papel]` (`:51`). Gap de precisão: o "link" medido é `a[aria-current=page].text-primary` a 360 px (`:45-46`), a aba ativa da barra inferior, não um link de conteúdo | PASS |
| C4 | com `#8a1538`: botão e item ativo Vinho, fundo tingido | ✓ `com cor da instituicao a acao segue a cor` | `interface-marca.spec.ts:64-65` - `perto(botao, Vinho)`, `perto(ativo, Vinho)`; `:67` - `expect(fundo).not.toEqual(Papel)` (gap de precisão da rodada 1 mantido: desigualdade exata) | PASS |
| C5 | derivações de `--marca`; 14 neutros em `color-mix` 2-6% | ✓ `tokens derivam de marca nos três temas`, ✓ `neutros tingidos pela marca nos três temas` | `web/src/aparencia.test.tsx:98-99` - `toMatch(/^var\(--marca[\w-]*, .+\)$/)`; `:101` - `toBe('color-mix(in oklch, var(--marca) 55%, white)')`; `:102-103` - `/^var\(--marca-clara, /` e `toBe('oklch(0.216 0.073 281)')`; `web/src/ux.test.tsx:134-139` - formato, `>= 2`, `<= 6`, total 14 | PASS |
| C6 | 8 pares de texto ≥ 4,5:1 nos 3 temas | ✓ `contraste dos tokens` | `interface.test.ts:242` - `toBeGreaterThanOrEqual(4.5)` sobre `ParesDeTexto` × 3 temas | PASS |
| C7 | bordas e foco ≥ 3:1, `.form-resposta` ≥ 3:1 | ✓ `contraste das bordas e do foco` | `interface.test.ts:249` - `toHaveLength(10)`; `:250` - `toBeGreaterThanOrEqual(3)` | PASS |
| C8 | 8 cores prontas × 11 pares × 3 temas = 264 | ✓ `contraste com as oito cores prontas` | `interface.test.ts:261` - `toBeGreaterThanOrEqual(minimo)`; `:264` - `toBe(264)` | PASS |
| C9 | sem `C83A23`/`F16A4B` nem matiz 30-36 com C > 0,1 | ✓ `Origem fora da interface` | `interface.test.ts:277-278` - `not.toMatch(/c83a23\|f16a4b/i)` no CSS e nos `.ts/.tsx` fora de teste; `:280` - `quentes` igual a `[]` (gap de precisão da rodada 1 mantido: a matiz só é lida no `index.css`) | PASS |
| C10 | fontes, raios 4/6/12/16, gráficos intactos | ✓ `tipografia raios e graficos intactos` | `interface.test.ts:291-293` - `--font-sans`, `--font-heading`, raios `toEqual(['4px', '6px', '12px', '16px'])`; `:295-296` - os 8 valores de `--grafico-1..4` | PASS |
| C11 | `.form-resposta` com base `0.59 0.012 94` e 6% | ✓ `regra da borda do form da resposta` | `interface.test.ts:328` - um só seletor `.form-resposta:not(.dark *)`; `:331` - `toBe('--input: ' + token com 0.59 0.012 94)` | PASS |
| C12 | preset Azul `#224fa7`; `--primary` cai em Tinta | ✓ `preset Azul mantem o valor sem o sobrenome Esclari` | `web/src/administracao/aparencia/guidelinesCores.test.tsx:55-57` - `toContain('--primary: var(--marca, oklch(0.216 0.073 281))')`, `not.toContain(oklch(0.45 0.15 262))`, `toContain("['Azul', '224fa7']")` | PASS |
| C13 | `index.html` e `corDaBarra.json` | ✓ `C7 index.html traz theme-color por tema` | `web/src/guidelinesGlobal.test.ts:200` - `toEqual(['#f7f5ee', '#0e0e18'])`; `:201` - `CorDaBarra` `toEqual({ light, dark, black: '#000000' })` | PASS |
| C14 | Escuro, Claro, Preto gravam `#0e0e18`, `#f7f5ee`, `#000000` | ✓ `C7 trocar o tema atualiza o theme-color sem media` | `guidelinesGlobal.test.ts:209` - literais; `:213` - `expect(cor(), rotulo).toBe(esperado)` | PASS |
| C15 | `theme_color` e `background_color` `#f7f5ee` | ✓ `C8 manifest com id, scope, ícone maskable e background igual ao theme_color claro` | `guidelinesGlobal.test.ts:264` - `toEqual(['#f7f5ee', '#f7f5ee'])` | PASS |
| C16 | manifesto da API: fundo `#F7F5EE`, tema `#8a1538` / `#C83A23` | `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests"`: exit 0, `Total de testes: 7`, `Aprovados: 7` (`Manifesto_da_instituicao`, `Manifesto_padrao_sem_aparencia`, `Manifesto_declara_o_fundo_do_app` entre eles) | `Esclari.Api.Tests/ManifestoTests.cs:26` - `"#8a1538", "#F7F5EE"` com aparência; `:49` - `Assert.Equal("#C83A23", theme_color)`; `:129` - `Assert.Equal("#F7F5EE", background_color)`, pela rota HTTP | PASS |
| C17 | cantos dos `any` Papel ±3; maskable e apple iguais à base | ✓ `margem dos icones any em Papel`; `git diff --exit-code 49cba64 -- web/public/icons/icon-maskable-512.png web/public/icons/apple-touch-icon.png`: exit 0 | `web/src/test/icones.test.ts:177` - `parecida(imagem.pixel(x, y), Papel)` nos 4 cantos; `:67` - tolerância `<= 3`. `git status` continuou limpo depois do teste, que regrava os ícones | PASS |
| C18 | 28 capturas `<tela>-<tema>-<largura>.png` nos tamanhos certos | `provar_interface.py -v`: `test_C18_capturas ... ok` | `.specs/features/interface-marca-esclari/provar_interface.py:42` - 28 nomes; `:44` - conjunto presente igual ao esperado; `:46` - 360×780 e 1280×800 | PASS |
| C19 (E1) | só os 9 arquivos de teste da lista (7 originais + `ux.spec.ts` e `aparencia.spec.ts`) mudam | `provar_interface.py -v`: `test_C19_testes_tocados ... ok` | `provar_interface.py:52` - `sorted(tocados - TESTES_PERMITIDOS) == []`, com a lista em `:14-24`; conferido à mão com `git diff --name-only 49cba64..HEAD`: os 9, nada mais | PASS |
| C20 (E2) | arranjo, celular, casca-celular e guidelines passam, exceto os 4 de `arranjo.spec.ts` pré-existentes | `npm --prefix D:\esclari\web run e2e -- e2e/arranjo.spec.ts e2e/celular.spec.ts e2e/casca-celular.spec.ts e2e/guidelines.spec.ts --grep-invert "arranjo Indicadores\|raiz com a marca e o texto centralizados\|quebras medidas dos dois lados\|valores de tipo e bordas"`: exit 0, `Running 95 tests`, `95 passed` | as 95 são as mesmas que passaram na rodada 1 (99 - 4). Os 4 excluídos rodados à parte em `HEAD`: `4 failed` com as mesmas mensagens da base (`strict mode violation: getByLabel('De') resolved to 7 elements`, timeout, `Expected "24px" Received "16px"`, timeout); suites em `web/e2e/arranjo.spec.ts`, `celular.spec.ts`, `casca-celular.spec.ts`, `guidelines.spec.ts` | PASS |
| C21 (E3) | build web, build .NET, Vitest inteiro, crapper < 10 saem 0; `dryer` sobre as 6 fatias e a raiz imprime "No duplicate candidates found." | `npm --prefix web run build` exit 0; `dotnet build Esclari.slnx` exit 0 (`0 Aviso(s)`, `0 Erro(s)`); `npm --prefix web test -- --maxWorkers=4` exit 0, `115 passed`, `1075 passed`; `npm --prefix web run coverage` exit 0; `crapper --source-root src --use-existing-coverage --threshold 10` exit 0, maior nota 9.8 (`Sidebar`); `provar_interface.py -k test_C21_dryer` ok, e o mesmo `dryer` rodado à mão imprime `No duplicate candidates found.` | `provar_interface.py:60` - `returncode == 0`; `:61` - `assertIn("No duplicate candidates found.", r.stdout)`; `:62` - `assertNotIn("DUPLICATE", r.stdout)`. Mutação suspensa (AD-025) | PASS |
| C22 | com cor, cor de ação só em botão principal, item ativo, link e foco; quando `--primary` pinta igual a `--foreground` (Raiz), a regra de texto sai e fundo e borda ficam | E2E acima: ✓ `ux.spec.ts:1250 cor de ação só em botão principal, item ativo, link e foco` | `web/e2e/ux.spec.ts:1258` - lê `--primary` e `--foreground`; `:1280` - `primaria.join() === texto.join()`; `:1275` - `if (!acaoIgualAoTexto && igual(s.color, '#fff') && !el.closest('a, ...'))`; `:1269-1273` fundo e `:1277` borda sem a condição; `:1281` - `expect(indevidos).toEqual([])`; `:1283` - `comAparencia` | PASS |
| C23 | botão Entrar: `#8a1538` no claro e `color-mix(... 55%, white)` no escuro e no preto com cor; Tinta no claro e Papel no escuro sem cor | E2E acima: ✓ `aparencia.spec.ts:80 cor da instituição nos três temas` | `web/e2e/aparencia.spec.ts:83-87` - os 5 casos com os literais do claim; `:95` - `expect(fundo).toBe(esperado)`, com `esperado` pintado num `div` com a referência (`:70-73`) | PASS |
| C24 | todo `<a>` com `text-primary` fora de testes tem `underline` (pelo menos 4) | Vitest acima: ✓ `links text-primary sublinhados` | `interface.test.ts:284` - `matchAll(/<a\b[^>]*className="([^"]*\btext-primary\b[^"]*)"/g)` sobre os `.tsx` fora de `.test.`; `:285` - `toBeGreaterThanOrEqual(4)`; `:286` - `expect(classes.split(' ')).toContain('underline')`. Os 4 que casam: `aparencia.tsx:171,176`, `Conversa.tsx:64`, `LinkDeAnexo.tsx:8`. A prova não vê `RouterLink`, `cn(...)` nem `className={...}` (ver Binding sources, AD-064) | PASS |

## Coverage

Recomputado em `3a98b3b` para os conjuntos que o fix tocou; o resto carried from `45b1193`, com as autoridades inalteradas.

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| portas do Landing (3) | `plan.md` Landing | door 1 C1 · door 2 C2, C5 · door 3 C13-C17 | - |
| tokens da door 1 (claro 20 + `.form-resposta`, escuro 15 + `--sidebar-primary`, preto 1) | `plan.md` door 1 contra `index.css:6-114`, refeito nesta rodada | todos na tabela `Tokens` de C1 com o mesmo literal; `.form-resposta` C11 | - |
| `color-mix` com fallback (27) | contagem própria em `index.css`: 10 + 10 + 6 + 1 | C2, contagem e igualdade nos 27 | - |
| cores prontas (8) | `CoresProntas.tsx:5-12` | C8 lê o arquivo e mede os 8 | - |
| lugares do fundo do app (8) | carried from `45b1193` | C13 ×3 · C14 · C15 ×2 · C16 · C17 | - |
| ícones do PWA (4) | `web/public/icons/` | `icon-192`, `icon-512` C17 teste · `icon-maskable-512`, `apple-touch-icon` C17 `git diff` | - |
| elementos de ação × temas (9) | AC 2 | C3, 3 × 3 (o "link" é a aba ativa da barra inferior, ver C3) | - |
| pares 4,5:1 (24) e 3:1 (10) | AC 4 | C6 (8 × 3), C7 (3 × 3 + 1) | - |
| capturas (28) | AC 11 | C18 | - |
| invariantes (4) | AC 6 + Impact | tipografia, raios, gráficos C10 · preset Azul C12 | - |
| gates do AC 13 (6) | AC 13 + E3 | build web, build .NET, Vitest, crapper C21 · `.NET` tocados C16 · dryer C21 (E3) | - |
| testes alterados (9) | `git diff --name-only 49cba64..3a98b3b` filtrado por teste | C19 (E1) | - |
| regra da cor de ação da AD-064 (3 ramos) | `STATE.md:70` | com cor, só botão/item ativo/link/foco C22 · sem cor, regra de texto desligada C22 (falta E5) · sem cor, fundo e borda seguem C22 (falta E4) | - |
| reconhecimento da ação sem cor, AD-064 (4) | `STATE.md:70` | botão preenchido C23 (login) e C3 · barra de 3 px do item ativo: cor em C3, largura na suíte existente `web/e2e/casca.spec.ts:273` `item ativo com barra de 3 px` (rodada em `HEAD`: `1 passed`; não é prova de check) · links do contato com `underline underline-offset-4` C24 (só `underline`) · todo `<a>` com `text-primary` sublinhado C24 nos 4 `<a>` literais | aba ativa da barra inferior (`Autenticado.tsx:292-296`): `<a>` com `text-primary` sem `underline` e sem barra de 3 px |

O `checks.md` escreve a linha "ação reconhecível sem cor própria (3)" sem a célula `Unproven` e mantém o cabeçalho "21 checks em 3 slices"; são lapsos do artefato, sem efeito nas provas.

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| `index.css` decide (fallback, 3 temas) | `web/src/index.css` | estática (C1, C2, C5-C11) e E2E (C3, C4, C23) | yes - cada token da door 1 por tema, cada fallback, pares por tema e por cor pronta; verificado em `3a98b3b` |
| `ManifestoInstituicao` constante | `Esclari.Api/Instituicoes/Carga/ManifestoInstituicao.cs` | uma na rota HTTP | yes - `ManifestoTests.cs:26` com aparência, `:129` sem |
| `gerar-icones.mjs` lê `theme_color` | `web/scripts/gerar-icones.mjs` | uma sobre o PNG gerado | yes - `icones.test.ts:177`; maskable e apple pelo `git diff` |

`web/src/aparencia.tsx` (tocado pelo fix) não tem linha própria: a mudança é uma classe estática, provada pela leitura estática de C24, como a política do repositório faz para classes.

## Faults injected

Todas num worktree separado (`git worktree add --detach D:\esclari-v2 HEAD`, `node_modules` por junção), cada uma revertida com `git -C D:\esclari-v2 checkout -- <arquivo>` antes da seguinte. Controles sem falta antes: Vitest `3 passed` no worktree; E2E `4 passed` contra o Vite do worktree. As E2E de produto rodaram contra o Vite do worktree; as mutações de teste (E5, E7) contra :8080 ou o Vite, ambos em `HEAD`. P2 fez um commit só no worktree destacado, desfeito com `reset --hard 3a98b3b` no próprio worktree. Depois: junção removida, worktree removido, porcelain da árvore real igual à linha de base (`?? .tmp-capturas/`).

| Mutation | Location | Killed |
| --- | --- | --- |
| V1 `--muted-foreground` claro `0.48` -> `0.50` | `web/src/index.css:28` | yes - C1 (`expected '0.50 0.012 281' to be '0.48 0.012 281'`, 2 testes) |
| V2 fallback do `--border` claro trocado por Tinta | `index.css:34` | yes - C2 (`expected 'oklch(0.216 0.073 281)' to be 'oklch(0.89 0.012 94)'`) |
| V3 `--marca-clara` 55% -> 20% | `index.css:45` | yes - C5 (`expected 'color-mix(... 20%,…' to be '... 55%,…'`) |
| V4 `--marca-sutil-escuro` 40% -> 90% | `index.css:46` | yes - C8 (`Azul .dark --muted-foreground/--primary-subtle: 3.377 >= 4.5`) |
| V5 `--destructive` claro `0.55` -> `0.60` | `index.css:33` | yes - C6 (`:root --destructive/--background: 4.036 >= 4.5`) |
| V6 `--input` escuro `0.58` -> `0.40` | `index.css:79` | yes - C7 (`.dark --input/--background: 2.074 >= 3`) |
| V7 `--muted` preto 4% -> 8% | `index.css:103` | yes - C5, `ux.test.tsx` (`.black --muted: expected 8 to be <= 6`) |
| V8 `corDaBarra.json` dark `#0e0e18` -> `#101113` | `web/src/corDaBarra.json` | yes - C14 (`Escuro: expected '#101113' to be '#0e0e18'`) |
| V9 `theme-color` claro -> `#fafbfd` | `web/index.html:6` | yes - C13 (`expected '#fafbfd' to be '#f7f5ee'`) |
| V10 `background_color` -> `#fafbfd` | `web/vite.config.ts:27` | yes - C15 (`expected '#fafbfd' to be '#f7f5ee'`) |
| V11 preset novo `['Origem', 'c83a23']` | `web/src/administracao/aparencia/CoresProntas.tsx:12` | yes - C9 (`expected [ Array(1) ] to deeply equal []`) |
| V12 `--radius-md` 12px -> 10px | `index.css:129` | yes - C10 (`[ '4px', '6px', '10px', '16px' ]`) |
| V13 base do `.form-resposta` `0.59` -> `0.62` | `index.css:113` | yes - C11 |
| V14 preset Azul `224fa7` -> `224fa8` | `CoresProntas.tsx:5` | yes - C12 (`1 failed`) |
| V15 (novo) `underline` tirado do link do telefone | `web/src/aparencia.tsx:171` | yes - C24 (`aparencia.tsx: expected [ Array(2) ] to include 'underline'`) |
| V16 margem dos ícones `any` pintada `#fafbfd` | `web/scripts/gerar-icones.mjs:44` | yes - C17 (`icon-192.png 0,0: expected false to be true`); PNGs regravados revertidos |
| P1 uma captura apagada (`fila-escuro-360.png`) | `capturas/` | yes - C18 (`Lists differ`) |
| P2 (novo) teste fora da lista alterado e commitado | `web/e2e/arranjo.spec.ts` | yes - C19 (`['web/e2e/arranjo.spec.ts'] != []`) |
| P3 (novo) cópia de `Chamados.tsx` numa fatia | `web/src/chamados/CopiaDaFalta.tsx` | yes - C21 E3 (`'No duplicate candidates found.' not found`, `DUPLICATE score=1.00 src/chamados/Chamados.tsx:40-61`) |
| D1 `FundoDoApp` -> `#FAFBFD` | `Esclari.Api/Instituicoes/Carga/ManifestoInstituicao.cs:11` | yes - C16 (`Manifesto_declara_o_fundo_do_app`, `Manifesto_da_instituicao`: `Expected "#F7F5EE" Actual "#FAFBFD"`) |
| E1 `--sidebar-primary` escuro com fallback Tinta | `index.css:60` | yes - C3 (`dark item ativo: 21,19,58 contra 247,245,238`) |
| E2 `--primary` claro sem `var(--marca, ...)` | `index.css:22` | yes - C4 (`botão: 21,19,58 contra 138,21,56`) |
| E3 (novo) texto do login em `text-primary` com cor da instituição | `web/src/acesso/Acesso.tsx:237` | yes - C22 (`Login: cor de ação fora de botão principal, item ativo, link e foco`) |
| E4 (novo) borda `border-primary` num parágrafo da Raiz, sem cor | `Acesso.tsx:58` | yes - C22 (`Raiz: ...`, 1 achado): fundo e borda seguem valendo quando a regra de texto sai |
| E5 (novo) condição da AD-064 removida da varredura | `web/e2e/ux.spec.ts:1275` | yes - C22 (`Raiz: ...`, 7 achados de texto, como na rodada 1) |
| E7 (novo) literais de `aparencia.spec.ts` voltam ao azul antigo | `web/e2e/aparencia.spec.ts:86-87` | yes - C23 contra `HEAD` (`light sem cor: Expected "oklch(0.45 0.15 262)" Received "oklch(0.216 0.073 281)"`) |
| E8 (novo) `--primary` escuro com fallback `0.95 0.008 94` | `index.css:66` | yes - C23 (`dark sem cor: Expected "oklch(0.97 0.01 94)" Received "oklch(0.95 0.008 94)"`) |
| E9 (novo) `--foreground` claro = `var(--primary)`, texto na cor da instituição | `index.css:9` | yes - C1 (`:root --foreground: expected 'var(--primary)'`) e C22 (`Artigos: ...`, pela regra de fundo, já que a de texto sai quando os dois tokens pintam igual) |

Isenção forçada a `true` (`ux.spec.ts:1280`, `[primaria, true]`): sozinha, `1 passed`; somada à falta E3, `1 passed`. É uma mutação do teste, e por construção nenhuma prova a pega: com a isenção sempre ligada, a regra de texto não vale em tela nenhuma. O que mostra que a isenção real não está sempre ligada é E3 morrer com a condição verdadeira. E9 mostra o limite da condição: se o texto passar a ter a cor da instituição, a regra de texto se desliga sozinha; C1 pega o caso estático e C22 só o pegou por um selo `bg-foreground` em Artigos.

## Findings for the owner

1. **AD-064 x aba ativa da barra inferior (reprova esta rodada).** `web/src/Autenticado.tsx:292-296` renderiza `<a aria-current="page">` com `font-semibold text-primary` e sem `underline` em toda tela autenticada a menos de 768 px. A AD-064 diz que todo `<a>` com `text-primary` tem `underline` e que, sem cor, a ação se reconhece por botão preenchido, barra de 3 px ou sublinhado; essa aba não tem nenhum dos três (se distingue das outras pelo peso e pela Tinta contra o cinza `--muted-foreground`). C24 não a vê porque a regex só casa `<a className="...">` literal. Decidir uma das duas: (a) emendar a AD-064 para "todo link `<a>` com `text-primary` que não seja item de navegação ativo (`aria-current=page`)", o mesmo recorte que a varredura de `ux.spec.ts:1275` já faz, e registrar que a aba ativa se reconhece pelo peso; ou (b) dar à aba ativa um sinal próprio (sublinhado ou barra) e estender C24 ao DOM renderizado. A (a) não muda código e fecha com uma emenda de texto e uma linha no `checks.md`.
2. **Gaps de precisão (não reprovam).** C3 chama de "link" a aba ativa da barra inferior (`interface-marca.spec.ts:45-46`); nenhum E2E mede a cor de um link de conteúdo, só C24 por leitura estática. C24 não exige `underline-offset-4`, que a AD-064 nomeia para os links do contato (o código tem). C4 continua com desigualdade exata contra Papel. A matiz de C9 continua lida só no `index.css`. A largura de 3 px do item ativo é provada por `casca.spec.ts:273`, que não é prova de nenhum check.
3. **Artefatos.** `checks.md` ainda diz "21 checks em 3 slices", e a linha "ação reconhecível sem cor própria (3)" do Coverage não tem a célula `Unproven`. O `## Handoff` do `STATE.md:74` ainda diz "Próximo passo: Verificador".
4. **Ambiente.** O Vite do worktree usou o `node_modules` da árvore real por junção, então o cache de dependências `web/node_modules/.vite` foi regravado (pasta ignorada). O `dryer` da E3 regrava `web/.metrics/dry.edn` (ignorado). O build de teste do worktree mostrou avisos CS8604/CS8631/xUnit2013 em arquivos de teste que esta feature não tocou; o `dotnet build Esclari.slnx` da árvore real saiu com 0 avisos.

## Lessons

Para quem tem a skill registrar com `lessons.py` (esta rodada só escreve o `verification.md`): uma prova estática sobre `<a className="...">` literal não vê componentes que renderizam `<a>` (`RouterLink`, `NavLink`, `asChild`) nem `className={...}`; uma regra sobre todo `<a>` renderizado pede uma varredura no navegador ou a lista dos componentes que renderizam âncora.

## Gate

- Vitest focado: 18 passed, 0 failed. Vitest inteiro: 1075 passed, 0 failed (115 arquivos). Cobertura: exit 0.
- `ManifestoTests`: 7 passed, 0 failed.
- E2E C3, C4, C22, C23: 5 passed. E2E C20 (E2): 95 passed, 0 failed; os 4 excluídos falham como na base. `casca.spec.ts` `item ativo com barra de 3 px`: 1 passed.
- `provar_interface.py`: 3 passed (C18, C19, C21 dryer). `git diff --exit-code` do C17: exit 0.
- Builds web e .NET: exit 0. `crapper`: exit 0, máximo 9.8.
- Faltas: 28 injetadas, 28 mortas.
- `mutator`: suspenso por decisão do usuário (AD-025), não rodou.
