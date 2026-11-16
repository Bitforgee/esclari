# Indicadores claros para o gestor - checks

Profile: ui
Plan: `.specs/features/indicadores-claros/plan.md`

36 checks em 5 slices · 2 one-way doors · 0 perguntas abertas (plano aprovado pelo usuário em 16/11: "siga com o que for recomendado eu confio em você"; recomendações aceitas: rota nova e mediana no lugar da média)

**Como as provas rodam.** Na raiz `D:\esclari`, salvo `cd web &&`. API: `docker compose up -d postgres mailpit minio` e uma execução de `dotnet test` por vez. Vitest: `cd web && npx vitest run <arquivo> -t "<nome>"`. Um filtro que não seleciona teste sai com 0 (L-002): conferir a contagem. Mutação suspensa (AD-025).

**Emenda de construção (Landing, aditiva).** A conversa tem um quinto desfecho, `ENCERRADA` (o aluno fecha sem avaliar), que entra no denominador das taxas atuais. Para a soma de `desfechos` ser o total de conversas, a porta 1 ganha a chave `encerradas` ao lado das quatro do plano, e a barra do AC 3 ganha um quarto segmento neutro "Encerrada pelo aluno". Os percentuais continuam sobre as conversas com desfecho, como as taxas de hoje.

## Checks

### S1 - O assistente está resolvendo? · 5 arquivos · ~25k

**C1** - `desfechos` traz `resolvidas`, `transferidas`, `abandonadas`, `encerradas` e `semDesfecho`, e a soma é o total de conversas criadas no período (AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~IndicadoresDetalheTests.Desfechos_somam_o_total_de_conversas"`

**C2** - Com `setorId`, `desfechos` é `null` e `conversas` é `null` em todo ponto (AC 2, AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~IndicadoresDetalheTests.Com_setor_desfechos_e_conversas_sao_nulos"`

**C3** - A barra de desfechos tem 4 segmentos com rótulo "nome · contagem · percentual" (80 de 115 com desfecho = 69,6 %), calculado sobre as conversas com desfecho (AC 3)
Proof: `cd web && npx vitest run src/indicadores/desfechos.test.tsx -t "barra com rotulo direto"`

**C4** - Mostra "de 115 conversas com desfecho" e "5 ainda sem desfecho", sem incluir as 5 na barra; sem `semDesfecho` omite a segunda frase (AC 4)
Proof: `cd web && npx vitest run src/indicadores/desfechos.test.tsx -t "denominador"`

**C5** - Com 9 conversas com desfecho aparece "Amostra pequena: menos de 10 conversas"; com 10 não aparece (AC 5)
Proof: `cd web && npx vitest run src/indicadores/desfechos.test.tsx -t "amostra pequena"`

**C6** - Com `desfechos` `null` não há barra e aparece "Desfechos do assistente aparecem na visão geral, sem filtro de setor." (AC 6)
Proof: `cd web && npx vitest run src/indicadores/desfechos.test.tsx -t "sem filtro de setor"`

**C7** - Cada segmento tem um tom de luminosidade diferente (3 tons distintos entre os 4 segmentos) e uma linha separadora de 2 px; os tons em cinza diferem em pelo menos 10 pontos de luminosidade (AC 7)
Proof: `cd web && npx vitest run src/indicadores/contraste.test.ts -t "tons de cinza distintos"`
Proof: `cd web && npx vitest run src/indicadores/desfechos.test.tsx -t "separador de 2 px"`

### S2 - Quanto chega e como evolui? · 6 arquivos · ~35k

**C8** - Período de 3 dias devolve `granularidade` `dia` com 3 pontos, e o dia sem atividade vem com 0 (AC 8)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CalculoDetalheTests.Ate_62_dias_um_ponto_por_dia"`

**C9** - O limite: 62 dias dá `dia` (62 pontos) e 63 dias dá `semana` (AC 8, AC 9)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CalculoDetalheTests.Limite_de_62_dias"`

**C10** - Em `semana`, cada `inicio` é segunda-feira no fuso `America/Cuiaba` e a semana que toca o período entra inteira como ponto, contando só o que está dentro do período (AC 9)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CalculoDetalheTests.Semana_comeca_na_segunda_no_fuso"`

**C11** - Um evento às 23:30 de 10/11 em Cuiabá (03:30 UTC de 11/11) cai no ponto 10/11 (AC 10)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CalculoDetalheTests.Ponto_usa_o_fuso_da_instituicao"`

**C12** - `conversas` conta pela criação da conversa, `chamadosAbertos` pela criação do chamado, `chamadosResolvidos` pela transição para `RESOLVIDO`, duplicado fica de fora, e a soma de `chamadosResolvidos` na série é igual a `chamadosResolvidos` de `GET /api/indicadores` (AC 10)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~IndicadoresDetalheTests.Serie_conta_cada_coisa_pela_sua_data"`

**C13** - O gráfico de colunas "Conversas por dia" tem o eixo vertical em 0, o valor máximo e o dia de maior volume rotulados em texto, e uma coluna por ponto (AC 12)
Proof: `cd web && npx vitest run src/indicadores/series.test.tsx -t "colunas de conversas"`

**C14** - O gráfico de linhas "Chamados abertos e resolvidos por dia" tem 2 linhas, uma contínua e uma com `stroke-dasharray`, e o rótulo no fim de cada uma; em `semana` o título diz "por semana" (AC 13)
Proof: `cd web && npx vitest run src/indicadores/series.test.tsx -t "duas linhas"`

**C15** - Todo gráfico de série e de barras tem `role="img"` com `aria-label` com tipo, período, total e pico, e o botão "Ver como tabela" mostra a tabela com cabeçalhos e os mesmos valores (AC 14)
Proof: `cd web && npx vitest run src/indicadores/series.test.tsx -t "ver como tabela"`

**C16** - Foco do teclado e toque em um ponto mostram "dia, valor de cada série" em texto de 12 px ou mais (AC 15)
Proof: `cd web && npx vitest run src/indicadores/series.test.tsx -t "valor do ponto"`

### S3 - A equipe responde a tempo? · 5 arquivos · ~30k

**C17** - Para respostas de 1, 2, 3 e 10 horas: mediana 2,5 e p90 10 (posto mais próximo, índice ceil(0,9·n)−1) (AC 16)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CalculoDetalheTests.Mediana_e_p90_da_primeira_resposta"`

**C18** - A resolução usa o mesmo desconto de "aguardando aluno" da média atual: um chamado de 10 h com 4 h aguardando conta 6 h (AC 17)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CalculoDetalheTests.Resolucao_desconta_aguardando_aluno"`

**C19** - Sem chamado com resposta ou resolução no período, os 4 campos de `tempos` são `null` (AC 18)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CalculoDetalheTests.Sem_chamados_tempos_nulos"`

**C20** - `chamadosPorEstado` traz as 4 chaves e a soma é igual a `chamadosAbertos` de `/api/indicadores`; `FECHADO` não entra (AC 19)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~IndicadoresDetalheTests.Estados_somam_os_chamados_abertos"`

**C21** - `maisAntigoDias` é 9 para um chamado `ABERTO` criado 9 dias e 3 h antes de "agora" (`FakeTimeProvider`), ignora `AGUARDANDO_ALUNO` e `RESOLVIDO`, e é `null` sem nenhum (AC 20)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CalculoDetalheTests.Mais_antigo_conta_so_o_que_espera_a_equipe"`

**C22** - A tela mostra "mediana 2,5 h" em destaque e "9 em 10 foram atendidos em até 10 h" abaixo, e não mostra a média (AC 21)
Proof: `cd web && npx vitest run src/indicadores/fila.test.tsx -t "mediana e p90"`

**C23** - Os chamados abertos agora saem em uma barra empilhada pelos 4 estados, com rótulo e contagem por estado, e "O mais antigo espera há 9 dias" (AC 22)
Proof: `cd web && npx vitest run src/indicadores/fila.test.tsx -t "fila por estado"`

**C24** - Com `chamadosAbertos` 0 aparece "Nenhum chamado aberto agora." e nenhuma barra (AC 23)
Proof: `cd web && npx vitest run src/indicadores/fila.test.tsx -t "nenhum chamado aberto"`

### S4 - Variação, denominador e leitura · 4 arquivos · ~25k

**C25** - Cada uma das 7 taxas mostra a variação com seta, valor em pontos e "melhor" ou "pior", e "sem variação" quando 0 (AC 24)
Proof: `cd web && npx vitest run src/indicadores/variacao.test.tsx -t "variacao nas sete taxas"`

**C26** - A direção: subir indicador principal, autoatendimento e roteamento correto é "melhor"; subir escalonamento, abandono, perguntas sem artigo e reabertura é "pior" (AC 25)
Proof: `cd web && npx vitest run src/indicadores/variacao.test.tsx -t "direcao de cada taxa"`

**C27** - Sem variação quando o período anterior é `semDados`, quando a taxa é `null` em um dos períodos, ou quando há menos de 10 casos (9 omite, 10 mostra) (AC 26)
Proof: `cd web && npx vitest run src/indicadores/variacao.test.tsx -t "omite a variacao"`

**C28** - O Resumo mostra o indicador principal, a variação, "de 115 conversas e chamados com desfecho" e a definição "Resolvidas sem reabertura, sobre as conversas com desfecho." (AC 27)
Proof: `cd web && npx vitest run src/indicadores/resumo.test.tsx -t "resumo com denominador"`

**C29** - Nenhuma variação nem segmento depende só de cor: toda variação tem seta e palavra, todo segmento tem texto (AC 28)
Proof: `cd web && npx vitest run src/indicadores/variacao.test.tsx -t "nunca so por cor"`

**C30** - Os rankings saem como barras horizontais com o valor escrito ao fim, ordenadas, com a largura da maior em 100 % e "—" para lista vazia (AC 29)
Proof: `cd web && npx vitest run src/indicadores/ranking.test.tsx -t "barras com valor escrito"`

**C31** - A pasta `web/src/indicadores` não tem pizza, rosca, medidor nem 3D, e `web/package.json` não ganha dependência (AC 30, door 2)
Proof: `node -e "const fs=require('fs');const p=require('path');const w=d=>fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?w(p.join(d,e.name)):[p.join(d,e.name)]);const t=w('web/src/indicadores').filter(f=>!/test/.test(f)).map(f=>fs.readFileSync(f,'utf8')).join('');if(/PieChart|donut|rosca|gauge|medidor|recharts|d3-/i.test(t))process.exit(1)"`
Proof: `git diff --exit-code 8190316 -- web/package.json`

### S5 - Estados e acessibilidade · 6 arquivos · ~35k

**C32** - Período sem atendimento mantém "Sem atendimentos neste período." e nenhuma seção nova (AC 31)
Proof: `cd web && npx vitest run src/indicadores/estados.test.tsx -t "sem atendimentos"`

**C33** - Com `detalhe` pendente cada seção nova ocupa a altura final (`min-h`) com "Carregando…" (AC 32)
Proof: `cd web && npx vitest run src/indicadores/estados.test.tsx -t "carregando"`

**C34** - Com `detalhe` em 500, as seções novas mostram o erro e "Tentar de novo", e o Resumo, a variação e os rankings continuam (AC 33)
Proof: `cd web && npx vitest run src/indicadores/estados.test.tsx -t "erro so no detalhe"`

**C35** - `GET /api/indicadores/detalhe`: aluno `403`, liderança de turma `403`, atendente com setor alheio `403`, sem `de`/`ate` `400`, sem sessão `401`, e o JSON de sucesso tem as chaves literais da porta 1 (AC 34)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~IndicadoresDetalheTests.Quem_pode_e_o_formato_da_resposta"`

**C36** - Toda marca de gráfico tem contraste mínimo de 3:1 e todo texto 4,5:1 contra o fundo, nos 4 temas (Claro, Escuro, Preto, e o Claro com cor de marca no limite de 4,5:1) (AC 35)
Proof: `cd web && npx vitest run src/indicadores/contraste.test.ts -t "contraste de graficos"`

**C37** - No celular 360×780 e 844×390, a tela não rola na horizontal, nenhum texto fica cortado e cada gráfico tem ao menos 160 px de altura (AC 36)
Proof: `cd web && npx playwright test e2e/celular.spec.ts -g "Indicadores claros"`

**C38** - As suítes e os gates seguem verdes: API inteira, Vitest inteiro, build com tipos, `crapper` e `dryer` (AD-018, Impact)
Proof: `dotnet test Esclari.Api.Tests`
Proof: `cd web && npx vitest run`
Proof: `npm --prefix web run build`
Proof: `npm --prefix web run coverage && cd web && crapper --use-existing-coverage -s src --threshold 9.99`
Proof: `cd web && dryer -s src/acesso -s src/assistente -s src/chamados -s src/conhecimento -s src/indicadores -s src/administracao --threshold 0.8 src/App.tsx src/Autenticado.tsx src/api.ts src/chamado.ts src/datas.ts src/sessao.ts`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| `GET /api/indicadores/detalhe` statuses (4) | 200 C35 · 400 C35 · 401 C35 · 403 C35 | - |
| `desfechos` chaves (5) | resolvidas C1 · transferidas C1 · abandonadas C1 · encerradas C1 · semDesfecho C1 | - |
| segmentos da barra de desfechos (4) | resolvida C3 · transferida C3 · abandonada C3 · encerrada C3 | - |
| estados do chamado em `chamadosPorEstado` (4) | ABERTO C20 · EM_ATENDIMENTO C20 · AGUARDANDO_ALUNO C20 · RESOLVIDO C20 | - |
| estados da barra da fila (4) | ABERTO C23 · EM_ATENDIMENTO C23 · AGUARDANDO_ALUNO C23 · RESOLVIDO C23 | - |
| `granularidade` (2) | dia C8 · semana C10 | - |
| limite 62 dias (2 bordas) | 62 C9 · 63 C9 | - |
| campos de `tempos` (4) | primeiraRespostaMedianaHoras C17 · primeiraRespostaP90Horas C17 · resolucaoMedianaHoras C18 · resolucaoP90Horas C18 | - |
| taxas com variação (7) | indicador principal C25 · autoatendimento C25 · escalonamento C25 · abandono C25 · perguntas sem artigo C25 · roteamento correto C25 · reabertura C25 | - |
| direção da taxa (7) | indicador principal C26 · autoatendimento C26 · roteamento correto C26 · escalonamento C26 · abandono C26 · perguntas sem artigo C26 · reabertura C26 | - |
| omissão da variação (4) | período anterior sem dados C27 · taxa nula C27 · 9 casos C27 · 10 casos C27 | - |
| gráficos (5) | barra de desfechos C3 · colunas C13 · linhas C14 · barra da fila C23 · barras de categoria C30 | - |
| estados da tela (4) | vazio C32 · carregando C33 · erro C34 · não autorizado C35 | - |
| temas para contraste (4) | claro C36 · escuro C36 · preto C36 · claro com marca no limite C36 | - |
| viewports (2) | 360×780 C37 · 844×390 C37 | - |
| portas (2) | 1 C35 · 2 C31 | - |
| fusos (2 bordas) | 23:30 local do dia C11 · virada de semana C10 | - |

- Afirmam status, rota ou formato: C1, C2, C20, C35, todos com prova que passa pela rota HTTP
- C8 a C11 e C17 a C19 decidem em `CalculoDetalhe`: prova na própria camada; C12 e C35 provam a mesma lógica pela rota
- Nenhuma outra afirma mais que o caso que a prova exercita

## Test policy

| Code | Required proofs | Coverage expectation |
| --- | --- | --- |
| Decide, atravessa a rota HTTP (`ConsultarIndicadoresDetalhe`) | uma na rota e uma na própria camada | contrato e papéis na rota; uma linha por caso das tabelas na camada |
| Decide, na tela (variação, limite de 10, formato dos tempos) | uma na própria camada | um caso por linha das tabelas dos AC 24 a 26 |
| Desenho (SVG) sem condicional | uma de renderização | rótulos, `role`, `aria-label` e valores |

Evidence:

- `CalculoDetalhe` (novo): série (2 granularidades, 3 contagens), percentil, estados, idade: 9 pontos de decisão -> decide
- `Indicadores.tsx` hoje: `variacao` com 3 ramos -> decide; os gráficos só mapeiam número em coordenada, e o caso sem dado é uma condicional -> instrumentação com 1 ramo
- análogo no repo: `CalculoIndicadores` + `IndicadoresCalculoTests.cs`, mesma forma, provado na própria camada

Cost: 7 testes na camada em 1 arquivo novo. Sem estas linhas, as bordas de fuso e de 62 dias ficariam provadas só por um caminho que as atravessa.

## Swept

- validation: C35, C9
- failure modes: C34
- idempotency: n/a - rota de leitura; C12 exclui duplicados
- authorization: C35
- concurrency: n/a - leitura sem estado
- data lifecycle: n/a - nada novo é guardado
- dependency failure: n/a - só o PostgreSQL
- state transitions: C20, C21
- observability: n/a - sem telemetria de consulta hoje (decidido no plano)

## Handoff

- S1 a S5 somam cerca de 150k estimados (arquivos tocados: `Indicadores.tsx` 11 KB, `CalculoIndicadores.cs` 6 KB, `ConsultarIndicadores.cs` 5 KB, 5 testes web ~30 KB, 2 testes API ~20 KB, 6 componentes novos ~30 KB, e2e e `simulada.ts` ~25 KB => ~130 KB ≈ 33k tokens de leitura mais escrita). Abaixo do limite de 150k: um construtor
- Mechanism: one builder

- Base da feature: `8190316` ($b..HEAD é o que o Verificador lê)

- **Boundary:** C1-C38 construídos; último commit de código `cebd19d`; base `8190316`
- **Settled mid-build:** (1) emenda da porta 1 já descrita no topo (ncerradas, quarto segmento). (2) O texto de C28 e AC 27 sai "de 115 conversas com desfecho" e não "conversas e chamados", porque o denominador do indicador principal no cálculo existente (CalculoIndicadores) é só conversas com desfecho; escrever "e chamados" seria falso. (3) C37 foi executado contra o servidor Vite com a API simulada de `web/e2e/simulada.ts` (`npx playwright test` com `baseURL` do Vite), porque o e2e padrão exige `docker compose up --build`. (4) `crapper` sai 1 por `useModo` (CRAP 11, `Autenticado.tsx`) e `Usuarios` (10), ambos intocados por esta feature e anteriores a ela; nenhuma função de `indicadores/` passa de 9,2.
- **Abandoned:** recharts (AD-037); campos novos em `/api/indicadores` (porta 1, alternativa rejeitada)
