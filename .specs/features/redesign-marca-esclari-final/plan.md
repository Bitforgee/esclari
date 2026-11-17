# Fio de procedência — marca final do Esclari

## Problem

O dono escolheu **Fio de procedência** na prancha de conceitos de 17/11/2026. O conceito aprovado ainda não é um logo de produção: usa um traço SVG não expandido, um canvas de comparação, a palavra-marca Sora da identidade anterior e somente preto. Não existem corte pequeno, lockup vertical, versões monocromáticas finais, paleta aprovada, apresentação aplicada nem guia atualizado. O produto continua publicando o `E` azul e amarelo anterior.

Quem paga é a equipe que precisa usar a marca em PWA, e-mail, apresentação, GitHub e co-branding institucional. Publicar diretamente o conceito deixaria um master frágil para impressão e repetiria o problema tipográfico que o redesign deveria resolver.

Depois desta feature, o Fio de procedência vira um desenho preenchido e opticamente corrigido; “esclari” ganha palavra-marca própria; os ativos públicos, o favicon e os ícones PWA usam a identidade nova; os fallbacks de e-mail e manifesto recebem a cor Origem; e o kit documenta aplicações e limites. A identidade configurada pela instituição continua prevalecendo.

## Flow

Reaproveita os caminhos públicos, o gerador de ícones, a troca de tema da raiz, o manifesto por instituição e os fallbacks de e-mail e push existentes; troca o desenho e amplia o kit sem criar uma segunda cadeia de marca.

1. `web/public/marca/` (exists) recebe o símbolo final expandido, o corte pequeno, a palavra-marca e os lockups da door 1 e da door 2.
2. `web/scripts/gerar-icones.mjs` (exists) passa a ler qualquer `viewBox` quadrado do símbolo pequeno, aplica a cor Origem e regrava os quatro PNG nos nomes existentes.
3. `web/index.html` (exists), `web/src/aparencia.tsx` (exists) e `web/src/acesso/Acesso.tsx` (exists) mantêm a precedência do favicon institucional e a escolha claro/escuro da raiz; só os arquivos apontados mudam de desenho.
4. `ManifestoInstituicao` (exists) e `LayoutEmail` (exists) usam Origem quando a instituição não definiu cor; o valor configurado pela instituição continua intacto.
5. `CoresProntas` (exists) renomeia “Azul Esclari” para “Azul”, sem trocar `#224fa7`; os tokens de ação do produto não mudam.
6. `.specs/features/redesign-marca-esclari-final/` (new, door 3) guarda os masters de entrega, auditorias, prancha de apresentação, mockups e guia final; as referências externas não entram no kit.

## Impact

| Front | What changes |
| --- | --- |
| marca | Fio de procedência substitui o `E` destacado; palavra-marca deixa de ser Sora e passa a desenho próprio derivado de Public Sans OFL-1.1 |
| ativos públicos | entram corte pequeno, palavra-marca, vertical, preto e branco; horizontal claro, horizontal escuro, símbolo e favicon mantêm os caminhos atuais |
| PWA | os quatro PNG mantêm nome e dimensão, mas passam a usar o Fio branco sobre Origem `#C83A23`; o gerador deixa de assumir `viewBox 64` |
| e-mail e manifesto | fallback `#2438d6` vira `#C83A23`; a cor configurada pela instituição continua tendo precedência |
| interface | `#224fa7` continua cor de ação padrão; o preset “Azul Esclari” vira “Azul” |
| guia | `../marca-esclari/guia-de-marca.md` é substituído por um guia do Fio com paleta, medidas, co-branding e versões |
| testes | asserções que fixam o `E`, amarelo, quatro cores e nomes da paleta mudam para o novo contrato; nenhum teste é removido ou enfraquecido |
| stored data | nada |
| contratos da instituição | nenhum campo, rota ou formato muda |

## Relations

None - no stored-data shape change

## Surface

| Route | In | Out | Status |
| --- | --- | --- | --- |
| `GET /marca/simbolo.svg` | nenhuma | SVG `viewBox="0 0 256 256"`, símbolo master com `currentColor` | `200` |
| `GET /marca/simbolo-pequeno.svg` | nenhuma | SVG `viewBox="0 0 256 256"`, corte para 16–32 px com `currentColor` | `200` |
| `GET /marca/palavra-marca.svg` | nenhuma | SVG em curvas | `200` |
| `GET /marca/logo-horizontal.svg` | nenhuma | SVG Origem + Tinta para fundo claro | `200` |
| `GET /marca/logo-horizontal-escuro.svg` | nenhuma | SVG Origem clara + Papel para fundo Tinta | `200` |
| `GET /marca/logo-vertical.svg` | nenhuma | SVG Origem + Tinta | `200` |
| `GET /marca/logo-preto.svg` | nenhuma | SVG de uma cor preta | `200` |
| `GET /marca/logo-branco.svg` | nenhuma | SVG de uma cor branca | `200` |
| `GET /marca/favicon.svg` | nenhuma | placa Origem com corte pequeno branco | `200` |
| `GET /icons/apple-touch-icon.png` | nenhuma | PNG opaco 180×180 | `200` |
| `GET /icons/icon-192.png` | nenhuma | PNG 192×192 | `200` |
| `GET /icons/icon-512.png` | nenhuma | PNG 512×512 | `200` |
| `GET /icons/icon-maskable-512.png` | nenhuma | PNG opaco 512×512, conteúdo na zona segura | `200` |

## Landing

| One-way door | Literal shape | Alternative rejected |
| --- | --- | --- |
| 1. Símbolo Fio de procedência | master `viewBox="0 0 256 256"`; centro de construção `M36 224 C36 112 78 32 150 32 C198 32 228 68 228 116 C228 164 196 200 150 200 C112 200 86 174 86 140 C86 106 112 82 146 82 H224`, largura 28, pontas e encontros redondos, expandido para exatamente um `<path fill="currentColor">`, sem `stroke`, `mask`, `clipPath`, `filter`, gradient ou transform; corte pequeno mantém o percurso, aumenta a largura óptica e encurta as duas extremidades | publicar o `stroke`: impressoras, cortadores e importadores podem mudar sua largura; redesenhar como espiral genérica: apagaria a direção que o dono escolheu |
| 2. Palavra-marca | “esclari” minúsculo, desenhado a partir de Public Sans peso 650 (OFL-1.1), convertido em contornos; `e`, `a`, `r` e `i` recebem abertura/terminais derivados do Fio; pingo do `i` é monocromático; `r` e `i` mantêm vão visível a 24 px; nenhum `<text>` ou dependência de fonte | manter Sora da marca anterior: repete a palavra genérica e o encontro `r`/`i`; usar Public Sans sem alteração: pode ser reproduzido digitando e não cria propriedade |
| 3. Paleta e variantes | Origem `#C83A23`, Origem clara `#F16A4B`, Tinta `#15133A`, Papel `#F7F5EE`; claro = símbolo Origem + palavra Tinta; escuro = símbolo Origem clara + palavra Papel sobre Tinta; preto e branco = exatamente uma cor; favicon/PWA = símbolo pequeno branco sobre Origem; e-mail e manifesto sem cor institucional = Origem | manter ultramar + amarelo: conserva o argumento visual que o redesign rejeitou; aplicar vermelho como ação da interface: confundiria marca com erro/perigo e contrariaria o azul sóbrio de AD-027 |
| 4. Contrato dos arquivos | caminhos públicos listados em Surface; os quatro PNG preservam nomes e dimensões; SVG master e variantes ficam em `web/public/marca/`; os arquivos de apresentação e entrega ficam na pasta desta feature | renomear os PNG para incluir Esclari: quebra PWA instalada, manifesto e push; pôr mockups em `web/public`: publica arquivos sem uso pelo produto |
| 5. Refinamento após inspeção (supersede a door 2; decisão do dono em 17/11 depois de comparar o resultado com `../redesign-marca-esclari/conceitos.png`) | o Fio é expandido diretamente das curvas Bézier do centro da door 1 por offset vetorial de 14 unidades (18 no corte pequeno), com cap e join redondos, sem rasterização nem traçado automático; por correção óptica, a terminal interna master termina em `H208` e a pequena em `H192`, sem tocar a volta externa; a palavra-marca usa os contornos Sora SemiBold da direção C aprovada, em minúsculas, com o `i` afastado 2 unidades do `r` antes da escala e pingo monocromático; o lockup preserva peso, proporção e espaçamento visual da prancha de conceitos | contorno obtido por `potrace`/raster: introduziu oscilações visíveis nas curvas; palavra redesenhada em monoline: ficou materialmente diferente da opção escolhida pelo dono |

- Nothing else in this change is hard to reverse.

## Criteria

### S1: Símbolo e palavra-marca finais (P1)

**Acceptance Criteria**

1. The system SHALL publicar `simbolo.svg` com `viewBox="0 0 256 256"`, exatamente um `<path fill="currentColor">` que conserva o centro de construção da door 1 depois da expansão, e nenhum `stroke`, `mask`, `clipPath`, `filter`, gradient, transform, `<text>`, `<image>` ou cor hexadecimal.
2. The system SHALL publicar `simbolo-pequeno.svg` com `viewBox="0 0 256 256"`, exatamente um `<path fill="currentColor">`, peso óptico maior que o master e as duas extremidades encurtadas, sem detalhe menor que 8 unidades.
3. The system SHALL publicar `palavra-marca.svg` com “esclari” legível em minúsculas, letras em contornos, as alterações de `e`, `a`, `r` e `i` da door 2, vão visível entre `r` e `i` numa renderização de 24 px e nenhum `<text>`, `font-family`, `<image>` ou dependência externa.
4. The system SHALL dar aos 8 SVGs de marca nota mínima 95/100 no `svg_audit.py`, sem `FAIL` nem `WARN`, e SHALL manter símbolo e palavra-marca reconhecíveis em 16, 24, 32, 64 e 256 px, preto sobre branco e branco sobre preto.

> **Emenda 1, 17/11:** no AC 3, “as alterações da door 2” é supersedido pela door 5: a palavra-marca reproduz a tipografia e as proporções da direção C em `../redesign-marca-esclari/conceitos.png`, com apenas a correção do vão `r`/`i`. Nos AC 1, 2, 4, 6, 7, 9, 11, 15 e 19, o contorno do Fio é o offset Bézier vetorial da door 5, sem rasterização; os números dos critérios permanecem.

### S2: Cor e variantes (P1)

**Acceptance Criteria**

5. The system SHALL usar somente Origem `#C83A23`, Origem clara `#F16A4B`, Tinta `#15133A`, Papel `#F7F5EE`, preto, branco e `currentColor` nos SVGs, com cada papel da door 3 e sem gradient.
6. The system SHALL publicar os lockups horizontal claro, horizontal escuro e vertical, todos com símbolo, palavra-marca, alinhamento óptico e espaço entre eles derivado da largura expandida do Fio.
7. The system SHALL publicar `logo-preto.svg` e `logo-branco.svg` com exatamente uma cor cada, sem perder nenhum componente do lockup horizontal.
8. The system SHALL manter contraste medido de pelo menos 4,5:1 entre Origem e branco, Origem clara e Tinta, Tinta e branco, e Tinta e Papel.

### S3: Favicon, PWA e fallbacks (P1)

**Acceptance Criteria**

9. WHEN `npm --prefix web run icones` runs THEN the system SHALL gerar `icon-192.png` 192×192, `icon-512.png` 512×512, `icon-maskable-512.png` 512×512 e `apple-touch-icon.png` 180×180, com corte pequeno branco sobre Origem, e uma segunda execução SHALL produzir os mesmos hashes.
10. The system SHALL manter todo pixel do símbolo do `icon-maskable-512.png` dentro do círculo central de raio 204,8 px e SHALL manter alfa 255 em cada pixel do maskable e do apple-touch-icon.
11. The system SHALL manter o Fio identificável no favicon renderizado em 16×16, sem fechamento da volta interna nem fusão das duas extremidades.
12. WHERE uma instituição configurou logo, ícone ou cor, the system SHALL continuar usando os três valores dela no login, sidebar, favicon, manifesto, push e e-mail, sem mostrar nem aplicar a marca Esclari nesses lugares.
13. WHERE não há cor da instituição, the system SHALL usar `#C83A23` no cabeçalho do e-mail e no `theme_color` do manifesto, com texto branco no cabeçalho.
14. The system SHALL manter `#224fa7` como cor de ação padrão da interface e SHALL renomear o preset “Azul Esclari” para “Azul”, sem alterar o valor do preset.

### S4: Kit e guia (P2)

**Acceptance Criteria**

15. The system SHALL entregar masters SVG de símbolo, corte pequeno, palavra-marca, horizontal claro, horizontal escuro, vertical, preto e branco, mais PNG transparente de símbolo e lockups em 256, 512 e 1200 px.
16. The system SHALL entregar uma prancha com seis aplicações: aba do navegador, ícone PWA, cabeçalho de e-mail, capa de apresentação, co-branding com uma instituição fictícia e assinatura “Atendimento por Esclari”.
17. The guide SHALL documentar ideia, arquivos, paleta HEX/RGB/CMYK aproximado, contraste, área de respeito, tamanhos mínimos, fundos permitidos, co-branding, usos proibidos, tipografia e a necessidade de prova física antes de indicar Pantone.
18. The guide SHALL fixar área de respeito em metade da largura expandida do Fio, mínimo de 16 px para o corte pequeno, 24 px para o símbolo master, 96 px para o horizontal e 72 px para o vertical.

### S5: Aplicação e invariantes (P1)

**Acceptance Criteria**

19. WHILE the root `/` uses the light theme, the system SHALL mostrar `logo-horizontal.svg`; WHILE it uses dark or black, the system SHALL mostrar `logo-horizontal-escuro.svg`, com nome acessível “Esclari”.
20. WHILE the viewport is 360×780, the root `/` SHALL mostrar o novo logo a 40 px de altura sem rolagem horizontal.
21. WHILE the viewport is 844×390, the root `/` SHALL mostrar o novo logo e os dois parágrafos sem `scrollHeight` maior que 390.
22. The system SHALL manter build .NET, testes .NET tocados, build web, testes web tocados, `crapper` abaixo de 10 e `dryer` sem candidato a 0,8 verdes; mutação permanece suspensa por AD-025.

## Out of scope

| Excluded | Why |
| --- | --- |
| trocar o azul de ação da interface por Origem | vermelho em controles cria semântica de erro; AD-027 continua |
| redesenhar telas | o pedido é a marca e seu kit; arranjo, componentes e navegação não mudam |
| rodapé “Atendimento por Esclari” no produto institucional | aparece como mockup de marca, não como nova funcionalidade de co-branding |
| animação do Fio | o símbolo permite movimento, mas não há uso aprovado no produto |
| slogan novo | a frase atual da raiz permanece |
| Pantone definitivo | exige prova física no material e fornecedor escolhidos |
| registro ou parecer jurídico | busca INPI e busca reversa são ações do dono antes do lançamento público |
| alterar logos, ícones ou cores enviados pela instituição | AD-032 dá precedência à identidade dela |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| direção | Fio de procedência | escolhida pelo dono depois da prancha de conceitos | y |
| kit completo | produzir depois deste plano | o dono respondeu “vamos seguir com a recomendada” ao convite para continuar | y |
| paleta | Origem, Origem clara, Tinta e Papel da door 3 | território quente distingue o Esclari do azul dominante; contrastes medidos | n |
| palavra-marca | Public Sans 650 como base, depois redesenhada | já licenciada e usada no produto; a customização evita palavra genérica | n |
| interface padrão | azul `#224fa7` permanece | diferencia ação de marca e respeita AD-027 | n |
| perfil | `ui` | muda ativos visuais vistos na raiz, PWA e e-mail | n |

**Open questions:** none - os defaults não confirmados estão expostos neste plano para aprovação.

## Observable

| Surface | Decision | Landing |
| --- | --- | --- |
| raiz `/` | tema claro, escuro e preto | AC 19 |
| raiz `/` | empty e loading | n/a - tela estática, sem dados |
| raiz `/` | error | existing - `alt="Esclari"` continua quando a imagem não carrega |
| raiz `/` | unauthorized | n/a - rota pública |
| raiz `/` | celular 360×780 | AC 20 |
| raiz `/` | celular deitado 844×390 | AC 21 |
| raiz `/` | teclado aberto | n/a - tela sem campo |
| raiz `/` | toque | n/a - logo não é controle |
| raiz `/` | destructive action confirms | n/a - sem ação destrutiva |
| favicon e PWA | 16 px, tamanhos, opacidade, zona segura e determinismo | AC 9, AC 10, AC 11 |
| identidade da instituição | logo, ícone e cor prevalecem | AC 12 |
| e-mail e manifesto sem cor institucional | Origem + branco | AC 13 |
| tela Aparência | preset de cor | AC 14 |
| documento guia | estrutura, tom, profundidade e próximo passo | AC 17, AC 18 |
| prancha | seis contextos nomeados | AC 16 |
| assets `GET /marca/*` | formato e status | AC 1 a AC 7; Surface |
| assets públicos | autorização, limite de taxa e versionamento | n/a - arquivos estáticos públicos; nomes são o contrato da door 4 |
| comando `npm --prefix web run icones` | saída, repetição e falha | AC 9; falha existing - termina 1 antes de escrever se símbolo, `theme_color` ou Chromium faltar |

## Sources

- Escolha do dono em 17/11/2026 e `.specs/features/redesign-marca-esclari/` (binding): direção, SVG aprovado, testes, riscos e recomendação.
- `.specs/features/redesign-marca-esclari-final/pesquisa.md` (binding): acabamento pendente, paleta, contrastes, tipografia, contratos atuais e composição do kit.
- AD-027, AD-032, AD-034, AD-058 e `../marca-esclari/verification.md` (binding): cor de ação, precedência institucional, fontes, nome e lacunas da marca anterior.
