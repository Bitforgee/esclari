# esclari-pendencias checks

Profile: standard
Plan: `.specs/features/esclari-pendencias/plan.md`

16 checks in 3 slices · 4 one-way doors · 0 open that block after the user's "pode seguir pelo recomendado" (que aceita o glifo "E", os nomes `Bitforgee/Esclari` e `D:\esclari-system` e a limpeza do Docker)

Os testes são escritos a partir destes checks, nunca lendo a implementação. A `verificação` roda em agente novo.

## Checks

### S1 - o app instalado mostra a marca Esclari · 4 arquivos · 60 KB · ~15k

**C1** - `icon-192.png` tem 192×192, `icon-512.png` 512×512 e `icon-maskable-512.png` 512×512 (AC 1)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "dimensoes dos tres icones"`

**C2** - No ícone maskable, todo pixel branco do glifo fica dentro do círculo central de diâmetro 410 px (80% de 512) e o resto é a cor da marca (AC 2)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "maskable dentro da zona segura"`

**C3** - No ícone 192, o canto superior esquerdo tem a cor de `theme_color` do manifesto, há pixels brancos, e a imagem não é igual à própria rotação de 180° (o anel do One era simétrico) (AC 3)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "icone any branco sobre a cor do manifesto"`

**C4** - Com o Chromium indisponível, `npm --prefix web run icones` sai com código `1`, imprime uma mensagem em português e os três PNGs mantêm o mesmo SHA-256 (AC 4)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "falha do chromium"`

**C5** - O manifesto lista `/icons/icon-192.png` e `/icons/icon-512.png` com `purpose` `any` e `/icons/icon-maskable-512.png` com `maskable` (AC 5)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "manifesto lista os tres icones"`

### S2 - o ambiente de desenvolvimento sobe do zero com um comando · 6 arquivos · 40 KB · ~10k

**C6** - A primeira linha de `docker-compose.yml` é `name: esclari` e `docker compose config --format json` rodado de outra pasta mostra `"name": "esclari"` (AC 6)
Proof: `npm --prefix web test -- src/test/ambiente.test.ts -t "nome do projeto compose"`

**C7** - `docker compose up -d` cria os volumes `esclari_pgdata` e `esclari_miniodata` e o contêiner `esclari-api-1` (AC 7)
Proof: `docker compose up -d --build ; docker volume inspect esclari_pgdata esclari_miniodata ; docker inspect esclari-api-1`

**C8** - `e2e:preparar` com as duas instituições ausentes cria `demo` e `e2e` com os administradores `admin@demo.esclari` e `admin@e2e.esclari` e imprime `criada demo` e `criada e2e` (AC 8)
Proof: `npm --prefix web test -- src/test/preparar-e2e.test.ts -t "cria demo e e2e"`
Proof: `npm --prefix web run e2e:preparar` (contra a API do compose, com as duas ausentes)

**C9** - Na segunda rodada imprime `ja existe demo` e `ja existe e2e`, sai com `0` e não chama o comando de criar (AC 9)
Proof: `npm --prefix web test -- src/test/preparar-e2e.test.ts -t "segunda rodada ja existe"`
Proof: `npm --prefix web run e2e:preparar` (segunda execução, contra a API do compose)

**C10** - Com o contêiner fora do ar sai com `1` e a mensagem cita o contêiner tentado (AC 10)
Proof: `npm --prefix web test -- src/test/preparar-e2e.test.ts -t "conteiner ausente sai 1"`

**C11** - Sem `E2E_CONTAINER`, o e2e `instituicoes-real` usa `esclari-api-1` (AC 11)
Proof: `npm --prefix web test -- src/test/ambiente.test.ts -t "container padrao do e2e"`

**C12** - Depois da limpeza autorizada, `docker volume ls` não tem nome começando por `one`, e o banco `esclari` não tem instituições `demo-*` nem `operadora-*` (AC 12)
Proof: `docker volume ls --format "{{.Name}}" | Select-String "^one" ; if ($?) { exit 1 }`
Proof: `docker compose exec -T postgres psql -U postgres -d esclari -At -c "select count(*) from instituicao where slug like 'demo-%' or slug like 'operadora-%'"` com saída `0`

### S3 - repositório e pasta se chamam Esclari · 5 arquivos · 90 KB · ~22k

**C13** - `git remote get-url origin` responde `https://github.com/Bitforgee/Esclari` e `git ls-remote origin HEAD` sai com 0 (AC 13)
Proof: `git remote get-url origin ; git ls-remote origin HEAD`

**C14** - `AGENTS.md`, `ROADMAP.md` e `STATE.md` citam `Bitforgee/Esclari` nas linhas vivas, `AGENTS.md` e `STATE.md` citam a pasta `D:\esclari` (emenda E1, 18/11, AD-059) e o `STATE.md` tem um AD novo que registra a mudança (AC 14)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~NomeDoProdutoTests.State_registra_o_AD_do_remoto_e_pasta"`

**C15** - O guarda `Nenhum_arquivo_rastreado_cita_One_fora_do_historico` passa com as exceções `github.com/Bitforgee/One` e `D:\one-system` removidas da lista (AC 15)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~NomeDoProdutoTests.Nenhum_arquivo_rastreado_cita_One_fora_do_historico"`

**C16** - Os commits de S3 só existem depois que a pasta `D:\esclari` e o remoto novo existem (AC 16; emenda E1, 18/11, AD-059: a pasta real é `D:\esclari`, não `D:\esclari-system`)
Proof: `if (-not (Test-Path D:\esclari\.git)) { exit 1 } ; git ls-remote https://github.com/Bitforgee/Esclari HEAD`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| `npm run icones` statuses (2) | 200 exit 0 C1 · 500 exit 1 C4 | - |
| `npm run e2e:preparar` statuses (2) | 200 exit 0 C8 · 500 exit 1 C10 | - |
| `manifest.webmanifest` statuses (1) | 200 C5 | - |
| Landing door 1: nome do projeto Compose (3 efeitos) | `name` C6 · volumes C7 · contêiner C7 | - |
| Landing door 2: ícones (3 arquivos) | 192 C1 · 512 C1 · maskable C2 | - |
| Landing door 3: comando de preparo (2 instituições) | `demo` C8 · `e2e` C8 | - |
| Landing door 4: nome do repositório e da pasta (2) | remoto C13 · pasta C16 | - |
| ícones no manifesto (3) | 192 C5 · 512 C5 · maskable C5 | - |
| artefatos Docker antigos (7) | `one-system_pgdata` C12 · `one-system_miniodata` C12 · `one-nuget` C12 · `one-system-api-1` C12 · `one-system-postgres-1` C12 · `one-system-minio-1` C12 · `one-system-mailpit-1` C12 | - |
| startup config: nome do contêiner do e2e (2 places) | `instituicoes-real.spec.ts` C11 · `preparar-e2e.mjs` C10 | - |
| documentos vivos (3) | `AGENTS.md` C14 · `ROADMAP.md` C14 · `STATE.md` C14 | - |

- Claims que citam código de saída: C4, C8, C9, C10; cada prova cruza a fronteira do comando (processo filho ou exit real)
- Celular deitado, teclado aberto e toque: n/a, o ícone não é tela (plan.md, Observable)

## Test policy

| Code | Required proofs | Coverage expectation |
| --- | --- | --- |
| Decide (preparar `demo`/`e2e`: criar ou pular; gerador de ícone: sucesso ou falha do Chromium) | uma na própria camada com `exec` injetado **e** uma contra o Compose real | um caso por linha da decisão (criada, já existe, contêiner ausente) |
| Instrumentação (nomes e URLs em documentos, `name:` no compose) | nenhuma própria | coberta por `ambiente.test.ts` e `NomeDoProdutoTests` |

Evidence:

- `web/scripts/preparar-e2e.mjs` (novo): 3 decisões (existe, não existe, contêiner ausente) -> decisão
- `web/scripts/gerar-icones.mjs` (novo): 2 decisões (render ok, Chromium indisponível) -> decisão
- `docker-compose.yml`: uma linha de configuração, sem condicional -> instrumentação
- analogia mais próxima: `web/scripts/entrada-sem-canvas.mjs` e seus testes em `web/src/test/`

Cost: 2 arquivos de teste novos (`icones.test.ts`, `preparar-e2e.test.ts`) mais `ambiente.test.ts`, 8 casos.

## Swept

- validation: C2 (zona segura), C9 (slug já existente não duplica)
- failure modes: C4 (Chromium), C10 (contêiner ausente)
- idempotency: C9
- authorization: n/a - o preparo roda no contêiner local de desenvolvimento, sem rota nova e sem papel
- concurrency: n/a - comandos de desenvolvimento rodados por uma pessoa; a unicidade do slug já existe na API (C9)
- data lifecycle: C12 (volumes antigos e dados de teste), a mudança do PWA já instalado não migra (plan.md, Impact)
- dependency failure: C4, C10
- state transitions: n/a - nenhuma máquina de estados envolvida
- observability: n/a - nenhuma exigência de log; cada comando imprime uma linha por ação (C8, C9)

## Out of scope

- Logotipo completo e identidade visual - precisa de designer
- Domínio, DNS e certificado de `esclari.com.br`
- Corrigir `crapper` (`useModo`, `Usuarios`) e `chat.spec.ts:47`, que já falhavam na base

## Handoff

- S1 = ~15k (ícones, `vite.config.ts`, testes); S2 = ~10k (compose, dois `.mjs`, spec e2e); S3 = ~22k (`AGENTS.md`, `ROADMAP.md` 40 KB, `STATE.md` 50 KB, `NomeDoProdutoTests.cs`). Total ~47k, abaixo dos 150k: um construtor por lote.
- S1 e S2 num único construtor (~25k). S3 depois que o dono renomear (ou autorizar) o repositório e a pasta, por outro construtor.
- Mechanism: handoff (um subagente construtor por lote, em sequência, só no verde) - decisão do dono, "usar subagentes"
