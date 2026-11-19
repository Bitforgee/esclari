# Verificação em 2 etapas: campo do código e identidade do Esclari verification

**Verdict**: FAIL
**Profile**: ui
**Diff range**: bdc8169..HEAD (HEAD = 3cc4ff8; commits 0618f42 and 3cc4ff8)
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

## Binding sources

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `.specs/STATE.md` AD-018 (CA1502 no build; `web/` abaixo de CRAP 10; `dryer` sem candidato 0.8; `mutator` suspenso por AD-025) | yes - `Select-String AD-018` em `.specs/STATE.md` | none (C16 e C17 repetem o gate; mutação fica suspensa e fora dos checks) | - |
| FreeOTP `URI.md` (https://github.com/freeotp/freeotp-android/blob/master/URI.md), parâmetro `image` | yes - web_fetch, modo selective: "Image - OPTIONAL: ... a URL-encoded URL to an image to display next to the token. If omitted, invalid or unfetchable, FreeOTP uses an internal algorithm" | none: C10 e C11 exigem o endereço URL-encoded e opcional, o que coincide com o documento. `Totp.cs:35` usa `System.Uri.EscapeDataString(imagem)` | - |
| Design / mock de tela | n/a - o plano não marca nenhum design como binding; só há AD-018 e o URI.md. A enumeração por tela (controles, ordem, arranjo) foi feita contra o `Surface`/`Observable` do plano: campo único, QR 200px com símbolo no centro, chave em grupos, link só em toque, botão `Entrar`. Nenhum elemento renderizado fora do plano. Nenhum item do plano sem check | none | - |

Observação sobre a isenção de medidas em pixels: o checks.md a enumera (C1 e C9 por classes/atributos no jsdom, "não pela medida do navegador"), limitada a C1/C9. Não é cláusula genérica.

## Checks

Provas rodadas em `HEAD` (3cc4ff8), uma invocação por alvo:

- API: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests|FullyQualifiedName~TotpTests|FullyQualifiedName~PlataformaLimiteTests|FullyQualifiedName~ManifestoTests" --logger "trx;..."`: 42 executados, 42 aprovados, 0 falhas (cada teste nomeado aparece como `Passed` no TRX).
- Web: `npx vitest run src/plataforma/entrada.test.tsx --reporter=verbose` em `D:\esclari\web`: 21 passed (cada teste nomeado aparece com ✓).
- Existência dos testes: `Select-String` nos arquivos (linhas abaixo). Todos os nomes existem; nenhum filtro vazio.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | campo grande: `input` único, inputmode, autocomplete, maxlength 6, mono, centralizado, 24px, tracking | vitest `campo do codigo grande` ✓ | `web/src/plataforma/entrada.test.tsx:145-150` - `toHaveAttribute('inputmode','numeric')`, `('autocomplete','one-time-code')`, `('maxlength','6')`, `for (const classe of ['font-mono','text-center','text-2xl']) expect(codigo).toHaveClass(classe)`, `expect(codigo.className).toMatch(/tracking-widest\|tracking-\[0\.\d+em\]/)`, `querySelectorAll('input[name="codigo"]')).toHaveLength(1)`. Código: `Entrada.tsx:~87` `ClasseDoCodigo` com `tracking-[0.5em]`, `text-2xl`, `h-14` | PASS (pixel por classe, conforme isenção enumerada) |
| C2 | digitar `12a3456` e colar `12-34 56789` deixam `123456` | vitest `campo do codigo so aceita digitos` ✓ e `com 2FA já cadastrado...` ✓ | `entrada.test.tsx:156` `expect(codigo).toHaveValue('123')` (após `12a3`); `:160` `expect(codigo).toHaveValue('123456')` (após `paste('12-34 56789')`); `:60-61` `type(codigo,'12a3456')` e `toHaveValue('123456')` | PASS |
| C3 | ao 6º dígito envia um único POST `{codigo:'123456'}` e entra, sem clique | vitest `envia sozinho ao completar 6 digitos` ✓ | `entrada.test.tsx:166` `findByRole('navigation',{name:'Navegação'})` e `:167` `expect(codigosEnviados(chamadas)).toEqual([{ codigo: '123456' }])`. Mutante B (remover `if (digitos.length === 6)`) matou 3 testes | PASS |
| C4 | com resposta pendente, `Enter` e mais um dígito não geram 2º POST | vitest `nao envia de novo enquanto pendente` ✓ | `entrada.test.tsx:181` e `:184` `expect(codigosEnviados(chamadas)).toEqual([{ codigo: '123456' }])`. **Mutante A (remover `if (!confirmar.isPending)`, `Entrada.tsx:101`) sobreviveu: 21/21 passam.** O teste só alcança o caminho que outra barreira já fecha (o botão desabilitado bloqueia o `Enter` e `maxLength` impede o 7º dígito); a trava de `enviar` nunca é exercitada | FAIL |
| C5 | erro 401: `role="alert"` com a mensagem, campo `''` e focado | vitest `codigo errado limpa o campo e devolve o foco` ✓ | `entrada.test.tsx:191` `expect(alerta).toHaveTextContent('Código inválido.')`, `:192` `expect(codigo).toHaveValue('')`, `:193` `expect(codigo).toHaveFocus()`. Mutante C (tirar `setCodigo('')` e `focus()` do `onError`) matou este teste e o de C6 | PASS |
| C6 | `123`+`Enter` e clique em `Entrar` enviam `{codigo:'123'}` e mostram o erro da API | vitest `envia com menos de 6 digitos...` ✓ | `entrada.test.tsx:200` `findByText('Informe os 6 dígitos.')`; `:203` `expect(codigosEnviados(chamadas)).toEqual([{ codigo: '123' }, { codigo: '45' }])` (o 2º envio vem do clique em `Entrar`, após o campo limpar) | PASS |
| C7 | a 2ª etapa abre com o foco no campo | vitest `abre com o foco no codigo` ✓ | `entrada.test.tsx:208` `expect(codigo).toHaveFocus()`; código `Entrada.tsx:124` `autoFocus` | PASS |
| C8 | `src` do QR = SVG nível H; símbolo `/marca/simbolo-pequeno.svg` em fundo branco no mesmo bloco | vitest `qr code com o simbolo no centro e correcao H` ✓ | `entrada.test.tsx:218` `expect(simbolo).toHaveClass('bg-white')`, `:219` `expect(qr.parentElement).toContainElement(simbolo)`, `:220` `expect(qr).toHaveAttribute('src', ... QRCode.toString(Uri,{type:'svg',errorCorrectionLevel:'H'}))`. Mutante D (H→M) matou este teste e `qr code da uri e chave em grupos de 4` (`:89-90`). Observação abaixo | PASS (com observação) |
| C9 | símbolo no máximo 40px (20% de 200) por `width`/`height` renderizados | vitest `simbolo ocupa no maximo 20% do qr` ✓ | `entrada.test.tsx:230` `expect(lado).toBe(200)`, `:231-232` `expect(Number(simbolo.getAttribute('width'))).toBeLessThanOrEqual(lado * 0.2)` e idem `height`. Sem mutante de tamanho rodado (limite de 5 faltas) | PASS |
| C10 | `uri` de `entrar` sem 2FA = `otpauth://totp/Esclari:{email}?secret={segredo}&issuer=Esclari&image=` + `EscapeDataString("{UrlBase}/icons/icon-192.png")` | dotnet `Entrar_sem_segundo_fator_devolve_segredo_e_uri` Passed | `PlataformaAcessoTests.cs:111` `Assert.Equal($"otpauth://totp/Esclari:{operador.Email}?secret={segredo}&issuer=Esclari&image=http%3A%2F%2Flocalhost%3A8080%2Ficons%2Ficon-192.png", j.GetProperty("uri").GetString())`. Código `EntrarNaPlataforma.cs:45`. Mutante E (tirar o 3º argumento) matou o teste | PASS |
| C11 | `Totp.Uri` sem endereço não leva `image`; com endereço acrescenta `&image=` codificado | dotnet `Segredo_novo_tem_20_bytes...` e `Uri_com_imagem_codifica_o_endereco` Passed | `TotpTests.cs:62` `Assert.Equal($"otpauth://totp/Esclari:ana@equipe.esclari?secret={segredo}&issuer=Esclari", Totp.Uri("ana@equipe.esclari", segredo))`; `:68` `Assert.Equal("otpauth://totp/Esclari:ana@equipe.esclari?secret=ABC&issuer=Esclari&image=https%3A%2F%2Fesclari.exemplo%2Ficons%2Ficon-192.png", Totp.Uri(..., "ABC", "https://esclari.exemplo/icons/icon-192.png"))` | PASS |
| C12 | etapa `CODIGO`: API devolve `segredo: null` e `uri: null`; tela sem QR, símbolo, chave nem link | dotnet `Entrar_com_segundo_fator_pede_o_codigo` Passed; vitest `codigo sem qr code nem chave` ✓ | Web: `entrada.test.tsx:122-123` `queryByRole('img',{name:'QR code para o aplicativo autenticador'})).not.toBeInTheDocument()` e `queryByLabelText('Chave do aplicativo')).not...`. API: `PlataformaAcessoTests.cs:94-95` só afirma `etapa == "CODIGO"` e `Assert.Equal(["etapa","segredo","uri"], j.EnumerateObject().Select(p => p.Name))`, isto é, os nomes das chaves e não os valores. **Mutante F (devolver segredo e uri preenchidos em `CODIGO`, `EntrarNaPlataforma.cs:43`) sobreviveu: o teste passou.** A parte "null" da claim não tem asserção. A tela também não afirma ausência do link `Abrir no aplicativo` nem do símbolo neste teste (o link é afirmado em `:59` do teste de 2FA) | FAIL |
| C13 | chave em grupos de 4; link só em toque; `alt` do QR | vitest `sem 2FA mostra a chave em grupos de 4...` ✓ e `link otpauth so em aparelho de toque` ✓ | `entrada.test.tsx:78` `findByText('JBSW Y3DP EHPK 3PXP')`, `:79` `getByRole('link',{name:'Abrir no aplicativo'})).toHaveAttribute('href', uri)`, `:108` `queryByRole('link',...)).not.toBeInTheDocument()` (sem toque), `:115` link presente com toque, `:88` `findByRole('img',{name:'QR code para o aplicativo autenticador'})` | PASS |
| C14 | statuses de `entrar` inalterados: 400, 401 com a mesma frase, 429 na 6ª | dotnet `Corpo_invalido_400`, `Entrar_401_com_a_mesma_frase_nos_tres_casos`, `Limitador_auth_429_na_sexta_tentativa` Passed | `PlataformaAcessoTests.cs:211` `Assert.Equal(HttpStatusCode.BadRequest, (await c.EntrarComSenha("")).StatusCode)`; `:132-133` `Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode)` e `Assert.Equal("E-mail ou senha inválidos.", ...detail)`; `PlataformaLimiteTests.cs:15` `Assert.Equal(HttpStatusCode.TooManyRequests, (await c.EntrarComSenha("ninguem@equipe.esclari")).StatusCode)` | PASS |
| C15 | `icon-192.png` existe e o manifesto padrão o lista | `Test-Path web/public/icons/icon-192.png` = True; dotnet `Manifesto_padrao_sem_aparencia` Passed | `ManifestoTests.cs:53` `Assert.Equal(["/icons/icon-192.png","/icons/icon-512.png","/icons/icon-maskable-512.png"], icones.Select(x => x.GetProperty("src").GetString()))` | PASS |
| C16 | build não quebra por CA1502; suítes verdes | `dotnet build Esclari.slnx`: 0 avisos, 0 erros. Rodei só as 6 classes/arquivos tocados (42 API + 21 web, acima). `dotnet test` completo, `npm --prefix web test` e `npm --prefix web run build` não foram rodados por mim (o autor os rodou) | build: "Compilação com êxito. 0 Aviso(s) 0 Erro(s)"; testes tocados: 63 passed, 0 failed | PASS no que rodei; suíte completa não re-executada |
| C17 | `crapper` 0 no limite 9.99 e `dryer` sem candidato 0.8 | `crapper --use-existing-coverage -s src --threshold 9.99` exit 0 (cobertura gerada 11:23, depois do último commit 11:16); `dryer -s src/acesso ... -s src/plataforma --threshold 0.8` exit 0 | saída do `crapper`: `Codigo plataforma.Entrada 4 75.0% 4.2`, `emGrupos 3 50.0% 4.1`, `QrCode 2 100.0% 2.0`; saída do `dryer`: "No duplicate candidates found." | PASS |

**Observação (C8, contêiner branco).** O código não tem um contêiner branco separado. O `<img>` do símbolo tem `bg-white p-1` e `size-10` (40px, com 4px de margem), dentro de um `div.relative` que também guarda o `<img>` do QR (`Entrada.tsx:60-64`). Isso cumpre a intenção do AC 8 ("quadrado branco com margem") e do AC 9 (símbolo + quadrado branco ≤ 20%): o quadrado branco e o símbolo são a mesma caixa de 40px. O teste afirma `bg-white` no próprio símbolo e que ele está no mesmo pai do QR (`:218-219`). Não afirma `p-1` (a margem) nem que a classe `size-10` combina com o atributo `width=40`; C9 mede só o atributo. É uma lacuna de precisão menor, que não reprovo isoladamente.

## Coverage

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| formas de enviar o código (4) | `Entrada.tsx`: `preencher` (6º dígito e colar), `onSubmit` -> `enviar`; trava em `:101` | automático ao 6º dígito C3 · `Enter` com <6 C6 · clique em `Entrar` com <6 C6 · duplicado durante o envio: C4 não consegue falhar sem a trava (mutante A sobreviveu) | duplicado durante o envio (a trava de `isPending` em `Entrada.tsx:101` não tem prova que a distinga; o caminho de colar 6 dígitos com envio pendente também não é exercitado) |
| entrada de caracteres no campo (3) | `preencher`: `replace(/\D/g,'').slice(0,6)` | dígito, letra/símbolo, além do 6º: C2 (`:156`, `:160`) | - |
| estados do campo (4) | `autoFocus`, `isPending`, `onError`, `onSuccess` | abertura C7 · erro C5 (mutante C morto) · sucesso C3 · pendente C4 (não discrimina, ver acima) | pendente |
| etapas da resposta de `entrar` (2) | `EntrarNaPlataforma.cs:43` e `:45` | `CADASTRAR` C10 · `CODIGO`: etapa e nomes de chave C12, mas `segredo: null` e `uri: null` sem asserção (mutante F sobreviveu) | `CODIGO` com `segredo` e `uri` nulos |
| `POST /api/plataforma/auth/entrar` statuses (4) | `EntrarNaPlataforma.cs:21` (`RequireRateLimiting`), `:34-35` (400), `:40` (401), `:43/45` (200) | 200 C10 · 400 C14 (`:211`) · 401 C14 (`:132`) · 429 C14 (`PlataformaLimiteTests.cs:15`) | - |
| partes do QR (3) | `Entrada.tsx:58-64` | nível H C8 (mutante D morto) · símbolo no centro C8 (`:218-219`, só classe `bg-white` e pai) · limite de 20% C9 | - |
| partes da URI (4) | `Totp.cs:35`, `EntrarNaPlataforma.cs:45` | `secret`, `issuer`, `image` C10 (mutante E morto) · sem `image` C11 (`TotpTests.cs:62`) | - |
| itens preservados do cadastro (3) | `Cadastro` em `Entrada.tsx` | chave em grupos C13 (`:78`) · link só em toque C13 (`:108`, `:115`) · `alt` do QR C13 (`:88`) | - |
| linhas de Observable com critério (3) | plano, tabela Observable | celular 360×780 C1 · teclado aberto C7 · toque C1 (`h-14`, 56px ≥ 44px) | - |
| startup config (0 assemblies) | n/a - nada precisa estar configurado antes da primeira requisição. `UrlBase` tem padrão, exercido em `PlataformaAcessoTests.cs:111` (`localhost:8080`) | n/a | - |

Conjuntos sem linha na tabela do autor: nenhum encontrado além dos acima. O plano cita `entrar` com `200, 400, 401, 429` e todos têm linha.

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| Decide, na tela (`Codigo`: filtro, envio automático, trava de pendente, limpar e focar no erro) | `web/src/plataforma/Entrada.tsx` | uma na própria camada; um caso por membro de `formas de enviar o código` e de `estados do campo` | no. Há um caso por membro, mas o caso "pendente" (C4) não prova a trava: o mutante A sobreviveu |
| Decide, atravessa a rota HTTP (`entrar` monta a URI com a imagem) | `EntrarNaPlataforma.cs` | uma na rota; a URI inteira por igualdade | yes. `PlataformaAcessoTests.cs:111`, mutante E morto |
| `Totp.Uri`, um condicional (`image` presente ou não) | `Totp.cs` | uma na própria camada; com e sem endereço | yes. `TotpTests.cs:62` e `:68` |
| `QrCode` (monta SVG e sobrepõe o símbolo) | `Entrada.tsx` (`QrCode`) | uma na própria camada; nível H, símbolo e limite de tamanho | yes. `:220`, `:218-219`, `:230-232`; mutante D morto |

## Faults injected

Rodados em `git worktree` descartável `D:\esclari-verify-scratch` (HEAD), com `node_modules` por junção; removido com `git worktree remove --force`. Foram 6 mutantes, 1 acima do teto de 5 (o B, do envio automático, foi o extra). A primeira rodada de A e C não aplicou a mutação por erro de script; foi descartada e refeita, com `git diff --stat` confirmando a aplicação em cada uma.

| Mutation | Location | Killed |
| --- | --- | --- |
| remover a trava `if (!confirmar.isPending)` em `enviar` (`confirmar.mutate(digitos)` direto) | `web/src/plataforma/Entrada.tsx:101` | no (21/21 passam; mutante sobrevivente, C4) |
| remover o envio automático `if (digitos.length === 6) enviar(digitos)` | `web/src/plataforma/Entrada.tsx` (`preencher`) | yes (3 testes falham: `com 2FA já cadastrado...`, `envia sozinho...`, `codigo errado...`) |
| remover `setCodigo('')` e `campo.current?.focus()` do `onError` | `web/src/plataforma/Entrada.tsx` (`onError`) | yes (`codigo errado limpa o campo e devolve o foco` e `envia com menos de 6 digitos...`) |
| nível de correção `'H'` -> `'M'` | `web/src/plataforma/Entrada.tsx:58` | yes (`qr code da uri e chave em grupos de 4` e `qr code com o simbolo no centro e correcao H`) |
| tirar o 3º argumento (endereço do ícone) de `Totp.Uri` | `Esclari.Api/Plataforma/EntrarNaPlataforma.cs:45` | yes (`Entrar_sem_segundo_fator_devolve_segredo_e_uri` falhou) |
| `CODIGO` devolvendo `segredo` e `uri` preenchidos em vez de `null` | `Esclari.Api/Plataforma/EntrarNaPlataforma.cs:43` | no (`Entrar_com_segundo_fator_pede_o_codigo` passou; mutante sobrevivente, C12) |

Árvore real: `git status --porcelain` antes `?? .tmp-capturas/` e depois `?? .tmp-capturas/` (igual). Worktree removido. O `crapper` e o `dryer` regravaram `web/.metrics/*.edn`, que o git ignora (porcelain não mudou). Não tentei mutante do tamanho do símbolo (C9) nem do filtro de dígitos (C2).

## Gate

`dotnet test Esclari.Api.Tests --filter "...PlataformaAcessoTests|TotpTests|PlataformaLimiteTests|ManifestoTests"` - 42 passed, 0 failed; `npx vitest run src/plataforma/entrada.test.tsx` - 21 passed, 0 failed; `dotnet build Esclari.slnx` - 0 erros, 0 avisos; `crapper` exit 0 e `dryer` exit 0. As suítes completas (`dotnet test`, `npm --prefix web test`, `npm --prefix web run build`) não foram re-executadas. Os gates reprovam por 2 mutantes sobreviventes (C4 e C12), não por teste vermelho.
