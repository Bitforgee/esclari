# seletor-de-cor verification

**Verdict**: PASS
**Profile**: ui
**Diff range**: b9c5cad..ad370f8 (código da feature em `e695042`; `671d093` e `ad370f8` só mexem em docs de `interface-marca-esclari`)
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

Resumo: as 7 provas (C1-C7) saem verdes no `HEAD`, cada uma com asserção localizada no valor que o check nomeia. 10 faltas injetadas num worktree separado (6 no Vitest, 4 no Chromium servido pelo Vite na 5182), 10 mortas; uma 11ª mutação (tirar só o `data-amostra`) é equivalente e está explicada em Findings 1. A tela bate com o pedido do dono a 360×780 e a 1280×800, no claro e no escuro. A árvore real terminou com o porcelain da linha de base (`?? .tmp-capturas/`).

## Binding sources

Não há `plan.md`; a fonte que decide é o `## Intent` do `checks.md`, aprovado pelo dono em 17/11 ("siga com as recomendações") e registrado na AD-065.

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `checks.md` `## Intent` (proposta aprovada pelo dono: botão junto do campo "Cor principal", amostra da cor atual, texto visível "Escolher cor", amostra neutra sem cor, seletor nativo mantido) | yes | none - posição junto do campo: C1 (mesmo pai) e C5 (mesma linha, ±2 px); texto visível: C1 (`textContent`, sem `.sr-only`) e C5 (clique no texto visível); amostra da cor / neutra: C2; seletor nativo: C1 (`type="color"`, `aparenciaAbas.test.tsx:388`); toque e teclado: C5 (alvo ≥ 44 px, clique foca) e C4 (ordem de Tab) | - |
| AD-065 (`.specs/STATE.md:71`), trecho `seletor-de-cor` | yes | none - descreve a mesma proposta ("labelled button 'Escolher cor' with a swatch of the current colour (dashed neutral swatch without a valid colour)") | - |
| MDN `<input type="color">` (referência do Intent) | yes - https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/input/color | none nos checks - a MDN confirma que valor ausente ou inválido vira `#000000`, que é o motivo da amostra neutra não depender do input; ver Findings 3 sobre "só aceita `#rrggbb`" | - |
| HTMLInput.com, Codefronts, Once UI, Eddie DS | não - o Intent os marca como referências parafraseadas de apoio, não como fonte que decide | n/a | - |

Enumeração da tela (aba Marca da Aparência), controles que a decisão fixa, na ordem em que aparecem:

| Elemento / arranjo | Decidido por | Check |
| --- | --- | --- |
| grupo "Cores prontas" (8 cores) acima do campo, sem o seletor dentro | Intent ("nada muda nas cores prontas"; seletor sai dali) | C1 (`aparenciaAbas.test.tsx:391`) |
| rótulo "Cor principal" e campo de texto | existente | C1, C3 |
| botão "Escolher cor" na mesma linha, à direita do campo | Intent | C1 (`:390`), C5 (`seletor-de-cor.spec.ts:15`) |
| amostra dentro do botão: cor atual ou neutra tracejada | Intent | C2 |
| texto visível "Escolher cor" | Intent | C1 (`:392-393`), C5 (`:19`) |
| linha de contraste abaixo do campo | existente (fora do escopo) | C3 (`:416`) |
| ordem de Tab: cor marcada, campo, botão | consequência do arranjo | C4 |

Fora de alcance de seletor nesta tela: espaçamento entre campo e botão, cor da borda tracejada e peso da fonte do botão. Julgados nas capturas (Visual check abaixo).

## Checks

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | seletor `type=color` "Escolher cor" no mesmo pai do campo, fora do grupo "Cores prontas", texto visível no `<label>` | invocação única do Vitest (comando em Gate) exit 0, 4 passed, `✓ Cores prontas > seletor de cor ao lado do campo Cor principal` | `web/src/administracao/aparencia/aparenciaAbas.test.tsx:390` - `expect(rotulo.parentElement).toBe(campo.parentElement)`; `:391` - `expect(screen.getByRole('radiogroup', { name: 'Cores prontas' }).parentElement!.contains(seletor)).toBe(false)`; `:392-393` - `toHaveTextContent('Escolher cor')`, `querySelector('.sr-only')).toBeNull()` | PASS |
| C2 | `#8a1538` dá `background-color` `#8a1538`; vazio e `#12` dão amostra sem fundo e com `border-dashed` | mesma invocação, `✓ Cores prontas > amostra do seletor mostra a cor ou fica neutra` | `aparenciaAbas.test.tsx:401-402` - `expect(amostra().style.backgroundColor).toBe('rgb(138, 21, 56)')`, `not.toHaveClass('border-dashed')`; `:405-406` (laço sobre `''` e `#12`) - `toBe('')`, `toHaveClass('border-dashed')` | PASS |
| C3 | escolher `#ffff00` no seletor preenche o campo, contraste em vermelho, Salvar desabilitado | mesma invocação, `✓ Cores prontas > cor sem contraste bloqueia o Salvar` | `aparenciaAbas.test.tsx:415-417` - `toHaveValue(hex('ffff00'))`, `toHaveClass('text-destructive')`, `expect(salvar()).toBeDisabled()` | PASS |
| C4 | Tab: cor marcada, "Cor principal", "Escolher cor" | mesma invocação, `✓ CoresProntas por teclado > C25 Tab atravessa o grupo numa só parada` | `web/src/administracao/aparencia/guidelinesCores.test.tsx:65,67,69` - `radio 'Roxo' toHaveFocus()`, `getByLabelText('Cor principal')).toHaveFocus()`, `getByLabelText('Escolher cor')).toHaveFocus()` | PASS |
| C5 | Chromium a 360×780 e 1280×800: mesma linha (≤ 2 px), dentro da página, ≥ 44 px a 360, clique no texto foca o input | invocação única do E2E (comando em Gate) exit 0, 3 passed, `✓ e2e\seletor-de-cor.spec.ts:6:1 › seletor ao lado do campo` | `web/e2e/seletor-de-cor.spec.ts:15` - `toBeLessThanOrEqual(2)` (centros); `:16-17` - `r.x + r.width` e `scrollWidth` `toBeLessThanOrEqual(largura)`; `:18` - `r.height` `toBeGreaterThanOrEqual(44)` a 360; `:19-20` - clique em `getByText('Escolher cor')`, `toBeFocused()` | PASS |
| C6 | a amostra do seletor não conta como uso indevido da cor de ação | mesma invocação E2E, `✓ e2e\ux.spec.ts:1250:3 › S6 varreduras › cor de ação só em botão principal, item ativo, link e foco` | `web/e2e/ux.spec.ts:1281` - `expect(indevidos, ...).toEqual([])` sobre a tela Aparência (`web/e2e/simulada.ts:162`) com `cor: '#8a1538'` (`simulada.ts:28`); isenção em `ux.spec.ts:1272` (`data-amostra`) e também `:1271` (`aria-hidden` sem texto), ver Findings 1 | PASS |
| C7 | build, Vitest inteiro, cobertura, `crapper` < 10, `dryer` sem candidato | `npm --prefix web run build` exit 0; `npm --prefix D:\esclari\web test -- --maxWorkers=4` exit 0; `npm --prefix web run coverage` exit 0, `Test Files 115 passed (115)`, `Tests 1077 passed (1077)`; `crapper --source-root src --use-existing-coverage --threshold 10` exit 0 (`SeletorDeCor` 4.0, `LinhaDaEntrada` 2.0, `CoresProntas` 4.0); `python .specs/features/interface-marca-esclari/provar_interface.py -k test_C21_dryer` `Ran 1 test ... OK` | `web/src/administracao/aparencia/CoresProntas.tsx:57-77` e `Aparencia.tsx:130-139` medidos pelo `crapper` (saída acima); exit codes como asserção | PASS |

## Coverage

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| estados da amostra (3) | código: `CoresProntas.tsx:58` `valida = CorValida.test(cor)` decide `:64-65`; o Intent separa vazio e inválido | cor válida C2 (`:401`) · vazio C2 (`:405`, `''`) · hexadecimal inválido C2 (`:405`, `#12`) | - |
| entradas do seletor (3) | Intent ("funciona com toque e teclado") e `CoresProntas.tsx:68-73` (`onChange`, input sobre o rótulo inteiro) | clique C5 (`seletor-de-cor.spec.ts:19-20`) · teclado C4 (`guidelinesCores.test.tsx:69`) · troca de valor C3 (`aparenciaAbas.test.tsx:414-415`) | - |
| tamanhos (2) | Intent e `AGENTS.md` (celular primeiro) | 360×780 C5 · 1280×800 C5 (laço `seletor-de-cor.spec.ts:8`) | - |
| conjuntos sem linha no `checks.md` | varredura do Intent e das claims | temas claro/escuro: o Intent não os nomeia; julgados nas capturas, abaixo. Sem `plan.md`, as linhas "deitado 844×390" e "teclado aberto" do AGENTS.md não são devidas (elas valem para `plan.md`) | - |

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| `SeletorDeCor` (decide: amostra com cor ou neutra) | `web/src/administracao/aparencia/CoresProntas.tsx` | uma na própria tela: `aparenciaAbas.test.tsx:396-407` renderiza `/univag/aparencia` | yes - cor válida, vazia e inválida em `:401-406` |

Swept `existing`, relidos no código: validação, `corProibida` em `Aparencia.tsx:104` com `CorValida` e `MinimoDeContraste`, provada por C3; autorização, a rota `aparencia` em `web/src/App.tsx:74` e o item de menu com `papeis: ['ADMINISTRADOR']` em `web/src/Autenticado.tsx:30`, nenhum dos dois tocado pelo diff.

## Faults injected

Worktree `%TEMP%\seletor-wt` em `671d093` (mesmo código de `ad370f8`), `node_modules` por junção; E2E contra o Vite do worktree na 5182 (`E2E_URL=http://localhost:5182`, linha de base 2/2 verde antes das faltas). Cada falta foi revertida com `git -C <wt> checkout -- .` antes da seguinte.

| Mutation | Location | Killed |
| --- | --- | --- |
| F1 seletor de volta para dentro do bloco das cores prontas (sai de `LinhaDaEntrada`) | `Aparencia.tsx:136`, `CoresProntas.tsx:52` | yes - C1 (`:390`, pai diferente) e C4 |
| F2 amostra sempre neutra (classe fixa `border-dashed bg-muted`, sem `style`) | `CoresProntas.tsx:64-65` | yes - C2 (`expected '' to be 'rgb(138, 21, 56)'`) |
| F3 amostra sempre preenchida, preta sem cor válida (`backgroundColor: valida ? cor : '#000000'`, sem tracejado) | `CoresProntas.tsx:64-65` | yes - C2 (`expected 'rgb(0, 0, 0)' to be ''`) |
| F4 texto só para leitor de tela (`<span className="sr-only">Escolher cor</span>`) | `CoresProntas.tsx:67` | yes - C1 (`expected <span class="sr-only"></span> to be null`) |
| F5 botão antes do campo na ordem de Tab | `Aparencia.tsx:135-136` | yes - C4 |
| F6 `onChange` não repassa a cor escolhida (`() => escolher(cor)`) | `CoresProntas.tsx:72` | yes - C3 |
| B1 rótulo com 36 px no celular (sem `max-md:h-11 pointer-coarse:h-11`) | `CoresProntas.tsx:60` | yes - C5 (`alvo de toque`, `Expected: >= 44`, `Received: 36`) |
| B2 botão numa linha abaixo do campo (`flex-col items-start`) | `Aparencia.tsx:134` | yes - C5 (`360: mesma linha`, `Received: 45.5`) |
| B3 texto presente mas invisível (`<span className="invisible">`) | `CoresProntas.tsx:67` | yes - C5 (clique no texto não acontece; teste falha por tempo) |
| B5 amostra sem `data-amostra` e sem `aria-hidden` | `CoresProntas.tsx:62-63` | yes - C6 (`Aparência: ... "fundo span.size-5 shrink-0 rounded-xs border "`) |

Mutação equivalente, fora da tabela: B4, tirar só o `data-amostra` (`CoresProntas.tsx:62`), deixa C6 verde (2 passed). A amostra continua isenta pela regra de sigla (`ux.spec.ts:1271`: `aria-hidden="true"` e texto com até 2 caracteres), então a afirmação observável de C6 continua verdadeira; o que fica sem prova é só o parêntese "(atributo `data-amostra`)". Findings 1.

Linha de base da árvore real antes e depois: `?? .tmp-capturas/`. `git worktree remove --force` feito, junção removida com `cmd /c rmdir`, porta 5182 livre.

## Visual check

Capturas em `%TEMP%\seletor-capturas\` (fora do repo), tela `/univag/aparencia` com a API simulada, com cor `#8a1538` e sem cor, temas `light` e `dark`, 360×780 e 1280×800 (8 combinações):

- 360×780: campo em `16,273 189×44` e botão em `213,273 131×44` (mesma linha, mesma altura, dentro dos 360 px). 1280×800: campo `280,209 429×36`, botão `717,209 131×36`.
- Com cor: amostra `rgb(138, 21, 56)` 20×20 dentro do botão, com o texto "Escolher cor" ao lado, nos dois temas.
- Sem cor: amostra `border-style: dashed` com o fundo `bg-muted` (claro `oklch(0.945 0.012 94)`, escuro `oklch(0.22 0.02 281)`), sem preto em nenhum tema.
- O grupo das 8 cores prontas fica acima de "Cor principal" e não tem mais o quadrado preto no fim.

Resultado contra o pedido: atende. O botão fica junto do campo, mostra a cor atual, o texto "Escolher cor" está visível e a amostra sem cor é neutra. Só a borda tracejada aparece pouco (Findings 2).

## Findings for the owner

1. C6 (precisão): o `data-amostra` posto na amostra não é o que a isenta na varredura. Ela já passa pela regra de sigla (`aria-hidden` sem texto, `ux.spec.ts:1271`). A afirmação observável está provada (B5 morre), mas o parêntese do check não está. Isso não quebra nada; só deixa redundante o atributo ou a isenção.
2. Cosmético: o tracejado da amostra neutra quase não aparece. A borda é `oklch(0.89)` sobre o fundo `oklch(0.945)` no claro e `oklch(0.27)` sobre `oklch(0.22)` no escuro, num quadrado de 20 px. Ela se lê como um quadrado cinza claro ou escuro, e não como "vazio". Isso ainda atende o "neutra em vez de preto". Se o dono quiser que se leia como vazio, basta uma borda mais forte, por exemplo `border-muted-foreground`.
3. Referência desatualizada no Intent: a MDN de hoje diz que `<input type="color">` aceita qualquer cor CSS, não só `#rrggbb`. A queda para `#000000` com valor ausente ou inválido continua valendo, então a decisão da amostra neutra não muda.
4. Limites das provas: C5 prova que clicar no texto foca o input, mas não que a janela nativa abre (o Playwright não alcança essa janela). O toque também não é emulado: a 360 px a altura de 44 px vem de `max-md:h-11`, e o ramo `pointer-coarse:h-11` (tablet ou celular deitado com mais de 768 px) não é exercitado.
5. Sobra estrutural, sem efeito visível: `CoresProntas.tsx:30` mantém um `div flex` que agora envolve só o `radiogroup`.

## Gate

- Vitest, cwd `C:\`: `npm --prefix D:\esclari\web test -- src/administracao/aparencia/aparenciaAbas.test.tsx src/administracao/aparencia/guidelinesCores.test.tsx -t "seletor de cor ao lado do campo Cor principal|amostra do seletor mostra a cor ou fica neutra|cor sem contraste bloqueia o Salvar|C25 Tab atravessa o grupo numa só parada" --reporter=verbose`: 4 passed, 21 skipped, exit 0
- E2E contra :8080: `npm --prefix D:\esclari\web run e2e -- e2e/seletor-de-cor.spec.ts e2e/ux.spec.ts -g "seletor ao lado do campo|cor de ação só em botão principal, item ativo, link e foco"`: 3 passed, exit 0
- Vitest com cobertura: 115 arquivos, 1077 passed, exit 0
- build web, `crapper`, `test_C21_dryer`: exit 0
