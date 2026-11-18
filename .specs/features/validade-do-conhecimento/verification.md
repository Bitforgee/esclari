# Validade de um ano para artigos e documentos - verificação

**Verdict**: PASS (rodada 3 escopada: C23 fechou. Rodada 2 em `061f8d5` reprovou só o C23; rodada 1 reprovou C42, C4, C5 e C32, que fecharam na rodada 2)
**Profile**: ui
**Diff range**: rodada 2 em 39cdc29..061f8d5; rodada 1 em 39cdc29..b57541a (commits da feature: `7c4141b`, `1f725c0`, `1431bf6`, `99d99fe`, e `b57541a`, que moveu o filtro de `web/src/conhecimento/Validade.tsx` para o genérico `FiltroNaUrl.tsx`; os commits de `prazos-de-atendimento` e `artigo-a-partir-do-chamado` no meio do range rodaram junto nas suítes inteiras)
**Round**: 2 (a rodada 1 segue abaixo sem mudança; a rodada 2 está em `## Rodada 2`, no fim do arquivo)
**Verifier**: sub-agente independente (author != verifier); não escreveu o plano, os checks, o código nem os testes
**Data**: 17/11/2026

O estado verificado é o `HEAD` `b57541a`. A emenda E1 (rotas de renovar em `/api/admin/artigos/{id}/renovar` e `/api/admin/documentos/{id}/renovar`, AD-071) foi aplicada na leitura dos checks C9 a C15 e C47.

Os 50 checks têm prova que roda e passa. A leitura dos testes e as faltas injetadas acharam um check cuja prova não decide a afirmação: o C42 afirma "2 vencem em 30 dias e 1 vencido", mas o teste aceita qualquer número nessas duas medidas (as faltas W7 e W8 sobreviveram). Há mais 3 gaps sem peso no veredito, nos checks C4, C5 e C32.

## Binding sources

Fontes abertas: `plan.md` `## Surface` (3 rotas), `## Landing` (3 doors), `## Criteria` AC 1-32, `## Observable` (telas `Artigos`/`Documentos` e seção do painel), `checks.md` (E1) e `.specs/STATE.md` AD-069 e AD-071 (`STATE.md:75`, `:77`).

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| Surface: `POST /api/admin/artigos/{id}/renovar` (E1) | sim - `RenovarArtigo.cs:9`; `Retratos/rotas.txt` (linha nova `POST /api/admin/artigos/{id:long}/renovar`) | nenhuma | - |
| Surface: `POST /api/admin/documentos/{id}/renovar` (E1) | sim - `RenovarDocumento.cs:10` | nenhuma | - |
| Surface: `GET /api/conhecimento/validade` | sim - `ConsultarValidade.cs:15` (`SoAdministrador`) | nenhuma | - |
| Landing door 1: `valido_ate date NOT NULL`, default só na migration | sim - `20261117190700_ValidadeDoConhecimento.cs:20`, `:27`, `:59-60` | nenhuma | - |
| Landing door 2: `aviso_validade` com índice único de 5 colunas | sim - `AvisoValidadeMapeamento.cs` `HasIndex(...).IsUnique()`, migration `:53-57` | nenhuma | - |
| Landing door 3: contrato do resumo e `validoAte`/`validade` nas listas | sim - `ConsultarValidade.cs:10-12`, `ArtigoResposta.cs`, `Validade.cs:16-17` | nenhuma | - |
| Observable: etiqueta na linha, filtro, "Renovar" com 44 px, 360×780 e 844×390 | sim - `Validade.tsx:24-79`, `FiltroNaUrl.tsx:23-39`, `e2e/validade.spec.ts:40-66` | nenhuma | - |
| Observable: seção do painel vazio/erro/carregando | sim - `BaseDeConhecimento.tsx:28-37` | nenhuma | - |

## Checks

Verified at `b57541a`. Provas, uma invocação por alvo:
- API: `dotnet test Esclari.Api.Tests --logger trx` deu `Aprovado! – Com falha: 0, Aprovado: 1176, Ignorado: 0, Total: 1176`, exit 0. Cada nome citado abaixo foi lido no TRX com `outcome=Passed` e contado: 1 execução por nome. `Duas_execucoes_avisam_uma_vez` e `Aviso_nao_cruza_instituicoes` aparecem 2 vezes porque `AvisarAtrasos*Tests` tem métodos de mesmo nome; o filtro do check (`AvisarValidadeConcorrenciaTests.` / `AvisarValidadeTests.`) seleciona só o desta feature. `RetratoTests` 6/6 e `ArquiteturaTests` 82/82.
- Vitest: `npx vitest run --reporter=json` deu `total=1128 passed=1128 failed=0`, exit 0. Cada prova `-t` do checks.md rodou em separado, com a contagem conferida (L-002): C39 `2 passed | 4 skipped`, C40 `2 passed | 4 skipped`, C41 `1 passed | 5 skipped`, C42 `2 passed | 3 skipped`, C43, C44 e C45 `1 passed | 4 skipped` cada.
- Playwright: `npx playwright test e2e/validade.spec.ts --workers=1` contra a imagem do compose em :8080 (reconstruída no `HEAD` depois das faltas) deu `4 passed`, exit 0.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | criar artigo em 17/11/2026 grava 2027-11-17 | TRX `Criar_artigo_grava_validade_de_12_meses` Passed (1) | `ValidadeConhecimentoTests.cs:73` resposta `"2027-11-17"`; `:75` banco `new DateOnly(2027, 11, 17)` | PASS |
| C2 | criar documento grava 2027-11-17 | TRX `Criar_documento_grava_validade_de_12_meses` Passed (1) | `ValidadeConhecimentoTests.cs:87`, `:89` | PASS |
| C3 | 2026-11-18T01:00Z em UTC-4 grava 2027-11-17 | TRX `Validade_usa_a_data_no_fuso_da_instituicao` Passed (1) | `ValidadeConhecimentoTests.cs:95` relógio `2026-11-18 01:00Z`; `:97` `America/Cuiaba`; `:102-103` artigo e documento `"2027-11-17"` | PASS |
| C4 | versão 2 de artigo refaz `validoAte` de 2027-01-10 para 2027-11-17 | TRX `ArtigoDominioTests.Nova_versao_refaz_a_validade` Passed (1) | `ArtigoDominioTests.cs:93-98`. Gap 2: a rota `PUT /api/admin/artigos/{id}` não tem teste da validade nova (falta F5 sobreviveu na suíte inteira) | PASS (gap) |
| C5 | versão 2 de documento refaz `validoAte` | TRX `DocumentoDominioTests.Nova_versao_refaz_a_validade` Passed (1) | `DocumentoDominioTests.cs:61-63`. Gap 2: a rota `POST /api/admin/documentos/{id}/versoes` não tem teste (falta F6 sobreviveu) | PASS (gap) |
| C6 | migration dá data da migration + 12 meses a 2 artigos e 2 documentos, nenhum vencido | TRX `Linhas_existentes_ganham_validade_de_12_meses` Passed (1) | `MigracaoValidadeTests.cs:17-18` (2+2 linhas antes da migration), `:69-71` | PASS |
| C7 | 1 e 60 sobem, 0 e 61 recusam citando a chave, padrão 12 | TRX `PrazosTests.Validade_do_conhecimento_aceita_de_1_a_60` Passed (1) | `PrazosTests.cs:77-81` (`Prazos.Ler`), `:83-86` (subida da factory recusada com a chave), `:88-90` (60 e padrão 12 na factory) | PASS |
| C8 | `valido_ate` NOT NULL, sem default, inserir sem ela falha | TRX `Coluna_e_obrigatoria_sem_default` Passed (1) | `MigracaoValidadeTests.cs:77` (`date,NO,` sem default), `:82-86` (`NotNullViolation` em `valido_ate`) | PASS |
| C9 | renovar artigo ATIVO: 200, 2027-11-17, VIGENTE, mesmo nº de versões | TRX `Renovar_artigo_ativo_nao_cria_versao` Passed (1) | `RenovarConhecimentoTests.cs:26` (`2026-12-01`), `:30-37` (duas chamadas, `versao.numero` 1, 1 versão no banco) | PASS |
| C10 | renovar RASCUNHO: 200 e refaz | TRX `Renovar_rascunho_responde_200` Passed (1) | `RenovarConhecimentoTests.cs:50-52` | PASS |
| C11 | renovar documento ATIVO: 200, refaz, sem versão | TRX `Renovar_documento_ativo_nao_cria_versao` Passed (1) | `RenovarConhecimentoTests.cs:64-69` | PASS |
| C12 | artigo ARQUIVADO: 409 com a mensagem, sem mudar | TRX `Renovar_artigo_arquivado_responde_409` Passed (1) | `RenovarConhecimentoTests.cs:82-84` | PASS |
| C13 | documento ARQUIVADO: 409 com a mensagem, sem mudar | TRX `Renovar_documento_arquivado_responde_409` Passed (1) | `RenovarConhecimentoTests.cs:97-99` | PASS |
| C14 | inexistente e de outra instituição: 404 igual nas duas rotas | TRX `Renovar_inexistente_ou_de_outra_instituicao_responde_404` Passed (1) | `RenovarConhecimentoTests.cs:112-126` (4 casos, mesmo `detail` por rota, item alheio intacto) | PASS |
| C15 | atendente e aluno 403, sem sessão 401, nas duas rotas | TRX `Renovar_exige_administrador` Passed (1) | `RenovarConhecimentoTests.cs:138-145` | PASS |
| C16 | artigo com validade de hoje fica, de ontem sai | TRX `Artigo_vencido_sai_da_busca_e_o_do_dia_fica` Passed (1) | `BuscaValidadeTests.cs:22-27` | PASS |
| C17 | documento de ontem sai, de hoje fica | TRX `Documento_vencido_sai_da_busca` Passed (1) | `BuscaValidadeTests.cs:36-39` | PASS |
| C18 | MCP `buscar` sem vencido, formato `{ fontes: [{ tipo, titulo, texto, links }] }` | TRX `McpFerramentasTests.Buscar_ignora_conteudo_vencido` Passed (1) | `McpFerramentasTests.cs:213-216` | PASS |
| C19 | renovar volta à busca, mesmos `TrechoFonte` | TRX `Renovar_volta_a_busca_sem_reindexar` Passed (1) | `BuscaValidadeTests.cs:50-57` (lista de trechos com id e indexação igual antes e depois) | PASS |
| C20 | única fonte vencida: `SemBase` e oferece atendente | TRX `AssistenteTests.Fonte_vencida_conta_como_sem_base` Passed (1) | `AssistenteTests.cs:49-54` | PASS |
| C21 | vencido continua ATIVO e a lista diz VENCIDO | TRX `Vencido_continua_ativo_e_aparece_como_vencido` Passed (1) | `ValidadeConhecimentoTests.cs:118-121` | PASS |
| C22 | 30 dias gera `EM_30_DIAS`, 31 nada | TRX `Marco_de_30_dias` Passed (1) | `AvisarValidadeTests.cs:55-61` | PASS |
| C23 | 7 dias gera `EM_7_DIAS`, 8 só o que já havia | TRX `Marco_de_7_dias` Passed (1) | `AvisarValidadeTests.cs:69-81` | PASS |
| C24 | ontem gera `VENCIDO`, hoje não | TRX `Marco_de_vencido` Passed (1) | `AvisarValidadeTests.cs:94` (hoje fica `EM_7_DIAS`) | PASS |
| C25 | a 5 dias na primeira execução grava só `EM_7_DIAS`, uma linha | TRX `Cruzar_varios_marcos_grava_so_o_mais_grave` Passed (1) | `AvisarValidadeTests.cs:107-110` | PASS |
| C26 | 2 admins, 3 itens: 2 e-mails, assunto, itens por marco, link filtrado | TRX `Um_email_por_administrador_com_os_itens_por_marco` Passed (1) | `AvisarValidadeTests.cs:131-146` (destinatários sem o inativo, assunto `[Univag] Base de conhecimento: 3 itens para renovar`, linhas por marco e `Abrir: .../<slug>/artigos?validade=...`) | PASS |
| C27 | segunda execução: 0 e-mails e 0 linhas novas | TRX `Segunda_execucao_nao_repete` Passed (1) | `AvisarValidadeTests.cs:158`, `:162` | PASS |
| C28 | renovado avisa de novo com o novo `valido_ate`, linha antiga fica | TRX `Renovado_avisa_de_novo_no_ano_seguinte` Passed (1) | `AvisarValidadeTests.cs:177-186` | PASS |
| C29 | sem admin com e-mail: 0 linhas, 0 e-mails, log de aviso com o id; depois avisa | TRX `Sem_administrador_nao_grava_aviso` Passed (1) | `AvisarValidadeTests.cs:202-210` | PASS |
| C30 | duas execuções concorrentes: 1 linha, 1 e-mail por admin | TRX `AvisarValidadeConcorrenciaTests.Duas_execucoes_avisam_uma_vez` Passed (1) | `AvisarValidadeTests.cs:312-318`. Instrumento I1 confirmou que a colisão acontece de fato no teste | PASS |
| C31 | falha no `EmailSaida` desfaz os avisos, a seguinte tenta de novo | TRX `Falha_no_email_desfaz_os_avisos` Passed (1) | `AvisarValidadeTests.cs:221-231` | PASS |
| C32 | artigo ARQUIVADO, RASCUNHO e documento ARQUIVADO vencidos: 0 avisos | TRX `Arquivado_e_rascunho_nao_geram_aviso` Passed (1) | `AvisarValidadeTests.cs:239-246`. Gap 3: só a seleção de instituições em `Ler` segura o caso (falta F22 sobreviveu) | PASS (gap) |
| C33 | log informativo com id e contagem por marco, sem título | TRX `AgendadorLogsTests.Avisar_validade_registra_contagem_por_marco` Passed (1) | `AgendadorLogsTests.cs:45-48` | PASS |
| C34 | índice único recusa a repetida e aceita outro `valido_ate` | TRX `Indice_unico_do_aviso` Passed (1) | `AvisarValidadeTests.cs:261-275` | PASS |
| C35 | admin de B não recebe item de A; filtro por instituição | TRX `AvisarValidadeTests.Aviso_nao_cruza_instituicoes` Passed (1) | `AvisarValidadeTests.cs:287-294` | PASS |
| C36 | resumo 200 com `{ artigos: {1,1,1}, documentos: {1,0,1} }` | TRX `Resumo_conta_vigentes_a_vencer_e_vencidos` Passed (1) | `ValidadeConhecimentoTests.cs:172-173` (JSON literal; o teste soma um rascunho e um arquivado vencidos que não entram, nota 2) | PASS |
| C37 | resumo: 403 atendente e aluno, 401 sem sessão | TRX `Resumo_exige_administrador` Passed (1) | `ValidadeConhecimentoTests.cs:182-184` | PASS |
| C38 | listas com `validoAte` e as 3 situações, fronteiras 30/31 e hoje/ontem | TRX `Lista_traz_validade_e_situacao` Passed (1) | `ValidadeConhecimentoTests.cs:128-149` (5 casos, artigo e documento) | PASS |
| C39 | "Vence em 17/11/2027", "Vence em 12 dias", "Vencido em 10/11/2026" com ícone | vitest `-t "etiqueta de validade"` 2 passed | `web/src/conhecimento/validade.test.tsx:41-45` (artigos e documentos) | PASS |
| C40 | filtro "Vencem em 30 dias"/"Vencidos" e `?validade=vencidos` | vitest `-t "filtro de validade"` 2 passed | `validade.test.tsx:68-82`, `:88-91` | PASS |
| C41 | "Renovar" chama a rota, "Renovado até 17/11/2027", troca a etiqueta, sem diálogo | vitest `-t "renovar sem confirmacao"` 1 passed | `validade.test.tsx:113-117` | PASS |
| C42 | seção com 12 vigentes, 2 vencem em 30 dias, 1 vencido e link "Ver vencidos" | vitest `-t "secao com totais e link"` 2 passed | `web/src/indicadores/baseDeConhecimento.test.tsx:35` decide o 12 e `:40` decide o link. `:37` e `:38` usam `toHaveTextContent('2')`/`('1')` no elemento pai, que também contém a nota "2 artigos e 0 documentos"/"1 artigo e 0 documentos". Com o total errado (3 e 2), o teste passa: faltas W7 e W8 sobreviveram | FAIL |
| C43 | vazio: "Nenhum conteúdo publicado ainda", sem número | vitest `-t "vazio"` 1 passed | `baseDeConhecimento.test.tsx:57-59` | PASS |
| C44 | erro padrão com "Tentar de novo", painel visível | vitest `-t "erro sem esconder o painel"` 1 passed | `baseDeConhecimento.test.tsx:68-74` | PASS |
| C45 | esqueleto, sem totais | vitest `-t "carregando"` 1 passed | `baseDeConhecimento.test.tsx:83-86` | PASS |
| C46 | 360×780 e 844×390 sem rolagem horizontal, etiqueta inteira, "Renovar" ≥ 44 px | playwright `e2e/validade.spec.ts` 4 passed | `web/e2e/validade.spec.ts:43-52`, `:60-64` | PASS |
| C47 | retrato com as 3 rotas e os 2 campos, nada mais muda | TRX `RetratoTests` 6/6 Passed | `git show 1f725c0 -- Esclari.Api.Tests/Retratos/`: `rotas.txt` ganha só as 3 linhas; em `sucessos.txt` as linhas existentes de artigos e documentos ganham só `"validoAte":"2027-11-02","validade":"VIGENTE"`, e entram 3 linhas novas (2 renovar e o resumo) | PASS |
| C48 | build 0 e `ArquiteturaTests` passa | `dotnet build Esclari.slnx --no-incremental` exit 0 (0 erros); TRX `ArquiteturaTests` 82/82 | `ArquiteturaTests.cs:71-78` (`AvisarValidade.cs` na lista de `IgnoreQueryFilters`), mapa da fatia `Conhecimento` com `RenovarArtigo`, `RenovarDocumento`, `AvisarValidade` | PASS |
| C49 | suítes inteiras e build do web | `dotnet test` 1176/1176; `npx vitest run` 1128/1128; `npm --prefix web run build` exit 0 | logs em `D:\esclari-verif\vc\` (`api-full.log`, `vitest.json`, `webbuild.log`) | PASS |
| C50 | `crapper` sem CRAP ≥ 10; `dryer` sem candidato a 0.8 | `npm run coverage` exit 0; `crapper --source-root src --use-existing-coverage --threshold 10` exit 0, máximo 9,8 (`Sidebar`, `ui/sidebar`); funções novas: `useFiltroNaUrl` 7,0, `EtiquetaValidade` 5,0, `BaseDeConhecimento` 4,0; `dryer` no escopo do E2 de `filtros-e-relatorios` (6 fatias e os 30 `.ts`/`.tsx` sem teste da raiz de `web/src`) exit 0, "No duplicate candidates found." `mutator`: suspenso por decisão do usuário (AD-025) | `D:\esclari-verif\vc\crapper.log`, `dryer.log` | PASS |

## Coverage

Recomputado a partir do `checks.md` `## Coverage`, com o membro achado no teste.

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| renovar artigo, status (5) | 200 C9 `RenovarConhecimentoTests.cs:32` · 401 e 403 C15 `:138` · 404 C14 `:122` · 409 C12 `:82` | - |
| renovar documento, status (5) | 200 C11 `:64` · 401 e 403 C15 `:138` · 404 C14 `:122` · 409 C13 `:97` | - |
| resumo, status (3) | 200 C36 `ValidadeConhecimentoTests.cs:172` · 401 e 403 C37 `:182-184` | - |
| `validade` (3) | `VIGENTE`, `VENCE_EM_BREVE` C38 `:130-133` · `VENCIDO` C21 `:118` | - |
| marcos (3) | `EM_30_DIAS` C22 · `EM_7_DIAS` C23 · `VENCIDO` C24 | - |
| tipos no aviso (2) | `ARTIGO`, `DOCUMENTO` C26 `AvisarValidadeTests.cs:146` | - |
| `Prazos:ValidadeConhecimentoMeses` (4) | 0, 1, 60, 61 C7 `PrazosTests.cs:77-89` | - |
| fronteiras de busca, marcos, estados, doors, relations, viewports, ações da lista | como no `checks.md`; cada membro achado no teste do check citado | - |
| estados de tela da seção (3) | vazio C43 · erro C44 · carregando C45 | - |
| totais da seção (3, implícito no C42) | vigentes `baseDeConhecimento.test.tsx:35` | vencem em 30 dias e vencidos (C42, gap 1) |

## Test policy rows

O `checks.md` não acrescenta linha nova (`## Test policy`): valem as do `AGENTS.md`. Regra de domínio com teste na entidade: atendida (`ArtigoDominioTests.cs:91`, `DocumentoDominioTests.cs:56`). Integração na rota HTTP: atendida para as 3 rotas novas e as listas; não atendida para a validade refeita em `PUT /api/admin/artigos/{id}` e `POST /api/admin/documentos/{id}/versoes` (gap 2).

## Faults injected

Cada falta foi aplicada à mão, a prova correspondente rodou e o arquivo voltou com `git checkout -- <arquivo>`. Depois de cada uma, `git status --porcelain -- Esclari.Api Esclari.Api.Tests web` saiu vazio. A falta de e2e exigiu reconstruir a imagem do compose. A imagem foi reconstruída de novo no `HEAD` limpo, e `e2e/validade.spec.ts` voltou a dar `4 passed`.

| Mutation | Location | Killed |
| --- | --- | --- |
| F1 situação: `<= 30` vira `< 30` | `Esclari.Api/Dados/Validade.cs:17` | yes - `Lista_traz_validade_e_situacao`, `Resumo_conta_...` |
| F2 "hoje" sem fuso (data UTC) | `Validade.cs:14` | yes - `Validade_usa_a_data_no_fuso_da_instituicao` |
| F3 validade de 11 meses | `Validade.cs:11` | yes - `Criar_artigo_...`, `Criar_documento_...` |
| F4 `Artigo.NovaVersao` não refaz `ValidoAte` | `Dados/Artigos/Artigo.cs` `NovaVersao` | yes - `ArtigoDominioTests.Nova_versao_refaz_a_validade` |
| F5 `PUT` de artigo mantém a validade antiga | `Conhecimento/Artigos/AlterarArtigo.cs` (`new Validade(a.ValidoAte, 0)`) | no - survived (suíte inteira: 1176/1176) |
| F6 nova versão de documento mantém a validade | `Conhecimento/Documentos/NovaVersaoDocumento.cs` | no - survived (64/64 em Documento, Retrato, Validade e Renovar) |
| F7 busca de artigo com `> hoje` | `Conhecimento/BuscarFontes.cs:30` | yes - `Artigo_vencido_sai_da_busca_e_o_do_dia_fica` |
| F8 busca sem o filtro de documento | `BuscarFontes.cs:34` | yes - `Documento_vencido_sai_da_busca`, `Buscar_ignora_conteudo_vencido`, `Renovar_volta_a_busca_...` |
| F9 busca sem o filtro de artigo | `BuscarFontes.cs:30` | yes - `Buscar_ignora_conteudo_vencido`, `Fonte_vencida_conta_como_sem_base` |
| F10 renovar artigo arquivado aceito | `Artigo.cs` `Renovar` | yes - `Renovar_artigo_arquivado_responde_409` |
| F11 renovar documento sem `SaveChanges` | `RenovarDocumento.cs:21` | yes - `Renovar_documento_ativo_nao_cria_versao`, `Renovar_volta_a_busca_...` |
| F12 documento inexistente responde 409 | `RenovarDocumento.cs:18` | yes - `Renovar_inexistente_ou_de_outra_instituicao_responde_404` |
| F13 renovar artigo cria versão | `RenovarArtigo.cs:19` | yes - `Renovar_artigo_ativo_nao_cria_versao` |
| F14 marco de 7 dias com `< 7` | `Dados/Avisos/AvisoValidade.cs:26` | yes - `Marco_de_7_dias` |
| F15 marco de 30 dias com `<= 31` | `AvisoValidade.cs:27` | equivalente - a consulta de `Novos` já corta em hoje + 30 (`AvisarValidade.cs:59`) |
| F15b `<= 31` e consulta até hoje + 31 | `AvisoValidade.cs:27`, `AvisarValidade.cs:59` | yes - `Marco_de_30_dias` |
| F16 `VENCIDO` inclui hoje | `AvisoValidade.cs:25` | yes - `Marco_de_vencido` |
| F17 `JaAvisado` só com o mesmo marco | `AvisoValidade.cs:32` | equivalente - com relógio que só anda para a frente, o marco de um mesmo `valido_ate` só fica mais grave |
| F18 `JaAvisado` sempre falso | `AvisoValidade.cs:32` | yes - `Marco_de_7_dias` |
| F19 avisos gravados antes dos e-mails (2 `SaveChanges`) | `AvisarValidade.cs:46` | yes - `Falha_no_email_desfaz_os_avisos` |
| F20 sem administrador segue e grava | `AvisarValidade.cs:41-45` | yes - `Sem_administrador_nao_grava_aviso` |
| F21 sem o `catch` da violação única | `AvisarValidade.cs:50` | equivalente para o C30 - o `Agendador` captura a exceção (`Agendador.cs:88`) e a transação perdedora não grava nada; o instrumento I1 confirmou que o teste produz a colisão |
| F22 `Novos` aceita artigo `RASCUNHO` | `AvisarValidade.cs:60` | no - survived (14/14 em AvisarValidade e no log) |
| F22b `Ler` e `Novos` aceitam `RASCUNHO` | `AvisarValidade.cs:29`, `:60` | yes - `Arquivado_e_rascunho_nao_geram_aviso` |
| F23 índice de `aviso_validade` não único | migration `20261117190700_ValidadeDoConhecimento.cs:57` | yes - `Indice_unico_do_aviso`, `Duas_execucoes_avisam_uma_vez` |
| F24 `documento.valido_ate` mantém o default | migration `:60` | yes - `Coluna_e_obrigatoria_sem_default` |
| F25 backfill do documento com 11 meses | migration `:20` | yes - `Linhas_existentes_ganham_validade_de_12_meses` |
| F26 `Prazos` aceita 61 | `Esclari.Api/Prazos.cs:14` | yes - `Validade_do_conhecimento_aceita_de_1_a_60` |
| F27 resumo conta rascunho | `Indicadores/ConsultarValidade.cs:23` | yes - `Resumo_conta_vigentes_a_vencer_e_vencidos` |
| F28 resumo sem `SoAdministrador` | `ConsultarValidade.cs:15` | yes - `Resumo_exige_administrador` |
| F29 log com vencidos e 7 dias trocados | `AvisarValidade.cs:52` | yes - `Avisar_validade_registra_contagem_por_marco` |
| F30 link do e-mail sem `?validade=` | `AvisarValidade.cs:84` | yes - `Um_email_por_administrador_com_os_itens_por_marco` |
| F31 administradores de todas as instituições | `AvisarValidade.cs:40` (`IgnoreQueryFilters`) | yes - `Aviso_nao_cruza_instituicoes` |
| W1 etiqueta de vencido sem ícone | `web/src/conhecimento/Validade.tsx:29` | yes - `etiqueta de validade` |
| W2 "Vencem em 30 dias" inclui vencidos | `Validade.tsx:49` | yes - `filtro de validade` |
| W3 aviso "Renovado em" | `Validade.tsx:61` | yes - `renovar sem confirmacao`, `renovar documento sem confirmacao` |
| W4 dias até o vencimento com 1 a mais | `Validade.tsx:16` | yes - `etiqueta de validade` e mais 2 |
| W5 filtro não lê a URL | `web/src/conhecimento/FiltroNaUrl.tsx:25` | yes - `filtro de validade`, `filtro de validade vem da rota do e-mail` |
| W6 "Ver vencidos" sem `?validade=vencidos` | `web/src/indicadores/BaseDeConhecimento.tsx:46` | yes - `secao com totais e link` |
| W7 total "Vencem em 30 dias" + 1 (mostra 3) | `BaseDeConhecimento.tsx:42` | no - survived (5/5) |
| W8 total "Vencidos" + 1 (mostra 2) | `BaseDeConhecimento.tsx:42` | no - survived (5/5) |
| W9 sem o estado vazio | `BaseDeConhecimento.tsx:37` | yes - `vazio` |
| W10 erro sem `ErroDe`/"Tentar de novo" | `BaseDeConhecimento.tsx:28` | yes - `erro sem esconder o painel` |
| W11 esqueleto com 2 blocos | `BaseDeConhecimento.tsx:32` | yes - `carregando` |
| W12 total "Vigentes" + 1 | `BaseDeConhecimento.tsx:42` | yes - `secao com totais e link` |
| E1 "Renovar" com `max-h-8` (32 px) | `Validade.tsx:65` (imagem reconstruída) | yes - `e2e/validade.spec.ts:52`, 2 testes falharam (360×780 e 844×390) |

Resumo: 46 faltas. 38 mortas, 5 sobreviveram (F5, F6, F22, W7, W8) e 3 são equivalentes (F15, F17, F21). I1 foi só instrumentação (gravar um arquivo no `catch`) e não conta como falta. A mutação por ferramenta (`mutator`) está suspensa por decisão do usuário (AD-025) e não rodou.

## Gate

Verified at `b57541a`:
- `dotnet test Esclari.Api.Tests` - 1176 passed, 0 failed
- `npx vitest run` (web) - 1128 passed, 0 failed
- `npx playwright test e2e/validade.spec.ts --workers=1` - 4 passed
- `dotnet build Esclari.slnx --no-incremental` (0 erros), `npm --prefix web run build`, `npm run coverage` + `crapper` (máx. 9,8) e `dryer` ("No duplicate candidates found.") - exit 0
- `python validate_verification.py validade-do-conhecimento --root D:\esclari` - exit 1 (`ERROR ... verdict is FAIL - route the ranked gaps back as fixes, then re-verify`), como esperado para um veredito FAIL

## Gaps

1. **C42 (FAIL): os totais "2" e "1" não são provados.** `baseDeConhecimento.test.tsx:37-38` procura o dígito no elemento pai da medida, e esse elemento também contém a nota ("2 artigos e 0 documentos", "1 artigo e 0 documentos"). Com o total de "Vencem em 30 dias" em 3 (W7) ou o de "Vencidos" em 2 (W8), o teste passa. Correção sugerida, só no teste e sem enfraquecer nada: comparar o valor da medida de forma exata, por exemplo `expect(within(await medida('Vencem em 30 dias')).getByText('2', { exact: true })).toBeInTheDocument()` (ou ler o `dd` do valor de `Medida`), e repetir para "Vencidos" e "Vigentes". Depois, rodar de novo W7 e W8.
2. **C4 e C5 (PASS com gap): a validade nova nas rotas de nova versão não tem prova HTTP.** As provas aprovadas são unitárias e decidem a regra da entidade. Nenhum teste de integração confere que `PUT /api/admin/artigos/{id}` e `POST /api/admin/documentos/{id}/versoes` passam a validade de hoje: F5 e F6 sobreviveram, e o retrato não pega porque criação e versão acontecem no mesmo dia. O AC 2 ("WHEN o administrador salva uma nova versão") fica coberto só pela entidade. Correção sugerida: um teste de integração que mova a validade para `2027-01-10` com `ValidadeDoArtigo`/`ValidadeDoDocumento`, salve a versão 2 pela rota e confira `validoAte` = `2027-11-17` na resposta e no banco.
3. **C32 (PASS com gap): a exclusão de rascunho em `Novos` não é exercitada.** O teste só tem itens não ativos, então a instituição nem entra em `Ler` (`AvisarValidade.cs:29`) e `Executar` não roda. Com F22 (rascunho aceito em `Novos`), um rascunho vencido entra no e-mail sempre que a instituição tiver também um item ativo em marco, e o teste continua verde. Correção sugerida: acrescentar ao `Arquivado_e_rascunho_nao_geram_aviso` um artigo ativo vencido e conferir que só ele gera linha e aparece no corpo do e-mail.

## Notes

Sem peso no veredito:

1. O `AvisarValidade.Ler` (`AvisarValidade.cs:28`) usa a data UTC de `agora` para o corte da instituição (`DiasEmBreve + 1`), e o `Executar` usa o fuso. A folga de um dia cobre a diferença, e nenhum check depende disso.
2. O C36 soma um rascunho e um arquivado vencidos ao cenário e espera que não entrem no resumo (`ValidadeConhecimentoTests.cs:163-167`). O plano não diz se o resumo conta rascunho, mas a escolha (só `ATIVO`, `ConsultarValidade.cs:23-24`) segue o AC 23 e o texto "Nenhum conteúdo publicado ainda".
3. A imagem do compose em :8080 tinha sido criada 45 s antes do commit `b57541a`. Este Verify a reconstruiu no `HEAD` limpo e repetiu o e2e (`4 passed`).
4. Lições candidatas, para quem tiver a skill registrar com `lessons.py` (este Verify só commita este arquivo e o `STATE.md`):
   - (a) `toHaveTextContent(<número>)` num contêiner que também mostra uma nota com números não prova o valor. Compare o elemento do valor de forma exata.
   - (b) quando a prova de uma regra é só unitária na entidade, o check deve nomear também a rota que chama a regra. Do contrário, a ligação handler → entidade fica sem prova (F5, F6).
   - (c) um teste de "não gera" precisa de um item que gera no mesmo cenário. Sem ele, um filtro anterior pode segurar o caso sozinho (F22).

## Rodada 2

**Verdict (rodada 2)**: FAIL (1 check reprovado: C23). Os 4 achados da rodada 1 (C42, e os gaps de C4, C5 e C32) fecharam.
**Round**: 2
**Diff range**: 39cdc29..061f8d5. Os testes que fecham a rodada 1 estão em `c45ac55`. Depois dele entraram só commits de `prazos-de-atendimento` e `artigo-a-partir-do-chamado` (`f89920e`, `6ddc5c2`, `0cb56f1`, `061f8d5`), e nenhum toca código de produto (`git diff c45ac55 061f8d5 --stat -- Esclari.Api web Esclari.Api.Tests` lista só arquivos de teste daquelas features).
**Verifier**: sub-agente novo e independente. Não escreveu o plano, os checks, o código nem os testes, e não fez a rodada 1.
**Data**: 17/11/2026

A emenda E1 (rotas de renovar no grupo `/api/admin`, AD-071) e a emenda E2 (provas mais fortes para C4, C5, C32 e C42) foram aplicadas na leitura dos checks.

### Provas rodadas em `061f8d5`

- `dotnet build Esclari.slnx --no-incremental`: exit 0, 0 erros.
- `dotnet test Esclari.Api.Tests --logger trx`: `Aprovado! – Com falha: 0, Aprovado: 1178, Ignorado: 0, Total: 1178`, exit 0 (`D:\esclari-verif\vc2\api-full.trx`). Cada nome de teste citado no `checks.md` foi lido no TRX com `outcome=Passed` e aparece 1 vez (L-002), inclusive o novo `ValidadeConhecimentoTests.Nova_versao_pela_rota_refaz_a_validade`. `RetratoTests` 6/6 e `ArquiteturaTests` 82/82.
- `npx vitest run --maxWorkers=4 --reporter=json`: `total=1129 passed=1129 failed=0`, exit 0. Cada prova `-t` rodou em separado, com a contagem conferida: C39 `2 passed | 4 skipped (6)`, C40 `2 passed | 4 skipped (6)`, C41 `1 passed | 5 skipped (6)`, C42 `2 passed | 3 skipped (5)`, e C43, C44 e C45 `1 passed | 4 skipped (5)` cada.
- `docker compose up -d --build` no `HEAD` limpo, `npm run e2e:preparar` ("ja existe demo", "ja existe e2e") e `npx playwright test e2e/validade.spec.ts --workers=1`: `4 passed`, exit 0. Depois das faltas de e2e, a imagem foi reconstruída de novo no `HEAD` limpo (o CSS servido em :8080 não tem mais `420px`) e o e2e deu `4 passed` de novo.
- `npm --prefix web run build`: exit 0.
- `cd web && npm run coverage` exit 0 e `crapper --source-root src --use-existing-coverage --threshold 10` exit 0, máximo 9,8 (`Sidebar`, `ui/sidebar`). As funções da feature: `useFiltroNaUrl` 7,0, `EtiquetaValidade` 5,0, `BaseDeConhecimento` 4,0.
- `dryer -s src/acesso -s src/administracao -s src/assistente -s src/chamados -s src/conhecimento -s src/indicadores` mais `-s` para cada um dos 30 `.ts`/`.tsx` sem teste da raiz de `web/src`, `--threshold 0.8`: exit 0, "No duplicate candidates found."
- `mutator`: suspenso por decisão do usuário (AD-025), não rodou.

### Checks

Só os checks que mudam em relação à rodada 1 têm linha com evidência nova. Os outros repetem a evidência da rodada 1, com a prova rodada de novo em `061f8d5` e `Passed` no TRX ou no Vitest.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 a C3, C6 a C22 | como na rodada 1 | TRX, cada nome Passed (1) | evidência da rodada 1 | PASS |
| C4 | versão 2 de artigo refaz `validoAte` de 2027-01-10 para 2027-11-17 | TRX `ArtigoDominioTests.Nova_versao_refaz_a_validade` Passed (1) e `ValidadeConhecimentoTests.Nova_versao_pela_rota_refaz_a_validade` Passed (1) | `Esclari.Api.Tests/ValidadeConhecimentoTests.cs:112` (validade em 2027-01-10), `:120` (resposta do `PUT` com `"2027-11-17"`), `:122` (banco). F5 morreu | PASS |
| C5 | versão 2 de documento refaz `validoAte` | TRX `DocumentoDominioTests.Nova_versao_refaz_a_validade` Passed (1) e `Nova_versao_pela_rota_refaz_a_validade` Passed (1) | `ValidadeConhecimentoTests.cs:113`, `:121` (resposta `201` de `POST .../versoes`), `:123`. F6 morreu | PASS |
| C23 | item `ATIVO` a 7 dias gera `EM_7_DIAS`; a 8 dias, só o que já havia | TRX `AvisarValidadeTests.Marco_de_7_dias` Passed (1) | `AvisarValidadeTests.cs:69-79`. O item a 7 dias do teste não tem aviso anterior. Com a falta N11 (ordem de `Gravidade` trocada em `Dados/Avisos/AvisoValidade.cs:11`), um item que já recebeu `EM_30_DIAS` não recebe mais `EM_7_DIAS`, e a suíte passa (12/12 em `AvisarValidadeTests`). Esse é o caminho normal de todo item, que passa pelos 30 dias antes dos 7. O instrumento I2 confirmou que o defeito é real | FAIL |
| C24 a C31, C33 a C41 | como na rodada 1 | TRX ou Vitest, cada prova passou com a contagem acima | evidência da rodada 1 | PASS |
| C32 | artigo `ARQUIVADO`, `RASCUNHO` e documento `ARQUIVADO` vencidos: 0 avisos | TRX `AvisarValidadeTests.Arquivado_e_rascunho_nao_geram_aviso` Passed (1) | `AvisarValidadeTests.cs:248` (artigo ativo vencido no mesmo cenário), `:253`, `:255`. F22, N1 e N1b morreram | PASS |
| C42 | seção com 12 vigentes, 2 vencem em 30 dias, 1 vencido e link "Ver vencidos" | vitest `-t "secao com totais e link"` 2 passed | `web/src/indicadores/baseDeConhecimento.test.tsx:36-38` comparam o `dd > span` de cada medida com `'12'`, `'2'` e `'1'` exatos. W7, W8 e W16 morreram | PASS |
| C43 a C50 | como na rodada 1 | provas acima | evidência da rodada 1; C46 `4 passed`, C47 `RetratoTests` 6/6, C48 build 0 e `ArquiteturaTests` 82/82, C49 1178/1178, 1129/1129 e build do web 0, C50 `crapper` máx. 9,8 e `dryer` limpo | PASS |

### Coverage

O `## Coverage` do `checks.md` foi recomputado sobre a rodada 2. A linha "totais da seção" da rodada 1 agora tem prova: vigentes, vencem em 30 dias e vencidos em `baseDeConhecimento.test.tsx:36-38`.

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| totais da seção (3) | vigentes, vencem em 30 dias e vencidos C42 `baseDeConhecimento.test.tsx:36-38` | - |
| marcos de aviso (3) | `EM_30_DIAS` C22 · `EM_7_DIAS` C23 só para item sem aviso anterior · `VENCIDO` C24 | `EM_7_DIAS` depois de `EM_30_DIAS` (C23) |
| demais conjuntos | como na rodada 1, cada membro achado no teste do check | - |

### Test policy rows

A regra de domínio tem teste na entidade, e agora a integração na rota HTTP também cobre a validade refeita por `PUT /api/admin/artigos/{id}` e `POST /api/admin/documentos/{id}/versoes` (`ValidadeConhecimentoTests.cs:107-124`). O gap 2 da rodada 1 fechou.

### Faults injected

Cada falta foi aplicada à mão com troca de texto exata (o trecho original aparecia 1 vez no arquivo), a prova rodou e o arquivo voltou com `git checkout -- <arquivo>`. Depois de cada uma, `git status --porcelain -- Esclari.Api Esclari.Api.Tests web` saiu vazio. As faltas de e2e exigiram reconstruir a imagem, e a imagem foi reconstruída de novo no `HEAD` limpo no fim.

| Mutation | Location | Killed |
| --- | --- | --- |
| F5 (rodada 1) `PUT` de artigo mantém a validade antiga (`new Validade(a.ValidoAte, 0)`) | `Esclari.Api/Conhecimento/Artigos/AlterarArtigo.cs:26` | yes - `Nova_versao_pela_rota_refaz_a_validade` (1 de 19) |
| F6 (rodada 1) nova versão de documento mantém a validade | `Conhecimento/Documentos/NovaVersaoDocumento.cs:26` | yes - `Nova_versao_pela_rota_refaz_a_validade` (1 de 25) |
| F22 (rodada 1) `Novos` aceita artigo `RASCUNHO` | `Conhecimento/AvisarValidade.cs:60` | yes - `Arquivado_e_rascunho_nao_geram_aviso` |
| W7 (rodada 1) total "Vencem em 30 dias" + 1 | `web/src/indicadores/BaseDeConhecimento.tsx:42` | yes - `secao com totais e link` |
| W8 (rodada 1) total "Vencidos" + 1 | `BaseDeConhecimento.tsx:42` | yes - `secao com totais e link` |
| N1 `Novos` aceita documento em qualquer estado | `AvisarValidade.cs:62` | yes - `Arquivado_e_rascunho_nao_geram_aviso` |
| N1b `Novos` aceita artigo `ARQUIVADO` (`!= Rascunho`) | `AvisarValidade.cs:60` | yes - `Arquivado_e_rascunho_nao_geram_aviso` |
| N2 `Documento.Renovar` aceita arquivado | `Esclari.Api/Dados/Documentos/Documento.cs:34` | yes - `Renovar_documento_arquivado_responde_409`, `DocumentoDominioTests.Nova_versao_refaz_a_validade` |
| N3 renovar artigo sem `SaveChanges` | `Conhecimento/Artigos/RenovarArtigo.cs:20` | yes - `Renovar_artigo_ativo_nao_cria_versao`, `Renovar_rascunho_responde_200`, `Renovar_volta_a_busca_sem_reindexar` |
| N4 busca de documento com `> hoje` | `Conhecimento/BuscarFontes.cs:34` | yes - `Documento_vencido_sai_da_busca` |
| N6 e-mail também para administrador inativo | `AvisarValidade.cs:40` | yes - `Um_email_por_administrador_com_os_itens_por_marco` |
| N7 link do grupo de vencidos com `?validade=em-30-dias` | `AvisarValidade.cs:21` | yes - `Um_email_por_administrador_com_os_itens_por_marco` |
| N8 resumo conta os artigos no lugar dos documentos | `Esclari.Api/Indicadores/ConsultarValidade.cs:25` | yes - `Resumo_conta_vigentes_a_vencer_e_vencidos` |
| N9 `Prazos` aceita 0 | `Esclari.Api/Prazos.cs:14` | yes - `Validade_do_conhecimento_aceita_de_1_a_60` |
| N10 `artigo.valido_ate` mantém o default | migration `20261117190700_ValidadeDoConhecimento.cs:59` | yes - `Coluna_e_obrigatoria_sem_default` |
| N11 ordem de `Gravidade` com `EM_7_DIAS` antes de `EM_30_DIAS` | `Esclari.Api/Dados/Avisos/AvisoValidade.cs:11` | no - survived (12/12 em `AvisarValidadeTests`) |
| N12 resumo com `RequireAuthorization()` no lugar de `SoAdministrador()` | `ConsultarValidade.cs:15` | yes - `Resumo_exige_administrador` |
| N13 a execução perdedora da colisão enfileira os e-mails mesmo assim | `AvisarValidade.cs:50` | yes - `AvisarValidadeConcorrenciaTests.Duas_execucoes_avisam_uma_vez` |
| W13 filtro "Vencidos" aceita também `VENCE_EM_BREVE` | `web/src/conhecimento/Validade.tsx:50` | yes - `filtro de validade` |
| W14 nota da medida troca artigos e documentos | `BaseDeConhecimento.tsx:19` | yes - `secao com totais e link` |
| W15 "Renovar" aparece também em item vigente | `Validade.tsx:59` | yes - `etiqueta de validade` |
| W16 "Vencem em 30 dias" lê a chave `vencidos` | `BaseDeConhecimento.tsx:15` | yes - `secao com totais e link` |
| W17 data em `AAAA/MM/DD` | `Validade.tsx:11` | yes - 4 testes de `validade.test.tsx` |
| W18 "Renovar" pede confirmação (`window.confirm`) | `Validade.tsx:65` | yes - `renovar sem confirmacao`, `renovar documento sem confirmacao` |
| E2a linha da validade com `min-w-[420px] flex-nowrap` (imagem reconstruída) | `Validade.tsx:74` | equivalente para o C46 - o documento segue com `scrollWidth` 360, as etiquetas ficam inteiras e o "Renovar" fica em x=166 com 44 px; o excesso da linha (432 px num espaço de 326) é cortado por um ancestral, o que o C46 não afirma (nota R2-2) |
| E2b etiqueta de vencido com `max-w-20` (imagem reconstruída) | `Validade.tsx:28` | yes - `e2e/validade.spec.ts:37` nos 2 viewports (2 de 4 falharam) |

Resumo: 26 faltas, sendo 5 repetidas da rodada 1 e 21 novas. 24 mortas, 1 sobreviveu (N11) e 1 é equivalente para a afirmação (E2a). Há falta nova em cada área testada: domínio (N2, N11), rotas de renovar (N3), busca (N4), aviso e e-mail (N1, N1b, N6, N7), concorrência (N13), resumo e autorização (N8, N12), configuração (N9), migration (N10), lista do web (W13, W15, W17, W18), painel do web (W14, W16) e e2e (E2a, E2b).

Instrumento I2 (não conta como falta): um teste temporário `Esclari.Api.Tests/TmpVerifInstrumentoTests.cs` criou um artigo a 7 dias com uma linha `EM_30_DIAS` anterior e rodou a tarefa. No código limpo ele passou (grava `EM_7_DIAS` e envia o e-mail com o título). Com N11 ele falhou. O arquivo foi apagado em seguida e nunca entrou em commit.

### Gate

Verified at `061f8d5`:
- `dotnet test Esclari.Api.Tests` - 1178 passed, 0 failed
- `npx vitest run --maxWorkers=4` (web) - 1129 passed, 0 failed
- `npx playwright test e2e/validade.spec.ts --workers=1` - 4 passed
- `dotnet build Esclari.slnx --no-incremental`, `npm --prefix web run build`, `npm run coverage` + `crapper` (máx. 9,8) e `dryer` ("No duplicate candidates found.") - exit 0
- `python validate_verification.py validade-do-conhecimento --root D:\esclari` - exit 1 (`ERROR ... verdict is FAIL - route the ranked gaps back as fixes, then re-verify`), como esperado para um veredito FAIL

### Gaps

1. **C23 (FAIL): o marco de 7 dias depois do de 30 dias não é provado.** `Marco_de_7_dias` (`AvisarValidadeTests.cs:64-82`) cria o item a 7 dias sem aviso anterior. Na vida real todo item recebe `EM_30_DIAS` antes. Com a ordem de `Gravidade` trocada (N11), `AvisoValidade.JaAvisado` considera o `EM_30_DIAS` já gravado como mais grave que o `EM_7_DIAS`, e o lembrete de 7 dias nunca sai. A suíte inteira continua verde. Correção sugerida, só no teste e sem enfraquecer nada: no `Marco_de_7_dias`, gravar também uma linha `EM_30_DIAS` para o item a 7 dias (como o teste já faz para o de 8 dias, `:73`) e conferir que a execução acrescenta a linha `EM_7_DIAS` e que o e-mail traz o título "Trancamento". Depois, rodar N11 de novo. A prova do C23 não muda de nome, então basta uma emenda de prova no estilo da E2.

### Notes

Sem peso no veredito:

1. **R2-1.** O código de produto não mudou entre `b57541a` e `061f8d5` nesta feature. A rodada 2 só confirma que os testes de `c45ac55` fecham os 4 achados da rodada 1: F5, F6, F22, W7 e W8 morreram.
2. **R2-2.** Com E2a, uma linha da lista mais larga que a coluna fica cortada sem rolagem. O C46 afirma "sem rolagem horizontal", não "nada cortado", então a falta não contradiz o check. Se o dono quiser essa garantia, o e2e pode conferir que nenhuma linha da tabela tem `scrollWidth` maior que `clientWidth`.
3. **R2-3.** Lição candidata para quem tiver a skill registrar com `lessons.py`: um teste de marco que avança (30 dias, depois 7 dias) precisa partir do estado anterior real (o aviso de 30 dias já gravado). Do contrário, a regra de "já avisado" fica sem prova.

## Rodada 3 (escopada, fechamento)

**C23: PASS.** Correção só de teste em `c06eaa2` (`Esclari.Api.Tests/AvisarValidadeTests.cs`, 4 linhas).

- `dotnet test Esclari.Api.Tests --no-build --filter "FullyQualifiedName~AvisarValidadeTests.Marco_de_7_dias"`: Aprovado 1, Falha 0, Total 1. A DLL (18/11 06:50) é posterior ao arquivo de teste (01:41), então o build contém a correção.
- Leitura: o teste grava `EM_30_DIAS` para o item a 7 dias e para o a 8 dias, roda a tarefa e exige exatamente `EM_30_DIAS` (8d), `EM_30_DIAS` (7d) e `EM_7_DIAS` (7d), com um só e-mail que cita Trancamento e não Rematrícula. Isso decide a afirmação do C23 pelo caminho real (30 dias antes de 7) que a falta N11 escapava.
- `git diff --stat 061f8d5..HEAD -- Esclari.Api web/src`: só `Esclari.Api.Tests/AvisarValidadeTests.cs` (fora desses caminhos, só docs/specs). Nenhum código de produto mudou.

**Sem verificar (rodada escopada):** não injetei falta (N11 não foi reaplicada, exigiria rebuild); não rodei a suíte inteira, web nem e2e; a prova de que o teste novo pega N11 vem da leitura, não de execução.
