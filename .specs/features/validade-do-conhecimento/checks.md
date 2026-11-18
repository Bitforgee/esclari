# Validade de um ano para artigos e documentos - checks

Profile: ui
Plan: `.specs/features/validade-do-conhecimento/plan.md`

50 checks em 5 slices · 3 one-way doors · 0 perguntas abertas (plano aprovado pelo dono em 17/11/2026: "siga com o que for recomendado", "aprovo"; AD-069)

**Emenda de construção E1 (C9 a C15 e C47, só o caminho; aceita no AD-071).** As rotas de renovar ficam no grupo de administração que já serve artigos e documentos: `POST /api/admin/artigos/{id}/renovar` e `POST /api/admin/documentos/{id}/renovar`. O plano escreve `/api/artigos/...` e `/api/documentos/...`, como escreve `GET /api/artigos` para a lista que é `GET /api/admin/artigos`. `GET /api/conhecimento/validade` fica como está escrito. As afirmações não mudam.

**Emenda de construção E2 (C4, C5, C32 e C42, só as provas; depois da rodada 1 do Verify).** As afirmações não mudam; as provas ganham força. C4 e C5 ganham a prova de rota `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ValidadeConhecimentoTests.Nova_versao_pela_rota_refaz_a_validade"`, além do teste da entidade. `AvisarValidadeTests.Arquivado_e_rascunho_nao_geram_aviso` (C32) passa a ter também um artigo ativo vencido no mesmo cenário, que é o único a gerar aviso. `baseDeConhecimento.test.tsx` "secao com totais e link" (C42) compara o valor exato de cada medida, sem casar com a nota.

**Como as provas rodam.** Na raiz `D:\esclari`, salvo `cd web &&`. API: `docker compose up -d postgres mailpit minio` e uma execução de `dotnet test` por vez. Vitest: `cd web && npx vitest run <arquivo> -t "<nome>"`. Um filtro que não seleciona teste sai com 0 (L-002): conferir a contagem de testes executados. `mutator` está suspenso (AD-025). Os nomes de teste abaixo são o contrato: os testes são escritos a partir dos checks, antes da implementação, e cada um nasce em `Esclari.Api.Tests` ou `web/src` com o nome citado. O relógio dos testes é `FakeTimeProvider`; a data de hoje de cada caso vem dele.

## Checks

### S1 - Todo conteúdo nasce com validade de um ano · 12 arquivos · ~60 KB · ~15k

**C1** - Criar um artigo em `2026-11-17` grava `validoAte` = `2027-11-17` (AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ValidadeConhecimentoTests.Criar_artigo_grava_validade_de_12_meses"`

**C2** - Criar um documento em `2026-11-17` grava `validoAte` = `2027-11-17` (AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ValidadeConhecimentoTests.Criar_documento_grava_validade_de_12_meses"`

**C3** - O instante `2026-11-18T01:00:00Z` numa instituição de fuso UTC-4 é `2026-11-17` local e grava `validoAte` = `2027-11-17`, não `2027-11-18` (AC 1, fuso)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ValidadeConhecimentoTests.Validade_usa_a_data_no_fuso_da_instituicao"`

**C4** - Salvar a versão 2 de um artigo com `validoAte` `2027-01-10` e hoje `2026-11-17` refaz `validoAte` para `2027-11-17` (AC 2)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDominioTests.Nova_versao_refaz_a_validade"`

**C5** - Salvar a versão 2 de um documento refaz `validoAte` para hoje mais 12 meses (AC 2)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~DocumentoDominioTests.Nova_versao_refaz_a_validade"`

**C6** - A migration aplicada sobre um banco com 2 artigos e 2 documentos anteriores dá a cada um `validoAte` = data da migration mais 12 meses, e nenhum fica vencido (AC 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MigracaoValidadeTests.Linhas_existentes_ganham_validade_de_12_meses"`

**C7** - `Prazos:ValidadeConhecimentoMeses` com 1 e 60 sobe, com 0 e 61 recusa a subida, e a mensagem de recusa cita `Prazos:ValidadeConhecimentoMeses`; sem a chave vale 12 (AC 4)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosTests.Validade_do_conhecimento_aceita_de_1_a_60"`

**C8** - A coluna `valido_ate` de `artigo` e de `documento` é obrigatória e sem valor padrão no banco depois da migration: inserir sem ela falha (door 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MigracaoValidadeTests.Coluna_e_obrigatoria_sem_default"`

### S2 - Renovar sem reescrever · 6 arquivos · ~12 KB · ~3k

**C9** - `POST /api/artigos/{id}/renovar` de um artigo `ATIVO` com `validoAte` `2026-12-01` responde `200` com `validoAte` `2027-11-17` e `validade` `VIGENTE`, e o número de versões continua o mesmo (AC 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~RenovarConhecimentoTests.Renovar_artigo_ativo_nao_cria_versao"`

**C10** - Renovar um artigo `RASCUNHO` responde `200` e refaz `validoAte` (AC 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~RenovarConhecimentoTests.Renovar_rascunho_responde_200"`

**C11** - `POST /api/documentos/{id}/renovar` de um documento `ATIVO` responde `200`, refaz `validoAte` e não cria versão (AC 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~RenovarConhecimentoTests.Renovar_documento_ativo_nao_cria_versao"`

**C12** - Renovar artigo `ARQUIVADO` responde `409` com "Item arquivado não pode ser renovado." e não muda `validoAte` (AC 6)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~RenovarConhecimentoTests.Renovar_artigo_arquivado_responde_409"`

**C13** - Renovar documento `ARQUIVADO` responde `409` com a mesma mensagem e não muda `validoAte` (AC 6)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~RenovarConhecimentoTests.Renovar_documento_arquivado_responde_409"`

**C14** - Renovar id inexistente e id de outra instituição responde `404` nas duas rotas, sem revelar a diferença (AC 7)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~RenovarConhecimentoTests.Renovar_inexistente_ou_de_outra_instituicao_responde_404"`

**C15** - Atendente e aluno recebem `403` e a chamada sem sessão recebe `401`, nas duas rotas de renovar (AC 8)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~RenovarConhecimentoTests.Renovar_exige_administrador"`

### S3 - O que venceu deixa de ser citado · 5 arquivos · ~20 KB · ~5k

**C16** - Com hoje `2026-11-17`, trechos de artigo com `validoAte` `2026-11-17` aparecem na busca e com `2026-11-16` não aparecem (AC 9, fronteira)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaValidadeTests.Artigo_vencido_sai_da_busca_e_o_do_dia_fica"`

**C17** - Trechos de documento com `validoAte` `2026-11-16` não aparecem e com `2026-11-17` aparecem (AC 9)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaValidadeTests.Documento_vencido_sai_da_busca"`

**C18** - A ferramenta MCP `buscar` não devolve fonte vencida e mantém o formato `{ fontes: [{ tipo, titulo, texto, links }] }` (AC 9, AD-055)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~McpFerramentasTests.Buscar_ignora_conteudo_vencido"`

**C19** - Renovar um item vencido o devolve na busca seguinte, e a contagem de `TrechoFonte` antes e depois é a mesma (sem reindexar) (AC 10)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaValidadeTests.Renovar_volta_a_busca_sem_reindexar"`

**C20** - Pergunta cuja única fonte relevante está vencida produz conversa com `SemBase` verdadeiro e a resposta oferece o atendente (AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AssistenteTests.Fonte_vencida_conta_como_sem_base"`

**C21** - Um item vencido continua com `Estado` `ATIVO` na lista e a lista devolve `validade` `VENCIDO` (AC 12)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ValidadeConhecimentoTests.Vencido_continua_ativo_e_aparece_como_vencido"`

### S4 - O gestor é avisado · 7 arquivos · ~30 KB · ~8k

**C22** - Item `ATIVO` com `validoAte` a 30 dias de hoje gera o marco `EM_30_DIAS` e a 31 dias não gera nenhum aviso (AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeTests.Marco_de_30_dias"`

**C23** - Item `ATIVO` com `validoAte` a 7 dias de hoje gera `EM_7_DIAS` e a 8 dias gera só o que já estava (nenhum marco novo) (AC 14)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeTests.Marco_de_7_dias"`

**C24** - Item `ATIVO` com `validoAte` de ontem gera `VENCIDO` e com `validoAte` de hoje não gera `VENCIDO` (AC 15)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeTests.Marco_de_vencido"`

**C25** - Um item que na primeira execução já está a 5 dias do fim grava só `EM_7_DIAS` e uma linha, nunca também `EM_30_DIAS` (AC 16)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeTests.Cruzar_varios_marcos_grava_so_o_mais_grave"`

**C26** - Com 2 administradores ativos e 3 itens em marcos diferentes, saem 2 e-mails (um por administrador), com assunto `[Univag] Base de conhecimento: 3 itens para renovar`, os itens agrupados por marco com tipo, título e data, e o link `/<slug>/artigos?validade=...` (AC 17)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeTests.Um_email_por_administrador_com_os_itens_por_marco"`

**C27** - Uma segunda execução sem item novo em marco deixa 0 `EmailSaida` novos e 0 linhas novas de `aviso_validade` (AC 18)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeTests.Segunda_execucao_nao_repete"`

**C28** - Renovar um item que já tinha `EM_30_DIAS` e voltar a 30 dias do novo `validoAte` grava novo `EM_30_DIAS` com o novo `valido_ate`, e as linhas antigas permanecem (AC 19)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeTests.Renovado_avisa_de_novo_no_ano_seguinte"`

**C29** - Instituição sem administrador ativo com e-mail: 0 linhas em `aviso_validade`, 0 e-mails, um log de aviso com o id da instituição, e a execução seguinte, depois de existir administrador, avisa (AC 20)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeTests.Sem_administrador_nao_grava_aviso"`

**C30** - Duas execuções concorrentes sobre o mesmo item deixam 1 linha em `aviso_validade` e 1 e-mail por administrador (AC 21)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeConcorrenciaTests.Duas_execucoes_avisam_uma_vez"`

**C31** - Se a gravação do `EmailSaida` falha, nenhuma linha de `aviso_validade` do lote fica gravada e a execução seguinte tenta de novo (AC 22)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeTests.Falha_no_email_desfaz_os_avisos"`

**C32** - Artigo `ARQUIVADO`, artigo `RASCUNHO` e documento `ARQUIVADO` vencidos geram 0 avisos (AC 23)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeTests.Arquivado_e_rascunho_nao_geram_aviso"`

**C33** - Ao terminar a instituição a tarefa registra um log informativo com o id da instituição e a contagem por marco, sem título de item (AC 24)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AgendadorLogsTests.Avisar_validade_registra_contagem_por_marco"`

**C34** - O índice único de `aviso_validade` rejeita a segunda linha com o mesmo `(instituicao_id, tipo, item_id, valido_ate, marco)` e aceita a mesma com outro `valido_ate` (door 2)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeTests.Indice_unico_do_aviso"`

**C35** - Um administrador da instituição B não recebe e-mail de itens da instituição A, e `aviso_validade` respeita o filtro por instituição (AD-008)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeTests.Aviso_nao_cruza_instituicoes"`

### S5 - Ver o que vence e renovar nas telas · 14 arquivos · ~120 KB · ~30k

**C36** - `GET /api/conhecimento/validade` com 3 artigos (vigente, a 30 dias, vencido) e 2 documentos (vigente, vencido) responde `200` com `{ artigos: { vigentes: 1, venceEm30Dias: 1, vencidos: 1 }, documentos: { vigentes: 1, venceEm30Dias: 0, vencidos: 1 } }` (door 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ValidadeConhecimentoTests.Resumo_conta_vigentes_a_vencer_e_vencidos"`

**C37** - `GET /api/conhecimento/validade` responde `403` a atendente e aluno e `401` sem sessão (AC 32)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ValidadeConhecimentoTests.Resumo_exige_administrador"`

**C38** - `GET /api/artigos` e `GET /api/documentos` trazem `validoAte` `"2027-11-17"` e `validade` `VIGENTE`, `VENCE_EM_BREVE` (30 dias ou menos) ou `VENCIDO`, com as fronteiras 30/31 dias e hoje/ontem (door 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ValidadeConhecimentoTests.Lista_traz_validade_e_situacao"`

**C39** - Cada linha de `Artigos` e `Documentos` mostra o texto "Vence em 17/11/2027" (vigente), "Vence em 12 dias" (30 dias ou menos) ou "Vencido em 10/11/2026", e a etiqueta de vencido tem o texto e um ícone, nunca só cor (AC 25)
Proof: `cd web && npx vitest run src/conhecimento/validade.test.tsx -t "etiqueta de validade"`

**C40** - O filtro de validade lista só "Vencem em 30 dias" ou só "Vencidos" quando escolhido, e a rota `?validade=vencidos` abre a lista já filtrada (AC 26)
Proof: `cd web && npx vitest run src/conhecimento/validade.test.tsx -t "filtro de validade"`

**C41** - "Renovar" chama a rota, mostra "Renovado até 17/11/2027", troca a etiqueta para "Vence em 17/11/2027" e não abre confirmação (AC 27)
Proof: `cd web && npx vitest run src/conhecimento/validade.test.tsx -t "renovar sem confirmacao"`

**C42** - `Indicadores` mostra a seção "A base de conhecimento está em dia?" com 12 vigentes, 2 vencem em 30 dias e 1 vencido, e o link "Ver vencidos" aponta para `/univag/artigos?validade=vencidos` (AC 28)
Proof: `cd web && npx vitest run src/indicadores/baseDeConhecimento.test.tsx -t "secao com totais e link"`

**C43** - Sem artigo nem documento a seção mostra "Nenhum conteúdo publicado ainda" e nenhum número zerado (AC 29)
Proof: `cd web && npx vitest run src/indicadores/baseDeConhecimento.test.tsx -t "vazio"`

**C44** - Com `GET /api/conhecimento/validade` falhando a seção mostra o erro padrão com "Tentar de novo" e as outras seções do painel continuam visíveis (AC 30)
Proof: `cd web && npx vitest run src/indicadores/baseDeConhecimento.test.tsx -t "erro sem esconder o painel"`

**C45** - Enquanto a leitura carrega a seção mostra o esqueleto e não mostra totais (AC 31)
Proof: `cd web && npx vitest run src/indicadores/baseDeConhecimento.test.tsx -t "carregando"`

**C46** - Em 360×780 e em 844×390 a lista de `Artigos` e a seção de `Indicadores` não têm rolagem horizontal, a etiqueta de validade não corta a data e "Renovar" tem pelo menos 44 px de altura (Observable)
Proof: `cd web && npx playwright test e2e/validade.spec.ts --workers=1`

**C47** - O retrato das rotas (`Retratos/rotas.txt` e `sucessos.txt`) inclui as 3 rotas novas e os campos `validoAte` e `validade`, e nenhum outro campo das rotas existentes muda (Impact, door 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~RetratoTests"`

**C48** - `dotnet build Esclari.slnx` sai com 0 (CA1502 abaixo de 11, AD-018) e `ArquiteturaTests` passa com as classes novas (um arquivo por caso de uso, `AvisarValidade` como `ITarefaAgendada` com `IgnoreQueryFilters` só nela)
Proof: `dotnet build Esclari.slnx`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArquiteturaTests"`

**C49** - A suíte inteira da API e a do web passam sem asserção enfraquecida, e `npm --prefix web run build` sai com 0 (Impact: testes existentes que criam fontes ficam com validade em dia)
Proof: `dotnet test Esclari.Api.Tests`
Proof: `npm --prefix web test`
Proof: `npm --prefix web run build`

**C50** - `crapper` não acha função com CRAP 10 ou mais em `web/src` e `dryer` não acha candidato a 0.8 (AD-018; `mutator` suspenso pelo AD-025)
Proof: `cd web && npm run coverage && crapper --source-root src --use-existing-coverage --threshold 10`
Proof: `cd web && dryer`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| `POST /api/artigos/{id}/renovar` statuses (5) | 200 C9 · 401 C15 · 403 C15 · 404 C14 · 409 C12 | - |
| `POST /api/documentos/{id}/renovar` statuses (5) | 200 C11 · 401 C15 · 403 C15 · 404 C14 · 409 C13 | - |
| `GET /api/conhecimento/validade` statuses (3) | 200 C36 · 401 C37 · 403 C37 | - |
| `validade` values (3) | `VIGENTE` C38 · `VENCE_EM_BREVE` C38 · `VENCIDO` C21 | - |
| marcos de aviso (3) | `EM_30_DIAS` C22 · `EM_7_DIAS` C23 · `VENCIDO` C24 | - |
| tipos de item no aviso (2) | `ARTIGO` C26 · `DOCUMENTO` C26 | - |
| `Prazos:ValidadeConhecimentoMeses` (4 edges) | 0 C7 · 1 C7 · 60 C7 · 61 C7 | - |
| fronteira de data da busca (2) | hoje fica C16 · ontem sai C16 | - |
| fronteira de marcos (5) | 30 dias C22 · 31 dias C22 · 7 dias C23 · 8 dias C23 · cruza vários C25 | - |
| estados que não geram aviso (3) | artigo `ARQUIVADO` C32 · artigo `RASCUNHO` C32 · documento `ARQUIVADO` C32 | - |
| estados que recusam renovar (2) | artigo `ARQUIVADO` C12 · documento `ARQUIVADO` C13 | - |
| doors do Landing (3) | door 1 C6 e C8 · door 2 C34 · door 3 C36 e C38 e C47 | - |
| entidades de Relations (3) | `ARTIGO`-`AVISO_VALIDADE` C26 · `DOCUMENTO`-`AVISO_VALIDADE` C26 · `INSTITUICAO`-`AVISO_VALIDADE` C35 | - |
| startup config: `Prazos:ValidadeConhecimentoMeses` (2 assemblies) | `Program` da API C7 · `EsclariFactory` dos testes C7 | - |
| estados de tela da seção do painel (3) | vazio C43 · erro C44 · carregando C45 | - |
| ações de lista (3) | etiqueta C39 · filtro C40 · renovar C41 | - |
| viewports (2) | 360×780 C46 · 844×390 C46 | - |

- Claims naming a status code, route or response shape: C9 a C15, C36 a C38, C47 - cada um tem uma prova que cruza a fronteira HTTP
- Nenhum outro check afirma mais que o caso único que a prova exercita

## Test policy

Rows already answered by the repo (`AGENTS.md`: regra de domínio com teste unitário na entidade, integração na rota HTTP) cobrem as camadas tocadas; nenhuma linha nova.

## Swept

- validation: C7, C12, C13, C4 (e C3 para fronteira de fuso)
- failure modes: C31, C29, C44
- idempotency: C27, C9 (renovar duas vezes só repete a data), C34
- authorization: C15, C37, C35
- concurrency: C30
- data lifecycle: C6, C5, C19, C32
- dependency failure: C31 (e a fila `EmailSaida` já repete com atraso crescente, existing)
- state transitions: C21, C12, C13
- observability: C33, C29

## Handoff

- Medido com `wc -c` dos arquivos que cada slice toca, dividido por 4: S1 e S2 entram em `Conhecimento` e `Dados/Artigos`/`Dados/Documentos` (~9k) mais testes novos (~6k) e a migration = ~18k; S3 em `BuscarFontes`, `Assistente` e `Mcp` = ~5k; S4 em `Agendador`, `Prazos`, `Dados/Emails` e uma tarefa nova = ~8k; S5 no `web/src/conhecimento` (~18k), `web/src/indicadores` (~24k) e e2e = ~30k
- Total ~66k, abaixo do orçamento de 150k (AGENTS.md, `budget: 150k`) - um construtor, sem pergunta
- Mechanism: one builder
