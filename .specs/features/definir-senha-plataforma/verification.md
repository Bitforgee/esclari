# Tela de criar senha do operador verification

**Verdict**: PASS
**Profile**: standard
**Diff range**: 1e8a4c5~1..1e8a4c5 (verificado em HEAD cf1d25c)
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

## Binding sources

None - no plan.md, no binding source (profile `standard`, not `ui`).

## Checks

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | `/plataforma/definir-senha?token=abc123`: "Nova senha" e "Repita a senha" sem e-mail, "As senhas não são iguais.", envia `{ token: "abc123", senha }`, mensagem de sucesso e link `/plataforma` | `npm --prefix web test -- src/plataforma/entrada.test.tsx -t "definir senha da plataforma pelo link do convite"` exit 0, 1 passed 12 skipped | `web/src/plataforma/entrada.test.tsx:183` - `queryByLabelText('E-mail')` `.not.toBeInTheDocument()`; `:186` - `getByText('As senhas não são iguais.')`; `:192` - `findByText('Senha criada. Agora entre no console e cadastre o aplicativo autenticador.')`; `:193` - `getByRole('link', { name: 'Entrar no console' })` `.toHaveAttribute('href', '/plataforma')`; `:194` - `toEqual([['/api/plataforma/auth/definir-senha', { token: 'abc123', senha: 'uma senha longa' }]])` | PASS |
| C2 | `400` de token vencido mostra "O link é inválido ou venceu. Peça um convite novo." | `... -t "link vencido mostra a mensagem da API"` exit 0, 1 passed 12 skipped | `entrada.test.tsx:203` - `expect(await screen.findByText('O link é inválido ou venceu. Peça um convite novo.')).toBeInTheDocument()` | PASS |

Rota: `web/src/App.tsx:59` - `<Route path="plataforma/definir-senha" element={<DefinirSenhaDaPlataforma />} />`. O C1 abre a tela por essa rota via `abrir('/plataforma/definir-senha?token=abc123')` (`entrada.test.tsx:180`).

## Coverage

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| respostas da API em `definir-senha` usadas pela tela (2: sucesso, 400 token inválido) | `DefinirSenha.tsx` (`onSuccess`, `campos.token`) e checks | sucesso C1 · 400 C2 | - |

## Faults injected

| Mutation | Location | Killed |
| --- | --- | --- |
| corpo enviado com `novaSenha` em vez de `senha` | `web/src/plataforma/DefinirSenha.tsx` (`mutationFn`) | yes - falha `definir senha da plataforma pelo link do convite e ir para a entrada` (1 failed, 12 passed) |

Mutação extra fora da tabela, equivalente: trocar `if (!diferente) definir.mutate()` por `definir.mutate()` sobrevive (13 passed). O botão "Criar senha" fica `disabled` com senhas diferentes (`disabled={!senha || !!diferente}`) e o teste assere isso em `entrada.test.tsx:187`, então o formulário não é enviado por clique nem por Enter; o `if` é redundante com o `disabled`, por isso não conta como lacuna de prova.

Worktree descartado com `git worktree remove --force`; `git status --porcelain` da árvore principal igual ao de antes.

## Gate

`cd web; npx vitest run` - 1151 passed, 0 failed (131 arquivos); `npx tsc -b --noEmit` exit 0
