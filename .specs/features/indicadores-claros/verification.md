# Indicadores claros para o gestor verification

**Verdict**: FAIL
**Profile**: ui
**Diff range**: 8190316..1bf47dd
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

Todas as provas rodaram em `HEAD` (1bf47dd): 38 checks, nenhum pulado. Os 36 testes nomeados existem e rodaram, e a suíte inteira está verde. O veredito é FAIL por 2 mutantes que sobreviveram (C16, C17), 1 check cuja afirmação o código não cumpre (C15) e elementos de arranjo/ordem que o plano decide e nenhum check cobre. Nenhum código foi alterado por este verificador.

## Binding sources

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `.specs/STATE.md` AD-037 (rankings em HTML, `recharts` sai) | yes - linha da tabela de ADs | none: `git diff --exit-code 8190316 -- web/package.json` exit 0 e varredura de `web/src/indicadores` sem `recharts`/pizza/rosca/medidor (C31, exit 0); rankings seguem como barras HTML dentro da tabela (`Indicadores.tsx:58`) | - |
| `.specs/STATE.md` AD-055 (saída do MCP `ver_indicadores`) | yes - linha da tabela de ADs | none: `git diff 8190316..HEAD --stat -- Esclari.Api/Mcp Esclari.Api.Tests/McpFerramentasTests.cs` vazio; `ConsultarIndicadores.cs` só extrai `Validar`/`Ler` para `LeitorIndicadores`, o `Resultado` de `GET /api/indicadores` não ganha campo (`ConversaLida` ganha `CriadaEm` opcional, que não é serializado); suíte API inteira verde (inclui `McpFerramentasTests`) | - |
| `.specs/STATE.md` AD-035 (contraste 3:1 dos gráficos entra nesta rodada) | yes - linha da tabela de ADs | none: C36 mede 3:1 (marcas) e 4,5:1 (texto) nos 4 cenários | - |
| plan `## Surface` (ordem das 6 seções sob a barra do período) e `## Observable` (arranjo, alvo de toque) | yes - `plan.md` | none | ordem das seções (teste existe em `web/src/indicadores/indicadores.test.tsx:95-113`, mas nenhum check a cita); arranjo em 2 colunas dos gráficos a partir de `@4xl/main` (`indicadores.test.tsx:86`, sem check); alvo de 44 px de "Ver como tabela" no celular (Observable "toque", sem check); "seções 3 e 4 em duas colunas" a 844 px (Observable, sem check nem implementação evidente: só os gráficos da seção 4 e os tempos da seção 3 vão a 2 colunas) |
| AD-034 e as 3 referências externas do plan `## Sources` (Supportman, Few, accessibility.build) | no - não abri; o plano as usa só como justificativa, e nenhuma delas fixa rota, rótulo ou status que um check possa contradizer | not assessed | - |

Enumeração por tela (o que o plano decide e um seletor alcança):

| Elemento decidido | Check | Observação |
| --- | --- | --- |
| barra do período acima das seções | `indicadores.test.tsx:113` | sem check |
| 6 seções na ordem Resumo, assistente, evolução, equipe, qualidade, o que pesa | `indicadores.test.tsx:95-111` | sem check |
| barra de desfechos (4 segmentos, rótulo direto, denominador, amostra pequena, vazio) | C3-C7 | ok |
| colunas e linhas, eixo em 0, pico, tracejada, rótulo no fim | C13, C14 | ok |
| tempos mediana + p90, sem média | C22 | ok |
| barra da fila + "mais antigo" + vazio | C23, C24 | ok |
| 7 variações com direção e omissão | C25-C27, C29 | ok |
| rankings em barras com valor ao fim e "—" | C30 | ok |
| "Ver como tabela", `role="img"` e `aria-label` em todo gráfico de série e de barras | C15 | o código só o faz em colunas e linhas (ver C15) |
| estados vazio, carregando, erro | C32-C34 | ok |
| celular 360×780 e 844×390 | C37 | só mede SVG de série; barras empilhadas (24 px) e ranking ficam fora do critério "160 px" |
| elementos que o código desenha e o plano não decide | - | `Alunos ativos`, `Conversas` e `Chamados resolvidos` como 3 cartões na seção 4 (`Indicadores.tsx:148-152`): o plano não os lista, mas cobre-se por `indicadores.test.tsx:63-79`; sem contradição |

## Checks

Provas rodadas: API `dotnet test Esclari.Api.Tests --artifacts-path D:/esclari/.artifacts-agente-v --filter "FullyQualifiedName~CalculoDetalheTests|FullyQualifiedName~IndicadoresDetalheTests"` (14 de 14 listados individualmente, exit 0); Web `npx vitest run` nos 8 arquivos da feature em uma chamada (31 de 31 listados individualmente); C31 `node -e ...` exit 0 e `git diff --exit-code 8190316 -- web/package.json` exit 0; C37 com config temporária contra Vite na 5199 (2 de 2, exit 0).

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | 5 chaves de `desfechos`, soma = conversas | `dotnet test --filter ...IndicadoresDetalheTests.Desfechos_somam_o_total_de_conversas` exit 0 | `Esclari.Api.Tests/IndicadoresDetalheTests.cs:76` - `Assert.Equal([2, 1, 1, 1, 1], chaves.Select(k => j.GetProperty(k).GetInt32()))` e `:77` soma == `conversas` de `/api/indicadores` | PASS |
| C2 | com `setorId`, `desfechos` e `conversas` nulos | `...Com_setor_desfechos_e_conversas_sao_nulos` exit 0 | `IndicadoresDetalheTests.cs:88` - `Assert.Equal(JsonValueKind.Null, j.GetProperty("desfechos").ValueKind)` e `:89` `conversas` nulo em todo ponto | PASS |
| C3 | 4 segmentos "nome · contagem · percentual", 69,6 % | `vitest ... -t "barra com rotulo direto"` passou | `web/src/indicadores/desfechos.test.tsx:24-29` - `expect(itens).toEqual(['Resolvida pelo assistente · 80 · 69,6 %', ... 'Encerrada pelo aluno · 0 · 0 %'])` | PASS |
| C4 | "de 115 conversas com desfecho", "5 ainda sem desfecho", fora da barra; sem `semDesfecho` omite | `-t "denominador"` passou | `desfechos.test.tsx:35` `findByText('de 115 conversas com desfecho · 5 ainda sem desfecho')`, `:39` soma das larguras `toBeCloseTo(100, 5)`, `:44` `queryByText(/ainda sem desfecho/)).not.toBeInTheDocument()` | PASS |
| C5 | 9 mostra aviso, 10 não | `-t "amostra pequena"` passou | `desfechos.test.tsx:49` aviso com 9 (5+2+2+0); `:53` `queryByText('Amostra pequena: menos de 10 conversas')).not.toBeInTheDocument()` com 10 | PASS |
| C6 | `null` sem barra e com a frase | `-t "sem filtro de setor"` passou | `desfechos.test.tsx:59` `getByText('Desfechos do assistente aparecem na visão geral, sem filtro de setor.')`, `:60` `queryByRole('img')` ausente | PASS |
| C7 | tons distintos por luminosidade (>= 10 pontos) e separador de 2 px | `-t "tons de cinza distintos"` e `-t "separador de 2 px"` passaram | `contraste.test.ts:89` `expect(luz[i] - luz[i - 1], ...).toBeGreaterThanOrEqual(10)` (claro e escuro; o preto não é medido); `desfechos.test.tsx:67` `toHaveClass('gap-0.5', 'bg-background')` e `:68` fundos `var(--grafico-1..3)`. Precisão: o texto do check diz "3 tons distintos entre os 4 segmentos"; o teste mede 4 tokens e o segmento encerrado (0) nem é desenhado (3 filhos); `:69` afirma `detalheVazio.desfechos.semDesfecho` e não tem relação com o check | PASS |
| C8 | 3 dias dão `dia` com 3 pontos e dia sem atividade em 0 | `CalculoDetalheTests.Ate_62_dias_um_ponto_por_dia` exit 0 | `Esclari.Api.Tests/CalculoDetalheTests.cs:33-35` - `Assert.Equal("dia", d.Granularidade)`, datas 10, 11, 12/11, `Assert.Equal((0, 0), (p.ChamadosAbertos, p.ChamadosResolvidos))` | PASS |
| C9 | 62 dias dá `dia` (62 pontos), 63 dá `semana` | `...Limite_de_62_dias` exit 0 | `CalculoDetalheTests.cs:45` - `Assert.Equal(("dia", 62), (com62.Granularidade, com62.Pontos.Count))` e `:46` `Assert.Equal("semana", com63.Granularidade)`; mutante 62→63 morto | PASS |
| C10 | `inicio` é segunda no fuso, semana tocada entra inteira, conta só o que está dentro | `...Semana_comeca_na_segunda_no_fuso` exit 0 | `CalculoDetalheTests.cs:58` `Assert.All(... DayOfWeek.Monday ...)`, `:59` primeiro ponto `2026-08-31`, `:60` 11 pontos, `:61` `Assert.Equal(1, d.Pontos[0].ChamadosAbertos)`; mutante do deslocamento de segunda morto | PASS |
| C11 | 23:30 local de 10/11 cai em 10/11 | `...Ponto_usa_o_fuso_da_instituicao` exit 0 | `CalculoDetalheTests.cs:71` - `Assert.Equal([1, 0], d.Pontos.Select(p => p.ChamadosAbertos))` | PASS |
| C12 | cada série conta pela sua data, duplicado fora, soma de resolvidos = `/api/indicadores` | `...Serie_conta_cada_coisa_pela_sua_data` exit 0 | `IndicadoresDetalheTests.cs:107` `[3, 1]` conversas, `:108` `[2, 0]` abertos, `:110` `Assert.Equal(1, resolvidos)`, `:111` igual a `chamadosResolvidos` da rota principal | PASS |
| C13 | colunas com eixo em 0, máximo, pico rotulados, 1 coluna por ponto | `-t "colunas de conversas"` passou | `web/src/indicadores/series.test.tsx:29` `querySelectorAll('[data-coluna]')).toHaveLength(3)`, `:30` `'Maior volume: 12 em 11/11'`, `:32` `getByText('0', {selector:'span'})`, `:34` `expect(alturas).toEqual([(2/12)*100, 100, (5/12)*100])` | PASS |
| C14 | 2 linhas, uma tracejada, rótulo no fim, "por semana" | `-t "duas linhas"` passou | `series.test.tsx:49` `stroke-dasharray` nulo em resolvidos, `:50` `toBe('6 4')` em abertos, `:51-52` `'Abertos · 2'`, `'Resolvidos · 2'`; `:58` `'Chamados abertos e resolvidos por semana'` | PASS |
| C15 | todo gráfico de série e de barras tem `role="img"` com `aria-label` (tipo, período, total, pico) e botão "Ver como tabela" | `-t "ver como tabela"` passou | `series.test.tsx:70` `columnheader` `['Dia','Abertos','Resolvidos']`, `:71-75` linhas da tabela, `:76` `aria-label` `toContain('Total 6 abertos e 7 resolvidos')`. Só o gráfico de linhas é exercido. No código, `Figura.tsx:91-117` (`Quadro`) dá botão e `role="img"` a colunas e linhas, mas `BarraEmpilhada` (`Figura.tsx:16`, usada em desfechos e fila) não tem botão e o `aria-label` não traz período, total nem pico (`Desfechos.tsx:18`, `Fila.tsx:31`), e o ranking (`Indicadores.tsx:30-80`) não tem `role="img"` nem `aria-label`. A tabela das colunas não é provada por nenhum teste | FAIL |
| C16 | foco do teclado **e toque** mostram "dia, valor" em texto >= 12 px | `-t "valor do ponto"` passou | `series.test.tsx:85` `findByText('11/11: 12 conversas')`, `:91` `toHaveClass('text-sm')`; todo o teste usa `grupo.focus()` e setas. O ramo de toque (`Figura.tsx:48`, `onPointerDown`) não é exercido: mutante `if (false) ir(` em `Figura.tsx` sobreviveu (17 de 17 passam) | FAIL |
| C17 | mediana 2,5 e p90 10 para 1, 2, 3, 10 (índice `ceil(0,9·n)−1`) | `...Mediana_e_p90_da_primeira_resposta` exit 0 | `CalculoDetalheTests.cs:91` `Assert.Equal(2.5, ...MedianaHoras)`, `:92` `Assert.Equal(10, ...P90Horas)`. Com n = 4 qualquer fração em (0,75; 1] dá o mesmo índice: mutante `Math.Ceiling(0.8 * valores.Count)` em `CalculoDetalhe.cs` sobreviveu (9 de 9 passam) | FAIL |
| C18 | resolução desconta "aguardando aluno": 10 h com 4 h dá 6 h | `...Resolucao_desconta_aguardando_aluno` exit 0 | `CalculoDetalheTests.cs:102` `Assert.Equal(6, d.Tempos.ResolucaoMedianaHoras)` e `:103` p90 | PASS |
| C19 | sem chamados, 4 campos nulos | `...Sem_chamados_tempos_nulos` exit 0 | `CalculoDetalheTests.cs:79` - `Assert.Equal(new Tempos(null, null, null, null), d.Tempos)` | PASS |
| C20 | 4 chaves, soma = `chamadosAbertos`, `FECHADO` fora | `...Estados_somam_os_chamados_abertos` exit 0 | `IndicadoresDetalheTests.cs:129` `Assert.Equal([1, 2, 1, 1], ...)`, `:130` `Assert.Equal(5, ... "chamadosAbertos")`, `:131` `Assert.False(estados.TryGetProperty(Fechado, out _))` | PASS |
| C21 | `maisAntigoDias` 9, ignora aguardando e resolvido, nulo sem nenhum | `...Mais_antigo_conta_so_o_que_espera_a_equipe` exit 0 | `CalculoDetalheTests.cs:116` `Assert.Equal(9, d.MaisAntigoDias)`, `:120` `Assert.Null(nenhum.MaisAntigoDias)` | PASS |
| C22 | "mediana 2,5 h" em destaque, "9 em 10 ... até 10 h", sem média | `-t "mediana e p90"` passou | `web/src/indicadores/fila.test.tsx:23` `toEqual(['mediana 2,5 h', '9 em 10 foram atendidos em até 10 h'])`, `:25` `queryByText(/média/i)).not.toBeInTheDocument()` | PASS |
| C23 | barra empilhada pelos 4 estados com rótulo e contagem, "mais antigo" | `-t "fila por estado"` passou | `fila.test.tsx:38-43` `toEqual(['Aberto · 3 · 20 %', 'Em atendimento · 6 · 40 %', 'Aguardando aluno · 2 · 13,3 %', 'Resolvido · 4 · 26,7 %'])`, `:45` `'O mais antigo espera há 9 dias'` | PASS |
| C24 | com 0 aparece "Nenhum chamado aberto agora." e nenhuma barra | `-t "nenhum chamado aberto"` passou | `fila.test.tsx:61` `getByText('Nenhum chamado aberto agora.')`, `:62` `queryByRole('img')).not.toBeInTheDocument()` | PASS |
| C25 | 7 taxas com seta, pontos e melhor/pior, "sem variação" em 0 | `-t "variacao nas sete taxas"` passou | `web/src/indicadores/variacao.test.tsx:79-89` `toEqual([['Indicador principal','▲ 10,0 pts · melhor'], ... ['Abandono','sem variação'], ... ['Reabertura','▼ 10,0 pts · melhor']])` | PASS |
| C26 | direção por taxa | `-t "direcao de cada taxa"` passou | `variacao.test.tsx:20-28` `expect(sobe).toEqual([... ['Escalonamento','▲ 10,0 pts · pior'] ...])` para as 7 e `:29` o espelho da queda | PASS |
| C27 | omite com anterior sem dados, taxa nula, 9 casos; mostra com 10 | `-t "omite a variacao"` passou | `variacao.test.tsx:41` `expect(variacaoDaTaxa(...) !== undefined, nome).toBe(mostra)` para os 5 casos, `:44-45` o mesmo para chamados (9 omite, 10 mostra); mutante `MinimoDeCasos = 9` morto | PASS |
| C28 | Resumo com principal, variação, denominador e definição | `-t "resumo com denominador"` passou | `web/src/indicadores/resumo.test.tsx:30` `'de 115 conversas com desfecho'`, `:31` `'71,2 %'`, `:32` `'▲ 4,2 pts · melhor'`, `:33` `'Resolvidas sem reabertura, sobre as conversas com desfecho.'` (texto emendado, ver Emendas) | PASS |
| C29 | variação nunca só por cor | `-t "nunca so por cor"` passou | `variacao.test.tsx:96` `toHaveLength(7)` e `:97` `expect(t).toMatch(/^(▲|▼) [\d,]+ pts? · (melhor|pior)$|^sem variação$/)`; segmentos com texto em `desfechos.test.tsx:24` e `fila.test.tsx:38` | PASS |
| C30 | rankings em barras com valor ao fim, maior em 100 %, "—" se vazio | `-t "barras com valor escrito"` passou | `web/src/indicadores/ranking.test.tsx:31` células `[['Boleto','8'],['Matrícula','2']]`, `:32` larguras `['100%','25%']`; `:40-41` `getByText('—')` e 0 barras no vazio | PASS |
| C31 | sem pizza/rosca/medidor/3D e sem dependência nova | `node -e ...` exit 0; `git diff --exit-code 8190316 -- web/package.json` exit 0 | comando do próprio check, 2 provas por exit code | PASS |
| C32 | período vazio mantém a frase e nenhuma seção nova | `-t "sem atendimentos"` passou | `web/src/indicadores/estados.test.tsx:30` `findByText('Sem atendimentos neste período.')`, `:32` `queryByRole('region', {name: titulo})).not.toBeInTheDocument()` nas 6 seções | PASS |
| C33 | `detalhe` pendente: cada seção nova com `min-h` e "Carregando…" | `-t "carregando"` passou | `estados.test.tsx:40-41` `getByText('Carregando…')` com `toHaveClass('min-h-40')` nas 3 seções que dependem do detalhe | PASS |
| C34 | `detalhe` em 500: erro com "Tentar de novo" só nas seções novas | `-t "erro so no detalhe"` passou | `estados.test.tsx:48` `toHaveLength(3)` alertas, `:51` botão `'Tentar de novo'`, `:53-55` Resumo `'71,2 %'`, Qualidade e Rankings presentes | PASS |
| C35 | 403 aluno/liderança/setor alheio, 400, 401, formato literal | `...Quem_pode_e_o_formato_da_resposta` exit 0 | `IndicadoresDetalheTests.cs:143-145` `HttpStatusCode.Forbidden` x3, `:146-147` `BadRequest` x2, `:148` `Unauthorized`, `:151-154` `Assert.Equal([...chaves literais da porta 1...], ...EnumerateObject().Select(p => p.Name))` nas 4 listas | PASS |
| C36 | 3:1 nas marcas e 4,5:1 no texto, nos 4 temas | `-t "contraste de graficos"` passou | `contraste.test.ts:76` `expect(cenarios).toHaveLength(4)`, `:80` `toBeGreaterThanOrEqual(3)` em `--grafico-1..4`, `:82` `toBeGreaterThanOrEqual(4.5)` em `--foreground` e `--muted-foreground` | PASS |
| C37 | 360×780 e 844×390: sem rolagem horizontal, sem texto cortado, gráficos >= 160 px | `npx playwright test -c playwright.verif.config.ts e2e/celular.spec.ts -g "Indicadores claros"` (Vite na 5199 com `simulada.ts`; config e `test-results` removidos) 2 passed | `web/e2e/celular.spec.ts:100` `expect(medidas.rolagem).toBeLessThanOrEqual(0)`, `:102` `expect(h).toBeGreaterThanOrEqual(160)` por SVG, `:104` `expect(medidas.cortados).toEqual([])`. Limites: "cortado" é medido só por `text-overflow: ellipsis`; só SVGs de série entram nos 160 px; rodou contra a API simulada, não contra o compose | PASS |
| C38 | suítes e gates verdes | ver `## Gate` | ver `## Gate`; `crapper` sai com 2 por `useModo` 11,0 e `Usuarios` 10,0, ambos fora do diff (resíduo anterior à feature) | PASS |

C38: o resíduo do `crapper` está registrado no `checks.md` (Handoff, item 4). Confirmei com `git diff 8190316..HEAD --stat -- web/src/Autenticado.tsx web/src/administracao`: vazio. Maior CRAP em `indicadores/`: `variacaoDaTaxa` 9,2. O check C38 escrito exige `crapper` abaixo de 10, então como escrito ele não passa; eu o conto como PASS só porque o `checks.md` registra a exceção e a feature não a causa. O dono deve confirmar essa leitura.

Emendas registradas no `checks.md`, avaliadas:

- Chave `encerradas` e quarto segmento "Encerrada pelo aluno": razoável e necessária. `ENCERRADA` é um desfecho real do domínio e já entra no denominador das taxas; sem a chave, a soma de `desfechos` não seria o total de conversas (AC 1). Custo: a porta 1 ganha uma chave além do literal do plano, e `Quem_pode_e_o_formato_da_resposta` (`IndicadoresDetalheTests.cs:153`) a fixa. O `plan.md` não foi corrigido; a emenda vive só no `checks.md`.
- Resumo "de N conversas com desfecho" em vez de "conversas e chamados": razoável. O denominador do indicador principal em `CalculoIndicadores` é só conversas com desfecho, então o texto do plano seria falso. O `plan.md` (AC 27) continua com o texto antigo.

## Coverage

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| statuses de `GET /api/indicadores/detalhe` (4) | plan `## Surface` | 200 `IndicadoresDetalheTests.cs:61`; 400 `:146-147`; 401 `:148`; 403 `:143-145` (C35) | - |
| chaves de `desfechos` (5) | `CalculoDetalhe.cs:8` (record) | resolvidas, transferidas, abandonadas, encerradas, semDesfecho: `IndicadoresDetalheTests.cs:76`, `CalculoDetalheTests.cs:132` | - |
| segmentos da barra de desfechos (4) | `Desfechos.cs:13-16` | 4 rótulos em `desfechos.test.tsx:24-29` | - |
| estados em `chamadosPorEstado` (4) | `CalculoDetalhe.cs:15` | `IndicadoresDetalheTests.cs:129`, `CalculoDetalheTests.cs:117` | - |
| estados da barra da fila (4) | `Fila.tsx:25` | `fila.test.tsx:38-43` | - |
| `granularidade` (2) | plan door 1 | dia `CalculoDetalheTests.cs:33`; semana `:57` | - |
| limite 62 (2 bordas) | `CalculoDetalhe.cs:14` | 62 `:45`; 63 `:46`; mutante 63 morto | - |
| campos de `tempos` (4) | `CalculoDetalhe.cs:9` | `CalculoDetalheTests.cs:91-92, 102-103` | p90 só provado em n = 4 e n = 1: o índice `ceil(0,9·n)−1` não é provado (mutante 0,8 sobreviveu) |
| taxas com variação (7) | `Variacao.tsx:8-16` | `variacao.test.tsx:79-89` | - |
| direção da taxa (7) | `Variacao.tsx:9-15` | `variacao.test.tsx:20-28` | - |
| omissão da variação (4 + chamados) | `Variacao.tsx:25-30` | `variacao.test.tsx:41, 44-45`; mutante `MinimoDeCasos` morto | - |
| gráficos (5) e o que cada um deve ter (role, rótulo, tabela) | plan AC 14 | barra de desfechos `desfechos.test.tsx:30`; colunas `series.test.tsx:35`; linhas `:62-77`; barra da fila `fila.test.tsx:46` | tabela das colunas; "Ver como tabela" e período/total/pico nas barras empilhadas; `role="img"` e `aria-label` do ranking (nenhum no código) |
| interação do ponto (2 entradas) | plan AC 15 | teclado `series.test.tsx:85-99` | toque (`onPointerDown`, `Figura.tsx:48`) |
| estados da tela (4) | plan `## Observable` | vazio `estados.test.tsx:30`; carregando `:41`; erro `:48`; não autorizado `IndicadoresDetalheTests.cs:143-145` | - |
| temas (4) | plan AC 35 | `contraste.test.ts:76-82` (4 cenários) | - |
| viewports (2) | plan AC 36 | `celular.spec.ts:100-104` (2 execuções) | - |
| portas (2) | plan `## Landing` | 1 C35; 2 C31 | - |
| fusos (2 bordas) | plan AC 9-10 | `CalculoDetalheTests.cs:71` e `:58-62` | - |
| seções da tela na ordem (6 + barra do período) | plan `## Surface` | `indicadores.test.tsx:95-113` (não ligado a check) | sem check |
| arranjo (2 colunas, alvo de toque de 44 px) | plan `## Observable` | `indicadores.test.tsx:86` (classe de grid); alvo de 44 px: `ui/button.tsx:24` (`max-md:h-11`) sem teste | sem check |

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| Decide, atravessa a rota HTTP (`ConsultarIndicadoresDetalhe`) | `CalculoDetalhe.cs`, `ConsultarIndicadoresDetalhe.cs`, `LeitorIndicadores.cs` | rota: `IndicadoresDetalheTests.cs` (5 testes) · camada: `CalculoDetalheTests.cs` (9 testes) | no - rota e papéis sim; "uma linha por caso das tabelas na camada" não vale para o p90, cujo índice só tem 1 caso (n = 4) e deixa o mutante 0,8 vivo |
| Decide, na tela (variação, limite de 10, formato dos tempos) | `Variacao.tsx`, `Fila.tsx` | uma na própria camada | yes - `variacao.test.tsx:20-46` cobre as 7 direções e os 5 casos de omissão; `fila.test.tsx:23-24` o formato dos tempos |
| Desenho (SVG) sem condicional | `Figura.tsx`, `Series.tsx`, `Desfechos.tsx` | uma de renderização | yes - rótulos, `role`, `aria-label` e valores assertados em `series.test.tsx:29-35` e `desfechos.test.tsx:24-30`, exceto as lacunas já listadas em C15 e C16 |

## Faults injected

Worktree `D:\esclari-verif-wt` (HEAD), `node_modules` por junção; removido ao fim. `git status --porcelain` da árvore real antes e depois: só `?? .artifacts-agente/`, igual ao baseline (removi o `.artifacts-agente-v/` que eu criei). `web/playwright.verif.config.ts` e `web/test-results` apagados.

| Mutation | Location | Killed |
| --- | --- | --- |
| 90º percentil: `Math.Ceiling(0.9 * valores.Count)` -> `0.8` | `Esclari.Api/Indicadores/CalculoDetalhe.cs` (`Percentil90`); proof `CalculoDetalheTests`: 9 de 9 passam | no |
| começo da semana: `(DayOfWeek + 6) % 7` -> `+ 5` | `CalculoDetalhe.cs` (`Balde`); falhou `CalculoDetalheTests.Semana_comeca_na_segunda_no_fuso` | yes |
| limite: `LimiteDeDiasPorDia = 62` -> `63` | `CalculoDetalhe.cs:14`; falhou `CalculoDetalheTests.Limite_de_62_dias` | yes |
| mínimo de casos: `MinimoDeCasos = 10` -> `9` | `web/src/indicadores/Variacao.tsx:4`; falhou `variacao.test.tsx` "omite a variacao" (`expected 'sem variação' to be undefined`) | yes |
| toque no gráfico desligado: `if (caixa.width > 0) ir(` -> `if (false) ir(` | `web/src/indicadores/Figura.tsx:48-51`; `series`, `fila` e `desfechos`: 17 de 17 passam | no |

Não injetei falha em `Desfechos.tsx` (limite de amostra pequena), em `Fila.tsx` nem no `maisAntigo`: o teto é de 5 e as superfícies escolhidas cobrem 3 arquivos de teste distintos da API e da web.

## Gate

`dotnet test Esclari.Api.Tests --no-build --artifacts-path D:/esclari/.artifacts-agente-v` - 1010 passed, 0 failed (3 min 27 s)
`cd web && npx vitest run` - 1039 passed, 0 failed (112 arquivos)
`npm --prefix web run build` - exit 0 (gera `Esclari.Api/wwwroot`, que está no `.gitignore`)
`npm --prefix web run coverage && crapper --use-existing-coverage -s src --threshold 9.99` - cobertura 1039 passed; `crapper` exit 2: `useModo` (`Autenticado.tsx`) 11,0 e `Usuarios` (`administracao.cadastros`) 10,0; ambos intocados por esta feature (resíduo anterior); nenhuma função de `indicadores/` passa de 9,2
`cd web && dryer ... --threshold 0.8` - exit 0, "No duplicate candidates found."

## Ranked gaps

1. C17 e Test policy da camada: o índice do p90 só é provado com n = 4 (`CalculoDetalheTests.cs:91-92`); `0.8` no lugar de `0.9` passa. Acrescentar um caso com n em que `ceil(0,9·n)` difira de `ceil(0,8·n)` (por exemplo n = 10: p90 é o 9º valor).
2. C16: o toque (`Figura.tsx:48`, `onPointerDown`) não tem teste (`series.test.tsx:79-92` só usa foco e setas); remover o ramo não quebra nada.
3. C15: a afirmação "todo gráfico de série e de barras" não vale para barras empilhadas (sem botão e sem período/total/pico no `aria-label`, `Desfechos.tsx:18`, `Fila.tsx:31`) nem para o ranking (sem `role="img"`/`aria-label`, `Indicadores.tsx:30-80`); a tabela das colunas não é provada. Decisão a tomar: corrigir o código ou emendar C15 e AC 14 para "série" apenas.
4. Arranjo sem check: ordem das seções (`indicadores.test.tsx:95-113`), grade em 2 colunas (`:86`), alvo de 44 px e "seções 3 e 4 em duas colunas" a 844 px (Observable). Ligar os dois testes existentes a checks e decidir o que o plano quis dizer com "seções 3 e 4 em duas colunas".
5. C37 (precisão): o critério de 160 px só mede `figure svg[preserveAspectRatio]`; barras empilhadas medem 24 px, e "texto cortado" é medido só por `text-overflow: ellipsis`. Corrigir a redação do check ou do AC 36.
6. C7 (precisão): "3 tons distintos entre os 4 segmentos" não bate com o teste (4 tokens medidos, 3 segmentos desenhados); o tema Preto não entra em "tons de cinza distintos"; `desfechos.test.tsx:69` afirma um valor sem relação com o check.
7. Plano e checks divergem: as duas emendas (`encerradas`, texto do Resumo) vivem só em `checks.md`; o `plan.md` ainda traz as 4 chaves e "conversas e chamados".
8. C38: como escrito, `crapper` abaixo de 10 não passa por `useModo` e `Usuarios`, anteriores à feature e fora do diff; o dono precisa aceitar o resíduo (AD) ou corrigir.
