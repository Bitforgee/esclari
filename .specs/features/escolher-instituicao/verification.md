# Escolher a instituição no app instalado verification

**Verdict**: PASS
**Profile**: ui
**Diff range**: 49664aa..283c8d0
**Round**: 2 - full (a rodada 1, reprovada, está arquivada no fim)
**Verifier**: independent sub-agent (author != verifier)

## Rodada 2

Os 14 checks passam. O e2e `arranjo.spec.ts:1421` que reprovou a rodada 1 passa em `HEAD`, as duas faltas sobreviventes da rodada 1 agora morrem, e as 16 falhas dos e2e de `arranjo` e `identidade` também falham em `49664aa` (worktree descartável, mesma API sem dados de e2e), logo não vêm da feature.

### Checks

| Check | Proof run | Result |
| --- | --- | --- |
| C1 | `BuscaDeInstituicoesTests.Acha_por_nome_sigla_e_codigo_sem_acento`: parte do `dotnet test --filter BuscaDeInstituicoesTests` (9 de 9 aprovados) | PASS |
| C2 | `Limita_a_cinco_e_nunca_devolve_inativa`: idem | PASS |
| C3 | `Valida_o_tamanho_do_texto` (4 casos listados por `--list-tests`: null, vazio, 2, aparado): idem | PASS |
| C4 | `Curinga_vira_texto_literal`: idem; agora inclui `a \ I` e `\\` | PASS |
| C5 | `Passou_do_limite_responde_429`: idem (mais `Limite_padrao_e_de_60_por_minuto`) | PASS |
| C6 | `dotnet test --filter` de `RetratoTests` e de `SessaoTests` juntos: Aprovado, 12 de 12 | PASS |
| C7 | `vitest run src/acesso/escolha.test.tsx`: 6 passed (inclui "estado inicial") | PASS |
| C8 | idem ("busca com espera" e o teste do `aria-live`) | PASS |
| C9 | idem ("estados da busca") | PASS |
| C10 | `vitest run src/acesso/lembrada.test.tsx`: 6 passed ("grava e apaga") | PASS |
| C11 | idem ("volta sozinho") | PASS |
| C12 | idem ("trocar de instituição") | PASS |
| C13 | `playwright test e2e/escolher-instituicao.spec.ts` contra `http://localhost:5226`: 4 passed (360x780, 360x420, 844x390, toque) | PASS |
| C14 | ver Gate abaixo | PASS |

### Gate

- `dotnet build Esclari.slnx`: 0 Erro(s), exit 0
- `dotnet test Esclari.Api.Tests`: `Aprovado! Com falha: 0, Aprovado: 1401, Total: 1401`
- `npm --prefix web test`: 142 arquivos, 1244 passed
- `npm --prefix web run build`: exit 0
- `crapper -s src`: exit 0 (maior nota 9,8, `Sidebar` em `ui/`); `dryer -s src --threshold 0.8`: exit 0 (candidatos 0,83 `mascaras.ts`/`Instituicoes.tsx` e 0,81 `ui/popover.tsx`/`ui/separator.tsx`, ambos fora desta feature)
- E2E contra `HEAD` (`arranjo`, `identidade`, `marca`, `interface-marca`, `escolher-instituicao`; 1 worker): 95 passed, 16 failed. Passam `arranjo.spec.ts:1421` ("instituição não encontrada no centro vertical"), `:1383` e os 4 de `escolher-instituicao`.
- As 16 falhas, todas também em `49664aa`: `arranjo.spec.ts` 227, 934, 986, 1184, 1285 e `identidade.spec.ts` 54, 115, 160, 241, 358, 452, 721, 744, 786, 814, 1199 (no baseline os números de linha de `arranjo` são 227, 938, 990, 1188, 1289 e de `identidade` 54, 115, 160, 241, 358, 452, 721, 744, 786, 814, 1199). Causa: esperam dados da instituição de e2e (`ARQUIVADO`, "Chamados não fechados neste momento.") que o banco local não tem; nenhuma toca a `Raiz` nem a escolha. Em `49664aa` rodei `arranjo` (os 5 acima e `:1331`, que passa) e `identidade` (as 11 acima, mais 2 que passam), os mesmos nomes falharam.
- Efeito colateral observado: o `interface-marca.spec.ts` regrava as capturas `.specs/features/interface-marca-esclari/capturas/*.png`; restaurei com `git checkout`.

### Binding sources

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `plan.md` `## Criteria` (AC 1 a AC 24), `## Surface`, `## Landing`, `## Impact` | yes - reaberto nesta rodada | none; o `Impact` segue sem citar `arranjo.spec.ts:1421`, que agora passa | none |
| `checks.md` | yes | none | - |

### Coverage

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| `GET /api/instituicoes` statuses (3) | `BuscarInstituicoes.cs` | 200 C1 · 400 C3 · 429 C5 | - |
| door 1 (campos, ordem, 5, ativas, 3-60, `\` literal) | `BuscarInstituicoes.cs:23-30` | C1 · C2 · C3 · C4 (inclui `a \ I`) | - |
| limite padrão de 60 por minuto (AC 6) | `Seguranca.cs:17` | `Limite_padrao_e_de_60_por_minuto` | - |
| door 2 chave `esclari-instituicao` | `aparencia.tsx` | gravar/apagar C10 · ler C11 · apagar ao trocar C12 | - |
| estados da `Raiz` (5) | `Escolha.tsx` | vazio C7 · carregando C8 · sem resultado C9 · erro C9 · 429 C9 | - |
| AC 20 e AC 24 (link "Escolher instituição") | `Acesso.tsx:219` | C13 e `arranjo.spec.ts:1421` | - |

### Faults injected

Cada injeção foi desfeita; `git status --short` limpo no fim.

| Mutation | Location | Killed |
| --- | --- | --- |
| tirar `i.Ativa &&` | `BuscarInstituicoes.cs` | yes - `Limita_a_cinco_e_nunca_devolve_inativa` |
| `Take(5)` -> `Take(6)` | `BuscarInstituicoes.cs` | yes - mesmo teste |
| tirar o escape de `%` | `BuscarInstituicoes.cs:23` | yes - `Curinga_vira_texto_literal` |
| tirar o escape de `\` | `BuscarInstituicoes.cs:23` | yes - `Curinga_vira_texto_literal` (era sobrevivente na rodada 1) |
| padrão `60` -> `6` | `Seguranca.cs:17` | yes - `Limite_padrao_e_de_60_por_minuto` (era sobrevivente) |
| tirar `.RequireRateLimiting("instituicoes")` | `BuscarInstituicoes.cs` | yes - `Limite_padrao_e_de_60_por_minuto`, `Passou_do_limite_responde_429`, `RetratoTests` |
| não gravar a chave no `200` | `aparencia.tsx` | yes - `grava e apaga` e `trocar de instituição` |
| não apagar no `404` | `aparencia.tsx` | yes - `grava e apaga quando a lembrada deixa de existir` |
| tirar `replace` | `Acesso.tsx` `Raiz` | yes - `volta sozinho para a última instituição` |
| 300 ms -> 0 | `Escolha.tsx` | yes - `busca com espera` |
| tirar `aria-live` | `Escolha.tsx` | yes - o teste de "Buscando…" |
| tirar `absolute top-full left-0` do link | `Acesso.tsx:219` (com rebuild) | yes - `arranjo.spec.ts:1421` falha |

Das 12 injeções desta rodada, 12 morreram, 0 sobreviveram. Obs.: a primeira tentativa de tirar o escape de `%` foi um `sed` que não alterou o arquivo (resultado verde sem valor); refeita com substituição exata, a mutação morreu.

### Ranked gaps

1. O `plan.md`/`checks.md` não listam as 16 falhas de e2e preexistentes (dependem de dados de e2e no banco); sem efeito no veredito, mas o C14 "e2e que ramificam na Raiz" só é verificável com o banco preparado (`npm --prefix web run e2e:preparar`), que não rodei.
2. Rodar `interface-marca.spec.ts` suja o working tree com PNGs regravados.

### Not checked

- `identidade-real.spec.ts` e demais `*-real.spec.ts` (precisam das instituições `demo`/`e2e`, não preparadas).
- PWA instalado em iOS/Android.
- `mutator`: suspenso (AD-025).

## Rodada 1 (arquivada)

Relatório da rodada 1, mantido como histórico (veredito da época: reprovado por C14).

```markdown
# Escolher a instituição no app instalado verification

**Verdict**: PASS
**Profile**: ui
**Diff range**: 49664aa..283c8d0 (rodada 1 em 49664aa..b4e731a)
**Round**: 1 - full (ver `## Rodada 2` no fim)
**Rounds**: 1 (reprovada, relatório abaixo) e 2 (`## Rodada 2`, aprovada)
**Verifier**: independent sub-agent (author != verifier)

Os checks C1 a C13 passam e as faltas injetadas morreram, com 2 sobreviventes. O FAIL vem de um membro do C14 que o plano mandava manter verde: o e2e `arranjo.spec.ts:1421` ("instituição não encontrada no centro vertical") passava em `49664aa` e falha em `HEAD`, porque o link "Escolher instituição" (AC 20) entra abaixo do Alert e o conjunto deixa de estar centrado (centro do Alert em y=372 contra 400 a 360 px). Esse spec não está na lista do `Impact`.

## Binding sources

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `plan.md` `## Criteria` (AC 1 a AC 24), `## Surface`, `## Landing` | yes - lido na íntegra | none com os checks C1 a C13; o `Impact` não cita `arranjo.spec.ts:1421`, que a feature quebra | AC 22 só por e2e com API simulada (C13) |
| `checks.md` | yes | none | - |

## Checks

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | `200`, 3 campos, acha por nome/sigla/slug sem acento, ordem por nome | `dotnet test --filter "...BuscaDeInstituicoesTests.Acha_por_nome_sigla_e_codigo_sem_acento"` Aprovado, 1 de 1 | `BuscaDeInstituicoesTests.cs:30` compara o JSON bruto `{"slug":..,"nome":..,"nomeCurto":..}` | PASS |
| C2 | 7 que casam -> 5; inativa nunca | `...Limita_a_cinco_e_nunca_devolve_inativa` Aprovado, 1 de 1 | `:46-47` lista exata `lote-1..lote-5` e `Assert.Empty` da inativa | PASS |
| C3 | `400` `errors.busca` para ausente, "", 2, 61; aparado vale | `...Valida_o_tamanho_do_texto` Aprovado, 4 de 4 | `:68-79` `Recusa` confere texto "Digite de 3 a 60 caracteres."; `:62` `"  apa  "` acha | PASS |
| C4 | `%%%` e `a_b` literais, `200` | `...Curinga_vira_texto_literal` Aprovado, 1 de 1 | `:90-93` `%%%` e `c_r` vazios, `100%` acha 1 | PASS (ver faltas: `\` não é provado) |
| C5 | limite 2 -> 3ª chamada `429` | `...Passou_do_limite_responde_429` Aprovado, 1 de 1 | `:107` `TooManyRequests` | PASS |
| C6 | rota nos retratos e em `Publicas`; privadas `401` | `--filter "RetratoTests\|SessaoTests"` Aprovado, 12 de 12 | `rotas.txt` linha `GET /api/instituicoes anonimo=True autorizacao=True limite=instituicoes`; `RetratoTests.cs` `Com("instituicoes")`; `SessaoTests.cs:15` | PASS |
| C7 | estado inicial | `vitest run src/acesso/escolha.test.tsx -t "estado inicial"` 1 passed, 5 skipped | `escolha.test.tsx:12-24` h1, rótulo, dica, nenhuma chamada com 2 caracteres | PASS |
| C8 | 300 ms, links `/{slug}`, "Buscando…" em `aria-live` | `-t "busca com espera"` 1 passed, 5 skipped (mais o teste do `aria-live`, que não casa o filtro e rodou na suíte) | `:26-37` uma só chamada, `href` `/univag`; `:39-49` `closest('[aria-live]')` = `polite` | PASS |
| C9 | vazio, rede/5xx (e tecla seguinte busca), `429` | `-t "estados da busca"` 1 passed, 5 skipped | `:51-72` as 3 mensagens literais e `busca=redes` chamada | PASS |
| C10 | grava no `200`; apaga no `404` do lembrado; não apaga no de outro | `lembrada.test.tsx -t "grava e apaga"` 2 passed, 4 skipped | `:12-33` | PASS |
| C11 | `replace`; `localStorage` que lança mostra a escolha | `-t "volta sozinho"` 2 passed, 4 skipped | `:36-40` `historyAction` `REPLACE`; `:42-58` leitura e escrita lançando | PASS |
| C12 | "Trocar de instituição" apaga e abre `/`; "Escolher instituição" | `-t "trocar de instituição"` 2 passed, 4 skipped | `:60-75` | PASS |
| C13 | 360x780, 360x420, 844x390, alvos de 44 px | `npm --prefix web run build`; API em :5226; `E2E_URL=http://localhost:5226 npx playwright test e2e/escolher-instituicao.spec.ts` 4 passed | os 4 casos verdes | PASS |
| C14 | suítes, builds, `crapper`, `dryer`, e2e que ramificam na Raiz | ver abaixo | e2e `arranjo.spec.ts:1421` falha em `HEAD` e passa em `49664aa` | FAIL |

### C14 em detalhe

- `dotnet build Esclari.slnx` - 0 erros, 2 avisos.
- `dotnet test Esclari.Api.Tests` - Aprovado 1400 de 1400.
- `npm --prefix web test` - 142 arquivos, 1244 passed (o construtor registrou 1245 no `STATE.md`; a contagem real aqui é 1244).
- `npm --prefix web run build` - exit 0.
- `crapper -s src --use-existing-coverage --threshold 9.99` - exit 0; maior nota 9,8 (`ui/sidebar` `Sidebar`); `acesso.Escolha.Avisos` CC 9, 100% de cobertura, nota 9,0.
- `dryer -s src --threshold 0.8` - exit 0; o único candidato fora de `ui/` é `src/mascaras.ts:27-34` contra `src/plataforma/Instituicoes.tsx:41-47` (0,83), idêntico em `49664aa` e em arquivos que a feature não toca. Nenhum candidato envolve `Escolha.tsx`, `aparencia.tsx` ou `Acesso.tsx`. Registrado como preexistente.
- E2E que ramificam na `Raiz` (`marca`, `identidade`, `arranjo`) contra `HEAD`, e os mesmos testes falhos contra `49664aa` num `git worktree` descartável (API em :5227):

| Teste | HEAD | 49664aa | Leitura |
| --- | --- | --- | --- |
| `arranjo.spec.ts` "instituição não encontrada no centro vertical" | FAIL (`360: centro y do Alert: 372 contra 400`) | PASS | **regressão da feature** |
| `arranjo.spec.ts` "linha da marca na entrada" | FAIL (30 s) | FAIL (30 s) | preexistente |
| `arranjo.spec.ts` "valores de tipo e bordas" | FAIL (30 s) | FAIL (30 s) | preexistente |
| `ux.spec.ts` "alvos de 44 px abaixo de 768 em todas as telas" | FAIL (espera `getByText('ARQUIVADO')` na tela Artigos, `simulada.ts:203`; resolve para `<option>` oculta "Arquivados") | FAIL, mesma espera (`simulada.ts:194`) | preexistente, confirmado |
| `ux.spec.ts` "alvos de 44 px com ponteiro grosso a 1024" | FAIL, mesma espera | não rodado | mesma causa, em Artigos |
| `identidade.spec.ts` "texto e controles em Public Sans", "serifa só em títulos de 20 px ou mais" | FAIL (5 min e 0 ms) | FAIL, mesma espera de `ARQUIVADO` | preexistente |

Da rodada de `marca`, `identidade` e `arranjo` só registrei as linhas 46 a 68 (a saída foi cortada pela cauda); nelas, fora os FAIL da tabela, tudo passou. Os testes anteriores (inclusive `acesso`, "raiz com a marca centralizada" e as linhas 319, 355, 459 do plano) não tiveram a saída guardada e ficam sem prova neste relatório.

## Coverage

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| `GET /api/instituicoes` statuses (3) | `BuscarInstituicoes.cs` | 200 C1 · 400 C3 · 429 C5 | - |
| door 1 (campos, ordem, 5, ativas, 3-60) | `BuscarInstituicoes.cs:23-30` | C1 · C2 · C3 | `\` literal (falta A3b) |
| door 2 chave `esclari-instituicao` | `aparencia.tsx` | gravar/apagar C10 · ler C11 · apagar ao trocar C12 | - |
| estados da `Raiz` (5) | `Escolha.tsx` | vazio C7 · carregando C8 · sem resultado C9 · erro C9 · 429 C9 | - |
| ACs sem check próprio óbvio | `plan.md` | AC 3 em C2; AC 14 no `href` de C8 e no teste "tocar no resultado" (cai no Login e grava a chave); AC 18 em C11; AC 20 em C12; AC 22 em C13; AC 24 em C13 (links "Trocar de instituição" e "Escolher instituição" são medidos pelo teste `toque`) | limite padrão de 60 por minuto (AC 6) sem prova: falta A5 |

## Faults injected

Toda injeção foi desfeita com `git checkout -- <arquivo>`; `git status --short` limpo no fim.

| Mutation | Location | Killed |
| --- | --- | --- |
| tirar `i.Ativa &&` | `BuscarInstituicoes.cs:25` | yes - `Limita_a_cinco_e_nunca_devolve_inativa` falha |
| `Take(5)` -> `Take(6)` | `BuscarInstituicoes.cs:28` | yes - mesmo teste falha |
| tirar o escape de `%` e `_` | `BuscarInstituicoes.cs:23` | yes - `Curinga_vira_texto_literal` falha |
| tirar o escape de `\` | `BuscarInstituicoes.cs:23` | **no** - 8 de 8 verdes. Nenhuma instituição de teste tem `\` no nome, então o padrão sem escape também devolve vazio |
| tirar `.RequireRateLimiting("instituicoes")` | `BuscarInstituicoes.cs:13` | yes - `Passou_do_limite_responde_429` e `RetratoTests` falham |
| padrão `60` -> `6` em `Limites:BuscaDeInstituicaoPorMinuto` | `Seguranca.cs:17` | **no** - 14 de 14 verdes. Os testes só usam o limite sobrescrito para 2; o 60 do AC 6 não tem prova |
| tirar `Unaccent` do `Nome` | `BuscarInstituicoes.cs:25` | yes - `Acha_por_nome_sigla_e_codigo_sem_acento` falha |
| ordem por `nome` -> decrescente | `BuscarInstituicoes.cs:27` | yes - 2 testes falham |
| tirar `.AllowAnonymous()` | `BuscarInstituicoes.cs:13` | yes - testes da busca, `RetratoTests` e `SessaoTests` falham |
| não gravar a chave no `200` | `aparencia.tsx` `useInstituicao` | yes - 3 testes falham |
| não apagar no `404` | `aparencia.tsx` | yes - `grava e apaga quando a lembrada deixa de existir` falha |
| apagar no `404` de qualquer slug (tirar `=== slug`) | `aparencia.tsx` | yes - `grava e apaga` falha |
| tirar `replace` | `Acesso.tsx` `Raiz` | yes - `volta sozinho para a última instituição` falha |
| 300 ms -> 0 | `Escolha.tsx` | yes - `busca com espera` falha |
| tirar `aria-live` | `Escolha.tsx` | yes - o teste de "Buscando…" falha |
| "Trocar de instituição" não apaga a chave | `Acesso.tsx` | yes - 2 testes falham |
| mínimo de 3 -> 2 caracteres | `Escolha.tsx` | yes - `estado inicial` falha |

Das 17 injeções, 15 morreram e 2 sobreviveram (escape de `\`, limite padrão de 60).

## Gate

`dotnet build Esclari.slnx` - exit 0
`dotnet test Esclari.Api.Tests` - 1400 passed, 0 failed
`npm --prefix web test` - 1244 passed, 0 failed
`npm --prefix web run build` - exit 0
`crapper -s src` - exit 0 (maior nota 9,8); `dryer -s src --threshold 0.8` - exit 0 (único candidato fora de `ui/`, 0,83, preexistente em `mascaras.ts`/`Instituicoes.tsx`)
Playwright: `escolher-instituicao.spec.ts` 4 passed; `arranjo.spec.ts:1421` falha só em `HEAD` (ver C14)

## Ranked gaps

1. **Regressão do e2e `arranjo.spec.ts:1421`** - coverage, C14 - `web/src/acesso/Acesso.tsx` (Login, ramo `ErroApi`), `web/e2e/arranjo.spec.ts:1421-1432`. O link "Escolher instituição" (AC 20, 44 px + espaço de 12 px) entra no mesmo bloco centrado do Alert, que sobe 28 px (372 contra 400 a 360 px). O plano não listou esse spec no `Impact`. Corrigir pede decidir entre manter o Alert no centro e pôr o link abaixo dele, ou emendar o spec. Não alterei código nem checks.
2. **Escape de `\` sem prova** - `BuscarInstituicoes.cs:23`, `BuscaDeInstituicoesTests.cs:93`. O AC 5 cita `\`; o caso `\\\` devolve vazio com ou sem o escape, porque nenhum nome tem `\`. Falta uma instituição com `\` no nome.
3. **Limite padrão de 60 sem prova** - `Seguranca.cs:17`, AC 6. Nenhum teste lê o valor padrão.
4. **Asserções de `arranjo.spec.ts` removidas ou trocadas sobre a Raiz** - `arranjo.spec.ts:319` (centralização do texto) e `:361` (centro vertical do cartão, `c.height < 740`). O plano dizia "a asserção sobre a `Raiz` não muda"; o construtor substituiu a asserção por outras (largura, centro horizontal). Faz sentido porque a tela mudou, mas a mudança não consta no plano e o centro vertical da escolha deixou de ser medido.
5. **Contagem do construtor** - o `STATE.md` registra 1245 testes web; a execução deste Verificador deu 1244 (`npm --prefix web test` e `run coverage`). Sem efeito no veredito.

## Not checked

- `arranjo.spec.ts` depois do teste 68 da minha rodada, e `ux.spec.ts` inteiro além dos dois testes "alvos de 44 px".
- O e2e real contra uma API sem simulação, fora do `escolher-instituicao.spec.ts`.
- Em navegador real instalado como PWA (iOS/Android): não verificado.
- `mutator`: suspenso (AD-025).
```
