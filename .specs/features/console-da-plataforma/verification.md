# Console da plataforma verification

**Verdict**: PASS
**Profile**: standard
**Diff range**: 3c2c49f..HEAD
**Round**: 2 - scoped (round 1 below is kept as history)
**Verifier**: independent sub-agent (author != verifier)

Rodada única e enxuta, por decisão do dono (18/11): uma invocação por alvo de prova, três falhas injetadas. Prova e evidência foram tiradas de `HEAD` (374db24).

## Binding sources

Nenhuma fonte vinculante no plano (perfil `standard`, não `ui`); o plano cita só pesquisa de apoio (Microsoft Learn, RFC 6238).

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| nenhuma fonte marcada como binding | n/a | none | - |

## Checks

Provas API: `dotnet build Esclari.slnx` (0 erros) e `dotnet test Esclari.Api.Tests --no-build` com TRX, uma única invocação: 1269 aprovados, 1 falha (`NomeDoProdutoTests`, ver Deviations), 1270 no total. Cada teste nomeado abaixo consta como `Passed` no TRX. Prova web: `cd web; npx vitest run --reporter=verbose` uma vez: 129 arquivos, 1135 testes passaram. Os `-t "<nome>"` de C32, C42 a C48 e C54 não casam com nenhum teste existente (ver Deviations); o teste mais próximo está citado e passa.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | operador sem `instituicao_id`, fora de `/api/admin/usuarios` | `dotnet test --filter PlataformaAcessoTests.Operador_nao_pertence_a_instituicao` Passed | `Esclari.Api.Tests/PlataformaAcessoTests.cs:46` - `Assert.DoesNotContain("instituicao_id", colunas)` e `:48` | PASS |
| C2 | comando cria operador sem senha, audita, link de 60 min | `PlataformaComandoTests.Criar_imprime_o_link_de_60_minutos_e_audita` Passed | `PlataformaComandoTests.cs:40` - `Assert.Equal(f.Relogio.GetUtcNow().UtcDateTime.AddMinutes(60), operador.ConviteExpiraEm)`; `:41` auditoria 1 | PASS |
| C3 | convite novo invalida o anterior; com senha sai 1 e "Esse operador já existe." | `PlataformaComandoTests.Convite_novo_invalida_o_anterior_e_operador_com_senha_e_recusado` Passed | `PlataformaComandoTests.cs:56` (token antigo 400), `:60-61` (código 1 e frase) | PASS |
| C4 | `--desativar`, `--reativar`, `--redefinir-2fa` e as recusas | `PlataformaComandoTests.Desativar_reativar_e_redefinir_2fa_com_as_recusas` Passed | `PlataformaComandoTests.cs:83` (3 auditorias), `:86-87` inexistente, `:91-94` último ativo sem gravar | PASS |
| C5 | `definir-senha` 204, hash gravado, token de uso único | `PlataformaAcessoTests.Definir_senha_204_e_token_de_uso_unico` Passed | `PlataformaAcessoTests.cs:61-62` (204 e depois 400), `:66` `Assert.Null(linha.ConviteHash)` | PASS |
| C6 | `definir-senha` 400 nos quatro casos | `PlataformaAcessoTests.Definir_senha_400_nos_quatro_casos` Passed | `PlataformaAcessoTests.cs:77-81` (inválido, 7, 129, vencido) e `:82` | PASS |
| C7 | `entrar` com 2º fator: `CODIGO`, cookie de etapa, sem sessão | `PlataformaAcessoTests.Entrar_com_segundo_fator_pede_o_codigo` Passed | `PlataformaAcessoTests.cs:94` `"CODIGO"`, `:96-97` etapa sim, sessão não | PASS |
| C8 | `CADASTRAR`, segredo base32 de 32, `uri` literal | `PlataformaAcessoTests.Entrar_sem_segundo_fator_devolve_segredo_e_uri` Passed | `PlataformaAcessoTests.cs:110` `^[A-Z2-7]{32}$`, `:111` `otpauth://totp/Esclari:{email}?secret={segredo}&issuer=Esclari` | PASS |
| C9 | 401 com a mesma frase nos três casos | `PlataformaAcessoTests.Entrar_401_com_a_mesma_frase_nos_tres_casos` Passed | `PlataformaAcessoTests.cs:132-133` `Assert.Equal("E-mail ou senha inválidos.", ...)` | PASS |
| C10 | `codigo` nos passos -1, 0, +1 abre a sessão, grava passo e auditoria, segredo no cadastro | `PlataformaAcessoTests.Codigo_abre_a_sessao_nos_tres_passos_e_grava_o_segredo_no_cadastro` (3 casos) e `Codigo_no_cadastro_grava_o_segredo_protegido` Passed | `PlataformaAcessoTests.cs:150-155` (204, cookie, etapa apagada, `TotpUltimoPasso`, OPERADOR_ENTROU), `:171` segredo protegido | PASS |
| C11 | `codigo` 401: errado, repetido, sem etapa, etapa vencida | `PlataformaAcessoTests.Codigo_401_errado_repetido_sem_etapa_e_etapa_vencida` Passed | `PlataformaAcessoTests.cs:182`, `:188-189`, `:192`, `:198-199` | PASS |
| C12 | 400 para código de 5 dígitos, com letra e e-mail vazio | `PlataformaAcessoTests.Corpo_invalido_400` Passed | `PlataformaAcessoTests.cs:209-211` | PASS |
| C13 | 6ª chamada em `entrar`, `codigo`, `definir-senha` dá 429 | `PlataformaLimiteTests.Limitador_auth_429_na_sexta_tentativa` Passed | `PlataformaLimiteTests.cs:15-17` `Assert.Equal(HttpStatusCode.TooManyRequests, ...)` | PASS |
| C14 | sessão cai aos 31 min sem uso e aos 8 h 1 min com uso | `PlataformaAcessoTests.Sessao_expira_aos_30_minutos_sem_uso_e_aos_8_horas` Passed | `PlataformaAcessoTests.cs:219` (parada 401), `:225` (ativa 200), `:228` (8 h 1 min 401) | PASS |
| C15 | `sair` 204 e expira os dois cookies | `PlataformaAcessoTests.Sair_apaga_os_dois_cookies` Passed | `PlataformaAcessoTests.cs:239-240` | PASS |
| C16 | `eu` devolve exatamente `id`, `nome`, `email` | `PlataformaAcessoTests.Eu_devolve_so_id_nome_e_email` Passed | `PlataformaAcessoTests.cs:253` `Assert.Equal(["id", "nome", "email"], ...)` | PASS |
| C17 | cada cookie só vale no próprio mundo | `PlataformaAcessoTests.Cada_cookie_so_vale_no_proprio_mundo` Passed | `PlataformaAcessoTests.cs:264` e `:266` (401 nos dois sentidos), `:268` (200 do próprio) | PASS |
| C18 | operador desativado perde a sessão na requisição seguinte | `PlataformaAcessoTests.Operador_desativado_perde_a_sessao_na_requisicao_seguinte` Passed | `PlataformaAcessoTests.cs:275` (200) e `:283` (401) | PASS |
| C19 | contagens e datas da saúde batem com os dados semeados | `PlataformaSaudeTests.Contagens_e_datas_batem_com_os_dados_semeados` Passed | `PlataformaSaudeTests.cs:64-71` (2, 5, 3, 4, 0,25, 2, 1, `agora.AddDays(-1)`) | PASS |
| C20 | instituição vazia devolve nulos | `PlataformaSaudeTests.Instituicao_vazia_devolve_nulos` Passed | `PlataformaSaudeTests.cs:82` `Assert.Equal(JsonValueKind.Null, ...)` | PASS |
| C21 | `emailsComFalha` e `conteudoVencido` contam só o devido | `PlataformaSaudeTests.Emails_com_falha_e_conteudo_vencido_so_contam_o_que_deve` Passed | `PlataformaSaudeTests.cs:105-106` (1 e 2) | PASS |
| C22 | `saude` com as dez chaves e sem dado pessoal | `PlataformaSaudeTests.Saude_so_tem_contagens_uma_fracao_e_uma_data` Passed | `PlataformaSaudeTests.cs:121` `Assert.Equal(ChavesDaSaude, ...)`, `:123-125` | PASS |
| C23 | `atencao` nos limites de cada etiqueta | `PlataformaSaudeTests.Atencao_nos_limites_de_cada_etiqueta` Passed | `PlataformaSaudeTests.cs:139-140` (14 sim, 13 não), `:142-144` (20 com 7 sim; 19 não), `:146-152` | PASS |
| C24 | ordem atenção, ativas, desativadas, por nome | `PlataformaSaudeTests.Ordem_da_lista_atencao_ativas_desativadas` Passed | `PlataformaSaudeTests.cs:169` lista literal de 5 nomes | PASS |
| C25 | `situacao` nos três valores | `PlataformaSaudeTests.Situacao_nos_tres_valores` Passed | `PlataformaSaudeTests.cs:187-190` | PASS |
| C26 | rotas de hoje mantêm contrato; sem sessão dão 401 | `PlataformaInstituicoesTests.Rotas_de_hoje_mantem_o_contrato_com_a_sessao_da_plataforma` e `Rotas_sem_sessao_da_plataforma_401` Passed | `PlataformaInstituicoesTests.cs:41-48` (statuses, Location, chaves), `:60-68` (401 em cada rota) | PASS |
| C27 | slug `plataforma` recusado com 400 no campo `slug` | `PlataformaInstituicoesTests.Slug_plataforma_e_reservado` Passed | `PlataformaInstituicoesTests.cs:81-82` | PASS |
| C28 | desativar exige motivo de 3 a 200 | `PlataformaInstituicoesTests.Desativar_exige_motivo_de_3_a_200_caracteres` Passed | `PlataformaInstituicoesTests.cs:94` (400 para "", "ab", 201), `:101-103`; o 200 aceito vem de `PlataformaAuditoriaTests.cs:109`; o limite inferior de 3 caracteres aceito não é asserido (ver Deviations) | PASS |
| C29 | detalhe 200 com e-mail mascarado; 404 | `PlataformaInstituicoesTests.Detalhe_200_mascarado_e_404` Passed | `PlataformaInstituicoesTests.cs:122-125` | PASS |
| C30 | `PUT` altera só nome e fuso | `PlataformaInstituicoesTests.Editar_altera_so_nome_e_fuso` Passed | `PlataformaInstituicoesTests.cs:140` `Assert.Equal(("Nome Novo da Faculdade", "America/Sao_Paulo", i.Slug, i.Cor), ...)` | PASS |
| C31 | `PUT` 400 por campo e 404 | `PlataformaInstituicoesTests.Editar_400_por_campo_e_404` Passed | `PlataformaInstituicoesTests.cs:158-163` | PASS |
| C32 | ao criar, o operador vê o passo de importar a carga com as contagens, na mesma tela | `vitest -t "criar leva a importar a carga na mesma tela"` não casa com nenhum teste | `web/src/plataforma/instituicoes.test.tsx:110` (criar só fecha o diálogo, avisa e recarrega) e `:168` (importar é ação da linha, sem encadeamento com criar) | PASS (re-run in round 2: ver secao Round 2; resultado da rodada 1 era FAIL) |
| C33 | cada uma das oito ações grava uma linha | `PlataformaAuditoriaTests.Cada_acao_grava_uma_linha_na_mesma_transacao` Passed | `PlataformaAuditoriaTests.cs:45` `Assert.Equal(AcoesDaPlataforma.Todas.Order(), linhas.Select(l => l.Acao).Order())`, `:47-48` | PASS |
| C34 | criação que falha não deixa linha | `PlataformaAuditoriaTests.Acao_que_volta_nao_deixa_linha` Passed | `PlataformaAuditoriaTests.cs:59`, `:66` | PASS |
| C35 | auditoria só inclusão | `PlataformaAuditoriaTests.Auditoria_so_inclusao` Passed | `PlataformaAuditoriaTests.cs:79-81` `Assert.Equal(["GET"], rotas)`, `Assert.Empty(setters)` | PASS |
| C36 | `resumo` das oito ações sem CPF, senha, token, até 200 | `PlataformaAuditoriaTests.Resumo_sem_dado_pessoal_e_ate_200_caracteres` Passed | `PlataformaAuditoriaTests.cs:99` `Assert.True(linhas.Count >= 4)`, `:102-107` e `:109`. O teste exercita 4 das 8 ações (criada, reenviado, desativada, operador alterado). Faltam `OPERADOR_ENTROU`, `INSTITUICAO_ALTERADA`, `INSTITUICAO_REATIVADA` e `CARGA_IMPORTADA` | PASS (re-run in round 2: ver secao Round 2; resultado da rodada 1 era FAIL) |
| C37 | 50 por página, mais novo primeiro, total, filtro | `PlataformaAuditoriaTests.Pagina_de_50_do_mais_novo_ao_mais_antigo_com_filtro` Passed | `PlataformaAuditoriaTests.cs:129-137` | PASS |
| C38 | `pagina=0` e `instituicao` inexistente dão 400 | `PlataformaAuditoriaTests.Pagina_e_instituicao_invalidas_400` Passed | `PlataformaAuditoriaTests.cs:145-146` | PASS |
| C39 | migration apaga só as linhas `OPERADOR` | `MigracaoOperadorTests.Apaga_so_as_linhas_operador` Passed | `MigracaoOperadorTests.cs:54` (antes, com 2 `OPERADOR`) e `:58` `Assert.Equal(["ADMINISTRADOR", "ALUNO", "ATENDENTE"], ...)` | PASS |
| C40 | criar usuário com `OPERADOR` dá 400 em `papeis`; `Papeis.Operador` não existe | `PlataformaComandoTests.Papel_operador_nao_existe_mais` Passed | `PlataformaComandoTests.cs:105-108` | PASS |
| C41 | `instituicao operador --slug demo --cpf 123` imprime uso e sai 1 | `PlataformaComandoTests.Instituicao_operador_nao_existe_mais` Passed | `PlataformaComandoTests.cs:118-123` | PASS |
| C42 | menu de administrador de instituição sem "Plataforma" nem "Instituições" | `vitest -t "menu da instituicao nao tem o grupo Plataforma"` não casa com nenhum teste. Teste equivalente que passa: "administrador de instituição não vê grupo Plataforma nem Instituições" | `web/src/plataforma/menu.test.tsx:20-21` `expect(...queryByRole('group', { name: 'Plataforma' })).not.toBeInTheDocument()` | PASS (re-run in round 2: ver secao Round 2; resultado da rodada 1 era FAIL) |
| C43 | entrada em três etapas | `vitest -t "entrada em tres etapas"` não casa. Testes equivalentes passam (3 testes de `entrada no console`) | `web/src/plataforma/entrada.test.tsx:44` (`autocomplete`), `:62-63` (`JBSW Y3DP EHPK 3PXP` e link "Abrir no aplicativo") | PASS (re-run in round 2: ver secao Round 2; resultado da rodada 1 era FAIL) |
| C44 | casca com a marca e só "Instituições" e "Auditoria" | `vitest -t "casca da plataforma com a marca do Esclari e dois itens"` em `instituicoes.test.tsx` não casa. O teste equivalente foi para `menu.test.tsx` e passa | `web/src/plataforma/menu.test.tsx:30-34` e `:34` (`/marca/logo-horizontal.svg`) | PASS (re-run in round 2: ver secao Round 2; resultado da rodada 1 era FAIL) |
| C45 | lista com situação, taxa em % e as cinco etiquetas | `vitest -t "lista com saude e etiquetas"` não casa. Equivalentes passam | `web/src/plataforma/instituicoes.test.tsx:38` (`12,5 %`) e `:54` (as cinco etiquetas em português) | PASS (re-run in round 2: ver secao Round 2; resultado da rodada 1 era FAIL) |
| C46 | estados vazios de instituições e de auditoria | `vitest -t "estados vazios"` não casa. Equivalentes passam | `web/src/plataforma/instituicoes.test.tsx:61-62` e `menu.test.tsx:39` | PASS (re-run in round 2: ver secao Round 2; resultado da rodada 1 era FAIL) |
| C47 | 401 com sessão aberta leva à entrada com "Sua sessão terminou. Entre de novo." | `vitest -t "401 leva a entrada com a frase de sessao encerrada"` não casa. O equivalente está em `instituicoes.test.tsx` e não em `entrada.test.tsx`, e passa | `web/src/plataforma/instituicoes.test.tsx:247-258` | PASS (re-run in round 2: ver secao Round 2; resultado da rodada 1 era FAIL) |
| C48 | desativar pede confirmação, nome e motivo | `vitest -t "desativar pede confirmacao e motivo"` não casa. Equivalente passa | `web/src/plataforma/instituicoes.test.tsx:210-212` (texto com o nome, botão desabilitado sem motivo) | PASS (re-run in round 2: ver secao Round 2; resultado da rodada 1 era FAIL) |
| C49 | porta 1: só as duas tabelas sem `instituicao_id`, FK anulável, índice único | `ArquiteturaTests.Tabelas_da_plataforma_sao_as_unicas_sem_instituicao` Passed | `ArquiteturaTests.cs:50` `Assert.Equal(["auditoria_plataforma", "operador_plataforma"], semInstituicao.Order())`, `:56-57` | PASS |
| C50 | porta 2: atributos dos cookies | `PlataformaAcessoTests.Atributos_dos_cookies_da_plataforma` Passed | `PlataformaAcessoTests.cs:299-304` (`path=/api/plataforma`, `httponly`, `samesite=strict`, `secure`, `max-age=300`) | PASS |
| C51 | porta 3: vetores RFC 6238, janela, passo usado | `dotnet test --filter TotpTests` (14 casos) Passed | `Esclari.Api.Tests/TotpTests.cs` (`Vetores_do_RFC_6238_com_SHA1` 5 casos, `Janela_de_um_passo_para_cada_lado`, `Passo_ja_usado_e_recusado`) | PASS |
| C52 | porta 4: oito ações e `resumo` até 200 | `PlataformaAuditoriaTests.Acoes_fechadas_e_resumo_curto` Passed | `PlataformaAuditoriaTests.cs:156-158` | PASS |
| C53 | porta 5/8: `Plataforma` é fatia; `Instituicoes/Plataforma` não existe | `ArquiteturaTests.Fatias_do_api` Passed e `estrutura.test.ts` "estrutura em fatias" Passed | `ArquiteturaTests.cs:294-300` (`Assert.Equal([.. Fatias.Append("Dados").Order()], pastas)`) e `FatiasPlanas` ganha `Plataforma`; `web/src/test/estrutura.test.ts:7` e `:25`; `Esclari.Api/Instituicoes/Plataforma` não existe (Test-Path = False) | PASS |
| C54 | porta 6: `/plataforma` renderiza antes de `/:slug` | `vitest -t "plataforma nao e uma instituicao"` não casa com nenhum teste | `web/src/App.tsx:57` declara `plataforma` antes de `/:slug` (`:62`); nenhum teste afirma a precedência, e os testes que abrem `/plataforma/...` só a cobrem de forma implícita | PASS (re-run in round 2: ver secao Round 2; resultado da rodada 1 era FAIL) |
| C55 | porta 7: migration no histórico e `Papeis` só com três valores | `PlataformaComandoTests.Papel_operador_nao_existe_mais` Passed | `PlataformaComandoTests.cs:108` `Assert.Equal(["Administrador", "Aluno", "Atendente"], ...)`; migration em `MigracaoOperadorTests.cs:58` | PASS |

## Coverage

Recomputado a partir do código (rotas em `Esclari.Api/Plataforma/*.cs`, `AcoesDaPlataforma`, `Totp.cs`, `SessaoDaPlataforma.cs`) e do plano (`Surface`, `Landing`), não da tabela do autor.

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| linhas da tabela `Surface` (12) | rotas mapeadas na fatia e `rotas.txt` | todas com prova HTTP (C5-C18, C26-C31, C37-C38) | - |
| status de `entrar` (4) | `EntrarNaPlataforma.cs` | 200 C7 C8, 400 C12, 401 C9, 429 C13 | - |
| status de `codigo` (4) | `CodigoDaPlataforma.cs` | 204 C10, 400 C12, 401 C11, 429 C13 | - |
| status de `definir-senha` (3) | `DefinirSenhaDaPlataforma.cs` | 204 C5, 400 C6, 429 C13 | - |
| status de `desativar` (4) | `DesativarInstituicao.cs` | 200 C28, 400 C28, 401 C26, 404 C26 | - |
| ações da auditoria (8) | `AcoesDaPlataforma.cs` | gravação das 8 em C33; ausência de dado pessoal no `resumo` provada para 4 em C36 | - |
| chaves de `saude` (10) | `ConsultaDasInstituicoes.cs` | C19 (8 chaves) e C21 (2 chaves), C22 as dez | - |
| etiquetas de `atencao` (5) | `ConsultaDasInstituicoes.cs` | C23 | - |
| valores de `situacao` (3) | idem | C25 | - |
| bordas do TOTP (4) | `Totp.cs` | C10 (-1, 0, +1), C51 (dois passos fora, passo usado) | - |
| cookies (2) | `SessaoDaPlataforma.cs` | C50 | - |
| tabelas sem `instituicao_id` (2) | modelo EF | C49 | - |
| telas do console (3) | `web/src/plataforma/*.tsx` | entrada C43, lista C45, desativação C48: testes existem e passam, mas as provas nomeadas nos checks não casam | - |

## Test policy rows

`checks.md` não tem seção `Test policy`: sem linhas a julgar; vale a convenção do repositório (xUnit com `WebApplicationFactory` para rota, teste unitário para regra de domínio, Vitest para tela).

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| nenhuma | n/a | n/a | n/a |

## Faults injected

Em `git worktree` descartável (`..\esclari-verify`, removido com `--force`; `git worktree list` confirma que sumiu e o `git status --porcelain` da árvore real só ganhou os arquivos temporários desta verificação). As três falhas rodaram juntas em uma só execução (`TotpTests`, `PlataformaAcessoTests`, `PlataformaAuditoriaTests`, `PlataformaInstituicoesTests`: 6 falhas, 39 aprovados) e cada uma foi atribuída ao teste que a pegou.

| Mutation | Location | Killed |
| --- | --- | --- |
| reuso do passo: `passo > ultimoPasso` virou `passo >= ultimoPasso` | `Esclari.Api/Plataforma/Totp.cs` (linha de `Verificar`) | yes - `TotpTests.Passo_ja_usado_e_recusado` e `PlataformaAcessoTests.Codigo_401_errado_repetido_sem_etapa_e_etapa_vencida` |
| teto absoluto de 8 h dobrado para 16 h | `Esclari.Api/Plataforma/SessaoDaPlataforma.cs` (`Validar`) | yes - `PlataformaAcessoTests.Sessao_expira_aos_30_minutos_sem_uso_e_aos_8_horas` |
| remoção do `db.AuditoriaDaPlataforma.Add` ao desativar | `Esclari.Api/Plataforma/DesativarInstituicao.cs` (`Handle`) | yes - `PlataformaAuditoriaTests.Cada_acao_grava_uma_linha_na_mesma_transacao`, `PlataformaAuditoriaTests.Resumo_sem_dado_pessoal_e_ate_200_caracteres` e `PlataformaInstituicoesTests.Desativar_exige_motivo_de_3_a_200_caracteres` |

## Deviations

1. **Provas web não executam o teste que o check nomeia (C32, C42 a C48, C54).** Os `-t "<nome>"` de `checks.md` não coincidem com nenhum `it(...)` de `web/src/plataforma/`; o Vitest sai com 0 sem rodar nada, o que dá um check verde sem teste. Os testes equivalentes existem e passam (citados na tabela). As amendas de `checks.md` ("Acertos de texto durante o Build") renomearam provas só da API; as da web ficaram sem acerto. Correção: reescrever as linhas `Proof:` para os nomes reais (é acerto de nome, não muda a obrigação) ou renomear os `it`.
2. **C32 sem prova de conteúdo.** O critério 35 do plano pede oferecer, "na mesma tela", a importação da carga ao terminar de criar uma instituição. `instituicoes.test.tsx:110` mostra que criar só fecha o diálogo, avisa e recarrega a lista; a importação é uma ação da linha (`Instituicoes.tsx:188`). Nenhum teste afirma o passo de importar depois de criar.
3. **C36 prova 4 das 8 ações** (ver tabela de Coverage). A afirmação do check é sobre as oito.
4. **`NomeDoProdutoTests.Nenhum_arquivo_rastreado_cita_One_fora_do_historico` falha em `HEAD`** (`Esclari.Api.Tests/NomeDoProdutoTests.cs:86`): `web/src/plataforma/Entrada.tsx:87` (`autoComplete="one-time-code"`) e `web/src/plataforma/entrada.test.tsx:44` casam com o padrão de nome antigo. É teste existente quebrado pela feature, fora dos 55 checks; a suíte da API não está verde (1269 de 1270).
5. **Arquivo estranho commitado**: `instituicoes-painel.png` (83 KB) na raiz, no commit 19f4e50. Não pertence ao plano.
6. **Limite inferior de C28 sem asserção**: nenhum teste aceita um motivo de exatamente 3 caracteres, então um limite deslocado para 4 passaria. Registrado como lacuna de precisão, sem mudar o veredito.
7. **Portas 1 a 8 e amendas de `checks.md`**: conferidas contra o código e consistentes. Porta 1 (`ArquiteturaTests.cs:50-57`), porta 2 (`SessaoDaPlataforma.cs`: `Path=/api/plataforma`, `HttpOnly`, `SameSite=Strict`, `SecurePolicy=SameAsRequest`, 30 min deslizante, 8 h pelo claim `inicio`, etapa de 5 min), porta 3 (`Totp.cs`: HMAC-SHA1, 6 dígitos, passo 30, janela de ±1, `passo > ultimoPasso`), porta 4 (`AcoesDaPlataforma` com 8 constantes, `resumo` cortado em 200), porta 7 (`MigracaoOperadorTests.cs:58`), porta 8 (`Esclari.Api/Plataforma/` plana; `Esclari.Api/Instituicoes/` só tem `Aparencia` e `Carga`). As amendas de C1, C13 e C8, e as listas de `## Existing assertions changed`, não baixam a exigência: as asserções removidas de `PlataformaTests`, `UsuarioDominioTests` e `ProgramDespachoTests` eram todas do papel `OPERADOR` ou da regra "desativar a própria instituição", ambos extintos pelo plano; a mudança `[2, 0, 1, 0]` para `[2, 0, 0, 0]` em `PlataformaTests` vem do usuário-operador que deixou de existir.
8. Os gates `crapper` e `dryer` do `web/` não foram rodados nesta rodada (nenhum check os lista).

## Gate

`dotnet test Esclari.Api.Tests --no-build` - 1269 passed, 1 failed (`NomeDoProdutoTests`); `npx vitest run` em `web/` - 1135 passed, 0 failed; `dotnet build Esclari.slnx` - 0 erros. Checks (rodada 1): 45 PASS, 10 FAIL (C32, C36, C42 a C48, C54); todos PASS na rodada 2.

## Round 2

Escopo: so os 10 checks que falharam na rodada 1 (C32, C36, C42 a C48, C54) mais `NomeDoProdutoTests`, contra `HEAD` com as correcoes de `024bb0f`. Verificador independente, uma execucao por alvo de prova, duas falhas injetadas em `git worktree` descartavel (removido com `--force`; `git worktree list` so mostra a arvore principal).

Diff de `checks.md` desde `3c2c49f`: so aparecem os acertos de nome ja registrados (rota de C1, classe de prova de C13), a lista `Existing assertions changed` (testes apagados ou renomeados por consequencia do plano) e notas em `Notes`. Nenhuma linha `Check` ou `Proof` de C32, C36, C42 a C48 e C54 mudou, e nenhuma obrigacao foi enfraquecida.

| Check | Proof run | Evidence | Result |
| --- | --- | --- | --- |
| C32 | `vitest src/plataforma/instituicoes.test.tsx -t "criar leva a importar a carga na mesma tela"` - 1 passed, 11 skipped | `instituicoes.test.tsx:266-279`: cria, clica "Importar carga" no aviso, envia o arquivo e ve `Importados: 2 setores, 1 curso, 3 categorias, 2 regras, 5 artigos.` no mesmo dialogo; as gravacoes sao `POST Lista` e `POST Lista/faculdade-demo/carga` | PASS |
| C36 | `dotnet test --filter PlataformaAuditoriaTests.Resumo_sem_dado_pessoal_e_ate_200_caracteres` - 1 passed | `PlataformaAuditoriaTests.cs`: `Assert.Equal(AcoesDaPlataforma.Todas.Order(), linhas.Select(l => l.Acao).Distinct().Order())` (as 8), depois por linha tamanho <= 200, sem CPF, e-mail, padrao de CPF, `token`/`senha`/`password` nem hash de token; `INSTITUICAO_DESATIVADA` com motivo de 200 tem `Resumo.Length == 200` | PASS |
| C42 | `-t "menu da instituicao nao tem o grupo Plataforma"` em `menu.test.tsx` - 1 passed, 3 skipped | teste existe e executa | PASS |
| C43 | `-t "entrada em tres etapas"` em `entrada.test.tsx` - 3 passed, 4 skipped | tres testes executam | PASS |
| C44 | `-t "casca da plataforma com a marca do Esclari e dois itens"` em `instituicoes.test.tsx` - 1 passed, 11 skipped | teste existe e executa | PASS |
| C45 | `-t "lista com saude e etiquetas"` - 2 passed, 10 skipped | testes executam | PASS |
| C46 | `-t "estados vazios"` - 2 passed, 10 skipped | testes executam | PASS |
| C47 | `-t "401 leva a entrada com a frase de sessao encerrada"` em `entrada.test.tsx` - 1 passed, 6 skipped | teste existe e executa | PASS |
| C48 | `-t "desativar pede confirmacao e motivo"` - 1 passed, 11 skipped | teste existe e executa | PASS |
| C54 | `-t "plataforma nao e uma instituicao"` em `entrada.test.tsx` - 1 passed, 6 skipped | teste existe e executa | PASS |
| C28 (re-leitura) | `PlataformaInstituicoesTests.Desativar_exige_motivo_de_3_a_200_caracteres` (na suite completa) | `PlataformaInstituicoesTests.cs:91-104`: 400 e instituicao ativa para `""`, `"ab"`, 201 e corpo sem motivo; 200 OK e desativada para `"abc"` e para 200 caracteres | PASS |

Gate da rodada 2: `dotnet build Esclari.slnx` 0 erros; `dotnet test Esclari.Api.Tests` 1270 passed, 0 failed (`NomeDoProdutoTests` verde); `cd web; npx vitest run` 129 arquivos, 1138 passed, 0 failed.

### Faults injected (round 2)

| Mutation | Location | Killed |
| --- | --- | --- |
| botao Desativar habilitado sem motivo (`disabled={motivo.trim().length < 3}` virou `disabled={false}`) | `web/src/plataforma/Instituicoes.tsx:222` | yes - `instituicoes.test.tsx` "desativar pede confirmacao e motivo, reativar e reenviar agem direto" (1 failed, 11 passed) |
| `resumo` de reativacao vaza a palavra `token` (`Reativou {slug} token`) | `Esclari.Api/Plataforma/ReativarInstituicao.cs:21` | yes - `PlataformaAuditoriaTests.Resumo_sem_dado_pessoal_e_ate_200_caracteres` (1 failed, 6 passed) |

### Resolution of round 1 deviations

1. Provas web de C32, C42 a C48 e C54: agora casam testes existentes e passam com N >= 1. Resolvido.
2. C32: o teste encadeia criar e importar com as contagens na mesma tela. Resolvido.
3. C36: cobre as oito acoes. Resolvido.
4. `NomeDoProdutoTests`: passa. Resolvido.
5. Arquivo estranho `instituicoes-painel.png` na raiz (commit 19f4e50): nao tratado nesta rodada, fica como observacao nao bloqueante.
6. Limite inferior de C28: agora aceita `"abc"` (3) e 200 caracteres. Resolvido.
7. Gates `crapper` e `dryer` do `web/` nao rodados (nenhum check os lista); `mutator` suspenso (AD-025).

### Final verdict

Todos os 55 checks: 55 PASS (45 da rodada 1 mantidos, 10 corrigidos na rodada 2). Suites completas verdes. Verdict: PASS.
