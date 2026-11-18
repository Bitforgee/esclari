# Prazos de atendimento, estouro e acompanhamento da gestão - verificação

**Verdict**: FAIL (1 check reprovado: C22; 5 checks aprovados com gap: C2, C13, C23, C30, C35)
**Profile**: ui
**Diff range**: 99d99fe..c45ac55 (commits da feature: `761f988` API, `dcce2cf` web, `1cef2ea` handoff; os commits de `artigo-a-partir-do-chamado` e as correções de `validade-do-conhecimento` no meio do range rodaram junto nas suítes inteiras)
**Round**: 1
**Verifier**: sub-agente independente (author != verifier); não escreveu o plano, os checks, o código nem os testes
**Data**: 17/11/2026

O estado verificado é o `HEAD` `c45ac55`. As emendas E1 (`transferir` responde `201`), E2 (retrato com as linhas de `busca` e `detalhe` e o prazo na resposta da mensagem do aluno) e E3 (prova do `dryer` no escopo do E2 de `filtros-e-relatorios`), aceitas no AD-071 (`STATE.md:77`), foram aplicadas na leitura de C1, C48, C51 e C53.

Os 53 checks têm prova que roda e passa. A leitura dos testes e as faltas injetadas acharam um check cuja prova não decide a afirmação: o C22 afirma que a execução 23 h 59 min depois não repete, mas o teste mede o intervalo pela própria constante `AvisarAtrasos.Repeticao`, e com a repetição em 12 h a suíte inteira passa (falta F21). Há mais 5 gaps sem peso no veredito.

## Binding sources

Fontes abertas: `plan.md` `## Surface` (4 rotas, `plan.md:43-46`), `## Landing` (3 doors, `plan.md:52-54`), `## Criteria` AC 1-28 (`plan.md:58-110`), `## Observable`, `checks.md` (E1-E3) e `.specs/STATE.md` AD-068 e AD-071 (`STATE.md:74`, `:77`).

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| Surface: `GET /api/chamados/busca` (`atraso`, 200/400/401/403) | sim - `BuscarChamados.cs` (`Item(..., string? Atraso)`), `FiltroDeChamados.cs:18`, `:50` | nenhuma | - |
| Surface: `GET /api/chamados/{numero}` (`prazoPrimeiraRespostaEm` só para o aluno) | sim - `DetalheChamado.cs` (`Previsao`, `JsonIgnore WhenWritingNull`) | nenhuma | - |
| Surface: `POST /api/conversas/{id}/transferir` (E1: `201`) | sim - `TransferirConversa.cs:12`, `:28` | nenhuma (E1) | - |
| Surface: `GET /api/indicadores/detalhe` (`prazos`) | sim - `CalculoDetalhe.cs:10-11`, `:30-38` | nenhuma | - |
| Landing door 1: `atraso_avisado_em timestamptz NULL` | sim - `20261117205823_PrazosDeAtendimento.cs:14-18`, `Chamado.cs:44`, `:224` | nenhuma | - |
| Landing door 2: chaves 1..8760, recusa na subida | sim - `Prazos.cs:6-9`, `:16-17`, `:26-31` | nenhuma | - |
| Landing door 3: `itens[].atraso` e `detalhe.prazos` | sim - `CalculoDetalhe.cs:10`; `Retratos/sucessos.txt` linhas `GET /api/chamados/busca?escopo=fila` (`"atraso":null`) e `GET /api/indicadores/detalhe` (`"prazos"`) | nenhuma | - |
| Observable: etiqueta, chave de 44 px, previsão, seção do painel em 360×780 e 844×390 | sim - `Chamados.tsx` (`SeloDoAtraso`, `Marca id="filtro-atraso"`), `Conversa.tsx` (`PrevisaoDaResposta`), `Prazos.tsx`, `e2e/prazos.spec.ts:41-70` | nenhuma | - |

## Checks

Verified at `c45ac55`. Provas, uma invocação por alvo:
- API: `dotnet test Esclari.Api.Tests --logger trx` deu `Aprovado! – Com falha: 0, Aprovado: 1177, Ignorado: 0, Total: 1177`, exit 0. Cada nome citado abaixo foi lido no TRX com `outcome=Passed`, 1 execução por nome. `Duas_execucoes_avisam_uma_vez` e `Aviso_nao_cruza_instituicoes` aparecem 2 vezes porque `AvisarValidade*Tests` tem métodos de mesmo nome; o filtro do check (`AvisarAtrasosConcorrenciaTests.` / `AvisarAtrasosTests.`) seleciona só o desta feature. Classes inteiras: `AgendadorTarefasTests` 2/2, `McpFerramentasTests` 15/15, `VisibilidadeTests` 4/4, `RetratoTests` 6/6, `ArquiteturaTests` 82/82.
- Vitest: `npx vitest run --reporter=json` deu `total=1128 passed=1128 failed=0`, exit 0. Cada prova `-t` rodou em separado, com a contagem conferida (L-002): C2 `1 passed (1)`; C4, C6, C14, C16 `1 passed | 1 skipped (2)` cada; C40, C41, C42, C43 (tabela) `1 passed | 3 skipped (4)` cada; C43 (contraste) `1 passed | 2 skipped (3)`.
- Playwright: `npx playwright test e2e/prazos.spec.ts --workers=1` contra a imagem do compose em :8080, reconstruída no `HEAD` limpo antes e depois das faltas de e2e, deu `6 passed`, exit 0.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | transferir `201` (E1) com `prazoPrimeiraRespostaHoras` 24, `numero`, `estado`, `setor`; com 8 devolve 8 | TRX `Transferir_devolve_o_prazo_da_primeira_resposta` Passed (1) | `PrazosAtendimentoTests.cs:25-30` (201, 24, `ESC-`, `ABERTO`, `Triagem`), `:32-37` (factory com 8 devolve 8) | PASS |
| C2 | confirmação mostra "A equipe responde em até 24 horas" | vitest `-t "confirmacao com prazo"` 1 passed | `web/src/assistente/prazo.test.tsx:23`. Gap 4: com o texto fixo em 24 (W6) a suíte web inteira passa | PASS (gap) |
| C3 | detalhe do aluno: `prazoPrimeiraRespostaEm` = `2026-11-18T12:00:00Z` | TRX `Detalhe_do_aluno_traz_a_previsao` Passed (1) | `PrazosAtendimentoTests.cs:44` (relógio 12:00Z), `:50` (valor), `:51` (equipe não recebe) | PASS |
| C4 | "Resposta prevista até 18/11 às 08:00" em UTC-4 | vitest `-t "previsao no fuso da instituicao"` 1 passed | `web/src/chamados/prazoDoAluno.test.tsx:8-14` (`America/Cuiaba`, `2026-11-18T12:00:00Z`) | PASS |
| C5 | nulo depois da resposta pública e em `AGUARDANDO_ALUNO`, `RESOLVIDO`, `FECHADO` | TRX `Previsao_some_depois_da_resposta_e_fora_dos_estados_abertos` Passed (1) | `PrazosAtendimentoTests.cs:57-72` | PASS |
| C6 | sem previsão, sem a frase | vitest `-t "sem previsao"` 1 passed | `prazoDoAluno.test.tsx:25-26` | PASS |
| C7 | outra aluna: `404` sem `prazoPrimeiraRespostaEm` | TRX `Chamado_de_outro_aluno_responde_404_sem_prazo` Passed (1) | `PrazosAtendimentoTests.cs:83-87` | PASS |
| C8 | `PRIMEIRA_RESPOSTA` com 24 h exatas, não com 23 h 59 min | TRX `Primeira_resposta_vence_em_24_horas_exatas` Passed (1) | `AtrasoDoChamadoTests.cs:21-28` (ABERTO e EM_ATENDIMENTO) | PASS |
| C9 | nota e mensagem do solicitante não contam; pública do atendente conta | TRX `So_mensagem_publica_da_equipe_conta_como_resposta` Passed (1) | `AtrasoDoChamadoTests.cs:36-41` | PASS |
| C10 | 30 h de pausa: `RESOLUCAO` em t0+150 h, não em 149 h 59 min | TRX `Resolucao_pausa_em_aguardando_aluno` Passed (1) | `AtrasoDoChamadoTests.cs:51-55` | PASS |
| C11 | `AGUARDANDO_ALUNO`, `RESOLVIDO`, `FECHADO` nunca atrasam (1000 h) | TRX `Estados_fora_do_atendimento_nunca_atrasam` Passed (1) | `AtrasoDoChamadoTests.cs:70-75` | PASS |
| C12 | os dois atrasos devolvem `PRIMEIRA_RESPOSTA` | TRX `Os_dois_atrasos_devolvem_primeira_resposta` Passed (1) | `AtrasoDoChamadoTests.cs:85-87` | PASS |
| C13 | `busca` traz `atraso` em cada item, `null` em dia; `?escopo=fila\|meus` sem o campo | TRX `Itens_trazem_atraso` Passed (1) | `BuscaDeChamadosTests.cs:416-427`. Gap 2: a leitura em lote (`FiltroDeChamados.cs:96-99`) não é exercitada com pausa nem com nota; F15 e F16 sobreviveram à suíte inteira | PASS (gap) |
| C14 | etiquetas "Atrasado: 1ª resposta"/"Atrasado: resolução" com ícone, nada em dia | vitest `-t "etiqueta de atraso"` 1 passed | `web/src/chamados/atraso.test.tsx:24-29` | PASS |
| C15 | `atraso=true` 2 de 5, `false` 3, sem parâmetro 5 | TRX `Filtro_atraso` Passed (1) | `BuscaDeChamadosTests.cs:441-447` (inclui `total` = 2) | PASS |
| C16 | "Filtros (1)" e o link `/univag/chamados?atraso=true` abre filtrado | vitest `-t "filtro de atraso"` 1 passed | `atraso.test.tsx:44-58` | PASS |
| C17 | `talvez` e vazio `400` com ProblemDetails de `atraso`; `true`/`false` `200` | TRX `Atraso_invalido_responde_400` Passed (1) | `BuscaDeChamadosTests.cs:457-464` | PASS |
| C18 | CSV com `atraso=true` só os atrasados, 16 colunas | TRX `CsvDeChamadosTests.Exporta_so_os_atrasados` Passed (1) | `CsvDeChamadosTests.cs:49-56` | PASS |
| C19 | 1 e-mail ao responsável com protocolo, título, tipo, tempo, link; marca = agora | TRX `Primeiro_estouro_avisa_o_responsavel` Passed (1) | `AvisarAtrasosTests.cs:60-70` | PASS |
| C20 | sem responsável: 2 atendentes ativos do setor; inativo e outro setor não | TRX `Sem_responsavel_avisa_os_atendentes_do_setor` Passed (1) | `AvisarAtrasosTests.cs:83-89` | PASS |
| C21 | cada admin ativo: `[Univag] 3 chamados em atraso` com os 3; marcas atualizadas | TRX `Resumo_ao_administrador` Passed (1) | `AvisarAtrasosTests.cs:105-112` (inclui admin inativo fora) | PASS |
| C22 | 23 h 59 min depois: 0 e-mails e marcas iguais | TRX `Antes_de_24_horas_nao_repete` Passed (1) | `AvisarAtrasosTests.cs:124` avança `AvisarAtrasos.Repeticao - 1 min`, não 23 h 59 min literais. Com `Repeticao` = 12 h (F21) o produto repete em 12 h e a suíte inteira passa (1177/1177): a prova não decide o valor da afirmação | FAIL |
| C23 | 24 h depois: só o resumo ao admin | TRX `Depois_de_24_horas_so_o_administrador` Passed (1) | `AvisarAtrasosTests.cs:141-148`. Gap 1: avança `AvisarAtrasos.Repeticao`, não 24 h literais (a afirmação continua verdadeira com F21, por isso não reprova) | PASS (gap) |
| C24 | 40 chamados de 3 atendentes: 4 e-mails | TRX `Primeira_execucao_agrupa_por_destinatario` Passed (1) | `AvisarAtrasosTests.cs:166-178` (40 no banco, 4 e-mails, 14/14/12 e o resumo com 40 linhas) | PASS |
| C25 | sem destinatário: sem marca, 1 log de aviso com o id, depois avisa | TRX `Sem_destinatario_nao_grava_marca` Passed (1) | `AvisarAtrasosTests.cs:196-206` | PASS |
| C26 | concorrência: 1 marca e 1 e-mail por destinatário | TRX `AvisarAtrasosConcorrenciaTests.Duas_execucoes_avisam_uma_vez` Passed (1) | `AvisarAtrasosTests.cs:335-343`; F26b (marca por `ExecuteUpdate`, sem o `xmin`) dá `Expected: 2 Actual: 4` | PASS |
| C27 | falha no `EmailSaida`: nenhuma marca | TRX `Falha_no_email_desfaz_a_marca` Passed (1) | `AvisarAtrasosTests.cs:215-221` | PASS |
| C28 | resposta, resolução, cancelamento e `AGUARDANDO_ALUNO` tiram do atraso, da lista e dos e-mails | TRX `Sai_do_atraso_e_para_de_avisar` Passed (1) | `AvisarAtrasosTests.cs:238-254` | PASS |
| C29 | 1000 h: estado igual, sem evento novo | TRX `Atraso_nunca_fecha_nem_resolve` Passed (1) | `AvisarAtrasosTests.cs:263-272` | PASS |
| C30 | regras de `SubirPorPrazo`, `LembrarAguardando`, `ResolverSemResposta`, `FecharResolvidos` iguais | TRX `AgendadorTarefasTests` 2/2 Passed | `AgendadorTarefasTests.cs:10-44` afirma só o registro e a ordem das tarefas. Gap 3: os resultados das regras são provados por outras classes da suíte inteira (`AgendadorTests`, `ExtrasTests`, `ChamadoAgendadorDominioTests`, `AgendadorConcorrenciaTests`, todas Passed), não pela prova citada | PASS (gap) |
| C31 | log informativo com id, contagem por tipo e e-mails, sem título | TRX `AgendadorLogsTests.Avisar_atrasos_registra_contagens` Passed (1) | `AgendadorLogsTests.cs:66-72` | PASS |
| C32 | e-mails sem nome, matrícula e CPF do aluno | TRX `Email_nao_carrega_dados_do_aluno` Passed (1) | `AvisarAtrasosTests.cs:285-294` | PASS |
| C33 | admin de B não recebe chamado de A | TRX `AvisarAtrasosTests.Aviso_nao_cruza_instituicoes` Passed (1) | `AvisarAtrasosTests.cs:311-319` | PASS |
| C34 | coluna anulável, existentes nulos, nada mais muda | TRX `Coluna_anulavel_sem_efeito_nos_existentes` Passed (1) | `MigracaoAtrasoTests.cs:66-71` | PASS |
| C35 | `prazos` com metas 24/120, `emAtrasoAgora` 3, mais antigo 5 dias | TRX `Prazos_traz_metas_e_atraso_agora` Passed (1) | `IndicadoresDetalheTests.cs:171-177`. Gap 2: o cálculo de `emAtrasoAgora` (`CalculoDetalhe.cs:43-50`) não é exercitado com pausa nem com resposta da equipe; F43 e F44 sobreviveram à suíte inteira | PASS (gap) |
| C36 | `primeiraRespostaNoPrazo` 0,8 (8 de 10), só respondidos pela equipe | TRX `CalculoDetalheTests.Primeira_resposta_no_prazo` Passed (1) | `CalculoDetalheTests.cs:184-191` (chamado 50 sem resposta e 51 respondido pelo solicitante ficam fora; 24 h exatas conta) | PASS |
| C37 | `resolucaoNoPrazo` 0,7 com pausa | TRX `Resolucao_no_prazo_com_pausa` Passed (1) | `CalculoDetalheTests.cs:166-176` (150 h com 30 h de pausa = dentro; 151 h = fora), `:199-202` | PASS |
| C38 | 9 casos `null`, 10 preenchidas | TRX `Taxas_de_prazo_exigem_10_casos` Passed (1) | `CalculoDetalheTests.cs:208-217` | PASS |
| C39 | com `setorId` só o setor | TRX `Prazos_com_setor` Passed (1) | `IndicadoresDetalheTests.cs:184-192` (3/11 sem filtro, 2/1 no setor) | PASS |
| C40 | meta, "82 % dentro da meta", "melhor"/"pior", `null` oculto | vitest `-t "meta e percentual"` 1 passed | `web/src/indicadores/prazos.test.tsx:37-49` | PASS |
| C41 | "Em atraso agora: 3", "há 5 dias", link; sem atraso "Nenhum chamado em atraso" | vitest `-t "em atraso agora"` 1 passed | `prazos.test.tsx:55-65` | PASS |
| C42 | esqueleto; erro padrão com "Tentar de novo" sem esconder as outras seções | vitest `-t "carregando e erro"` 1 passed | `prazos.test.tsx:71-83` | PASS |
| C43 | "Ver como tabela" com meta e valores; contraste 3:1 nos 4 temas | vitest `-t "ver como tabela"` 1 passed; `-t "prazos nos 4 temas"` 1 passed | `prazos.test.tsx:91-100`; `web/src/indicadores/contraste.test.ts:86-95` | PASS |
| C44 | `detalhe` mantém 400, 401, 403 | TRX `Status_continuam_os_mesmos` Passed (1) | `IndicadoresDetalheTests.cs:201-205` | PASS |
| C45 | 1 e 8760 sobem; 0 e 8761 recusam citando a chave, nas duas chaves | TRX `PrazosTests.Metas_aceitam_de_1_a_8760` Passed (1) | `PrazosTests.cs:118-127` (`Prazos.Ler` e subida da factory) | PASS |
| C46 | 23 com 24 recusa; 24 com 24 sobe | TRX `Resolucao_nao_pode_ser_menor_que_a_primeira_resposta` Passed (1) | `PrazosTests.cs:133-134` | PASS |
| C47 | `busca` 200/400/401/403; detalhe 200/401/404 | TRX `Status_das_rotas_alteradas` Passed (1) | `PrazosAtendimentoTests.cs:97-104` | PASS |
| C48 | transferir 401, 404, 409 além do `201` (E1) | TRX `Transferir_mantem_os_status` Passed (1) | `PrazosAtendimentoTests.cs:114-117` | PASS |
| C49 | 360×780 e 844×390 sem rolagem horizontal, textos inteiros, chave ≥ 44 px | playwright `e2e/prazos.spec.ts` 6 passed | `web/e2e/prazos.spec.ts:41-50` (etiquetas e chave), `:52-56` (previsão), `:58-70` (seção) | PASS |
| C50 | `GET /api/indicadores`, `?escopo=fila\|meus`, MCP `ver_indicadores` e `listar_chamados` com o JSON de hoje | TRX `McpFerramentasTests` 15/15, `VisibilidadeTests` 4/4 | `McpFerramentasTests.cs:309-327` (`DeepEquals` com a rota), `:66-90`. O formato dessas rotas está preso também pelas linhas inalteradas do `sucessos.txt` (C51) e por `BuscaDeChamadosTests.cs:420-427` | PASS |
| C51 | retrato com os campos novos nas 4 rotas, nada mais muda | TRX `RetratoTests` 6/6 Passed | `git diff 99d99fe..761f988 -- Esclari.Api.Tests/Retratos/sucessos.txt`: só entram `prazoPrimeiraRespostaHoras` (transferir), `prazoPrimeiraRespostaEm` (detalhe e mensagem do aluno, E2) e as 2 linhas novas de `busca` (com `"atraso"`) e `detalhe` (com `"prazos"`) | PASS |
| C52 | build 0; `ArquiteturaTests` com `AvisarAtrasos` como `ITarefaAgendada` e na lista de `IgnoreQueryFilters` | `dotnet build Esclari.slnx --no-incremental` exit 0 (0 erros); TRX `ArquiteturaTests` 82/82 | `ArquiteturaTests.cs:74` (lista com `AvisarAtrasos.cs`), `:362` (mapa da fatia) | PASS |
| C53 | suítes, build web, `crapper` < 10, `dryer` sem candidato | `dotnet test` 1177/1177; `npx vitest run` 1128/1128; `npm --prefix web run build` exit 0; `npm run coverage` exit 0 e `crapper --source-root src --use-existing-coverage --threshold 10` exit 0, máximo 9,8 (`Sidebar`, `ui/sidebar`); funções novas: `medicoes` 5,0, `EmAtraso` 4,1, `SeloDoAtraso` 3,1, `PrevisaoDaResposta` 2,0, `PrazoDaEquipe` 2,0; `dryer` (E3: 6 fatias e os 30 `.ts`/`.tsx` sem teste da raiz de `web/src`, `--threshold 0.8`) exit 0, "No duplicate candidates found." `mutator`: suspenso por decisão do usuário (AD-025) | logs em `D:\esclari-verif\pa\` (`api-full.log`, `vitest.json`, `webbuild.log`, `crapper.log`, `dryer.log`). Os testes alterados (`estados.test.tsx`, `fila.test.tsx`, `textos.test.tsx`, `DetalheChamadoTests.cs`) só estreitaram seletores ou somaram itens novos; nenhuma asserção foi removida sem substituta | PASS |

## Coverage

Recomputado a partir do `checks.md` `## Coverage`, com o membro achado no teste.

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| `GET /api/chamados/busca` statuses (4) | 200, 401, 403 C47 `PrazosAtendimentoTests.cs:97-100` · 400 C17 `BuscaDeChamadosTests.cs:459` | - |
| `GET /api/chamados/{numero}` statuses (3) | 200 C3 `:50` · 401 C47 `:103` · 404 C7 `:83` | - |
| `POST /api/conversas/{id}/transferir` statuses (4, E1) | 201 C1 `:25` · 401, 404, 409 C48 `:114-117` | - |
| `GET /api/indicadores/detalhe` statuses (4) | 200 C35 · 400, 401, 403 C44 `IndicadoresDetalheTests.cs:201-205` | - |
| `atraso` values (3) | `PRIMEIRA_RESPOSTA` C8 · `RESOLUCAO` C10 · `null` C11 | - |
| estados sem atraso (3) e com previsão nula (4) | como no `checks.md`; cada membro achado em `AtrasoDoChamadoTests.cs:70-75` e `PrazosAtendimentoTests.cs:63-72` | - |
| `atraso` na entrada (4) | `true`, `false`, ausente C15 `BuscaDeChamadosTests.cs:441-444` · inválido C17 `:457` | - |
| fronteiras de relógio (5) | 24 h exatas e 23 h 59 min C8 · 149 h 59 min e 150 h C10 | 24 h do reenvio e 23 h 59 min do não-reenvio (C22/C23, medidos pela constante; gap 1) |
| saídas do atraso (4) | resposta, resolução, cancelamento, `AGUARDANDO_ALUNO` C28 `AvisarAtrasosTests.cs:238-254` | - |
| destinatários (3) | responsável C19 · setor C20 · administradores C21 | - |
| chaves de meta (4 edges por chave) e relação (2) | 0, 1, 8760, 8761 C45 `PrazosTests.cs:118-127` · menor e igual C46 `:133-134` | - |
| doors (3), Relations (1), startup (2 assemblies) | door 1 C34 · door 2 C45, C46 (`Prazos.Ler` e `EsclariFactoryMetasConfiguradas`) · door 3 C35, C51 | - |
| estados de tela da seção (3) | em atraso/sem atraso C41 · carregando e erro C42 | - |
| viewports (2) | 360×780 e 844×390 C49 | - |
| rotas que não podem mudar (4) | C50 + linhas inalteradas do retrato (C51) | - |

## Test policy rows

O `checks.md` não acrescenta linha nova (`## Test policy`): valem as do `AGENTS.md`. Regra de domínio com teste na entidade: atendida (`AtrasoDoChamadoTests.cs`, `ChamadoDominioTests.Marcar_atraso_avisado_so_grava_a_marca`). Integração na rota HTTP: atendida para as 4 rotas. A ressalva do próprio `checks.md` ("as rotas e a tarefa provam só o que cada uma monta com ela") não é atendida por inteiro: a montagem da pausa e do autor da mensagem na leitura em lote (`FiltroDeChamados.Atrasados`) e no painel (`CalculoDetalhe.Vencidos`) fica sem prova (gap 2).

## Faults injected

Cada falta foi aplicada à mão (script que troca o trecho, roda a prova e volta com `git checkout -- <arquivo>`). Depois de cada uma, `git status --porcelain -- Esclari.Api Esclari.Api.Tests web` saiu vazio. As faltas de e2e exigiram reconstruir a imagem do compose; a imagem foi reconstruída de novo no `HEAD` limpo e `e2e/prazos.spec.ts` voltou a dar `6 passed`.

| Mutation | Location | Killed |
| --- | --- | --- |
| F1 primeira resposta com `<` (24 h exatas não atrasa) | `Esclari.Api/Dados/Chamados/AtrasoDoChamado.cs:45` | yes - `Primeira_resposta_vence_em_24_horas_exatas` |
| F2 resolução com `<` | `AtrasoDoChamado.cs:47` | yes - `Resolucao_pausa_em_aguardando_aluno` |
| F3 resolução sem a pausa | `AtrasoDoChamado.cs:46` | yes - `Resolucao_pausa_em_aguardando_aluno` |
| F4 mensagem do solicitante conta como resposta | `AtrasoDoChamado.cs:23` | yes - `So_mensagem_publica_da_equipe_conta_como_resposta` |
| F5 nota interna conta como resposta | `AtrasoDoChamado.cs:23` | yes - `So_mensagem_publica_...` |
| F6 `AGUARDANDO_ALUNO` atrasa | `AtrasoDoChamado.cs:20` | yes - `Estados_fora_do_atendimento_nunca_atrasam` |
| F7 com os dois atrasos devolve `RESOLUCAO` | `AtrasoDoChamado.cs:45` | yes - `Os_dois_atrasos_devolvem_primeira_resposta` |
| F8 início da pausa com `=` em vez de `??=` | `AtrasoDoChamado.cs:31` | equivalente - a máquina de estados não tem transição `AGUARDANDO_ALUNO` → `AGUARDANDO_ALUNO` |
| F9 `atraso=true` não filtra | `Esclari.Api/Chamados/FiltroDeChamados.cs:86` | yes - `Filtro_atraso`, `Exporta_so_os_atrasados` |
| F10 `atraso=false` não filtra | `FiltroDeChamados.cs:87` | yes - `Filtro_atraso`, `Exporta_so_os_atrasados` |
| F11 sem a recusa de `atraso` | `FiltroDeChamados.cs:50` | yes - `Atraso_invalido_responde_400`, `Status_das_rotas_alteradas` |
| F12 `atraso=` vazio aceito | `FiltroDeChamados.cs:18` | yes - `Atraso_invalido_responde_400` |
| F13 CSV ignora `atraso` | `Chamados/Consulta/ExportarChamados.cs:42` | yes - `Exporta_so_os_atrasados` |
| F14 `itens[].atraso` sempre `null` | `Chamados/Consulta/BuscarChamados.cs:40` | yes - `Itens_trazem_atraso` |
| F15 leitura em lote sem as passagens por `AGUARDANDO_ALUNO` (sem pausa) | `FiltroDeChamados.cs:98` | no - survived (suíte inteira 1177/1177, junto com F16) |
| F16 leitura em lote conta nota interna como resposta | `FiltroDeChamados.cs:96` | no - survived (suíte inteira 1177/1177, junto com F15) |
| F17 responsável ignorado (vai ao setor) | `Chamados/Tarefas/AvisarAtrasos.cs:72` | yes - `Primeiro_estouro_...` e mais 3 |
| F18 atendente inativo recebe | `AvisarAtrasos.cs:69` | yes - `Sem_responsavel_avisa_os_atendentes_do_setor` |
| F19 atendentes de todos os setores recebem | `AvisarAtrasos.cs:68`, `:72` | yes - `Sem_responsavel_avisa_...` |
| F20 reenvio com `<` 24 h (exatas não repete) | `AvisarAtrasos.cs:26`, `:39` | yes - `Depois_de_24_horas_so_o_administrador`, `Sai_do_atraso_...`, `Atraso_nunca_fecha_...` |
| F21 `Repeticao` = 12 h | `AvisarAtrasos.cs:10` | no - survived (suíte inteira 1177/1177) |
| F22 setor recebe de novo depois de 24 h | `AvisarAtrasos.cs:42` | yes - `Depois_de_24_horas_...`, `Sai_do_atraso_...` |
| F23 um e-mail por chamado | `AvisarAtrasos.cs:73-74` | yes - `Primeira_execucao_agrupa_por_destinatario` |
| F24 sem destinatário grava a marca | `AvisarAtrasos.cs:47-48` | yes - `Sem_destinatario_nao_grava_marca` |
| F25 marca gravada antes dos e-mails (2 `SaveChanges`) | `AvisarAtrasos.cs:48` | yes - `Falha_no_email_desfaz_a_marca` |
| F26 sem `IsRowVersion` no `xmin` | `Dados/Mapeamento/ChamadoMapeamento.cs:22` | descartada - o teste falhou em 1 ms, antes da asserção (o modelo muda sem migration); não conta |
| F26b marca por `ExecuteUpdate`, fora do token de concorrência | `AvisarAtrasos.cs:48` | yes - `Duas_execucoes_avisam_uma_vez` (`Expected: 2 Actual: 4`) |
| F27 administradores de todas as instituições | `AvisarAtrasos.cs:59` (`IgnoreQueryFilters`) | yes - `Aviso_nao_cruza_instituicoes` e mais 4 |
| F28 log com os tipos trocados | `AvisarAtrasos.cs:53` | yes - `Avisar_atrasos_registra_contagens` |
| F29 meta 0 aceita | `Esclari.Api/Prazos.cs:29` | yes - `Metas_aceitam_de_1_a_8760` |
| F30 meta 8761 aceita | `Prazos.cs:29` | yes - `Metas_aceitam_de_1_a_8760` |
| F31 resolução igual à primeira recusada | `Prazos.cs:17` | yes - `Resolucao_nao_pode_...`, `Metas_aceitam_...` |
| F32 resolução 23 com 24 aceita | `Prazos.cs:17` | yes - `Resolucao_nao_pode_...` |
| F33 `ResolucaoHoras` sem faixa | `Prazos.cs:16` | yes - `Metas_aceitam_...` |
| F34 previsão também para a equipe | `Chamados/Consulta/DetalheChamado.cs:66` | yes - `Detalhe_do_aluno_traz_a_previsao`, `RetratoTests` |
| F35 previsão ignora a resposta da equipe | `DetalheChamado.cs:67` | yes - `Previsao_some_...`, `RetratoTests` |
| F36 previsão em todo estado menos `FECHADO` | `AtrasoDoChamado.cs:53` | equivalente - `AGUARDANDO_ALUNO` só se alcança por mensagem pública da equipe (`Chamado.cs:98`) e `RESOLVIDO` grava mensagem pública (`Chamado.cs:111`), então a previsão já é nula |
| F37 previsão com a meta de resolução | `AtrasoDoChamado.cs:53` | yes - `Detalhe_do_aluno_traz_a_previsao` |
| F38 transferir com 24 fixo | `Assistente/Conversas/TransferirConversa.cs:28` | yes - `Transferir_devolve_o_prazo_da_primeira_resposta` |
| F39 taxa com `<` (24 h exatas fora) | `Indicadores/CalculoDetalhe.cs:41` | yes - `Primeira_resposta_no_prazo` e mais 2 |
| F40 mínimo de 9 casos | `CalculoDetalhe.cs:16` | yes - `Taxas_de_prazo_exigem_10_casos` |
| F41 mais antigo com `Max` | `CalculoDetalhe.cs:37` | yes - `Prazos_traz_metas_e_atraso_agora`, `Prazos_com_setor` |
| F42 resolução com 30 h a mais de meta | `CalculoDetalhe.cs:36` | yes - `Resolucao_no_prazo_com_pausa`, `Taxas_de_prazo_...` |
| F43 `emAtrasoAgora` sem a pausa | `CalculoDetalhe.cs:49` | no - survived (suíte inteira 1177/1177, junto com F44) |
| F44 `emAtrasoAgora` ignora a resposta da equipe | `CalculoDetalhe.cs:48` | no - survived (suíte inteira 1177/1177, junto com F43) |
| F45 detalhe sem o filtro de setor | `Indicadores/ConsultarIndicadoresDetalhe.cs:22` | yes - `Prazos_com_setor` e mais 1 |
| F46 coluna com `DEFAULT now()` | migration `20261117205823_PrazosDeAtendimento.cs:18` | yes - `Coluna_anulavel_sem_efeito_nos_existentes` |
| W1 rótulo "Atrasada: 1ª resposta" | `web/src/chamados/Chamados.tsx:33` | yes - `etiqueta de atraso` |
| W2 etiqueta sem ícone | `Chamados.tsx:40` | yes - `etiqueta de atraso` |
| W3 filtro não lê `?atraso=true` da URL | `web/src/chamados/Busca.tsx:37` | yes - `filtro de atraso` |
| W4 filtro sem etiqueta (não conta em "Filtros (N)") | `Busca.tsx:64` | yes - `filtro de atraso` |
| W5 filtro não vai para a API | `Busca.tsx:32` | yes - `filtro de atraso` |
| W6 "A equipe responde em até 24 horas" fixo | `web/src/assistente/Assistente.tsx:222` | no - survived (suíte web inteira 1128/1128) |
| W7 previsão em UTC | `web/src/chamados/Conversa.tsx:15` | yes - `previsao no fuso da instituicao` |
| W8 frase "Resposta prevista" sem prazo | `Conversa.tsx:14` | yes - `sem previsao` |
| W8b sem a previsão no cabeçalho | `Conversa.tsx:30` | yes - `previsao no fuso da instituicao` |
| W9 link do painel sempre para `fila` | `web/src/indicadores/Prazos.tsx:62` | yes - `em atraso agora` |
| W10 dias do mais antigo + 1 | `Prazos.tsx:66` | yes - `em atraso agora` |
| W11 variação com o sentido trocado | `Prazos.tsx:16` | yes - `meta e percentual` |
| W12 `null` vira percentual | `Prazos.tsx:15` | yes - `meta e percentual` |
| W13 sem o esqueleto da seção | `web/src/indicadores/Indicadores.tsx:166` | yes - `carregando e erro` |
| W14 meta da tabela + 1 | `Prazos.tsx:40` | yes - `ver como tabela` |
| W15 cor do período anterior em `--muted` | `Prazos.tsx:12` | yes - `prazos nos 4 temas` |
| W16 sem o detalhe do período anterior | `Indicadores.tsx:169` | yes - `meta e percentual`, `ver como tabela` |
| W17 "Nenhum chamado em atraso" nunca aparece | `Prazos.tsx:61` | yes - `em atraso agora` |
| E1 contêiner da etiqueta com `min-width: 420px` | `Chamados.tsx:38` (imagem reconstruída) | equivalente - o contêiner da tabela corta o excesso; o texto da etiqueta ficou inteiro e a página sem rolagem, então o C49 continua verdadeiro |
| E1b etiqueta com `margin-left: 320px` | `Chamados.tsx:39` (imagem reconstruída) | yes - `e2e/prazos.spec.ts:43`, o teste de 360×780 falhou |

Resumo: 66 faltas contadas (46 na API, 18 no web, 2 no e2e), mais F26 descartada. 57 mortas, 6 sobreviveram (F15, F16, F21, F43, F44, W6) e 3 são equivalentes (F8, F36, E1). A mutação por ferramenta (`mutator`) está suspensa por decisão do usuário (AD-025) e não rodou.

## Gate

Verified at `c45ac55`:
- `dotnet test Esclari.Api.Tests` - 1177 passed, 0 failed
- `npx vitest run` (web) - 1128 passed, 0 failed
- `npx playwright test e2e/prazos.spec.ts --workers=1` - 6 passed
- `dotnet build Esclari.slnx --no-incremental` (0 erros), `npm --prefix web run build`, `npm run coverage` + `crapper` (máx. 9,8) e `dryer` no escopo E3 ("No duplicate candidates found.") - exit 0
- `python validate_verification.py prazos-de-atendimento --root D:\esclari` - exit 1 (`verdict is FAIL - route the ranked gaps back as fixes, then re-verify`), como esperado para um veredito FAIL

## Gaps

1. **C22 (FAIL) e C23 (PASS com gap): o intervalo de 24 h do reenvio não está preso a um valor.** `AvisarAtrasosTests.cs:124` avança `AvisarAtrasos.Repeticao - 1 min` e `:141` avança `AvisarAtrasos.Repeticao`. Com a constante em 12 h (F21) o resumo sairia 12 h depois, contra o AC 13, e a suíte inteira passa. O código de hoje está certo (`AvisarAtrasos.cs:10`, `TimeSpan.FromHours(24)`); falta a prova. Correção sugerida, só no teste: usar os valores literais do check, `TimeSpan.FromHours(24) - TimeSpan.FromMinutes(1)` em `:124` e `TimeSpan.FromHours(24)` em `:141` (ou afirmar `Assert.Equal(TimeSpan.FromHours(24), AvisarAtrasos.Repeticao)`). Depois, rodar F21 de novo.
2. **C13 e C35 (PASS com gap): a pausa e o autor da resposta não são provados nas leituras em lote.** A regra é provada na entidade (C8 a C12), mas `FiltroDeChamados.Atrasados` (`FiltroDeChamados.cs:96-99`, usado por `busca`, CSV e `AvisarAtrasos`) e `CalculoDetalhe.Vencidos` (`CalculoDetalhe.cs:43-50`, usado pelo painel) montam a pausa e as mensagens com consultas próprias, e nenhum teste HTTP tem chamado com tempo em `AGUARDANDO_ALUNO` ou com nota interna. Sem a pausa (F15, F43) um chamado que esperou o aluno aparece atrasado antes da hora na lista, no e-mail e no painel; contando a nota como resposta (F16) um chamado só anotado sai da lista de atrasados; ignorando a resposta da equipe (F44) o painel conta como atrasado um chamado já respondido. As 4 faltas passam na suíte inteira. Correção sugerida: em `BuscaDeChamadosTests.Filtro_atraso` (ou num teste novo) incluir um chamado com 30 h em `AGUARDANDO_ALUNO` e 140 h de idade (em dia) e um com só nota interna e 25 h (atrasado), e repetir os dois em `IndicadoresDetalheTests.Prazos_traz_metas_e_atraso_agora` junto com um chamado respondido de 30 h que não conta.
3. **C30 (PASS com gap): a prova citada não afirma os resultados das regras.** `AgendadorTarefasTests` só confere registro, ordem e fatia das tarefas. Os resultados de `SubirPorPrazo`, `LembrarAguardando`, `ResolverSemResposta` e `FecharResolvidos` passam na suíte inteira, em `AgendadorTests`, `ExtrasTests`, `ChamadoAgendadorDominioTests` e `AgendadorConcorrenciaTests`. Correção sugerida: citar essas classes na prova do check numa rodada futura (o check não muda depois de aprovado; fica como nota para quem escrever checks parecidos).
4. **C2 (PASS com gap): a tela não prova que usa o número da API.** `prazo.test.tsx` só usa 24, e com o texto fixo "24 horas" (W6) a suíte web passa. O AC 1 diz que a confirmação mostra a meta configurada. Correção sugerida: repetir o caso com `prazoPrimeiraRespostaHoras: 8` e esperar "A equipe responde em até 8 horas".

## Notes

Sem peso no veredito:

1. O plano (`plan.md:56`) diz que "os 24 h de repetição do resumo ... mudam por refatoração". O AC 13 e os checks C22 e C23, aprovados depois, fixam 24 h como comportamento observável. Por isso o gap 1 conta.
2. A imagem do compose foi reconstruída 3 vezes neste Verify (antes do e2e, com E1, com E1b) e uma última no `HEAD` limpo, com `6 passed` no fim.
3. Lições candidatas, para quem tiver a skill registrar com `lessons.py` (este Verify só commita este arquivo e o `STATE.md`):
   - (a) um teste de tempo que avança o relógio pela constante do código (`Repeticao - 1 min`) não prova o valor da constante. Use o valor literal do check.
   - (b) quando a regra é provada só na entidade e há outras leituras que montam os dados dela com consultas próprias (lote, painel), cada leitura precisa de um caso com o dado que muda a regra (aqui, pausa e nota interna). A mesma lição (b) da verificação de `validade-do-conhecimento` aparece de novo.
   - (c) um teste de tela que usa o valor padrão (24) não distingue o valor vindo da API de um texto fixo. Use um valor diferente do padrão.
