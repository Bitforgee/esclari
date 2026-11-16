# Indicadores claros para o gestor verification

**Verdict**: FAIL
**Profile**: ui
**Diff range**: 8190316..a3eb11f (HEAD); correção da rodada 1: 1bf47dd..a3eb11f
**Round**: 2 - scoped
**Verifier**: independent sub-agent (author != verifier)

Rodada 2, escopo: o diff da correção (`a3eb11f`) e todo veredito que não foi PASS na rodada 1. Todas as provas rodaram de novo por inteiro em `a3eb11f`. Os 38 checks passam, os 3 mutantes que sobreviveram foram mortos e a superfície nova (tabela das barras empilhadas) recebeu 2 falhas, ambas mortas. Nenhum defeito de código resta. O veredito continua FAIL só por lacunas de cobertura que dependem do dono do projeto (ver `## Ranked gaps`): o arranjo que o plano decide e nenhum check cobre, e a leitura literal de C38 (`crapper`). Nenhum código foi alterado por este verificador.

## Binding sources

Carried from 1bf47dd, exceto a linha de arranjo (reavaliada porque a correção tocou `Indicadores.tsx`).

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `.specs/STATE.md` AD-037 (rankings em HTML, `recharts` sai) | yes - carried from 1bf47dd; reconfirmado: C31 exit 0 e `git diff --exit-code 8190316 -- web/package.json` exit 0 em a3eb11f | none | - |
| `.specs/STATE.md` AD-055 (saída do MCP `ver_indicadores`) | yes - carried from 1bf47dd; a3eb11f não toca `Esclari.Api/Mcp` (diff `1bf47dd..HEAD` só tem `CalculoDetalheTests.cs` na API) | none | - |
| `.specs/STATE.md` AD-035 (contraste 3:1) | yes - carried from 1bf47dd; C36 passa em a3eb11f | none | - |
| plan `## Surface` (ordem das 6 seções) e `## Observable` (arranjo, alvo de toque) | yes - `plan.md` relido em a3eb11f, inclusive `## Emendas de construção` | none | ordem das seções (teste `indicadores.test.tsx:95` existe, nenhum check o cita); par de gráficos em 2 colunas a partir de `@3xl/main` (`Indicadores.tsx:155`): nenhum teste o afirma (o único teste de grade, `indicadores.test.tsx:86`, mede os rankings em `@4xl/main`); alvo de 44 px de "Ver como tabela" sem teste; "seções 3 e 4 em duas colunas" a 844 px sem check |
| AD-034 e as 3 referências externas do plan `## Sources` | no - carried from 1bf47dd; só justificam, não fixam rota, rótulo nem status | not assessed | - |

Elementos novos da correção (botão "Ver como tabela" e total no `aria-label` das duas barras empilhadas): o plano os decide agora em `## Emendas de construção` (AC 14); cobertos por `desfechos.test.tsx:57` e `fila.test.tsx:37`.

## Checks

Provas rodadas em a3eb11f: API `dotnet test Esclari.Api.Tests --artifacts-path D:/esclari/.artifacts-agente-v` inteira (1011 passed, 0 failed) e, em seguida, `--no-build --filter "FullyQualifiedName~CalculoDetalheTests|FullyQualifiedName~IndicadoresDetalheTests"` com saída por teste (15 de 15 listados, incluindo `P90_usa_o_posto_mais_proximo_com_dez_valores`); Web `npx vitest run` inteiro (112 arquivos, 1041 passed) e `npx vitest run src/indicadores --reporter=verbose` (15 arquivos, 59 passed, cada nome de check listado individualmente); C31 `node -e ...` exit 0 e `git diff --exit-code 8190316 -- web/package.json` exit 0; C37 com config temporária contra Vite na 5199 (2 de 2 passed; config e `web/test-results` apagados).

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | 5 chaves de `desfechos`, soma = conversas | `dotnet test --filter ...Desfechos_somam_o_total_de_conversas` Aprovado (carried assertion from 1bf47dd, prova re-rodada em a3eb11f) | `Esclari.Api.Tests/IndicadoresDetalheTests.cs:76` - `Assert.Equal([2, 1, 1, 1, 1], chaves.Select(k => j.GetProperty(k).GetInt32()))` | PASS |
| C2 | com `setorId`, `desfechos` e `conversas` nulos | `...Com_setor_desfechos_e_conversas_sao_nulos` Aprovado | `IndicadoresDetalheTests.cs:88` - `Assert.Equal(JsonValueKind.Null, j.GetProperty("desfechos").ValueKind)` | PASS |
| C3 | 4 segmentos "nome · contagem · percentual", 69,6 % | `-t "barra com rotulo direto"` passou | `web/src/indicadores/desfechos.test.tsx:25` - `expect(itens).toEqual(['Resolvida pelo assistente · 80 · 69,6 %', ... 'Encerrada pelo aluno · 0 · 0 %'])` | PASS |
| C4 | "de 115 conversas com desfecho", "5 ainda sem desfecho", fora da barra | `-t "denominador"` passou | `desfechos.test.tsx` - `findByText('de 115 conversas com desfecho · 5 ainda sem desfecho')` e soma das larguras `toBeCloseTo(100, 5)` (carried from 1bf47dd; linhas deslocaram na correção, ver `grep` no arquivo) | PASS |
| C5 | 9 mostra aviso, 10 não | `-t "amostra pequena"` passou | `desfechos.test.tsx` - aviso com 9; `queryByText('Amostra pequena: menos de 10 conversas')).not.toBeInTheDocument()` com 10 | PASS |
| C6 | `null` sem barra e com a frase | `-t "sem filtro de setor"` passou | `desfechos.test.tsx` - `getByText('Desfechos do assistente aparecem na visão geral, sem filtro de setor.')` e `queryByRole('img')` ausente | PASS |
| C7 | tons distintos por luminosidade (>= 10 pontos) e separador de 2 px | `-t "tons de cinza distintos"` e `-t "separador de 2 px"` passaram | `contraste.test.ts` - `expect(luz[i] - luz[i - 1], ...).toBeGreaterThanOrEqual(10)`; `desfechos.test.tsx` - `toHaveClass('gap-0.5', 'bg-background')`. Precision gap mantido (rodada 1, item 6): texto diz "3 tons entre os 4 segmentos", o teste mede 4 tokens; preto não medido | PASS |
| C8 | 3 dias dão `dia`, 3 pontos, dia sem atividade em 0 | `...Ate_62_dias_um_ponto_por_dia` Aprovado | `Esclari.Api.Tests/CalculoDetalheTests.cs:33-35` (carried from 1bf47dd; arquivo só ganhou o teste do p90 depois) - `Assert.Equal("dia", d.Granularidade)` e `Assert.Equal((0, 0), (p.ChamadosAbertos, p.ChamadosResolvidos))` | PASS |
| C9 | 62 dias dá `dia` (62 pontos), 63 dá `semana` | `...Limite_de_62_dias` Aprovado | `CalculoDetalheTests.cs:45-46` - `Assert.Equal(("dia", 62), (com62.Granularidade, com62.Pontos.Count))` e `Assert.Equal("semana", com63.Granularidade)` | PASS |
| C10 | `inicio` é segunda no fuso, semana tocada entra inteira | `...Semana_comeca_na_segunda_no_fuso` Aprovado | `CalculoDetalheTests.cs:58-61` - `DayOfWeek.Monday`, primeiro ponto `2026-08-31`, 11 pontos, `Assert.Equal(1, d.Pontos[0].ChamadosAbertos)` | PASS |
| C11 | 23:30 local de 10/11 cai em 10/11 | `...Ponto_usa_o_fuso_da_instituicao` Aprovado | `CalculoDetalheTests.cs:71` - `Assert.Equal([1, 0], d.Pontos.Select(p => p.ChamadosAbertos))` | PASS |
| C12 | cada série conta pela sua data, soma de resolvidos = `/api/indicadores` | `...Serie_conta_cada_coisa_pela_sua_data` Aprovado | `IndicadoresDetalheTests.cs:107-111` - `[3, 1]`, `[2, 0]`, `Assert.Equal(1, resolvidos)` igual a `chamadosResolvidos` | PASS |
| C13 | colunas com eixo em 0, máximo, pico rotulados | `-t "colunas de conversas"` passou | `web/src/indicadores/series.test.tsx:29-34` (carried from 1bf47dd) - `toHaveLength(3)`, `'Maior volume: 12 em 11/11'`, `expect(alturas).toEqual([(2/12)*100, 100, (5/12)*100])` | PASS |
| C14 | 2 linhas, uma tracejada, rótulo no fim, "por semana" | `-t "duas linhas"` passou | `series.test.tsx:49-52, 58` - `toBe('6 4')`, `'Abertos · 2'`, `'Resolvidos · 2'`, `'Chamados abertos e resolvidos por semana'` | PASS |
| C15 | todo gráfico de série e de barras tem `role="img"` + `aria-label` (tipo, período, total, pico) e "Ver como tabela" | `-t "ver como tabela"` em `series.test.tsx` passou; os testes novos das barras (`desfechos.test.tsx` "ver como tabela", `fila.test.tsx` "fila por estado") também passaram na rodada `src/indicadores` | `series.test.tsx:76` `toContain('Total 6 abertos e 7 resolvidos')` (linhas); `desfechos.test.tsx:63-69` - `expect(linhas).toEqual([['Resolvida pelo assistente', '80', '69,6\u00a0%'], ...])` e `getAttribute('aria-label')).toContain('Total 115.')`; `fila.test.tsx:48-50` - `toContain('Em atendimento: 6. Aguardando aluno: 2. Resolvido: 4. Total 15.')` e células `['3', '6', '2', '4']`. Rankings são tabelas (plan `## Emendas`). Precision gap: a prova escrita em `checks.md` só roda `series.test.tsx`; os testes das barras e das colunas não entram nela. A tabela das colunas segue sem teste (ver Coverage) | PASS |
| C16 | foco do teclado e toque mostram "dia, valor" em texto >= 12 px | `-t "valor do ponto"` passou; `series.test.tsx` "toque no gráfico escolhe o ponto pela posição" passou | `series.test.tsx:99-102` - `fireEvent.pointerDown(grupo, { clientX: 130 })` -> `findByText('10/11: 2 conversas')`; `clientX: 390` -> `'12/11: 5 conversas'`; `:91` (carried) `toHaveClass('text-sm')`. Precision gap: o nome do teste novo não casa com `-t "valor do ponto"`, a prova escrita no check não o seleciona. Mutante do toque morto (ver Faults) | PASS |
| C17 | mediana 2,5 e p90 10 para 1, 2, 3, 10 (índice `ceil(0,9·n)−1`) | `...Mediana_e_p90_da_primeira_resposta` e `...P90_usa_o_posto_mais_proximo_com_dez_valores` Aprovado | `CalculoDetalheTests.cs:91-92` (carried) `Assert.Equal(10, ...P90Horas)`; novo `:103-104` - `Assert.Equal(5.5, d.Tempos.PrimeiraRespostaMedianaHoras)` e `Assert.Equal(9, d.Tempos.PrimeiraRespostaP90Horas)`. Precision gap: a prova escrita no check só nomeia o primeiro teste. Mutante 0,8 morto (ver Faults) | PASS |
| C18 | resolução desconta "aguardando aluno": 10 h com 4 h dá 6 h | `...Resolucao_desconta_aguardando_aluno` Aprovado | `CalculoDetalheTests.cs` - `Assert.Equal(6, d.Tempos.ResolucaoMedianaHoras)` (carried from 1bf47dd) | PASS |
| C19 | sem chamados, 4 campos nulos | `...Sem_chamados_tempos_nulos` Aprovado | `CalculoDetalheTests.cs` - `Assert.Equal(new Tempos(null, null, null, null), d.Tempos)` | PASS |
| C20 | 4 chaves, soma = `chamadosAbertos`, `FECHADO` fora | `...Estados_somam_os_chamados_abertos` Aprovado | `IndicadoresDetalheTests.cs:129-131` - `[1, 2, 1, 1]`, `Assert.Equal(5, ...)`, `Assert.False(estados.TryGetProperty(Fechado, out _))` | PASS |
| C21 | `maisAntigoDias` 9, ignora aguardando e resolvido, nulo sem nenhum | `...Mais_antigo_conta_so_o_que_espera_a_equipe` Aprovado | `CalculoDetalheTests.cs` - `Assert.Equal(9, d.MaisAntigoDias)` e `Assert.Null(nenhum.MaisAntigoDias)` | PASS |
| C22 | "mediana 2,5 h" e "9 em 10 ... até 10 h", sem média | `-t "mediana e p90"` passou | `web/src/indicadores/fila.test.tsx` - `toEqual(['mediana 2,5 h', '9 em 10 foram atendidos em até 10 h'])` e `queryByText(/média/i)).not.toBeInTheDocument()` | PASS |
| C23 | barra empilhada pelos 4 estados com rótulo e contagem, "mais antigo" | `-t "fila por estado"` passou | `fila.test.tsx` - `toEqual(['Aberto · 3 · 20 %', 'Em atendimento · 6 · 40 %', 'Aguardando aluno · 2 · 13,3 %', 'Resolvido · 4 · 26,7 %'])` e `'O mais antigo espera há 9 dias'` | PASS |
| C24 | com 0, "Nenhum chamado aberto agora." e nenhuma barra | `-t "nenhum chamado aberto"` passou | `fila.test.tsx` - `getByText('Nenhum chamado aberto agora.')` e `queryByRole('img')).not.toBeInTheDocument()` | PASS |
| C25 | 7 taxas com seta, pontos e melhor/pior, "sem variação" em 0 | `-t "variacao nas sete taxas"` passou | `web/src/indicadores/variacao.test.tsx:79-89` - `toEqual([['Indicador principal','▲ 10,0 pts · melhor'], ... ['Abandono','sem variação'], ...])` | PASS |
| C26 | direção por taxa | `-t "direcao de cada taxa"` passou | `variacao.test.tsx:20-29` - `expect(sobe).toEqual([... ['Escalonamento','▲ 10,0 pts · pior'] ...])` e o espelho | PASS |
| C27 | omite com anterior sem dados, taxa nula, 9 casos; mostra com 10 | `-t "omite a variacao"` passou | `variacao.test.tsx:41, 44-45` - `expect(variacaoDaTaxa(...) !== undefined, nome).toBe(mostra)` | PASS |
| C28 | Resumo com principal, variação, denominador e definição | `-t "resumo com denominador"` passou | `web/src/indicadores/resumo.test.tsx:30-33` - `'de 115 conversas com desfecho'`, `'71,2 %'`, `'▲ 4,2 pts · melhor'`, `'Resolvidas sem reabertura, sobre as conversas com desfecho.'` (texto emendado, agora registrado em `plan.md ## Emendas de construção`; `checks.md` C28 ainda diz "conversas e chamados") | PASS |
| C29 | variação nunca só por cor | `-t "nunca so por cor"` passou | `variacao.test.tsx:96-97` - `toHaveLength(7)` e `expect(t).toMatch(/^(▲|▼) [\d,]+ pts? · (melhor|pior)$|^sem variação$/)` | PASS |
| C30 | rankings em barras com valor ao fim, maior em 100 %, "—" se vazio | `-t "barras com valor escrito"` passou | `web/src/indicadores/ranking.test.tsx:31-32, 40-41` - células `[['Boleto','8'],['Matrícula','2']]`, larguras `['100%','25%']`, `getByText('—')` | PASS |
| C31 | sem pizza/rosca/medidor/3D e sem dependência nova | `node -e ...` exit 0; `git diff --exit-code 8190316 -- web/package.json` exit 0 | comando do próprio check, 2 provas por exit code, rodadas em a3eb11f | PASS |
| C32 | período vazio mantém a frase, sem seção nova | `-t "sem atendimentos"` passou | `web/src/indicadores/estados.test.tsx:30, 32` - `findByText('Sem atendimentos neste período.')` e `queryByRole('region', {name: titulo})).not.toBeInTheDocument()` | PASS |
| C33 | `detalhe` pendente: `min-h` e "Carregando…" | `-t "carregando"` passou | `estados.test.tsx:40-41` - `getByText('Carregando…')` com `toHaveClass('min-h-40')` | PASS |
| C34 | `detalhe` em 500: erro com "Tentar de novo" só nas seções novas | `-t "erro so no detalhe"` passou | `estados.test.tsx:48-55` - `toHaveLength(3)` alertas, botão `'Tentar de novo'`, Resumo `'71,2 %'` | PASS |
| C35 | 403/400/401 e formato literal | `...Quem_pode_e_o_formato_da_resposta` Aprovado | `IndicadoresDetalheTests.cs:143-154` - `HttpStatusCode.Forbidden` x3, `BadRequest` x2, `Unauthorized`, chaves literais da porta 1 | PASS |
| C36 | 3:1 nas marcas e 4,5:1 no texto, 4 temas | `-t "contraste de graficos"` passou | `contraste.test.ts:76, 80, 82` - `toHaveLength(4)`, `toBeGreaterThanOrEqual(3)`, `toBeGreaterThanOrEqual(4.5)` | PASS |
| C37 | 360×780 e 844×390: sem rolagem horizontal, sem texto cortado, gráficos >= 160 px | `npx playwright test -c playwright.verif.config.ts e2e/celular.spec.ts -g "Indicadores claros"` (Vite na 5199, API simulada) 2 passed, exit 0 | `web/e2e/celular.spec.ts:100-104` (carried from 1bf47dd) - `expect(medidas.rolagem).toBeLessThanOrEqual(0)`, `expect(h).toBeGreaterThanOrEqual(160)`, `expect(medidas.cortados).toEqual([])`. Precision gap mantido: só SVG de série entra nos 160 px; barras empilhadas medem 24 px; "cortado" = `text-overflow: ellipsis` | PASS |
| C38 | suítes e gates verdes | ver `## Gate` | 1011 API, 1041 Vitest, build exit 0, `dryer` exit 0; `crapper` exit 2 por `useModo` 11,0 e `Usuarios` 10,0 (fora do diff, resíduo anterior; registrado no `checks.md` Handoff 4 e em `plan.md ## Emendas`). Como escrito ("abaixo de 10"), `crapper` não passa; conto PASS só pela exceção registrada, decisão do dono (ver Ranked gaps 2) | PASS |

## Coverage

Recomputado para as linhas que a correção tocou; as demais carried from 1bf47dd sem diferença.

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| campos de `tempos` / índice do p90 | `CalculoDetalhe.cs:90` (`Math.Ceiling(0.9 * valores.Count)`) | n = 4 `CalculoDetalheTests.cs:92`; n = 10 `:104`; resolução C18 | - |
| gráficos (5) e o que cada um deve ter (role, rótulo, tabela) | plan AC 14 + `## Emendas` | barra de desfechos: role/total/tabela `desfechos.test.tsx:63-69`; barra da fila: total/tabela `fila.test.tsx:48-50`; linhas: tabela `series.test.tsx:70-76`; rankings: são tabelas (`ranking.test.tsx:31`) | tabela das colunas (`Series.tsx`, "Conversas por dia") sem teste; coluna "Percentual" da tabela da fila não é afirmada (`fila.test.tsx:50` lê só a coluna 1; a mesma linha de código é afirmada em `desfechos.test.tsx:64`) |
| interação do ponto (2 entradas) | plan AC 15 | teclado `series.test.tsx:85-91`; toque `:99-102` | - |
| barras empilhadas (2) | `Desfechos.tsx`, `Fila.tsx` | `BarraEmpilhada` com `VerTabela`: ambas provadas | - |
| seções da tela na ordem (6 + barra do período) | plan `## Surface` | `indicadores.test.tsx:95-113` (não ligado a check) | sem check |
| arranjo (par de gráficos em 2 colunas, alvo de 44 px, "seções 3 e 4 em duas colunas") | plan `## Observable` + `## Emendas` | `Indicadores.tsx:155` (`@3xl/main:grid-cols-2`): nenhum teste; `indicadores.test.tsx:86` afirma só os rankings (`@4xl/main`); alvo de 44 px `ui/button.tsx:24` sem teste | sem check, e a mudança `@4xl` -> `@3xl` da correção não tem teste que a faça falhar |

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| Decide, atravessa a rota HTTP (`ConsultarIndicadoresDetalhe`) | `CalculoDetalhe.cs`, `ConsultarIndicadoresDetalhe.cs`, `LeitorIndicadores.cs` | rota: `IndicadoresDetalheTests.cs` (5 testes) · camada: `CalculoDetalheTests.cs` (10 testes) | yes - o p90 agora tem 2 casos (n = 4 e n = 10); mutante 0,8 morto |
| Decide, na tela (variação, limite de 10, formato dos tempos) | `Variacao.tsx`, `Fila.tsx` | uma na própria camada | yes - `variacao.test.tsx:20-46`, `fila.test.tsx:23-24` |
| Desenho (SVG) sem condicional | `Figura.tsx`, `Series.tsx`, `Desfechos.tsx` | uma de renderização | yes - rótulos, `role`, `aria-label` e valores em `series.test.tsx`, `desfechos.test.tsx:25, 63-69`, `fila.test.tsx:48-50`; toque `series.test.tsx:99-102`; falta só a tabela das colunas (Coverage) |

## Faults injected

Worktree `D:\esclari-verif-wt` em a3eb11f, `node_modules` por junção, removido ao fim (`git worktree remove`, `prune`). `git status --porcelain` da árvore real antes e depois: só `?? .artifacts-agente/`, igual ao baseline (apaguei o log `.artifacts-agente-v-full.log` que criei). `web/playwright.verif.config.ts` e `web/test-results` apagados. (Os worktrees `Temp/v2-wt-api`, `v2-wt-web` e `verif-m1` já existiam e não são deste verificador.)

| Mutation | Location | Killed |
| --- | --- | --- |
| 90º percentil: `Math.Ceiling(0.9 * valores.Count)` -> `0.8` (sobrevivente da rodada 1) | `Esclari.Api/Indicadores/CalculoDetalhe.cs:90`; `dotnet test --filter CalculoDetalheTests`: 1 falhou, 9 passaram | yes - falhou `P90_usa_o_posto_mais_proximo_com_dez_valores` (o `Mediana_e_p90_...` antigo segue passando) |
| toque no gráfico desligado: `if (caixa.width > 0) ir(` -> `if (false) ir(` (sobrevivente da rodada 1) | `web/src/indicadores/Figura.tsx:51`; `vitest series.test.tsx`: 1 falhou, 7 passaram | yes - falhou "toque no gráfico escolhe o ponto pela posição" (`Unable to find an element with the text: 10/11: 2 conversas`) |
| tabela das barras sem a coluna "Percentual" (superfície nova) | `Figura.tsx:14` (`BarraEmpilhada`); `vitest desfechos.test.tsx fila.test.tsx`: 1 falhou, 10 passaram | yes - falhou `desfechos.test.tsx` "ver como tabela" |
| "Total" fora do `aria-label` da barra da fila (superfície nova) | `web/src/indicadores/Fila.tsx:31`; `vitest fila.test.tsx`: 1 falhou | yes - falhou `fila.test.tsx` "fila por estado" |

Não injetei falha em `Indicadores.tsx:155` (`@3xl`): nenhum teste o cobre, é a lacuna de arranjo acima.

## Gate

Verified at a3eb11f:

`dotnet test Esclari.Api.Tests --artifacts-path D:/esclari/.artifacts-agente-v` - 1011 passed, 0 failed (3 min 12 s)
`cd web && npx vitest run` - 1041 passed, 0 failed (112 arquivos)
`npm --prefix web run build` - exit 0
`npm --prefix web run coverage && crapper --use-existing-coverage -s src --threshold 9.99` - cobertura 1041 passed; `crapper` exit 2: `useModo` (`Autenticado.tsx`) 11,0 e `Usuarios` (`administracao.cadastros`) 10,0, ambos fora do diff; maior CRAP em `indicadores/`: `variacaoDaTaxa` 9,2
`cd web && dryer ... --threshold 0.8` - exit 0, "No duplicate candidates found."
C37 Playwright - 2 passed, exit 0
Round 1 (carried from 1bf47dd): 1010 API / 1039 Vitest; a diferença (+1, +2) são os 3 testes novos.

## Ranked gaps

Nenhum defeito de código. O que resta é decisão do dono do projeto:

1. Arranjo sem check (plan `## Observable`/`## Surface`): ordem das seções (`indicadores.test.tsx:95-113`, teste existe, check não), par de gráficos em duas colunas a partir de `@3xl/main` (`Indicadores.tsx:155`, sem teste; a correção trocou `@4xl` por `@3xl` e nenhum teste falha se voltar), alvo de 44 px de "Ver como tabela", "seções 3 e 4 em duas colunas" a 844 px. Opções: aprovar checks novos (checks aprovados não mudam sem o dono) ou registrar o risco aceito em um AD (AD-036 como modelo).
2. C38: `crapper` abaixo de 10 não passa por `useModo` (`Autenticado.tsx`) e `Usuarios`, anteriores à feature. O dono decide: AD aceitando o resíduo, ou uma feature separada que os reduza.
3. Precisão das provas escritas: as linhas `Proof:` de C15, C16 e C17 em `checks.md` não nomeiam os testes novos (`desfechos.test.tsx` "ver como tabela", `fila.test.tsx` "fila por estado", `series.test.tsx` "toque no gráfico...", `P90_usa_o_posto_mais_proximo_com_dez_valores`); quem roda só a prova escrita não os executa. C37 e C7 mantêm as imprecisões da rodada 1. `checks.md` C28/C1 ainda trazem o texto pré-emenda (a emenda vive no `plan.md`).
4. Tabela das colunas ("Conversas por dia") e coluna "Percentual" da fila: sem asserção própria (a coluna de desfechos mata o mesmo código).
