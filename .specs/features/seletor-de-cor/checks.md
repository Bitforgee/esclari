# Seletor de cor junto do campo Cor principal checks

Profile: ui
Base: `b9c5cad`

## Intent

Na aba Marca da Aparência, o seletor de cor nativo (`<input type="color">`) fica hoje depois das 8 cores prontas, como um quadrado preto sem texto visível; o nome "Escolher cor" só existe para leitor de tela, e o quadrado é preto quando não há cor. O dono não o encontrou e pediu um seletor de cores (17/11/2026). Ele aprovou a proposta: o seletor vai para o lado do campo "Cor principal", como um botão com a amostra da cor atual e o texto visível "Escolher cor"; sem cor válida, a amostra fica neutra (vazia, com borda tracejada) em vez de preta; continua o seletor nativo do navegador, que funciona com toque e teclado. Nada muda no valor gravado, na validação do hexadecimal nem nas cores prontas.

Referências (parafraseadas): um seletor de cor deve ter rótulo visível e mostrar a cor atual numa amostra com o hexadecimal ao lado ([HTMLInput.com, Color input](https://htmlinput.com/color-input)); o padrão comum junta a amostra do `<input type="color">` a um campo de hexadecimal com o mesmo valor ([Codefronts, Color picker input](https://codefronts.com/components/css-input-fields/color-picker/); [Once UI, ColorInput](https://docs.once-ui.com/form-controls/colorInput)); o `<input type="color">` só aceita `#rrggbb` e cai em preto com qualquer outro valor ([MDN](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/input/color); [Eddie Design System](https://ds.bradfrost.com/components/color-picker/)), por isso a amostra neutra não pode depender do valor do input.

7 checks em 1 slice · 0 portas de mão única · 0 perguntas abertas.

Comandos rodam em `D:\esclari`; Vitest com o PowerShell em `C:\` (`npm --prefix D:\esclari\web test -- <arquivo> -t "<nome>"`); E2E com `docker compose up -d --build api` e `npm --prefix web run e2e -- e2e/<arquivo> -g "<nome>"`.

## Checks

### S1 - Seletor ao lado do campo · 4 arquivos · ~60 KB · ~15k

**C1** - Na aba Marca, o `<input type="color">` de nome acessível "Escolher cor" fica na mesma linha do campo "Cor principal" (os dois têm o mesmo elemento pai) e fora do grupo "Cores prontas"; o texto "Escolher cor" aparece visível dentro do `<label>` que envolve o input
Proof: `npm --prefix web test -- src/administracao/aparencia/aparenciaAbas.test.tsx -t "seletor de cor ao lado do campo Cor principal"`

**C2** - Com cor válida `#8a1538` digitada, a amostra do botão tem `background-color` `#8a1538`; com o campo vazio ou com hexadecimal inválido (`#12`), a amostra não tem `background-color` e tem `border-dashed`
Proof: `npm --prefix web test -- src/administracao/aparencia/aparenciaAbas.test.tsx -t "amostra do seletor mostra a cor ou fica neutra"`

**C3** - Escolher `#ffff00` no seletor preenche "Cor principal" com `#ffff00`, mostra o contraste em vermelho e desabilita Salvar (comportamento de hoje, mantido)
Proof: `npm --prefix web test -- src/administracao/aparencia/aparenciaAbas.test.tsx -t "cor sem contraste bloqueia o Salvar"`

**C4** - Pelo teclado, a ordem é: cor marcada do grupo, "Cor principal", "Escolher cor"
Proof: `npm --prefix web test -- src/administracao/aparencia/guidelinesCores.test.tsx -t "C25 Tab atravessa o grupo numa só parada"`

**C5** - Num Chromium real, a 360×780 e a 1280×800, o botão "Escolher cor" fica na mesma linha do campo "Cor principal" (centros verticais a no máximo 2 px), não passa da largura da página, e a 360×780 tem pelo menos 44 px de altura; clicar no texto "Escolher cor" foca o input de cor
Proof: `npm --prefix web run e2e -- e2e/seletor-de-cor.spec.ts -g "seletor ao lado do campo"`

**C6** - Na varredura de cor de ação, a amostra do seletor não conta como uso indevido da cor de ação (atributo `data-amostra`)
Proof: `npm --prefix web run e2e -- e2e/ux.spec.ts -g "cor de ação só em botão principal, item ativo, link e foco"`

**C7** - Build web, Vitest inteiro, `crapper` abaixo de 10 em `web/src` e `dryer` sem candidato nas fatias saem verdes
Proof: `npm --prefix web run build`
Proof: `npm --prefix web test -- --maxWorkers=4`
Proof: `npm --prefix web run coverage`
Proof: `Push-Location web; crapper --source-root src --use-existing-coverage --threshold 10; $c = $LASTEXITCODE; Pop-Location; exit $c`
Proof: `python .specs/features/interface-marca-esclari/provar_interface.py -k test_C21_dryer`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| estados da amostra (3) | cor válida C2 · campo vazio C2 · hexadecimal inválido C2 | - |
| entradas do seletor (3) | clique C5 · teclado C4 · troca de valor C3 | - |
| tamanhos (2) | 360×780 C5 · 1280×800 C5 | - |

## Test policy

O repositório já responde: telas da Aparência são provadas em Vitest dentro da fatia (`aparenciaAbas.test.tsx`), e arranjo medido em Chromium real com a API simulada (`web/e2e/simulada.ts`). Nenhuma linha nova.

| Code | Required proofs | Coverage expectation |
| --- | --- | --- |
| `SeletorDeCor` (decide: amostra com cor ou neutra) | uma na própria tela | cor válida, vazia e inválida |

Evidence:

- `CoresProntas.tsx`: o input de cor só repassa o valor para `escolher`; a única decisão nova é a amostra (1 condição)

## Swept

- validation: existing - `CorValida` e o bloqueio de contraste continuam (C3)
- failure modes: C2 - hexadecimal inválido deixa a amostra neutra
- idempotency: n/a - só apresentação
- authorization: existing - a aba Aparência continua só para administrador
- concurrency: n/a - estado local do formulário
- data lifecycle: n/a - nada gravado muda
- dependency failure: n/a - sem dependência externa
- state transitions: C2 - vazio para cor válida
- observability: n/a - nenhuma exigência de log

## Handoff

- S1 ≈ 15k (CoresProntas.tsx 3 KB, Aparencia.tsx 16 KB, aparenciaAbas.test.tsx 20 KB, guidelinesCores.test.tsx 3 KB, e2e novo), abaixo do budget de 150k - um builder
- Mecanismo: um builder
- **Boundary:** C1-C7 construídos; provas do autor: Vitest 1077/1077, build web exit 0, cobertura exit 0, `crapper` exit 0, `test_C21_dryer` OK, E2E `seletor-de-cor`, `ux`, `aparencia` e `guidelines` 71/71 contra :8080. Inspeção: a 360×780 e a 1280×800, claro e escuro, o botão fica ao lado do campo, com a amostra da cor ou neutra.
- **Settled mid-build:** o input de cor fica invisível (`opacity-0`) sobre o `<label>` inteiro, para que toque e clique em qualquer ponto abram o seletor; o anel de foco vai para o `<label>` (`has-[:focus-visible]`). Sem cor válida, o input continua em `#000000` (o `<input type=color>` exige `#rrggbb`), mas não aparece.
- **Abandoned:** nada.
