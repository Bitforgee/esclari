# Limpeza de arquivos inúteis - checks

Profile: standard
Plan: `.specs/features/limpeza-de-arquivos/plan.md`

9 checks em 2 slices · 1 one-way door · 1 pergunta aberta (não bloqueia; plano aprovado pelo usuário em 24/11: "aprovo o plano ... crie os checks e construa")

**Como as provas rodam.** Na raiz `D:\esclari`, em Git Bash. `docker compose up -d postgres mailpit minio` antes do `dotnet test`. `<base>` é o commit anterior ao da limpeza. Mutação suspensa (AD-025). Sem comportamento novo: não há teste novo a escrever; a prova são comandos cujo exit code decide.

## Checks

### S1 - repositório sem arquivos versionados inúteis

**C1** - [ ] `git ls-files .tmp-apresentacao web/playwright.verify.config.ts` lista 0 arquivos (AC 1)
Proof: `test -z "$(git ls-files .tmp-apresentacao web/playwright.verify.config.ts)"`

**C2** - [ ] O commit da limpeza só apaga esses 8 arquivos e altera o `.gitignore`: `git diff --name-status <base>..HEAD` mostra 8 `D` e 1 `M` (`.gitignore`), mais os arquivos de `.specs/features/limpeza-de-arquivos/` (Impact)
Proof: `git diff --name-status <base>..HEAD -- . ':!.specs' | awk '{print $1}' | sort | uniq -c` sai `8 D` e `1 M`

**C3** - [ ] `dotnet build Esclari.slnx` e `npm --prefix web run build` saem com código 0 (AC 2)
Proof: `dotnet build Esclari.slnx`
Proof: `npm --prefix web run build`

**C4** - [ ] `dotnet test Esclari.Api.Tests` e `npm --prefix web test` terminam com 0 falhas (AC 3)
Proof: `dotnet test Esclari.Api.Tests`
Proof: `npm --prefix web test`

**C5** - [ ] Arquivo em `.tmp-teste/a.txt` e em `.artifacts-teste/a.txt` na raiz não aparece no `git status --porcelain` (AC 4)
Proof: `mkdir .tmp-teste .artifacts-teste && echo x > .tmp-teste/a.txt && echo x > .artifacts-teste/a.txt && test -z "$(git status --porcelain | grep -E 'tmp-teste|artifacts-teste')"; r=$?; rm -r .tmp-teste .artifacts-teste; exit $r`

### S2 - pasta local sem sobras

**C6** - [ ] Nenhum caminho da lista "apagar" existe: `.artifacts-agente-v`, `.playwright-mcp`, `demo-live-*.png`, `node_modules` (raiz), `TestResults`, `Esclari.Api.Tests/TestResults`, `__pycache__` em `.specs/features/refatoracao-solid`, e em `.tmp-capturas` tudo menos `deploy` e `readme` (AC 5)
Proof: `test -z "$(ls -d .artifacts-agente-v .playwright-mcp demo-live-*.png node_modules TestResults Esclari.Api.Tests/TestResults 2>/dev/null; find .specs/features/refatoracao-solid -name __pycache__; ls .tmp-capturas | grep -vxE 'deploy|readme')"`

**C7** - [ ] Seguem existindo `.env` (sha256 igual ao de antes: começa em `1a81915be7145792`), `.idea/`, `.metrics/`, `web/node_modules/`, `.tmp-capturas/deploy/docker-compose.yml` e `.tmp-capturas/readme/` (AC 6)
Proof: `test -d .idea && test -d .metrics && test -d web/node_modules && test -f .tmp-capturas/deploy/docker-compose.yml && test -d .tmp-capturas/readme && sha256sum .env | grep -q ^1a81915be7145792`

**C8** - [ ] Rodar a limpeza local uma segunda vez, com os caminhos já ausentes, sai com código 0 (AC 7)
Proof: o mesmo bloco `Remove-Item -ErrorAction SilentlyContinue` da lista fechada, rodado de novo, sai 0 e C6 segue verde

**C9** - [ ] `git status --porcelain` sai vazio fora de `.specs/features/limpeza-de-arquivos/` (AC 8)
Proof: `test -z "$(git status --porcelain | grep -v limpeza-de-arquivos)"`

## One-way doors

**D1** - [ ] A limpeza local usa `Remove-Item -Recurse -Force` caminho por caminho sobre a lista fechada, nunca `git clean -fdX` (Landing)
Proof: C7 (o `.env`, `.idea/`, `.metrics/` e `web/node_modules/` sobrevivem; `git clean -fdX` os apagaria)

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| critérios de aceite (8) | 1 C1 · 2 C3 · 3 C4 · 4 C5 · 5 C6 · 6 C7 · 7 C8 · 8 C9 | - |
| versionados a remover (2 itens, 8 arquivos) | `.tmp-apresentacao/` C1, C2 · `playwright.verify.config.ts` C1, C2 | - |
| one-way doors (1) | D1 | - |
| rotas (0) | n/a - sem Surface | - |
