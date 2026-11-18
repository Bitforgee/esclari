# Artigo a partir da resposta de um chamado - checks

Profile: ui
Plan: `.specs/features/artigo-a-partir-do-chamado/plan.md`

34 checks em 3 slices · 1 one-way door · 0 perguntas abertas (plano aprovado pelo dono em 17/11/2026: "aprovo o plano artigo-a-partir-do-chamado pode iniciar a construção"; AD-070)

**Emenda de construção E1 (C31; aceita no AD-071).** "O formulário ocupa a largura da tela" vale em 360×780, como o Observable do plano diz. Em 844×390 o `Editor` existente abre a janela de 512 px (o corte dele é `max-width: 767px`), com rolagem vertical e "Salvar rascunho" no fim, visível ao rolar.

**Emenda de construção E2 (C21; aceita no AD-071).** `artigoDaResposta` também entra na linha `POST /api/chamados/{numero}/duplicado` do `sucessos.txt`, porque o chamado marcado como duplicado fica `FECHADO` e `FECHADO` pode originar artigo (AC 1). O retrato só acrescenta `origemChamado` nas rotas de artigo, `artigoDaResposta` em `resolver` e `duplicado` e a linha da rota nova.

**Emenda de construção E3 (AC 12, C30; aceita no AD-071).** A tela `Artigos` não tem filtro de estado. O rascunho aparece pelo selo "Rascunho" da linha, que o C30 prova; filtro de estado novo fica fora desta feature.

**Como as provas rodam.** Na raiz `D:\esclari`, salvo `cd web &&`. API: `docker compose up -d postgres mailpit minio` e uma execução de `dotnet test` por vez. Vitest: `cd web && npx vitest run <arquivo> -t "<nome>"`. E2E: `docker compose up -d --build` e depois `cd web && npx playwright test <spec> --workers=1`. Um filtro que não seleciona teste sai com 0 (L-002): conferir a contagem de testes executados. `mutator` está suspenso (AD-025). Os nomes de teste abaixo são o contrato: os testes são escritos a partir dos checks, antes da implementação, e cada um nasce em `Esclari.Api.Tests` ou `web/src` com o nome citado. O relógio dos testes da API é o de `EsclariFactoryValidade` (`2026-11-17T12:00Z`, hoje `2026-11-17`). Nos exemplos, o chamado nasce de `Balcao.Chamado` (resolvido pela atendente A com "Boleto reemitido.") e o corpo válido é `{ titulo: "Segunda via do boleto", perguntas: ["como tiro a segunda via do boleto?"], resposta: "Boleto reemitido.", links: [] }`.

**Notas de precisão (L-021, tiradas do código).**
- A lista de artigos é `GET /api/admin/artigos` (`Conhecimento/Artigos/ListarArtigos.cs`, grupo `MapAdmin`), não `GET /api/artigos` como escreve o Impact do plano; é a mesma emenda E1 proposta em `validade-do-conhecimento`. Os checks usam o caminho real.
- A tela `Artigos` não tem filtro de estado (só o filtro de validade, `Validade.tsx`); o AC 12 diz "o filtro de estado existente mostra os rascunhos". O que existe é o selo de estado de cada linha (`SeloEstado`, "Rascunho"). O C30 prova o selo; um filtro de estado novo fica fora desta feature (o plano diz que `Artigos` só ganha a etiqueta).
- Quem vê o chamado sai de `Chamados/Consulta/ChamadoDoUsuario.cs`: o solicitante tem o papel `ALUNO`; o atendente de um setor do chamado tem `ATENDENTE` (ou `LIDER` se o setor é de liderança, `CursoId` não nulo); o `ADMINISTRADOR` sem vínculo tem `LEITURA` (AD-066/AD-067); qualquer outro recebe `404`. O responsável é sempre atendente do setor (o encaminhamento tira o responsável), então "atendente do setor ou responsável" do AC 1 é o papel `ATENDENTE` ou `LIDER`. Os status do AC 5 cabem nesse acesso sem mudar o comportamento de rota existente.
- A tela só sabe se o chamado já originou artigo se a API disser: o detalhe do chamado (`DetalheChamado`, devolvido por `GET /api/chamados/{numero}` e pelas 10 ações) ganha o campo aditivo `artigoDaResposta` (`PODE_CRIAR`/`CRIADO`), omitido quando nulo. O Surface do plano não lista esse campo; ele é aditivo, não é porta, e fica registrado no AD-070 (C20).
- "o aviso do AC 10" do AC 11 é lido como o texto "Artigo criado: ESC-NNNNNN" que substitui o botão; o aviso passageiro (toast) só sai no momento da criação.

## Checks

### S1 - O atendente transforma a resposta em rascunho (API) · 14 arquivos · ~70 KB · ~18k

**C1** - A atendente responsável chama `POST /api/chamados/{numero}/artigo` com o corpo válido sobre o chamado `RESOLVIDO` e recebe `201` com `Location` `/api/admin/artigos/{id}`, `estado` `RASCUNHO`, `categoriaId` igual à categoria do chamado, `origemChamado` igual ao protocolo `ESC-NNNNNN` do chamado, `validoAte` `"2027-11-17"`, `validade` `VIGENTE` e `versao.titulo` `"Segunda via do boleto"`; no banco o artigo tem `origem_chamado_id` = id do chamado (AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Atendente_responsavel_cria_rascunho_do_chamado_resolvido"`

**C2** - O mesmo pedido sobre um chamado `FECHADO` responde `201` com `estado` `RASCUNHO` e `origemChamado` igual ao protocolo (AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Chamado_fechado_tambem_origina"`

**C3** - A atendente B, do setor do chamado e sem ser a responsável, recebe `201` (AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Atendente_do_setor_sem_ser_responsavel_cria"`

**C4** - O atendente de um setor de liderança (papel `LIDER` no chamado) recebe `201` sobre o chamado `RESOLVIDO` da liderança (AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Lider_do_setor_cria"`

**C5** - Um chamado reclassificado de "Boleto" para "Matrícula" depois de resolvido gera o artigo com `categoriaId` de "Matrícula" e `categoria` `"Matrícula"`, não a categoria original (AC 1, Assumptions)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Categoria_e_a_atual_do_chamado"`

**C6** - Com título vazio a resposta é `400` com `errors.titulo`; perguntas `[]` ou `["  "]` dão `400` com `errors.perguntas` "Informe ao menos uma pergunta relacionada."; resposta vazia e resposta de 8001 caracteres dão `400` com `errors.resposta` "Informe a resposta, com até 8000 caracteres."; resposta de 8000 caracteres responde `201`; link `ftp://portal` dá `400` com `errors.links` "Todo link deve começar com https:// ou http://."; nenhum caso recusado cria artigo (AC 2)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Mesmas_regras_do_artigo_do_administrador"`

**C7** - Sobre chamado `ABERTO`, `EM_ATENDIMENTO` e `AGUARDANDO_ALUNO` a resposta é `409` com `detail` "O chamado ainda não foi resolvido." e nenhum artigo é criado (AC 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Chamado_nao_resolvido_responde_409"`

**C8** - A segunda chamada para o mesmo chamado, com outro título, responde `409` com `detail` "Este chamado já originou o artigo “Segunda via do boleto”." e a instituição continua com 1 artigo de origem nesse chamado (AC 4, door 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Segundo_artigo_do_mesmo_chamado_responde_409"`

**C9** - Duas chamadas simultâneas (`Task.WhenAll`) para o mesmo chamado terminam com uma `201` e uma `409` com o `detail` do C8, e o banco fica com 1 artigo com esse `origem_chamado_id` (AC 4, concorrência)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Duas_chamadas_simultaneas_criam_um_artigo"`

**C10** - Número inexistente, chamado de outra instituição e atendente de outro setor (`Balcao.Alheio`) recebem `404` com `detail` "Chamado não encontrado.", e nenhum artigo é criado (AC 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Chamado_que_o_usuario_nao_ve_responde_404"`

**C11** - O `ADMINISTRADOR` sem vínculo com o setor (papel `LEITURA`) e o aluno dono do chamado recebem `403`, e nenhum artigo é criado (AC 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Administrador_e_aluno_recebem_403"`

**C12** - A chamada sem sessão recebe `401` (AC 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Sem_sessao_responde_401"`

**C13** - O CPF válido `111.444.777-35` no título, `11144477735` numa pergunta e `111.444.777-35` na resposta dão cada um `400` com `detail` "Remova o CPF do texto antes de salvar." em `errors.titulo`, `errors.perguntas` e `errors.resposta`, sem criar artigo; o número `111.444.777-36` (dígito errado) na resposta responde `201`. `Cpf.NoTexto` acha CPF válido com e sem pontuação no meio de uma frase e não acha número de 11 dígitos inválido nem um CPF colado a outros dígitos (AC 6)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Cpf_no_texto_responde_400"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CpfValorTests.No_texto_acha_cpf_valido_formatado_ou_nao"`

**C14** - A ordem das recusas é acesso, campos, CPF, estado e duplicidade: o aluno com corpo inválido num chamado aberto recebe `403`; a atendente com título vazio num chamado aberto recebe `400`; com CPF na resposta num chamado aberto recebe `400`; com corpo válido num chamado aberto recebe `409` "O chamado ainda não foi resolvido."; com título vazio num chamado que já originou artigo recebe `400` (AC 2 a AC 6, L-023)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Recusas_na_ordem_acesso_campos_estado"`

**C15** - Ao criar o rascunho sai 1 log `Information` com o modelo "Rascunho de artigo criado a partir do chamado {Numero} (instituição {InstituicaoId})", `Numero` = protocolo e `InstituicaoId` = id da instituição, e a mensagem formatada não contém o título nem a resposta (AC 7)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Registra_log_sem_o_texto"`

**C16** - `Artigo.DoChamado(7, categoriaId, versão 1, validade)` nasce com `Estado` `RASCUNHO`, `OrigemChamadoId` 7, `CategoriaId` informado, `VisivelDe` e `VisivelAte` nulos e `ValidoAte` = `validade.Ate`; `Artigo.Criar` continua com `OrigemChamadoId` nulo (Relations, domínio)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDominioTests.Do_chamado_nasce_rascunho_com_a_origem"`

**C17** - Depois da migration a coluna `artigo.origem_chamado_id` é `bigint` anulável, o índice único tem `WHERE (origem_chamado_id IS NOT NULL)` no `pg_indexes.indexdef`, dois artigos com `origem_chamado_id` nulo convivem e um segundo artigo com o mesmo `origem_chamado_id` falha com `23505` (door 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Indice_unico_parcial_da_origem"`

**C18** - O rascunho criado do chamado tem 0 `TrechoFonte` da sua versão; depois de `POST /api/admin/artigos/{id}/ativar` pelo administrador responde `200` com `estado` `ATIVO` e passa a ter 1 `TrechoFonte` (Flow passo 4)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Rascunho_so_entra_na_base_depois_de_ativar"`

**C19** - `GET /api/admin/artigos` traz `origemChamado` `"ESC-NNNNNN"` no artigo criado do chamado e `origemChamado` `null` no artigo criado pelo administrador (AC 12, Impact)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Lista_traz_a_origem_do_chamado"`

**C20** - `GET /api/chamados/{numero}` para a atendente traz `artigoDaResposta` `"PODE_CRIAR"` no chamado `RESOLVIDO` sem artigo e `"CRIADO"` depois da criação; no chamado `EM_ATENDIMENTO`, para o aluno dono e para o administrador em `LEITURA` o campo não aparece no JSON (AC 8, AC 11; campo aditivo, AD-070)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArtigoDoChamadoTests.Detalhe_diz_se_pode_criar_ou_ja_criou"`

**C21** - `Retratos/rotas.txt` ganha só a linha `POST /api/chamados/{numero}/artigo anonimo=False autorizacao=True limite=-`; `Retratos/sucessos.txt` ganha a linha `POST /api/chamados/{numero}/artigo atendente 201` e, nas linhas existentes, só os campos `origemChamado` das rotas de artigo e `artigoDaResposta` do detalhe da equipe em chamado resolvido (Impact, door 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~RetratoTests"`

**C22** - `dotnet build Esclari.slnx` sai com 0 (CA1502 abaixo de 11, AD-018); `ArquiteturaTests` passa com o caso de uso novo em `Conhecimento/Artigos/CriarArtigoDoChamado.cs` (um arquivo, uma mensagem, um handler, um `Map*`, `Entrada` própria, resposta `ArtigoResposta`, `Artigo.DoChamado` no inventário do domínio, nenhuma referência nova entre fatias); `MediatorTests` mostra que `AgirNoChamado` segue só nas 10 ações (AD-056, AD-070)
Proof: `dotnet build Esclari.slnx`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArquiteturaTests|FullyQualifiedName~MediatorTests"`

### S2 - A tela leva o atendente até o rascunho e o administrador até a ativação · 8 arquivos · ~60 KB · ~15k

**C23** - Com `artigoDaResposta` `PODE_CRIAR` a atendente vê o botão "Criar artigo a partir desta resposta" nos chamados `RESOLVIDO` e `FECHADO`; sem o campo (chamado `EM_ATENDIMENTO`, aluno, administrador em `LEITURA`) o botão não existe (AC 8, AC 5)
Proof: `cd web && npx vitest run src/chamados/artigoDaResposta.test.tsx -t "botao so no chamado resolvido"`

**C24** - Acionar o botão abre o formulário "Criar artigo a partir desta resposta" com "Título" e "Perguntas relacionadas (uma por linha)" vazios, "Resposta" preenchida com a última mensagem pública da equipe (entre duas respostas públicas da equipe, uma nota interna depois delas, uma mensagem do aluno e uma do "Sistema", vale a segunda resposta pública), e a faixa "Remova nomes, CPF e matrícula antes de salvar" antes do primeiro campo (AC 9)
Proof: `cd web && npx vitest run src/chamados/artigoDaResposta.test.tsx -t "formulario preenchido com a ultima resposta publica"`

**C25** - "Salvar rascunho" envia `POST /api/chamados/ESC-000001/artigo` com `{ titulo, perguntas: [uma por linha, sem vazias], resposta, links: [um por linha] }`, mostra o aviso "Rascunho criado. O administrador precisa ativar antes de o assistente usar." na região `Notificações`, fecha o formulário e troca o botão pelo texto "Artigo criado: ESC-000001" (AC 10)
Proof: `cd web && npx vitest run src/chamados/artigoDaResposta.test.tsx -t "salvar cria o rascunho"`

**C26** - Com `artigoDaResposta` `CRIADO` a tela não mostra o botão e mostra "Artigo criado: ESC-000001" (AC 11)
Proof: `cd web && npx vitest run src/chamados/artigoDaResposta.test.tsx -t "chamado que ja originou artigo"`

**C27** - Se o `POST` responde `400` com `errors.titulo` o formulário continua aberto com o texto digitado nos campos e mostra a mensagem do campo; se responde `409` com `detail` "Este chamado já originou o artigo “Boleto”." a mensagem aparece no formulário aberto (AC 13)
Proof: `cd web && npx vitest run src/chamados/artigoDaResposta.test.tsx -t "falha mantem o formulario"`

**C28** - Enquanto o `POST` não responde, o botão de salvar fica desabilitado com `aria-busy="true"` e o texto "Salvando…" (AC 14)
Proof: `cd web && npx vitest run src/chamados/artigoDaResposta.test.tsx -t "salvar desabilitado durante a criacao"`

**C29** - O detalhe da equipe em `EM_ATENDIMENTO` com o campo ausente não ganha nenhum texto novo, e o inventário de textos do detalhe (`textos detalhe do chamado`) continua passando (Observable: densidade)
Proof: `cd web && npx vitest run src/chamados/textos.test.tsx -t "textos detalhe do chamado"`

**C30** - Em `Artigos`, a linha do artigo com `origemChamado` `ESC-000123` mostra "Origem: ESC-000123" na célula do título, junto da etiqueta de validade; o artigo sem origem não mostra "Origem:"; a linha do rascunho mostra o selo "Rascunho" e oferece "Ativar" no menu (AC 12; ver nota de precisão sobre o filtro de estado)
Proof: `cd web && npx vitest run src/conhecimento/origem.test.tsx -t "etiqueta de origem"`

**C31** - Em 360×780 e em 844×390 o detalhe do chamado resolvido não tem rolagem horizontal e o botão "Criar artigo a partir desta resposta" tem pelo menos 44 px de altura; o formulário aberto ocupa a largura da tela, a faixa de aviso fica acima do campo "Título" e o botão "Salvar rascunho" fica visível depois de rolar até o fim e tem pelo menos 44 px; com a altura reduzida a 360×400 (teclado aberto) o campo "Resposta" em foco fica visível e "Salvar rascunho" é alcançável rolando; o toque no botão (`tap`) abre o formulário (Observable: celular 360×780, celular deitado 844×390, teclado aberto, toque)
Proof: `cd web && npx playwright test e2e/artigo-do-chamado.spec.ts --workers=1`

### S3 - Transversais · 3 arquivos · ~5 KB · ~1k

**C32** - `.specs/GLOSSARIO.md` tem a linha do termo novo `origem do artigo` (Impact, domain)
Proof: `git grep -q "origem do artigo" -- .specs/GLOSSARIO.md`

**C33** - A suíte inteira da API e a do web passam sem asserção enfraquecida, e `npm --prefix web run build` sai com 0
Proof: `dotnet test Esclari.Api.Tests`
Proof: `npm --prefix web test`
Proof: `npm --prefix web run build`

**C34** - `crapper` não acha função com CRAP 10 ou mais em `web/src` e `dryer` não acha candidato a 0.8 no escopo do E2 de `filtros-e-relatorios` (AD-018, AD-067; `mutator` suspenso pelo AD-025)
Proof: `cd web && npm run coverage && crapper --source-root src --use-existing-coverage --threshold 10`
Proof: `cd web && dryer -s src/acesso -s src/administracao -s src/assistente -s src/chamados -s src/conhecimento -s src/indicadores -s <cada .ts/.tsx não teste direto em web/src> --threshold 0.8`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| `POST /api/chamados/{numero}/artigo` statuses (6) | 201 C1 · 400 C6 e C13 · 401 C12 · 403 C11 · 404 C10 · 409 C7 e C8 | - |
| papéis no chamado (5) | `ATENDENTE` responsável C1 · `ATENDENTE` sem ser responsável C3 · `LIDER` C4 · `LEITURA` C11 · `ALUNO` C11 | - |
| quem não vê o chamado (3) | inexistente C10 · outra instituição C10 · atendente de outro setor C10 | - |
| estados do chamado (5) | `ABERTO` C7 · `EM_ATENDIMENTO` C7 · `AGUARDANDO_ALUNO` C7 · `RESOLVIDO` C1 · `FECHADO` C2 | - |
| regras de `ValidacaoDeArtigo` (5) | título C6 · perguntas C6 · resposta vazia C6 · resposta 8000/8001 C6 · links C6 | - |
| campos com CPF (3) | `titulo` C13 · `perguntas` C13 · `resposta` C13 | - |
| formas de CPF (4) | formatado C13 · sem pontuação C13 · dígito errado aceito C13 · colado a outros dígitos C13 | - |
| ordem das recusas (5 pares) | 403 antes de 400 C14 · 400 antes de 409 estado C14 · CPF antes de 409 C14 · 409 estado C7 · 400 antes de 409 duplicidade C14 | - |
| `artigoDaResposta` (3) | `PODE_CRIAR` C20 e C23 · `CRIADO` C20 e C26 · ausente C20 e C23 | - |
| `origemChamado` (2) | protocolo C19 e C30 · `null` C19 e C30 | - |
| door do Landing (1) | door 1 C8, C9 e C17 | - |
| entidades de Relations (1) | `CHAMADO`-`ARTIGO` (0..1) C16 e C17 | - |
| estados da tela do formulário (4) | aberto C24 · salvando C28 · erro C27 · salvo C25 | - |
| viewports e entradas (4) | 360×780 C31 · 844×390 C31 · teclado aberto C31 · toque C31 | - |

- Claims naming a status code, route or response shape: C1 a C12, C14, C18 a C21 - cada um tem uma prova que cruza a fronteira HTTP
- Nenhum outro check afirma mais que o caso único que a prova exercita

## Test policy

Rows already answered by the repo (`AGENTS.md`: regra de domínio com teste unitário na entidade, integração na rota HTTP) cobrem as camadas tocadas. A detecção de CPF em texto livre é regra pura sobre `Cpf` e fica no teste unitário sem banco (`CpfValorTests`), e a rota prova só o campo que ela recusa (C13). Sem linha nova para o guia do repositório.

## Swept

- validation: C6, C13, C14
- failure modes: C27, C9 (a criação é um único `SaveChanges`; a falha do índice vira `409`)
- idempotency: C8, C9, C17
- authorization: C10, C11, C12, C23
- concurrency: C9
- data lifecycle: n/a - o artigo não depende do chamado depois de criado (FK `ON DELETE SET NULL`, C17) e a validade de 1 ano vem de `validade-do-conhecimento` (C1)
- dependency failure: n/a - nenhuma dependência externa; a indexação só acontece na ativação (C18)
- state transitions: C7, C2, C1, C18
- observability: C15

## Handoff

- Medido com `wc -c` dos arquivos que cada slice toca, dividido por 4: S1 em `Conhecimento/Artigos` (~4k), `Dados/Artigos` e `Dados/Usuarios/Cpf.cs` (~2k), `Chamados/Consulta/DetalheChamado.cs` (~2k), migration, testes novos (~6k) e inventários de `ArquiteturaTests`/`RetratoTests` (~4k) = ~18k; S2 em `web/src/chamados` (`ChamadoDetalhe.tsx` ~6k e um componente novo), `web/src/conhecimento/Artigos.tsx` (~3k), testes e e2e = ~15k; S3 = ~1k
- Total ~34k, abaixo do orçamento de 150k (AGENTS.md, `budget: 150k`) - um construtor, sem pergunta
- Mechanism: one builder
