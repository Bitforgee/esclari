# Interface com a identidade do Esclari

## Problem

A marca do Esclari mudou (AD-062), mas a interface não. Fora do logo, o sistema continua com o azul `#224fa7` de AD-027 em botões, item ativo da barra lateral e tingimento de fundos, e com neutros cinza frios. Quem abre o produto vê o logo Origem e Tinta sobre uma interface azul: são duas identidades na mesma tela.

Quem paga é o Esclari, cuja cara própria só aparece no logo, e a instituição sem cor configurada, que recebe por padrão um azul que não pertence a ninguém. O dono pediu em 17/11/2026 que a paleta da marca chegasse ao sistema, depois de confirmar que só o logo havia mudado.

Depois desta feature, sem cor da instituição, a interface usa Tinta como cor de ação e Papel como fundo claro, e o tema escuro ganha uma matiz Tinta. Origem continua só no logo e no ícone, porque tem quase a mesma matiz do vermelho de erro. Tipografia e arranjo não mudam. A instituição com cor própria continua vendo a cor dela em botões e destaques.

## Flow

Reaproveita os papéis de token que já existem em `web/src/index.css` e o mecanismo de `--marca` da AD-032; não cria componente nem token novo.

1. `web/src/index.css` (exists) troca os valores de base de `:root`, `.dark` e `.black` pela tabela da door 1 e a regra de fallback pela door 2.
2. `web/src/aparencia.tsx` (exists) continua escrevendo `--marca` só quando a instituição tem cor; nada muda nele.
3. `web/index.html` (exists), `web/vite.config.ts` (exists) e `ManifestoInstituicao` (exists) trocam o fundo do app pela door 3.
4. `web/scripts/gerar-icones.mjs` (exists) lê o novo `theme_color` e regrava os ícones `any` com margem Papel.
5. Os componentes compartilhados do shadcn (exist) e as telas continuam usando as classes utilitárias de cor que já usam: recebem a cor nova pelos tokens, sem mudar código.

## Impact

| Front | What changes |
| --- | --- |
| tokens claros | fundo Papel, cartão branco, texto Tinta, ação Tinta, neutros com matiz quente leve (94°), erro `0.55` |
| tokens escuros | fundo e superfícies com matiz Tinta (281°), texto e ação Papel |
| tema Preto | fundo `#000` mantido; só os destaques sutis trocam a matiz azul pela Tinta |
| instituição com cor | botões, item ativo e destaques continuam na cor dela; os neutros que ela tinge passam a partir da nova base |
| instituição sem cor | deixa de ver o azul `#224fa7`; vê Tinta em botões e Papel no fundo |
| fora do CSS | `theme-color` do `index.html`, `theme_color`/`background_color` do PWA, `FundoDoApp` do manifesto da API e margem dos ícones `any` |
| preset “Azul” | continua `#224fa7` como opção da tela Aparência |
| tipografia, raios, arranjo | nada |
| gráficos `--grafico-1..4` | nada (AD-060) |
| testes | `interface.test.ts` (tabela `Tokens` e helper `tingido`), `guidelinesGlobal.test.ts` (C7 theme-color), `aparencia.test.tsx` (tokens que derivam de `--marca`), `icones.test.ts` (cor da margem), `ManifestoTests` (fundo do app); a lista fechada vai no `checks.md` |
| stored data | nada |

## Relations

None - no stored-data shape change

## Surface

None - nenhuma rota nova nem assinatura alterada; `GET /api/instituicoes/{slug}/manifest.webmanifest` muda só o valor de `background_color`, coberto pelo AC 9

## Landing

| One-way door | Literal shape | Alternative rejected |
| --- | --- | --- |
| 1. Valores de base | Claro: `--background` `oklch(0.970 0.010 94)` (Papel), `--card`/`--popover` `oklch(1 0 0)`, `--foreground` e textos de cartão/popover `oklch(0.216 0.073 281)` (Tinta), `--primary` `oklch(0.216 0.073 281)`, `--primary-foreground` `oklch(0.985 0 0)`, `--primary-subtle` `oklch(0.94 0.02 281)`, `--sidebar` `oklch(0.955 0.012 94)`, `--sidebar-accent` `oklch(0.92 0.014 94)`, `--sidebar-border` `oklch(0.88 0.014 94)`, `--sidebar-primary` = `--primary`, `--muted` `oklch(0.945 0.012 94)`, `--accent` `oklch(0.935 0.013 94)`, `--border` `oklch(0.89 0.012 94)`, `--input` `oklch(0.62 0.012 94)` (`0.59` no `.form-resposta`), `--muted-foreground` `oklch(0.48 0.012 281)`, `--destructive` `oklch(0.55 0.22 27.3)`, `--ring` `oklch(0.5 0 0)`. Escuro: `--background` `oklch(0.17 0.02 281)`, `--card`/`--popover` `oklch(0.205 0.02 281)`, `--foreground` `oklch(0.95 0.008 94)`, `--primary` `oklch(0.970 0.010 94)`, `--primary-foreground` `oklch(0.216 0.073 281)`, `--primary-subtle` `oklch(0.27 0.04 281)`, `--sidebar` `oklch(0.15 0.02 281)`, `--sidebar-accent` `oklch(0.27 0.025 281)`, `--sidebar-border` `oklch(0.27 0.02 281)`, `--muted` `oklch(0.22 0.02 281)`, `--accent` `oklch(0.25 0.022 281)`, `--border` `oklch(0.27 0.02 281)`, `--input` `oklch(0.58 0.015 281)`, `--muted-foreground` `oklch(0.71 0.01 281)`. Preto: mantém fundo, cartão e texto atuais; `--primary-subtle` `oklch(0.24 0.04 281)`. `--secondary`, `--warning-subtle`, `--success`, gráficos e o `--destructive` escuro não mudam | só trocar o azul por Tinta e manter os cinzas frios: o fundo branco-azulado contradiz o Papel do logo e a interface continua genérica; usar Origem como ação: matiz 31,9 contra 27,3 do erro, e status não deve virar marca (`pesquisa.md`) |
| 2. Regra de fallback | Sem `--marca`, cada neutro é exatamente o valor da door 1 (o `color-mix` usa a própria base como fallback, então não tinge) e `--primary`/`--sidebar-primary` são Tinta no claro e Papel no escuro. Com `--marca`, ficam o mesmo `color-mix` e as mesmas porcentagens de hoje, aplicados sobre as bases novas; `--primary`, `--sidebar-primary`, `--ring` e as derivações `--marca-clara`/`--marca-sutil*` seguem a cor da instituição como hoje | fallback Tinta dentro de todos os `color-mix`: 2 a 6% de Tinta escurece e esfria o Papel, que deixa de ser a cor da marca |
| 3. Fundo do app fora do CSS | `theme-color` claro `#F7F5EE` e escuro `#0E0E18` em `web/index.html`; `theme_color` e `background_color` `#F7F5EE` em `web/vite.config.ts`; `ManifestoInstituicao.FundoDoApp` `#F7F5EE`; ícones `any` regravados com margem `#F7F5EE`; tema Preto continua `#000000` | manter `#fafbfd`: a barra do navegador e a tela de abertura do PWA ficariam frias sobre um app quente |

- Nothing else in this change is hard to reverse.

## Criteria

### S1: Paleta da interface (P1)

**Acceptance Criteria**

1. WHILE no institution colour is configured, the system SHALL apply in `:root`, `.dark` and `.black` exactly the token values of door 1, and SHALL NOT contain `oklch(0.45 0.15 262)`, `oklch(0.72 0.12 262)` or `oklch(0.96 0.02 262)` in `web/src/index.css`.
2. WHILE no institution colour is configured, the system SHALL render primary buttons, the active sidebar indicator and links that use `--primary` in Tinta `oklch(0.216 0.073 281)` in the light theme and Papel `oklch(0.970 0.010 94)` in dark and black.
3. WHERE an institution colour is configured, the system SHALL keep `--primary`, `--sidebar-primary`, `--ring` and the `--marca-*` derivations equal to today's formulas over that colour, and SHALL tint background, sidebar, muted, accent, border and input with the same percentages as today over the new bases.
4. The system SHALL keep at least 4.5:1 for text/background, text/card, secondary text/background, secondary text/muted, secondary text/primary-subtle, secondary text/warning-subtle, button text/button and destructive/background, and at least 3:1 for field border/background, field border/muted and focus/background, in light, dark and black, with and without the 8 Aparência presets.
5. The system SHALL NOT use Origem `#C83A23` or Origem clara `#F16A4B` in any token of `web/src/index.css`, any class in `web/src/**/*.tsx` or any focus, active, status or button colour.
6. The system SHALL keep `--font-sans`, `--font-heading`, the four radii and `--grafico-1..4` exactly as before.

### S2: Fundo do app fora do CSS (P1)

**Acceptance Criteria**

7. The system SHALL declare `theme-color` `#F7F5EE` for `prefers-color-scheme: light` and `#0E0E18` for dark in `web/index.html`, and SHALL write `#F7F5EE`, `#0E0E18` and `#000000` into the unscoped `theme-color` when the user picks Claro, Escuro and Preto.
8. The system SHALL set `theme_color` and `background_color` to `#F7F5EE` in the PWA manifest of `web/vite.config.ts`.
9. WHEN `GET /api/instituicoes/{slug}/manifest.webmanifest` is requested THEN the system SHALL return `background_color` `#F7F5EE`, and SHALL keep `theme_color` as the institution colour or the AD-062 fallback.
10. WHEN `npm --prefix web run icones` runs THEN the system SHALL draw the margin of `icon-192.png` and `icon-512.png` in `#F7F5EE` and keep maskable and apple icons unchanged in shape and colour.

### S3: Telas e invariantes (P1)

**Acceptance Criteria**

11. The system SHALL produce screenshots of Login, Assistente, Minhas solicitações, Detalhe do chamado, Fila, Indicadores and Aparência in Claro and Escuro at 360×780 and 1280×800, without institution colour, saved in the feature folder and inspected before Verify.
12. The system SHALL keep the existing arrangement, mobile, touch and keyboard suites green, with no layout assertion changed.
13. The system SHALL keep build web, build .NET, the touched .NET tests, the full Vitest suite, `crapper` below 10 and `dryer` with no candidate at 0.8 green; mutation stays suspended by AD-025.

## Out of scope

| Excluded | Why |
| --- | --- |
| Origem em controles ou estados | quase a mesma matiz do erro; status não vira marca |
| tipografia da interface | AD-034 continua; a Sora fica só no logo |
| paleta dos gráficos (`--grafico-1..4`) | AD-060 tem contrastes medidos nos 4 temas; outra feature se for preciso |
| novo preset “Tinta” na Aparência | ninguém pediu; o padrão sem cor já é Tinta |
| cor do cabeçalho do e-mail e `theme_color` do manifesto | já decididos na AD-062 (Origem) |
| arranjo, componentes, ícones Lucide | só valores de cor mudam |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| Cor de ação | Tinta no claro, Papel no escuro | recomendação aceita pelo dono em 17/11 ("sim planeje") | y |
| Neutros | Papel quente no claro e matiz Tinta no escuro, para todas as instituições | dá cara própria à interface; a matiz é baixa (C ≤ 0,02) e convive com qualquer cor de instituição | n |
| Origem na interface | nenhuma, nem no indicador ativo | matiz 31,9 contra 27,3 do erro (`pesquisa.md`) | n |
| Erro no claro | `oklch(0.55 0.22 27.3)` | o atual dá 4,37:1 sobre Papel | n |
| Instituições já cadastradas sem cor | passam a ver Tinta e Papel sem aviso | não há produção (AD-058); a Univag de teste pode configurar cor na Aparência | n |
| Perfil | `ui` | muda cor de todas as telas | n |

**Open questions:** none - os defaults não confirmados estão expostos acima para a aprovação.

## Observable

| Surface | Decision | Landing |
| --- | --- | --- |
| todas as telas | cor sem instituição e com instituição | AC 2, AC 3 |
| todas as telas | contraste nos 3 temas e 8 presets | AC 4 |
| todas as telas | vazio, carregando, erro, não autorizado | existing - estados das features anteriores; só a cor muda, coberto por AC 4 |
| todas as telas | celular 360×780 | AC 11; arranjo existing - AC 12 |
| todas as telas | celular deitado 844×390 | n/a - nenhum tamanho ou posição muda; as suítes de arranjo existentes continuam (AC 12) |
| todas as telas | teclado aberto | n/a - nenhum campo muda de lugar ou altura; só a cor da borda, coberta por AC 4 |
| todas as telas | toque | n/a - alvos de 44 px e `touch-action` não mudam (AC 12) |
| foco | anel de foco visível | AC 4 |
| ações destrutivas | confirmação e cor de erro distinta da marca | AC 4, AC 5 |
| barra do navegador e abertura do PWA | cor do fundo | AC 7, AC 8, AC 9 |
| ícones do PWA | margem dos ícones `any` | AC 10 |

## Sources

- Pedido do dono em 17/11/2026 ("sim planeje") sobre a recomendação de Tinta como ação e Origem só em destaques; `pesquisa.md` desta pasta (binding): valores medidos, contrastes e referências.
- AD-027, AD-032, AD-034, AD-037, AD-060 e AD-062 em `.specs/STATE.md` (binding).
- `../redesign-marca-esclari-final/guia-de-marca.md` (binding): paleta e papéis das cores da marca.
