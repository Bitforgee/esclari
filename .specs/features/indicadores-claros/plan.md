# Indicadores claros para o gestor

## Problem

A tela Indicadores mostra 13 números e 2 rankings, e o gestor não consegue responder às perguntas que o levam até ela. Hoje a tela é uma lista de valores: um número grande (indicador principal), três números médios e nove pares rótulo/valor, todos com o mesmo peso (`web/src/indicadores/Indicadores.tsx`).

- Não mostra evolução. Só há um valor por período, então "a demanda está subindo?" e "a fila está crescendo?" não têm resposta na tela.
- Não mostra de onde vem cada taxa. Autoatendimento, escalonamento e abandono são três percentuais separados, sem o número de conversas por trás (a API devolve só a taxa). Uma taxa de 100% sobre 2 conversas parece igual a 100% sobre 2.000.
- Os tempos aparecem como média (`tempoPrimeiraRespostaHoras`, `tempoResolucaoHoras`). A média esconde o aluno que esperou muito, e a fila não diz há quanto tempo o chamado mais antigo espera.
- Só o indicador principal tem comparação com o período anterior. As outras 12 métricas não dizem se melhoraram ou pioraram.
- Quem paga é o gestor (administrador) e o atendente de setor, que abrem a tela para decidir onde agir e saem sem saber. Evidência: nenhuma medida de uso (não há produção); a observação vem da leitura da tela e das práticas de painéis de suporte (ver Sources).

Depois disto, a tela se organiza em perguntas que o gestor faz, cada uma com o gráfico adequado ao tipo de dado: o assistente está resolvendo (barra empilhada de desfechos), quanto chega e como evolui (barras e linhas por dia), a equipe responde a tempo (mediana, 90º percentil e fila por estado), o que pesa (barras por categoria). Toda taxa mostra o denominador e a variação contra o período anterior, com sentido (melhor/pior) escrito.

## Flow

Reusa `Massa`, `Escopo` e `Ler` de `ConsultarIndicadores`, a consulta do período anterior (`useVariacao`), os formatos de `web/src/formatos.ts` e os tokens `--chart-1`/`--chart-2` do AD-037; não cria biblioteca de gráficos (o `recharts` saiu no AD-037).

1. `GET /api/indicadores/detalhe?de&ate&setorId` -> `ConsultarIndicadoresDetalhe` (new, door 1) - mesmas recusas de papel, liderança, período e setor de `ConsultarIndicadores` (exists)
2. `CalculoIndicadores` (exists) - ganha o cálculo da série por dia ou semana, dos desfechos em contagem, dos chamados por estado, da idade do mais antigo e da mediana e do 90º percentil; usa `TimeProvider` para "agora"
3. `Indicadores.tsx` (exists) - além da consulta atual e da do período anterior, faz a consulta de `detalhe` e monta as seções na ordem do `## Surface` de tela abaixo
4. gráficos em SVG e HTML escritos à mão (new, no door - placement por convenção) - barra empilhada, colunas, linhas, barras horizontais, cada um com título, rótulo direto, descrição de texto e "Ver como tabela"
5. out: tela com 5 seções; nada persiste

## Impact

| Front | What changes |
| --- | --- |
| domain | termo novo: `desfecho` em contagem (`resolvidas`, `transferidas`, `abandonadas`, `semDesfecho`) - hoje só existe como taxa; vive em `Indicadores` |
| domain | termo novo: `mediana` e `p90` (90º percentil) dos tempos - hoje a tela só mostra a média; a média continua na API e no MCP, e sai da tela |
| domain | termo novo: `maisAntigoDias` - dias desde a abertura do chamado mais antigo ainda em `ABERTO` ou `EM_ATENDIMENTO` |
| rota existente | `GET /api/indicadores` não muda de forma; `ver_indicadores` do MCP (`Mcp/FerramentasMcp.cs`, AD-055) continua com o mesmo JSON e `McpFerramentasTests` não muda |
| quem lê hoje | `Indicadores.tsx`, `ranking.test.tsx`, `identidade.test.tsx`, `indicadores.test.tsx`, `guidelinesFormatos.test.tsx` e os e2e `arranjo`, `casca`, `celular`, `guidelines`, `identidade`, `ux` leem textos da tela atual; os que codificam a lista de 9 pares viram as seções novas |
| stored data | nothing to migrate - tudo é calculado de `chamado`, `evento_chamado`, `conversa` e `mensagem_conversa` que já existem |

## Relations

`None - no stored-data shape change`

## Surface

| Route | In | Out | Status |
| --- | --- | --- | --- |
| `GET /api/indicadores/detalhe` | `de`, `ate` (datas), `setorId` (opcional) | `granularidade` · `pontos` · `desfechos` · `chamadosPorEstado` · `maisAntigoDias` · `tempos` | `200`, `400`, `401`, `403` |

Tela `/:slug/indicadores`, de cima para baixo (ordem decidida; o conteúdo exato vem dos critérios):

1. barra do período (existe)
2. **Resumo** - indicador principal, variação, denominador
3. **O assistente está resolvendo?** - barra empilhada de desfechos, perguntas sem artigo
4. **Quanto chega e como evolui?** - colunas de conversas por dia; linhas de chamados abertos e resolvidos por dia
5. **A equipe responde a tempo?** - mediana e 90º percentil dos dois tempos; chamados abertos agora por estado; o mais antigo
6. **Qualidade do atendimento** - roteamento correto e reabertura, com variação
7. **O que mais pesa** - categorias por conversas e por chamados

## Landing

| One-way door | Literal shape | Alternative rejected |
| --- | --- | --- |
| 1. contrato da rota nova | `GET /api/indicadores/detalhe` -> `{ "granularidade": "dia"\|"semana", "pontos": [{ "inicio": "2026-11-03", "conversas": 12\|null, "chamadosAbertos": 5, "chamadosResolvidos": 4 }], "desfechos": { "resolvidas": 80, "transferidas": 25, "abandonadas": 10, "semDesfecho": 5 }\|null, "chamadosPorEstado": { "ABERTO": 3, "EM_ATENDIMENTO": 6, "AGUARDANDO_ALUNO": 2, "RESOLVIDO": 4 }, "maisAntigoDias": 9\|null, "tempos": { "primeiraRespostaMedianaHoras": 1.5\|null, "primeiraRespostaP90Horas": 6\|null, "resolucaoMedianaHoras": 20\|null, "resolucaoP90Horas": 70\|null } }`; `granularidade` é `dia` até 62 dias de período e `semana` acima; `inicio` é a segunda-feira da semana no fuso da instituição; `desfechos` e `conversas` são `null` com `setorId` informado, como `conversas` já é hoje | acrescentar esses campos a `GET /api/indicadores`: o `ver_indicadores` do MCP serializa o mesmo `Resultado` e passaria a entregar campos novos a clientes externos, contra o AD-055 que fixa essa saída; fica como alternativa viva, se o dono preferir um endpoint só |
| 2. sem biblioteca de gráficos | gráficos como componentes em `web/src/indicadores/` com `<svg role="img" aria-label>` e HTML; nenhuma dependência nova em `web/package.json` | voltar ao `recharts`: AD-037 o removeu por peso e por desenho próprio; o custo é manter 4 desenhos à mão, aceito |

- Nada mais nesta mudança é difícil de desfazer. A ordem das seções, a paleta e os textos mudam por refatoração.

## Criteria

### S1: O assistente está resolvendo? (P1)

O gestor vê, sem somar de cabeça, como as conversas terminaram e com quantas conversas isso foi medido.

**Acceptance Criteria**

1. WHEN `GET /api/indicadores/detalhe` recebe um período com conversas THEN the system SHALL devolver `desfechos` com as contagens `resolvidas`, `transferidas`, `abandonadas` e `semDesfecho` cuja soma é o total de conversas criadas no período, no fuso da instituição
2. WHEN `setorId` é informado THEN the system SHALL devolver `desfechos` igual a `null`
3. WHEN o período tem conversas THEN the system SHALL mostrar na seção "O assistente está resolvendo?" uma única barra horizontal empilhada com três segmentos (Resolvida pelo assistente, Transferida a um atendente, Abandonada), cada um com rótulo direto "nome · contagem · percentual", e o percentual calculado sobre as conversas com desfecho
4. The system SHALL mostrar abaixo da barra o texto "de N conversas com desfecho" e, quando `semDesfecho` é maior que 0, "M ainda sem desfecho", sem incluí-las na barra
5. WHEN as conversas com desfecho são menos de 10 THEN the system SHALL mostrar o aviso "Amostra pequena: menos de 10 conversas" ao lado da barra
6. WHILE `desfechos` é `null` the system SHALL omitir a barra e mostrar "Desfechos do assistente aparecem na visão geral, sem filtro de setor."
7. The system SHALL usar para os três segmentos cores distintas por luminosidade e separá-los com uma linha de 2 px da cor do fundo, de modo que a barra continue legível em tons de cinza

### S2: Quanto chega e como evolui? (P1)

O gestor vê se a demanda sobe e se a equipe dá conta dela.

**Acceptance Criteria**

8. WHEN o período tem até 62 dias THEN the system SHALL devolver `granularidade` `dia` com um ponto por dia do período, incluindo dias sem atividade com contagens 0
9. WHEN o período tem mais de 62 dias THEN the system SHALL devolver `granularidade` `semana` com um ponto por semana que toca o período, com `inicio` na segunda-feira no fuso da instituição
10. The system SHALL contar em cada ponto `conversas` pela data de criação da conversa, `chamadosAbertos` pela data de criação do chamado e `chamadosResolvidos` pela data da transição para `RESOLVIDO`, todos no fuso da instituição e excluindo chamados fechados como duplicados, como os indicadores atuais
11. WHEN `setorId` é informado THEN the system SHALL devolver `conversas` igual a `null` em cada ponto
12. WHEN o período tem atividade THEN the system SHALL mostrar um gráfico de colunas "Conversas por dia" (ou "por semana") com o eixo vertical começando em 0, o valor máximo e o dia de maior volume rotulados
13. WHEN o período tem atividade THEN the system SHALL mostrar um gráfico de duas linhas "Chamados abertos e resolvidos por dia" (ou "por semana"), uma contínua e uma tracejada, cada uma com o rótulo no fim da linha, para que a diferença entre as duas indique se a fila cresce
14. The system SHALL oferecer em cada gráfico de série e de barras um botão "Ver como tabela" que mostra os mesmos valores em uma tabela com cabeçalhos, e o gráfico SHALL ter `role="img"` com `aria-label` que diz o tipo, o período, o total e o pico
15. WHEN o gráfico recebe foco do ponteiro ou do teclado sobre um ponto THEN the system SHALL mostrar o rótulo do dia ou da semana e o valor de cada série em um texto visível de 12 px ou mais

### S3: A equipe responde a tempo? (P1)

O gestor vê o aluno que espera mais, e não só o aluno médio, e onde a fila está parada.

**Acceptance Criteria**

16. WHEN o período tem chamados com primeira resposta THEN the system SHALL devolver `primeiraRespostaMedianaHoras` e `primeiraRespostaP90Horas` calculadas sobre os mesmos chamados da média atual, com 2 casas decimais
17. WHEN o período tem chamados com resolução THEN the system SHALL devolver `resolucaoMedianaHoras` e `resolucaoP90Horas` sobre os mesmos chamados e com o mesmo desconto de "aguardando aluno" que a média atual usa
18. IF não há chamados com primeira resposta ou com resolução no período THEN the system SHALL devolver os quatro campos de tempo correspondentes como `null`
19. The system SHALL devolver `chamadosPorEstado` com a contagem agora dos chamados não fechados em cada um dos estados `ABERTO`, `EM_ATENDIMENTO`, `AGUARDANDO_ALUNO` e `RESOLVIDO`, somando o mesmo total de `chamadosAbertos`
20. The system SHALL devolver `maisAntigoDias` com os dias inteiros desde a criação do chamado mais antigo em `ABERTO` ou `EM_ATENDIMENTO`, usando `TimeProvider`, e `null` quando não há nenhum
21. WHEN `tempos` tem valor THEN the system SHALL mostrar cada tempo como "mediana X h" em destaque e "9 em 10 foram atendidos em até Y h" abaixo, em vez da média
22. The system SHALL mostrar os chamados abertos agora como uma barra empilhada pelos quatro estados, com rótulo direto por estado e contagem, e a frase "O mais antigo espera há N dias" quando `maisAntigoDias` não é `null`
23. WHEN `chamadosAbertos` é 0 THEN the system SHALL mostrar "Nenhum chamado aberto agora." no lugar da barra

### S4: Variação, denominador e leitura (P2)

Cada número diz se melhorou, se piorou e com quantos casos foi medido.

**Acceptance Criteria**

24. WHEN o período anterior tem dados e o atual tem dados THEN the system SHALL mostrar em cada taxa (indicador principal, autoatendimento, escalonamento, abandono, perguntas sem artigo, roteamento correto, reabertura) a variação em pontos percentuais com seta e a palavra "melhor" ou "pior", e "sem variação" quando for 0
25. The system SHALL tratar como melhor um aumento de indicador principal, autoatendimento e roteamento correto, e como melhor uma queda de escalonamento, abandono, perguntas sem artigo e reabertura
26. IF o período anterior não tem dados, ou a taxa é `null` em um dos períodos, ou os chamados e conversas por trás têm menos de 10 casos THEN the system SHALL omitir a variação desse cartão
27. WHEN o período tem dados THEN the system SHALL mostrar a seção "Resumo" com o indicador principal, a variação, o texto "de N conversas e chamados com desfecho" e a definição atual "Resolvidas sem reabertura, sobre as conversas com desfecho"
28. The system SHALL nunca indicar melhor ou pior apenas por cor: toda variação traz seta e palavra, e todo segmento de gráfico traz rótulo de texto
29. The system SHALL mostrar as categorias por conversas e por chamados como barras horizontais ordenadas, com o valor escrito ao fim de cada barra e o eixo começando em 0, e "—" quando a lista é vazia
30. The system SHALL usar apenas barra empilhada, colunas, linhas e barras horizontais e nenhum gráfico de pizza, rosca, medidor ou 3D

### S5: Estados e acessibilidade da tela (P1)

**Acceptance Criteria**

31. WHEN o período não tem atendimentos THEN the system SHALL manter "Sem atendimentos neste período." e nenhuma seção nova
32. WHILE a consulta de `detalhe` está pendente the system SHALL mostrar em cada seção nova um espaço da altura final com o texto "Carregando…" para a página não pular
33. IF a consulta de `detalhe` falha THEN the system SHALL mostrar o erro com "Tentar de novo" só nas seções novas e manter o resumo, a variação e os rankings da consulta principal
34. IF o papel é aluno ou liderança de turma, ou o `setorId` não é do atendente THEN `GET /api/indicadores/detalhe` SHALL responder `403` com ProblemDetails, e sem período `400`
35. The system SHALL desenhar toda marca de gráfico com contraste mínimo de 3:1 contra o fundo do tema ativo, nos 4 temas, e todo texto de gráfico com 4,5:1 (AD-035 manda contar os gráficos nesta rodada)
36. WHILE a tela tem menos de 768 px de largura the system SHALL empilhar as seções em uma coluna, mostrar os gráficos na largura inteira com altura mínima de 160 px, e manter o rótulo direto sem sobrepor outro texto

**Independent test:** abrir `/univag/indicadores` com o período do mês, ver as 5 seções na ordem acima com os dados do e2e, e repetir com um período de 90 dias (semanas) e com `setorId`.

## Out of scope

| Excluded | Why |
| --- | --- |
| meta (SLA) configurável por instituição e gráfico de bala contra a meta | não existe meta no produto; criá-la é tabela, tela de configuração e decisão de produto. Sem meta, o gráfico de bala não tem o que mostrar |
| satisfação (CSAT) e nota do aluno | o produto não coleta nota hoje |
| desempenho por atendente | não pedido, e carga por atendente sem contexto leva a comparação injusta |
| exportar CSV ou PDF e agendar relatório | capacidade nova, fica para outro plano |
| clicar no gráfico e abrir a lista de chamados | exige filtros novos na fila; o gráfico mostra o número e a fila segue em outra tela |
| filtro de setor e período em ranges rápidos (7, 30, 90 dias) | a barra de período atual segue; atalhos podem vir depois |
| tempo real na tela | a tela segue sendo consultada por período |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| uma rota nova em vez de campos novos na rota atual | `GET /api/indicadores/detalhe` (door 1) | mantém a saída do MCP (AD-055) e o `Resultado` atual; custo: 1 consulta a mais por tela | n |
| a média sai da tela | mediana em destaque e 90º percentil abaixo; a média continua na API e no MCP | a média esconde a espera longa; painéis de suporte pareiam mediana e cauda | n |
| mínimo de casos | 10 para exibir variação e para tirar o aviso de amostra pequena | evita ▲ em 2 casos; sem estudo da base real da Univag | n |
| semana começa na segunda | segunda-feira no fuso da instituição | convenção brasileira de calendário letivo | n |
| 62 dias é o limite entre dia e semana | até 62 dias, dia; acima, semana | 62 pontos cabem em 360 px com colunas de 4 px; passando disso viram ruído | n |
| gráficos sem biblioteca | SVG e HTML à mão | AD-037 | n |
| `maisAntigoDias` ignora `AGUARDANDO_ALUNO` | só conta chamados que esperam a equipe | espera do aluno não é falha da equipe | n |
| "melhor/pior" depende da métrica | tabela do AC 25 | direção é regra de domínio, não gosto visual | n |

**Open questions:** none - all resolved or logged above.

## Observable

| Surface | Decision | Landing |
| --- | --- | --- |
| screen `Indicadores` | empty state | AC 31 |
| screen `Indicadores` | loading state | AC 32 |
| screen `Indicadores` | error state | AC 33 |
| screen `Indicadores` | unauthorised state | existing - `ConsultarIndicadores` devolve 403 e a rota só aparece na barra para ADMINISTRADOR e ATENDENTE; AC 34 para a rota nova |
| screen `Indicadores` | density and ordering | AC 27, AC 29 e a ordem das seções no `## Surface` |
| screen `Indicadores` | destructive action confirms | n/a - a tela só lê |
| screen `Indicadores` celular 360×780 | layout | AC 36 |
| screen `Indicadores` celular deitado 844×390 | layout | AC 36: com `main` de 768 px ou mais as seções 3 e 4 ficam em duas colunas; a altura de 390 px rola na vertical sem cortar rótulos |
| screen `Indicadores` teclado aberto | layout | n/a - a única entrada são dois campos de data no topo; o teclado do seletor de data cobre só o formulário e a tela já rola |
| screen `Indicadores` toque | alvos | AC 14 e AC 15: "Ver como tabela" tem 44 px de altura no celular; o valor do ponto aparece no toque sem exigir pairar |
| screen `Indicadores` | acessibilidade de gráfico | AC 7, 14, 28, 35 |
| API `GET /api/indicadores/detalhe` | error shape and codes | AC 34 (ProblemDetails de `Problemas.Erro`, `400` e `403`); `401` sem sessão como as outras rotas |
| API `GET /api/indicadores/detalhe` | who may call | AC 34 |
| API `GET /api/indicadores/detalhe` | versioning, rate limits | n/a - a API não versiona rotas e nenhuma rota de leitura tem limite de taxa hoje |
| API `GET /api/indicadores/detalhe` | response shape | door 1 |

### As nove dimensões (resumo; o registro com checks vai em `checks.md` `## Swept`)

| Dimension | Landing |
| --- | --- |
| Validation and bounds | AC 34 (período e setor), AC 8 e 9 (limite de pontos: no máximo 62 dias por dia e ~260 semanas em 5 anos) |
| Failure and partial failure | AC 33 |
| Idempotency, retry, duplicates | n/a - rota de leitura; AC 10 exclui duplicados |
| Authorization and rate limits | AC 34; limite de taxa n/a, nenhuma rota de leitura o tem |
| Concurrency and ordering | n/a - leitura sem estado; `OrderBy(CriadoEm, Id)` já ordena os eventos |
| Data lifecycle | n/a - nada novo é guardado |
| External-dependency failure | n/a - o cálculo só usa o PostgreSQL |
| State transitions | AC 19 e 20 (estados dos chamados) |
| Observability | n/a - a API não registra métricas de consulta hoje; a tela fica sem telemetria nova |

## Sources

- `.specs/STATE.md` AD-037 (rankings em HTML, `recharts` sai), AD-035 (contraste 3:1 dos gráficos entra nesta rodada), AD-055 (saída do MCP `ver_indicadores`), AD-034 (fonte e tokens)
- Supportman, [Seven metrics every customer support dashboard should show](https://supportman.io/field-notes/customer-support-dashboard-metrics/): primeira tela como painel de exceções, mediana e 90º percentil, denominador e definição em cada cartão, número mínimo de casos
- Stephen Few, [Bullet Graph Design Specification](https://www.perceptualedge.com/articles/misc/Bullet_Graph_Design_Spec.pdf), e [Tableau, bullet graphs vs gauges](https://www.tableau.com/blog/bullet-graphs-beat-gauge-charts): medidor ocupa espaço sem mostrar histórico; sem meta não há gráfico de bala, por isso fica fora de escopo
- [Accessible charts (WCAG 2.2)](https://accessibility.build/guides/accessible-charts): 1.1.1, 1.4.1 e 1.4.11 viram os AC 14, 28 e 35
