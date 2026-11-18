# QR code do aplicativo autenticador verification

**Verdict**: PASS
**Profile**: standard
**Diff range**: a12e949..cf1d25c
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

## Binding sources

None - no plan.md, no binding source (profile `standard`, not `ui`).

## Checks

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | `img` "QR code para o aplicativo autenticador" com `src` data:image/svg+xml do `QRCode.toString(uri)`; chave `JBSW Y3DP EHPK 3PXP` | `npm --prefix web test -- src/plataforma/entrada.test.tsx -t "qr code da uri e chave em grupos de 4"` exit 0, 1 passed 12 skipped | `web/src/plataforma/entrada.test.tsx:91` - `expect(qr).toHaveAttribute('src', esperado)` (esperado = `'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(await QRCode.toString(Uri, { type: 'svg' }))`); `:92` - `getByText('JBSW Y3DP EHPK 3PXP')` | PASS |
| C2 | só requisições `/api/plataforma/...` depois do QR | `... -t "qr code sem chamada de rede"` exit 0, 1 passed | `entrada.test.tsx:100` - `expect(chamadas.length).toBeGreaterThan(0)`; `:101` - `expect(chamadas.filter((c) => !c.url.startsWith('/api/plataforma/'))).toEqual([])`. Código: `Entrada.tsx` `QrCode` usa só `QRCode.toString` + `data:` URI, sem URL externa (`git diff a12e949 HEAD -- web/src/plataforma/Entrada.tsx`) | PASS |
| C3 | link "Abrir no aplicativo" ausente em ponteiro fino, presente com `href` = uri em `(pointer: coarse)` | `... -t "link otpauth so em aparelho de toque"` exit 0, 1 passed | `entrada.test.tsx:110` - `expect(screen.queryByRole('link', { name: 'Abrir no aplicativo' })).not.toBeInTheDocument()`; `:116` - `expect(await screen.findByRole('link', { name: 'Abrir no aplicativo' })).toHaveAttribute('href', Uri)`. Código `Entrada.tsx`: `useConsulta('(pointer: coarse)')` e `etapa.uri && toque && (<a ...>` | PASS |
| C4 | etapa `CODIGO`: sem QR nem chave | `... -t "codigo sem qr code"` exit 0, 1 passed | `entrada.test.tsx:123` - `queryByRole('img', { name: 'QR code para o aplicativo autenticador' })` `.not.toBeInTheDocument()`; `:124` - `queryByLabelText('Chave do aplicativo')` `.not.toBeInTheDocument()` | PASS |
| C5 | `qrcode` com versão exata `1.5.4`; suíte de interface aceita | `npm --prefix web test -- src/test/interface.test.ts -t "versões exatas e pacotes proibidos"` exit 0, 1 passed 30 skipped | `web/package.json` diff: `"qrcode": "1.5.4"` (e `"@types/qrcode": "1.5.6"`); teste em `web/src/test/interface.test.ts:171` | PASS |

Nota C2: o stub de `fetch` só enxerga chamadas feitas por `api`; ele não pegaria um `<img src="https://...">`. Quem fecha esse buraco é C1 (o `src` tem de ser o `data:` URI gerado localmente) e a leitura do código (sem URL externa). Não é falha, é um limite da prova.

## Coverage

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| etapas da entrada (2) | `type Etapa` em `Entrada.tsx` (CODIGO e CADASTRAR) | `CADASTRAR` C1 C2 C3 · `CODIGO` C4 | - |
| tipos de ponteiro (2) | `(pointer: coarse)` em `Entrada.tsx` (matchMedia verdadeiro/falso) | fino C3 (`ponteiro(false)`, :105) · toque C3 (`ponteiro(true)`, :113) | - |

## Existing assertions changed

`git diff a12e949 HEAD -- web/src/plataforma/entrada.test.tsx`: no teste "entrada em tres etapas: sem 2FA mostra a chave em grupos de 4 e o link para o aplicativo" a única mudança é `const uri = Uri` e `ponteiro(true)` (como listado em `checks.md`); as asserções da chave e do `href` ficam intactas. Nenhuma linha removida além disso, nenhum teste apagado ou pulado. Nada enfraquecido.

## Faults injected

| Mutation | Location | Killed |
| --- | --- | --- |
| QR gerado do segredo em vez da `uri` (`QRCode.toString(uri.split('secret=')[1]...)`) | `web/src/plataforma/Entrada.tsx` (`QrCode`, queryFn) | yes - falha `qr code da uri e chave em grupos de 4` (1 failed, 12 passed) |
| link "Abrir no aplicativo" sempre visível (`etapa.uri && toque && (` -> `etapa.uri && (`) | `web/src/plataforma/Entrada.tsx` (`Cadastro`) | yes - falha `link otpauth so em aparelho de toque` (1 failed, 12 passed) |

Worktree descartado com `git worktree remove --force`; `git status --porcelain` da árvore principal igual ao de antes (só `?? .tmp-capturas/`).

## Gate

`cd web; npx vitest run` - 1151 passed, 0 failed (131 arquivos); `npx tsc -b --noEmit` exit 0
