# Máscaras nos campos e ícone do app em JPEG

## Problem

Seis campos do `web/` pedem um formato fixo e aceitam texto solto. O CPF aparece em 5 formulários (Login, Esqueci a senha, Usuários, Equipe da plataforma, Criar instituição) com o placeholder `000.000.000-00…`, mas quem digita vê só os dígitos sem pontos. O telefone de atendimento da Aparência é texto livre. O campo "Duplicado de outro pedido" pede `ESC-000123` e deixa digitar qualquer coisa. O "Endereço" da instituição só falha depois do envio, porque a API aceita apenas `[a-z0-9-]{3,30}`. Quem paga é a equipe da instituição, que erra o formato e só descobre na recusa, e a coordenadora da Univag, que vê isso na apresentação.

Na Aparência, o seletor de arquivo já aceita `image/png,image/jpeg`, e a API aceita PNG e JPEG para Logo, Imagem do login e Foto do assistente. Só o **Ícone do app** recusa JPEG: a API responde `415 "O ícone deve ser PNG."` depois de o seletor ter deixado escolher o arquivo. Evidência: pedido do usuário em 19/11 e `EnviarImagemInstituicao.cs` (`icone ? [Arquivos.Png] : [Arquivos.Png, Arquivos.Jpeg]`). Não há medida de uso, porque a produção tem uma só instituição.

Depois disto, os seis campos formatam enquanto se digita, e o ícone do app aceita JPG ou PNG quadrado de pelo menos 512 px.

## Flow

Reusa `cpfFormatado` de `web/src/administracao/comum.tsx` como forma de exibição do CPF, o componente `Campo` e a recusa de imagens de `Arquivos.Ler`, sem criar outro formatador nem outro leitor de imagem.

Máscaras (só `web/`):

1. digitação ou colagem em `Campo` / `Input` (exists) -> `web/src/mascaras.ts` (new, door 1) - devolve o texto formatado
2. o estado da tela guarda o texto formatado; o corpo enviado à API leva só os dígitos do CPF (e o protocolo `ESC-NNNNNN` em maiúsculas, como hoje)
3. a API (exists) valida como já valida: `Cpf.Tentar`, `SlugValido`, `numeroOriginal`

Ícone em JPEG:

1. `POST /api/aparencia/imagens/icone` (exists) -> `EnviarImagemInstituicaoHandler` (exists) - passa a aceitar `[Png, Jpeg]` para `icone` e mede largura e altura em PNG (cabeçalho, como hoje) e em JPEG (marcador SOF)
2. `imagem_instituicao.Conteudo` (exists) - já guarda `image/png` ou `image/jpeg`
3. `GET /api/instituicoes/{slug}/imagens/icone` (exists) - já devolve o tipo gravado
4. `ManifestoInstituicaoHandler` (exists) - passa a ler o tipo gravado e a escrever o `type` do ícone com ele (door 2)
5. `VerInstituicaoHandler` (exists) - devolve `iconeTipo` com o tipo gravado do ícone
6. `web/src/aparencia.tsx` (exists) - o `<link rel="icon">` passa a ter o `type` de `iconeTipo`

## Impact

| Front | What changes |
| --- | --- |
| domain | novo termo: máscara - formatação do texto do campo enquanto se digita; o valor enviado à API não muda de formato |
| domain | termo existente: "ícone do app" aceitava só PNG, passa a aceitar PNG ou JPEG; quem ramifica nele: `EnviarImagemInstituicao`, `ManifestoInstituicao.Icones`, `aparencia.tsx` (`link[rel=icon][type=image/png]`), os textos de `abasDaAparencia.ts` |
| existing tests | os testes que digitam o CPF só em dígitos continuam válidos, porque a máscara aceita dígitos; os que afirmam `"O ícone deve ser PNG."` e o `type: "image/png"` do manifesto do ícone próprio passam às frases de AC 13, AC 14 e AC 16; os 4 que afirmam o corpo com CPF pontuado (`usuarios.test.tsx` 2, `implantacao.test.tsx` 2) passam a afirmar os 11 dígitos de AC 5; `AparenciaAdminTests.Tipo_de_imagem_recusado_415` afirmava JPEG recusado no ícone e passa a recusar GIF (AC 13) |
| retrato da API | `instituicao-publica.txt` e `sucessos.txt` ganham só `iconeTipo` (acrescentado no build) |
| stored data | nothing to migrate - nenhuma coluna muda, e ícones PNG já gravados continuam como estão |
| produção | `esclari.com.br` só ganha a mudança no próximo deploy, que exige "ok" explícito (AGENTS.md) |

## Relations

None - no stored-data shape change.

## Surface

Só rotas cuja assinatura muda.

| Route | In | Out | Status |
| --- | --- | --- | --- |
| `POST /api/aparencia/imagens/icone` | multipart `arquivo` (PNG ou JPEG) | `tipo`, `versao` | `200`, `400`, `413`, `415` |
| `GET /api/instituicoes/{slug}/manifest.webmanifest` | `slug` | `icons[].type` = tipo gravado do ícone | `200`, `404` |
| `GET /api/instituicoes/{slug}` (acrescentada no build: AC 17 precisa do tipo no web) | `slug` | campo novo `iconeTipo`: `image/png`, `image/jpeg` ou `null` sem ícone; os demais campos não mudam | `200`, `404` |

Nota do build: a rota do envio é `POST /api/admin/aparencia/imagens/icone` (o grupo `admin` faltou na linha acima).

## Landing

| One-way door | Literal shape | Alternative rejected |
| --- | --- | --- |
| 1. módulo `web/src/mascaras.ts` (padrão novo que as próximas telas vão copiar) | `export const cpfMascara = (t: string) => string`, `telefoneMascara`, `protocoloMascara`, `enderecoMascara`, e `soDigitos`; cada função é pura, recebe o texto bruto e devolve o formatado | biblioteca `react-imask` ou `react-input-mask`: dependência nova para quatro funções curtas, e o `AGENTS.md` exige justificar pacote novo |
| 2. `type` do ícone no manifesto vira o tipo gravado | `{"src":"/api/instituicoes/{slug}/imagens/icone?v=N","sizes":"512x512","type":"image/jpeg","purpose":"any"}` | converter o JPEG em PNG no servidor: exige biblioteca de imagem nova (ImageSharp ou SkiaSharp) e muda a licença e a imagem do contêiner |

- Nada mais nesta mudança é difícil de desfazer: o corpo enviado à API não muda de formato, e nenhuma tabela muda

## Criteria

### S1: CPF com máscara em todo formulário (P1)

Quem digita ou cola o CPF vê `000.000.000-00` formado a cada tecla.

**Acceptance Criteria**

1. WHEN the user types digits in a CPF field THEN the web SHALL show them grouped progressively as `000.000.000-00` (`046`, `046.4`, `046.473.3`, `046.473.331-67`)
2. WHEN the user types a non-digit in a CPF field THEN the web SHALL ignore that character
3. WHEN the user pastes `046.473.331-67` or `04647333167` THEN the web SHALL show `046.473.331-67` in both cases
4. IF the pasted text has more than 11 digits THEN the web SHALL keep only the first 11
5. WHEN a CPF form is submitted THEN the web SHALL send the CPF as 11 digits with no dots or dash
6. WHERE the field is a CPF field the web SHALL set `inputMode="numeric"` and `maxLength` 14

**Independent test:** em `/univag`, digitar `04647333167` no Login e ler `046.473.331-67`.

### S2: Telefone, protocolo e endereço com máscara (P1)

Os três campos restantes impedem o formato errado antes do envio.

**Acceptance Criteria**

7. WHEN the user types digits in the Aparência "Telefone" field THEN the web SHALL show `(65) 3321-1234` for 10 digits and `(65) 99999-1234` for 11 digits
8. WHEN the user pastes a phone starting with `+55` or `55` that has more than 11 digits THEN the web SHALL drop the `55` and show the remaining digits masked
9. IF the phone has more than 11 digits after that THEN the web SHALL keep only the first 11
10. WHEN the user types in "Duplicado de outro pedido" THEN the web SHALL show `ESC-` followed by at most 6 digits, in upper case (`esc123456` becomes `ESC-123456`), and SHALL show an empty field when there is no digit
11. WHEN the user types in "Endereço" of Criar instituição THEN the web SHALL show only lower case letters, digits and `-`, without accents, with spaces turned into `-`, and at most 30 characters

**Independent test:** em Aparência, digitar `65999991234` e ler `(65) 99999-1234`.

### S3: Ícone do app em JPEG (P1)

O administrador envia o ícone como JPG ou PNG.

**Acceptance Criteria**

12. WHEN an administrator sends a JPEG square of at least 512 px and up to 1 MB as `icone` THEN the API SHALL answer `200` with `tipo` `icone` and the new `versao`
13. IF the `icone` file is neither PNG nor JPEG THEN the API SHALL answer `415` with `O ícone deve ser PNG ou JPEG.`
14. IF the `icone` file is not square or has a side below 512 px THEN the API SHALL answer `400` with `O ícone deve ser um PNG ou JPEG quadrado de pelo menos 512 px.`, for PNG and for JPEG
15. IF the `icone` file is above 1 MB THEN the API SHALL answer `413` with `A imagem passa de 1 MB.`
16. WHEN the institution has a JPEG icon THEN the manifest SHALL list the icon with `"type": "image/jpeg"`, and WHEN it has a PNG icon with `"type": "image/png"`
17. WHEN the institution has a JPEG icon THEN the web SHALL set `<link rel="icon">` with `type` `image/jpeg`
18. The web SHALL describe the icon as `PNG ou JPEG quadrado de pelo menos 512 px, até 1 MB.`

**Independent test:** em Aparência, "App instalado", enviar um JPEG 512×512 e ver a miniatura.

## Out of scope

| Excluded | Why |
| --- | --- |
| Máscara de matrícula e do CPF dentro do CSV | o CSV é arquivo e não campo de formulário; `Cpf.Tentar` já aceita pontos e traço |
| Máscara na busca "Nome, CPF ou e-mail" | o campo mistura três tipos de texto |
| Máscara de e-mail, URL do Moodle, prazo em horas e código de 2 fatores | e-mail e URL usam `type` nativo, `prazoHoras` já é numérico com mínimo e máximo, e o código de 6 dígitos já filtra os dígitos |
| Converter o JPEG do ícone em PNG | exige biblioteca de imagem nova (Landing, door 2) |
| Validar o DDD ou o número do telefone | o campo é texto livre na API, e o contato pode ser um 0800 |
| Aceitar WebP, SVG ou GIF em qualquer imagem | o pedido foi JPG e JPEG |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| Telefone 0800 ou fora de 10 e 11 dígitos | a máscara agrupa só os 10 e 11 dígitos; o valor já gravado aparece como está até o usuário editar | não apagar contato que funciona | n |
| Valor do telefone enviado à API | o texto mascarado, como `(65) 99999-1234` | o campo é texto livre, e o link `tel:` já tira os não dígitos (`aparencia.tsx:175`) | n |
| Dimensão do JPEG | lida do primeiro marcador SOF (`FFC0` a `FFCF`, exceto `FFC4`, `FFC8`, `FFCC`) | não exige biblioteca | n |
| Transparência do ícone | continua só uma recomendação no texto, sem checagem | já era assim para PNG | n |

**Open questions:** none - all resolved or logged above.

## Observable

| Surface | Decision | Landing |
| --- | --- | --- |
| screen Login, Esqueci a senha, Usuários, Equipe, Criar instituição | empty, loading, error | existing - os formulários não mudam; o erro do campo segue `Campo` |
| screen Aparência (Telefone, App instalado) | empty, loading, error | AC 13 a AC 15 para o ícone; Telefone existing |
| screen campos mascarados | unauthorised, destructive action confirms | n/a - nenhuma ação destrutiva e nenhuma regra de acesso muda |
| screen campos mascarados | density and ordering | n/a - o campo troca só o texto, sem mudar o arranjo |
| screen campos mascarados | celular 360×780 | AC 6 - `maxLength` 14 cabe sem rolagem lateral; `Campo` já é uma coluna |
| screen campos mascarados | celular deitado 844×390 | n/a - o formulário não muda de arranjo, só o texto do campo |
| screen campos mascarados | teclado aberto | AC 6 - `inputMode="numeric"` abre o teclado numérico; o telefone usa `inputMode="tel"` |
| screen campos mascarados | toque | n/a - nenhum controle novo, a altura do `Input` não muda |
| API `POST /api/aparencia/imagens/icone` | error shape and codes | AC 12 a AC 15 |
| API `POST /api/aparencia/imagens/icone` | who may call | existing - administrador da instituição, sem mudança |
| API `GET .../manifest.webmanifest` | response shape | AC 16 |
| API ícone | versioning, rate limits | n/a - a rota e o `?v=` versionado existem; nenhum limite novo |
| document dica do ícone | structure, tone, next step | AC 18 |

## Sources

- Pedido do usuário em 19/11/2026: "campos que deveriam ter máscaras e não têm" e "aceite jpg/jpeg também" na Aparência
- `Esclari.Api/Instituicoes/Aparencia/EnviarImagemInstituicao.cs` e `web/src/administracao/aparencia/abasDaAparencia.ts` - onde o ícone é só PNG
- `Esclari.Api/Plataforma/Criacao.cs:16` (`^[a-z0-9-]{3,30}$`) e `Esclari.Api/Dados/Usuarios/Cpf.cs` - os formatos que a API já exige
