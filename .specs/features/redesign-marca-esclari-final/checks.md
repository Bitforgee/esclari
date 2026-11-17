# Fio de procedência — marca final do Esclari checks

Profile: ui
Plan: `.specs/features/redesign-marca-esclari-final/plan.md`
Base: `108f95e19455f5a7ac0cc92d94bc969aa8c7b8dc`

23 checks em 5 slices · 4 portas de mão única · 0 perguntas abertas.

Comandos rodam em `D:\esclari`. Testes web usam `npm --prefix web test -- <arquivo> -t "<nome>"`; testes .NET usam o modo VSTest documentado no repositório e filtros por `FullyQualifiedName`; provas do kit usam `python .specs/features/redesign-marca-esclari-final/provar_marca_final.py -k test_CNN_`.

## Checks

### S1 - Símbolo e palavra-marca · ~8 arquivos · ~50 KB · ~15k

**C1** - `simbolo.svg` tem `viewBox="0 0 256 256"`, exatamente um `<path fill="currentColor">`, nenhum `stroke`, `mask`, `clipPath`, `filter`, gradient, transform, `<text>`, `<image>` ou hexadecimal, e o contorno preenchido mantém os extremos ópticos do centro de construção da door 1 com tolerância de 2 unidades (AC 1, door 1)
Proof: `npm --prefix web test -- src/test/logo.test.ts -t "simbolo final e um contorno preenchido"`

**C2** - `simbolo-pequeno.svg` tem `viewBox="0 0 256 256"`, exatamente um `<path fill="currentColor">`, nenhum elemento proibido de C1, área preenchida maior que o master, caixa horizontal e vertical menor que a do master e nenhum segmento ou vão menor que 8 unidades (AC 2, door 1)
Proof: `npm --prefix web test -- src/test/logo.test.ts -t "corte pequeno reforca peso e encurta pontas"`

**C3** - `palavra-marca.svg` contém sete letras minúsculas em contornos, `e`, `a`, `r` e `i` com terminais derivados da abertura arredondada do Fio, pingo monocromático e um vão de pelo menos 1 pixel entre `r` e `i` quando renderizada a 24 px; não contém `<text>`, `font-family`, `<image>`, URL ou dependência externa (AC 3, door 2)
Proof: `npm --prefix web test -- src/test/logo.test.ts -t "palavra-marca e propria e separa r de i"`

**C4** - Os oito SVGs de marca obtêm pelo menos 95/100 no `svg_audit.py`, sem `FAIL` nem `WARN`, e os PNGs de inspeção mostram símbolo e palavra legíveis em 16, 24, 32, 64 e 256 px, preto sobre branco e branco sobre preto (AC 4)
Proof: `python .specs/features/redesign-marca-esclari-final/provar_marca_final.py -k test_C04_auditoria_e_escala`

### S2 - Paleta e variantes · ~8 arquivos · ~50 KB · ~15k

**C5** - Os SVGs usam somente Origem `#C83A23`, Origem clara `#F16A4B`, Tinta `#15133A`, Papel `#F7F5EE`, preto, branco e `currentColor`, nos papéis da door 3 e sem gradient (AC 5, door 3)
Proof: `npm --prefix web test -- src/test/logo.test.ts -t "marca usa somente a paleta e os papeis aprovados"`

**C6** - `logo-horizontal.svg`, `logo-horizontal-escuro.svg` e `logo-vertical.svg` contêm o Fio e a palavra-marca, têm distância entre ambos igual à largura expandida do Fio com tolerância de 2 unidades e alinham o símbolo opticamente ao corpo da palavra (AC 6)
Proof: `npm --prefix web test -- src/test/logo.test.ts -t "lockups compartilham simbolo palavra e espacamento"`

**C7** - `logo-preto.svg` usa somente `#000000`, `logo-branco.svg` usa somente `#FFFFFF`, e ambos contêm exatamente os mesmos contornos do lockup horizontal, sem perder símbolo ou letra (AC 7)
Proof: `npm --prefix web test -- src/test/logo.test.ts -t "logos preto e branco preservam o lockup"`

**C8** - As razões WCAG calculadas são no mínimo 4,5:1: Origem/branco 5,14; Origem clara/Tinta 5,82; Tinta/branco 17,70; Tinta/Papel 16,23 (AC 8)
Proof: `npm --prefix web test -- src/test/logo.test.ts -t "paleta cumpre os quatro contrastes"`

### S3 - Favicon, PWA e fallbacks · ~10 arquivos · ~60 KB · ~20k

**C9** - Uma execução de `npm --prefix web run icones` gera os quatro PNG nas dimensões 192×192, 512×512, 512×512 e 180×180, todos com corte pequeno branco sobre `#C83A23`; a segunda execução gera hashes byte a byte idênticos (AC 9, door 4)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "gera quatro icones deterministas com o fio pequeno"`

**C10** - No PNG recém-gerado, cada pixel fora da placa de `icon-maskable-512.png` fica a no máximo 204,8 px do centro, e todos os pixels do maskable e apple-touch têm alfa 255 (AC 10)
Proof: `npm --prefix web test -- src/test/icones.test.ts -t "icone gerado fica opaco e dentro da zona segura"`

**C11** - Renderizado em 16×16, `favicon.svg` mantém uma região branca interna e duas extremidades brancas separadas por ao menos um pixel da placa Origem, sem fechar a volta do Fio (AC 11)
Proof: `npm --prefix web test -- src/test/logo.test.ts -t "favicon preserva volta e extremidades em 16 px"`

**C12** - Com logo, ícone e cor da instituição configurados, login e sidebar usam o logo institucional; favicon, manifesto e push usam o ícone institucional; e-mail e interface usam a cor institucional; nenhum deles mostra ou aplica os fallbacks Esclari (AC 12)
Proof: `npm --prefix web test -- src/acesso/logo-esclari.test.tsx src/administracao/marca.test.tsx src/aparencia.test.tsx -t "identidade da instituicao prevalece"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailMarcaTests|FullyQualifiedName~ManifestoTests.Manifesto_com_aparencia|FullyQualifiedName~PushAgendadorTests.Payload_exato_com_icone"`

**C13** - Sem cor da instituição, o cabeçalho HTML do e-mail usa `background-color:#C83A23` com `color:#ffffff`, e o manifesto devolve `theme_color` `#C83A23`; com cor configurada, os dois devolvem a cor configurada (AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailMarcaTests|FullyQualifiedName~ManifestoTests"`

**C14** - `CoresProntas` mostra o preset `Azul` com valor `#224fa7`, não mostra “Azul Esclari”, e os fallbacks `--marca`/`--primary` da interface continuam equivalentes a `#224fa7` (AC 14)
Proof: `npm --prefix web test -- src/administracao/aparencia/guidelinesCores.test.tsx src/administracao/aparencia/aparenciaAbas.test.tsx src/aparencia.test.tsx -t "preset Azul mantem o valor sem o sobrenome Esclari"`

### S4 - Kit e guia · ~20 arquivos · ~3 MB · ~40k

**C15** - O kit contém os oito masters SVG nomeados na Surface e PNGs transparentes de símbolo, corte pequeno, palavra-marca, horizontal, vertical, preto e branco nos tamanhos aplicáveis de 256, 512 e 1200 px (AC 15, door 4)
Proof: `python .specs/features/redesign-marca-esclari-final/provar_marca_final.py -k test_C15_kit_de_masters_e_pngs`

**C16** - `apresentacao.png` e `apresentacao.html` mostram exatamente seis aplicações nomeadas: aba do navegador, ícone PWA, cabeçalho de e-mail, capa de apresentação, co-branding com “Instituição Horizonte” e assinatura “Atendimento por Esclari” (AC 16)
Proof: `python .specs/features/redesign-marca-esclari-final/provar_marca_final.py -k test_C16_seis_aplicacoes`

**C17** - `guia-de-marca.md` contém as seções Ideia, Arquivos, Paleta, Contraste, Área de respeito, Tamanho mínimo, Fundos, Co-branding, Usos proibidos, Tipografia e Impressão; registra HEX, RGB e CMYK aproximado das quatro cores e não atribui Pantone sem prova física (AC 17)
Proof: `npm --prefix web test -- src/test/guia.test.ts -t "guia final cobre entrega uso e impressao"`

**C18** - O guia fixa área de respeito em metade da largura expandida do Fio e mínimos de 16 px para corte pequeno, 24 px para master, 96 px para horizontal e 72 px para vertical (AC 18)
Proof: `npm --prefix web test -- src/test/guia.test.ts -t "guia final fixa quatro tamanhos minimos"`

### S5 - Aplicação e invariantes · ~8 arquivos · ~30 KB · ~10k

**C19** - A raiz usa `logo-horizontal.svg` no tema light e `logo-horizontal-escuro.svg` nos temas dark e black, com `alt="Esclari"` e nome acessível do `h1` igual a “Esclari” (AC 19)
Proof: `npm --prefix web test -- src/acesso/logo-esclari.test.tsx -t "raiz troca o logo no tema escuro|raiz mostra o logo dentro do h1"`

**C20** - Em 360×780, a raiz mostra o novo logo com 40 px de altura e `scrollWidth` no máximo 360 (AC 20)
Proof: `$env:E2E_URL='http://127.0.0.1:5173'; npm --prefix web run e2e -- e2e/marca.spec.ts -g "raiz em celular 360x780"`

**C21** - Em 844×390, a raiz mostra o novo logo e os dois parágrafos com `scrollHeight` no máximo 390 (AC 21)
Proof: `$env:E2E_URL='http://127.0.0.1:5173'; npm --prefix web run e2e -- e2e/marca.spec.ts -g "raiz deitada 844x390"`

**C22** - `GET /marca/simbolo.svg`, `/marca/simbolo-pequeno.svg`, `/marca/palavra-marca.svg`, `/marca/logo-horizontal.svg`, `/marca/logo-horizontal-escuro.svg`, `/marca/logo-vertical.svg`, `/marca/logo-preto.svg`, `/marca/logo-branco.svg`, `/marca/favicon.svg`, `/icons/apple-touch-icon.png`, `/icons/icon-192.png`, `/icons/icon-512.png` e `/icons/icon-maskable-512.png` respondem `200` com `image/svg+xml` nos 9 SVGs e `image/png` nos 4 PNGs (Surface)
Proof: `$env:E2E_URL='http://127.0.0.1:5173'; npm --prefix web run e2e -- e2e/marca.spec.ts -g "arquivos da marca final respondem 200"`

**C23** - `dotnet build Esclari.slnx`, testes .NET tocados, `npm --prefix web run build`, testes web tocados, `crapper` abaixo de 10 e `dryer` sem candidato a 0,8 terminam com código 0; mutação fica registrada como suspensa por AD-025 (AC 22)
Proof: `dotnet build Esclari.slnx; dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailMarcaTests|FullyQualifiedName~ManifestoTests|FullyQualifiedName~PushAgendadorTests.Payload_exato"; npm --prefix web test -- src/test/logo.test.ts src/test/icones.test.ts src/test/guia.test.ts src/acesso/logo-esclari.test.tsx src/administracao/marca.test.tsx src/administracao/aparencia/guidelinesCores.test.tsx src/administracao/aparencia/aparenciaAbas.test.tsx src/aparencia.test.tsx; npm --prefix web run build; npm --prefix web run coverage; Push-Location web; try { crapper --source-root src --use-existing-coverage --threshold 10; dryer --threshold 0.8 } finally { Pop-Location }`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| critérios do plano (22) | AC1 C1 · AC2 C2 · AC3 C3 · AC4 C4 · AC5 C5 · AC6 C6 · AC7 C7 · AC8 C8 · AC9 C9 · AC10 C10 · AC11 C11 · AC12 C12 · AC13 C13 · AC14 C14 · AC15 C15 · AC16 C16 · AC17 C17 · AC18 C18 · AC19 C19 · AC20 C20 · AC21 C21 · AC22 C23 | - |
| portas Landing (4) | símbolo C1,C2,C4,C11 · palavra C3,C4,C6 · paleta C5,C7,C8,C13,C14 · arquivos C9,C15,C22 | - |
| SVGs públicos (9) | símbolo C1,C4,C22 · pequeno C2,C4,C22 · palavra C3,C4,C22 · horizontal C4-C7,C19,C22 · horizontal escuro C4-C7,C19,C22 · vertical C4-C6,C15,C22 · preto C4,C5,C7,C15,C22 · branco C4,C5,C7,C15,C22 · favicon C4,C5,C11,C22 | - |
| PNGs públicos (4) | 192 C9,C22 · 512 C9,C22 · maskable C9,C10,C22 · apple C9,C10,C22 | - |
| contrastes (4) | Origem/branco C8 · Origem clara/Tinta C8 · Tinta/branco C8 · Tinta/Papel C8 | - |
| contextos da identidade institucional (6) | login C12 · sidebar C12 · favicon C12 · manifesto/push C12 · e-mail C12 · interface C12 | - |
| aplicações da prancha (6) | aba C16 · PWA C16 · e-mail C16 · apresentação C16 · co-branding C16 · assinatura C16 | - |
| tamanhos mínimos do guia (4) | pequeno 16 C18 · master 24 C18 · horizontal 96 C18 · vertical 72 C18 | - |
| temas da raiz (3) | light C19 · dark C19 · black C19 | - |
| viewports (2) | 360×780 C20 · 844×390 C21 | - |
| Surface statuses (13) | 200 símbolo C22 · 200 pequeno C22 · 200 palavra C22 · 200 horizontal C22 · 200 horizontal escuro C22 · 200 vertical C22 · 200 preto C22 · 200 branco C22 · 200 favicon C22 · 200 apple C22 · 200 192 C22 · 200 512 C22 · 200 maskable C22 | - |
| Relations entities (0) | `None - no stored-data shape change` | - |

- C12 e C13 afirmam valores numa fronteira e suas provas cruzam a tela ou a API.
- C4 e C16 combinam prova estrutural reproduzível com inspeção visual obrigatória do Verifier; arquivo existente sozinho não conta como PASS visual.

## Test policy

| Code | Required proofs | Coverage expectation |
| --- | --- | --- |
| SVG e PNG, decidem forma e legibilidade | prova estrutural no próprio nível, renderização e inspeção visual independente | cada ativo final, cada tamanho e cada fundo enumerado |
| gerador de ícones, decide escala, cor e escrita | spawn no próprio nível | duas execuções idênticas, dimensões, cor, opacidade e zona segura no resultado recém-gerado |
| fallback API, decide cor e ícone | integração no limite HTTP/e-mail | ausência e presença da configuração institucional |
| seleção de tema e preset, decide valor visível | teste no nível da tela | light/dark/black e nome+valor do preset |
| guia e apresentação, documentos | parser no próprio arquivo e inspeção visual | cada seção, medida e aplicação enumerada |

Evidence:

- `gerar-icones.mjs` contém decisões de entrada, escala por modo e composição da placa; fica provado no próprio nível por C9 e C10;
- `LayoutEmail` e `ManifestoInstituicao` escolhem fallback ou cor institucional; C12 e C13 provam os dois ramos;
- `Acesso.tsx` escolhe o logo por tema; C19 prova os três valores;
- `provar_marca_final.py` mede estrutura e presença, mas não substitui o julgamento visual do Verifier.

Cost: 10 provas web focadas, 2 filtros .NET, 4 provas do kit, 3 E2E e os gates finais. As regras valem somente para esta feature.

## Swept

- validation: C1-C11, C15-C18 validam geometria, arquivos, cores, contraste, tamanhos, zona segura e documentos
- failure modes: existing - o gerador sai 1 antes de escrever quando símbolo, `theme_color` ou Chromium falta; C23 mantém os testes existentes desses ramos
- idempotency: C9 prova duas execuções e hashes idênticos
- authorization: n/a - ativos são estáticos públicos; fallbacks respeitam a instituição autenticada/configurada em C12
- concurrency: n/a - o gerador local não roda como serviço e troca cada PNG por rename atômico existente
- data lifecycle: n/a - nenhum dado persistido muda; SVG e PNG são versionados no Git
- external-dependency failure: existing - Chromium ausente termina 1 e deixa ícones intactos; C23 mantém a prova existente
- state transitions: n/a - sem máquina de estados; tema e presença da identidade são estados de leitura cobertos por C12 e C19
- observability: C4 guarda auditorias e folhas de escala; C9 imprime cada arquivo; C16 guarda prancha HTML/PNG; C23 mostra saída dos gates

## Handoff

- S1 ~15k + S2 ~15k + S3 ~20k + S4 ~40k + S5 ~10k = ~100k, abaixo do orçamento de 150k
- Mechanism: one builder
- Base: `108f95e19455f5a7ac0cc92d94bc969aa8c7b8dc`
- **Settled mid-build:** as provas C20-C22 usam o script existente `npm --prefix web run e2e -- ...`; `npm --prefix web exec playwright ...` executa fora de `web/`, não lê `playwright.config.ts` e deixa `page.goto('/')` sem `baseURL`. O valor e a asserção dos checks não mudaram. No C23, `crapper` lê a cobertura já produzida e limita a análise a `web/src`; sem `--source-root src`, ele inclui `e2e/`, fora do gate definido no `AGENTS.md`. O primeiro contorno da palavra partiu diretamente dos glifos Public Sans e recebeu 424 âncoras; para cumprir C4, as sete letras foram redesenhadas na mesma altura-x e peso, expandidas e reduzidas a 159 âncoras, com auditoria 99/100.
- **Blocked:** C12, C13 e a parte .NET do C23 aguardam Postgres, Mailpit e MinIO. `docker compose up` falhou porque o Docker não está instalado/rodando; o filtro encontrou 11 testes, todos falharam no setup antes de executar asserções.
