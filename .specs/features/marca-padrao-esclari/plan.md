# Marca padrão do Esclari em toda instituição nova

## Problem

Uma instituição que ainda não enviou logo, ícone, cor nem foto do assistente aparece sem identidade: um selo com as iniciais do nome no cabeçalho e na tela de entrada, o avatar do assistente sem imagem, o e-mail com o nome em texto sobre um cabeçalho branco, e o `manifest.webmanifest` com `theme_color` `#C83A23`, o vermelho do traço da marca, que não é a cor da interface (Tinta e Papel, AD-063) e destoa do manifest estático do app, `#f7f5ee`. A identidade do Esclari (símbolo "Fio de procedência", logo horizontal em duas versões, Tinta `#0e1330`, Papel `#f7f5ee`, ícones do app) existe pronta em `web/public/marca/` e `web/public/icons/`, mas o AD-032 a limitou à raiz `/` e à reserva do ícone do app. Quem paga é o aluno de uma instituição recém-criada, que vê um produto sem rosto, e a equipe do Esclari, que entrega um cliente "em branco" até a gestão dele enviar os arquivos. Evidência no código: `Sigla` em `web/src/aparencia.tsx`, `Identidade` em `web/src/acesso/Acesso.tsx`, `CabecalhoDaBarra` em `web/src/Autenticado.tsx`, `fotoDoAssistente` em `web/src/assistente/Assistente.tsx`, `ManifestoInstituicao.CorPadrao` e `LayoutEmail.Cabecalho`. O pedido é do dono, em 18/11/2026. A fonte não traz números.

Depois desta feature, toda instituição nasce com a marca do Esclari em todos esses lugares, e a gestão troca cada parte quando quiser (logo, ícone, cor, foto do assistente) pela tela Aparência que já existe. Cada parte volta ao padrão sozinha quando o campo é limpo, e uma melhoria futura da marca do Esclari chega a quem nunca personalizou.

## Flow

Reusa `GET /api/instituicoes/{slug}` e a tela Aparência (AD-032), o `normalizar` e o `Sigla` de `web/src/aparencia.tsx`, a rota de manifest por instituição, o `EnviadorSmtp` com `cid:logo`, o `LayoutEmail` e as imagens de marca já versionadas em `web/public/`.

1. `GET /api/instituicoes/{slug}` (exists) - devolve só o que a instituição enviou, com `null` onde não enviou (door 1)
2. `web/src/aparencia.tsx` (exists) - resolve cada parte como `valor da instituição ?? marca do Esclari`, em um lugar só
3. telas `Acesso`, `Autenticado`, `Assistente` e `Aparencia` (exist) - usam o resultado e deixam de montar o selo de iniciais
4. `ManifestoInstituicao` (exists) - `theme_color` sem cor própria passa a `#f7f5ee`
5. `EnviadorSmtp` (exists) - sem logo própria, anexa o PNG da marca do Esclari como `cid:logo`, lido de um recurso do projeto (new asset, door 2)
6. `LayoutEmail` (exists) - cabeçalho com a logo (a da instituição ou a do Esclari) e o nome da instituição em texto

## Impact

| Front | What changes |
| --- | --- |
| domain | termo existente: "marca da instituição" era só o que ela enviou; passa a ser `o que ela enviou ?? marca do Esclari`. Quem ramifica nele hoje: `Sigla`, `Identidade`, `CabecalhoDaBarra`, `fotoDoAssistente`, `iconeDe`, `ManifestoInstituicao.Icones`, `LayoutEmail.Cabecalho` e o aviso push (`push-com-marca`, que já usa o ícone do Esclari como reserva) |
| domain | termo existente: o AD-032 diz que a marca do Esclari aparece só na raiz e como reserva do ícone; este plano supersede essa frase (AD novo) e mantém o resto do AD-032 |
| API | `GET /api/instituicoes/{slug}` não muda; `GET /api/instituicoes/{slug}/manifest.webmanifest` troca o valor de `theme_color` sem cor própria |
| stored data | nenhuma migration: a coluna vazia (`NULL`) passa a significar "usa a marca do Esclari"; instituições existentes sem imagem passam a mostrar a marca sozinhas |
| tests | testes que afirmam o selo de iniciais como reserva (`aparencia`, `Autenticado`, `Acesso`), `#C83A23` no manifest e o nome em texto no cabeçalho do e-mail mudam por consequência direta, numa lista explícita em `checks.md` que o Verifier confere |
| docs | AD novo no `STATE.md`; `GLOSSARIO.md`; `guia-de-marca.md` (a seção "Nome" e a frase de uso) |

## Relations

None - no stored-data shape change.

## Surface

None - nothing consumed outside. O manifest e a rota de instituição mantêm a assinatura; só o valor padrão de `theme_color` muda, e isso é critério.

## Landing

| One-way door | Literal shape | Alternative rejected |
| --- | --- | --- |
| 1. onde mora o padrão | a instituição guarda só o que sobrescreveu: `NULL` em `logo`, `icone`, `login`, `assistente`, `cor` significa "usa o Esclari", e nenhuma migration copia a marca para as linhas | gravar a marca do Esclari em cada instituição na criação: uma mudança futura da marca não chegaria a quem não personalizou, e "usar a marca do Esclari" teria de saber qual era o padrão naquele dia |
| 2. logo do e-mail | recurso do projeto `Esclari.Api/Marca/logo-horizontal-email.png`, PNG com o dobro do tamanho exibido (348 por 80 px, exibido em 174 por 40), anexado como `cid:logo`, o mesmo identificador de hoje | URL pública da imagem: depende do endereço público (pergunta 5), muitos clientes bloqueiam imagem remota por padrão e o teste local com o Mailpit não a alcança; SVG: Gmail e Outlook não o desenham |

- Nada mais nesta mudança é difícil de desfazer. Medidas, textos de apoio e a ordem na tela mudam por refatoração.

## Criteria

### S1: Telas da instituição (P1)

**Acceptance Criteria**

1. WHEN a instituição não tem ícone THEN the system SHALL mostrar o símbolo do Esclari, `/marca/favicon.svg` (o mesmo desenho do ícone do app), no lugar do selo de iniciais no cabeçalho da barra lateral e no cabeçalho do celular, ao lado do nome da instituição, com `alt` vazio
2. WHEN a instituição tem ícone THEN the system SHALL mostrar o ícone dela, como hoje
3. WHEN a instituição não tem logo THEN the system SHALL mostrar na tela de entrada a logo horizontal do Esclari, `/marca/logo-horizontal.svg` no tema claro e `/marca/logo-horizontal-escuro.svg` nos temas Escuro e Preto, com `alt` "Esclari", e o nome da instituição como título da página abaixo
4. WHEN a instituição tem logo THEN the system SHALL mostrar a logo dela na tela de entrada e no cabeçalho da barra lateral, como hoje
5. WHILE o tema é Escuro ou Preto, WHEN a instituição tem logo própria THEN the system SHALL mostrá-la sobre uma placa branca de cantos de 6 px e 4 px de respiro, para a logo não sumir sobre o fundo escuro
6. WHEN o assistente não tem foto THEN the system SHALL mostrar o símbolo `/marca/favicon.svg` como avatar dele, no cabeçalho da conversa e na pré-visualização da Aparência
7. WHERE a instituição tem cor própria e não tem logo, the system SHALL mostrar a logo do Esclari com a cor própria só nos elementos de ação (botão, link, foco), nunca no símbolo ou na logo
8. The system SHALL manter o selo de iniciais só para o avatar da pessoa logada, nunca para a instituição

### S2: Aplicativo instalado (P1)

**Acceptance Criteria**

9. WHERE a instituição não tem cor própria, the system SHALL devolver `theme_color` igual a `#f7f5ee` no manifest dela; WHERE tem, SHALL devolver a cor dela
10. WHERE a instituição não tem ícone, the system SHALL devolver no manifest os ícones do Esclari (`/icons/icon-192.png`, `/icons/icon-512.png` e `/icons/icon-maskable-512.png`) e, WHERE tem, o ícone dela
11. WHEN a gestão troca ou limpa a logo, o ícone ou a cor THEN the system SHALL manter o `id`, o `scope` e o endereço do manifest como estão
12. WHERE a instituição não tem `nomeCurto`, the system SHALL usar o `nome` da instituição como `name` e `short_name` do manifest, como hoje

### S3: E-mail (P1)

**Acceptance Criteria**

13. WHEN a instituição não tem logo, ou o armazenamento do logo está fora, THEN the system SHALL anexar o PNG do Esclari como `cid:logo` e mostrá-lo no cabeçalho com `alt` "Esclari", `max-height:40px`, ao lado do nome da instituição em texto de 20 px em negrito
14. WHEN a instituição tem logo e ela é lida THEN the system SHALL anexar a logo dela como `cid:logo` e mostrar só ela, com o nome em `alt`, como hoje
15. The system SHALL entregar o PNG do Esclari com 348 por 80 px, `Content-Type` `image/png` e sem canal alfa parcial que dependa de fundo escuro, sobre o cabeçalho branco do e-mail
16. IF o recurso do PNG do Esclari falta no projeto THEN the system SHALL falhar o build, nunca enviar e-mail sem imagem em silêncio

### S4: Tela Aparência (P1)

**Acceptance Criteria**

17. WHEN a gestão abre a tela Aparência de uma instituição sem imagens THEN the system SHALL mostrar na pré-visualização a marca do Esclari e, ao lado da logo, do ícone e da foto do assistente, a frase "Marca do Esclari (padrão)" e, ao lado da imagem do login, "Imagem padrão do Esclari"
18. WHEN a gestão envia uma imagem THEN the system SHALL mostrar a imagem nova na pré-visualização e trocar a frase por "Enviada por você", sem salvar até ela confirmar
19. WHEN a gestão aperta "Usar a marca do Esclari" ao lado de uma imagem (o botão que hoje se chama "Remover"), ou "Usar a cor padrão" ao lado da cor, e confirma THEN the system SHALL remover a imagem ou limpar a cor e mostrar o padrão do Esclari na pré-visualização
20. IF a gestão envia um ícone que não é PNG quadrado de 512 px ou mais, ou uma cor com contraste abaixo de 4,5 para 1 contra o branco, THEN the system SHALL recusar como hoje, sem mudar a marca atual

## Out of scope

| Excluded | Why |
| --- | --- |
| Remover a atribuição "Esclari" (rodapé do e-mail e o texto sob o nome na barra lateral) | continua do produto; trocar isso é decisão comercial |
| Enviar uma versão escura da logo da instituição | a placa branca resolve sem pedir outro arquivo; a pesquisa registra a variante como alternativa |
| Redesenhar a marca, a paleta ou a tipografia do Esclari | a marca "Fio de procedência" e a paleta estão aprovadas (AD-062 a AD-065) |
| Trocar fonte, ilustrações ou textos de apoio por instituição | fora do que a tela Aparência já faz |
| Aplicar a marca nova a ícones de apps já instalados no celular | quem controla é o sistema do aparelho; o aviso fica no texto da tela |
| Marca do Esclari no cartão de aviso push | o aviso push já usa o ícone do Esclari como reserva (`push-com-marca`) |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| A marca do Esclari é o padrão em todo lugar onde falta marca própria | sim, em tela, e-mail e manifest | pedido do dono em 18/11 | y |
| A gestão pode trocar cada parte e voltar ao padrão | sim, pela tela Aparência | pedido do dono em 18/11 | y |
| Cabeçalho do e-mail sem logo própria | logo do Esclari mais o nome da instituição em texto | quem recebe precisa saber de qual instituição vem; o rodapé já diz "para <instituição>" | n |
| Logo própria no tema escuro | placa branca | pesquisa: logo isenta de contraste só quando o manual de marca exige, e uma placa neutra evita pedir variante | n |
| A atribuição do Esclari fica | rodapé do e-mail e linha "Esclari" sob o nome na barra | práticas de Zendesk e Intercom tratam a remoção como recurso à parte | n |
| `theme_color` sem cor própria | `#f7f5ee` | mesmo valor do manifest estático do app (`vite.config.ts`) | n |
| PNG do e-mail | 348 por 80 px, gerado a partir de `logo-horizontal.svg` | o dobro do tamanho exibido, como recomenda a pesquisa | n |

**Open questions:** none - all resolved or logged above.

## Observable

| Surface | Decision | Landing |
| --- | --- | --- |
| screen "Entrada" | empty state (sem imagem nenhuma) | AC 3 |
| screen "Entrada" | loading state | existing - o esqueleto da tela de entrada |
| screen "Entrada" | error state | existing - instituição inexistente mostra "não encontrada" |
| screen "Barra lateral e cabeçalho" | empty state | AC 1, AC 6 |
| screen "Barra lateral e cabeçalho" | density and ordering | AC 1 (símbolo, depois nome) |
| screen "Aparência" | empty state | AC 17 |
| screen "Aparência" | loading, error state | existing - estados da tela de hoje |
| screen "Aparência" | destructive action confirms | AC 19 |
| screen "Aparência" | unauthorised | existing - só o administrador da instituição |
| API manifest | error shape and codes | existing - `404` para slug inexistente |
| API manifest | versioning, rate limits | n/a - rota anônima de leitura, sem versão |
| documento e-mail | structure, tone, next step | AC 13, AC 14 |
| celular 360×780 | símbolo de 24 px no cabeçalho, logo da entrada com `max-w-full` | AC 1, AC 3 |
| celular deitado 844×390 | n/a - a tela de entrada rola dentro da página | n/a - sem layout dependente da altura |
| teclado aberto | n/a - esta feature não altera campos de texto | n/a - sem campo novo |
| toque | alvos de "Usar a marca do Esclari" de 44 px ou mais | AC 19 |

## Sources

- `.specs/features/marca-esclari/guia-de-marca.md` e AD-062 a AD-065 - a marca "Fio de procedência", Tinta, Papel e os arquivos em `web/public/marca/` e `web/public/icons/`
- AD-032 (`aparencia-instituicao`) - o que a gestão já troca e a frase que este plano supersede
- Pedido do dono, 18/11/2026: o padrão traz a logo e a identidade visual do Esclari, e a gestão muda depois se quiser
- [Zendesk: remover a marca "Powered by"](https://support.zendesk.com/hc/en-us/articles/4408846619546-How-do-I-remove-the-Zendesk-logo) e [Intercom: marca do Messenger](https://www.intercom.com/help/en/articles/6612589-set-up-and-customize-the-messenger) - a marca da plataforma é o padrão e removê-la é recurso à parte
- [Atlassian Statuspage: personalização](https://atlassian.com/software/statuspage/features/customization) - logo e cores trocáveis pelo cliente, o resto continua da plataforma
- [W3C WCAG 1.4.11: contraste de não texto](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html) - 3 para 1 em elementos de interface e a isenção limitada de logotipos
- [web.dev: ícones maskable](https://web.dev/maskable-icon/) e [web.dev: atualização de manifest](https://web.dev/articles/manifest-updates) - área segura de 40%, janela de até 24 h para o aparelho atualizar nome, ícone e cor, e o ícone no desktop que não atualiza
- [Chrome: `id` do manifest](https://developer.chrome.com/docs/capabilities/pwa-manifest-id) - `id` estável mantém o mesmo app instalado quando a marca muda
- [MailSlurp](https://www.mailslurp.com/blog/embedding-images-in-html/) e [Scribe](https://www.scribe-mail.com/en-US/handbook/email-signature-image-size) - imagem em e-mail anexada ou por URL, e o dobro do tamanho exibido com largura e altura fixas
