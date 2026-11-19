# Máscaras nos campos e ícone do app em JPEG - checks

Profile: standard
Plan: `.specs/features/mascaras-e-icone-jpeg/plan.md`

19 checks em 3 slices · 2 one-way doors · 0 perguntas abertas (plano aprovado pelo usuário em 19/11: "aprovado")

**Como as provas rodam.** Na raiz `D:\esclari`. API: `docker compose up -d postgres mailpit minio` e uma execução de `dotnet test` por vez. Vitest: `cd web; npx vitest run <arquivo> -t "<nome>"`. Um filtro que não seleciona teste sai com 0 (L-002): conferir a contagem no relatório. Mutação suspensa (AD-025).

## Checks

### S1 - CPF com máscara · 9 arquivos · ~120 KB · ~30k

**C1** - `cpfMascara` agrupa aos poucos: `046` -> `046`, `0464` -> `046.4`, `0464733` -> `046.473.3`, `04647333167` -> `046.473.331-67`, e `''` -> `''` (AC 1)
Proof: `cd web; npx vitest run src/mascaras.test.ts -t "cpf agrupa aos poucos"`

**C2** - `cpfMascara` ignora o que não é dígito: `0a4b6` -> `046`, `abc` -> `''` (AC 2)
Proof: `cd web; npx vitest run src/mascaras.test.ts -t "cpf ignora o que não é dígito"`

**C3** - `cpfMascara('046.473.331-67')` e `cpfMascara('04647333167')` devolvem `046.473.331-67` (AC 3)
Proof: `cd web; npx vitest run src/mascaras.test.ts -t "cpf colado com ou sem pontos"`

**C4** - `cpfMascara('046473331679999')` devolve `046.473.331-67` (AC 4)
Proof: `cd web; npx vitest run src/mascaras.test.ts -t "cpf corta em 11"`

**C5** - Nos 5 formulários (Login, Primeiro acesso, Novo usuário, Equipe da plataforma, Criar instituição), digitar `04647333167` mostra `046.473.331-67` no campo CPF, e o corpo enviado leva `cpf: "04647333167"` (`admin.cpf` em Criar instituição) (AC 1, AC 5)
Proof: `cd web; npx vitest run src/acesso/mascaras.test.tsx -t "cpf do login e do primeiro acesso"`
Proof: `cd web; npx vitest run src/administracao/cadastros/mascaras.test.tsx -t "cpf do novo usuário"`
Proof: `cd web; npx vitest run src/plataforma/mascaras.test.tsx -t "cpf da equipe e da nova instituição"`

**C6** - Editar um usuário com CPF gravado `11144477735` mostra `111.444.777-35` no campo e envia `cpf: "11144477735"` (AC 3, AC 5)
Proof: `cd web; npx vitest run src/administracao/cadastros/mascaras.test.tsx -t "cpf ao editar usuário"`

**C7** - Os 5 campos CPF têm `inputMode="numeric"` e `maxLength` 14 (AC 6)
Proof: `cd web; npx vitest run src/acesso/mascaras.test.tsx -t "cpf do login e do primeiro acesso"`
Proof: `cd web; npx vitest run src/administracao/cadastros/mascaras.test.tsx -t "cpf do novo usuário"`
Proof: `cd web; npx vitest run src/plataforma/mascaras.test.tsx -t "cpf da equipe e da nova instituição"`

### S2 - Telefone, protocolo e endereço · 6 arquivos · ~90 KB · ~23k

**C8** - `telefoneMascara`: `6533211234` -> `(65) 3321-1234`, `65999991234` -> `(65) 99999-1234`, `6` -> `(6`, `659` -> `(65) 9`, `''` -> `''` (AC 7)
Proof: `cd web; npx vitest run src/mascaras.test.ts -t "telefone com 10 e 11 dígitos"`

**C9** - `telefoneMascara`: `+55 65 99999-1234` -> `(65) 99999-1234`, `556533211234` -> `(65) 3321-1234` (AC 8)
Proof: `cd web; npx vitest run src/mascaras.test.ts -t "telefone tira o 55"`

**C10** - `telefoneMascara('659999912349999')` devolve `(65) 99999-1234` (AC 9)
Proof: `cd web; npx vitest run src/mascaras.test.ts -t "telefone corta em 11"`

**C11** - Na Aparência, aba Contato, digitar `65999991234` em "Telefone" mostra `(65) 99999-1234` e salva `contato.telefone` `(65) 99999-1234` (AC 7)
Proof: `cd web; npx vitest run src/administracao/aparencia/mascaras.test.tsx -t "telefone do contato"`

**C12** - `protocoloMascara`: `esc123456` -> `ESC-123456`, `1234567` -> `ESC-123456`, `ESC-` -> `''`, `abc` -> `''`; no detalhe do chamado, digitar `esc12` em "Duplicado de outro pedido" mostra `ESC-12` (AC 10)
Proof: `cd web; npx vitest run src/mascaras.test.ts -t "protocolo"`
Proof: `cd web; npx vitest run src/chamados/mascaras.test.tsx -t "protocolo do duplicado"`

**C13** - `enderecoMascara`: `Faculdade São João` -> `faculdade-sao-joao`, `A_b!c` -> `abc`, 40 letras `a` -> 30; em Criar instituição, digitar `Univag Sul` em "Endereço" mostra `univag-sul` (AC 11)
Proof: `cd web; npx vitest run src/mascaras.test.ts -t "endereço"`
Proof: `cd web; npx vitest run src/plataforma/mascaras.test.tsx -t "endereço da nova instituição"`

### S3 - Ícone do app em JPEG · 8 arquivos · ~110 KB · ~28k

**C14** - `POST /api/admin/aparencia/imagens/icone` com JPEG 512×512 e com JPEG 1024×1024 responde `200` com `tipo` `icone` e `versao` 1, grava `imagem_instituicao.conteudo` `image/jpeg` e o objeto no MinIO (AC 12)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AparenciaAdminTests.Icone_jpeg_quadrado_200"`

**C15** - Ícone GIF responde `415` com `detail` `O ícone deve ser PNG ou JPEG.` e nada é gravado; Logo e Login continuam recusando GIF e PDF com `415` (AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AparenciaAdminTests.Tipo_de_imagem_recusado_415"`

**C16** - Ícone PNG e JPEG de 512×511 e 511×511, e JPEG sem marcador SOF, respondem `400` com erro em `arquivo` `O ícone deve ser um PNG ou JPEG quadrado de pelo menos 512 px.`, sem gravar linha nem objeto (AC 14)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AparenciaAdminTests.Icone_nao_quadrado_ou_pequeno_400"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AparenciaAdminTests.Icone_jpeg_nao_quadrado_ou_pequeno_400"`

**C17** - Ícone PNG e ícone JPEG com 1 MB + 1 byte respondem `413` com `detail` contendo `1 MB` (AC 15)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AparenciaAdminTests.Imagem_acima_do_limite_413"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AparenciaAdminTests.Icone_jpeg_quadrado_200"`

**C18** - Com ícone JPEG, o manifesto lista os 2 ícones com `"type": "image/jpeg"` e `GET /api/instituicoes/{slug}` devolve `iconeTipo` `image/jpeg`; com ícone PNG, `image/png` nos dois; sem ícone, `iconeTipo` `null`; slug inexistente continua `404` nas duas rotas (AC 16, AC 17)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests.Icone_jpeg_declara_image_jpeg"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests.Manifesto_da_instituicao"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests.Instituicao_nova_nao_grava_a_marca_do_Esclari"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests.Instituicao_inexistente_ou_inativa_responde_404"`

**C19** - Com `iconeTipo` `image/jpeg`, o `<link rel="icon">` da instituição tem `type="image/jpeg"` e o `href` do ícone; sem ícone, volta a `type="image/png"` e `/icons/icon-192.png`; a dica do ícone é `PNG ou JPEG quadrado de pelo menos 512 px, até 1 MB.` (com espaço fixo) (AC 17, AC 18)
Proof: `cd web; npx vitest run src/iconeJpeg.test.tsx -t "favicon com o tipo do ícone"`
Proof: `cd web; npx vitest run src/guidelinesFormatos.test.tsx`

### Gates (todas as slices)

**C20** - As suítes continuam verdes e o retrato só ganha `iconeTipo`: API inteira, Vitest inteiro e o build com tipos (Impact)
Proof: `dotnet test Esclari.Api.Tests`
Proof: `cd web; npx vitest run`
Proof: `npm --prefix web run build`
Proof: `dotnet build Esclari.slnx`

**C21** - O `crapper` sobre `web/src` sai 0 no limite 9.99 e o `dryer` não acha candidato com nota 0.8 ou mais (AD-018)
Proof: `npm --prefix web run coverage; cd web; crapper --use-existing-coverage -s src --threshold 9.99`
Proof: `cd web; dryer -s src/acesso -s src/assistente -s src/chamados -s src/conhecimento -s src/indicadores -s src/administracao -s src/plataforma --threshold 0.8`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| formulários com CPF (5) | Login C5 · Primeiro acesso C5 · Novo usuário C5 · Equipe C5 · Criar instituição C5 | - |
| entrada do CPF (5) | dígitos aos poucos C1 · não dígito C2 · colado com pontos C3 · colado sem pontos C3 · mais de 11 C4 | - |
| tamanho do telefone (4) | 1 a 2 dígitos C8 · 10 C8 · 11 C8 · mais de 11 C10 | - |
| prefixo do telefone (2) | `+55` C9 · `55` C9 | - |
| entrada do protocolo (3) | com `esc` C12 · só dígitos acima de 6 C12 · sem dígito C12 | - |
| entrada do endereço (4) | acento e espaço C13 · caractere proibido C13 · acima de 30 C13 · na tela C13 | - |
| `POST /api/admin/aparencia/imagens/icone` statuses (4) | 200 C14 · 400 C16 · 413 C17 · 415 C15 | - |
| tipo do ícone aceito (2) | PNG C16 · JPEG C14 | - |
| `GET /api/instituicoes/{slug}/manifest.webmanifest` statuses (2) | 200 C18 · 404 C18 | - |
| `GET /api/instituicoes/{slug}` statuses (2) | 200 C18 · 404 C18 | - |
| `iconeTipo` (3) | `image/jpeg` C18 · `image/png` C18 · `null` C18 | - |
| Landing doors (2) | `web/src/mascaras.ts` C1-C4, C8-C13 · `type` do manifesto C18 | - |

- Afirmam status ou campo da API: C14 a C18, todos por rota HTTP
- C5 a C7, C11 a C13 e C19 são afirmações de tela

## Test policy

| Code | Required proofs | Coverage expectation |
| --- | --- | --- |
| Decide, função pura (`web/src/mascaras.ts`) | uma na própria camada | um caso por linha da entrada de cada máscara |
| Decide, atravessa a rota (tipo e dimensão do ícone) | uma na rota HTTP | PNG e JPEG em cada recusa e no aceite |
| Instrumentação nas telas (passar a máscara no `onChange`) | uma na tela por formulário | o valor mostrado e o corpo enviado |

Evidence:

- `web/src/mascaras.ts`: 4 máscaras, cerca de 12 pontos de decisão -> decide
- `EnviarImagemInstituicao.cs`: aceita tipo por recurso, mede PNG e JPEG -> decide
- análogo no repositório: `cpfFormatado` (`administracao/comum.tsx`), provado na tela; `Icone_nao_quadrado_ou_pequeno_400`, provado na rota

Cost: 1 arquivo de teste unitário, 5 de tela, 3 testes de API novos.

## Swept

- validation: C1-C4, C8-C13, C15-C17
- failure modes: C16 (JPEG truncado sem SOF cai no 400)
- idempotency: existing - reenviar o ícone sobe a `versao`, provado em `AparenciaAdminTests` (`Enviar_imagem_grava_e_reenviar_sobe_a_versao`)
- authorization: existing - a rota do ícone exige o administrador da instituição, sem mudança
- concurrency: n/a - nenhuma regra nova de concorrência; `ImagemInstituicao.Substituir` não muda
- data lifecycle: n/a - nada a migrar; ícones PNG já gravados continuam
- dependency failure: existing - `Minio_fora_503_sem_mudar_a_imagem` cobre o envio de imagem
- state transitions: n/a - nenhum estado muda
- observability: n/a - nenhum log novo

## Handoff

- S1 ~30k + S2 ~23k + S3 ~28k = ~81k (wc -c dos arquivos tocados / 4), abaixo do orçamento de 150k - um construtor
- **Boundary:** C1-C21 closed at the commit after `2d813c7`; base `17852da`. API 1392 de 1392, Vitest 140 arquivos verdes, `npm run build`, `dotnet build`, `crapper` e `dryer` saem 0
- **Settled mid-build:** `GET /api/instituicoes/{slug}` ganhou `iconeTipo` (linha acrescentada ao `Surface` antes do código, porque AC 17 precisa do tipo no web); a rota do envio é `/api/admin/aparencia/imagens/icone`. Asserções existentes trocadas para o comportamento aprovado: 6 corpos com CPF pontuado passam aos 11 dígitos (AC 5), `Tipo_de_imagem_recusado_415` recusa GIF no ícone em vez de JPEG (AC 13), `Icone_nao_quadrado_ou_pequeno_400` e `guidelinesFormatos` usam as frases novas (AC 14, AC 18), a lista de campos de `AparenciaPublicaTests` e as listas de `estrutura.test.ts` ganham os itens novos. `enderecoDe` (endereço gerado do nome) não corta em 30, porque AC 11 trata só da digitação no campo
- **Abandoned:** nada
