# Pesquisa: acabamento e aplicação do Fio de procedência

Data: 17/11/2026. Continuação de `../redesign-marca-esclari/pesquisa.md`. O dono escolheu a direção recomendada, **Fio de procedência**, depois de ver a prancha monocromática.

## O que ainda não é final

O conceito aprovado é uma linha central editável de 28 unidades, com pontas e encontros arredondados:

`M36 224 C36 112 78 32 150 32 C198 32 228 68 228 116 C228 164 196 200 150 200 C112 200 86 174 86 140 C86 106 112 82 146 82 H224`

Ele passou nos testes de 16, 32 e 64 px, mas o `svg_audit.py` ainda informa três resíduos próprios de exploração:

- o traço precisa ser expandido para preenchimento no master;
- o `viewBox` precisa ser recortado e centrado opticamente;
- os lockups ainda usam a palavra em Sora da marca anterior, com complexidade acima da mediana e o encontro conhecido entre `r` e `i`.

A aprovação escolhe a ideia e a silhueta, não autoriza publicar esse arquivo exploratório como master.

## Palavra-marca

A marca tem pouca exposição própria porque a instituição prevalece nas telas. “Esclari” precisa continuar escrito no primeiro contato. A hipótese é uma palavra-marca minúscula de desenho próprio, construída a partir da Public Sans, já usada no produto e licenciada sob OFL-1.1 ([projeto oficial](https://github.com/uswds/public-sans)).

O desenho final usa a fonte somente como ponto de partida: peso 650, contornos expandidos, espaçamento óptico e alterações no `e`, `a`, `r` e `i` para compartilhar a abertura e os terminais arredondados do fio. O `i` não recebe ponto colorido. A [Intercom](https://www.intercom.com/blog/how-and-why-we-refreshed-our-brand) relata que uma sans neutra sem assinatura passa a se parecer com a de qualquer empresa de tecnologia; por isso o arquivo não será apenas Public Sans digitada.

## Cor

A forma venceu em preto e branco. A cor passa a reforçar a ideia, sem ser necessária para entendê-la.

A biblioteca da skill mostrou azul em 43,5% das marcas de educação. A [Renaissance, da Pentagram](https://www.pentagram.com/work/renaissance), escolheu um vermelho quente para se separar do azul dominante da categoria. A lição é ocupar território quente e humano, sem copiar forma, fonte ou tom exato.

Paleta proposta:

| Papel | Nome | HEX | RGB | Contraste medido |
| --- | --- | --- | --- | --- |
| símbolo em fundo claro e fundo do ícone | Origem | `#C83A23` | 200, 58, 35 | 5,14:1 com branco |
| símbolo em fundo escuro | Origem clara | `#F16A4B` | 241, 106, 75 | 5,82:1 com Tinta |
| palavra e fundo escuro | Tinta | `#15133A` | 21, 19, 58 | 17,70:1 com branco; 16,23:1 com Papel |
| fundo de apresentação | Papel | `#F7F5EE` | 247, 245, 238 | 16,23:1 com Tinta |

As razões foram calculadas pela luminância relativa de WCAG. Origem pode receber texto branco; Origem clara só aparece sobre Tinta. A marca também terá versões preta e branca. Pantone não será inventado sem prova física; o guia registra que a equivalência exige prova de impressão.

O azul `#224fa7` permanece como cor de ação padrão da interface, conforme AD-027/AD-032. Vermelho como botão primário criaria semântica de perigo. O preset “Azul Esclari” vira apenas “Azul”, para não afirmar que a cor da interface é a cor nova da marca.

## Aplicações e contratos atuais

- `web/public/marca/` publica quatro SVGs: símbolo, horizontal claro, horizontal escuro e favicon.
- `web/scripts/gerar-icones.mjs` supõe `viewBox 64`, extrai o conteúdo do símbolo e gera os quatro PNG do PWA.
- O manifesto por instituição e o push dependem dos nomes atuais dos PNG. Eles não podem mudar.
- `LayoutEmail.CorPadrao` e `ManifestoInstituicao.CorPadrao` usam `#2438d6`; precisam acompanhar Origem quando não há cor da instituição.
- A raiz `/` escolhe o logo claro ou escuro pelo tema. A identidade da instituição prevalece em login, sidebar, e-mail, manifesto e push quando configurada.
- A verificação anterior encontrou que o teste do favicon não exigia o detalhe amarelo e que a zona segura só falhava numa segunda execução. O novo teste deve gerar o PNG antes de medir e comparar bytes para provar determinismo.

## Kit necessário

A [Zendesk](https://www.zendesk.com.br/company/brand-guidelines/) separa símbolo, palavra-marca, logo principal, tamanhos mínimos, co-branding e usos proibidos. Para o Esclari, o kit precisa ser pequeno e operacional:

- símbolo master e corte pequeno;
- palavra-marca;
- lockups horizontal e vertical;
- versões colorida clara, colorida escura, preta e branca;
- favicon e quatro PNG existentes do PWA;
- prancha com navegador, PWA, e-mail, slide, co-branding institucional e assinatura de suporte;
- guia de uma página com medidas, cores, fundos, co-branding e usos proibidos.

## Inspeção do dono depois da primeira aplicação

Ao comparar a aplicação final com `../redesign-marca-esclari/conceitos.png`, o dono aprovou o símbolo, mas apontou dois desvios: a palavra “Esclari” ficou materialmente diferente da proposta e o contorno do Fio parecia tremido. A causa foi confirmada no build: a palavra havia sido redesenhada como monoline, e o traço havia sido expandido por rasterização e retraçado por `potrace`.

Correção escolhida: restaurar os contornos Sora SemiBold usados na direção C, corrigindo apenas o vão entre `r` e `i`, e expandir o centro Bézier por offset vetorial com `paperjs-offset`, que preserva curvas e não passa por pixels. As ferramentas são apenas de construção em `.git/testagent`; nenhuma dependência entra no produto.

## Limites

O kit não altera o logo ou a cor enviados por uma instituição. Não redesenha telas. Não registra a marca. Busca nominativa e figurativa no INPI e busca reversa continuam necessárias antes do lançamento público.

O conteúdo das fontes externas foi parafraseado. Marcas de referência pertencem aos seus titulares e não serão incluídas no kit final.
