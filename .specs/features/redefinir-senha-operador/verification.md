# Redefinir a senha de um operador da plataforma verification

**Verdict**: PASS
**Profile**: standard
**Diff range**: be6d4d3..HEAD (b10cbb3)
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

## Checks

Proofs run at HEAD b10cbb3 in one batch: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaComandoTests"` exit 0, 10 passed, 0 failed (each of the 10 tests listed individually as Passed in the TRX). `dotnet build Esclari.slnx` exit 0, 0 errors, so no CA1502 break.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | exit 0, link impresso, convite agora + 60 min, token aceito 204, login com senha nova 200 | filtro `Redefinir_senha_imprime_o_link_de_60_minutos_e_a_senha_nova_entra` (test exists at line 98, Passed) exit 0 | `Esclari.Api.Tests/PlataformaComandoTests.cs:104` `Assert.Equal((0, ""), (codigo, erro))`; `:107` `Assert.Contains("http://localhost:8080/plataforma/definir-senha?token=", saida)`; `:108` `Assert.Equal(f.Relogio.GetUtcNow().UtcDateTime.AddMinutes(60), (await Linha(operador.Email)).ConviteExpiraEm)`; `:109` `Assert.Equal(HttpStatusCode.NoContent, ...definir-senha...)`; `:110` `Assert.Equal(HttpStatusCode.OK, (await f.Cliente().EntrarComSenha(...)).StatusCode)` | PASS |
| C2 | TotpSegredo segue preenchido, TemSegundoFator verdadeiro | filtro `Redefinir_senha_mantem_o_segundo_fator` (line 114, Passed) exit 0 | `PlataformaComandoTests.cs:123` `Assert.NotNull(depois.TotpSegredo)`; `:124` `Assert.Equal(antes.TotpSegredo, depois.TotpSegredo)`; `:125` `Assert.True(depois.TemSegundoFator)` | PASS |
| C3 | exatamente 1 auditoria OPERADOR_ALTERADO com o texto exato | filtro `Redefinir_senha_audita_o_comando` (line 129, Passed) exit 0 | `PlataformaComandoTests.cs:135` `Assert.Equal(1, await Auditorias(operador.Id))`; `:137` `Assert.Equal($"Comando: novo link de senha para {operador.Nome}", detalhe)` | PASS |
| C4 | link novo invalida o anterior: 400 e depois 204 | filtro `Redefinir_senha_invalida_o_link_anterior` (line 141, Passed) exit 0 | `PlataformaComandoTests.cs:148` `Assert.Equal(HttpStatusCode.BadRequest, ...token = primeiro...)`; `:149` `Assert.Equal(HttpStatusCode.NoContent, ...token = segundo...)` | PASS |
| C5 | e-mail desconhecido e inválido saem com 1 e as mensagens exatas, sem auditoria nem convite | filtro `Redefinir_senha_recusa_email_desconhecido_e_invalido` (line 153, Passed) exit 0 | `PlataformaComandoTests.cs:161` `Assert.Equal(1, sem.Codigo)`; `:162` `Assert.Contains("--email: não há operador com esse e-mail", sem.Erro)`; `:163-164` idem código 1 e `Assert.Contains("--email: informe um e-mail válido", invalido.Erro)`; `:165` `Assert.Equal("", sem.Saida + invalido.Saida)`; `:166` contagem de auditoria igual à anterior; `:167` `Assert.False(...AnyAsync(o => o.Email == desconhecido))` | PASS |
| C6 | ações antigas intactas | filtro `PlataformaComandoTests` (10 passed) exit 0 | `PlataformaComandoTests.cs:28` criar (`:41` `Assert.Equal(1, await Auditorias(operador.Id))`); `:46` convidar de novo e recusa (`:61` `Assert.Contains("Esse operador já existe.", depois.Erro)`); `:65` desativar/reativar/2FA e recusas (`:73` `Assert.False((await Linha(a.Email)).Ativo)`, `:77` `Assert.True(...Ativo)`, `:82` `Assert.Equal((null, null), (depois.TotpSegredo, depois.TotpUltimoPasso))`, `:87` `Assert.Contains("não há operador", ...)`, `:92` `Assert.Contains("último operador ativo", ultimo.Erro)`) | PASS |
| C7 | build não quebra por CA1502 | `dotnet build Esclari.slnx` exit 0 | build output: 0 Erro(s), 15 avisos (nenhum CA1502) | PASS |

Precision notes (not failures): C5 uses `Assert.Contains` for the two error strings, which is weaker than equality but still contains the full claimed string, and stderr carries only that line. The C5 claim "nem convite" is proven by the unknown e-mail producing no operator row (`:167`); the invalid e-mail case has no row to check, which is inherent.

## Coverage

Members recomputed from `Esclari.Api/Plataforma/PlataformaComando.cs`: `Acoes` (line 9, 4 entries), `Executar` branches (e-mail inválido line 38, `acao is null` line 40), `CriarOuConvidar` (lines 43-57), `Alterar` (lines 59-76), `NovoLinkDeSenha` (lines 78-86).

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| Acoes (4) | `PlataformaComando.cs:9` | `--desativar` C6 test line 70 · `--reativar` C6 line 75 · `--redefinir-2fa` C6 line 79 · `--redefinir-senha` C1 line 102 | - |
| entradas de `--redefinir-senha` (3) | `Alterar` line 60 and `Executar` line 38 | operador existente C1 · e-mail desconhecido C5 line 162 · e-mail inválido C5 line 164 | - |
| efeitos de `NovoLinkDeSenha` (4) | `PlataformaComando.cs:80-85` | convite 60 min C1 line 108 · senha nova entra C1 line 110 · 2FA preservado C2 line 124 · auditoria C3 line 137 · convite anterior invalidado C4 line 148 | - |
| recusas de `Alterar` (2) | `PlataformaComando.cs:60,62` | operador inexistente `:87` · último operador ativo `:92` | - |
| ramos de `CriarOuConvidar` ligados ao check C6 (3) | `PlataformaComando.cs:45,50-51` | criar `:28` · convidar de novo `:46` · já existe `:61` | - |

Observation outside the checks (not a member of any set the feature claims): two pre-existing branches have no test anywhere, the `--nome: informe de 3 a 120 caracteres` refusal (`PlataformaComando.cs:47`) and the `uso: plataforma operador ...` message for a first argument other than `operador` (`:21`). Search: `Select-String Esclari.Api.Tests/*.cs "informe de 3 a 120","uso: plataforma"` returned no hits. The diff does not touch them and C6 does not claim them.

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| `ComandoOperadorDaPlataforma.Alterar` decide por ação: uma na própria camada, atravessando `PlataformaComando.Executar`, um caso por ação e por recusa | `Esclari.Api/Plataforma/PlataformaComando.cs` | 4 ações: C6 test (3 antigas) e C1 (nova), todas via `PlataformaComando.Executar` (`PlataformaComandoTests.cs:17`); recusas de `Alterar`: inexistente (`:87`, `:162`) e último ativo (`:92`) | yes |

## Faults injected

Scratch worktree `D:\esclari-verify-scratch2` at HEAD, each mutation confirmed applied with `git diff --stat` (1 file, `PlataformaComando.cs`), narrowest covering test run in the scratch.

| Mutation | Location | Killed |
| --- | --- | --- |
| `NovoConvite(agora)` -> `NovoConvite(agora.AddMinutes(-30))` in `NovoLinkDeSenha` (validade errada) | `PlataformaComando.cs:80` | yes - `Redefinir_senha_imprime_o_link_de_60_minutos_e_a_senha_nova_entra` failed |
| removed `db.AuditoriaDaPlataforma.Add(...)` in `NovoLinkDeSenha` | `PlataformaComando.cs:81` | yes - `Redefinir_senha_audita_o_comando` failed |
| added `operador.RedefinirSegundoFator()` after `NovoConvite` in `NovoLinkDeSenha` | `PlataformaComando.cs:80` | yes - `Redefinir_senha_mantem_o_segundo_fator` failed (first attempt did not apply because of CRLF, then redone and confirmed by diff stat) |
| dropped `--redefinir-senha` from `Acoes` | `PlataformaComando.cs:9` | yes - `Redefinir_senha_invalida_o_link_anterior` failed |
| unknown e-mail creates an operator instead of refusing | `PlataformaComando.cs:60` | yes - `Redefinir_senha_recusa_email_desconhecido_e_invalido` failed |

The scratch worktree was removed with `git worktree remove --force`. `git status --porcelain` of D:\esclari before and after: `?? .tmp-capturas/` only (the verifier's `Esclari.Api.Tests/TestResults/v.trx` is git-ignored).

## Gate

`dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaComandoTests"` - 10 passed, 0 failed; `dotnet build Esclari.slnx` - 0 errors
