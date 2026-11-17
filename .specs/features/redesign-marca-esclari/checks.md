# Redesign da marca Esclari — etapa de conceitos checks

Profile: ui
Plan: `.specs/features/redesign-marca-esclari/plan.md`
Base: `108f95e19455f5a7ac0cc92d94bc969aa8c7b8dc`

14 checks em 3 slices · 0 portas de mão única · 0 perguntas abertas.

Todos os comandos rodam em `D:\esclari`. `provar_conceitos.py` usa somente a biblioteca padrão do Python e chama os scripts da skill `logo-design` pelo caminho do usuário.

## Checks

### S1 - Estratégia própria · 2 arquivos · ~20 KB · ~5k

**C1** - `pesquisa.md` contém exatamente 10 territórios numerados, distribuídos entre `wordmark`, `letterform`, `pictorial`, `emblema`, `combination` e `abstract`, e identifica Fonte embutida, Manícula contemporânea e Fio de procedência como as três direções construídas (AC 1, AC 4)
Proof: `python .specs/features/redesign-marca-esclari/provar_conceitos.py -k test_C1_`

**C2** - `pesquisa.md` registra como proibidos os 13 grupos: balão de conversa/três pontos; robô/rosto de bot/headset; faísca/estrela; cérebro/circuito/rede/orbe; capelo/livro/diploma/lápis/lâmpada/louros/brasão/coluna; escudo com check/seta/elo/ponte; inicial em placa; monograma de fonte pronta; sans minúscula com ponto colorido; azul+amarelo como único argumento; chaves/colchetes; gradiente violeta-ciano; avatares humano/IA (AC 2)
Proof: `python .specs/features/redesign-marca-esclari/provar_conceitos.py -k test_C2_`

**C3** - `refinamentos.md` explica cada uma das três direções em uma frase ligada a “clareza com procedência”, sem depender de cor para que a frase continue verdadeira (AC 3)
Proof: `python .specs/features/redesign-marca-esclari/provar_conceitos.py -k test_C3_`

### S2 - Três conceitos comparáveis · ~16 arquivos · ~100 KB · ~25k

**C4** - Existem exatamente três pares finais em `conceitos/`: `fonte-embutida-symbol.svg` + `fonte-embutida-lockup.svg`, `manicula-symbol.svg` + `manicula-lockup.svg` e `fio-procedencia-symbol.svg` + `fio-procedencia-lockup.svg`; `refinamentos.md` nomeia seus tipos como wordmark-led combination, pictorial combination e abstract combination, respectivamente (AC 4)
Proof: `python .specs/features/redesign-marca-esclari/provar_conceitos.py -k test_C4_`

**C5** - Cada um dos 6 SVG finais é XML válido, usa `viewBox` inteiro com símbolo `0 0 256 256` ou lockup com altura 256, contém ao menos um `<path>`, usa somente preto `#000000`/`#000` e não contém `<text>`, `<image>`, raster, `filter`, `mask`, gradient, fonte externa nem URL (AC 5)
Proof: `python .specs/features/redesign-marca-esclari/provar_conceitos.py -k test_C5_`

**C6** - Cada um dos 6 SVG finais obtém nota de prontidão de pelo menos 90/100 no `svg_audit.py`, sem linha `FAIL`; cada `WARN` remanescente aparece em `refinamentos.md` com justificativa específica do arquivo (AC 6)
Proof: `python .specs/features/redesign-marca-esclari/provar_conceitos.py -k test_C6_`

**C7** - Para cada direção, `preview_sheet.py` gera uma folha que mostra preto sobre branco, branco sobre preto, 64, 32 e 16 px, desfoque, espelho e rotações; a inspeção registrada em `refinamentos.md` confirma que a ideia continua identificável nos três tamanhos e não aparece leitura ofensiva, política, religiosa ou sexual (AC 7)
Proof: `python .specs/features/redesign-marca-esclari/provar_conceitos.py -k test_C7_`

**C8** - Cada direção conserva `-v1.svg` e `-v2.svg` do símbolo e do lockup, com hashes diferentes; `refinamentos.md` nomeia ao menos um defeito visual da v1 e a correção observada na v2 para Fonte embutida, Manícula contemporânea e Fio de procedência (AC 8)
Proof: `python .specs/features/redesign-marca-esclari/provar_conceitos.py -k test_C8_`

**C9** - `prateleira.html` põe os três símbolos finais, a marca atual e pelo menos três marcas de referência de educação ou atendimento no mesmo tamanho; `refinamentos.md` registra o teste de familiaridade de cada direção e declara que nenhuma geometria externa foi traçada ou reutilizada (AC 9)
Proof: `python .specs/features/redesign-marca-esclari/provar_conceitos.py -k test_C9_`

**C10** - Existem PNGs de 512 px dos 3 símbolos e 3 lockups finais e `refinamentos.md` registra a inspeção de cada arquivo e das três folhas de preview antes da recomendação (AC 10)
Proof: `python .specs/features/redesign-marca-esclari/provar_conceitos.py -k test_C10_`

### S3 - Prancha e decisão · 3 arquivos · ~2 MB · ~500k

**C11** - `conceitos.png` tem exatamente três colunas e, para cada direção, mostra símbolo grande, lockup, amostras reais de 64, 32 e 16 px, nome, tipo, ideia em uma frase e risco; a prancha é monocromática, salvo texto neutro e bordas da própria apresentação (AC 11)
Proof: `python .specs/features/redesign-marca-esclari/provar_conceitos.py -k test_C11_`

**C12** - `recomendacao.md` escolhe exatamente uma direção, liga a escolha aos cinco adjetivos `claro`, `confiável`, `humano`, `inteligente` e `inesperado`, registra um risco para cada direção e inclui a ressalva de que não há exclusividade jurídica nem garantia de “melhor logo de todas” (AC 12)
Proof: `python .specs/features/redesign-marca-esclari/provar_conceitos.py -k test_C12_`

**C13** - Em `108f95e..HEAD` e no working tree, nenhum arquivo rastreado ou não rastreado sob `web/`, `Esclari.Api/` ou `Esclari.Api.Tests/` muda; não existem variantes finais de cor nem kit final dentro da feature (AC 13)
Proof: `python .specs/features/redesign-marca-esclari/provar_conceitos.py -k test_C13_`

**C14** - `recomendacao.md` termina no checkpoint, pedindo que o dono escolha uma direção ou peça revisão, e não afirma que a aplicação ou o kit final estão prontos (AC 14)
Proof: `python .specs/features/redesign-marca-esclari/provar_conceitos.py -k test_C14_`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| critérios do plano (14) | AC1 C1 · AC2 C2 · AC3 C3 · AC4 C1,C4 · AC5 C5 · AC6 C6 · AC7 C7 · AC8 C8 · AC9 C9 · AC10 C10 · AC11 C11 · AC12 C12 · AC13 C13 · AC14 C14 | - |
| direções construídas (3) | Fonte embutida C3-C10 · Manícula contemporânea C3-C10 · Fio de procedência C3-C10 | - |
| tipos de marca exigidos (3) | wordmark-led combination C4 · pictorial combination C4 · abstract combination C4 | - |
| arquivos finais SVG (6) | Fonte símbolo C4-C6 · Fonte lockup C4-C6 · Manícula símbolo C4-C6 · Manícula lockup C4-C6 · Fio símbolo C4-C6 · Fio lockup C4-C6 | - |
| modos por conceito (6) | preto/branco C7 · branco/preto C7 · 64 px C7 · 32 px C7 · 16 px C7 · desfoque/espelho/rotações C7 | - |
| iterações por direção (2) | v1 C8 · v2 C8 | - |
| comparação de prateleira (7) | Fonte C9 · Manícula C9 · Fio C9 · marca atual C9 · referência 1 C9 · referência 2 C9 · referência 3 C9 | - |
| itens por coluna da prancha (8) | símbolo C11 · lockup C11 · 64 px C11 · 32 px C11 · 16 px C11 · nome C11 · tipo C11 · ideia+risco C11 | - |
| módulos de produção preservados (3) | `web/` C13 · `Esclari.Api/` C13 · `Esclari.Api.Tests/` C13 | - |
| Landing doors (0) | `None - etapa de exploração` | - |
| Relations entities (0) | `None - no stored-data shape change` | - |
| Surface routes (0) | `None - nothing consumed outside` | - |

- Nenhum check afirma status HTTP, rota ou forma de resposta.
- C7, C9, C10 e C11 combinam prova estrutural reproduzível com inspeção visual obrigatória do Verifier; a existência do arquivo sozinha não conta como PASS visual.

## Test policy

| Code | Required proofs | Coverage expectation |
| --- | --- | --- |
| SVG exploratório, decide forma e leitura | auditoria estrutural no próprio arquivo e inspeção visual independente | todos os 6 arquivos; todos os modos enumerados em C7; uma falha visual injetada por área no Verify |
| documento de estratégia, decide o que é permitido | parser no próprio documento e comparação do Verifier com o plano e as fontes binding | todos os territórios, clichês e riscos enumerados |
| prancha gerada, instrumentação | prova estrutural do conteúdo mais inspeção visual | exatamente 3 colunas e os 8 itens por coluna |
| scripts externos da skill, instrumentação | não ganham teste próprio | a saída auditada é a prova do conceito consumidor |

Evidence:

- os SVGs contêm a decisão visual inteira e podem falhar sem código de aplicação; por isso são provados no próprio nível e por renderização;
- `provar_conceitos.py` apenas mede estrutura, contagem, cor, auditoria e presença; não substitui o julgamento visual do Verifier;
- o análogo mais próximo é `web/src/test/logo.test.ts`, que valida estrutura e tamanho da marca atual, mas esta etapa não altera nem reutiliza essa suíte.

Cost: um script de prova com 14 seletores, 6 auditorias e inspeção de 5 artefatos visuais compostos. As regras valem só para esta feature e não entram no `AGENTS.md`.

## Swept

- validation: C1-C8, C11-C14 validam contagens, nomes, XML, viewBox, elementos proibidos, cor, nota, arquivos e checkpoint
- failure modes: C5 e C6 falham em SVG inválido, dependência proibida, `FAIL` ou nota abaixo de 90; C13 falha se tocar produção
- idempotency: n/a - artefatos versionados são sobrescritos só durante a exploração; cada iteração relevante recebe nome novo em C8
- authorization: n/a - arquivos locais de design não expõem operação nem dado
- concurrency: n/a - um único builder escreve arquivos locais sem estado compartilhado
- data lifecycle: n/a - nenhum dado de usuário ou persistência; versões exploratórias ficam como histórico da decisão
- external-dependency failure: C6, C7, C9, C10 e C11 falham se os scripts da skill ou o Chrome não produzirem as saídas exigidas
- state transitions: n/a - o único estado é o checkpoint humano, coberto por C13 e C14, sem máquina de estados
- observability: C6 guarda auditorias; C7 e C9 guardam folhas HTML; C10 guarda renders; C11 guarda a prancha; `refinamentos.md` registra o julgamento

## Handoff

- S1 = ~5k; S2 = ~25k; S3 inclui imagens mas exige ~20k de contexto visual; total estimado ~50k, abaixo do orçamento de 150k
- Mechanism: one builder
- Base: `108f95e19455f5a7ac0cc92d94bc969aa8c7b8dc`


