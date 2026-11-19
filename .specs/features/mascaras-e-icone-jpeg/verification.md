# Máscaras nos campos e ícone do app em JPEG verification

**Verdict**: PASS
**Profile**: standard
**Diff range**: 17852da..6bae358
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

Os 21 checks passam com evidência localizada, as 11 falhas injetadas foram todas mortas por pelo menos uma prova nomeada, e o `Coverage` recomputado a partir do plano não deixa membro sem prova. Há uma observação de precisão (telefone, ver `## Observations`) que não derruba o veredito.

## Checks

Todas as provas rodaram em `HEAD` (6bae358), nesta execução. Vitest: 1 invocação para os 8 arquivos da feature (8 arquivos, 48 testes, exit 0, cada nome aparece individualmente no modo verbose). API: 1 invocação com o filtro de C14 a C18 (21 testes, exit 0, cada nome aparece).

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | `cpfMascara` agrupa aos poucos | `npx vitest run src/mascaras.test.ts -t "cpf agrupa aos poucos"` exit 0, 1 teste rodou | `web/src/mascaras.test.ts:6-16` - `expect(['', '0', '046', '0464', ..., '04647333167'].map(cpfMascara)).toEqual(['', '0', '046', '046.4', '046.473', '046.473.3', '046.473.331', '046.473.331-6', '046.473.331-67'])` | PASS |
| C2 | ignora não dígitos | `... -t "cpf ignora o que não é dígito"` exit 0, 1 teste | `web/src/mascaras.test.ts:20` - `expect(cpfMascara('0a4b6')).toBe('046')`; `:21` - `expect(cpfMascara('abc')).toBe('')` | PASS |
| C3 | colado com e sem pontos | `... -t "cpf colado com ou sem pontos"` exit 0, 1 teste | `web/src/mascaras.test.ts:25` - `expect(cpfMascara('046.473.331-67')).toBe('046.473.331-67')`; `:26` - `expect(cpfMascara('04647333167')).toBe('046.473.331-67')` | PASS |
| C4 | corta em 11 | `... -t "cpf corta em 11"` exit 0, 1 teste | `web/src/mascaras.test.ts:31` - `expect(cpfMascara('046473331679999')).toBe('046.473.331-67')` | PASS |
| C5 | 5 formulários mostram a máscara e enviam 11 dígitos | 3 provas de tela, exit 0, 1 teste cada (`cpf do login e do primeiro acesso`, `cpf do novo usuário`, `cpf da equipe e da nova instituição`) | Login: `web/src/acesso/mascaras.test.tsx:18` `toHaveValue('046.473.331-67')` e `:22` `toEqual([{ instituicao: 'univag', cpf: '04647333167', senha: 'senha-boa-123' }])`. Primeiro acesso: `:31` e `:33` `[{ instituicao: 'univag', cpf: '04647333167' }]`. Novo usuário: `web/src/administracao/cadastros/mascaras.test.tsx:29` e `:35` `{ cpf: '04647333167', ... }`. Equipe: `web/src/plataforma/mascaras.test.tsx:39` e `:44` `{ nome: 'Gil', cpf: '04647333167', ... }`. Criar instituição: `:60` e `:64` `expect(gravacoes(criacao)[0][1].admin.cpf).toBe('04647333167')` | PASS |
| C6 | editar usuário com `11144477735` mostra `111.444.777-35` e envia 11 dígitos | `... cadastros/mascaras.test.tsx -t "cpf ao editar usuário"` exit 0, 1 teste | `web/src/administracao/cadastros/mascaras.test.tsx:43` - `expect(d.getByLabelText('CPF')).toHaveValue('111.444.777-35')`; `:46` - `['PUT', '/api/admin/usuarios/5', { cpf: '11144477735', ... }]` | PASS |
| C7 | 5 campos CPF com `inputMode` numeric e `maxLength` 14 | as mesmas 3 provas de C5 | Login `acesso/mascaras.test.tsx:15-16`; Primeiro acesso `:28-29`; Novo usuário `cadastros/mascaras.test.tsx:26-27`; Equipe `plataforma/mascaras.test.tsx:35-36`; Criar instituição `:55-56` - `toHaveAttribute('inputmode', 'numeric')` e `toHaveAttribute('maxlength', '14')` | PASS |
| C8 | `telefoneMascara` 10 e 11 dígitos e parciais | `... mascaras.test.ts -t "telefone com 10 e 11 dígitos"` exit 0, 1 teste | `web/src/mascaras.test.ts:35` `'(65) 3321-1234'`; `:36` `'(65) 99999-1234'`; `:37` `'(6'`; `:39` `'(65) 9'`; `:41` `''` | PASS |
| C9 | tira o `55` | `... -t "telefone tira o 55"` exit 0, 1 teste | `web/src/mascaras.test.ts:45` - `expect(telefoneMascara('+55 65 99999-1234')).toBe('(65) 99999-1234')`; `:46` - `'556533211234'` -> `'(65) 3321-1234'` | PASS |
| C10 | corta em 11 | `... -t "telefone corta em 11"` exit 0, 1 teste | `web/src/mascaras.test.ts:50` - `expect(telefoneMascara('659999912349999')).toBe('(65) 99999-1234')` | PASS |
| C11 | Aparência mostra e salva `(65) 99999-1234` | `... aparencia/mascaras.test.tsx -t "telefone do contato"` exit 0, 1 teste | `web/src/administracao/aparencia/mascaras.test.tsx:25` - `toHaveValue('(65) 99999-1234')`; `:28` - `expect(enviado.contato.telefone).toBe('(65) 99999-1234')` | PASS |
| C12 | `protocoloMascara` e campo do duplicado | `... mascaras.test.ts -t "protocolo"` e `... chamados/mascaras.test.tsx -t "protocolo do duplicado"`, exit 0, 1 teste cada | `web/src/mascaras.test.ts:54-57` - `'esc123456'` e `'1234567'` -> `'ESC-123456'`, `'ESC-'` e `'abc'` -> `''`; tela `web/src/chamados/mascaras.test.tsx:15` - `expect(original).toHaveValue('ESC-12')` (digitado `esc12`) | PASS |
| C13 | `enderecoMascara` e campo Endereço | `... mascaras.test.ts -t "endereço"` e `... plataforma/mascaras.test.tsx -t "endereço da nova instituição"`, exit 0, 1 teste cada | `web/src/mascaras.test.ts:62-64` - `'Faculdade São João'` -> `'faculdade-sao-joao'`, `'A_b!c'` -> `'abc'`, 40 `a` -> 30; tela `web/src/plataforma/mascaras.test.tsx:74` - `expect(endereco).toHaveValue('univag-sul')` (digitado `Univag Sul`) | PASS |
| C14 | JPEG 512 e 1024 respondem 200, `versao` 1, `image/jpeg`, objeto no MinIO | `dotnet test --filter ...Icone_jpeg_quadrado_200` Aprovado | `Esclari.Api.Tests/AparenciaAdminTests.cs:312` `Assert.Equal(HttpStatusCode.OK, r.StatusCode)`; `:314` `Assert.Equal(("icone", 1), (tipo, versao))`; `:316` `("icone", "image/jpeg", (long)jpeg.Length)`; `:317` `Assert.Equal(jpeg, await f.Objeto(linha.Chave))`; `:320` JPEG 1024 -> `OK` | PASS |
| C15 | ícone GIF 415 com a frase; Logo e Login recusam GIF e PDF | `...Tipo_de_imagem_recusado_415` Aprovado | `AparenciaAdminTests.cs:227` `Assert.Equal(HttpStatusCode.UnsupportedMediaType, icone.StatusCode)`; `:228` `Assert.Equal("O ícone deve ser PNG ou JPEG.", ...detail)`; `:224-225` logo e login 415; `:230-231` nada gravado | PASS |
| C16 | PNG e JPEG não quadrado ou pequeno, e JPEG sem SOF, 400 sem gravar | `...Icone_nao_quadrado_ou_pequeno_400` (4 casos) e `...Icone_jpeg_nao_quadrado_ou_pequeno_400` (4 casos) Aprovado | PNG `:258-259` (512x511, 511x511); JPEG `:281-282` e `:284` (`comSof: false`); `:274` `Assert.Equal(HttpStatusCode.BadRequest, ...)`; `:275` e `:293` `Assert.Equal(["O ícone deve ser um PNG ou JPEG quadrado de pelo menos 512 px."], Erros(..., "arquivo"))`; `:276-277` e `:294-295` sem linha e sem objeto | PASS |
| C17 | PNG e JPEG com 1 MB + 1 byte respondem 413 | `...Imagem_acima_do_limite_413` e `...Icone_jpeg_quadrado_200` Aprovado | PNG `AparenciaAdminTests.cs:244-248` (`("icone", Mb)` com `Png(512, 512, limite + 1)`) `Assert.Equal(HttpStatusCode.RequestEntityTooLarge, ...)` e `Assert.Contains("1 MB", detail)`; JPEG `:304-306` `JpegDe(512, 512, Mb + 1)` -> `RequestEntityTooLarge`, `Contains("1 MB", ...)`, `:307` `Assert.Empty(await Linhas(i.Id))` | PASS |
| C18 | manifesto e `iconeTipo` por tipo gravado, `null` sem ícone, 404 nas duas rotas | 4 filtros `ManifestoTests.*` Aprovado (10 de 10 na classe) | JPEG: `Esclari.Api.Tests/ManifestoTests.cs:55-57` `Assert.Equal(2, icones.Count)`, `Assert.All(icones, x => Assert.Equal("image/jpeg", x.GetProperty("type")...))`, `Assert.Equal("image/jpeg", publica.GetProperty("iconeTipo")...)`. PNG: `:35` `Assert.Equal("image/png", x.GetProperty("type")...)` e `:38` `iconeTipo` `image/png`. Sem ícone: `:179` `Assert.Equal(JsonValueKind.Null, publica.GetProperty("iconeTipo").ValueKind)`. 404: `:88` manifesto e `:93` `Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/instituicoes/{slug}")).StatusCode)` | PASS |
| C19 | favicon com `type` do ícone, volta a PNG sem ícone, dica nova | `... iconeJpeg.test.tsx -t "favicon com o tipo do ícone"` (1 teste) e `... guidelinesFormatos.test.tsx` (31 testes), exit 0 | `web/src/iconeJpeg.test.tsx:18-19` - `toHaveAttribute('href', '/api/instituicoes/univag/imagens/icone?v=4')` e `toHaveAttribute('type', 'image/jpeg')`; `:24-25` - `'/icons/icon-192.png'` e `'image/png'`; dica `web/src/guidelinesFormatos.test.tsx:45` - ``expect(Rotulos.icone[1]).toBe(`PNG ou JPEG quadrado de pelo menos 512${NB}px, até 1${NB}MB.`)`` | PASS |
| C20 | suítes e build verdes; retrato só ganha `iconeTipo` | `dotnet test Esclari.Api.Tests`; `cd web; npx vitest run`; `npm --prefix web run build`; `dotnet build Esclari.slnx` | API: 1392 aprovados, 0 com falha, exit 0. Vitest: 140 arquivos, 1232 testes, exit 0. Build web exit 0. `dotnet build`: 0 avisos, 0 erros, exit 0. Retrato: `Esclari.Api.Tests/AparenciaPublicaTests.cs:30` lista `[..., "imagens", "iconeTipo"]`; `git diff` dos retratos mostra 1 linha em `instituicao-publica.txt` e 1 em `sucessos.txt`; `Esclari.Api/Acesso/VerInstituicao.cs` ganha só `IconeTipo` | PASS |
| C21 | `crapper` 9.99 e `dryer` 0.8 | `npm --prefix web run coverage` (1232 de 1232, exit 0); `crapper --use-existing-coverage -s src --threshold 9.99` exit 0; `dryer -s src/acesso -s ... -s src/plataforma --threshold 0.8` exit 0, "No duplicate candidates found." | saída dos dois comandos acima; os arquivos `web/.metrics/*.edn` são ignorados pelo git e `git status` ficou limpo | PASS |

## Binding sources

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| `Esclari.Api/Instituicoes/Aparencia/EnviarImagemInstituicao.cs` | yes - arquivo lido na íntegra | none - aceita `[Png, Jpeg]` para todo tipo (`:51`), recusa com `O ícone deve ser PNG ou JPEG.` (415), mede PNG no cabeçalho (`DoPng`) e JPEG no SOF (`DoJpeg`, `InicioDeQuadro` exclui `C4`, `C8`, `CC`), recusa com a frase de AC 14 (400), limite de 1 MB (413); batem com C14 a C17 | - |
| `web/src/administracao/aparencia/abasDaAparencia.ts` | yes - diff e linha 11 lidos | none - `Rotulos.icone[1]` é `PNG ou JPEG quadrado de pelo menos 512 px, até 1 MB.`, igual a C19. A dica longa (terceiro item) segue `PNG quadrado, sem transparência...`, fora do AC 18 | - |
| `Esclari.Api/Plataforma/Criacao.cs:16` | yes - citado no plano e conferido: o formato do slug é `^[a-z0-9-]{3,30}$` | none - `enderecoMascara` só produz `[a-z0-9-]` com até 30 caracteres, o que a API aceita | - |
| `Esclari.Api/Dados/Usuarios/Cpf.cs` | yes - `Cpf.Tentar` aceita dígitos, com ou sem pontos e traço | none - a API recebe os 11 dígitos de AC 5 | - |

Enumeração por tela (celular primeiro): Login, Primeiro acesso, Usuários, Equipe e Criar instituição trocam só o texto do campo, e `PropsDoCpf` em `web/src/mascaras.ts` fixa `inputMode`, `maxLength` e placeholder (C7). Nenhuma região, controle ou ordem muda. As linhas "celular 360×780" e "teclado aberto" do `Observable` citam AC 6, que C7 afirma. Ver `## Observations` sobre o `inputMode="tel"` do telefone.

## Coverage

Recomputado a partir da `## Criteria` (AC 1 a AC 18), do `## Surface`, do `## Landing` e do código (`web/src/mascaras.ts`, `EnviarImagemInstituicao.cs`), e não da tabela de `checks.md`.

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| critérios de aceite (18) | `plan.md ## Criteria` | AC1 C1 · AC2 C2 · AC3 C3, C6 · AC4 C4 · AC5 C5, C6 · AC6 C7 · AC7 C8, C11 · AC8 C9 · AC9 C10 · AC10 C12 · AC11 C13 · AC12 C14 · AC13 C15 · AC14 C16 · AC15 C17 · AC16 C18 · AC17 C18, C19 · AC18 C19 | - |
| formulários com CPF (5) | `grep` de `PropsDoCpf` em `Acesso.tsx:246,272`, `Usuarios.tsx:122`, `Equipe.tsx:38`, `Instituicoes.tsx:87` | Login, Primeiro acesso, Novo usuário, Equipe e Criar instituição, todos em C5 e C7; edição de usuário em C6 | - |
| ramos de `cpfMascara` (5) | `web/src/mascaras.ts:3-7` | vazio e curto C1 · grupos C1 · 11 dígitos C1 · não dígito C2 · mais de 11 C4 · colado com pontos C3 | - |
| ramos de `telefoneMascara` (6) | `web/src/mascaras.ts:9-20` | vazio C8 · 1 a 2 dígitos C8 · 3 a 6 dígitos C8 · 10 C8 · 11 C8 · `55` com mais de 11 C9 · mais de 11 C10 | - |
| ramos de `protocoloMascara` (3) | `web/src/mascaras.ts:22-25` | sem dígito C12 · com `esc` C12 · acima de 6 C12 | - |
| ramos de `enderecoMascara` (4) | `web/src/mascaras.ts:27-34` | acento C13 · espaço C13 · caractere proibido C13 · acima de 30 C13 · na tela C13 | - |
| `POST /api/admin/aparencia/imagens/icone` statuses (4, `Surface`) | `EnviarImagemInstituicao.cs` e `plan.md ## Surface` | 200 C14 · 400 C16 · 413 C17 · 415 C15 | - |
| tipo do ícone aceito (2) | `EnviarImagemInstituicao.cs:51` | PNG C16 (e 200 em `AparenciaAdminTests.cs:260-261`) · JPEG C14 | - |
| recusa de dimensão por formato (2 x 3) | `IconeAceito`, `DoPng`, `DoJpeg` | PNG não quadrado/pequeno C16 · JPEG não quadrado/pequeno C16 · JPEG sem SOF C16 | - |
| `GET /api/instituicoes/{slug}/manifest.webmanifest` statuses (2) | `plan.md ## Surface` | 200 C18 · 404 C18 | - |
| `GET /api/instituicoes/{slug}` statuses (2) | `plan.md ## Surface` | 200 C18 · 404 C18 | - |
| `iconeTipo` (3) | `VerInstituicao.cs` | `image/jpeg` C18 · `image/png` C18 · `null` C18 | - |
| Landing doors (2) | `plan.md ## Landing` | `web/src/mascaras.ts` com `cpfMascara`, `telefoneMascara`, `protocoloMascara`, `enderecoMascara`, `soDigitos` C1-C4, C8-C13 · `type` do manifesto `ManifestoInstituicao.Icones` C18 | - |

Os 404 do envio por tipo desconhecido (`Tipo_desconhecido_404`, `AparenciaAdminTests.cs:325`) e a recusa do campo `arquivo` ausente já existiam e não mudam; ficam fora do `Surface` desta feature.

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| Decide, função pura (`web/src/mascaras.ts`) | `web/src/mascaras.ts` | na própria camada: C1 · C2 · C3 · C4 · C8 · C9 · C10 · C12 · C13 em `web/src/mascaras.test.ts` | yes - um caso por linha da entrada de cada máscara: `:6-16` (9 entradas do CPF), `:35-41` (10, 11, 1, 2, 3, 7 dígitos e vazio), `:45-46`, `:50`, `:54-58`, `:62-65` |
| Decide, atravessa a rota (tipo e dimensão do ícone) | `Esclari.Api/Instituicoes/Aparencia/EnviarImagemInstituicao.cs` | na rota HTTP: C14 · C15 · C16 · C17 em `AparenciaAdminTests.cs` | yes - PNG e JPEG aceitos (`:260-261`, `:312`) e recusados em cada status: 400 (`:258-259`, `:281-284`), 413 (`:244`, `:304`), 415 (`:226-228`) |
| Instrumentação nas telas (máscara no `onChange`) | `Acesso.tsx`, `Usuarios.tsx`, `Equipe.tsx`, `Instituicoes.tsx`, `Aparencia.tsx`, `ChamadoDetalhe.tsx`, `aparencia.tsx` | uma na tela por formulário: valor mostrado e corpo enviado | yes - Login e Primeiro acesso (`acesso/mascaras.test.tsx:18,22,31,33`), Novo usuário e edição (`cadastros/mascaras.test.tsx:29,35,43,46`), Equipe (`plataforma/mascaras.test.tsx:39,44`), Criar instituição (`:60,64`), Endereço (`:74`), Telefone (`aparencia/mascaras.test.tsx:25,28`), Duplicado (`chamados/mascaras.test.tsx:15`), favicon (`iconeJpeg.test.tsx:19`) |

`Swept`, linhas `existing` relidas contra o código:

- idempotency: `Enviar_imagem_grava_e_reenviar_sobe_a_versao` existe em `AparenciaAdminTests.cs:188` e afirma `:207` `Assert.Equal(2, (await r2.Json()).GetProperty("versao").GetInt32())`. A restrição citada existe.
- authorization: `InstituicoesExtensoes.cs:14-18` registra `EnviarImagemInstituicao.Mapear(admin)` sob `api.MapAdmin(Funcoes.AparenciaGerenciar)`, e `Rotas_de_admin_403_e_401` (`AparenciaAdminTests.cs:407`) afirma 403 para aluno e atendente e 401 para anônimo no `POST .../imagens/logo`, que passa pelo mesmo handler. A restrição citada existe.
- dependency failure: `Minio_fora_503_sem_mudar_a_imagem` existe em `AparenciaAdminTests.cs:334` e afirma `:343` `Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode)`. A restrição citada existe.

## Faults injected

Isolamento: pelo pedido da execução, as falhas foram aplicadas na árvore real e revertidas com `git checkout -- <arquivo>` depois de cada prova (sem `git stash`). `git status --porcelain` antes: só `?? .tmp-capturas/`; depois de cada falha e no fim: igual. Depois da última falha, `dotnet build Esclari.slnx` refez o `bin` a partir do código restaurado (0 erros).

| Mutation | Location | Killed |
| --- | --- | --- |
| `d.length > 9` -> `d.length > 10` (o traço do CPF sai um dígito tarde) | `web/src/mascaras.ts:6` | yes - `mascaras.test.ts > cpf agrupa aos poucos` com falha |
| `cpf: soDigitos(v.cpf)` -> `cpf: v.cpf` no login | `web/src/acesso/Acesso.tsx:239` | yes - `cpf do login e do primeiro acesso` com falha |
| `soDigitos` removido do corpo de Criar instituição | `web/src/plataforma/Instituicoes.tsx:51` | yes - `cpf da equipe e da nova instituição` com falha |
| `soDigitos` removido do corpo de Usuários | `web/src/administracao/cadastros/Usuarios.tsx:32` | yes - `cpf do novo usuário` e `cpf ao editar usuário` com falha (2 de 2) |
| `telefoneMascara` removida do `onChange` do Telefone | `web/src/administracao/aparencia/Aparencia.tsx:114` | yes - `telefone do contato` com falha |
| `enderecoMascara` removida do `onChange` do Endereço | `web/src/plataforma/Instituicoes.tsx:67` | yes - `endereço da nova instituição` com falha (a de CPF, no mesmo arquivo, passou) |
| `protocoloMascara` removida do `onChange` do Duplicado | `web/src/chamados/ChamadoDetalhe.tsx:358` | yes - `protocolo do duplicado` com falha |
| atualização do `type` do favicon removida | `web/src/aparencia.tsx:106` | yes - `favicon com o tipo do ícone` com falha |
| dica do ícone volta a `PNG quadrado de pelo menos` | `web/src/administracao/aparencia/abasDaAparencia.ts:11` | yes - `guidelinesFormatos > C36 as ajudas de tamanho e de pixels usam espaço inseparável` com falha (1 de 31) |
| SOF do JPEG: largura lida do offset da altura (`i + 7` -> `i + 5`) | `Esclari.Api/Instituicoes/Aparencia/EnviarImagemInstituicao.cs:32` | yes - `Icone_jpeg_nao_quadrado_ou_pequeno_400(1024, 512, comSof: True)` com falha (1 de 9) |
| limite de 512 px -> 511 px | `EnviarImagemInstituicao.cs:22` | yes - `Icone_nao_quadrado_ou_pequeno_400(511, 511)` e `Icone_jpeg_nao_quadrado_ou_pequeno_400(511, 511)` com falha (2 de 9) |
| `type` do ícone no manifesto fixo em `image/png` | `Esclari.Api/Instituicoes/Carga/ManifestoInstituicao.cs:43` | yes - `Icone_jpeg_declara_image_jpeg` com falha (1 de 10) |
| `iconeTipo` fixo em `image/png` | `Esclari.Api/Acesso/VerInstituicao.cs:29` | yes - `Icone_jpeg_declara_image_jpeg` com falha (1 de 10) |
| mensagem do 415 volta a `O ícone deve ser PNG.` | `EnviarImagemInstituicao.cs:52` | yes - `Tipo_de_imagem_recusado_415` com falha |

Toda prova nomeada de C1 a C19 falhou ao menos uma vez, com uma exceção: a asserção de `maxLength` e `inputMode` (C7) e a de `type="tel"` não foram mutadas (ver `## Observations`). `mutator` não rodou (AD-025).

## Observations

1. **Precisão do `Observable`, sem check**: a linha "teclado aberto" do plano diz que o telefone usa `inputMode="tel"`. O código usa `type="tel"` no `Input` (`web/src/administracao/aparencia/abasDaAparencia.ts:59`, `Aparencia.tsx:114`), que abre o mesmo teclado de telefone, e nenhum teste afirma `type` ou `inputmode` do Telefone. Remover o `tipo: 'tel'` sobreviveria, mas também desligaria a máscara (`tipo === 'tel'`), então `telefone do contato` o mataria. Não é critério (AC), não derruba o veredito.
2. O `Assumption` "telefone 0800 ou fora de 10 e 11 dígitos: o valor já gravado aparece como está até o usuário editar" não tem check. Está marcado `Confirmed? n` e fica fora dos AC, então não conta como membro.
3. `Impact` diz que `estrutura.test.ts` vai de 53 para 55 arquivos; `web/src/test/estrutura.test.ts:9,67,69` lista `iconeJpeg.test.tsx`, `mascaras.test.ts` e os dois `mascaras.test.tsx`, e passa na suíte inteira (C20).

## Gate

`dotnet test Esclari.Api.Tests` - 1392 passed, 0 failed
`cd web; npx vitest run` - 1232 passed, 0 failed (140 arquivos)
`npm --prefix web run build` - exit 0
`dotnet build Esclari.slnx` - 0 avisos, 0 erros
`crapper` - exit 0; `dryer` - exit 0

## Not checked

- Passo 5 (percorrer o fluxo com o usuário): não feito, não cabe nesta execução.
- Navegador real e Playwright (`web/e2e`): não rodaram; nenhum check os pede. As provas de tela são jsdom.
- Deploy e produção: não tocados (AGENTS.md).
