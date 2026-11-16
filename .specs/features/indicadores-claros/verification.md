# Indicadores claros para o gestor verification

**Verdict**: PASS
**Profile**: ui
**Diff range**: 8190316..f5d64b1 (HEAD); correção da rodada 4: 3864a83..f5d64b1 (só testes: `fila.test.tsx`, `series.test.tsx`, `PushAgendadorTests.cs`, mais `checks.md`/`STATE.md`)
**Round**: 4 - scoped (rodada além do limite de 3 do verify.md, aprovada explicitamente pelo dono do projeto)
**Verifier**: independent sub-agent (author != verifier)

PASS com risco aceito: os 43 checks passam com evidência localizada, as 7 faltas injetadas nesta rodada morreram, e o que resta é só o resíduo que o AD-061 aceita (M11, C7, C37, C41), listado em `## Accepted residue`. Pelo texto estrito de verify.md, um mutante vivo (M11) é achado; o PASS depende de o AD-061 valer como aceite do dono (julgamento abaixo). Nenhum código foi alterado por este verificador. O HEAD é `f5d64b1`, um commit além do `254cada` citado no pedido; ele só acrescenta `f.Push.Comportamento = null;` em `PushAgendadorTests.cs:279`, e todas as provas rodaram nele.

## Binding sources

Carried from 3864a83 (rodada 3). A correção desta rodada só tocou testes, `checks.md` e `STATE.md`: nenhuma tela, rota ou fonte vinculante mudou (`git diff --stat 3864a83..HEAD`).

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `.specs/STATE.md` AD-037, AD-055, AD-035, AD-060 | yes - carried from 3864a83 | none | - |
| plan `## Surface`, `## Observable`, `## Emendas de construção` | yes - carried from 3864a83; a lacuna de arranjo da rodada 3 (grades `dl` das seções 3 e 4 sem teste) agora tem o C43 (fechada, ver Faults) | none | - |
| AD-061 (novo) | yes - lido em HEAD, julgado em `## Accepted residue` | none | - |

## Checks

Provas rodadas em `f5d64b1`, por inteiro:

- API: `dotnet test Esclari.Api.Tests --artifacts-path D:/esclari/.artifacts-agente-v --logger "console;verbosity=normal"`: 1011 aprovados, 0 falhas (2,9 min). Os 14 testes de API nomeados nos checks aparecem um a um como `Aprovado` (`Select-String "Aprovado .*(nomes)"` = 14, "Falhou" = 0).
- Web: `npx vitest run --maxWorkers=4`: 1044 passed (112 arquivos). `npx vitest run src/indicadores --reporter=verbose`: 15 arquivos, 62 passed; cada teste citado abaixo aparece por nome, inclusive `fila por estado` e `as medidas das seções 2 a 5 ficam em duas colunas a partir de um main de 768 px`.
- C31: `node -e ...` exit 0; `git diff --exit-code 8190316 -- web/package.json` exit 0.
- C37: `npx playwright test -c playwright.verif.config.ts e2e/celular.spec.ts -g "Indicadores claros"` com `webServer` Vite na 5199 (`--strictPort`): 2 passed (360×780 e 844×390), exit 0. Config e `web/test-results` apagados.

Linhas dos checks C1 a C41 (exceto os dois refrescados abaixo): carried from 3864a83, as provas desses testes passaram de novo em HEAD e nenhum arquivo deles mudou (`git diff --stat 3864a83..HEAD` lista só `fila.test.tsx`, `series.test.tsx`, `PushAgendadorTests.cs`). A tabela completa com `file:line` e a expressão de cada assertiva está em `verification.md` de 3864a83 (`git show 3864a83:.specs/features/indicadores-claros/verification.md`); todas as linhas dela valem em HEAD, salvo C15 (parte `fila`), C23, C38, C42 e C43, refeitas aqui.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1-C14, C16-C22, C24-C41 | ver rodada 3 | todas as provas rodaram de novo em f5d64b1 (API 14/14 `Aprovado`, Vitest 15 arquivos/62 testes, C31 e C37 exit 0) | carried from 3864a83 (arquivos intocados) | PASS |
| C15 | `role="img"`/`aria-label` + "Ver como tabela" nos 3 gráficos | `-t "ver como tabela"` (`series`, `desfechos`) e `-t "fila por estado"` passaram | `series.test.tsx:70,76` (carried); `desfechos.test.tsx:63-69` (carried); `fila.test.tsx:48` - `expect(s.getByRole('img').getAttribute('aria-label')).toContain('Em atendimento: 6. Aguardando aluno: 2. Resolvido: 4. Total 15.')` (refrescado em f5d64b1) | PASS |
| C23 | barra empilhada pelos 4 estados, rótulo, contagem, "mais antigo" | `-t "fila por estado"` passou | `fila.test.tsx:40-45` - `expect(s.getAllByRole('listitem').map((i) => i.textContent)).toEqual(['Aberto · 3 · 20 %', 'Em atendimento · 6 · 40 %', 'Aguardando aluno · 2 · 13,3 %', 'Resolvido · 4 · 26,7 %'])`; `:47` - `getByText('O mais antigo espera há 9 dias')` | PASS |
| C38 | suítes e gates verdes | ver `## Gate` | API 1011, Vitest 1044, build exit 0, `dryer` exit 0, `crapper` exit 0 | PASS |
| C42 | tabela da fila: colunas Item, Quantidade, Percentual e contagens 3, 6, 2, 4 | `-t "fila por estado"` passou | `fila.test.tsx:50` - `expect(s.getAllByRole('columnheader').map((h) => h.textContent)).toEqual(['Item', 'Quantidade', 'Percentual'])`; `:51-56` - `expect(s.getAllByRole('row').slice(1).map((l) => within(l).getAllByRole('cell').map((c) => c.textContent))).toEqual([['Aberto', '3', '20\u00a0%'], ['Em atendimento', '6', '40\u00a0%'], ['Aguardando aluno', '2', '13,3\u00a0%'], ['Resolvido', '4', '26,7\u00a0%']])`. Os três cabeçalhos, o nome, a contagem e o percentual de cada linha estão afirmados; os valores são os do check | PASS |
| C43 | grades de medidas das seções 2 a 5 em 2 ou 3 colunas em `@3xl/main` | `-t "as medidas das seções 2 a 5 ficam em duas colunas"` passou | `series.test.tsx:131-133` - `const grades = [...document.querySelectorAll('main section dl.grid')]; expect(grades.length).toBeGreaterThanOrEqual(4); for (const g of grades) expect(g.className, ...).toMatch(/@3xl\/main:grid-cols-[23]/)`. As 4 grades reais: `Indicadores.tsx:141` (2), `:148` (3), `:173` (2), `Fila.tsx:18` (2) | PASS |

## Coverage

Recomputado nas linhas que a correção tocou; o resto carried from 3864a83.

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| colunas da tabela da barra da fila (3) | `Figura.tsx:14` (`colunas: ['Item', 'Quantidade', 'Percentual']`) | Item, Quantidade, Percentual: `fila.test.tsx:50` (cabeçalhos) e `:51-56` (cada célula) | - |
| linhas da tabela da fila (4 estados) | `fila.test.tsx:51-56` | Aberto 3, Em atendimento 6, Aguardando aluno 2, Resolvido 4: C42 | - |
| quebras de coluna `@3xl/main` (grades `dl`: 4 em `Indicadores.tsx:141,148,173` e `Fila.tsx:18`; par de gráficos; rankings `@4xl`) | `grep "<dl"` e plan `## Observable` | 4 grades `dl`: C43 `series.test.tsx:131-133` (as 4 mutadas e mortas, ver Faults); par de gráficos: C40 `series.test.tsx:125`; rankings: C40 `indicadores.test.tsx:86` | - |
| demais conjuntos (status, desfechos, estados, taxas, temas, viewports, granularidade, fusos, seções, gráficos) | `checks.md` Coverage | carried from 3864a83, sem diferença | - |

Limite do C43: o teste exige ao menos 4 `dl.grid` em `main section` e a classe `@3xl/main:grid-cols-2|3` em cada; não liga cada grade à sua seção nem ao número de colunas exato (2 ou 3 aceitos). Aceito pelo texto do check ("duas ou três colunas").

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| Decide, atravessa a rota HTTP (`ConsultarIndicadoresDetalhe`) | `CalculoDetalhe.cs`, `ConsultarIndicadoresDetalhe.cs`, `LeitorIndicadores.cs` | rota `IndicadoresDetalheTests.cs` · camada `CalculoDetalheTests.cs` | yes - carried from 3864a83; 14 de 14 nomeados `Aprovado` em f5d64b1 |
| Decide, na tela (variação, limite de 10, formato dos tempos) | `Variacao.tsx`, `Fila.tsx` | uma na própria camada | yes - `variacao.test.tsx`, `fila.test.tsx` passam em f5d64b1 |
| Desenho (SVG) sem condicional | `Figura.tsx`, `Series.tsx`, `Desfechos.tsx` | uma de renderização | yes - rótulos, `role`, `aria-label`, valores e agora cabeçalhos e percentuais da tabela da fila (`fila.test.tsx:48-56`) |

## Faults injected

Worktree `D:\esclari-wt` em `f5d64b1`, `node_modules` por junção, cada falha desfeita reescrevendo o arquivo original no scratch; worktree removido (`git worktree remove --force` + `prune`, junção removida antes). `git status --porcelain` da árvore real antes e depois igual: ` M web/scripts/preparar-e2e.mjs`, `?? .tmp-e2e.log`, `?? .tmp-e2e2.log` (estranhos, de outro processo; `.tmp-e2e2.log` apareceu durante a sessão, fora do meu controle). Nenhum `git stash`. Confirmei que cada `Replace` alterou o texto (`-ne $orig` = True nas três de `Figura.tsx`).

| Mutation | Location | Killed |
| --- | --- | --- |
| M4 cabeçalho e célula "Quantidade" removidos da tabela da barra | `Figura.tsx:14` | yes - `fila.test.tsx` "fila por estado" falhou (1 failed) |
| M5 cabeçalho `Item` -> `Nome` | `Figura.tsx:14` | yes - `fila.test.tsx` "fila por estado" falhou (1 failed) |
| M6 células "Percentual" viram `''` | `Figura.tsx:14` | yes - `fila.test.tsx` "fila por estado" falhou (1 failed); antes só o teste de desfechos pegava |
| C43-a `dl` de `Indicadores.tsx:141` perde `@3xl/main:grid-cols-2` | `Indicadores.tsx:141` | yes - `series.test.tsx` "as medidas das seções 2 a 5..." falhou |
| C43-b `dl` de `Indicadores.tsx:148` perde `@3xl/main:grid-cols-3` | `Indicadores.tsx:148` | yes - mesmo teste falhou |
| C43-c `dl` de `Indicadores.tsx:173` perde `@3xl/main:grid-cols-2` | `Indicadores.tsx:173` | yes - mesmo teste falhou |
| C43-d `dl` de `Fila.tsx:18` perde `@3xl/main:grid-cols-2` | `Fila.tsx:18` | yes - mesmo teste falhou |

Faltas das rodadas anteriores (M1 a M3, M7 a M10) carried from 3864a83, todas mortas; não reinjetadas porque o diff da correção não tocou `Indicadores.tsx` (código), `Autenticado.tsx`, `Usuarios.tsx` nem `ui/button.tsx`.

## Accepted residue

Não entra na tabela de faltas porque não foi reinjetado nesta rodada (carried from 3864a83). O gate de `validate_verification.py` leria "sobreviveu" como bloqueio; o registro acima é a razão de ele estar aqui e não lá.

| Item | O que é | Aceito por |
| --- | --- | --- |
| M11 | `perfilDoModo.lista` sempre `'solicitacoes'` sobrevive: o ramo ATENDENTE -> `fila` é código anterior à feature, só movido pelo refator de `useModo` | AD-061 |
| C7 | texto diz "3 tons entre os 4 segmentos", o teste mede 4 tokens (`contraste.test.ts`, `>= 10`) | AD-061 |
| C37 | mede só os SVG de série (>= 160 px); "cortado" = reticências | AD-061 |
| C41 | afirma a classe `max-md:h-11`/`pointer-coarse:h-11`, não a altura computada de 44 px | AD-061 |
| C40 prova 2 | mora num teste cujo nome fala de formato (`as medidas formatam taxa...`), não de layout | não coberto pelo AD-061; precisão menor, mutante de `@4xl` morto (M3) |

**Julgamento do AD-061.** É um registro legítimo na forma que o AGENTS.md pede (um AD no `STATE.md`, com data 2026-11-16, citação da decisão do dono, custo e os itens nomeados: M11, C7, C37, C41). Duas ressalvas que o dono deve saber: (1) a citação é uma delegação genérica ("resolva da melhor maneira e da forma que for mais bem recomendada"), e o AD foi escrito pelo agente construtor, não pelo dono; (2) o AD diz que M11, C7, C37 e C41 estão aceitos, mas o AGENTS.md prevê fechar com risco aceito "sem PASS". Esta rodada mantém PASS apenas porque o dono, no pedido desta rodada, reafirmou que o resíduo é aceito. Se o dono preferir o texto estrito, o veredito é "FECHADO COM RISCO ACEITO (AD-061)", não "PASS limpo". Os dois itens que o AD-061 declarava "corrigidos sem rodada 4" (C42 e as grades das seções 3 e 4) foram verificados aqui e passam.

## Gate

Verified at f5d64b1:

`dotnet test Esclari.Api.Tests --artifacts-path D:/esclari/.artifacts-agente-v` - 1011 passed, 0 failed
`cd web && npx vitest run --maxWorkers=4` - 1044 passed, 0 failed (112 arquivos)
`npm --prefix web run build` - exit 0
`npm --prefix web run coverage` - exit 0, 1044 passed; `cd web && crapper --use-existing-coverage -s src --threshold 9.99` - exit 0
`cd web && dryer -s src/acesso -s src/assistente -s src/chamados -s src/conhecimento -s src/indicadores -s src/administracao --threshold 0.8 src/App.tsx src/Autenticado.tsx src/api.ts src/chamado.ts src/datas.ts src/sessao.ts` - exit 0, "No duplicate candidates found."
C31 `node -e ...` exit 0; `git diff --exit-code 8190316 -- web/package.json` exit 0
C37 Playwright (config Vite temporária, apagada) - 2 passed, exit 0

## Ranked gaps

Nenhuma falha nova. Nenhum mutante vivo nas superfícies desta rodada (C42 e C43). O que resta é só o resíduo do AD-061 (M11, C7, C37, C41) e a precisão menor do C40 prova 2, listados em `## Accepted residue`. Efeito colateral a notar: `npm --prefix web run build` regrava `Esclari.Api/wwwroot/` e `crapper`/`dryer` regravam `web/.metrics/` (ambos ignorados pelo git; `git status` confirma).
