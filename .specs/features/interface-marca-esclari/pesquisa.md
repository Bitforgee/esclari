# Pesquisa: interface com a identidade do Esclari

Data: 17/11/2026. Conteúdo das fontes parafraseado.

## Estado atual (medido no código)

- Toda a base de cor de `web/src/index.css` usa `var(--marca, oklch(0.45 0.15 262))`: sem cor da instituição, botões, item ativo da barra lateral e os tingimentos de fundo, borda e campo saem do azul `#224fa7` (AD-027). Há 32 ocorrências do azul em `index.css`.
- Os neutros claros são cinzas frios (`oklch(1 0 none)`, `0.975`, `0.93`…); os escuros são cinzas sem matiz (`oklch(0.17 0 none)`…). O texto é `oklch(0.145 0 0)`.
- A cor da instituição (AD-032) entra por `--marca` e substitui o azul; o mesmo `color-mix` tinge os neutros com ela.
- A marca nova (AD-062) usa Origem `#C83A23`, Origem clara `#F16A4B`, Tinta `#15133A` e Papel `#F7F5EE`, mas nada disso chega à interface.
- O fundo do app aparece também fora do CSS: `<meta name="theme-color">` em `web/index.html` (`#fafbfd` e `#101113`), `theme_color` e `background_color` em `web/vite.config.ts` (`#fafbfd`), `ManifestoInstituicao.FundoDoApp` na API (`#fafbfd`) e a margem dos ícones `any`, que `gerar-icones.mjs` pinta com o `theme_color`.

Conversão das cores da marca para o formato dos tokens:

| Cor | HEX | OKLCH |
| --- | --- | --- |
| Tinta | `#15133A` | `oklch(0.216 0.073 280.7)` |
| Papel | `#F7F5EE` | `oklch(0.970 0.010 93.6)` |
| Origem | `#C83A23` | `oklch(0.557 0.183 31.9)` |
| Origem clara | `#F16A4B` | `oklch(0.685 0.173 34.3)` |
| vermelho de erro atual | — | `oklch(0.577 0.245 27.3)` |

## O que as referências dizem

- **Cor de status não é cor de marca.** O design system do governo da Louisiana recomenda não usar as cores de status como cor de marca, porque isso dilui o significado delas ([Pelican, Status colors](https://pelican.ots.la.gov/v1/foundation/status-colors)). Origem tem matiz 31,9 e o vermelho de erro tem 27,3: na tela, os dois se confundem. Por isso Origem fica fora de qualquer controle, estado ativo ou foco.
- **Cada token tem um papel.** O Carbon organiza a cor em tokens por papel, e o tema só define o valor que cumpre cada papel ([Carbon, Color overview](https://www.carbondesignsystem.com/building-blocks/foundations/color/overview)). O plano troca valores dentro dos papéis que já existem (`--primary`, `--background`…) e não cria papéis novos.
- **Ação usa a cor de marca; erro tem a sua.** No Material 3, as cores de marca vão para as ações (primary) e o erro tem um papel separado ([Material color system](https://m2.material.io/develop/ios/components/theming/color)). Aqui, a cor de marca de ação é Tinta e o erro continua vermelho, um pouco mais escuro para manter 4,5:1 sobre o Papel.
- **A pesquisa da identidade visual (AD-034) continua válida.** Public Sans e Source Serif 4 foram escolhidas contra a cara de modelo pronto; a Sora fica só desenhada dentro do logo.

## Contrastes medidos da proposta

Calculados pela luminância relativa WCAG a partir dos valores OKLCH do plano.

| Par | Claro | Escuro |
| --- | --- | --- |
| texto / fundo | 16,25 | 16,56 |
| texto / cartão | 17,72 | 15,52 |
| texto secundário / fundo | 6,01 | 7,43 |
| texto secundário / `--muted` | 5,58 | 6,73 |
| texto secundário / `--primary-subtle` | 5,48 | 5,89 |
| texto secundário / `--warning-subtle` | 6,01 | 5,31 |
| texto do botão / botão | 16,97 | 16,25 |
| erro / fundo | 4,98 (com `0.55`) | 6,62 |
| borda do campo / fundo | 3,34 | 4,46 |
| borda do campo / `--muted` | 3,10 | 4,04 |
| foco / fundo | 5,50 | 4,05 |

Com o vermelho de erro atual (`0.577`), o par erro/fundo no claro cai para 4,37:1. Por isso o erro claro passa a `0.55`.
