# Prazos de atendimento, estouro e acompanhamento da gestão - checks

Profile: ui
Plan: `.specs/features/prazos-de-atendimento/plan.md`

53 checks em 5 slices · 3 one-way doors · 0 perguntas abertas (plano aprovado pelo dono em 17/11/2026: "siga com o que for recomendado", "aprovo"; AD-068)

**Como as provas rodam.** Na raiz `D:\esclari`, salvo `cd web &&`. API: `docker compose up -d postgres mailpit minio` e uma execução de `dotnet test` por vez. Vitest: `cd web && npx vitest run <arquivo> -t "<nome>"`. Um filtro que não seleciona teste sai com 0 (L-002): conferir a contagem de testes executados. `mutator` está suspenso (AD-025). Os nomes de teste abaixo são o contrato: os testes são escritos a partir dos checks, antes da implementação. O relógio dos testes é `FakeTimeProvider`; as metas dos exemplos são 24 h e 120 h, salvo onde o check diz outra coisa.

## Checks

### S1 - O aluno sabe quando esperar · 6 arquivos · ~60 KB · ~15k

**C1** - `POST /api/conversas/{id}/transferir` responde `200` com `prazoPrimeiraRespostaHoras` = 24 além de `numero`, `estado` e `setor`; com `Prazos:PrimeiraRespostaHoras` = 8 devolve 8 (AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosAtendimentoTests.Transferir_devolve_o_prazo_da_primeira_resposta"`

**C2** - A tela de confirmação da transferência mostra "A equipe responde em até 24 horas" (AC 1)
Proof: `cd web && npx vitest run src/assistente/prazo.test.tsx -t "confirmacao com prazo"`

**C3** - O detalhe de um chamado `ABERTO` criado às `2026-11-17T12:00Z` e sem resposta da equipe traz `prazoPrimeiraRespostaEm` = `2026-11-18T12:00:00Z` para o aluno dono (AC 2)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosAtendimentoTests.Detalhe_do_aluno_traz_a_previsao"`

**C4** - A tela do aluno mostra "Resposta prevista até 18/11 às 08:00" quando o fuso da instituição é UTC-4 e o prazo é `2026-11-18T12:00Z` (AC 2)
Proof: `cd web && npx vitest run src/chamados/prazoDoAluno.test.tsx -t "previsao no fuso da instituicao"`

**C5** - `prazoPrimeiraRespostaEm` é nulo quando a equipe já escreveu mensagem pública e nos estados `AGUARDANDO_ALUNO`, `RESOLVIDO` e `FECHADO` (AC 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosAtendimentoTests.Previsao_some_depois_da_resposta_e_fora_dos_estados_abertos"`

**C6** - Com `prazoPrimeiraRespostaEm` nulo a tela do aluno não mostra a frase "Resposta prevista" (AC 3)
Proof: `cd web && npx vitest run src/chamados/prazoDoAluno.test.tsx -t "sem previsao"`

**C7** - O detalhe do chamado de outra aluna responde `404` e a resposta não contém `prazoPrimeiraRespostaEm` (AC 4)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosAtendimentoTests.Chamado_de_outro_aluno_responde_404_sem_prazo"`

### S2 - A equipe vê o que está atrasado · 12 arquivos · ~110 KB · ~28k

**C8** - Chamado `ABERTO` sem resposta da equipe é `PRIMEIRA_RESPOSTA` com 24 h exatas e não é com 23 h 59 min (AC 5, fronteira)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AtrasoDoChamadoTests.Primeira_resposta_vence_em_24_horas_exatas"`

**C9** - Nota interna e mensagem do próprio solicitante não contam como resposta da equipe: o chamado continua `PRIMEIRA_RESPOSTA` depois delas, e deixa de ser depois de uma mensagem pública de atendente (AC 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AtrasoDoChamadoTests.So_mensagem_publica_da_equipe_conta_como_resposta"`

**C10** - Chamado criado em `t0` que ficou 30 h em `AGUARDANDO_ALUNO` e já teve resposta da equipe é `RESOLUCAO` a partir de `t0` + 150 h e não é com `t0` + 149 h 59 min (AC 6, pausa)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AtrasoDoChamadoTests.Resolucao_pausa_em_aguardando_aluno"`

**C11** - Chamado em `AGUARDANDO_ALUNO`, `RESOLVIDO` e `FECHADO` nunca tem `atraso`, mesmo 1000 h depois (AC 6)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AtrasoDoChamadoTests.Estados_fora_do_atendimento_nunca_atrasam"`

**C12** - Chamado com os dois atrasos devolve `atraso` = `PRIMEIRA_RESPOSTA` (AC 7)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AtrasoDoChamadoTests.Os_dois_atrasos_devolvem_primeira_resposta"`

**C13** - `GET /api/chamados/busca` devolve `atraso` em cada item, `null` quando em dia, e as rotas `GET /api/chamados?escopo=fila|meus` não ganham o campo (AC 8, AD-055)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Itens_trazem_atraso"`

**C14** - As listas mostram "Atrasado: 1ª resposta" e "Atrasado: resolução" como texto, com ícone, e nada na linha de um chamado em dia (AC 8)
Proof: `cd web && npx vitest run src/chamados/atraso.test.tsx -t "etiqueta de atraso"`

**C15** - `atraso=true` lista só os 2 chamados atrasados de 5, `atraso=false` lista os 3 em dia, e sem o parâmetro lista os 5 (AC 9)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Filtro_atraso"`

**C16** - Ligar o filtro de atraso na tela mostra "Filtros (1)", e o link `/univag/chamados?atraso=true` abre a lista já filtrada (AC 9)
Proof: `cd web && npx vitest run src/chamados/atraso.test.tsx -t "filtro de atraso"`

**C17** - `atraso=talvez` e `atraso=` vazio respondem `400` com ProblemDetails do campo `atraso`, e `true` e `false` respondem `200` (AC 10)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Atraso_invalido_responde_400"`

**C18** - O CSV de `GET /api/chamados/exportar` com `atraso=true` exporta só os atrasados, com as mesmas 16 colunas (Out of scope: o CSV respeita o filtro)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CsvDeChamadosTests.Exporta_so_os_atrasados"`

### S3 - O estouro aciona alguém · 8 arquivos · ~45 KB · ~12k

**C19** - Chamado em atraso com responsável e `atraso_avisado_em` nulo gera 1 e-mail para o responsável com protocolo, título, tipo e tempo de atraso e link, e grava `atraso_avisado_em` = agora (AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Primeiro_estouro_avisa_o_responsavel"`

**C20** - Sem responsável, os 2 atendentes ativos do setor recebem 1 e-mail cada, e o atendente inativo e o de outro setor não recebem (AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Sem_responsavel_avisa_os_atendentes_do_setor"`

**C21** - Cada `ADMINISTRADOR` ativo recebe 1 e-mail com assunto `[Univag] 3 chamados em atraso` listando os 3 chamados, e a marca dos 3 é atualizada (AC 12)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Resumo_ao_administrador"`

**C22** - Uma segunda execução 23 h 59 min depois não enfileira e-mail e não altera nenhuma marca (AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Antes_de_24_horas_nao_repete"`

**C23** - Uma execução 24 h depois reenvia o resumo só ao administrador, com o chamado ainda atrasado, e não reenvia ao responsável nem ao setor (AC 12, AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Depois_de_24_horas_so_o_administrador"`

**C24** - A primeira execução sobre 40 chamados já atrasados de 3 atendentes gera 1 e-mail por atendente e 1 por administrador, 4 no total, nunca 40 (AC 14)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Primeira_execucao_agrupa_por_destinatario"`

**C25** - Sem destinatário ativo com e-mail nenhuma marca é gravada, há 1 log de aviso com o id da instituição, e a execução seguinte, depois de existir destinatário, avisa (AC 15)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Sem_destinatario_nao_grava_marca"`

**C26** - Duas execuções concorrentes sobre o mesmo chamado deixam 1 marca e 1 e-mail por destinatário (AC 16)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosConcorrenciaTests.Duas_execucoes_avisam_uma_vez"`

**C27** - Se a gravação do `EmailSaida` falha, nenhuma marca do lote fica gravada (AC 17)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Falha_no_email_desfaz_a_marca"`

**C28** - Resposta pública da equipe, resolução, cancelamento e passagem a `AGUARDANDO_ALUNO` tiram o chamado do atraso, da lista `atraso=true` e dos e-mails seguintes (AC 18)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Sai_do_atraso_e_para_de_avisar"`

**C29** - Depois de 1000 h de atraso a tarefa deixa o chamado `ABERTO` ou `EM_ATENDIMENTO` no mesmo estado, sem evento novo de transição (AC 19)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Atraso_nunca_fecha_nem_resolve"`

**C30** - As regras de `SubirPorPrazo`, `LembrarAguardando`, `ResolverSemResposta` e `FecharResolvidos` seguem com os resultados de hoje (AC 19)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AgendadorTarefasTests"`

**C31** - Ao terminar a instituição a tarefa registra um log informativo com o id da instituição, a contagem de atrasados por tipo e o número de e-mails, sem título de chamado (AC 20)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AgendadorLogsTests.Avisar_atrasos_registra_contagens"`

**C32** - O corpo dos e-mails de estouro e de resumo não contém nome do aluno, matrícula nem CPF (Observable)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Email_nao_carrega_dados_do_aluno"`

**C33** - Atrasos e destinatários de outra instituição não entram no e-mail: o administrador da instituição B não recebe chamados da A (AD-008)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Aviso_nao_cruza_instituicoes"`

**C34** - `chamado.atraso_avisado_em` é anulável, os chamados existentes ficam com nulo depois da migration e nenhuma outra coluna de `chamado` muda (door 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MigracaoAtrasoTests.Coluna_anulavel_sem_efeito_nos_existentes"`

### S4 - A gestão acompanha no painel · 10 arquivos · ~130 KB · ~32k

**C35** - `GET /api/indicadores/detalhe` traz `prazos` com `primeiraRespostaMetaHoras` 24, `resolucaoMetaHoras` 120, `emAtrasoAgora` e `maisAntigoEmAtrasoDias`, e os valores batem com 3 chamados atrasados, o mais antigo há 5 dias (AC 21, AC 23, door 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~IndicadoresDetalheTests.Prazos_traz_metas_e_atraso_agora"`

**C36** - `primeiraRespostaNoPrazo` é 0,8 com 10 chamados criados no período e respondidos, 8 deles em 24 h ou menos, e conta só os respondidos pela equipe (AC 22)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CalculoDetalheTests.Primeira_resposta_no_prazo"`

**C37** - `resolucaoNoPrazo` é 0,7 com 10 resolvidos no período, 7 com resolução de 120 h ou menos descontada a pausa em `AGUARDANDO_ALUNO` (AC 22)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CalculoDetalheTests.Resolucao_no_prazo_com_pausa"`

**C38** - Com 9 casos as duas taxas vêm `null` e com 10 casos vêm preenchidas (AC 21, regra do AD-060)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CalculoDetalheTests.Taxas_de_prazo_exigem_10_casos"`

**C39** - Com `setorId` o bloco `prazos` considera só os chamados do setor (AC 25)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~IndicadoresDetalheTests.Prazos_com_setor"`

**C40** - A seção "A equipe responde a tempo?" mostra "Meta: primeira resposta em 24 h; resolução em 120 h", "82 % dentro da meta" com a variação "melhor" ou "pior" contra o período anterior, e oculta o percentual com `null` (AC 21)
Proof: `cd web && npx vitest run src/indicadores/prazos.test.tsx -t "meta e percentual"`

**C41** - Com 3 chamados em atraso a seção mostra "Em atraso agora: 3", "O mais antigo está atrasado há 5 dias" e o link `/univag/chamados?atraso=true`; sem atraso mostra "Nenhum chamado em atraso" (AC 23)
Proof: `cd web && npx vitest run src/indicadores/prazos.test.tsx -t "em atraso agora"`

**C42** - Carregando mostra o esqueleto da seção, e com a leitura falhando mostra o erro padrão com "Tentar de novo" sem esconder as outras seções (AC 24)
Proof: `cd web && npx vitest run src/indicadores/prazos.test.tsx -t "carregando e erro"`

**C43** - O gráfico novo tem "Ver como tabela" com a meta e o valor de cada período, e contraste 3:1 nos 4 temas (AC 26)
Proof: `cd web && npx vitest run src/indicadores/prazos.test.tsx -t "ver como tabela"`
Proof: `cd web && npx vitest run src/indicadores/contraste.test.ts -t "prazos nos 4 temas"`

**C44** - `GET /api/indicadores/detalhe` mantém `400` para período inválido, `401` sem sessão e `403` para aluno (Surface)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~IndicadoresDetalheTests.Status_continuam_os_mesmos"`

### S5 - Configuração segura · 3 arquivos · ~8 KB · ~2k

**C45** - Para cada uma das duas chaves, 1 e 8760 sobem e 0 e 8761 recusam a subida com mensagem que cita a chave (AC 27)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosTests.Metas_aceitam_de_1_a_8760"`

**C46** - `Prazos:ResolucaoHoras` = 23 com `Prazos:PrimeiraRespostaHoras` = 24 recusa a subida, e 24 com 24 sobe (AC 28)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosTests.Resolucao_nao_pode_ser_menor_que_a_primeira_resposta"`

### Transversais

**C47** - `GET /api/chamados/busca` responde `200`, `400`, `401` e `403` como o plano lista, e `GET /api/chamados/{numero}` responde `200`, `401` e `404` (Surface)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosAtendimentoTests.Status_das_rotas_alteradas"`

**C48** - `POST /api/conversas/{id}/transferir` mantém `401`, `404` e `409` além do `200` (Surface)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosAtendimentoTests.Transferir_mantem_os_status"`

**C49** - Em 360×780 e em 844×390 a lista de chamados com etiqueta, a tela do chamado do aluno e a seção de prazos em `Indicadores` não têm rolagem horizontal, a etiqueta e a frase de previsão não cortam texto e a chave do filtro de atraso tem 44 px de altura (Observable)
Proof: `cd web && npx playwright test e2e/prazos.spec.ts --workers=1`

**C50** - `GET /api/indicadores`, `GET /api/chamados?escopo=fila|meus` e as ferramentas MCP `ver_indicadores` e `listar_chamados` mantêm o JSON de hoje (Impact, AD-055)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~McpFerramentasTests"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~VisibilidadeTests"`

**C51** - O retrato das rotas inclui os campos novos nas 4 rotas e nenhum outro campo muda (Impact, door 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~RetratoTests"`

**C52** - `dotnet build Esclari.slnx` sai com 0 (CA1502 abaixo de 11, AD-018) e `ArquiteturaTests` passa com `AvisarAtrasos` como `ITarefaAgendada`, único a usar `IgnoreQueryFilters`
Proof: `dotnet build Esclari.slnx`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArquiteturaTests"`

**C53** - As suítes inteiras da API e do web passam sem asserção enfraquecida, `npm --prefix web run build` sai com 0, `crapper` não acha CRAP 10 ou mais e `dryer` não acha candidato a 0.8 (AD-018)
Proof: `dotnet test Esclari.Api.Tests`
Proof: `npm --prefix web test`
Proof: `npm --prefix web run build`
Proof: `cd web && npm run coverage && crapper --source-root src --use-existing-coverage --threshold 10`
Proof: `cd web && dryer`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| `GET /api/chamados/busca` statuses (4) | 200 C47 · 400 C17 · 401 C47 · 403 C47 | - |
| `GET /api/chamados/{numero}` statuses (3) | 200 C3 · 401 C47 · 404 C7 | - |
| `POST /api/conversas/{id}/transferir` statuses (4) | 200 C1 · 401 C48 · 404 C48 · 409 C48 | - |
| `GET /api/indicadores/detalhe` statuses (4) | 200 C35 · 400 C44 · 401 C44 · 403 C44 | - |
| `atraso` values (3) | `PRIMEIRA_RESPOSTA` C8 · `RESOLUCAO` C10 · `null` C11 | - |
| estados sem atraso (3) | `AGUARDANDO_ALUNO` C11 · `RESOLVIDO` C11 · `FECHADO` C11 | - |
| estados com previsão nula (4) | equipe respondeu C5 · `AGUARDANDO_ALUNO` C5 · `RESOLVIDO` C5 · `FECHADO` C5 | - |
| `atraso` na entrada (4) | `true` C15 · `false` C15 · ausente C15 · inválido C17 | - |
| fronteiras de relógio (5) | 24 h exatas C8 · 23 h 59 min C8 · 149 h 59 min C10 · 150 h C10 · 24 h do reenvio C23 | - |
| saídas do atraso (4) | resposta C28 · resolução C28 · cancelamento C28 · `AGUARDANDO_ALUNO` C28 | - |
| destinatários do estouro (3) | responsável C19 · atendentes do setor C20 · administradores C21 | - |
| `Prazos:PrimeiraRespostaHoras` e `Prazos:ResolucaoHoras` (4 edges por chave) | 0 C45 · 1 C45 · 8760 C45 · 8761 C45 | - |
| relação entre as metas (2) | resolução menor C46 · igual C46 | - |
| doors do Landing (3) | door 1 C34 · door 2 C45 e C46 · door 3 C35 e C51 | - |
| entidades de Relations (1) | `CHAMADO` com marca anulável C34 | - |
| startup config: metas de prazo (2 assemblies) | `Program` da API C45 · `EsclariFactory` dos testes C45 | - |
| estados de tela da seção do painel (3) | em atraso/sem atraso C41 · carregando C42 · erro C42 | - |
| viewports (2) | 360×780 C49 · 844×390 C49 | - |
| rotas que não podem mudar (4) | `GET /api/indicadores` C50 · `GET /api/chamados?escopo=` C50 · MCP `ver_indicadores` C50 · MCP `listar_chamados` C50 | - |

- Claims naming a status code, route or response shape: C1, C3, C7, C13, C15, C17, C35, C44, C47, C48, C50, C51 - cada um tem uma prova que cruza a fronteira HTTP
- Nenhum outro check afirma mais que o caso único que a prova exercita

## Test policy

Rows already answered by the repo (`AGENTS.md`: regra de domínio com teste unitário na entidade, integração na rota HTTP) cobrem as camadas tocadas. Uma ressalva de nível: a regra de atraso (C8 a C12) decide por estado, tempo e autor da mensagem e é lida em duas rotas e numa tarefa, então a prova fica na própria regra (`AtrasoDoChamadoTests`) e as rotas e a tarefa provam só o que cada uma monta com ela (C13, C19). Sem linha nova para o guia do repositório.

## Swept

- validation: C17, C45, C46
- failure modes: C25, C27, C42
- idempotency: C22, C26, C24
- authorization: C7, C44, C47, C33
- concurrency: C26
- data lifecycle: C28, C29, C30, C34
- dependency failure: C27 (e a fila `EmailSaida` já repete com atraso crescente, existing)
- state transitions: C11, C28, C29
- observability: C31, C25

## Handoff

- Medido com `wc -c` dos arquivos que cada slice toca, dividido por 4: S1 em `Chamados/Consulta`, `Dados/Chamados` e `Assistente/Conversas` (~16k) e `web/src/assistente` e `web/src/chamados` (~46k) = ~30k com os testes; S2 entra na mesma área de `Chamados/Consulta` e na lista web = ~28k; S3 em `Chamados/Tarefas`, `Agendador`, `Prazos` e `Dados/Emails` (~4k) mais testes e migration = ~12k; S4 em `Indicadores` (~16k) e `web/src/indicadores` (~24k) = ~32k; S5 = ~2k
- Total ~104k, abaixo do orçamento de 150k (AGENTS.md, `budget: 150k`) - um construtor, sem pergunta
- Mechanism: one builder
