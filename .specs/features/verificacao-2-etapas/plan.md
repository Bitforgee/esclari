# Verificação em 2 etapas: campo do código e identidade do Esclari

## Problem

No console da plataforma, a segunda etapa do login mostra um campo de texto comum para os 6 dígitos e, no primeiro acesso, um QR code em preto e branco sem nenhum sinal do Esclari. Quem paga é o operador da plataforma, que cadastra o aplicativo autenticador uma vez e digita o código em todo login. O código de 6 dígitos de um campo `h-9` com texto pequeno é difícil de conferir antes de enviar, e depois de escanear o QR a entrada aparece no aplicativo como um item genérico, parecido com qualquer outro. Não há medida de uso, porque não há produção. O pedido veio do dono do projeto, olhando a tela.

Depois disto, o campo do código é grande, centralizado, em fonte mono e com espaço entre os dígitos, e envia sozinho ao completar 6 dígitos. O QR code mostra o símbolo do Esclari no centro. A URI do QR também leva o endereço de um ícone, para os aplicativos que sabem usá-lo.

## Flow

Reusa o `Totp.Uri` (que já monta o `otpauth://`), o `QRCode` (pacote `qrcode`, já usado em `Entrada.tsx`), o `Campo` de `web/src/Campo.tsx` e o `Tokens.UrlBase(cfg)`, que já dá o endereço público da API nos e-mails e links.

1. `POST /api/plataforma/auth/entrar` (exists) - `EntrarNaPlataformaHandler` (exists) monta a URI com `Totp.Uri` (exists), agora com o endereço de `/icons/icon-192.png` (exists, servido de `wwwroot`) tirado de `Tokens.UrlBase(cfg)` (exists)
2. `Entrada.tsx` (exists), `QrCode` (exists) - gera o SVG com correção de erro H e sobrepõe o símbolo `/marca/simbolo-pequeno.svg` (exists) no centro, num quadrado branco
3. `Entrada.tsx`, `Codigo` (exists) - o campo `codigo` fica grande e, ao chegar a 6 dígitos, envia `POST /api/plataforma/auth/codigo` (exists, sem mudança)
4. out: o aplicativo autenticador lê a URI; os que aceitam `image` (FreeOTP) mostram o ícone, os demais ignoram o parâmetro

## Impact

| Front | What changes |
| --- | --- |
| domain | nenhum termo novo |
| `uri` devolvida por `POST /api/plataforma/auth/entrar` | ganha `&image=<url do ícone>` no fim. É o único campo que muda; `etapa` e `segredo` continuam iguais |
| `PlataformaAcessoTests.cs:111` e `TotpTests.cs:62` | afirmam a URI antiga, exata. O de `TotpTests` continua valendo para `Totp.Uri(email, segredo)` sem imagem; o de `PlataformaAcessoTests` passa a afirmar a URI com `image` |
| `web/src/plataforma/entrada.test.tsx` | o teste `qr code da uri e chave em grupos de 4` compara o SVG gerado sem nível H; passa a esperar o nível H, mais o símbolo. O teste do campo (`inputmode`, `autocomplete`, só dígitos) continua valendo |
| stored data | nothing to migrate: o segredo e o cadastro do operador não mudam; quem já cadastrou o aplicativo continua entrando com o mesmo código |
| quem lê a URI hoje | só o `QrCode` desta tela e o link "Abrir no aplicativo" (`href={etapa.uri}`); ambos aceitam o parâmetro extra |

## Relations

None - no stored-data shape change.

## Surface

| Route | In | Out | Status |
| --- | --- | --- | --- |
| `POST /api/plataforma/auth/entrar` | `email`, `senha` (sem mudança) | `etapa` · `segredo` · `uri` (com `image` no fim, quando `etapa` é `CADASTRAR`) | `200`, `400`, `401`, `429` |

## Landing

None - a URI é gerada a cada pedido e não é gravada; o parâmetro `image` é opcional e some em um commit. Nenhum dado, contrato consumido por outro sistema nem dependência nova entra (o campo é um `input` comum e o QR usa o pacote que já existe).

## Criteria

### S1: campo do código fácil de ler e de enviar (P1)

O operador vê os 6 dígitos grandes, confere e entra sem apertar o botão.

**Acceptance Criteria**

1. The system SHALL render the field `codigo` of the second step with a single `input` (`inputmode="numeric"`, `autocomplete="one-time-code"`, `maxlength=6`) in a mono font, centered, with letter-spacing, and with text of at least 24px
2. WHEN the operator types or pastes characters THEN the system SHALL keep only the digits and at most 6 of them in the field
3. WHEN the field reaches 6 digits and no submission is pending THEN the system SHALL send `POST /api/plataforma/auth/codigo` once with `{ codigo }`, without a click on `Entrar`
4. WHILE a submission is pending the system SHALL NOT send a second `POST /api/plataforma/auth/codigo`, even if the field changes or `Enter` is pressed
5. WHEN `POST /api/plataforma/auth/codigo` fails THEN the system SHALL show the error under the field (`role="alert"`), clear the field and put the focus back on it
6. WHEN the operator presses `Entrar` or `Enter` with fewer than 6 digits THEN the system SHALL still send the form and show the API error for `codigo`
7. WHEN the screen opens THEN the system SHALL focus the field `codigo`

### S2: QR code com o símbolo do Esclari e ícone na URI (P1)

O QR do primeiro acesso é reconhecível e continua legível, e o aplicativo recebe o ícone quando sabe usá-lo.

**Acceptance Criteria**

8. WHEN the step is `CADASTRAR` THEN the system SHALL draw the QR code with error correction level H and the symbol `/marca/simbolo-pequeno.svg` centered over it, on a white square with margin
9. The system SHALL limit the symbol with its white square to at most 20% of the QR side
10. WHEN the step is `CADASTRAR` THEN the API SHALL answer `uri` as `otpauth://totp/Esclari:{email}?secret={segredo}&issuer=Esclari&image={UrlBase}/icons/icon-192.png`, with the image address URL-encoded
11. WHEN `Totp.Uri` is called without an image address THEN the API SHALL keep returning the URI without the `image` parameter
12. WHEN the step is `CODIGO` THEN the API SHALL keep `segredo` and `uri` as `null` and the web SHALL NOT draw any QR code
13. The system SHALL keep the key in groups of 4, the link `Abrir no aplicativo` on touch screens and the `alt` `QR code para o aplicativo autenticador` as they are today

## Out of scope

| Excluded | Why |
| --- | --- |
| Códigos de recuperação e troca de aparelho | outro assunto; hoje o operador novo é criado pela linha de comando da plataforma |
| Verificação em 2 etapas para o login das instituições | só o console da plataforma tem segundo fator hoje |
| Garantir o ícone no Google Authenticator e no Microsoft Authenticator | o parâmetro `image` não faz parte do formato de URI deles; só o FreeOTP o documenta, e os demais ignoram. Não há como controlar isso do nosso lado |
| Seis caixinhas separadas (`input-otp`) | o dono pediu a opção recomendada, o `input` único; evita dependência nova |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| Qual imagem vai em `image` | `/icons/icon-192.png`, o ícone do PWA que já existe, servido sem login | é PNG, quadrado e já está na marca; evita criar arquivo novo | n |
| Endereço absoluto do ícone | `Tokens.UrlBase(cfg)` (`App:UrlBase`) | já é o endereço público usado nos e-mails; em desenvolvimento aponta para `localhost`, que o celular não alcança, então o ícone só aparece depois da hospedagem | n |
| Envio automático errou o código | limpa o campo e devolve o foco | o operador digita de novo sem apagar; o código TOTP já usado é recusado de qualquer forma | n |
| Cor do símbolo no QR | a mesma do texto da marca em fundo branco, sem tema escuro | o QR precisa de fundo branco para ser lido, nos dois temas | n |
| Legibilidade do QR com o símbolo | garantida pelo nível H e pelo limite de 20% do lado; sem leitura automática no teste | um decodificador no teste seria dependência nova; o dono confere com o celular | n |

**Open questions:**

| # | Kind | Question | Until answered |
| --- | --- | --- | --- |
| 1 | open | O dono escaneia o QR com o aplicativo que usa hoje e confirma que lê | AC 8 e AC 9 ficam provados por medida, sem leitura real |

## Observable

| Surface | Decision | Landing |
| --- | --- | --- |
| screen `Entrada`, etapa do código | empty state | n/a - a tela sempre tem o campo vazio na abertura; AC 7 |
| screen `Entrada`, etapa do código | loading | AC 4 (botão `Entrar` pendente via `BotaoAcao`, existente) |
| screen `Entrada`, etapa do código | error state | AC 5 |
| screen `Entrada`, etapa do código | unauthorised | existing - a etapa expirada devolve erro pela mesma rota e aparece no `ErroDe` |
| screen `Entrada`, etapa do código | density and ordering | AC 1 |
| screen `Entrada`, etapa do código | destructive action confirms | n/a - entrar não destrói nada |
| screen `Entrada`, etapa do código | celular 360×780 | AC 1 (campo ocupa a largura da moldura `max-w-sm`; 6 dígitos em 24px mono cabem em 360px) |
| screen `Entrada`, etapa do código | celular deitado 844×390 | n/a - a moldura já rola na vertical; o campo não muda de comportamento |
| screen `Entrada`, etapa do código | teclado aberto | AC 7 (teclado numérico por `inputmode`); o campo continua visível por rolagem, sem posição fixa |
| screen `Entrada`, etapa do código | toque | AC 1 (altura mínima de 44px, `pointer-coarse:h-11` do `Input`, mantida no campo grande) |
| screen `Entrada`, QR do cadastro | density and ordering | AC 8, AC 9 |
| screen `Entrada`, QR do cadastro | loading | existing - `QrCode` não desenha nada até o SVG existir |
| screen `Entrada`, QR do cadastro | celular 360×780, deitado, teclado, toque | n/a - o QR tem tamanho fixo de 200px, sem mudança |
| API `POST /api/plataforma/auth/entrar` | error shape and codes | existing - `Problemas.Invalido` (400) e `Problemas.Erro` (401); AC 10 só muda o corpo do 200 |
| API `POST /api/plataforma/auth/entrar` | who may call it, rate limit | existing - `AllowAnonymous` e `RequireRateLimiting("auth")` |
| API `POST /api/plataforma/auth/entrar` | versioning | n/a - a rota não tem versão e o campo `uri` só ganha um parâmetro no fim |

## Sources

- `.specs/STATE.md`, AD-018 (complexidade ciclomática e gates do `web/`) - o `Codigo` não pode estourar o CRAP 10 nem o CA1502.
- [FreeOTP: formato de URI](https://github.com/freeotp/freeotp-android/blob/master/URI.md) - o parâmetro `image` vem dele; [Key Uri Format do Google Authenticator](https://github.com/google/google-authenticator/wiki/Key-Uri-Format) não o lista (https://github.com/google/google-authenticator/issues/685).
