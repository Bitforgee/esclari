# Parâmetros por instituição verification

**Verdict**: PASS
**Profile**: standard
**Diff range**: 2b1f6b0..67f9326
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

Os 16 checks têm prova que roda e passa em `HEAD` (`67f9326`), e as 3 falhas injetadas foram pegas. Verificação enxuta por decisão do dono (18/11): uma rodada, no máximo 3 falhas, sem Playwright e sem `mutator` (AD-025). Achados não bloqueantes no fim.

## Checks

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | sete colunas `prazo_*` `integer` anuláveis e nulas na criação | suíte inteira (TRX), `ParametrosTests.Sete_colunas_anulaveis_e_nulas_na_criacao` Passed | `Esclari.Api.Tests/ParametrosTests.cs:37` - `Assert.Equal(("integer", "YES"), ...)`; `:40` - lista das sete colunas; `:43` - `Assert.Equal(new ParametrosDaInstituicao(), ...Parametros)` | PASS |
| C2 | `GET` 200 com `valor`/`padrao`/`efetivo`; sem próprio `valor` nulo e padrões 30, 7, 3, 7, 12, 24, 120 | `ParametrosTests.Ver_valor_padrao_e_efetivo` Passed | `ParametrosTests.cs:54` - `Assert.Equal(HttpStatusCode.OK, ...)`; `:57` - `Assert.Equal([(null, 30, 30), (null, 7, 7), (null, 3, 3), (null, 7, 7), (null, 12, 12), (null, 24, 24), (null, 120, 120)], ...)` | PASS |
| C3 | `PUT` grava o enviado, `null` restaura, ausente vale `null`, audita nome e valor, 200 com estado novo | `ParametrosTests.Alterar_grava_restaura_e_audita` Passed | `ParametrosTests.cs:68` - 200; `:70` - `Assert.Equal((45, 30, 45), ...)`; `:73` - parâmetros gravados só os 4 enviados (os outros 3 ausentes ficam `null`); `:76-77` - restaurar `abandonoMinutos` para `(null, 30, 30)`; `:81-85` - resumo com `abandonoMinutos=45`, `abandonoMinutos=padrão`, sem `confirmacaoDias` no segundo | PASS |
| C4 | fora da faixa 400 com o campo (14 valores), limites aceitos | `ParametrosTests.Faixas_400_por_campo` Passed | `ParametrosTests.cs:102` - `BadRequest` por valor; `:103` - `errors` contém o campo; `:105` - nada gravado; `:112` - `Assert.Equal(HttpStatusCode.OK, ...)` nos dois conjuntos de limites (5/1440/1/60/90/8760 e 89/90) | PASS |
| C5 | resolução < primeira resposta, lembrete >= resolução automática: 400 sem gravar, inclusive com um lado do padrão | `ParametrosTests.Conjunto_efetivo_400_sem_gravar` Passed | `ParametrosTests.cs:130` - `BadRequest` nos 4 casos (`resolucaoHoras = 23` e `lembreteAguardandoDias = 7` usam o padrão do outro lado); `:131` - campo em `errors`; `:133` - parâmetros intactos; `:134` - auditoria 0 | PASS |
| C6 | slug desconhecido 404, sem sessão 401, nas duas rotas | `ParametrosTests.Slug_404_e_sem_sessao_401` Passed | `ParametrosTests.cs:143-144` - `NotFound` em `GET` e `PUT`; `:145-146` - `Unauthorized` em `GET` e `PUT` | PASS |
| C7 | `PARAMETROS_ALTERADOS` na lista fechada (11 ações) | `PlataformaAuditoriaTests.Acoes_fechadas_e_resumo_curto` Passed | `Esclari.Api.Tests/PlataformaAuditoriaTests.cs:169` - `Assert.Equal(11, AcoesDaPlataforma.Todas.Count)`; `Esclari.Api/Dados/Operadores/AcoesDaPlataforma.cs:15` - constante | PASS |
| C8 | `Prazos.Para` usa o próprio ou o padrão, nos sete | `PrazosPorInstituicaoTests.Para_usa_o_proprio_ou_o_padrao` Passed | `Esclari.Api.Tests/PrazosPorInstituicaoTests.cs:36` - tudo nulo é o padrão; `:38` - `Assert.Equal(new Prazos(45, 2, 1, 4, 6, 8, 48), ...)`; `:40` - mistura `(30, 10, 3, 7, 12, 24, 200)` | PASS |
| C9 | meta 1 numa instituição: 2 h sem resposta atrasa e aparece nos indicadores com meta 1; a outra não | `PrazosPorInstituicaoTests.Metas_proprias_no_atraso_e_nos_indicadores` Passed | `PrazosPorInstituicaoTests.cs:54-55` - atrasados 1 e 0; `:58` - `(1, 1)`; `:59` - `(24, 0)` | PASS |
| C10 | `ConfirmacaoDias` 2: e-mail "(até 2 dias)", fecha o resolvido há 3 dias, não o da outra | `PrazosPorInstituicaoTests.Confirmacao_propria_no_email_e_no_fechamento` Passed | `PrazosPorInstituicaoTests.cs:74` - `"(até 2 dias)"`; `:75` - `"(até 7 dias)"`; `:80` - `Assert.Equal(Fechado, ...)`; `:81` - `Assert.Equal(Resolvido, ...)` | PASS |
| C11 | lembrete 1 e resolver 2: lembra aos 1,5 dia e resolve aos 3, só na própria | `PrazosPorInstituicaoTests.Aguardando_proprio_lembra_e_resolve` Passed | `PrazosPorInstituicaoTests.cs:102` - `Assert.NotNull(...LembreteEm)`; `:103` - `Assert.Equal(Resolvido, ...)`; `:104-105` - outra com `LembreteEm` nulo e `AguardandoAluno` | PASS |
| C12 | `AbandonoMinutos` 10: conversa parada 15 min abandonada, a da outra não | `PrazosPorInstituicaoTests.Abandono_proprio` Passed | `PrazosPorInstituicaoTests.cs:120` - `Assert.Equal("ABANDONADA", ...)`; `:121` - `Assert.Null(...)` na outra | PASS |
| C13 | validade 3 meses só no artigo novo; o gravado mantém 12 | `PrazosPorInstituicaoTests.Validade_propria_so_no_que_nasce_depois` Passed | `PrazosPorInstituicaoTests.cs:141` - `hoje.AddMonths(12)`; `:142` - `hoje.AddMonths(3)`; `:143` - `valido_ate` do antigo lido do banco continua `AddMonths(12)` | PASS |
| C14 | `PUT` vale na requisição seguinte sem reiniciar | `PrazosPorInstituicaoTests.Valor_novo_vale_na_proxima_leitura` Passed | `PrazosPorInstituicaoTests.cs:151` - meta 24 antes; `:156` - `Assert.Equal(5, ...)` depois do `PUT` | PASS |
| C15 | aba "Parâmetros" com sete campos, padrão no apoio, vazio herda, "Salvar" envia sete e mostra "Parâmetros salvos." | `npm --prefix web test -- src/plataforma/parametros.test.tsx -t "aba parametros mostra o padrao e salva"` 1 passed, 1 skipped (nome completo "aba parametros mostra o padrao e salva os sete") | `web/src/plataforma/parametros.test.tsx:51` - `toHaveValue(null)`; `:53` - `toHaveLength(7)`; `:52` - `'Padrão: 7 dias. Vazio usa o padrão.'`; `:59` - `'Parâmetros salvos.'`; `:60` - corpo do `PUT` com os sete (`confirmacaoDias: 2`, seis outros como enviados/`null`) | PASS |
| C16 | `400` mostra o erro no campo; "Usar padrão" esvazia e só envia `null` ao salvar | `... -t "erro no campo e usar padrao so ao salvar"` 1 passed, 1 skipped | `parametros.test.tsx:79` - descrição acessível com `'O abandono deve ficar entre 5 e 1440.'`; `:83` - `toHaveValue(null)`; `:84` - `toHaveLength(1)` (nenhum `PUT` novo antes de salvar); `:87` - `toMatchObject({ abandonoMinutos: null })` | PASS |

## Coverage

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| linhas da tabela `Surface` (2) | `plan.md` Surface e `Esclari.Api.Tests/Retratos/rotas.txt` (+2 linhas no diff) | `GET .../parametros` C2 C6 · `PUT .../parametros` C3 C4 C5 C6 | - |
| status do `GET` (3) | `VerParametros.cs` e plano | 200 C2 (`ParametrosTests.cs:54`) · 401 C6 (`:145`) · 404 C6 (`:143`) | - |
| status do `PUT` (4) | `AlterarParametros.cs`, `ParametrosDaPlataforma.cs:36-47` | 200 C3 (`:68`) · 400 C4 C5 (`:102`, `:130`) · 401 C6 (`:146`) · 404 C6 (`:144`) | - |
| parâmetros (7) | `Esclari.Api/Prazos.cs:5-6` e `ParametrosDaPlataforma.cs:15-24` | `AbandonoMinutos` C12 · `ConfirmacaoDias` C10 · `LembreteAguardandoDias` C11 · `ResolverAguardandoDias` C11 · `ValidadeConhecimentoMeses` C13 · `PrimeiraRespostaHoras` C9 · `ResolucaoHoras` C8 C4 C5 (mapeamento e validação; o efeito no consumidor não tem teste, ver achado 1) | - |
| ações da auditoria (11) | `AcoesDaPlataforma.cs` | `PARAMETROS_ALTERADOS` C3 (`ParametrosTests.cs:81-85`) C7 (`PlataformaAuditoriaTests.cs:169`); as outras 10 já provadas na feature `console-da-plataforma` | - |
| consumidores de `Prazos` no `Impact` (12) | leitura de `Esclari.Api/**` por `Prazos`/`PrazosAtuais` | `CalculoDetalhe` e `ConsultarIndicadoresDetalhe` `:17` (PrazosAtuais) C9 · `Avisos.Resolvido` (via `ResolverChamado.cs`) C10 · `FiltroDeChamados` (via `BuscarChamados.cs:26`) C9 · `BuscarFontes.NovaValidade` (artigo/documento: 7 handlers com `PrazosAtuais`) C13 só no `CriarArtigo` · `AvisarAtrasos.cs:25` `PorInstituicao` (sem teste de comportamento próprio) · `SubirPorPrazo` (usa `AvisarAtrasos.cs:43`, `prazos.Para`) · `LembrarAguardando.cs:14` C11 · `ResolverSemResposta.cs:14,29` C11 · `FecharResolvidos.cs:14,27` C10 · `AbandonarConversas.cs:14,23` C12 | - |

## Test policy rows

Nenhuma linha `Test policy` em `checks.md`. Vale a convenção do repositório (AGENTS.md): teste de integração bate na rota HTTP, e todos os checks de API acima o fazem; C8 é teste unitário da regra de `Prazos`.

## Faults injected

Isolamento: `git worktree add ..\esclari-verify-par HEAD`, removido com `git worktree remove --force` e `git worktree prune`. `git status --porcelain` do repositório real igual antes e depois (só `?? .tmp-capturas/`, anterior à verificação). Sem `git stash`.

| Mutation | Location | Killed |
| --- | --- | --- |
| `Com` ignora o valor próprio de `ConfirmacaoDias` (`p.ConfirmacaoDias ?? ConfirmacaoDias` vira `ConfirmacaoDias`) | `Esclari.Api/Prazos.cs:38` | yes - `PrazosPorInstituicaoTests.Para_usa_o_proprio_ou_o_padrao` e `Confirmacao_propria_no_email_e_no_fechamento` falham |
| sem a checagem `LembreteAguardandoDias >= ResolverAguardandoDias` (condição vira `false`) | `Esclari.Api/Plataforma/ParametrosDaPlataforma.cs:44` | yes - `ParametrosTests.Conjunto_efetivo_400_sem_gravar` falha |
| `FecharResolvidos.Ler` usa o prazo global (`agora - prazos.Confirmacao`) em vez do da instituição | `Esclari.Api/Chamados/Tarefas/FecharResolvidos.cs:16` | yes - `PrazosPorInstituicaoTests.Confirmacao_propria_no_email_e_no_fechamento` falha (o resolvido há 3 dias da instituição não fecha) |

## Landing e `Existing assertions changed`

- Door 1 (sete colunas `integer NULL`): migration `20261118195644_ParametrosPorInstituicao` e `Instituicao.cs` (+20); C1 lê `information_schema`. Sem divergência do formato literal do plano.
- Door 2 (`valor próprio ?? padrão`, validações sobre o efetivo): `Prazos.cs:36-38` e `ParametrosDaPlataforma.cs:36-47`; as faixas do plano (5-1440, 1-60, 1-90, 1-60, 1-8760) coincidem. A validação de `Prazos.Ler` (config global) continua só na subida.
- Door 3 (`PARAMETROS_ALTERADOS`, uma linha por `PUT` com nome e valor): `AcoesDaPlataforma.cs:15`, `ParametrosDaPlataforma.cs:49-54`.
- `PlataformaAuditoriaTests`: a lista passa de 10 para 11 e o fluxo ganha um `PUT .../parametros`; nenhuma asserção ficou mais fraca (a contagem sobe, o filtro de `InstituicaoAlvoId` ganha a ação). `ArquiteturaTests`, `RetratoTests`, `SessaoTests`, `MediatorTests`, `ServicosTests`, `rotas.txt`: só contagens e rotas novas, conferidas no diff (54 arquivos, +3735/-70, a maior parte é o `Designer.cs` da migration). Nenhum teste apagado nem pulado.
- `checks.md` mudou depois da base: o diff `2b1f6b0..HEAD` só troca a linha "Base da verificação" e acrescenta a nota sobre `ParametrosDaInstituicao` em `## Handoff`. Nenhum check, prova, `Coverage`, `Swept` ou `Existing assertions changed` foi tocado.

## Gate

`dotnet build Esclari.slnx` - 0 erros. `dotnet test Esclari.Api.Tests` - 1304 passed, 0 failed (os 14 testes nomeados aparecem individualmente no TRX como Passed). `npx vitest run` (em `web/`) - 1144 passed, 0 failed. As duas provas web rodadas exatamente como em `checks.md`: 1 passed e 1 skipped cada.

## Achados não bloqueantes

1. `ResolucaoHoras` e a previsão ao aluno: C9 só exercita `PrimeiraRespostaHoras`. Nenhum teste muda `ResolucaoHoras` própria e observa o efeito em `emAtrasoAgora`, no filtro ou no resumo de atrasos (`CalculoDetalhe.cs:36`, `FiltroDeChamados.cs:79`). Um consumidor que ignorasse só `ResolucaoHoras` própria passaria na suíte (não injetado nesta rodada).
2. C13 só exercita `CriarArtigo`; os outros seis handlers com `NovaValidade` (`AlterarArtigo`, `CriarArtigoDoChamado`, `RenovarArtigo`, `CriarDocumento`, `NovaVersaoDocumento`, `RenovarDocumento`) usam `PrazosAtuais` no código, mas sem teste com validade própria. O check pede só "artigo novo".
3. `Swept` cita idempotência em C3 ("mesmo `PUT` duas vezes deixa o mesmo estado"): nenhum teste envia o mesmo `PUT` duas vezes. O código é uma substituição total, então o risco é baixo.
4. `AvisarAtrasos` (aviso por e-mail com meta própria, `AvisarAtrasos.cs:43,92`) usa a meta da instituição no código, sem teste de comportamento próprio.
5. A previsão ao aluno em `TransferirConversa.cs:22,29` usa `PrazosAtuais`; também sem teste com meta própria.
