# esclari-pendencias verification

**Verdict**: FAIL (rodada 2: C12, C14 e C16 continuam reprovados)
**Profile**: standard
**Diff range**: 6e648a4..867a03f
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

Provas rodadas em `867a03f2f06689e78826201b537b05f84e767177` (HEAD, não mudou durante a verificação), na árvore real, com `postgres`, `minio`, `mailpit` e `api` do compose no ar.
Porcelain inicial e final: vazio. As falhas foram injetadas num `git worktree` temporário de HEAD (`D:\esclari-verif-pend`, removido no fim; cada arquivo restaurado e
confirmado com `git diff --quiet`, exit 0). Nenhum dado do Docker foi apagado.

Ranked gaps:

0. **C14 FAIL**: o teste citado na prova (`NomeDoProdutoTests.State_registra_o_AD_do_remoto_e_pasta`) não existe. O filtro casa 0 testes ("Nenhum teste corresponde ao filtro") e o `dotnet test` sai com 0, então a prova passa vazia e não decide nada. Além disso: `AGENTS.md` não cita `Bitforgee/Esclari` nem a pasta; `STATE.md:77` (`**Branch**`, linha do Handoff) ainda cita `https://github.com/Bitforgee/One`, isenta pelo prefixo `**Branch**: \`main\` (AD-043)` em `PrefixosDoStateAnteriores`; AD-059 cita `D:\esclari`, não `D:\esclari-system`. Correção: o dono emenda o C14 (pasta `D:\esclari`), o construtor escreve o teste e atualiza `AGENTS.md` e a linha 77.
1. **C16 FAIL (texto literal)**: `Test-Path D:\esclari-system\.git` dá `False`, exit 1. A pasta real é `D:\esclari` (AD-059, decisão do dono de 16/11) e tem `.git`. A parte do remoto passa (`git ls-remote https://github.com/Bitforgee/Esclari HEAD` exit 0). Sem emenda aprovada pelo dono, o literal não é satisfeito; não registrei PASS por conta própria.
2. **C12 FAIL**: a primeira prova passa (nenhum volume começa por `one`). A segunda dá `2`, esperado `0`: `demo-mwjmxeg9` (criada 2026-11-16 19:06:55 UTC) e `operadora-mwjmxeg9` (19:06:53 UTC) existem no banco `esclari`. Foram criadas por uma rodada de e2e depois da limpeza, não pelo código desta feature. A causa estrutural (specs `*-real` criam `demo-*`/`operadora-*` a cada rodada) está fora do escopo do plano (alternativa rejeitada da door 3 trata só de `demo`/`e2e`). Não apaguei as linhas (ação destrutiva sem "ok"). Correção: o dono autoriza apagar as duas linhas, ou emenda o C12 para a rodada de limpeza.
3. **Risco (C1 a C3)**: `icones.test.ts` lê só os PNGs commitados; não roda o gerador. Mutante em `gerar-icones.mjs` (glifo em `x = tamanho*0.02`, fora da zona segura) sobreviveu (5/5 passed) até eu regenerar os PNGs com `npm run icones`; só então os testes falharam. Se alguém editar o script e esquecer de regenerar, nada falha. O C4 é o único teste que executa o script.
4. **C8 prova real não executada**: o Compose já tem `demo` e `e2e` (a rodada de `e2e:preparar` imprime `ja existe`); rodar com as duas ausentes exige apagar as instituições. Não apaguei. A prova unitária do C8 passa. Os CPFs dos admins semeados foram corrigidos pela rota `PUT /api/admin/usuarios/{id}` depois de `4be6cfc`; a prova real de criação com o CPF novo `12345678909` fica sem execução.
5. Observação: `git ls-remote origin HEAD` devolve `8190316`, atrás do HEAD local `867a03f` (commits ainda sem push; push só com "ok" do dono, AGENTS.md).

## Binding sources

Nenhuma fonte marcada como binding no plano. Sem contradição.

## Checks

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | PNGs 192, 512 e 512 | `npm --prefix web test -- src/test/icones.test.ts -t "dimensoes dos tres icones"` exit 0, 1 passed (4 filtrados) | `web/src/test/icones.test.ts`; `web/public/icons/*.png` | PASS |
| C2 | maskable: branco dentro do círculo de 410 px, resto cor da marca | `-t "maskable dentro da zona segura"` exit 0, 1 passed | `icones.test.ts` (varre os 512×512 pixels) | PASS |
| C3 | ícone 192 sobre `theme_color`, não simétrico | `-t "icone any branco sobre a cor do manifesto"` exit 0, 1 passed | `icones.test.ts` | PASS |
| C4 | Chromium indisponível: exit 1, mensagem em português, SHA-256 igual | `-t "falha do chromium"` exit 0, 1 passed | `icones.test.ts` roda `node scripts/gerar-icones.mjs` como processo filho com `PLAYWRIGHT_BROWSERS_PATH` vazio | PASS |
| C5 | manifesto lista os três ícones com `purpose` certo | `-t "manifesto lista os tres icones"` exit 0, 1 passed | `web/vite.config.ts` | PASS |
| C6 | `name: esclari` na primeira linha e `docker compose config` de outra pasta | `src/test/ambiente.test.ts -t "nome do projeto compose"` exit 0, 1 passed | `ambiente.test.ts` (cwd = `tmpdir()`) | PASS |
| C7 | volumes `esclari_pgdata`, `esclari_miniodata` e `esclari-api-1` | `docker compose up -d --build` exit 0 (`esclari-api-1` Recreated, Started); `docker volume inspect esclari_pgdata esclari_miniodata` exit 0; `docker inspect esclari-api-1` exit 0, `/esclari-api-1 running` | saída dos comandos | PASS |
| C8 | `e2e:preparar` cria `demo` e `e2e` | unitária: `src/test/preparar-e2e.test.ts -t "cria demo e e2e"` exit 0, 1 passed. Real (com as duas ausentes): **não executada**, exige apagar instituições (sem "ok") | `preparar-e2e.test.ts`; gap 4 | PASS (prova unitária; prova real não executada, ver gap 4) |
| C9 | segunda rodada: `ja existe`, exit 0, sem criar | unitária: `-t "segunda rodada ja existe"` exit 0, 1 passed. Real: `npm --prefix web run e2e:preparar` exit 0, imprime `ja existe demo` e `ja existe e2e` | saída do comando | PASS |
| C10 | contêiner ausente: exit 1 e nome do contêiner | `-t "conteiner ausente sai 1"` exit 0, 1 passed | `preparar-e2e.test.ts` (`meu-conteiner-api-1` na mensagem, nenhuma criação) | PASS |
| C11 | `instituicoes-real` usa `esclari-api-1` por padrão | `ambiente.test.ts -t "container padrao do e2e"` exit 0, 1 passed | `web/e2e/instituicoes-real.spec.ts:4` | PASS |
| C12 | sem volume `one*` e sem `demo-*`/`operadora-*` | `docker volume ls --format "{{.Name}}" \| Select-String "^one"` sem saída (volumes: `esclari_miniodata`, `esclari_pgdata`, `awakecon-*` e 2 anônimos); `psql ... select count(*) ...` devolve `2` (exit 0), esperado `0` | `demo-mwjmxeg9`, `operadora-mwjmxeg9` criadas 2026-11-16 19:06 UTC | FAIL (gap 2) |
| C13 | `origin` é `https://github.com/Bitforgee/Esclari` e `ls-remote` sai 0 | `git remote get-url origin` = `https://github.com/Bitforgee/Esclari`; `git ls-remote origin HEAD` exit 0 (`8190316...`) | saída dos comandos | PASS |
| C14 | `AGENTS.md`, `ROADMAP.md`, `STATE.md` citam `Bitforgee/Esclari` e `D:\esclari-system`; AD novo | `dotnet test --filter "FullyQualifiedName~NomeDoProdutoTests.State_registra_o_AD_do_remoto_e_pasta"` exit 0, mas "Nenhum teste corresponde ao filtro" (0 testes). `ROADMAP.md:66,78,158,159,540` citam `Bitforgee/Esclari`; `STATE.md:64` (AD-059) existe; `AGENTS.md` sem citação; `STATE.md:77` cita `Bitforgee/One` | `NomeDoProdutoTests.cs` só tem 2 testes (`Nenhum_arquivo...`, `State_registra_o_AD_do_nome`) | FAIL (gap 0) |
| C15 | guarda passa sem as exceções `github.com/Bitforgee/One` e `D:\one-system` | `--filter "FullyQualifiedName~NomeDoProdutoTests.Nenhum_arquivo_rastreado_cita_One_fora_do_historico"` exit 0, 1/1 aprovado | `NomeDoProdutoTests.cs`: `TrechosPermitidos` sem as duas exceções | PASS |
| C16 | pasta `D:\esclari-system` e remoto novo existem | `Test-Path D:\esclari-system\.git` = `False` (exit 1); `git ls-remote https://github.com/Bitforgee/Esclari HEAD` exit 0 | pasta real `D:\esclari` (AD-059) | FAIL (literal; gap 1) |

Cada filtro web casou exatamente um teste (os demais do arquivo ficaram "skipped" pelo `-t`). O filtro do C14 casou zero.

## Coverage

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| `npm run icones` statuses (2) | plan Surface | 200 exit 0: C1 (PNGs gravados; geração só indireta, gap 3) · 500 exit 1: C4 | - |
| `npm run e2e:preparar` statuses (2) | plan Surface | 200 exit 0: C8 unitária, C9 real · 500 exit 1: C10 | C8 real (gap 4) |
| `manifest.webmanifest` statuses (1) | plan Surface | 200: C5 | - |
| door 1: nome do projeto Compose (3) | plan Landing | `name` C6 · volumes C7 · contêiner C7 | - |
| door 2: ícones (3 arquivos) | plan Landing | 192 C1 · 512 C1 · maskable C1 e C2 | - |
| door 3: comando de preparo (2 instituições) | plan Landing | `demo` C8 · `e2e` C8 (e-mail do admin morto por F10) | - |
| door 4: nome do repositório e da pasta (2) | plan Landing | remoto C13 · pasta C16 (FAIL literal) | pasta `D:\esclari-system` (AD-059 a substitui) |
| ícones no manifesto (3) | plan AC 5 | 192, 512, maskable C5 | - |
| artefatos Docker antigos (7) | plan Impact | `one-system_pgdata`, `one-system_miniodata`, `one-nuget` e os 4 contêineres: `docker volume ls` e `docker ps -a` não listam nenhum `one*` (C12, 1ª prova) | - |
| startup config: nome do contêiner do e2e (2 places) | plan Flow | `instituicoes-real.spec.ts` C11 · `preparar-e2e.mjs` C10 e F5 | - |
| documentos vivos (3) | plan AC 14 | `ROADMAP.md` ok · `STATE.md` AD-059 ok, linha 77 ainda `Bitforgee/One` · `AGENTS.md` sem citação | C14 sem teste (gap 0) |
| claims com código de saída (C4, C8, C9, C10) | checks.md | C4 processo filho real; C8/C9/C10 `exec` injetado, mais C9 real; fronteira de processo só em C9 | exit 1 de `preparar-e2e.mjs` por processo real não coberto (F7 morreu só na camada injetada) |

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| Decide (criar ou pular; render ok ou Chromium indisponível) | `web/scripts/preparar-e2e.mjs`, `web/scripts/gerar-icones.mjs` | uma na própria camada com `exec` injetado e uma contra o Compose real | `preparar`: injetada (3 casos) e real só para "já existe" (C9); `criada` real não executada. `gerar-icones`: C4 por processo real; render ok verificado só pelos PNGs commitados (gap 3) |
| Instrumentação (nomes, URLs, `name:`) | `docker-compose.yml`, documentos | coberta por `ambiente.test.ts` e `NomeDoProdutoTests` | `name:` coberto (C6, F8). Documentos de S3: sem teste de AD novo (gap 0) |

## Faults injected

Mutações à mão no worktree temporário (`D:\esclari-verif-pend`), desfeitas por reescrita do arquivo; `git diff --quiet` saiu 0 depois de cada uma.

| Mutation | Location | Killed |
| --- | --- | --- |
| F1 glifo em `x = tamanho*0.02` (fora da zona segura), sem regenerar | `web/scripts/gerar-icones.mjs` `svg()`; `icones.test.ts` 5/5 passed | **no (sobreviveu)** |
| F1b o mesmo, depois de `npm run icones` regenerar os PNGs | idem; `icones.test.ts` 1 falha | yes (só após regenerar) |
| F1c cor da marca `#224fa7` para `#ff0000`, regenerando | idem; `icones.test.ts` 2 falhas | yes (só após regenerar) |
| F2 falha do Chromium sai com 0 | `gerar-icones.mjs` bloco `catch`; `icones.test.ts` 1 falha ("falha do chromium") | yes |
| F3 maskable com `purpose: 'any'` | `web/vite.config.ts`; `icones.test.ts` 1 falha | yes |
| F4 sem checagem de existência (sempre cria) | `web/scripts/preparar-e2e.mjs`; `preparar-e2e.test.ts` 1 falha | yes |
| F5 padrão `esclari-plataforma` em vez de `esclari-api-1` | `preparar-e2e.mjs`; `preparar-e2e.test.ts` 1 falha | yes |
| F6 mensagem de erro sem o nome do contêiner | `preparar-e2e.mjs`; `preparar-e2e.test.ts` 1 falha | yes |
| F7 contêiner ausente devolve 0 | `preparar-e2e.mjs`; `preparar-e2e.test.ts` 1 falha | yes |
| F8 `name: one` no compose | `docker-compose.yml:1`; `ambiente.test.ts` 1 falha | yes |
| F9 padrão do `E2E_CONTAINER` no spec vira `esclari-plataforma` | `web/e2e/instituicoes-real.spec.ts:4`; `ambiente.test.ts` 1 falha | yes |
| F10 e-mail do admin `e2e` vira `admin@e2e.one` | `preparar-e2e.mjs`; `preparar-e2e.test.ts` 1 falha | yes |
| F11 linha `https://github.com/Bitforgee/One` em `ROADMAP.md` | `NomeDoProdutoTests` 1 falha de 2, `ROADMAP.md:551` | yes (guarda) |
| F12 linha `D:\one-system` em `ROADMAP.md` | `NomeDoProdutoTests` 1 falha de 2 | yes (guarda) |

Sobreviveu F1 (gap 3). Os demais morreram.

## Pontos do construtor

- `4be6cfc` (`fix(e2e)`) mudou o CPF semeado para `12345678909`, fora dos checks (C8 não o exige); nenhum teste unitário o fixa.
- `867a03f` (`test(acesso)`) pertence à feature `renomear-esclari`; fora do escopo dos checks.
- O diff `6e648a4..HEAD` inclui a feature `indicadores-claros` (outra sessão); não foi verificado aqui.
- Nenhum teste de `esclari-pendencias` apagado ou pulado.

## Resíduos para o dono

1. **Emenda do C14 e do C16 (AD-059)**: trocar `D:\esclari-system` por `D:\esclari` nos dois checks e no plano (door 4, Assumptions). Sem a emenda, C16 fica FAIL; não a apliquei.
2. **C14**: autorizar o construtor a escrever `State_registra_o_AD_do_remoto_e_pasta` (AD-059 com `Bitforgee/Esclari` e `D:\esclari`), citar remoto e pasta no `AGENTS.md` e trocar `Bitforgee/One` na linha 77 do `STATE.md` (removendo a isenção do prefixo `**Branch**`).
3. **C12**: "ok" para apagar `demo-mwjmxeg9` e `operadora-mwjmxeg9`, ou aceitar que a rodada de e2e as recria (AD-036). A causa (specs criam `demo-*`/`operadora-*` a cada rodada) pede feature própria.
4. **C8 prova real**: autorizar a rodada com as duas instituições ausentes (apagar `demo` e `e2e` e rodar `e2e:preparar`), ou aceitar a lacuna por AD (AD-036).
5. **Gap 3**: aceitar o risco do teste que só lê os PNGs, ou pedir um teste que rode o gerador num diretório temporário.
6. Push dos commits locais (remoto em `8190316`): só com "ok" explícito.

## Gate

- Provas rodadas: web `icones.test.ts` (5 casos pelo `-t`), `ambiente.test.ts` (2), `preparar-e2e.test.ts` (3); `docker compose up -d --build`, `docker volume inspect`, `docker inspect`, `docker volume ls`, `psql`, `npm --prefix web run e2e:preparar`, `git remote get-url origin`, `git ls-remote`; `dotnet test --filter NomeDoProdutoTests...` (C14 casou 0, C15 1/1).
- Suíte completa .NET e web, `build`, `coverage`, `crapper` e `dryer` não foram reexecutados nesta rodada (a verificação cobre as provas dos 16 checks, não o gate global).
- `mutator`: suspenso (AD-025), não rodado.
- E2E de specs: não rodado (o único check com Compose real que dependia disso é o C8, ver gap 4).
- HEAD usado: `867a03f2f06689e78826201b537b05f84e767177`.
- `validate_verification.py esclari-pendencias`: exit 1, único erro: `verdict is FAIL` (estrutura do relatório aceita).


## Rodada 2 (escopada, fechamento)

HEAD `14b9146`. Examinados só os checks reprovados na rodada 1 (C12, C14, C16). Desde `867a03f`, nenhum commit tocou `.specs/features/esclari-pendencias`, `AGENTS.md` ou `NomeDoProdutoTests.cs` (só `7c4141b`, que libera a URL antiga entre aspas no feedback da banca).

| Check | Prova | Resultado |
| --- | --- | --- |
| C12 | `docker volume ls` sem `^one`: vazio. `select slug from instituicao where slug like 'demo-%' or slug like 'operadora-%'` no banco `esclari`: 2 linhas (`operadora-mwjmxeg9`, `demo-mwjmxeg9`), esperado 0 | FAIL: as duas linhas de e2e continuam, sem "ok" do dono para apagar |
| C14 | `NomeDoProdutoTests.cs` continua com 2 testes (`Nenhum_arquivo...`, `State_registra_o_AD_do_nome`), sem `State_registra_o_AD_do_remoto_e_pasta`; `AGENTS.md` sem `Bitforgee`; `STATE.md:100` (antes 77) ainda cita `Bitforgee/One` | FAIL: sem teste do AD do remoto e da pasta, `AGENTS.md` e a linha do Handoff sem correção |
| C16 | `checks.md:66` ainda exige `D:\esclari-system`; a pasta real é `D:\esclari` (AD-059), sem emenda do dono | FAIL (literal) |

Não verificado: C8 prova real, gap 3 (teste dos ícones não roda o gerador), push. Os demais checks (C1 a C11, C13, C15) não foram reexecutados; valem os PASS da rodada 1.
## Correção do dono (18/11, executada pelo orquestrador, não é verificação independente)

Com o "ok" do dono: C12, apagadas do banco local as instituições `operadora-mwjmxeg9` e `demo-mwjmxeg9` e as 12 linhas filhas (usuário, setor, categoria, token, e-mail), numa transação; resta `demo` e `e2e`. C14, `STATE.md:100` e `AGENTS.md` passam a citar `Bitforgee/Esclari`, e entra o teste `NomeDoProdutoTests.State_registra_o_AD_do_remoto_e_pasta` (1/1). C16, emenda E1 do check para a pasta real `D:\esclari` (AD-059). Provas rodadas: pasta `.git` existe, `git ls-remote` devolve HEAD, `docker volume ls` sem `one*`, consulta `demo-%`/`operadora-%` devolve 0. O Verdict acima segue FAIL até um verificador independente reexecutar; os objetos no MinIO dessas duas instituições não foram apagados (bucket versionado).
