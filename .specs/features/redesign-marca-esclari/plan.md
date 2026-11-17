# Redesign da marca Esclari — etapa de conceitos

## Problem

A marca atual do Esclari é um `E` geométrico azul com uma cápsula amarela e uma palavra-marca em Sora. Ela funciona como arquivo e ícone, mas se parece com o vocabulário comum de SaaS: inicial em placa arredondada, azul tecnológico, acento amarelo, sans geométrica minúscula e pingo colorido. A cápsula só significa “trecho citado” depois que alguém lê o guia; em uma cor, a ideia desaparece. A verificação anterior também registrou o encontro óptico do `r` com o `i`.

Quem paga é o Esclari, que tem pouca exposição própria porque a identidade da instituição prevalece no produto. Uma marca pouco vista precisa ensinar o nome e deixar uma memória própria depressa. O pedido do dono em 17/11/2026 é refazer a logo com pesquisa de referências na internet e buscar um resultado surpreendente e bonito.

Esta etapa não promete “a melhor logo de todas”, uma afirmação que não pode ser provada. Ela produz três direções profissionais, distintas e comparáveis, elimina soluções genéricas por testes concretos e deixa o dono escolher antes de qualquer troca no produto.

**Design brief:** Esclari é um helpdesk acadêmico multi-instituição que leva a dúvida do aluno a uma resposta baseada em fonte publicada ou a uma pessoa. A marca deve ser clara, confiável, humana, inteligente e inesperada; não deve parecer infantil, burocrática, genérica de IA ou genérica de universidade. A ideia central é **clareza com procedência**: da dúvida à próxima ação, com origem verificável e uma pessoa ao alcance.

## Flow

Reaproveita os ativos e testes de `marca-esclari` como referência e mantém todos os arquivos de produção intactos durante esta etapa.

1. `.specs/features/redesign-marca-esclari/pesquisa.md` (new) registra o diagnóstico, as referências externas, os clichês proibidos, o mapa de palavras e a longlist de dez territórios.
2. `.specs/features/redesign-marca-esclari/conceitos/` (new) recebe três símbolos e três lockups exploratórios em SVG preto: **Fonte embutida** (wordmark-led), **Manícula contemporânea** (pictorial) e **Fio de procedência** (abstract).
3. Os scripts da skill `logo-design` (exists outside repository) auditam a estrutura, renderizam os SVG e montam testes de escala, uma cor, fundo invertido, espelho, rotação, desfoque, contexto e prateleira contra marcas da categoria.
4. Cada direção passa por pelo menos duas rodadas de ajuste, com versões preservadas em vez de sobrescritas.
5. `concept_sheet.py` (exists outside repository) gera uma única prancha em preto e branco com as três direções, os tamanhos reais de 64, 32 e 16 px, uma frase por ideia, o tipo de marca, a recomendação e um risco honesto.
6. O dono escolhe uma direção ou pede uma segunda rodada. Até essa escolha, nenhum kit final é produzido e os módulos de produção permanecem intactos.

## Impact

| Front | What changes |
| --- | --- |
| pesquisa | entra um registro das fontes vistas, convenções da categoria, clichês proibidos, mapa de palavras e territórios |
| conceitos | entram SVGs e PNGs exploratórios somente em `.specs/features/redesign-marca-esclari/`; não são servidos pelo produto |
| marca atual | `web/public/marca/`, `web/public/icons/`, cores, e-mails, manifesto e raiz `/` permanecem byte a byte iguais nesta etapa |
| testes | nenhum teste do produto muda; as provas são auditorias estruturais e folhas visuais dos conceitos |
| dados e contratos | nenhum |
| próximo passo | a direção escolhida ganha um plano separado com a geometria literal, arquivos finais, cores, aplicação, testes e kit; a escolha não é presumida aqui |

## Relations

None - no stored-data shape change

## Surface

None - nothing consumed outside; os conceitos são artefatos de revisão e não são publicados pelo produto

## Landing

None - esta etapa mantém as três alternativas abertas até a revisão visual; congelar a geometria, a paleta ou os contratos de arquivos antes da escolha transformaria uma exploração em decisão não revisada

## Criteria

### S1: Estratégia própria (P1)

**Acceptance Criteria**

1. The concept study SHALL registrar de 8 a 12 territórios em uma frase cada, distribuídos por pelo menos três tipos de marca, e SHALL identificar os três territórios escolhidos para desenho.
2. The concept study SHALL proibir balão de conversa, três pontos, robô, headset, faísca, cérebro, capelo, livro aberto, diploma, escudo com check, inicial dentro de placa arredondada, monograma de fonte pronta e azul com amarelo como único argumento visual.
3. The concept study SHALL ligar cada direção construída a “clareza com procedência” em uma frase que continue verdadeira sem cor.

### S2: Três conceitos comparáveis (P1)

**Acceptance Criteria**

4. The concept study SHALL apresentar exatamente três direções diferentes: **Fonte embutida** como wordmark-led combination, **Manícula contemporânea** como pictorial combination e **Fio de procedência** como abstract combination.
5. The concept SHALL incluir um símbolo e um lockup horizontal em SVG vetorial preto, com letras convertidas em curvas e sem `<text>`, `<image>`, raster, `filter`, `mask`, gradient ou fonte externa.
6. The concept SHALL obter pelo menos 90/100 no `svg_audit.py`, sem `FAIL`, e SHALL ter cada aviso resolvido ou justificado na apresentação.
7. The concept SHALL sobreviver em preto sobre branco e branco sobre preto, manter a ideia identificável em 64, 32 e 16 px e não criar leitura ofensiva ou símbolo político, religioso ou sexual quando espelhado ou girado 90° e 180°.
8. The concept SHALL passar por pelo menos duas iterações preservadas, com a segunda corrigindo um problema observado na auditoria ou na inspeção renderizada da primeira.
9. The concept study SHALL comparar as três direções, no mesmo tamanho, com a marca atual e com pelo menos três referências de educação ou atendimento, sem traçar nem reutilizar a geometria delas.
10. The concept study SHALL inspecionar os PNG renderizados a 512 px e a folha de testes antes de apresentar qualquer direção como recomendada.

### S3: Prancha e decisão (P1)

**Acceptance Criteria**

11. WHEN the three concepts have passed their tests THEN the concept study SHALL gerar uma prancha única em preto e branco com as três direções, símbolo grande, lockup, tamanhos reais de 64, 32 e 16 px, nome, tipo, ideia em uma frase e risco honesto.
12. The concept study SHALL recomendar uma direção com razões ligadas ao brief e SHALL registrar um risco de cada direção, sem alegar exclusividade jurídica nem “melhor logo de todas”.
13. WHILE the owner has not chosen a direction, the work SHALL NOT alter any tracked file under `web/`, `Esclari.Api/` or `Esclari.Api.Tests/`, SHALL NOT add final colour variants and SHALL NOT produce the final logo kit.
14. WHEN the concept sheet is presented THEN the work SHALL stop for the owner's choice or requested revision.

## Out of scope

| Excluded | Why |
| --- | --- |
| trocar a marca no produto | depende da direção escolhida e de um plano que fixe a geometria literal e os contratos dos ativos |
| paleta final | cor será aplicada só depois de a forma vencer em preto e branco |
| kit final, favicon, ícones PWA, lockups finais e guia | a skill exige o checkpoint dos conceitos antes desse trabalho |
| redesign da interface | a identidade da instituição e os tokens atuais permanecem |
| slogan | o pedido é de logo; a frase atual da raiz não muda |
| busca e registro no INPI | exigem pesquisa jurídica nominativa e figurativa; a busca web feita aqui não dá liberação |
| copiar ou “fazer parecido” com uma referência | referências ensinam mecanismos; suas formas e marcas pertencem aos titulares |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| decisão final | o dono do projeto que fez este pedido escolhe a direção | é quem pediu o redesign e controla o produto | n |
| nome e caixa | `Esclari` no texto; a caixa da palavra-marca será explorada sem mudar o nome | AD-058 fixa o nome; desenho tipográfico não altera a grafia textual | n |
| equidade a preservar | preservar a ideia de fonte destacada, não o `E`, a cápsula, o azul ou o amarelo atuais | mantém a verdade do produto e abre território formal | n |
| direção recomendada antes de desenhar | Fonte embutida | o nome curto precisa ser aprendido e terá pouca exposição; a palavra-marca pode gerar símbolo próprio | n |
| quantidade | três conceitos | é o checkpoint da skill e permite comparação sem diluir o acabamento | n |
| cor na apresentação | preto e branco | separa qualidade formal de gosto por paleta | n |
| arquivos exploratórios anteriores | `.specs/logo/` não é fonte nem entrega desta etapa | contém os primeiros conceitos rejeitados e capturas de pesquisa; o redesign parte do brief novo | n |

**Open questions:** none - os defaults são reversíveis nesta etapa e aparecem para aprovação neste plano.

## Observable

| Surface | Decision | Landing |
| --- | --- | --- |
| documento `pesquisa.md` | estrutura, tom, profundidade e próximo passo | AC 1, AC 2, AC 3 |
| três SVGs de símbolo e três lockups | formato, pureza vetorial, tipos diferentes | AC 4, AC 5, AC 6 |
| conceitos em escala | 64, 32 e 16 px, uma cor e invertido | AC 7 |
| processo visual | duas iterações e inspeção real da renderização | AC 8, AC 10 |
| teste de prateleira | marca atual + pelo menos três referências | AC 9 |
| prancha de conceitos | ordem, conteúdo, recomendação e risco | AC 11, AC 12 |
| checkpoint humano | escolha ou pedido de revisão antes da aplicação | AC 13, AC 14 |
| celular 360×780 | n/a - esta etapa entrega uma prancha de revisão, não uma tela do produto; a versão final será testada na raiz e no PWA |
| celular deitado 844×390 | n/a - esta etapa não altera tela; o plano final herdará os testes existentes da raiz |
| teclado aberto | n/a - nenhum campo ou tela muda |
| toque | n/a - nenhum controle ou tela muda |
| empty, loading, error e unauthorized | n/a - os artefatos são arquivos locais estáticos de revisão, sem estado de aplicação |
| destructive action confirms | n/a - a marca atual não é substituída nesta etapa |

## Sources

- Pedido do dono em 17/11/2026 e `.specs/features/redesign-marca-esclari/pesquisa.md` (binding): brief, diagnóstico, referências pesquisadas, clichês proibidos, mapa de palavras e territórios.
- `.specs/features/marca-esclari/plan.md`, `guia-de-marca.md` e `verification.md` (binding): marca atual, contratos existentes, aplicações e defeito óptico conhecido.
- AD-032, AD-034, AD-058 e AD-059 em `.specs/STATE.md` (binding): precedência da instituição, tipografia do produto, nome Esclari e caminhos do repositório.
