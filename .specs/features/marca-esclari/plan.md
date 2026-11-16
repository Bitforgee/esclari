# Logo e marca do Esclari

## Problem

O Esclari não tem logo. O que existe é um `E` branco em quadrado azul gerado por quatro retângulos em `web/scripts/gerar-icones.mjs`; não há arquivo vetorial, palavra-marca, favicon SVG, `apple-touch-icon` de 180 px nem regra de uso. O `ROADMAP.md` deixou isso aberto ("Logo e cores do Esclari: o grupo fornece ou aprova a proposta") e o nome mudou de One para Esclari em 15/11 (`renomear-esclari`, AD-058) sem trocar o ícone.

Quem paga: a equipe, que não tem um arquivo para pôr em apresentação, e-mail, GitHub ou na tela inicial de `/`; e a instituição, que vê no produto um `E` genérico quando não configurou o próprio ícone. A marca aparece pouco para a aluna (a interface é da instituição, AD-032), então ela precisa ser pequena, correta e fácil de manter, não um sistema grande.

Depois desta feature: o Esclari tem um símbolo (balão de conversa cortado em forma de E), uma palavra-marca, um logo horizontal, todos em SVG de uma cor, ícones de app gerados a partir do símbolo (inclusive o de iOS), favicon em SVG, o logo na raiz `/`, e um guia de marca de uma página com medidas.

## Flow

Reaproveita `web/scripts/gerar-icones.mjs` (que já gera os PNG a partir de SVG no Chromium do Playwright), o manifesto por instituição e o fallback de ícone que já existem; nada de back-end muda.

1. `web/public/marca/` (new, door 1) - guarda `simbolo.svg`, `logo-horizontal.svg` e `favicon.svg`, desenhados uma vez e commitados.
2. `web/scripts/gerar-icones.mjs` (exists) - lê `simbolo.svg` e regrava `web/public/icons/icon-192.png`, `icon-512.png`, `icon-maskable-512.png` e `apple-touch-icon.png` (door 2).
3. `web/index.html` (exists) - declara `favicon.svg`, o PNG de reserva e o `apple-touch-icon`; `aparencia.tsx` (exists) tira o `favicon.svg` da aba quando a instituição tem ícone e o devolve quando não tem.
4. `Acesso.tsx` `Raiz` (exists) - mostra o logo horizontal dentro do `h1`, o claro ou o escuro conforme `useTema` (`aparencia.tsx`, exists).
5. `ManifestoInstituicao` (exists) e `PushEnviador` (exists) - continuam apontando para `/icons/icon-*.png` sem mudança de código; recebem os arquivos novos nos mesmos caminhos.
6. `.specs/features/marca-esclari/guia-de-marca.md` (new, documento) - fixa nome, símbolo, logo, cor, área de respeito, tamanhos mínimos, usos proibidos, tipografia e voz.

## Impact

| Front | What changes |
| --- | --- |
| domain | new term: `símbolo` (o balão em E), `palavra-marca` (o texto "Esclari" em curvas) e `logo horizontal` (símbolo + palavra-marca); "ícone do app" continua a imagem PNG quadrada de cada instituição (AD-032) e o do Esclari é só o fallback dela |
| domain | quem ramifica sobre o ícone padrão hoje: `ManifestoInstituicao.Icones` (sem versão de ícone devolve os 3 caminhos fixos), `PayloadPush.IconePadrao` (`/icons/icon-192.png`), `Sigla`/`aplicar` em `aparencia.tsx` (sem ícone da instituição usa as iniciais e remove os `link rel=icon`), `index.html`; nenhum muda de contrato |
| stored data | nada |
| assets | os três PNG existentes mudam de conteúdo e mantêm nome e tamanho; entra `apple-touch-icon.png` 180×180; precache do PWA passa a incluir os SVG de `marca/` |
| testes | testes que fixam tamanho ou nome dos ícones (`ManifestoTests`, `PushAgendadorTests`, `guidelinesAcesso.test.tsx:45-47`) continuam valendo; a lista fechada de ajustes vem em `checks.md` |
| cor | `#224fa7` continua nos três lugares onde já está; unificar não faz parte desta feature |

## Relations

None - no stored-data shape change

## Surface

Só o que a feature adiciona ou muda de assinatura. O comando `node scripts/gerar-icones.mjs` (sem flags; exit 0, ou exit 1 se faltar `simbolo.svg` ou `theme_color`) não é rota e vive nos AC 4 e 5. Os caminhos já consumidos (`/icons/icon-192.png`, `/icons/icon-512.png`, `/icons/icon-maskable-512.png`) não mudam e são cobertos por critério.

| Route | In | Out | Status |
| --- | --- | --- | --- |
| `GET /marca/simbolo.svg` | nenhuma | SVG, `viewBox="0 0 64 64"`, um `<path>` | `200` |
| `GET /marca/logo-horizontal.svg` | nenhuma | SVG com símbolo e palavra-marca em curvas | `200` |
| `GET /marca/favicon.svg` | nenhuma | SVG com placa azul e símbolo branco | `200` |
| `GET /icons/apple-touch-icon.png` | nenhuma | PNG 180×180 opaco | `200` |

## Landing

| One-way door | Literal shape | Alternative rejected |
| --- | --- | --- |
| 1. Símbolo (superseded pela porta 5) | Arquivo `web/public/marca/simbolo.svg`: `viewBox="0 0 64 64"`, exatamente um `<path fill="currentColor" fill-rule="evenodd">`, sem `mask`, `filter`, `gradient`, `<text>` nem cor fixa. Geometria: o balão da direção A de `direcoes.html` (bolha 52×46 com raio 12, cauda embaixo à esquerda, dois cortes arredondados de 8 de altura abertos à direita), com os cortes já subtraídos do contorno | `mask` do SVG (como em `direcoes.html`): some em e-mail, em Satori e em editores, e o arquivo não é um desenho que se copia; direção B (duas cores): não vira uma cor só para uso em preto e branco; direção C (E de traço): não diz atendimento |
| 2. Nomes dos ícones | Os caminhos `/icons/icon-192.png`, `/icons/icon-512.png`, `/icons/icon-maskable-512.png` ficam como estão; entra `/icons/apple-touch-icon.png` (180×180, opaco) | Renomear para `esclari-*`: PWAs já instaladas e o `IconePadrao` do push apontam para os nomes atuais e o manifesto por instituição os devolve |
| 3. Palavra-marca em curvas | `logo-horizontal.svg` contém a palavra "Esclari" como contornos (Sora 600, minúscula; era Public Sans 700 até a porta 5), sem `<text>` nem `font-family` | Texto vivo com a fonte do site: o arquivo muda de aparência onde a fonte não existe (e-mail, GitHub, apresentação) |
| 4. Cor da marca (superseded pela porta 5) | `#224fa7` (azul atual, `oklch(0.45 0.15 262)` aproximado) como a única cor do Esclari; o símbolo é branco sobre ela ou dessa cor sobre claro | Nova paleta com cor de apoio (o amarelo da direção B): a instituição já troca a cor do produto (AD-032) e uma segunda cor fixa do Esclari competiria com a dela |

| 5. Símbolo v2 (supersede as portas 1 e 4, decisão do usuário em 17/11 depois de achar a marca fraca, `direcoes-2.png`, `direcoes-3.png`, `proposta-v4.png`) | `simbolo.svg`: `viewBox="0 0 64 64"`, um `<path fill="currentColor">` (o E: coluna e dois braços, cantos externos de raio 6) e um `<rect fill="#ffd43b" rx="6">` (o braço do meio, a cor marca-texto), sem `mask`, `filter`, `gradient`, `<text>`. Cor da marca `#2438d6` (Ultramar); tinta `#0e1330`; marca-texto `#ffd43b`; papel `#eef0fb`. Palavra-marca em Sora SemiBold, minúscula, com o pingo do "i" também marca-texto, em curvas. Duas versões do logo: `logo-horizontal.svg` (letras `#0e1330`, E `#2438d6`) e `logo-horizontal-escuro.svg` (letras e E `#ffffff`), as duas com o marca-texto | Manter `#224fa7` e o balão: genérico, o balão de chat é o símbolo mais comum do mercado e o azul não se distingue do padrão de IA; E de uma só cor (direções 1 a 3): perde a ideia do trecho citado destacado; inverter cores com CSS no tema escuro (`dark:invert`): o amarelo vira azul, então são dois arquivos |
- Nothing else in this change is hard to reverse

## Criteria

> Leitura depois da porta 5 (17/11): onde os critérios 1, 3, 4, 7, 16 e 17 falam do balão, de `#224fa7` ou de "um único path", valem o E de duas peças, `#2438d6` e a regra da porta 5; os números dos critérios não mudam. O logo escuro `GET /marca/logo-horizontal-escuro.svg` (200, SVG) entra na Surface junto dos outros arquivos de `marca/`.

### S1: Arquivos da marca (P1)

O Esclari passa a ter símbolo, logo horizontal e favicon em SVG, e os PNG de app gerados deles.

**Acceptance Criteria**

1. The system SHALL publish `/marca/simbolo.svg` with `viewBox="0 0 64 64"`, exactly one `<path>`, `fill="currentColor"` and none of `mask`, `filter`, `gradient`, `<text>` or a hex colour.
2. The system SHALL publish `/marca/logo-horizontal.svg` containing the symbol and the word "Esclari" as paths, with no `<text>` element and no `font-family`.
3. The system SHALL publish `/marca/favicon.svg` with a `#224fa7` tile and the symbol in white, and `web/index.html` SHALL declare it with `<link rel="icon" type="image/svg+xml">` before the PNG fallback.
4. WHEN `node scripts/gerar-icones.mjs` runs THEN the system SHALL rewrite `icon-192.png` (192×192), `icon-512.png` (512×512), `icon-maskable-512.png` (512×512) and `apple-touch-icon.png` (180×180) in `web/public/icons/` and exit with code 0.
5. IF `web/public/marca/simbolo.svg` does not exist THEN `gerar-icones.mjs` SHALL print which file is missing and exit with code 1 without changing any PNG.
6. The system SHALL render `icon-maskable-512.png` and `apple-touch-icon.png` with alpha 255 on every pixel.
7. The system SHALL keep every non-tile pixel of `icon-maskable-512.png` inside the circle of radius 40% of the width (204.8 px) centred on the image.
8. WHEN `apple-touch-icon.png` is requested THEN the system SHALL answer 200 with `image/png`, and `web/index.html` SHALL declare it with `<link rel="apple-touch-icon" href="/icons/apple-touch-icon.png">`.
9. The system SHALL keep the symbol recognisable at 16 px: in a 16×16 rendering of `favicon.svg`, the white pixels of the symbol (luminance above 200) are at least 15% and at most 45% of the tile.

**Independent test:** abrir `/marca/logo-horizontal.svg` no navegador e comparar com `direcoes.png`; rodar `npm --prefix web run icones` e ver os 4 PNG.

### S2: A marca nos lugares sem instituição (P1)

Onde só o Esclari aparece, aparece o logo; onde a instituição definiu a sua identidade, nada muda.

**Acceptance Criteria**

10. WHEN the root `/` loads THEN the system SHALL show `/marca/logo-horizontal.svg` as an `img` with `alt="Esclari"` inside the page's `h1`, keeping the accessible name of the `h1` equal to "Esclari".
11. WHERE an institution has an app icon or a logo, the system SHALL keep showing the institution's image everywhere it shows it today (login, sidebar, manifest, push, e-mail) and SHALL NOT show the Esclari symbol there.
12. WHERE an institution has no app icon, `GET /api/instituicoes/{slug}/manifest.webmanifest` SHALL keep returning `/icons/icon-192.png` (192x192, `any`), `/icons/icon-512.png` (512x512, `any`) and `/icons/icon-maskable-512.png` (512x512, `maskable`).
13. WHERE the push has no institution icon, `PayloadPush.IconePadrao` SHALL stay `/icons/icon-192.png`.
14. The system SHALL precache the three files under `/marca/` in the service worker so `/` shows the logo offline.

**Independent test:** abrir `/` e ver o logo; abrir `/univag` e ver que continua o da Univag; `GET /api/instituicoes/univag/manifest.webmanifest`.

### S3: Guia de marca (P2)

Uma página que diz como usar a marca, com números que o desenho respeita.

**Acceptance Criteria**

15. The system SHALL include `guia-de-marca.md` with the headings "Nome", "Símbolo", "Logo horizontal", "Cor", "Área de respeito", "Tamanho mínimo", "Usos proibidos", "Tipografia" and "Voz".
16. The guide SHALL state the clear space as half the symbol height, the minimum size as 16 px for the symbol and 96 px wide for the horizontal logo, and the colour as `#224fa7`.
17. The guide SHALL list at least five prohibited uses, each with the reason (for example: recolour outside the brand colour or white, stretch, add shadow or gradient, place on a busy photo, rewrite the name in capitals).
18. The guide SHALL write the name as "Esclari" (one capital, no article in headings) and give the protocol format `ESC-NNNNNN` and the domain `esclari.com.br` as the only other spellings.

**Independent test:** ler o guia numa página e montar o logo só com ele.

### S4: Celular e toque (P1)

Só a raiz `/` muda de tela.

**Acceptance Criteria**

19. WHILE the viewport is 360×780 the root `/` SHALL show the horizontal logo at 40 px high with no horizontal scroll.
20. WHILE the viewport is 844×390 the root `/` SHALL show the logo and both paragraphs without vertical scroll beyond 390 px.

**Independent test:** capturar `/` em 360×780 e 844×390 com o Playwright.

## Out of scope

| Excluded | Why |
| --- | --- |
| Registro da marca no INPI (classes 9, 41, 42) e busca de colidência do símbolo | Decisão jurídica do dono; o plano só avisa do risco (`pesquisa.md`) |
| Rodapé "Atendimento por Esclari" nas telas da instituição | Depende da pergunta 2 abaixo; sem resposta, nada aparece |
| Imagem de compartilhamento (Open Graph) e site de apresentação | Não há página pública do produto; fica para quando houver |
| Logo animado e vídeo de abertura | Sem uso no produto |
| Nova paleta, cor de apoio ou modo escuro do ícone | A cor da instituição já substitui a do produto (AD-032) |
| Trocar o `E`/iniciais que a instituição sem ícone vê no `Sigla` | É a identidade da instituição, não do Esclari |
| Unificar `#224fa7` nos três lugares do código | Refator reversível, sem relação com o desenho |
| Avatar do assistente e nome do assistente | Pertencem à instituição (AD-032) |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| Direção do símbolo | A, balão cortado em E | Única das três que diz "conversa" e "E" numa forma e funciona em uma cor (`pesquisa.md`); o usuário delegou ("vá pelo que for mais recomendado") | y - user delegated |
| Fonte da palavra-marca | Public Sans 700 em curvas | Já está no produto (AD-034); evita pagar ou licenciar fonte nova | n |
| Cor | `#224fa7` única | Já é a cor padrão em três lugares e a instituição a substitui | n |
| Esclari visível para o aluno nas telas da instituição (rodapé "Atendimento por Esclari") | Não aparece | A interface é da instituição (AD-032); o usuário delegou a decisão e o recomendado é não | y - user delegated |
| Slogan | Nenhum; vale a frase já usada na raiz ("Dúvidas, solicitações e respostas da sua instituição em um só lugar.") | Inventar slogan é decisão de marca do dono; o texto atual já está aprovado | n |
| Placa do ícone | Quadrado arredondado azul com símbolo branco, como hoje | Mantém o reconhecimento de quem já instalou o PWA | n |
| Formato dos arquivos | SVG desenhado à mão e commitado; PNG gerado pelo script existente | Sem nova dependência; o script já roda no Chromium do Playwright | n |

**Open questions:** none - all resolved or logged above.

## Observable

Worksheet, não a revisão. Cada linha cai em critério, em algo que já existe, ou em `n/a` com motivo.

| Surface | Decision | Landing |
| --- | --- | --- |
| screen raiz `/` | empty state | n/a - tela estática, sem dados |
| screen raiz `/` | loading state | n/a - não chama a API |
| screen raiz `/` | error state | n/a - sem chamada que falhe; se o SVG não carregar vale o `alt="Esclari"` (AC 10) |
| screen raiz `/` | offline | AC 14 |
| screen raiz `/` | celular 360×780 | AC 19 |
| screen raiz `/` | celular deitado 844×390 | AC 20 |
| screen raiz `/` | teclado aberto | n/a - não há campo de texto na tela |
| screen raiz `/` | toque | n/a - nenhum controle; o logo não é link |
| screen raiz `/` | acessibilidade do logo | AC 10 |
| screen raiz `/` | destructive action confirms | n/a - sem ação |
| screens com identidade da instituição (login, sidebar, assistente) | marca da instituição prevalece | AC 11 |
| assets `GET /marca/*`, `GET /icons/*` | formato, status e cache | AC 1, AC 2, AC 3, AC 8; cache existing - o service worker faz precache (AC 14) |
| assets `GET /marca/*`, `GET /icons/*` | quem pode chamar, versionamento, limite de taxa | n/a - arquivos estáticos públicos, sem sessão; o nome do arquivo é o contrato (door 2) |
| comando `gerar-icones.mjs` | saída, flags e código de saída | AC 4, AC 5; sem flags (n/a - lê só os arquivos do repositório) |
| comando `gerar-icones.mjs` | falha no meio da escrita | AC 5 cobre a entrada ausente; escrita parcial n/a - o script grava depois de renderizar os 4 e usa `renameSync` já existente |
| documento `guia-de-marca.md` | estrutura, tom, profundidade e próximo passo do leitor | AC 15, AC 16, AC 17, AC 18 |
| manifesto e push | caminhos dos ícones padrão | AC 12, AC 13 |

## Sources

- `ROADMAP.md` - deixou "Logo e cores do Esclari" em aberto e é a origem do pedido; `AGENTS.md` fixa o cenário mobile-first e a regra de que decisão de UI nova leva referência pesquisada.
- `.specs/features/marca-esclari/pesquisa.md` e `direcoes.png` - referências externas (web.dev, Apple, favicon, Zendesk, Notion) e as três direções vistas.
- AD-008, AD-032, AD-034, AD-058 em `.specs/STATE.md` - multi-instituição, aparência da instituição, tipografia e nome do produto.


