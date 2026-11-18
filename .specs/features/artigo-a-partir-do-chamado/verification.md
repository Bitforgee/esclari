# Artigo a partir da resposta de um chamado - verificação

**Verdict**: FAIL (1 check reprovado: C14; 2 checks aprovados com gap: C9, C20)
**Profile**: ui
**Diff range**: 1cef2ea..6ddc5c2 (commits da feature: `5d2224b` checks, `cd7e95f` API, `9243ee4` web, `d38a243` handoff, `02b9e5b` emendas, `b57541a` filtro "Situação"; os commits de `validade-do-conhecimento` e `prazos-de-atendimento` no meio do range rodaram junto nas suítes inteiras)
**Round**: 1
**Verifier**: sub-agente independente (author != verifier); não escreveu o plano, os checks, o código nem os testes
**Data**: 18/11/2026

O estado verificado é o `HEAD` `6ddc5c2`. As emendas E1 (largura total do formulário só em 360×780), E2 (`artigoDaResposta` também na linha `duplicado` do retrato) e E3 (filtro "Situação", `?estado=rascunho|ativo|arquivado`, com a prova extra `origem.test.tsx -t "filtro de situacao mostra os rascunhos"`), aceitas no AD-071 (`STATE.md:77`), foram aplicadas na leitura de C31, C21 e C30.

Os 34 checks têm prova que roda e passa. A leitura dos testes e as faltas injetadas acharam um check cuja prova não decide a afirmação: o C14 afirma que a ordem das recusas é "acesso, campos, CPF, estado e duplicidade", mas trocar a ordem entre campos e CPF (F9) passa na suíte inteira (1177/1177). Há mais 2 gaps com peso de risco (C9 dependente de tempo, C20 sem o `LIDER`) e 2 notas fora dos checks.

## Binding sources

Fontes abertas: `plan.md` `## Surface` (1 rota), `## Landing` (1 door), `## Criteria` AC 1-14, `## Observable`, `checks.md` (E1-E3 e as notas de precisão) e `.specs/STATE.md` AD-070 e AD-071 (`STATE.md:76`, `:77`).

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| Surface: `POST /api/chamados/{numero}/artigo` (201/400/401/403/404/409) | sim - `Esclari.Api/Conhecimento/Artigos/CriarArtigoDoChamado.cs:13-14` (rota), `:23-35` (ordem: 404/403, campos, CPF, 409 estado, 409 duplicidade) | nenhuma | - |
| Landing door 1: `origem_chamado_id` anulável, índice único parcial, `409` "Este chamado já originou o artigo “X”." | sim - migration `20261117220504_ArtigoOrigemChamado.cs:13-32`, `ArtigoMapeamento.cs:12-13`, `CriarArtigoDoChamado.cs:57-61`, `:63-76` | nenhuma | - |
| AD-070: `origemChamado` em todo `ArtigoResposta`; `artigoDaResposta` só para `ATENDENTE`/`LIDER` | sim - `ArtigoResposta.cs:6-20`, `ListarArtigos.cs:20-21`, `DetalheChamado.cs:59-63` | nenhuma | - |
| E3: filtro "Situação" na URL | sim - `web/src/conhecimento/Artigos.tsx:126-147`, `web/src/conhecimento/FiltroNaUrl.tsx:23-39` | nenhuma | - |
| Observable: botão e salvar com 44 px, formulário em 360×780/844×390, teclado aberto, toque | sim - `web/src/chamados/ArtigoDaResposta.tsx:75-78`, `web/e2e/artigo-do-chamado.spec.ts:51-82` | nenhuma (E1) | - |

## Checks

Verified at `6ddc5c2`. Provas, uma invocação por alvo:
- API: `dotnet test Esclari.Api.Tests --logger trx` deu `Aprovado! – Com falha: 0, Aprovado: 1177, Ignorado: 0, Total: 1177`, exit 0. Cada nome citado abaixo foi lido no TRX com `outcome=Passed`, 1 execução por nome: `ArtigoDoChamadoTests` 19/19, `ArtigoDominioTests.Do_chamado_nasce_rascunho_com_a_origem` 1/1, `CpfValorTests.No_texto_acha_cpf_valido_formatado_ou_nao` 1/1, `RetratoTests` 6/6, `ArquiteturaTests` 82/82, `MediatorTests` 2/2.
- Vitest: cada prova `-t` rodou em separado, com a contagem conferida (L-002): as 6 de `artigoDaResposta.test.tsx` `1 passed | 5 skipped (6)` cada; `textos.test.tsx -t "textos detalhe do chamado"` `1 passed | 3 skipped (4)`; as 2 de `origem.test.tsx` `1 passed | 1 skipped (2)` cada.
- Playwright: `npx playwright test e2e/artigo-do-chamado.spec.ts --workers=1` contra a imagem do compose em :8080, reconstruída no `HEAD` limpo antes e depois das faltas de e2e, deu `3 passed`, exit 0.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | responsável: `201`, `Location`, `RASCUNHO`, categoria, `origemChamado`, `validoAte` `2027-11-17`, `VIGENTE`, título, `origem_chamado_id` no banco | TRX `Atendente_responsavel_cria_rascunho_do_chamado_resolvido` Passed (1) | `ArtigoDoChamadoTests.cs:56-66` (data literal em `:63`, banco em `:66`) | PASS |
| C2 | `FECHADO` responde `201` `RASCUNHO` com a origem | TRX `Chamado_fechado_tambem_origina` Passed (1) | `ArtigoDoChamadoTests.cs:76-79` | PASS |
| C3 | atendente B do setor, não responsável, `201` | TRX `Atendente_do_setor_sem_ser_responsavel_cria` Passed (1) | `ArtigoDoChamadoTests.cs:87-92` (afirma que B não é o responsável) | PASS |
| C4 | `LIDER` da liderança `201` | TRX `Lider_do_setor_cria` Passed (1) | `ArtigoDoChamadoTests.cs:99-108` | PASS |
| C5 | reclassificado depois de resolvido: categoria atual "Matrícula" | TRX `Categoria_e_a_atual_do_chamado` Passed (1) | `ArtigoDoChamadoTests.cs:117-128` | PASS |
| C6 | `400` por campo com as mensagens; 8000 `201`; nada criado nas recusas | TRX `Mesmas_regras_do_artigo_do_administrador` Passed (1) | `ArtigoDoChamadoTests.cs:137-148` (8001 em `:141`, `ftp://` em `:142`) | PASS |
| C7 | `ABERTO`, `EM_ATENDIMENTO`, `AGUARDANDO_ALUNO`: `409` "O chamado ainda não foi resolvido." | TRX `Chamado_nao_resolvido_responde_409` Passed (1) | `ArtigoDoChamadoTests.cs:155-162` | PASS |
| C8 | segunda chamada: `409` com o título do primeiro; 1 artigo da origem | TRX `Segundo_artigo_do_mesmo_chamado_responde_409` Passed (1) | `ArtigoDoChamadoTests.cs:170-176` | PASS |
| C9 | duas simultâneas: uma `201`, uma `409`, 1 artigo | TRX `Duas_chamadas_simultaneas_criam_um_artigo` Passed (1) | `ArtigoDoChamadoTests.cs:185-189`. Gap 2: a corrida só chega ao índice quando o teste roda frio; na classe inteira, F12 (o `409` da corrida virando `500`) passou 3 de 3 | PASS (gap) |
| C10 | inexistente, outra instituição, outro setor: `404` "Chamado não encontrado." | TRX `Chamado_que_o_usuario_nao_ve_responde_404` Passed (1) | `ArtigoDoChamadoTests.cs:196-207` (`ESC-999999` em `:202`, `Alheio` em `:204`) | PASS |
| C11 | administrador em `LEITURA` e aluno: `403`, nada criado | TRX `Administrador_e_aluno_recebem_403` Passed (1) | `ArtigoDoChamadoTests.cs:216-220` (afirma antes que o admin vê o chamado, `:216`) | PASS |
| C12 | sem sessão `401` | TRX `Sem_sessao_responde_401` Passed (1) | `ArtigoDoChamadoTests.cs:229-230` | PASS |
| C13 | CPF em título, pergunta, resposta: `400`; dígito errado `201`; `Cpf.NoTexto` com e sem pontuação, colado a dígitos | TRX `Cpf_no_texto_responde_400` Passed (1); `CpfValorTests.No_texto_acha_cpf_valido_formatado_ou_nao` Passed (1) | `ArtigoDoChamadoTests.cs:239-246`; `CpfValorTests.cs:18-29` | PASS |
| C14 | ordem "acesso, campos, CPF, estado e duplicidade" e os 5 casos | TRX `Recusas_na_ordem_acesso_campos_estado` Passed (1) | `ArtigoDoChamadoTests.cs:257-262` afirma os 5 casos listados, mas nenhum caso tem campo inválido e CPF juntos. Com a ordem trocada para CPF antes dos campos (F9) a suíte inteira passa (1177/1177): a prova não decide a ordem "campos, CPF" que a afirmação nomeia | FAIL |
| C15 | 1 log `Information` com o modelo, `Numero`, `InstituicaoId`, sem título e resposta | TRX `Registra_log_sem_o_texto` Passed (1) | `ArtigoDoChamadoTests.cs:270-279` | PASS |
| C16 | `Artigo.DoChamado` nasce `RASCUNHO` com a origem, sem visibilidade, `ValidoAte`; `Criar` sem origem | TRX `Do_chamado_nasce_rascunho_com_a_origem` Passed (1) | `ArtigoDominioTests.cs:73-88` | PASS |
| C17 | `bigint` anulável, índice com `WHERE (origem_chamado_id IS NOT NULL)`, dois nulos convivem, repetido dá `23505` | TRX `Indice_unico_parcial_da_origem` Passed (1) | `ArtigoDoChamadoTests.cs:290-309` (`23505` em `:307`) | PASS |
| C18 | rascunho com 0 `TrechoFonte`; ativar `200` `ATIVO` e 1 trecho | TRX `Rascunho_so_entra_na_base_depois_de_ativar` Passed (1) | `ArtigoDoChamadoTests.cs:320-326` | PASS |
| C19 | lista: `origemChamado` protocolo e `null` | TRX `Lista_traz_a_origem_do_chamado` Passed (1) | `ArtigoDoChamadoTests.cs:340-344` | PASS |
| C20 | detalhe: `PODE_CRIAR`, depois `CRIADO`; ausente em `EM_ATENDIMENTO`, aluno e `LEITURA` | TRX `Detalhe_diz_se_pode_criar_ou_ja_criou` Passed (1) | `ArtigoDoChamadoTests.cs:355-366`. Gap 3: só o papel `ATENDENTE` é exercitado; sem o `LIDER` no campo (F34) a suíte inteira passa e o líder nunca veria o botão | PASS (gap) |
| C21 | `rotas.txt` ganha 1 linha; `sucessos.txt` só a linha nova, `origemChamado` nas rotas de artigo e `artigoDaResposta` em `resolver` e `duplicado` (E2) | TRX `RetratoTests` 6/6 Passed | `git diff 1cef2ea..HEAD -- Esclari.Api.Tests/Retratos`: `rotas.txt` +1 linha; script que tira `,"origemChamado":…` e `,"artigoDaResposta":…` de cada linha mudada e compara com a base deu igual nas 8 linhas mudadas (6 de artigo com `null`, `resolver` e `duplicado` com `"PODE_CRIAR"`), 61 para 62 linhas, nenhuma removida | PASS |
| C22 | build 0; `ArquiteturaTests` com o caso de uso novo; `AgirNoChamado` só nas 10 ações | `dotnet build Esclari.slnx --no-incremental` exit 0 (0 erros); TRX `ArquiteturaTests` 82/82, `MediatorTests` 2/2 | `ArquiteturaTests.cs:395` (inventário de `Artigos`), `:740` (`DoChamado` no domínio), `:853`, `:907`; `MediatorTests.cs:42` (`(10, 85)`) | PASS |
| C23 | botão com `PODE_CRIAR` em `RESOLVIDO`/`FECHADO`; sem o campo, nada | vitest `-t "botao so no chamado resolvido"` 1 passed | `web/src/chamados/artigoDaResposta.test.tsx:49-67` | PASS |
| C24 | formulário: título e perguntas vazios, resposta = segunda resposta pública, faixa antes do título | vitest `-t "formulario preenchido com a ultima resposta publica"` 1 passed | `artigoDaResposta.test.tsx:24-30` (as 5 mensagens do check), `:70-80` | PASS |
| C25 | `POST` com o corpo, aviso em `Notificações`, fecha, "Artigo criado: ESC-000001" | vitest `-t "salvar cria o rascunho"` 1 passed | `artigoDaResposta.test.tsx:82-100` (linhas vazias removidas em `:85`, corpo em `:92-96`) | PASS |
| C26 | `CRIADO`: sem botão, com o texto | vitest `-t "chamado que ja originou artigo"` 1 passed | `artigoDaResposta.test.tsx:102-107` | PASS |
| C27 | `400` mantém o texto e mostra o erro do campo; `409` mostra o `detail` no formulário | vitest `-t "falha mantem o formulario"` 1 passed | `artigoDaResposta.test.tsx:109-132` | PASS |
| C28 | salvando: desabilitado, `aria-busy="true"`, "Salvando…" | vitest `-t "salvar desabilitado durante a criacao"` 1 passed | `artigoDaResposta.test.tsx:134-145` | PASS |
| C29 | detalhe em `EM_ATENDIMENTO` sem texto novo; inventário passa | vitest `textos.test.tsx -t "textos detalhe do chamado"` 1 passed | `web/src/chamados/textos.test.tsx:93` | PASS |
| C30 | "Origem: ESC-000123" na célula do título junto da validade; sem origem nada; "Rascunho" e "Ativar" (E3: filtro "Situação") | vitest `-t "etiqueta de origem"` 1 passed; `-t "filtro de situacao mostra os rascunhos"` 1 passed | `web/src/conhecimento/origem.test.tsx:22-40`, `:42-71` | PASS |
| C31 | 360×780 e 844×390 sem rolagem horizontal, 44 px, largura total em 360 (E1), faixa acima do título, salvar alcançável; 360×400 com foco; `tap` | playwright `e2e/artigo-do-chamado.spec.ts` 3 passed | `web/e2e/artigo-do-chamado.spec.ts:51-64`, `:66-82` | PASS |
| C32 | glossário com `origem do artigo` | `git grep -q "origem do artigo" -- .specs/GLOSSARIO.md` exit 0 | `.specs/GLOSSARIO.md:89` | PASS |
| C33 | suítes e build web sem asserção enfraquecida | `dotnet test` 1177/1177; `npm --prefix web test` 126 arquivos, 1129/1129; `npm --prefix web run build` exit 0 | os trechos removidos de teste nos commits da feature (`git show cd7e95f 9243ee4 b57541a -- Esclari.Api.Tests web/src web/e2e`) são só contagens de inventário que sobem em 1 e linhas de retrato substituídas | PASS |
| C34 | `crapper` sem CRAP ≥ 10; `dryer` sem candidato a 0.8 | `npm run coverage` exit 0; `crapper --source-root src --use-existing-coverage --threshold 10` exit 0 (máx. 9,8 em `ui.sidebar`; `useFiltroNaUrl` 7,0, `ultimaRespostaDaEquipe` 5,2, `useFiltrosDeArtigo` 5,0, `ArtigoDaResposta` 4,0); `dryer` no escopo E2 de `filtros-e-relatorios` (6 fatias e os 30 `.ts/.tsx` não teste de `web/src`) exit 0, "No duplicate candidates found." | saída dos comandos | PASS |

## Coverage

Recomputado a partir do `checks.md` `## Coverage`, com o membro achado no teste.

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| `POST /api/chamados/{numero}/artigo` statuses (6) | 201 `ArtigoDoChamadoTests.cs:56` · 400 `:137-142`, `:239-241` · 401 `:229` · 403 `:218-219` · 404 `:202-204` · 409 `:160`, `:174` | - |
| papéis no chamado (5) | `ATENDENTE` responsável `:52` · `ATENDENTE` não responsável `:87` · `LIDER` `:105` · `LEITURA` `:218` · `ALUNO` `:219` | - |
| quem não vê o chamado (3) | inexistente `:202` · outra instituição `:203` · outro setor `:204` | - |
| estados do chamado (5) | `ABERTO`, `EM_ATENDIMENTO`, `AGUARDANDO_ALUNO` `:155` · `RESOLVIDO` `:49` · `FECHADO` `:73` | - |
| regras de `ValidacaoDeArtigo` (5) | `ArtigoDoChamadoTests.cs:137-146` | - |
| campos com CPF (3) e formas de CPF (4) | `:239-241`, `:244`; `CpfValorTests.cs:20-25` | - |
| ordem das recusas (5 pares do `checks.md`) | `:257-261` | campos antes de CPF, nomeado na afirmação do C14 e fora dos 5 pares (F9) |
| `artigoDaResposta` (3) | `PODE_CRIAR` `:355` e `artigoDaResposta.test.tsx:49` · `CRIADO` `:362` e `:102` · ausente `:356-358` e `:56-66` | - |
| `origemChamado` (2) | protocolo `:343` e `origem.test.tsx:31` · `null` `:344` e `:35` | - |
| door 1 e Relations (`CHAMADO`-`ARTIGO` 0..1) | C8 `:174`, C9 `:187`, C17 `:290-309`, C16 `ArtigoDominioTests.cs:73` | - |
| estados da tela do formulário (4) | aberto `artigoDaResposta.test.tsx:70` · salvando `:134` · erro `:109` · salvo `:82` | - |
| viewports e entradas (4) | 360×780, 844×390 `artigo-do-chamado.spec.ts:51` · teclado aberto, toque `:66`, `:38` | - |

## Test policy rows

O `checks.md` não acrescenta linha nova (`## Test policy`): valem as do `AGENTS.md`. Regra de domínio com teste na entidade: atendida (`ArtigoDominioTests.Do_chamado_nasce_rascunho_com_a_origem`, `CpfValorTests.No_texto_acha_cpf_valido_formatado_ou_nao`, sem banco). Integração na rota HTTP: atendida para a rota nova, para `GET /api/admin/artigos` e para `GET /api/chamados/{numero}`.

## Faults injected

Cada falta foi aplicada à mão (script que troca o trecho exato, roda a prova e volta com `git checkout -- <arquivo>`). Depois de cada uma, `git status --porcelain -- Esclari.Api Esclari.Api.Tests web` saiu vazio. As faltas de API rodaram contra `ArtigoDoChamadoTests` (mais `CpfValorTests`, `ArtigoDominioTests`, `RetratoTests` ou `ArquiteturaTests` quando a área pedia); as sobreviventes rodaram também na suíte inteira. As faltas de e2e exigiram reconstruir a imagem do compose; a imagem foi reconstruída de novo no `HEAD` limpo no fim, com `3 passed`.

| Mutation | Location | Killed |
| --- | --- | --- |
| F1 aluno recebe `404` em vez de `403` | `Esclari.Api/Conhecimento/Artigos/CriarArtigoDoChamado.cs:42` | yes - `Administrador_e_aluno_recebem_403`, `Recusas_na_ordem_...` |
| F2 administrador recebe `404` | `CriarArtigoDoChamado.cs:44` | yes - `Administrador_e_aluno_recebem_403` |
| F3 administrador em `LEITURA` cria | `CriarArtigoDoChamado.cs:44` | yes - `Administrador_e_aluno_recebem_403` |
| F4 atendente de qualquer setor cria | `CriarArtigoDoChamado.cs:43` | yes - `Chamado_que_o_usuario_nao_ve_responde_404` |
| F5 chamado lido sem o filtro da instituição (`IgnoreQueryFilters`) | `CriarArtigoDoChamado.cs:23` | yes - `ArquiteturaTests.IgnoreQueryFilters_so_no_agendador_e_no_comando` e mais 5 de `ArtigoDoChamadoTests` |
| F6 `AGUARDANDO_ALUNO` aceito | `CriarArtigoDoChamado.cs:28` | yes - `Chamado_nao_resolvido_responde_409` |
| F7 `FECHADO` recusado | `CriarArtigoDoChamado.cs:28` | yes - `Chamado_fechado_tambem_origina` |
| F8 sem `ValidacaoDeArtigo` | `CriarArtigoDoChamado.cs:27` | yes - `Mesmas_regras_...`, `Recusas_na_ordem_...` |
| F9 CPF conferido antes dos campos | `CriarArtigoDoChamado.cs:27` | no - survived (classe 19/19 e suíte inteira 1177/1177) |
| F10 estado conferido antes dos campos | `CriarArtigoDoChamado.cs:27` | yes - `Recusas_na_ordem_acesso_campos_estado` |
| F11 sem a consulta prévia de duplicidade | `CriarArtigoDoChamado.cs:29` | equivalente - o índice único recusa a gravação e `Gravou`/`JaOriginou` (`:33`, `:57-76`) devolvem o mesmo `409` com o título |
| F12 conflito do índice não vira `409` (`when (... && false)`) | `CriarArtigoDoChamado.cs:70` | yes - `Duas_chamadas_simultaneas_criam_um_artigo` rodado sozinho, 5 de 5. Rodado com a classe inteira, passou 3 de 3 (gap 2) |
| F13 índice sem `WHERE` | migration `20261117220504_ArtigoOrigemChamado.cs:24` | yes - `Indice_unico_parcial_da_origem` |
| F14 índice não único | migration `:23` | yes - `Indice_unico_parcial_da_origem` (classe); `Duas_chamadas_...` sozinho, 3 de 3 |
| F15 `409` sem o título do artigo | `CriarArtigoDoChamado.cs:60` | yes - `Segundo_artigo_...`, `Duas_chamadas_...` |
| F16 regex sem `(?<!\d)` (CPF colado a dígitos) | `Esclari.Api/Dados/Usuarios/Cpf.cs:24` | yes - `No_texto_acha_cpf_valido_formatado_ou_nao` |
| F17 sem conferir os dígitos | `Cpf.cs:22` | yes - `No_texto_...`, `Cpf_no_texto_responde_400` |
| F18 só CPF formatado | `Cpf.cs:24` | yes - `No_texto_...`, `Cpf_no_texto_...`, `Recusas_na_ordem_...` |
| F19 só CPF sem pontuação | `Cpf.cs:24` | yes - `No_texto_...`, `Cpf_no_texto_...` |
| F20 perguntas sem a busca de CPF | `CriarArtigoDoChamado.cs:53` | yes - `Cpf_no_texto_responde_400` |
| F21 título sem a busca de CPF | `CriarArtigoDoChamado.cs:52` | yes - `Cpf_no_texto_responde_400` |
| F22 resposta sem a busca de CPF | `CriarArtigoDoChamado.cs:54` | yes - `Cpf_no_texto_...`, `Recusas_na_ordem_...` |
| F23 `DoChamado` nasce `ATIVO` | `Esclari.Api/Dados/Artigos/Artigo.cs:29` | yes - `Do_chamado_nasce_...`, `Atendente_responsavel_...`, `Rascunho_so_entra_...` e mais 1 |
| F24 `DoChamado` sem a origem | `Artigo.cs:29` | yes - `Do_chamado_nasce_...` e mais 5 |
| F25 categoria original em vez da atual | `CriarArtigoDoChamado.cs:31` | yes - `Categoria_e_a_atual_do_chamado` |
| F26 validade de 6 meses | `CriarArtigoDoChamado.cs:30` | yes - `Atendente_responsavel_...` (data literal `2027-11-17`) |
| F27 `Location` `/api/artigos/{id}` | `CriarArtigoDoChamado.cs:35` | yes - `Atendente_responsavel_...` |
| F28 rascunho ganha `TrechoFonte` na criação | `CriarArtigoDoChamado.cs:32` | yes - `Rascunho_so_entra_na_base_depois_de_ativar` |
| F29 `DoChamado` com `VisivelDe` preenchido | `Artigo.cs:28` | yes - `Do_chamado_nasce_rascunho_com_a_origem` |
| F30 lista sem as origens | `Esclari.Api/Conhecimento/Artigos/ListarArtigos.cs:20` | yes - `Lista_traz_a_origem_do_chamado` |
| F31 `LEITURA` recebe `artigoDaResposta` | `Esclari.Api/Chamados/Consulta/DetalheChamado.cs:61` | yes - `Detalhe_diz_se_pode_criar_ou_ja_criou` |
| F32 nunca `CRIADO` | `DetalheChamado.cs:62` | yes - `Detalhe_diz_se_...` |
| F33 campo em todo estado | `DetalheChamado.cs:61` | yes - `Detalhe_diz_se_...`, `RetratoTests.Corpos_de_sucesso_iguais_aos_da_base` |
| F34 `LIDER` sem `artigoDaResposta` | `DetalheChamado.cs:61` | no - survived (classe e `RetratoTests` 25/25, suíte inteira 1177/1177) |
| F35 log com o título | `CriarArtigoDoChamado.cs:34` | yes - `Registra_log_sem_o_texto` |
| F36 log em `Debug` | `CriarArtigoDoChamado.cs:34` | yes - `Registra_log_sem_o_texto` |
| F37 duplicidade conferida antes dos campos | `CriarArtigoDoChamado.cs:27` | yes - `Recusas_na_ordem_acesso_campos_estado` |
| F38 só o responsável cria | `CriarArtigoDoChamado.cs:43` | yes - `Atendente_do_setor_sem_ser_responsavel_cria` e mais 4 |
| F39 FK `ON DELETE CASCADE` em vez de `SET NULL` | migration `:32` | no - survived (`ArtigoDoChamadoTests` e `Migracao*` 27/27); nenhum check afirma a FK (nota 1) |
| W1 botão aparece sem o campo | `web/src/chamados/ArtigoDaResposta.tsx:68` | yes - `botao so no chamado resolvido` |
| W2 primeira resposta em vez da última | `ArtigoDaResposta.tsx:22` | yes - `formulario preenchido ...`, `salvar cria ...`, `falha mantem ...` |
| W3 mensagem do "Sistema" conta | `ArtigoDaResposta.tsx:22` | yes - as mesmas 3 |
| W4 mensagem do aluno conta | `ArtigoDaResposta.tsx:22` | yes - as mesmas 3 |
| W5 nota interna conta | `ArtigoDaResposta.tsx:22` | yes - as mesmas 3 |
| W6 faixa depois dos campos | `ArtigoDaResposta.tsx:40` | yes - `formulario preenchido com a ultima resposta publica` |
| W7 resposta não preenchida | `ArtigoDaResposta.tsx:75` | yes - as mesmas 3 de W2 |
| W8 aviso "Rascunho criado." curto | `ArtigoDaResposta.tsx:17` | yes - `salvar cria o rascunho` |
| W9 botão não troca por "Artigo criado" | `ArtigoDaResposta.tsx:56` | yes - `salvar cria o rascunho` |
| W10 formulário não limpa o rascunho no sucesso | `ArtigoDaResposta.tsx:54` | equivalente - com `CRIADO` o componente devolve só o texto (`:61-67`) e o `Editor` sai da tela |
| W11 salvar habilitado durante o envio | `ArtigoDaResposta.tsx:78` | yes - `salvar desabilitado durante a criacao` |
| W12 linhas vazias de perguntas vão no corpo | `ArtigoDaResposta.tsx:19` | yes - `salvar cria o rascunho` |
| W13 erro esconde o componente | `ArtigoDaResposta.tsx:59` | yes - `falha mantem o formulario` |
| W13b erro fecha o formulário (`onError`) | `ArtigoDaResposta.tsx:53` | yes - `falha mantem o formulario` |
| W14 formulário sem o `ErroDe` (409 sem mensagem) | `ArtigoDaResposta.tsx:79` | yes - `falha mantem o formulario` |
| W15 "Artigo criado" sem o protocolo | `ArtigoDaResposta.tsx:65` | yes - `salvar cria ...`, `chamado que ja originou artigo` |
| W16 detalhe sem o componente | `web/src/chamados/ChamadoDetalhe.tsx:484` | yes - as 6 de `artigoDaResposta.test.tsx` |
| W17 rótulo "Origem ESC-…" sem dois pontos | `web/src/conhecimento/Artigos.tsx:66` | yes - `etiqueta de origem` |
| W18 "Origem:" em todo artigo | `Artigos.tsx:66` | yes - `etiqueta de origem` |
| W19 "Rascunhos" inclui os ativos | `Artigos.tsx:127` | yes - `filtro de situacao mostra os rascunhos` |
| W20 filtro não lê `?estado=` | `web/src/conhecimento/FiltroNaUrl.tsx:25` | yes - `filtro de situacao mostra os rascunhos` |
| W21 "Situação" não filtra a lista | `Artigos.tsx:134` | yes - `filtro de situacao mostra os rascunhos` |
| W22 vazio da situação "Nenhum artigo." | `Artigos.tsx:138` | no - survived (`src/conhecimento` 64/64); nenhum check nem o inventário de textos afirma a frase (nota 2) |
| E1 botão com 32 px (`h-8` em todas as variantes) | `ArtigoDaResposta.tsx:75` (imagem reconstruída) | yes - `artigo-do-chamado.spec.ts:51`, 360×780 e 844×390 (`Expected: >= 44, Received: 32`) |
| E2 botão com 420 px fixos | `ArtigoDaResposta.tsx:75` (imagem reconstruída) | yes - `artigo-do-chamado.spec.ts:51` 360×780 (`Expected: <= 360, Received: 436`) e `:66` |

Resumo: 63 faltas contadas (39 na API, 22 no web, 2 no e2e). 57 mortas, 4 sobreviveram (F9, F34, F39, W22) e 2 são equivalentes (F11, W10). Descartadas e fora da conta: um controle sem efeito (a consulta de duplicidade repetida, que não muda nada) e uma primeira tentativa de e2e com `h-9`, em que o 360×780 caiu por `ERR_EMPTY_RESPONSE` (contêiner ainda subindo) e `pointer-coarse:h-11` do `Button` mantém os 44 px no toque. A mutação por ferramenta (`mutator`) está suspensa por decisão do usuário (AD-025) e não rodou.

## Gate

Verified at `6ddc5c2`:
- `dotnet test Esclari.Api.Tests` - 1177 passed, 0 failed
- `npm --prefix web test` - 126 arquivos, 1129 passed, 0 failed
- `npx playwright test e2e/artigo-do-chamado.spec.ts --workers=1` - 3 passed
- `dotnet build Esclari.slnx --no-incremental` (0 erros), `npm --prefix web run build`, `npm run coverage` + `crapper` (máx. 9,8) e `dryer` no escopo E2 de `filtros-e-relatorios` ("No duplicate candidates found.") - exit 0
- `python validate_verification.py artigo-a-partir-do-chamado --root D:\esclari` - exit 1 (`verdict is FAIL - route the ranked gaps back as fixes, then re-verify`), como esperado para um veredito FAIL

## Gaps

1. **C14 (FAIL): a ordem "campos antes de CPF" não está provada.** A afirmação diz "a ordem das recusas é acesso, campos, CPF, estado e duplicidade", e o código segue essa ordem (`CriarArtigoDoChamado.cs:27`, `Validar(...) ?? ComCpf(i)`). Mas `Recusas_na_ordem_acesso_campos_estado` (`ArtigoDoChamadoTests.cs:257-262`) não tem caso com campo inválido e CPF no mesmo corpo, e a `## Coverage` do `checks.md` também deixa esse par de fora. Com a ordem trocada (F9) o corpo `{ titulo: "", resposta: "CPF 11144477735" }` responderia `errors.resposta` em vez de `errors.titulo`, e a suíte inteira passa. Se o dono ler a lista depois dos dois-pontos como a afirmação inteira, o C14 vira PASS com gap; a correção é a mesma. Correção sugerida, só no teste: em `Recusas_na_ordem_acesso_campos_estado`, acrescentar `await Campo(await Criar(b.A.Cliente, aberto, Corpo(titulo: "", resposta: "CPF 11144477735")), "titulo")` e afirmar que `errors` não tem `resposta`. Depois, rodar F9 de novo.
2. **C9 (PASS com gap): a corrida só acontece quando o teste roda frio.** Rodado sozinho (a prova do check), o teste chega ao índice único 5 de 5 vezes e mata F12 e F14. Rodado com a classe inteira (e, por consequência, na suíte inteira), as duas requisições já não se cruzam: a segunda acha o artigo na consulta prévia (`CriarArtigoDoChamado.cs:29`) e F12 passa 3 de 3. O caminho de `Gravou` (`:63-76`) fica sem prova sempre que a suíte roda junta. Correção sugerida, só no teste: forçar a corrida de forma determinística, por exemplo com um `DbCommandInterceptor` na factory que segura o `INSERT` em `artigo` da primeira requisição até a segunda passar pela consulta prévia, ou inserindo o artigo da mesma origem por outro `DbContext` entre a consulta e o `SaveChanges`; alternativa mais simples e menos firme: disparar 10 requisições em paralelo e afirmar 1 `201` e 9 `409`.
3. **C20 (PASS com gap): o papel `LIDER` não é exercitado no `artigoDaResposta`.** O AD-070 e a nota de precisão dizem que o campo vai para `ATENDENTE` e `LIDER`, e o C4 prova que o líder cria pela API. Mas `Detalhe_diz_se_pode_criar_ou_ja_criou` só usa atendentes do `Balcao`. Sem o `LIDER` em `DetalheChamado.cs:61` (F34) a suíte inteira passa, e na tela o líder do setor de liderança nunca veria o botão (AC 8). Correção sugerida: em `Lider_do_setor_cria` (`ArtigoDoChamadoTests.cs:96`), antes de criar, afirmar `artigoDaResposta` `"PODE_CRIAR"` no `GET /api/chamados/{numero}` do líder e, depois, `"CRIADO"`.

## Notes

Sem peso no veredito:

1. **FK `ON DELETE SET NULL` sem prova.** O `## Swept` do `checks.md` cita o C17 para o ciclo de vida ("FK `ON DELETE SET NULL`, C17") e o AD-070 fixa a FK, mas o C17 não afirma a FK e `Indice_unico_parcial_da_origem` não a lê. Com `CASCADE` (F39) tudo passa. Hoje nenhum fluxo apaga chamado, por isso fica como nota. Correção sugerida: em `Indice_unico_parcial_da_origem`, ler `confdeltype` de `pg_constraint` para `fk_artigo_chamados_origem_chamado_id` e afirmar `'n'`.
2. **Vazio do filtro "Situação" sem prova.** A frase "Nenhum artigo nesta situação." (`Artigos.tsx:138`, E3) não está em teste nem no inventário de `web/src/conhecimento/textos.test.tsx`, e W22 passa. Correção sugerida: em `filtro de situacao mostra os rascunhos`, escolher "Arquivados" numa lista sem arquivado e afirmar a frase, ou acrescentá-la ao inventário de `Artigos`.
3. A imagem do compose foi reconstruída 4 vezes neste Verify (antes do e2e, com E1, com E2) e uma última no `HEAD` limpo, com `3 passed` no fim.
4. Lições candidatas, para quem tiver a skill registrar com `lessons.py` (este Verify só commita este arquivo e o `STATE.md`):
   - (a) um teste de concorrência com `Task.WhenAll` de duas requisições pode deixar de correr junto quando a suíte aquece; a prova do check rodada sozinha mata a falta e a suíte inteira não. Force a corrida com uma barreira (interceptor ou inserção no meio) em vez de contar com o tempo.
   - (b) quando um check afirma uma ordem de N recusas, cada par vizinho precisa de um caso com as duas condições juntas; listar só alguns pares deixa as trocas entre os outros sem prova.
   - (c) um campo calculado por papel (`ATENDENTE` ou `LIDER`) precisa de um caso por papel na rota que o devolve, não só na rota que age.
