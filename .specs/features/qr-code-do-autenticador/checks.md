# QR code do aplicativo autenticador checks

Profile: standard

## Intent

No primeiro acesso ao console (`/plataforma`, etapa `CADASTRAR`), o operador só via a chave em grupos de 4 e um link "Abrir no aplicativo" com `otpauth://`. No computador o navegador recusa o link ("Prevented navigation to otpauth://... due to an unknown protocol") e não há como escanear, então o cadastro do 2FA exige digitar 32 letras no celular. Pedido do dono em 18/11, no primeiro deploy. A correção mostra um QR code da `uri` gerado no próprio navegador (pacote npm `qrcode` 1.5.4, MIT, sem chamada de rede, para o segredo nunca sair da página), mantém a chave digitável como alternativa, e mostra o link `otpauth://` só em aparelho de toque (`(pointer: coarse)`), onde o aplicativo autenticador instalado o abre. Reverte o "QR code" do `Out of scope` do `console-da-plataforma`. Sem decisão difícil de desfazer: a API não muda e a dependência sai com um commit.

## Checks

**C1** - Com `etapa` `CADASTRAR`, a entrada mostra uma imagem com o nome "QR code para o aplicativo autenticador" cujo `src` é o SVG `data:image/svg+xml` que `QRCode.toString(uri, { type: "svg" })` gera para exatamente a `uri` devolvida pela API, e a chave continua em grupos de 4 (`JBSW Y3DP EHPK 3PXP`)
Proof: `npm --prefix web test -- src/plataforma/entrada.test.tsx -t "qr code da uri e chave em grupos de 4"`

**C2** - O QR code é gerado sem nenhuma chamada de rede: depois de mostrar o QR, as únicas requisições feitas são as da API do console (`/api/plataforma/...`)
Proof: `npm --prefix web test -- src/plataforma/entrada.test.tsx -t "qr code sem chamada de rede"`

**C3** - Em tela de ponteiro fino (computador) o link "Abrir no aplicativo" não aparece; em aparelho de toque (`(pointer: coarse)`) ele aparece com `href` igual à `uri`
Proof: `npm --prefix web test -- src/plataforma/entrada.test.tsx -t "link otpauth so em aparelho de toque"`

**C4** - Com `etapa` `CODIGO` (2FA já cadastrado) não aparece QR code nem chave
Proof: `npm --prefix web test -- src/plataforma/entrada.test.tsx -t "codigo sem qr code"`

**C5** - `web/package.json` declara `qrcode` com versão exata `1.5.4` e a suíte de interface continua aceitando as dependências
Proof: `npm --prefix web test -- src/test/interface.test.ts -t "versões exatas e pacotes proibidos"`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| etapas da entrada (2) | `CADASTRAR` C1 C2 C3 · `CODIGO` C4 | - |
| tipos de ponteiro (2) | fino C3 · toque C3 | - |

## Existing assertions changed

- `entrada.test.tsx` "entrada em tres etapas: sem 2FA mostra a chave em grupos de 4 e o link para o aplicativo": o link passa a aparecer só em aparelho de toque, então o teste roda simulando `(pointer: coarse)`; as asserções sobre a chave e o `href` não mudam

## Swept

- validation: n/a - a tela não recebe entrada nova
- failure modes: C1 (a chave digitável continua como alternativa ao QR)
- idempotency: n/a - nada é gravado
- authorization: n/a - a etapa já exige o cookie de etapa (cookie de etapa do `console-da-plataforma`)
- concurrency: n/a - tela de uma pessoa
- data lifecycle: n/a - nada é gravado
- dependency failure: C2 (sem serviço externo de QR)
- state transitions: C4
- observability: n/a - sem evento novo

## Out of scope

- Mudar a API, o formato da `uri` ou o tempo da etapa.

## Handoff

- Mechanism: one builder (cerca de 4 arquivos)
- Verificação enxuta: Verifier independente, perfil `standard`, uma rodada
