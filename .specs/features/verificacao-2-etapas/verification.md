# Verificação em 2 etapas: campo do código e identidade do Esclari verification

**Verdict**: PASS
**Profile**: ui
**Diff range**: bdc8169..HEAD (HEAD = 794191d; commits 0618f42, 3cc4ff8 e o de correção 794191d)
**Round**: 2 - scoped
**Verifier**: independent sub-agent (author != verifier)

Escopo da rodada 2 (verify.md, "Re-verifying after a fix"): todas as provas re-rodadas inteiras em `794191d`; faltas re-injetadas nas superfícies que a correção tocou; citações atualizadas nos 2 arquivos de teste que a correção tocou (`PlataformaAcessoTests.cs`, `entrada.test.tsx`); o resto marcado `carried from 3cc4ff8` (rodada 1). A correção `794191d` só mexe nesses dois arquivos de teste (+2 linhas cada); nenhum código de produção mudou desde `3cc4ff8`.

## Achados da rodada 1 e situação

| Achado (rodada 1) | Situação |
| --- | --- |
| C4: mutante A (remover `if (!confirmar.isPending)`, `Entrada.tsx:101`) sobreviveu; linha "Decide, na tela" não atendida | Resolvido. O teste agora clica no campo e cola `654321` com o 1º POST pendente (`entrada.test.tsx:181-182`). Mutante A re-injetado: o teste falha (ver Faults injected) |
| C12: mutante F (`CODIGO` com `segredo`/`uri` preenchidos, `EntrarNaPlataforma.cs:43`) sobreviveu | Resolvido. `PlataformaAcessoTests.cs:96-97` afirma `JsonValueKind.Null` para os dois. Mutante F re-injetado: o teste falha |

## Binding sources

Carried from 3cc4ff8 (a correção não tocou interface nem fonte binding; nada a reabrir).

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `.specs/STATE.md` AD-018 (CA1502 no build; `web/` abaixo de CRAP 10; `dryer` sem candidato 0.8; `mutator` suspenso por AD-025) | yes - carried from 3cc4ff8 (`Select-String AD-018` em `.specs/STATE.md`) | none | - |
| FreeOTP `URI.md`, parâmetro `image` (opcional, URL-encoded) | yes - carried from 3cc4ff8 (web_fetch selective) | none: C10 e C11 coincidem; `Totp.cs:35` usa `Uri.EscapeDataString` | - |
| Design / mock de tela | n/a - o plano não marca design como binding; enumeração por tela feita na rodada 1 contra `Surface`/`Observable`, sem elemento fora do plano e sem item do plano sem check (carried from 3cc4ff8) | none | - |

## Checks

Provas rodadas em `794191d` (verified at 794191d), uma invocação por alvo:

- API: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests|FullyQualifiedName~TotpTests|FullyQualifiedName~PlataformaLimiteTests|FullyQualifiedName~ManifestoTests" --logger "console;verbosity=normal"`: Total 42, Aprovado 42, 0 falhas. Cada teste nomeado apareceu individualmente como `Aprovado` (incluindo `Entrar_com_segundo_fator_pede_o_codigo [69 ms]`, `Entrar_sem_segundo_fator_devolve_segredo_e_uri [81 ms]`, `Segredo_novo_tem_20_bytes_em_base32_e_a_uri_do_aplicativo`, `Uri_com_imagem_codifica_o_endereco`, `Corpo_invalido_400`, `Entrar_401_com_a_mesma_frase_nos_tres_casos`, `Limitador_auth_429_na_sexta_tentativa`, `Manifesto_padrao_sem_aparencia`).
- Web: `npx vitest run src/plataforma/entrada.test.tsx --reporter=verbose` em `D:\esclari\web`: 21 passed, 0 failed; os 17 testes nomeados nos checks apareceram com ✓ (inclui `nao envia de novo enquanto pendente: Enter e mais digitos nao geram segundo POST` 1166 ms e `codigo sem qr code nem chave`).
- Existência: `Select-String` nos arquivos (linhas abaixo); nenhum filtro vazio.
- Rodada completa do vitest (`npm run coverage`): 133 arquivos, 1215 testes passed.

Citações: `entrada.test.tsx` e `PlataformaAcessoTests.cs` foram tocados pela correção, então as linhas abaixo foram reobtidas em `794191d`. Os demais arquivos (`TotpTests.cs`, `ManifestoTests.cs`, `PlataformaLimiteTests.cs`, `Entrada.tsx`, `EntrarNaPlataforma.cs`) não mudaram: `carried from 3cc4ff8` onde indicado. Os números de linha que a rodada 1 deu para `entrada.test.tsx` depois da linha 182 estão +2 nesta tabela, e foram conferidos por leitura.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | campo grande: `input` único, inputmode, autocomplete, maxlength 6, mono, centralizado, 24px, tracking | vitest `campo do codigo grande` ✓ (verified at 794191d) | `web/src/plataforma/entrada.test.tsx:142` (teste); `:57-58` `toHaveAttribute('inputmode','numeric')` / `('autocomplete','one-time-code')` no teste de 2FA; classes e `maxlength` no corpo de `:142-151` (carried from 3cc4ff8: `toHaveClass('font-mono','text-center','text-2xl')`, `className` com `tracking-[0.5em]`, 1 input `name="codigo"`) | PASS (pixel por classe, conforme isenção enumerada em checks.md) |
| C2 | digitar `12a3456` e colar `12-34 56789` deixam `123456` | vitest `campo do codigo so aceita digitos` ✓ e `com 2FA já cadastrado...` ✓ | `entrada.test.tsx:60-61` `type(codigo,'12a3456')` e `expect(codigo).toHaveValue('123456')`; teste `:153` (carried from 3cc4ff8: `toHaveValue('123')` após `12a3`, `toHaveValue('123456')` após `paste('12-34 56789')`) | PASS |
| C3 | ao 6º dígito envia um único POST `{codigo:'123456'}` e entra, sem clique | vitest `envia sozinho ao completar 6 digitos` ✓ | teste em `entrada.test.tsx:163`; asserção `expect(codigosEnviados(chamadas)).toEqual([{ codigo: '123456' }])` e `findByRole('navigation',{name:'Navegação'})` (carried from 3cc4ff8 `:166-167`). Mutante B carried: matou 3 testes | PASS |
| C4 | com resposta pendente, `Enter`, mais um dígito e colar 6 dígitos não geram 2º POST | vitest `nao envia de novo enquanto pendente` ✓ (verified at 794191d) | `entrada.test.tsx:180` `type(codigo,'7{Enter}{Enter}')`; `:181-182` `userEvent.click(codigo)` e `userEvent.paste('654321')` com o POST pendente; `:183` `expect(codigosEnviados(chamadas)).toEqual([{ codigo: '123456' }])` e `:186` o mesmo após `liberar()`. Mutante A re-injetado: **morto** (1 failed, 20 passed) | PASS |
| C5 | erro 401: `role="alert"` com a mensagem, campo `''` e focado | vitest `codigo errado limpa o campo e devolve o foco` ✓ | `entrada.test.tsx:193` `expect(alerta).toHaveTextContent('Código inválido.')`, `:194` `expect(codigo).toHaveValue('')`, `:195` `expect(codigo).toHaveFocus()`. Mutante C carried: morto | PASS |
| C6 | `123`+`Enter` e clique em `Entrar` enviam `{codigo:'123'}` e mostram o erro da API | vitest `envia com menos de 6 digitos...` ✓ | `entrada.test.tsx:202` `findByText('Informe os 6 dígitos.')`; `:205` `expect(codigosEnviados(chamadas)).toEqual([{ codigo: '123' }, { codigo: '45' }])` | PASS |
| C7 | a 2ª etapa abre com o foco no campo | vitest `abre com o foco no codigo` ✓ | `entrada.test.tsx:210` `expect(codigo).toHaveFocus()`; código `Entrada.tsx:124` `autoFocus` (carried from 3cc4ff8) | PASS |
| C8 | `src` do QR = SVG nível H; símbolo `/marca/simbolo-pequeno.svg` em fundo branco no mesmo bloco | vitest `qr code com o simbolo no centro e correcao H` ✓ | `entrada.test.tsx:220` `expect(simbolo).toHaveClass('bg-white')`, `:221` `expect(qr.parentElement).toContainElement(simbolo)`, `:222` `expect(qr).toHaveAttribute('src', ... QRCode.toString(Uri,{type:'svg',errorCorrectionLevel:'H'}))`. Mutante D re-injetado: **morto** (2 testes falharam). Observação da rodada 1 mantida: o "contêiner branco" é o próprio `<img>` do símbolo (`bg-white p-1 size-10`); `p-1` e `size-10` não são afirmados, só `bg-white` e o pai | PASS (com observação de precisão, não reprovada) |
| C9 | símbolo no máximo 40px (20% de 200) por `width`/`height` renderizados | vitest `simbolo ocupa no maximo 20% do qr` ✓ | `entrada.test.tsx:232` `expect(lado).toBe(200)`, `:233` e `:234` `expect(Number(simbolo.getAttribute('width'))).toBeLessThanOrEqual(lado * 0.2)` | PASS |
| C10 | `uri` de `entrar` sem 2FA = `otpauth://totp/Esclari:{email}?secret={segredo}&issuer=Esclari&image=` + `EscapeDataString("{UrlBase}/icons/icon-192.png")` | dotnet `Entrar_sem_segundo_fator_devolve_segredo_e_uri` Aprovado | `Esclari.Api.Tests/PlataformaAcessoTests.cs:113` `Assert.Equal($"otpauth://totp/Esclari:{operador.Email}?secret={segredo}&issuer=Esclari&image=http%3A%2F%2Flocalhost%3A8080%2Ficons%2Ficon-192.png", j.GetProperty("uri").GetString())`. Código `EntrarNaPlataforma.cs:45`. Mutante E carried: morto | PASS |
| C11 | `Totp.Uri` sem endereço não leva `image`; com endereço acrescenta `&image=` codificado | dotnet `Segredo_novo_tem_20_bytes...` e `Uri_com_imagem_codifica_o_endereco` Aprovado | `TotpTests.cs:62` `Assert.Equal($"otpauth://totp/Esclari:ana@equipe.esclari?secret={segredo}&issuer=Esclari", Totp.Uri("ana@equipe.esclari", segredo))`; `:68` `...&image=https%3A%2F%2Fesclari.exemplo%2Ficons%2Ficon-192.png` (carried from 3cc4ff8; testes em `:55` e `:66`) | PASS |
| C12 | etapa `CODIGO`: API devolve `segredo: null` e `uri: null`; tela sem QR, chave nem link | dotnet `Entrar_com_segundo_fator_pede_o_codigo` Aprovado; vitest `codigo sem qr code nem chave` ✓ (verified at 794191d) | API: `PlataformaAcessoTests.cs:96` `Assert.Equal(JsonValueKind.Null, j.GetProperty("segredo").ValueKind)` e `:97` `Assert.Equal(JsonValueKind.Null, j.GetProperty("uri").ValueKind)`; `:95` nomes das chaves. Mutante F re-injetado: **morto** (`Expected: Null / Actual: String`). Tela: `entrada.test.tsx:122` `queryByRole('img',{name:'QR code para o aplicativo autenticador'})).not.toBeInTheDocument()`, `:123` `queryByLabelText('Chave do aplicativo')).not...`; link ausente em `:59` (teste de 2FA, mesma etapa `CODIGO`). Lacuna de precisão menor, não reprovada: a ausência do símbolo não tem asserção própria; o símbolo só é renderizado dentro de `QrCode`, que `:122` já prova ausente | PASS (com observação de precisão) |
| C13 | chave em grupos de 4; link só em toque; `alt` do QR | vitest `sem 2FA mostra a chave em grupos de 4...` ✓ e `link otpauth so em aparelho de toque` ✓ | `entrada.test.tsx` testes em `:70` e `:103`; asserções carried from 3cc4ff8: `findByText('JBSW Y3DP EHPK 3PXP')`, `getByRole('link',{name:'Abrir no aplicativo'})).toHaveAttribute('href', uri)`, link ausente sem toque e presente com toque, `findByRole('img',{name:'QR code para o aplicativo autenticador'})` | PASS |
| C14 | statuses de `entrar` inalterados: 400, 401 com a mesma frase, 429 na 6ª | dotnet `Corpo_invalido_400`, `Entrar_401_com_a_mesma_frase_nos_tres_casos`, `Limitador_auth_429_na_sexta_tentativa` Aprovado | `PlataformaAcessoTests.cs:213` `Assert.Equal(HttpStatusCode.BadRequest, (await c.EntrarComSenha("")).StatusCode)`; `:134` `Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode)` e `:135` `Assert.Equal("E-mail ou senha inválidos.", ...detail)`; `PlataformaLimiteTests.cs:15` `Assert.Equal(HttpStatusCode.TooManyRequests, ...)` (carried from 3cc4ff8) | PASS |
| C15 | `icon-192.png` existe e o manifesto padrão o lista | `Manifesto_padrao_sem_aparencia` Aprovado (verified at 794191d); `Test-Path` carried from 3cc4ff8 | `ManifestoTests.cs:53` `Assert.Equal(["/icons/icon-192.png","/icons/icon-512.png","/icons/icon-maskable-512.png"], ...)` (carried from 3cc4ff8; teste em `:40`) | PASS |
| C16 | build não quebra por CA1502; suítes verdes | `dotnet build Esclari.slnx` em 794191d: 0 Erro(s), 15 avisos (xUnit/CS8604 de testes já existentes). `npm run coverage` roda a suíte web inteira: 133 arquivos, 1215 passed. `dotnet test` completo e `npm --prefix web run build` não foram re-executados nesta rodada; a correção só mexeu em 2 arquivos de teste, e as 42 + 21 provas dos arquivos tocados passaram | build 0 erros; 42 API + 21 web + 1215 web totais passed | PASS no que rodei; `dotnet test` completo e `run build` não re-executados |
| C17 | `crapper` 0 no limite 9.99 e `dryer` sem candidato 0.8 | `npm run coverage` (1215 passed) e `crapper --use-existing-coverage -s src --threshold 9.99` exit 0; `dryer -s src/acesso ... -s src/plataforma --threshold 0.8` exit 0 (verified at 794191d) | saída do `crapper`: `Cadastro plataforma.Entrada 6 100.0% 6.0`, `Codigo plataforma.Entrada 4 100.0% 4.0`, `emGrupos 3 50.0% 4.1`, `QrCode 2 100.0% 2.0`; `dryer`: "No duplicate candidates found." | PASS |

## Coverage

Recomputado em `794191d` as linhas cuja autoridade a correção tocou; as demais carried from 3cc4ff8.

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| formas de enviar o código (4) - verified at 794191d | `Entrada.tsx`: `preencher` (6º dígito e colar), `onSubmit` -> `enviar`; trava `Entrada.tsx:101` `if (!confirmar.isPending) confirmar.mutate(digitos)` | automático ao 6º dígito C3 (`entrada.test.tsx:163`) · `Enter` com <6 C6 (`:201`) · clique em `Entrar` com <6 C6 (`:204`) · duplicado durante o envio C4 (`:180-183`: Enter, 7º dígito e colar 6 dígitos pendentes; mutante A morto) | - |
| entrada de caracteres no campo (3) - carried from 3cc4ff8 | `preencher`: `replace(/\D/g,'').slice(0,6)` | dígito, letra/símbolo, além do 6º: C2 | - |
| estados do campo (4) - verified at 794191d (pendente) | `autoFocus`, `isPending`, `onError`, `onSuccess` | abertura C7 · pendente C4 (agora discrimina a trava) · erro C5 (mutante C morto, carried) · sucesso C3 | - |
| etapas da resposta de `entrar` (2) - verified at 794191d | `EntrarNaPlataforma.cs:43` (`CODIGO`, `null, null`) e `:45` (`CADASTRAR`) | `CADASTRAR` C10 (`PlataformaAcessoTests.cs:113`) · `CODIGO` C12 (`:94` etapa, `:95` chaves, `:96-97` `segredo` e `uri` `Null`; mutante F morto) | - |
| `POST /api/plataforma/auth/entrar` statuses (4) - carried from 3cc4ff8 (linhas em 794191d) | `EntrarNaPlataforma.cs:21`, `:34-35`, `:40`, `:43/45` | 200 C10 · 400 C14 (`:213`) · 401 C14 (`:134`) · 429 C14 (`PlataformaLimiteTests.cs:15`) | - |
| partes do QR (3) - carried from 3cc4ff8 (linhas em 794191d) | `Entrada.tsx:58-64` | nível H C8 (`:222`, mutante D morto de novo) · símbolo no centro C8 (`:220-221`) · limite de 20% C9 (`:232-234`) | - |
| partes da URI (4) - carried from 3cc4ff8 | `Totp.cs:35`, `EntrarNaPlataforma.cs:45` | `secret`, `issuer`, `image` C10 · sem `image` C11 | - |
| itens preservados do cadastro (3) - carried from 3cc4ff8 | `Cadastro` em `Entrada.tsx` | chave em grupos, link só em toque, `alt` do QR: C13 | - |
| linhas de Observable com critério (3) - carried from 3cc4ff8 | plano, tabela Observable | celular 360×780 C1 · teclado aberto C7 · toque C1 | - |
| startup config (0 assemblies) - carried from 3cc4ff8 | n/a - nada precisa estar configurado antes da primeira requisição; `UrlBase` tem padrão, exercido em `PlataformaAcessoTests.cs:113` | n/a | - |

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| Decide, na tela (`Codigo`: filtro, envio automático, trava de pendente, limpar e focar no erro) - verified at 794191d | `web/src/plataforma/Entrada.tsx` | uma na própria camada; um caso por membro de `formas de enviar o código` e de `estados do campo` | yes. Um caso por membro nos dois conjuntos (C3, C6, C4, C7, C5); o caso "pendente" (`entrada.test.tsx:170-187`) agora falha sem a trava (mutante A morto) |
| Decide, atravessa a rota HTTP (`entrar` monta a URI com a imagem) - carried from 3cc4ff8 | `EntrarNaPlataforma.cs` | uma na rota; a URI inteira por igualdade | yes. `PlataformaAcessoTests.cs:113`; mutante E morto (carried) |
| `Totp.Uri`, um condicional (`image` presente ou não) - carried from 3cc4ff8 | `Totp.cs` | uma na própria camada; com e sem endereço | yes. `TotpTests.cs:62` e `:68` |
| `QrCode` (monta SVG e sobrepõe o símbolo) - verified at 794191d (mutante D) | `Entrada.tsx` (`QrCode`) | uma na própria camada; nível H, símbolo e limite de tamanho | yes. `entrada.test.tsx:222`, `:220-221`, `:232-234`; mutante D morto |

## Faults injected

Em `git worktree` descartável `D:\esclari-verify-scratch` (HEAD 794191d), com `web/node_modules` por junção. Cada mutação foi confirmada com `git diff --stat` (1 arquivo, 1 linha) antes de rodar. Mutantes revertidos com `git checkout --` entre um e outro.

| Mutation | Location | Killed |
| --- | --- | --- |
| remover a trava `if (!confirmar.isPending)` em `enviar` (`confirmar.mutate(digitos)` direto) - verified at 794191d | `web/src/plataforma/Entrada.tsx:101` | yes (`nao envia de novo enquanto pendente`: 1 failed, 20 passed) |
| `CODIGO` devolvendo `segredo` e `uri` `"X"` em vez de `null` - verified at 794191d | `Esclari.Api/Plataforma/EntrarNaPlataforma.cs:43` | yes (`Entrar_com_segundo_fator_pede_o_codigo`: `Expected: Null / Actual: String`) |
| nível de correção `'H'` -> `'M'` - verified at 794191d | `web/src/plataforma/Entrada.tsx:58` | yes (`qr code da uri e chave em grupos de 4` e `qr code com o simbolo no centro e correcao H` falharam) |
| remover o envio automático `if (digitos.length === 6) enviar(digitos)` - carried from 3cc4ff8 | `Entrada.tsx` (`preencher`) | yes (3 testes) |
| remover `setCodigo('')` e `campo.current?.focus()` do `onError` - carried from 3cc4ff8 | `Entrada.tsx` (`onError`) | yes (2 testes) |
| tirar o 3º argumento (endereço do ícone) de `Totp.Uri` - carried from 3cc4ff8 | `EntrarNaPlataforma.cs:45` | yes (`Entrar_sem_segundo_fator_devolve_segredo_e_uri`) |

Árvore real: `git status --porcelain` antes `?? .tmp-capturas/` e depois `?? .tmp-capturas/` (igual, também após `crapper`/`dryer`). Worktree `D:\esclari-verify-scratch` removido (`Test-Path` False). Sem mutante do tamanho do símbolo (C9) nem do filtro de dígitos (C2), como na rodada 1.

## Gate

`dotnet test Esclari.Api.Tests --filter "...PlataformaAcessoTests|TotpTests|PlataformaLimiteTests|ManifestoTests"` - 42 passed, 0 failed; `npx vitest run src/plataforma/entrada.test.tsx` - 21 passed, 0 failed; `npm run coverage` (suíte web) - 1215 passed, 0 failed; `dotnet build Esclari.slnx` - 0 erros; `crapper` exit 0; `dryer` exit 0. Não re-executados: `dotnet test` completo e `npm --prefix web run build`.
