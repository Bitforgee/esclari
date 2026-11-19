# Verificação em 2 etapas: campo do código e identidade do Esclari - checks

Profile: ui
Plan: `.specs/features/verificacao-2-etapas/plan.md`

17 checks em 2 slices · 0 one-way doors · 1 pergunta aberta (kind `open`, não bloqueia; plano aprovado pelo usuário: "aprovado")

**Como as provas rodam.** Na raiz `D:\esclari`. API: `docker compose up -d postgres mailpit minio` e uma execução de `dotnet test` por vez. Vitest: `npm --prefix web exec vitest run src/plataforma/entrada.test.tsx -t "<nome>"`. Um filtro que não seleciona teste sai com 0 (L-002): conferir a contagem de testes executados no relatório. Mutação suspensa (AD-025).

**Testes existentes que mudam por decisão aprovada, e não por enfraquecimento.** `entrada em tres etapas: com 2FA já cadastrado pede os 6 dígitos e entra` clicava em `Entrar` depois de digitar 6 dígitos; com o envio automático (AC 3) o clique deixa de existir, e o teste passa a esperar o envio sem clique. `código errado fica na mesma etapa com a mensagem` esperava o campo com `000000`; AC 5 manda limpar, e o teste passa a esperar o campo vazio e focado. `qr code da uri e chave em grupos de 4` e `Entrar_sem_segundo_fator_devolve_segredo_e_uri` mudam por AC 8 e AC 10. Nenhuma outra asserção é tocada.

## Checks

### S1 - campo do código fácil de ler e de enviar · 2 arquivos · 24 KB · ~6k

**C1** - O campo `Código de 6 dígitos` da segunda etapa é um único `input` com `inputmode="numeric"`, `autocomplete="one-time-code"`, `maxlength="6"` e as classes `font-mono`, `text-center`, `text-2xl` (24px) e `tracking-widest` ou maior (AC 1)
Proof: `npm --prefix web exec vitest run src/plataforma/entrada.test.tsx -t "campo do codigo grande"`

**C2** - Digitar `12a3456` e colar `12-34 56789` deixam `123456` no campo: só dígitos, no máximo 6 (AC 2)
Proof: `npm --prefix web exec vitest run src/plataforma/entrada.test.tsx -t "campo do codigo so aceita digitos"`

**C3** - Ao chegar ao sexto dígito, sem clicar em `Entrar`, a tela envia exatamente um `POST /api/plataforma/auth/codigo` com `{ "codigo": "123456" }` e entra no console (AC 3)
Proof: `npm --prefix web exec vitest run src/plataforma/entrada.test.tsx -t "envia sozinho ao completar 6 digitos"`

**C4** - Com a resposta do `POST /api/plataforma/auth/codigo` ainda pendente, apertar `Enter` e digitar mais um dígito não gera segundo `POST`; a lista de `POST` do teste tem um só `codigo` (AC 4)
Proof: `npm --prefix web exec vitest run src/plataforma/entrada.test.tsx -t "nao envia de novo enquanto pendente"`

**C5** - Quando `POST /api/plataforma/auth/codigo` devolve 401 `Código inválido.`, a mensagem aparece com `role="alert"` abaixo do campo, o campo fica com valor `''` e é o elemento focado (AC 5)
Proof: `npm --prefix web exec vitest run src/plataforma/entrada.test.tsx -t "codigo errado limpa o campo e devolve o foco"`

**C6** - Digitar `123` e apertar `Enter` (e, em outro passo do mesmo teste, clicar em `Entrar`) envia `{ "codigo": "123" }` e mostra a mensagem de erro de `codigo` da API, sem exigir 6 dígitos no navegador (AC 6)
Proof: `npm --prefix web exec vitest run src/plataforma/entrada.test.tsx -t "envia com menos de 6 digitos e mostra o erro da API"`

**C7** - Ao abrir a segunda etapa, `document.activeElement` é o campo `Código de 6 dígitos` (AC 7)
Proof: `npm --prefix web exec vitest run src/plataforma/entrada.test.tsx -t "abre com o foco no codigo"`

### S2 - QR code com o símbolo do Esclari e ícone na URI · 4 arquivos · 14 KB · ~4k

**C8** - Na etapa `CADASTRAR`, o `src` da imagem `QR code para o aplicativo autenticador` é o SVG de `QRCode.toString(uri, { type: 'svg', errorCorrectionLevel: 'H' })`, e há uma imagem do símbolo com `src="/marca/simbolo-pequeno.svg"` dentro de um contêiner de fundo branco no mesmo bloco do QR (AC 8)
Proof: `npm --prefix web exec vitest run src/plataforma/entrada.test.tsx -t "qr code com o simbolo no centro e correcao H"`

**C9** - O contêiner do símbolo tem no máximo 40px de largura e de altura sobre o QR de 200px (20% do lado), lidos dos atributos `width` e `height` renderizados (AC 9)
Proof: `npm --prefix web exec vitest run src/plataforma/entrada.test.tsx -t "simbolo ocupa no maximo 20% do qr"`

**C10** - `POST /api/plataforma/auth/entrar` de um operador sem segundo fator devolve `uri` igual a `otpauth://totp/Esclari:{email}?secret={segredo}&issuer=Esclari&image=` seguido de `Uri.EscapeDataString("{App:UrlBase}/icons/icon-192.png")` (AC 10)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Entrar_sem_segundo_fator_devolve_segredo_e_uri"`

**C11** - `Totp.Uri("ana@equipe.esclari", segredo)` sem endereço de imagem continua `otpauth://totp/Esclari:ana@equipe.esclari?secret={segredo}&issuer=Esclari`, sem `image`; e `Totp.Uri` com endereço acrescenta `&image=` com o endereço codificado (AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TotpTests.Segredo_novo_tem_20_bytes_em_base32_e_a_uri_do_aplicativo"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TotpTests.Uri_com_imagem_codifica_o_endereco"`

**C12** - Na etapa `CODIGO`, a API devolve `segredo: null` e `uri: null`, e a tela não mostra QR code, símbolo, chave nem o link `Abrir no aplicativo` (AC 12)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Entrar_com_segundo_fator_pede_o_codigo"`
Proof: `npm --prefix web exec vitest run src/plataforma/entrada.test.tsx -t "codigo sem qr code nem chave"`

**C13** - Na etapa `CADASTRAR`, a chave continua em grupos de 4 (`JBSW Y3DP EHPK 3PXP`), o link `Abrir no aplicativo` só aparece em aparelho de toque e o `alt` do QR continua `QR code para o aplicativo autenticador` (AC 13)
Proof: `npm --prefix web exec vitest run src/plataforma/entrada.test.tsx -t "sem 2FA mostra a chave em grupos de 4 e o link para o aplicativo"`
Proof: `npm --prefix web exec vitest run src/plataforma/entrada.test.tsx -t "link otpauth so em aparelho de toque"`

### Transversais · 4 arquivos · ~0k extra

**C14** - Os statuses de `POST /api/plataforma/auth/entrar` não mudam: 400 para corpo sem e-mail ou senha, 401 com a mesma frase nos três casos, 429 na sexta tentativa (Surface)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Corpo_invalido_400"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Entrar_401_com_a_mesma_frase_nos_tres_casos"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaLimiteTests.Limitador_auth_429_na_sexta_tentativa"`

**C15** - O ícone da URI existe e é o mesmo que o manifesto do PWA já publica sem login: `web/public/icons/icon-192.png` existe, e o manifesto padrão lista `/icons/icon-192.png` (AC 10)
Proof: `powershell -Command "if (-not (Test-Path web/public/icons/icon-192.png)) { exit 1 }"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests.Manifesto_padrao_sem_aparencia"`

**C16** - As suítes continuam verdes e o build não quebra por complexidade (CA1502, AD-018): `dotnet build`, `dotnet test`, `npm --prefix web test` e `npm --prefix web run build`
Proof: `dotnet build Esclari.slnx`
Proof: `dotnet test Esclari.Api.Tests`
Proof: `npm --prefix web test`
Proof: `npm --prefix web run build`

**C17** - O `crapper` sobre `web/src` sai 0 no limite 9.99 e o `dryer` não acha candidato com nota 0.8 ou mais (AD-018)
Proof: `npm --prefix web run coverage; cd web; crapper --use-existing-coverage -s src --threshold 9.99`
Proof: `cd web; dryer -s src/acesso -s src/assistente -s src/chamados -s src/conhecimento -s src/indicadores -s src/administracao -s src/plataforma --threshold 0.8`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| formas de enviar o código (4) | automático ao 6º dígito C3 · `Enter` com menos de 6 C6 · clique em `Entrar` com menos de 6 C6 · duplicado durante o envio C4 | - |
| entrada de caracteres no campo (3) | dígito C2 · letra e símbolo descartados C2 · além do 6º descartado C2 | - |
| estados do campo (4) | abertura com foco C7 · pendente C4 · erro C5 · sucesso C3 | - |
| etapas da resposta de `entrar` (2) | `CADASTRAR` C10 · `CODIGO` C12 | - |
| `POST /api/plataforma/auth/entrar` statuses (4) | 200 C10 · 400 C14 · 401 C14 · 429 C14 | - |
| partes do QR (3) | nível H C8 · símbolo no centro C8 · limite de 20% C9 | - |
| partes da URI (4) | `secret` C10 · `issuer` C10 · `image` C10 · sem `image` quando não há endereço C11 | - |
| itens preservados do cadastro (3) | chave em grupos de 4 C13 · link só em toque C13 · `alt` do QR C13 | - |
| linhas de Observable de tela com critério (3; celular deitado é `n/a - a moldura já rola na vertical`, no plano) | celular 360×780 C1 · teclado aberto C7 · toque C1 | - |
| startup config (0 assemblies) | n/a - nada precisa estar configurado antes da primeira requisição; `App:UrlBase` já tem valor padrão em `Tokens.UrlBase` | - |

- Afirmam status ou campo da API: C10, C11, C12, C14, C15, todos com prova que passa pela rota HTTP ou pelo `Totp.Uri` que ela chama
- C1 a C9 e C13 afirmam a tela no jsdom; tamanho em pixels (C1, C9) é afirmado pelas classes e atributos renderizados, não pela medida do navegador
- C17 e C16 não afirmam comportamento novo; protegem os gates do AD-018

## Test policy

| Code | Required proofs | Coverage expectation |
| --- | --- | --- |
| Decide, na tela (`Codigo`: filtro de dígitos, envio automático, trava de pendente, limpar e focar no erro) | uma na própria camada | um caso por membro de `formas de enviar o código` e de `estados do campo` |
| Decide, atravessa a rota HTTP (`entrar` monta a URI com a imagem) | uma na rota | a URI inteira, comparada por igualdade |
| `Totp.Uri`, monta uma string com um condicional (`image` presente ou não) | uma na própria camada | com e sem endereço |
| `QrCode` (monta SVG e sobrepõe o símbolo) | uma na própria camada | nível H, símbolo e limite de tamanho |

Evidence:

- `web/src/plataforma/Entrada.tsx`: `Codigo` ganha 3 decisões (filtro, envio no 6º dígito, trava de pendente) mais o `onError`, ~4 pontos de decisão novos, ainda abaixo do CRAP 10 (C17)
- `Esclari.Api/Plataforma/Totp.cs`: `Uri` ganha 1 condicional
- analogia mais próxima no repositório: `TotpTests.cs` já prova `Totp.Uri` com um caso por forma; `entrada.test.tsx` já prova o campo e o QR no mesmo nível

Cost: 4 provas a mais na própria camada em 2 arquivos de teste. Sem estas linhas, o envio automático seria provado só por um teste que passa por ele de passagem.

## Swept

- validation: C2, C6
- failure modes: C5, C12
- idempotency: C4
- authorization: existing - `entrar` continua `AllowAnonymous` com `RequireRateLimiting("auth")` (C14); o ícone é arquivo estático de `wwwroot`, já público pelo manifesto (C15)
- concurrency: C4
- data lifecycle: n/a - a URI é gerada a cada pedido e o segredo gravado não muda
- dependency failure: n/a - o QR é gerado no navegador, sem rede (`qr code sem chamada de rede` já existe e segue valendo); o aplicativo autenticador que não acha o ícone ignora o parâmetro, e o cadastro continua funcionando
- state transitions: C3, C5, C12
- observability: n/a - nenhum requisito de log ou métrica nesta mudança

## Handoff

- S1 = ~6k (Entrada.tsx 8 KB, entrada.test.tsx 14 KB ≈ 22 KB / 4 ≈ 5,5k); S2 entra em API e Web com ~4k (Totp.cs 3 KB, EntrarNaPlataforma.cs 3 KB, PlataformaAcessoTests.cs 12 KB, TotpTests.cs 3 KB ≈ 21 KB / 4 ≈ 5k); total estimado ~11k, abaixo do orçamento de 150k - um construtor
- Mechanism: one builder


