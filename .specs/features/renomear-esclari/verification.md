# renomear-esclari verification

**Verdict**: FAIL (rodada 3: C20 regrediu, ver `## Rodada 3`)
**Profile**: standard
**Diff range**: f57b031..4be6cfc
**Round**: 2 - full
**Verifier**: independent sub-agent (author != verifier)

Provas rodadas em `4be6cfc` (HEAD), na árvore real, com `postgres`, `minio` e `mailpit` do compose no ar. Todas as provas foram reexecutadas, sem
aproveitar o relatório da rodada 1. Porcelain inicial e final: vazio. As falhas foram injetadas num `git worktree` temporário de HEAD
(`D:\esclari-verif-ren`, removido no fim; cada arquivo restaurado e confirmado com `git diff --quiet`, exit 0).

Ranked gaps:

0. Coverage FAIL: o membro UserSecretsId = sclari-api (door 3 do Landing, atribuído a C20 no checks.md) não tem prova. Injetei sclari-api -> one-api em Esclari.Api/Esclari.Api.csproj num worktree: NomeDoProdutoTests ficou 2/2 aprovado, o mutante sobreviveu. Todos os 21 checks passam, mas a cobertura prometida do door 3 não se sustenta. Correção possível: uma asserção do valor sclari-api (por exemplo em MarcaTests) ou o dono aceitar o risco por AD (AD-036).
1. Risco (herdado da rodada 1, aceitável): a prova HTTP de C9 (`McpAcessoTests.Recusa_token_com_prefixo_antigo_onemcp`) continua verde com o regex
   do `TokenMcp` aceitando `onemcp_` (reconfirmado: 13/13 aprovados com o mutante). Só `TokenMcpDominioTests` mata essa falha. O check C9 passa
   porque lista as duas provas e a de domínio pega; o dono pode querer um teste HTTP com um token `onemcp_` que exista no banco.
2. Risco (herdado): `web/src/test/estrutura.test.ts` abriu exceção para `administracao/marca.test.tsx` (a raiz da fatia passou de 4 para 5 arquivos, AD-056).
3. Ver item 0 (`UserSecretsId`).
4. `crapper` e `dryer` agora saem com 0 (a rodada 1 viu `crapper` exit 2 em `useModo` e `Usuarios`; foi corrigido por `3864a83`, de outra feature).

## Binding sources

Nenhuma fonte marcada como binding no plano. Sem contradição.

## Checks

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | tela de acesso mostra `Esclari`, sem `One` | `npm --prefix web test -- src/acesso/marca.test.tsx -t "titulo da tela de acesso"` exit 0, 1 passed (4 filtrados) | `web/src/acesso/marca.test.tsx` heading nível 1 `Esclari` | PASS |
| C2 | shell mostra `Esclari` (barra e menu 360) | `-t "marca no shell"` exit 0, 1 passed | `acesso/marca.test.tsx` `within(barra)` e `within(menu)` | PASS |
| C3 | manifesto e `<title>` | `src/test/marca.test.ts -t "manifesto e titulo"` exit 0, 1 passed | `web/src/test/marca.test.ts` | PASS |
| C4 | e-mails e remetente | `dotnet test --filter MarcaTests.Emails_citam_Esclari_e_remetente_padrao` exit 0, 1/1 aprovado | `Esclari.Api.Tests/MarcaTests.cs` | PASS |
| C5 | Integrações com `esclari` | `src/administracao/marca.test.tsx -t "integracoes"` exit 0, 2 passed | `administracao/marca.test.tsx` | PASS |
| C6 | `Azul Esclari` na parada inicial | `-t "azul esclari"` exit 0, 1 passed | `administracao/marca.test.tsx` | PASS |
| C7 | `esclari-tema`, `esclari-barra`, nenhuma `one-*` | `src/acesso/marca.test.tsx -t "chaves esclari"` exit 0, 1 passed | `acesso/marca.test.tsx` | PASS |
| C8 | cookie `esclari.auth` | `--filter MarcaTests.Cookie_de_sessao_se_chama_esclari_auth` exit 0, 1/1 | `MarcaTests.cs`; app `Esclari.Api/Acesso/AcessoExtensoes.cs:18` | PASS |
| C9 | token `esclarimcp_`; `onemcp_` recebe 401 | `--filter TokenMcpDominioTests.Token_novo_usa_prefixo_esclarimcp` 1/1 e `McpAcessoTests.Recusa_token_com_prefixo_antigo_onemcp` 1/1, ambos exit 0 | `TokenMcpDominioTests.cs`, `McpAcessoTests.cs` | PASS (ressalva do gap 1) |
| C10 | `ESC-000123` e `ESC-000001` | `TransferenciaTests.Numero_por_instituicao` 1/1 e `...Numero_com_seis_digitos_apos_o_contador_122` 1/1, exit 0 | `TransferenciaTests.cs` | PASS |
| C11 | descrição MCP cita `ESC-000012` | `McpFerramentasTests.Descricao_cita_exemplo_ESC_000012` 1/1 exit 0 | `McpFerramentasTests.cs` | PASS |
| C12 | placeholder `Ex.: ESC-000123…` | `src/chamados/marca.test.tsx -t "placeholder do original"` exit 0, 1 passed | `chamados/marca.test.tsx` | PASS |
| C13 | duplicado 200 / 400 / numero_original | `ExtrasTests.Duplicado_grava_numero_original_no_formato_novo` 1/1 exit 0 | `ExtrasTests.cs` | PASS |
| C14 | `translate="no"` só no protocolo | `acesso/marca.test.tsx -t "protocolo sem traducao"` exit 0, 2 passed | `acesso/marca.test.tsx` | PASS |
| C15 | vinte aberturas paralelas distintas | `TransferenciaTests.Vinte_transferencias_paralelas_numeros_distintos` 1/1 exit 0 | `TransferenciaTests.cs` | PASS |
| C16 | build e `ArquiteturaTests` | `dotnet build Esclari.slnx` exit 0 (0 erros, 9 avisos); `--filter ArquiteturaTests` 82/82 exit 0 | `ArquiteturaTests.cs` | PASS |
| C17 | sem mudança pendente no modelo | `dotnet ef migrations has-pending-model-changes --project Esclari.Api` exit 0, "No changes have been made to the model since the last migration." | saída do comando | PASS |
| C18 | compose, banco, bucket, `ConnectionStrings__Esclari` | `docker compose up -d postgres minio` exit 0 e `psql -U postgres -d esclari -At -c "select 1"` saída `1`, exit 0; `MarcaTests.Compose_usa_nome_esclari` 1/1 | `MarcaTests.cs` | PASS |
| C19 | suítes e gates sem enfraquecer | `dotnet test Esclari.Api.Tests --blame-hang --blame-hang-timeout 4min` exit 0, 1011 aprovados, 0 falhas, 0 ignorados (3 min 15 s); `npm --prefix web test` exit 0, 112 arquivos, 1044 passed; `npm --prefix web run build` exit 0; `npm --prefix web run coverage` exit 0; `crapper.exe --use-existing-coverage --threshold 10 -s src` exit 0; `dryer.cmd --threshold 0.8` nas 6 fatias e nos arquivos da raiz: exit 0, "No duplicate candidates found." | saídas dos comandos; 0 testes ignorados na API | PASS |
| C20 | nenhum arquivo cita `One` fora do histórico | `NomeDoProdutoTests.Nenhum_arquivo_rastreado_cita_One_fora_do_historico` 1/1 exit 0 (falhava na rodada 1; `d81a54b` liberou `MarcaTests.cs`) | `NomeDoProdutoTests.cs` | PASS |
| C21 | AD novo no `STATE.md` | `NomeDoProdutoTests.State_registra_o_AD_do_nome` 1/1 exit 0 | `.specs/STATE.md` AD-058 | PASS |

Cada filtro casou exatamente os testes esperados (nenhum com zero). A suíte web caiu de falhas para 0 e subiu de 1001 para 1044 testes por causa
de features posteriores (`indicadores-claros`), sem teste apagado.

## Coverage

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| `GET /api/chamados/{numero}` statuses (2) | plan Surface | 200 `TransferenciaTests` (C10); 404 `VisibilidadeTests`/`IsolamentoTests` (rodados na suíte completa de C19) | - |
| `POST /api/chamados/{numero}/duplicado` statuses (3) | plan Surface | 200 e 400 C13; 404 `IsolamentoTests` (C19) | - |
| door 1 formato do protocolo (2) | plan Landing | contador 1 e 123 C10 | - |
| door 2 prefixo do token MCP (2) | plan Landing | `esclarimcp_` emitido C9; `onemcp_` recusado C9 (domínio; HTTP só indireto) | - |
| door 3 identificadores persistidos (6) | plan Landing | cookie C8; `esclari-tema` e `esclari-barra` C7; banco, usuário e bucket C18; `UserSecretsId` só guarda negativa C20 | `UserSecretsId` = `esclari-api`: mutante `one-api` sobreviveu a `NomeDoProdutoTests` (item 0) |
| texto da marca por tela (5) | plan AC 1, 2, 5, 6, 14 | C1, C2, C5, C6, C14 | - |
| e-mails com a marca (3) | plan AC 4 | C4 | - |
| campos do manifesto (3) | plan AC 3 | C3 | - |
| ocorrências de `One` no repositório (7) | plan Impact | `One.Api`, `One.Api.Tests`, `One.slnx` C16; `OneDb` e snapshot C17; infra C18; web e documentos vivos C20 (agora verde) | - |
| startup config: cookie e sessão (1) | `EsclariFactory` usa o `Program` do app | C8 | - |
| superfícies no celular (1) | plan Observable | 360×780 C2 | - |

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| Decide, atravessa fronteira | `AbrirChamado.cs`, `TokenMcp.cs` | `AbrirChamado`: HTTP em `TransferenciaTests` (C10, C15); `TokenMcp`: domínio `TokenMcpDominioTests` e HTTP `McpAcessoTests` | yes |
| Decide, sem fronteira | `Chamado.MarcarDuplicado` | `ChamadoDominioTests` com `ESC-000009`, na suíte completa de C19 | yes |
| Instrumentação: troca de texto | telas, e-mails, manifesto | `marca.test.*`, `MarcaTests`, `NomeDoProdutoTests` | yes |

## Faults injected

Mutações feitas à mão no worktree temporário e desfeitas por reescrita do arquivo original; `git diff --quiet` saiu 0 depois de cada uma.

| Mutation | Location | Killed |
| --- | --- | --- |
| protocolo com 5 dígitos (`D6` para `D5`) | `Esclari.Api/Chamados/Acoes/AbrirChamado.cs:22`; `TransferenciaTests` 4 falhas de 11 | yes |
| `UserSecretsId` `esclari-api` para `one-api` | `Esclari.Api/Esclari.Api.csproj:8`; `NomeDoProdutoTests` 2/2 aprovado | **no (sobreviveu)** |
| regex do token aceita `onemcp_` e `esclarimcp_` | `Esclari.Api/Dados/Mcp/TokenMcp.cs:30`; `TokenMcpDominioTests` 1 falha de 6; `McpAcessoTests` 13/13 continuou verde | yes (só pela camada de domínio) |
| cookie volta para `one.auth` | `Esclari.Api/Acesso/AcessoExtensoes.cs:18`; `MarcaTests` 1 falha de 6 | yes |
| e-mail de primeiro acesso diz `senha no One` | `Esclari.Api/Acesso/PrimeiroAcesso.cs:42`; `MarcaTests` 1 falha de 6 | yes |
| comentário `// One` inserido em arquivo rastreado | `Esclari.Api/Dados/Mcp/TokenMcp.cs`; `NomeDoProdutoTests` 1 falha de 2 (o guarda) | yes |
| manifesto `name: 'One'` | `web/vite.config.ts:18`; `src/test/marca.test.ts` 1 falha de 1 | yes |
| placeholder `ESC-123…` | `web/src/chamados/ChamadoDetalhe.tsx`; `src/chamados/marca.test.tsx` 1 falha de 1 | yes |

Sobreviveu o mutante de UserSecretsId (item 0). A prova HTTP de C9 também sobrevive ao mutante do regex do token (gap 1), mas a camada de domínio o matou.

## Pontos do construtor

Nada novo desde a rodada 1 além de `d81a54b` (liberou `MarcaTests.cs` no guarda de nome) e `8190316` (retirou as exceções do guarda). Confirmado: nenhum teste
apagado, 0 ignorados na API, web com 112 arquivos aprovados.

## Gate

- `dotnet build Esclari.slnx`: 0 erros. `dotnet test Esclari.Api.Tests`: 1011 aprovados, 0 falhas.
- `npm --prefix web test`: 1044 passed. `npm --prefix web run build`: exit 0. `npm --prefix web run coverage`: exit 0.
- `crapper.exe --use-existing-coverage --threshold 10 -s src`: exit 0. `dryer.cmd --threshold 0.8` (6 fatias e raiz): exit 0.
- `mutator`: suspenso (AD-025), não rodado.
- E2E: nenhum check desta feature depende de e2e.
- HEAD usado: `4be6cfcca2c32920162dbc6f14095944091a46cf` (não mudou durante a verificação).

## Rodada 3 (escopada, fechamento)

Escopo: o gap aberto da rodada 2 (item 0, `UserSecretsId`) e uma reconfirmacao de C20, em HEAD `14b9146`, com `--no-build` (sem build, sem e2e, sem suite inteira).

| Check | Prova | Resultado |
| --- | --- | --- |
| C20 / door 3 `UserSecretsId` | `dotnet test --no-build --filter "MarcaTests\|NomeDoProdutoTests"`: 9 testes, 8 aprovados, 1 falha. `MarcaTests.UserSecretsId_e_esclari_api` (commit `867a03f`) aprovado: o gap do item 0 foi fechado. | gap: PASS |
| C20 nenhum arquivo cita `One` | `NomeDoProdutoTests.Nenhum_arquivo_rastreado_cita_One_fora_do_historico` FALHA: `ROADMAP.md:3` e `ROADMAP.md:86` citam "One passa a Esclari" (texto de `48b76b1`, posterior a 4be6cfc). | FAIL |
| C1-C19, C21 | Nao reexecutados. Nenhum commit depois de 4be6cfc tocou produto de nome/protocolo/cookie/token. Somente `7c4141b` (guarda) e `867a03f` (teste). | NAO VERIFICADO nesta rodada (valem os resultados da rodada 2) |

Fora: suite completa, web, e2e, crapper/dryer, mutacoes novas. Nao tocou codigo nem testes.
Para fechar: reescrever as citacoes de "One" no ROADMAP.md (ou liberar a excecao no guarda por decisao do dono).
