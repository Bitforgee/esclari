# Indicadores claros para o gestor verification

**Verdict**: FAIL
**Profile**: ui
**Diff range**: 8190316..3864a83 (HEAD); correção da rodada 3: a3eb11f..3864a83
**Round**: 3 - scoped
**Verifier**: independent sub-agent (author != verifier)

Rodada 3, escopo: o diff da correção (`a3eb11f..3864a83`: `Autenticado.tsx`, `Usuarios.tsx`, `series.test.tsx`, `checks.md`, `STATE.md`) e todo veredito que não foi PASS na rodada 2. Todas as provas dos 42 checks rodaram de novo por inteiro em `3864a83`. Os 42 checks passam e C38 (`crapper`) agora sai 0. O veredito continua FAIL por um motivo só: dois mutantes sobreviveram na superfície nova de C42 (a tabela da barra da fila): o nome das colunas e a coluna "Percentual" não são afirmados pela prova de C42. Nenhum código foi alterado por este verificador.

## Binding sources

Carried from a3eb11f, exceto onde a linha diz o contrário. A correção não tocou fonte vinculante nem `Esclari.Api` (`git diff --stat a3eb11f..HEAD -- Esclari.Api Esclari.Api.Tests web/e2e` vazio).

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `.specs/STATE.md` AD-037 (rankings em HTML, `recharts` sai) | yes - carried from a3eb11f; C31 reconfirmado em 3864a83 (exit 0 e `git diff --exit-code 8190316 -- web/package.json` exit 0) | none | - |
| `.specs/STATE.md` AD-055 (saída do MCP) | yes - carried from a3eb11f; `Esclari.Api/Mcp` não está no diff da correção | none | - |
| `.specs/STATE.md` AD-035 (contraste 3:1) | yes - carried from a3eb11f; C36 passa em 3864a83 | none | - |
| `.specs/STATE.md` AD-060 (novo na correção) | yes - lido em 3864a83; registra a feature, a rota e as emendas | none | - |
| plan `## Surface`, `## Observable` e `## Emendas de construção` (ordem, arranjo, alvo de toque) | yes - relidos em 3864a83 | none | ver abaixo |
| AD-034 e as 3 referências externas do plan `## Sources` | no - carried from a3eb11f; só justificam | not assessed | - |

Arranjo recomputado contra o plano. Cobertos agora: ordem das 6 seções e posição depois da barra do período (C39, `indicadores.test.tsx:104-113`), par de gráficos em 2 colunas a partir de `@3xl/main` (C40, `series.test.tsx:125`), rankings em `@4xl/main` (C40, `indicadores.test.tsx:86`), alvo de 44 px (C41, `series.test.tsx:109`). Sem teste: o `plan.md ## Observable` diz "com `main` de 768 px ou mais as seções 3 e 4 ficam em duas colunas". As grades `dl` das seções (`Indicadores.tsx:141`, `:148`, `:173` e `Fila.tsx:18`, todas `@3xl/main:grid-cols-2|3`) não são afirmadas por nenhum teste (`grep "3xl|grid-cols"` nos `*.test.*` só acha `indicadores.test.tsx:86` e `series.test.tsx:125`). C40 só nomeia o par de gráficos e os rankings; o texto do plano para as seções 3 e 4 fica sem check. Não injetei falha ali (não há teste que a pegue).

## Checks

Provas rodadas em 3864a83:

- API: `dotnet test Esclari.Api.Tests --artifacts-path D:/esclari/.artifacts-agente-v` inteira, 1011 passed, 0 failed (3 min 26 s); em seguida `--no-build --filter "FullyQualifiedName~CalculoDetalheTests|FullyQualifiedName~IndicadoresDetalheTests" --logger "console;verbosity=normal"`: 15 de 15 listados `Aprovado` por nome, incluindo `P90_usa_o_posto_mais_proximo_com_dez_valores` e `Desfechos_contam_cada_um_e_ficam_nulos_sem_conversas_lidas`.
- Web: `npx vitest run` inteiro, 1043 passed (112 arquivos); `npx vitest run src/indicadores --reporter=verbose`: 15 arquivos, 61 passed, cada teste citado abaixo listado por nome.
- C31: `node -e ...` exit 0; `git diff --exit-code 8190316 -- web/package.json` exit 0.
- C37: `npx playwright test -c playwright.verif.config.ts e2e/celular.spec.ts -g "Indicadores claros"` com `webServer` Vite na 5199 (`--strictPort`), API simulada: 2 passed, exit 0. Config e `web/test-results` apagados.

Os arquivos de teste tocados pela correção são só `series.test.tsx`; as linhas dos outros arquivos de teste e da API seguem as da rodada 2 (confirmadas com `Select-String "Assert\."` em `IndicadoresDetalheTests.cs` e `CalculoDetalheTests.cs` em 3864a83).

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | 5 chaves de `desfechos`, soma = conversas | `...IndicadoresDetalheTests.Desfechos_somam_o_total_de_conversas` Aprovado | `Esclari.Api.Tests/IndicadoresDetalheTests.cs:76` - `Assert.Equal([2, 1, 1, 1, 1], chaves.Select(k => j.GetProperty(k).GetInt32()))`; `:77` soma = `conversas` | PASS |
| C2 | com `setorId`, `desfechos` e `conversas` nulos | `...Com_setor_desfechos_e_conversas_sao_nulos` Aprovado | `IndicadoresDetalheTests.cs:88-89` - `Assert.Equal(JsonValueKind.Null, j.GetProperty("desfechos").ValueKind)` | PASS |
| C3 | 4 segmentos "nome · contagem · percentual", 69,6 % | `-t "barra com rotulo direto"` passou | `web/src/indicadores/desfechos.test.tsx:25` (carried from a3eb11f) - `expect(itens).toEqual(['Resolvida pelo assistente · 80 · 69,6 %', ... 'Encerrada pelo aluno · 0 · 0 %'])` | PASS |
| C4 | "de 115 conversas com desfecho", "5 ainda sem desfecho", fora da barra | `-t "denominador"` passou | `desfechos.test.tsx` - `findByText('de 115 conversas com desfecho · 5 ainda sem desfecho')`, larguras `toBeCloseTo(100, 5)` | PASS |
| C5 | 9 mostra aviso, 10 não | `-t "amostra pequena"` passou | `desfechos.test.tsx` - aviso com 9; `queryByText('Amostra pequena: menos de 10 conversas')).not.toBeInTheDocument()` com 10 | PASS |
| C6 | `null` sem barra e com a frase | `-t "sem filtro de setor"` passou | `desfechos.test.tsx` - `getByText('Desfechos do assistente aparecem na visão geral, sem filtro de setor.')`, `queryByRole('img')` ausente | PASS |
| C7 | tons distintos (>= 10 pontos) e separador de 2 px | `-t "tons de cinza distintos"` e `-t "separador de 2 px"` passaram | `contraste.test.ts` - `toBeGreaterThanOrEqual(10)`; `desfechos.test.tsx` - `toHaveClass('gap-0.5', 'bg-background')`. Precision gap mantido: o texto diz "3 tons entre os 4 segmentos", o teste mede 4 tokens | PASS |
| C8 | 3 dias dão `dia`, 3 pontos, dia sem atividade em 0 | `...Ate_62_dias_um_ponto_por_dia` Aprovado | `CalculoDetalheTests.cs:33,35` - `Assert.Equal("dia", d.Granularidade)`, `Assert.Equal((0, 0), (p.ChamadosAbertos, p.ChamadosResolvidos))` | PASS |
| C9 | 62 dias `dia` (62 pontos), 63 `semana` | `...Limite_de_62_dias` Aprovado | `CalculoDetalheTests.cs:45-46` - `Assert.Equal(("dia", 62), (com62.Granularidade, com62.Pontos.Count))`, `Assert.Equal("semana", com63.Granularidade)` | PASS |
| C10 | `inicio` é segunda no fuso, semana tocada entra inteira | `...Semana_comeca_na_segunda_no_fuso` Aprovado | `CalculoDetalheTests.cs:58-61` - `DayOfWeek.Monday`, `new DateOnly(2026, 8, 31)`, `Assert.Equal(11, d.Pontos.Count)` | PASS |
| C11 | 23:30 local de 10/11 cai em 10/11 | `...Ponto_usa_o_fuso_da_instituicao` Aprovado | `CalculoDetalheTests.cs:71` - `Assert.Equal([1, 0], d.Pontos.Select(p => p.ChamadosAbertos))` | PASS |
| C12 | cada série pela sua data, soma de resolvidos = `/api/indicadores` | `...Serie_conta_cada_coisa_pela_sua_data` Aprovado | `IndicadoresDetalheTests.cs:107-111` - `[3, 1]`, `[2, 0]`, `Assert.Equal(1, resolvidos)` e igual a `chamadosResolvidos` | PASS |
| C13 | colunas com eixo em 0, máximo, pico rotulados | `-t "colunas de conversas"` passou | `series.test.tsx:29-34` - `toHaveLength(3)`, `'Maior volume: 12 em 11/11'`, `expect(alturas).toEqual([(2/12)*100, 100, (5/12)*100])` | PASS |
| C14 | 2 linhas, uma tracejada, rótulo no fim, "por semana" | `-t "duas linhas"` passou | `series.test.tsx:49-52,58` - `toBe('6 4')`, `'Abertos · 2'`, `'Chamados abertos e resolvidos por semana'` | PASS |
| C15 | `role="img"` + `aria-label` e "Ver como tabela" (séries, barras de desfecho, barra da fila) | `-t "ver como tabela"` em `series.test.tsx` e em `desfechos.test.tsx`, `-t "fila por estado"` em `fila.test.tsx` passaram | `series.test.tsx:70,76` - `['Dia', 'Abertos', 'Resolvidos']`, `toContain('Total 6 abertos e 7 resolvidos')`; `desfechos.test.tsx:63-69` - linhas `['Resolvida pelo assistente', '80', '69,6\u00a0%'] ...` e `toContain('Total 115.')`; `fila.test.tsx:48` - `toContain('Em atendimento: 6. Aguardando aluno: 2. Resolvido: 4. Total 15.')`. As 3 Proof do `checks.md` agora cobrem os 3 arquivos | PASS |
| C16 | foco e toque mostram "dia, valor" em texto >= 12 px | `-t "valor do ponto"` e `-t "toque no gráfico"` passaram (os dois nomes aparecem no verbose) | `series.test.tsx:99-102` - `fireEvent.pointerDown(grupo, { clientX: 130 })` -> `findByText('10/11: 2 conversas')`; `:91` `toHaveClass('text-sm')` | PASS |
| C17 | mediana 2,5 e p90 10 (n = 4), p90 9 (n = 10) | `...Mediana_e_p90_da_primeira_resposta` e `...P90_usa_o_posto_mais_proximo_com_dez_valores` Aprovado | `CalculoDetalheTests.cs:91-92` - `Assert.Equal(2.5, ...)`, `Assert.Equal(10, d.Tempos.PrimeiraRespostaP90Horas)`; `:103-104` - `Assert.Equal(5.5, ...)`, `Assert.Equal(9, ...)` | PASS |
| C18 | resolução desconta "aguardando aluno": 10 h com 4 h dá 6 h | `...Resolucao_desconta_aguardando_aluno` Aprovado | `CalculoDetalheTests.cs:113` - `Assert.Equal(6, d.Tempos.ResolucaoMedianaHoras)` | PASS |
| C19 | sem chamados, 4 campos nulos | `...Sem_chamados_tempos_nulos` Aprovado | `CalculoDetalheTests.cs:79` - `Assert.Equal(new Tempos(null, null, null, null), d.Tempos)` | PASS |
| C20 | 4 chaves, soma = `chamadosAbertos`, `FECHADO` fora | `...Estados_somam_os_chamados_abertos` Aprovado | `IndicadoresDetalheTests.cs:129-131` - `[1, 2, 1, 1]`, `Assert.Equal(5, ...)`, `Assert.False(estados.TryGetProperty(Fechado, out _))` | PASS |
| C21 | `maisAntigoDias` 9, ignora aguardando e resolvido, nulo sem nenhum | `...Mais_antigo_conta_so_o_que_espera_a_equipe` Aprovado | `CalculoDetalheTests.cs:127,131` - `Assert.Equal(9, d.MaisAntigoDias)`, `Assert.Null(nenhum.MaisAntigoDias)` | PASS |
| C22 | "mediana 2,5 h" e "9 em 10 ... até 10 h", sem média | `-t "mediana e p90"` passou | `fila.test.tsx` - `toEqual(['mediana 2,5 h', '9 em 10 foram atendidos em até 10 h'])`, `queryByText(/média/i)).not.toBeInTheDocument()` | PASS |
| C23 | barra empilhada pelos 4 estados com rótulo, contagem, "mais antigo" | `-t "fila por estado"` passou | `fila.test.tsx:40-45` - `toEqual(['Aberto · 3 · 20 %', 'Em atendimento · 6 · 40 %', 'Aguardando aluno · 2 · 13,3 %', 'Resolvido · 4 · 26,7 %'])`; `:47` `'O mais antigo espera há 9 dias'` | PASS |
| C24 | com 0, "Nenhum chamado aberto agora." e nenhuma barra | `-t "nenhum chamado aberto"` passou | `fila.test.tsx` - `getByText('Nenhum chamado aberto agora.')`, `queryByRole('img')).not.toBeInTheDocument()` | PASS |
| C25 | 7 taxas com seta, pontos e melhor/pior, "sem variação" em 0 | `-t "variacao nas sete taxas"` passou | `variacao.test.tsx:79-89` - `toEqual([['Indicador principal','▲ 10,0 pts · melhor'], ... ['Abandono','sem variação'], ...])` | PASS |
| C26 | direção por taxa | `-t "direcao de cada taxa"` passou | `variacao.test.tsx:20-29` - `expect(sobe).toEqual([... ['Escalonamento','▲ 10,0 pts · pior'] ...])` | PASS |
| C27 | omite com anterior sem dados, taxa nula, 9 casos; mostra com 10 | `-t "omite a variacao"` passou | `variacao.test.tsx:41,44-45` - `expect(variacaoDaTaxa(...) !== undefined, nome).toBe(mostra)` | PASS |
| C28 | Resumo: principal, variação, "de 115 conversas com desfecho", definição | `-t "resumo com denominador"` passou | `resumo.test.tsx:30-33` - `'de 115 conversas com desfecho'`, `'▲ 4,2 pts · melhor'`, `'Resolvidas sem reabertura, sobre as conversas com desfecho.'`; o texto de C28 no `checks.md` agora é o emendado | PASS |
| C29 | variação nunca só por cor | `-t "nunca so por cor"` passou | `variacao.test.tsx:96-97` - `toHaveLength(7)`, `toMatch(/^(▲|▼) [\d,]+ pts? · (melhor|pior)$|^sem variação$/)` | PASS |
| C30 | rankings em barras com valor ao fim, maior em 100 %, "—" se vazio | `-t "barras com valor escrito"` passou | `ranking.test.tsx:31-32,40-41` - células `[['Boleto','8'],['Matrícula','2']]`, larguras `['100%','25%']`, `getByText('—')` | PASS |
| C31 | sem pizza/rosca/medidor/3D e sem dependência nova | `node -e ...` exit 0; `git diff --exit-code 8190316 -- web/package.json` exit 0 | comando do próprio check, 2 provas por exit code | PASS |
| C32 | período vazio mantém a frase, sem seção nova | `-t "sem atendimentos"` passou | `estados.test.tsx:30,32` - `findByText('Sem atendimentos neste período.')`, `queryByRole('region', {name: titulo})).not.toBeInTheDocument()` | PASS |
| C33 | `detalhe` pendente: `min-h` e "Carregando…" | `-t "carregando"` passou | `estados.test.tsx:40-41` - `getByText('Carregando…')` com `toHaveClass('min-h-40')` | PASS |
| C34 | `detalhe` em 500: erro com "Tentar de novo" só nas seções novas | `-t "erro so no detalhe"` passou | `estados.test.tsx:48-55` - `toHaveLength(3)`, `'Tentar de novo'`, Resumo `'71,2 %'` | PASS |
| C35 | 403/400/401 e formato literal | `...Quem_pode_e_o_formato_da_resposta` Aprovado | `IndicadoresDetalheTests.cs:143-148` - `Forbidden` x3, `BadRequest` x2, `Unauthorized`; `:151-154` chaves literais | PASS |
| C36 | 3:1 nas marcas e 4,5:1 no texto, 4 temas | `-t "contraste de graficos"` passou | `contraste.test.ts:76,80,82` - `toHaveLength(4)`, `toBeGreaterThanOrEqual(3)`, `toBeGreaterThanOrEqual(4.5)` | PASS |
| C37 | 360×780 e 844×390: sem rolagem horizontal, sem texto cortado, gráficos >= 160 px | Playwright 2 passed, exit 0 | `web/e2e/celular.spec.ts:100-104` - `expect(medidas.rolagem).toBeLessThanOrEqual(0)`, `expect(h).toBeGreaterThanOrEqual(160)`, `expect(medidas.cortados).toEqual([])`. Precision gap mantido: só SVG de série entra nos 160 px | PASS |
| C38 | suítes e gates verdes | ver `## Gate` | 1011 API, 1043 Vitest, build exit 0, `dryer` exit 0, `crapper` exit 0 (maior CRAP 9,8) | PASS |
| C39 | 6 seções na ordem, depois da barra do período | `-t "as seções vêm na ordem"` passou | `indicadores.test.tsx:104-111` - `expect(termos).toEqual([['Resumo', ['Indicador principal']], ['O assistente está resolvendo?', [...]], ['Quanto chega e como evolui?', ...], ['A equipe responde a tempo?', ...], ['Qualidade do atendimento', ...], ['O que mais pesa', []]])`; `:113` `compareDocumentPosition(secoes[0]) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()`. Mutante de ordem morto | PASS |
| C40 | par de gráficos `grid-cols-1` -> `@3xl/main:grid-cols-2`; rankings `@4xl/main` | `-t "duas colunas a partir de um main de 768"` e `-t "as medidas formatam taxa"` passaram | `series.test.tsx:125` - `expect(grade).toHaveClass('grid-cols-1', '@3xl/main:grid-cols-2')`; `indicadores.test.tsx:86` - `expect(conversas.parentElement).toHaveClass('grid-cols-1', '@4xl/main:grid-cols-2')`. Os dois mutantes mortos. Precision gap: a prova 2 está num teste de formato, o nome não diz layout | PASS |
| C41 | botão `max-md:h-11` e `pointer-coarse:h-11`; tabela Dia e Conversas | `-t "tabela das colunas e alvo de toque"` passou | `series.test.tsx:109` - `expect(botao).toHaveClass('max-md:h-11', 'pointer-coarse:h-11')`; `:113` - `toEqual(['Dia', 'Conversas'])`; `:114-118` - `[['10/11','2'],['11/11','12'],['12/11','5']]`. Mutante em `ui/button.tsx` morto. Limite: afirma a classe, não a altura computada | PASS |
| C42 | tabela da fila com colunas Item, Quantidade, Percentual e contagens 3, 6, 2, 4 | `-t "fila por estado"` passou | `fila.test.tsx:50` - `expect(s.getAllByRole('row').slice(1).map((l) => within(l).getAllByRole('cell')[1].textContent)).toEqual(['3', '6', '2', '4'])`. Só as contagens estão afirmadas: nenhum `columnheader` e nenhuma célula da coluna "Percentual" (ver Faults M4, M5, M6, mutantes sobreviventes) | FAIL |

## Coverage

Recomputado nas linhas que a correção tocou e nas 4 novas (C39-C42); o resto carried from a3eb11f sem diferença (nenhum arquivo de código do `indicadores/` nem da API mudou).

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| seções da tela (6) | plan `## Surface` | Resumo, assistente, evolução, equipe, qualidade, categorias: C39, `indicadores.test.tsx:104-111` | - |
| quebras de coluna (2) | plan `## Observable` + `## Emendas` | gráficos `@3xl` C40 `series.test.tsx:125`; rankings `@4xl` C40 `indicadores.test.tsx:86` | grades `dl` das seções 3 e 4 (`Indicadores.tsx:141,148,173`, `Fila.tsx:18`, `@3xl/main`): nenhum teste, o plano as nomeia |
| alvo de toque do "Ver como tabela" (1) | `Figura.tsx:64` usa `Button size="xs"`; `ui/button.tsx:24` | C41 `series.test.tsx:109` | - |
| tabela das colunas (Dia, Conversas) | `Series.tsx` | C41 `series.test.tsx:113-118` | - |
| colunas da tabela da barra da fila (Item, Quantidade, Percentual) | `Figura.tsx:14` | Quantidade: `fila.test.tsx:50`; Item: nenhuma asserção; Percentual: só `desfechos.test.tsx:63-69` (barra de desfechos, fora da prova de C42) | cabeçalhos Item e Quantidade e Percentual; coluna Percentual da fila |
| gráficos (5), `tempos` (4), `desfechos` (5), estados (4), taxas (7), temas (4), viewports (2), `granularidade`, fusos, status (4) | `checks.md` Coverage | sem diferença desde a3eb11f | - |

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| Decide, atravessa a rota HTTP (`ConsultarIndicadoresDetalhe`) | `CalculoDetalhe.cs`, `ConsultarIndicadoresDetalhe.cs`, `LeitorIndicadores.cs` | rota: `IndicadoresDetalheTests.cs` (5) · camada: `CalculoDetalheTests.cs` (10) | yes - carried from a3eb11f; 15 de 15 rodaram em 3864a83 |
| Decide, na tela (variação, limite de 10, formato dos tempos) | `Variacao.tsx`, `Fila.tsx` | uma na própria camada | yes - `variacao.test.tsx:20-46`, `fila.test.tsx:23-24` |
| Desenho (SVG) sem condicional | `Figura.tsx`, `Series.tsx`, `Desfechos.tsx` | uma de renderização | partial - rótulos, `role`, `aria-label` e valores afirmados; a tabela da fila sem cabeçalhos e sem a coluna Percentual (C42) |

## Faults injected

Worktree `D:\esclari-verif3-wt` em 3864a83, `node_modules` por junção; cada falha desfeita com `git checkout -- <arquivo>` dentro do scratch; worktree removido com `git worktree remove --force` e `prune`. `git status --porcelain` da árvore real antes e depois: só `?? .tmp-dotnet.log`, igual. Apaguei `.tmp-tr/` e o log em `.artifacts-agente-v/` que as minhas provas criaram. Config do Playwright e `web/test-results` apagados. Nenhum `git stash`.

| Mutation | Location | Killed |
| --- | --- | --- |
| M1 par de gráficos `@3xl/main:grid-cols-2` -> `@4xl/main:grid-cols-2` | `Indicadores.tsx:155` | yes - falhou `series.test.tsx` "o par de gráficos fica em duas colunas a partir de um main de 768 px" (C40); 18 outros passaram |
| M2 seção "Qualidade do atendimento" movida para antes de "A equipe responde a tempo?" | `Indicadores.tsx:161-177` | yes - falhou `indicadores.test.tsx` "as seções vêm na ordem das perguntas do gestor e cada medida na sua seção" (C39) |
| M3 rankings `@4xl/main:grid-cols-2` -> `@3xl/main:grid-cols-2` | `Indicadores.tsx:179` | yes - falhou `indicadores.test.tsx` "as medidas formatam taxa, horas e número..." (C40, prova 2) |
| M7 `ui/button.tsx` tamanho `xs` sem `pointer-coarse:h-11` | `ui/button.tsx:24` | yes - falhou `series.test.tsx` "tabela das colunas e alvo de toque de 44 px" (C41) |
| M4b coluna Quantidade removida da tabela da barra (cabeçalho e células) | `Figura.tsx:14` | yes - falhou `fila.test.tsx` "fila por estado" (C42) e `desfechos.test.tsx` "ver como tabela" |
| M4 cabeçalho "Quantidade" tirado de `colunas` (células intactas) | `Figura.tsx:14`, `src/indicadores` inteiro: 15 arquivos passed | **no - sobreviveu** (C42: nenhum `columnheader` é afirmado) |
| M5 cabeçalho `Item` -> `Nome` | `Figura.tsx:14`, `src/indicadores` inteiro: 15 arquivos passed | **no - sobreviveu** (C42) |
| M6 células "Percentual" da tabela viram `''` | `Figura.tsx:14`; suíte inteira: só `desfechos.test.tsx` "ver como tabela" falhou | parcial - `fila.test.tsx` "fila por estado" (prova de C42) **não** pega; só o teste de desfechos pega, e ele não é prova de C42 |
| M8 refator: `useVoltar` sempre `navegar(-1)` | `Autenticado.tsx:409`; suíte inteira | yes - falharam `voltar.test.tsx` "Voltar volta direto para a lista do papel" (ATENDENTE e ALUNO) |
| M9 refator: `ConfirmarDesativacao` com `aberto={false}` | `Usuarios.tsx` (novo componente); suíte inteira | yes - 4 falhas, entre elas "desativar pede confirmação" |
| M10 refator: `vazioDaBusca` com condição invertida | `Usuarios.tsx`; suíte inteira | yes - 5+ falhas, entre elas "lista vazia numa linha" e "vazio com ícone nas 6 listas" |
| M11 refator: `perfilDoModo.lista` sempre `'solicitacoes'` (ramo ATENDENTE -> `'fila'`) | `Autenticado.tsx` (`perfilDoModo`); suíte inteira: 112 arquivos passed | **no - sobreviveu**. O ramo é anterior à correção (a linha foi só movida de `useModo`), o teste de voltar de ATENDENTE passa mesmo assim; lacuna herdada, não introduzida pela correção |

Revisão do diff do refator (`git diff a3eb11f..HEAD -- web/src/Autenticado.tsx web/src/administracao/cadastros/Usuarios.tsx`): `perfilDoModo` devolve os mesmos três valores que `useModo` calculava; `useVoltar` mantém `key === 'default' ? navegar(/slug/lista, { replace: true }) : navegar(-1)` e `useModo` continua só expondo `voltar` quando `celular && noChamado`; os hooks seguem chamados sem condição, na mesma ordem relativa; `ConfirmarDesativacao` repete `aberto`, `titulo`, `botao`, `destrutivo`, o texto filho e `confirmar`; `vazioDaBusca` repete os dois objetos literais. Sem mudança de comportamento que eu consiga ver; 1043 testes passam.

## Gate

Verified at 3864a83:

`dotnet test Esclari.Api.Tests --artifacts-path D:/esclari/.artifacts-agente-v` - 1011 passed, 0 failed (3 min 26 s)
`cd web && npx vitest run` - 1043 passed, 0 failed (112 arquivos)
`npm --prefix web run build` - exit 0
`npm --prefix web run coverage` - exit 0, 1043 passed; `cd web && crapper --use-existing-coverage -s src --threshold 9.99` - exit 0 (maior CRAP: `Sidebar` 9,8)
`cd web && dryer ... --threshold 0.8` - exit 0, "No duplicate candidates found."
C31 `node -e ...` exit 0; `git diff --exit-code 8190316 -- web/package.json` exit 0
C37 Playwright - 2 passed, exit 0
Round 2 (carried from a3eb11f): 1011 API / 1041 Vitest; a diferença (+2) são os testes novos de `series.test.tsx`.

## Ranked gaps

Nenhum defeito de código. O que resta:

1. C42 não está provado por inteiro (mutantes M4, M5 e M6 vivos): `fila.test.tsx:50` lê só `cell[1]`; nenhum teste afirma os cabeçalhos Item, Quantidade, Percentual da tabela da barra (`Figura.tsx:14`), e a coluna Percentual da fila só é pega pelo teste de desfechos. É a superfície nova que o check pedia. Correção mínima: no teste "fila por estado", afirmar `getAllByRole('columnheader')` igual a `['Item', 'Quantidade', 'Percentual']` e as linhas inteiras (como `desfechos.test.tsx:63-69`).
2. Arranjo das seções 3 e 4 em duas colunas a 768 px de `main` (plan `## Observable`): grades `dl` (`Indicadores.tsx:141,148,173`, `Fila.tsx:18`) sem teste; C40 só cobre o par de gráficos e os rankings. Decisão do dono: aprovar mais um check ou aceitar o risco em um AD.
3. M11: `perfilDoModo.lista` (ramo ATENDENTE -> `fila`) sem teste que o pegue; é código anterior à feature, só movido pelo refator.
4. Precisões herdadas: C7 ("3 tons" x 4 tokens), C37 (barras empilhadas de 24 px fora dos 160 px; "cortado" = reticências), C40 prova 2 em teste de formato, C41 afirma a classe e não a altura computada.
