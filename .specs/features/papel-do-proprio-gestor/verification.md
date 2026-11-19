# Papel do próprio gestor verification

**Verdict**: PASS
**Profile**: standard
**Diff range**: e8982f6..1ce7bfa (e3976ea só adiciona um plan.md de outra feature)
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

## Binding sources

No binding source - `checks.md` tem só `## Intent`, sem `plan.md`. Fonte de autoridade usada: `AGENTS.md` e `.specs/STATE.md`.

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `AGENTS.md` (arquitetura da API, testes, convenções) | yes | none | - |

## Checks

Todas as provas rodaram em `D:\esclari` com postgres, minio e mailpit no ar. Os seis testes de API apareceram um a um como Aprovado (6 de 6). Na tela, 4 testes passaram e 7 foram pulados pelo filtro `-t`, que é o esperado.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | PUT em si mesmo com papéis diferentes: 409, detalhe exato, papéis no banco intactos | `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AdminTests.Admin_nao_muda_o_proprio_papel"` exit 0, 1 teste | `Esclari.Api.Tests/AdminTests.cs:378` `Assert.Equal(HttpStatusCode.Conflict, r.StatusCode)`; `:379` `Assert.Equal("Você não pode mudar o próprio papel. Peça a outro administrador.", await Detalhe(r))`; `:380` papéis no banco = `[Papeis.Administrador]` | PASS |
| C2 | PUT em si mesmo com os mesmos papéis em outra ordem: 200 e nome novo gravado | `...~AdminTests.Admin_edita_o_proprio_nome_com_os_mesmos_papeis` exit 0, 1 teste | `AdminTests.cs:393` `Assert.Equal(HttpStatusCode.OK, r.StatusCode)`; `:394` `Assert.Equal("Nome Novo", ...Nome)`. O corpo manda `[Atendente, Administrador]` contra `[Administrador, Atendente]` (`:391`) | PASS |
| C3 | Admin muda o papel de outro admin para ALUNO: 200 e o outro sem ADMINISTRADOR | `...~AdminTests.Admin_muda_o_papel_de_outro_administrador` exit 0, 1 teste | `AdminTests.cs:406` `Assert.Equal(HttpStatusCode.OK, ...)`; `:407` `Assert.Equal([Papeis.Aluno], ...)` | PASS |
| C4 | POST desativar em si mesmo: 409, detalhe exato, `Ativo` continua true | `...~AdminTests.Admin_nao_desativa_a_si_mesmo` exit 0, 1 teste | `AdminTests.cs:419` `Assert.Equal(HttpStatusCode.Conflict, ...)`; `:420` `Assert.Equal("Você não pode desativar a própria conta.", await Detalhe(r))`; `:421` `Assert.True(...Ativo)` | PASS |
| C5 | Admin desativa outro admin: 200, `Ativo` false | `...~AdminTests.Admin_desativa_outro_administrador` exit 0, 1 teste | `AdminTests.cs:433` `Assert.Equal(HttpStatusCode.OK, ...)`; `:434` `Assert.False(...Ativo)` | PASS |
| C6 | `UltimoAdministrador`: único admin ativo vira 409 com o detalhe exato; segundo admin ativo, alvo que continua admin e alvo que não é admin dão null; segundo admin inativo volta a dar 409 | `...~AdminTests.Ultimo_administrador_ativo_nao_se_perde` exit 0, 1 teste | `AdminTests.cs:447` `Assert.Equal(409, recusa.StatusCode)`; `:448` `Assert.Equal("A instituição precisa de ao menos um administrador ativo.", recusa.ProblemDetails.Detail)`; `:449` continua admin, `:450` aluno, `:453` segundo ativo, `:460` segundo inativo `IsType<ProblemHttpResult>`, `:461` alvo (segundo, inativo) null; o ramo "alvo inativo" é `ValidacaoDeUsuario.cs:16`. Implementação em `ValidacaoDeUsuario.cs:14` | PASS |
| C7 | Linha própria só com "Editar"; janela sem caixas de papel, com a frase; PUT com os papéis atuais | `npm --prefix web test -- src/administracao/cadastros/usuarios.test.tsx -t "o próprio administrador"` (rodado em lote com C8 e C9) exit 0 | `web/src/administracao/cadastros/usuarios.test.tsx:51` `expect(await acoesDe('Ana')).toEqual(['Editar'])`; `:55` frase; `:56-58` `queryByLabelText` de Administrador, Aluno e Atendente ausentes; `:61` PUT com `papeis: ['ADMINISTRADOR']` | PASS |
| C8 | Linha de outro mantém "Editar" e "Desativar"; janela dele mantém as caixas de papel | filtros `-t "ações de usuário no menu"` e `-t "editar troca papéis, setores e a senha temporária"` (em lote) exit 0, 2 testes | `usuarios.test.tsx:44` e `:52` `toEqual(['Editar', 'Desativar!'])`; `:94` `getByLabelText('Atendente')).toBeChecked()` e `:95` `getByLabelText('Administrador')).not.toBeChecked()` | PASS |
| C9 | 409 "A instituição precisa de ao menos um administrador ativo." aparece no alerta ao salvar e ao desativar | `-t "mostra o erro da API"` (em lote) exit 0, 1 teste | `usuarios.test.tsx:70` `findByRole('alert')).toHaveTextContent(detail)` dentro do `dialog`; `:77` `findByText(detail)` após Desativar | PASS |

## Coverage

Recalculado a partir das rotas (`AlterarUsuario.cs:12`, `DesativarUsuario.cs`), do código de `UltimoAdministrador` (`ValidacaoDeUsuario.cs:14`, 3 condições de saída e 1 consulta) e do `LinhaUsuario` (`Usuarios.tsx:64`, `:83`). Busquei outras rotas que alterem papel ou desativem usuário. `DefinirAcesso`, `Desativar` e `AdicionarPapel` aparecem em `CriarUsuario`, `ImportarMatriculas` (só adiciona ALUNO) e nas rotas de `Plataforma/` (`CriarMembro`, `DesativarMembro`, `ReativarMembro`), que são do operador e fora de escopo no checks. Não há outra rota de admin da instituição que mude papéis ou ativo.

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| `PUT /api/admin/usuarios/{id}` statuses tocados (3) | `AlterarUsuario.cs` | 200 próprio C2 · 200 de outro C3 · 409 C1 | - |
| `POST .../desativar` statuses tocados (2) | `DesativarUsuario.cs` | 200 C5 · 409 C4 | - |
| decisão do `UltimoAdministrador` (5) | `ValidacaoDeUsuario.cs:14` | único admin perde o papel C6 (`:447`) · segundo ativo C6 (`:453`) · segundo inativo C6 (`:460`) · alvo não é admin ativo C6 (`:450`, `:461`) · continua admin C6 (`:449`) | - |
| quem edita (2) | `AlterarUsuario.cs:35` | a si mesmo C1 C2 · a outro C3 | - |
| quem desativa (2) | `DesativarUsuario.cs:21` | a si mesmo C4 · a outro C5 | - |
| linha na tela (2) | `Usuarios.tsx:180` | a própria C7 · de outra C8 | - |
| origem do erro mostrado (1) | `ErrosDeAdmin.SemAdministrador` | 409 do último admin C9 | - |

Sobre C6 como prova direta: o argumento do checks se sustenta. A rota exige um administrador ativo como chamador (`CookieDeSessao`), e esse chamador conta como "outro administrador ativo" quando o alvo é diferente dele. O alvo igual ao chamador já é recusado antes pelas guardas de próprio papel e de própria conta. Por isso o 409 do último administrador só ocorre por corrida entre dois administradores, o que o checks declara fora de escopo. A prova direta no banco real é a forma possível do check como escrito.

Ressalva, abaixo em Findings: nenhuma prova faz falhar a remoção da chamada a `UltimoAdministrador` dentro dos handlers.

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| Decide, atravessa a rota HTTP | `AlterarUsuario.cs`, `DesativarUsuario.cs` | uma na rota: C1 C2 C3 C4 C5 | yes - a si mesmo recusado, a si mesmo com os mesmos papéis, a outro permitido, nos dois handlers |
| Decide, sem rota que o alcance | `ValidacaoDeUsuario.cs` | uma na própria camada: C6 | yes - os 5 casos da tabela |
| Decide, na tela | `Usuarios.tsx` (`LinhaUsuario`, `PapeisESetores`) | uma na própria camada: C7 C8 | yes - própria e de outro |

## Existing assertions changed

Comparado com `git diff e8982f6 1ce7bfa -- Esclari.Api.Tests web/src`. Nada foi enfraquecido além do que o `checks.md` lista.

- `usuarios.test.tsx` e `textos.test.tsx`: só `me({ id: 99, ... })` na constante `admin`. Nenhuma `expect` removida ou alterada nesses arquivos. O diff de `usuarios.test.tsx` só adiciona linhas.
- `ArquiteturaTests.cs`: uma linha a mais na lista `Excecoes_Mensagem_sem_entidade` (`Administracao.Usuarios.AlterarUsuario`). Só afrouxa para a nova mensagem que carrega o `Usuario`, igual a `DesativarUsuario`. Nenhuma regra mudou.
- Sem teste apagado ou pulado. `AdminTests.cs` só tem linhas adicionadas.

## Faults injected

Worktree descartada (`git worktree remove --force`); o status do repositório real continua só com `?? .tmp-capturas/`, igual ao início. Foram 3 falhas, o teto do pedido.

| Mutation | Location | Killed |
| --- | --- | --- |
| remover o `if (u.Id == m.Admin.Id && !...SetEquals(novos)) return ErrosDeAdmin.ProprioPapel();` | `Esclari.Api/Administracao/Usuarios/AlterarUsuario.cs:35` | yes - `UsuarioAdminTests.Admin_nao_muda_o_proprio_papel` falhou (1 de 14) |
| remover o `if (id == m.Admin.Id) return ErrosDeAdmin.PropriaConta();` | `Esclari.Api/Administracao/Usuarios/DesativarUsuario.cs:21` | yes - `UsuarioAdminTests.Admin_nao_desativa_a_si_mesmo` falhou (1 de 14; o 409 do último admin responde no lugar, e o detalhe esperado não confere) |
| mostrar "Desativar" na própria linha (`{!propria && (` vira `{(`) | `web/src/administracao/cadastros/Usuarios.tsx:64` | yes - `o próprio administrador não desativa a conta nem muda o próprio papel` falhou (1 de 11) |

## Findings

1. Baixa. A chamada a `ValidacaoDeUsuario.UltimoAdministrador` dentro de `AlterarUsuario.cs:36` e `DesativarUsuario.cs:22` não tem prova que falhe se for removida: a regra só dispara por corrida, que o checks deixa fora de escopo (C6 prova a função, não a ligação). Li as duas chamadas e elas estão ligadas com os argumentos certos (`novos.Contains(Papeis.Administrador)` e `false`). Não injetei essa falha por ter atingido o teto de 3; ela sobreviveria por construção. Risco aceito pelo próprio checks, sem AD no `STATE.md`.
2. Cosmética. `usuarios.test.tsx:78-79` fica sem linha em branco entre o fim do teste novo e `it('carregando...')`.

## Gate

- `dotnet build Esclari.slnx` - 0 erros, 11 avisos (já existentes)
- `dotnet test Esclari.Api.Tests` - 1315 passed, 0 failed
- `npx tsc -b --noEmit` (em `web/`) - exit 0
- `npx vitest run` (em `web/`) - 1153 passed, 0 failed
