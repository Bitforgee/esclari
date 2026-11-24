# Limpeza de arquivos inúteis

## Problem

A pasta `D:\esclari` carrega cerca de 180 MB de arquivos que nenhum build, teste, script ou documento usa,
e dois desses grupos já estão versionados no Git. Quem clona o repositório baixa 7 capturas de tela que
ninguém cita (`.tmp-apresentacao/`, 540 KB). Elas entraram por acidente no commit `19f4e50`
(`feat(plataforma)`): os Verifies de 17/11 ainda as listavam como `?? .tmp-apresentacao/`, ou seja,
fora do Git (`.specs/features/emails-com-marca/verification.md:83`). Quem trabalha na pasta local
precisa conviver com `git status` sempre mostrando `?? .tmp-capturas/` e com 116 MB de `bin/obj` que
sobraram de um Verify (`.artifacts-agente-v/`). Cada Verify novo tem que explicar esse ruído no
`verification.md` (`filtros-e-relatorios/verification.md:173`).

Nada disso é código órfão. A varredura de `web/src` e de `Esclari.Api` não achou nenhum `.ts`, `.tsx`
ou `.cs` que ninguém importa ou referencia. O lixo está só em pastas temporárias e em sobras de
ferramentas.

Depois da limpeza, `git status` sai limpo, o clone fica sem as capturas soltas e uma pasta
`.tmp-*` ou `.artifacts-*` criada por um agente não entra mais num commit por engano.

### Inventário

| Caminho | Situação | Tamanho | Evidência de que é inútil | Destino |
| --- | --- | --- | --- | --- |
| `.tmp-apresentacao/` (7 PNG) | versionado | 540 KB | nenhuma referência no repositório; entrou por acidente em `19f4e50` | `git rm` |
| `web/playwright.verify.config.ts` | versionado | 9 linhas | nenhum script, doc ou spec usa (`git grep playwright.verify` só acha o próprio arquivo) | `git rm` (ver Assumptions) |
| `.artifacts-agente-v/` | local, ignorado | 116 MB | só `bin/` e `obj/` de um Verify de 16/11 | apagar |
| `.playwright-mcp/` | local, ignorado | 54 MB | 180 capturas de sessões do MCP Playwright | apagar |
| `demo-live-*.png` (15, na raiz) | local, ignorado | 1,2 MB | capturas de 12/11; o `.gitignore` já tem `/demo-*.png` | apagar |
| `.tmp-capturas/` menos `deploy/` e `readme/` | local, não ignorado | 2 MB | capturas, logs (`api.log`, `vite.err`) e rascunhos de 18 e 19/11 | apagar |
| `node_modules/` da raiz | local, ignorado | 4 MB | só `.vite/` e `.vitest-cache/`; não há `package.json` na raiz | apagar |
| `TestResults/` e `Esclari.Api.Tests/TestResults/` | local, ignorado | pequeno | resultados de `dotnet test` antigos | apagar |
| `.specs/features/refatoracao-solid/**/__pycache__/` | local, ignorado | pequeno | bytecode Python gerado | apagar |

### O que fica (parece lixo, mas não é)

| Caminho | Por que fica |
| --- | --- |
| `.tmp-capturas/deploy/docker-compose.yml` | compose de produção (`esclari-prod`, senha por `${POSTGRES_SENHA}`), que não existe em nenhum outro lugar |
| `.tmp-capturas/readme/*.mjs` | scripts que geram as `docs/imagens/*.png` citadas no `README.md` |
| scripts `.py`/`.sh`/`.ps1` em `.specs/features/*/` | são a prova (`Proof:`) de checks de features fechadas; sem eles a re-verificação quebra |
| capturas e conceitos de marca em `.specs/features/*marca*` | registram as decisões dos ADs de marca (AD-062 a AD-065) |
| `.specs/devolutiva-banca-17-11.md`, `prompt-*.md`, `relatorio-nome.md` | registros do projeto; `prompt-mobile-first.md` é citado no `AGENTS.md` e `relatorio-nome.md` em `NomeDoProdutoTests` |
| `web/apresentacao/` | ferramenta de gravar o vídeo, citada no `ROADMAP.md` e no `.gitignore` |
| `.env`, `.idea/`, `.metrics/` | segredo, configuração pessoal da IDE e pasta que o `AGENTS.md` proíbe tocar |
| `bin/`, `obj/`, `wwwroot/`, `web/node_modules/`, `web/coverage/`, `web/test-results/` | saída de build e teste, recriada a cada execução e já ignorada |

## Flow

Usa o `.gitignore` que já existe para evitar que o problema volte. Não cria script de limpeza novo.

1. inventário acima -> `git rm -r` dos 2 itens versionados, num commit `chore(repo): ...`
2. `.gitignore` (exists) ganha `.tmp-*/` e `.artifacts-*/`, no mesmo commit
3. lista fechada de caminhos locais -> `Remove-Item -Recurse -Force` caminho por caminho (door 1); nada vai para o Git
4. out: `git status --porcelain` vazio, build e suítes iguais a antes

## Impact

| Front | What changes |
| --- | --- |
| domain | nothing - nenhum termo, entidade ou rota muda |
| stored data | nothing to migrate - nenhum banco, migration ou bucket é tocado |
| repositório | 8 arquivos versionados a menos; `.gitignore` com 2 linhas a mais |
| pasta local | cerca de 177 MB a menos; os arquivos locais apagados não têm cópia no Git |
| Verifies futuros | a linha "porcelain antes: `?? .tmp-capturas/`" deixa de aparecer |

## Relations

None - no stored-data shape change

## Surface

None - nothing consumed outside

## Landing

| One-way door | Literal shape | Alternative rejected |
| --- | --- | --- |
| apagar arquivos locais que o Git não guarda | `Remove-Item -Recurse -Force` sobre a lista fechada do inventário, um caminho por vez, com `.tmp-capturas/deploy/` e `.tmp-capturas/readme/` fora dela | `git clean -fdX`: a lista é aberta e apagaria também o `.env` (chave da IA), `.idea/`, `.metrics/` e `web/node_modules/` |

- Nothing else in this change is hard to reverse: o `git rm` volta com `git revert`, e o `.gitignore` volta numa linha.

## Criteria

### S1: o repositório sem arquivos versionados inúteis (P1)

O clone deixa de trazer as capturas soltas e a configuração sem uso, e nada quebra.

**Acceptance Criteria**

1. WHEN o commit da limpeza estiver na `main` THEN `git ls-files .tmp-apresentacao web/playwright.verify.config.ts` SHALL listar 0 arquivos
2. WHEN o commit da limpeza estiver na `main` THEN `dotnet build Esclari.slnx` e `npm --prefix web run build` SHALL sair com código 0
3. WHEN o commit da limpeza estiver na `main` THEN `dotnet test Esclari.Api.Tests` e `npm --prefix web test` SHALL terminar com 0 falhas
4. WHEN um arquivo for criado em `.tmp-teste/a.txt` ou `.artifacts-teste/a.txt` na raiz THEN `git status --porcelain` SHALL não listar esse caminho

**Independent test:** `git ls-files .tmp-apresentacao` vazio e as quatro provas verdes num checkout limpo.

### S2: a pasta local sem sobras (P2)

A pasta de trabalho fica só com o que o Git guarda e com os arquivos que o desenvolvedor usa.

**Acceptance Criteria**

5. WHEN a limpeza local rodar THEN nenhum caminho da lista "apagar" do inventário SHALL existir em `D:\esclari`
6. The limpeza local SHALL deixar existindo, com o mesmo conteúdo, `.env`, `.idea/`, `.metrics/`, `web/node_modules/`, `.tmp-capturas/deploy/docker-compose.yml` e `.tmp-capturas/readme/`
7. IF um caminho da lista já não existir THEN a limpeza local SHALL seguir para o próximo sem erro
8. WHEN a limpeza local terminar THEN `git status --porcelain` SHALL sair vazio, com exceção de `.tmp-capturas/` enquanto `deploy/` e `readme/` estiverem lá

**Independent test:** `Test-Path` em cada caminho da lista (falso) e em cada caminho preservado (verdadeiro), mais o hash do `.env` igual antes e depois.

## Out of scope

| Excluded | Why |
| --- | --- |
| apagar código morto dentro de arquivos usados (ex.: `AddIndicadores` que só devolve `s`) | é refatoração de código, não arquivo inútil; vai noutra feature se o dono quiser |
| enxugar o histórico do Git (reescrever commits para tirar os PNGs) | exige `push --force` na `main` compartilhada por duas pessoas |
| podar `.specs/features/*` de features fechadas | são as provas e o registro das decisões |
| versionar o compose de produção | é decisão de implantação, não de limpeza (ver Open questions) |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| `web/playwright.verify.config.ts` (e2e contra `vite preview` na 4173, sem Docker) | remover | nada o usa desde 11/11; volta com `git revert` se fizer falta | n |
| `.tmp-capturas/deploy/` e `.tmp-capturas/readme/` | ficam onde estão, fora da limpeza | são a única cópia do compose de produção e dos scripts das imagens do README | n |
| o outro desenvolvedor tem as mesmas sobras locais | a limpeza local vale só para esta máquina; o commit leva o `.gitignore` para os dois | o S2 não passa pelo Git | n |

**Open questions:**

| # | Kind | Question | Until answered |
| --- | --- | --- | --- |
| 1 | open | Versionar `.tmp-capturas/deploy/docker-compose.yml` (ex.: `deploy/docker-compose.yml`) e os scripts de `readme/` em `web/scripts/`, para poder apagar `.tmp-capturas/` inteira? | o critério 8 mantém a exceção de `.tmp-capturas/` |

## Observable

None - no user-facing surface

## Sources

- commit `19f4e50`: onde `.tmp-apresentacao/` entrou no Git
- `.specs/features/filtros-e-relatorios/verification.md:173` e `emails-com-marca/verification.md:83`: as pastas temporárias registradas como fora do Git antes desse commit
