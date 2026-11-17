# Filtros na lista de chamados e relatório em CSV - verificação

**Verdict**: FAIL
**Profile**: ui
**Diff range**: 7005230..bb4bcb6
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier); não escreveu o código nem os checks

Os 62 checks passam com prova localizada no `HEAD` (`bb4bcb6`), e os 11 mutantes injetados morreram. O veredito é FAIL por cobertura, não por defeito observado: a seção "Telas" do plano (binding) decide quatro coisas que nenhum check prova (painel à direita a partir de 768 px, a barra e o painel na tela Chamados, a ordem barra → etiquetas/contagem → tabela, a ordem dos campos do painel), a contagem mostra um texto que o desenho não tem, e o recálculo da cobertura acha membros sem prova (rótulos de estado e motivo no CSV, o `\` escapado da porta 3, `BaixarAnexo` em `LEITURA`, 400 de parâmetro inválido em `exportar`). As capturas da seção Binding sources mostram que o código faz o que o desenho pede nos quatro pontos de arranjo. O que falta são as provas, e uma regressão nesses pontos passaria pela suíte.

**Emenda E3 sem confirmação do dono.** O C62 foi julgado contra o texto emendado pelo E3, que o próprio `checks.md` marca como "pede o 'ok' do dono". Esse "ok" não está no `checks.md` nem no AD-066 do `STATE.md`. Se o dono recusar o E3, o C62 volta ao texto original ("os e2e ... passam") e é FAIL, porque 15 testes falham no `HEAD`.

## Binding sources

Fontes binding no perfil `ui`: a seção "Telas" do `## Surface` do `plan.md` e os critérios (AC 18-29, 41, 46-52). Abri as duas e comparei com as telas reais em Playwright (API simulada por `e2e/simulada.ts`, servidor do compose em :8080 com a imagem do `HEAD`). As capturas ficaram fora do repositório, em `D:\esclari-verif\`: `fila-1280.png`, `fila-1280-painel.png`, `fila-360.png`, `fila-360-painel.png`, `chamados-1280.png`, `chamados-1280-painel.png`, `chamados-360.png`, `chamados-360-painel.png`. Caixa do painel medida: 1280 px → `{x:896, y:0, width:384, height:800}` (à direita, altura toda); 360 px → `{x:0, y:117, width:360, height:663}` (de baixo).

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| Telas 1 `/:slug/fila` (barra, etiquetas + "Limpar filtros", contagem, tabela) | sim - `plan.md` Surface/Telas; captura `fila-1280.png`, `fila-360.png` | nenhuma | arranjo: nenhum check prova que etiquetas e contagem ficam entre a barra e a tabela (C23 só prova barra antes da tabela, `filtros.test.tsx:40`; C29 não verifica posição, apesar do AC 24 dizer "acima da tabela"); a barra em uma linha a partir de 768 px não tem check (C54 só mede 360 px); a contagem mostra "1 chamado · Mais antigos primeiro" (`Chamados.tsx`, `Contagem`), e o desenho só tem "N chamados" |
| Telas 2 `/:slug/chamados` ("a mesma tela" com escopo `todos`, coluna "Setor", título "Chamados") | sim - captura `chamados-1280.png`, `chamados-360.png` | nenhuma | C52 prova título, colunas, vazio e as duas URLs, mas nenhum check prova a barra (busca, "Filtros", "Exportar CSV") nem o painel nessa tela (AC 47 "a mesma barra, painel e tabela") |
| Telas 3 painel "Filtros" (Sheet à direita a partir de 768 px e de baixo abaixo disso; campos Estado, Setor, Categoria, Responsável, Período, Ordem; rodapé "Limpar"/"Aplicar") | sim - captura `fila-1280-painel.png`, `fila-360-painel.png` | nenhuma | "à direita a partir de 768 px" sem check (C55 prova só o de baixo a 360; C56 a 844×390 mede rolagem, não o lado); a ordem dos 6 campos sem check (C25 prova presença e as opções, não a ordem) |
| Critérios AC 18-29, 41, 46-52 | sim - `plan.md` Criteria | nenhuma | coberto pelas linhas acima; o resto tem check (C23-C34, C46, C51-C58) |

Comparação dos checks contra os critérios: nenhum check contradiz um rótulo, contagem ou status da fonte. Observação de precisão (não contradição): C30 afirma a frase "Nenhum chamado na fila dos seus setores." e o Observable fala em "Fila em dia"; as duas são o mesmo estado vazio existente (título e frase, `Chamados.tsx`, `Fila`), mas o título não é afirmado.

## Checks

Provas rodadas em uma invocação por alvo, no `HEAD`:
- API: `dotnet test Esclari.Api.Tests --logger "trx;LogFileName=D:\esclari-verif\api-full.trx"` → `Aprovado! – Com falha: 0, Aprovado: 1069, Ignorado: 0, Total: 1069`. Cada teste nomeado foi lido do TRX individualmente com `outcome=Passed` (lista abaixo, 42 nomes; `Parametro_invalido_400` x6, `Aspas_quando_precisa` x6, `Prefixo_contra_formula` x7 casos).
- Vitest: `cd web && npx vitest run --reporter=json` (o script `npm test` é `vitest run`) → `total=1095 passed=1095 failed=0`; os 19 títulos nomeados nos checks aparecem cada um com `passed`.
- Playwright: `npx playwright test e2e/filtros.spec.ts` → `4 passed`; os 5 specs do C62 → `15 failed, 175 passed`.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | busca sem filtro = fila, 4 estados, sem FECHADO nem outro setor | TRX `BuscaDeChamadosTests.Sem_filtro_igual_a_fila` Passed | `Esclari.Api.Tests/BuscaDeChamadosTests.cs:47` `Assert.Equal(fila, busca)`; `:48` `Assert.Equal([aberto, atendimento, aguardando, resolvido], busca)` | PASS |
| C2 | cada um dos 5 estados; ABERTO+FECHADO somam | TRX `Estado_filtra_cada_um`, `Estado_repetido_soma` Passed | `BuscaDeChamadosTests.cs:61` `Assert.Equal([numero], ...$"&estado={estado}")` sobre os 5; `:72` `Assert.Equal([aberto, fechado], ..."&estado=ABERTO&estado=FECHADO")` | PASS |
| C3 | `matricula` e `MATRÍCULA` acham "Trancar a Matrícula", não "Boleto" | TRX `Busca_no_titulo_sem_acento_e_sem_caixa` Passed | `BuscaDeChamadosTests.cs:82`, `:83` `Assert.Equal([matricula], ...)`; `:84` `Assert.DoesNotContain(boleto, ...)` | PASS |
| C4 | 3 últimos dígitos e `esc-000` acham pelo número | TRX `Busca_no_numero` Passed | `BuscaDeChamadosTests.cs:94` `Assert.Equal([segundo], ..."&q=" + segundo[^3..])`; `:96` `Assert.Equal([primeiro, segundo], ..."&q=esc-000")` | PASS |
| C5 | parte do nome sem acento acha só os do solicitante | TRX `Busca_no_nome_do_solicitante` Passed | `BuscaDeChamadosTests.cs:111` `Assert.Equal([dele], achados)` (busca `goncalves` para "João Gonçalves Teste", `:109`); `:112` `Assert.DoesNotContain(doOutro, achados)` | PASS |
| C6 | `%` e `_` dão total 0; `50%` acha só o desconto | TRX `Curinga_e_texto_comum` Passed | `BuscaDeChamadosTests.cs:122` `Assert.Equal(0, ...EscapeDataString("%")...total)`; `:123` idem `q=_`; `:126` `Assert.Equal([desconto], ..."50%")` | PASS |
| C7 | categoria atual; reclassificado muda de categoria | TRX `Filtra_por_categoria_atual` Passed | `BuscaDeChamadosTests.cs:139` `Assert.Equal([reclassificado], ...categoriaId={financeiro.Id})`; `:140` `Assert.Equal([original], ...categoriaId={padrao.Id})` | PASS |
| C8 | `setorId` de atendente com 2 setores | TRX `Filtra_por_setor` Passed | `BuscaDeChamadosTests.cs:152` `Assert.Equal([noOutro], ...)`; `:153` `Assert.Equal([naTriagem], ...)` | PASS |
| C9 | setor alheio: 403 "Este setor não é seu." em busca e exportar | TRX `Setor_alheio_403` Passed | `BuscaDeChamadosTests.cs:165` `Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode)`; `:166` `Assert.Equal("Este setor não é seu.", ...detail)` em `Rotas[..2]` (busca, exportar) | PASS |
| C10 | `responsavel=eu` e `ninguem` | TRX `Responsavel_eu_e_ninguem` Passed | `BuscaDeChamadosTests.cs:179` `Assert.Equal([meu], ..."&responsavel=eu")`; `:180` `Assert.Equal([semNinguem], ..."&responsavel=ninguem")` | PASS |
| C11 | 03:30Z de 11/11 vem com 10/11 em Cuiabá, não com 11/11 | TRX `Periodo_no_fuso_da_instituicao` Passed | `BuscaDeChamadosTests.cs:192` `Assert.Equal([noite], ..."&de=2026-11-10&ate=2026-11-10")`; `:193` `Assert.Equal([meiaNoite], ..."&de=2026-11-11&ate=2026-11-11")` | PASS |
| C12 | `de` > `ate` → 400 com `errors.de` | TRX `De_depois_de_ate_400` Passed | `BuscaDeChamadosTests.cs:204` `Assert.True(...GetProperty("errors").TryGetProperty("de", out _))` | PASS |
| C13 | `recentes` invertido; `antigos` = ausente | TRX `Ordem_recentes_e_antigos` Passed | `BuscaDeChamadosTests.cs:218` `Assert.Equal([novo, medio, velho], ..."&ordem=recentes")`; `:219` `Assert.Equal([velho, medio, novo], ..."&ordem=antigos")` | PASS |
| C14 | 51 chamados: 50/1/0, total 51, porPagina 50 | TRX `Pagina_de_50_com_total` Passed | `BuscaDeChamadosTests.cs:233` `Assert.Equal((1, 50), ...)`; `:235` `Assert.Equal((3, 0), ...)`; `:237` `Assert.Equal((51, 50), (total, porPagina))` | PASS |
| C15 | chaves exatas do envelope e do item, valores do chamado | TRX `Forma_da_resposta` Passed | `BuscaDeChamadosTests.cs:251` `Assert.Equal(["itens", "total", "pagina", "porPagina"], ...)`; `:253` `Assert.Equal(["numero", "titulo", "estado", "setor", "categoria", "responsavel", "criadoEm", "dias"], ...)`; `:261` `Assert.Equal(2, ...dias)` | PASS |
| C16 | 6 parâmetros inválidos → 400 com o campo; 100 + espaços → 200 | TRX `Parametro_invalido_400` x6 Passed, `Busca_de_100_caracteres_aparada_200` Passed | `BuscaDeChamadosTests.cs:279` `Assert.True(...errors.TryGetProperty(campo, out _), campo)` sobre os 6 `InlineData`; `:289` `Assert.Equal(HttpStatusCode.OK, r.StatusCode)` | PASS |
| C17 | aluno → 403 nas 3 rotas | TRX `Aluno_403` Passed | `BuscaDeChamadosTests.cs:298` `Assert.Equal(HttpStatusCode.Forbidden, ...)` em `Rotas` (3) | PASS |
| C18 | atendente com `todos` → 403 nas 3 | TRX `Todos_so_para_administrador` Passed | `BuscaDeChamadosTests.cs:307` `Assert.Equal(HttpStatusCode.Forbidden, ...rota + "?escopo=todos")` | PASS |
| C19 | sem sessão → 401 nas 3 | TRX `Sem_sessao_401` Passed | `BuscaDeChamadosTests.cs:316` `Assert.Equal(HttpStatusCode.Unauthorized, ...)` | PASS |
| C20 | outra instituição nunca em busca, exportar, filtros | TRX `Outra_instituicao_nunca_aparece` Passed | `BuscaDeChamadosTests.cs:333` `Assert.Equal([minha], Numeros(busca))`; `:336` uma linha de dados no CSV; `:337` `Assert.DoesNotContain(filtros...setores, ...outra.Outro.Id || ...outra.Triagem.Id)` | PASS |
| C21 | filtros do atendente: setores dele, categorias ativas por nome, `{id, nome}` | TRX `Filtros_do_atendente` Passed | `BuscaDeChamadosTests.cs:353` `Assert.Equal(["Acesso ao portal", "Boleto", "Não classificado"], ...)` (sem "Antiga" inativa); `:354` `Assert.Equal(["id", "nome"], ...)` | PASS |
| C22 | `unaccent` instalada; modelo sem mudança pendente | TRX `Extensao_unaccent_instalada`, `ArquiteturaTests.Modelo_sem_mudanca_pendente` Passed | `BuscaDeChamadosTests.cs:362` `Assert.Equal(1, instalada)`; `Esclari.Api.Tests/ArquiteturaTests.cs:759` `Assert.False(...HasPendingModelChanges())` | PASS |
| C23 | busca rotulada, "Filtros", "Exportar CSV" antes da tabela | Vitest `barra da fila` passed | `web/src/chamados/filtros.test.tsx:40` `expect(el.compareDocumentPosition(tabela) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()` para os 3; rótulo pelo `findByRole('searchbox', { name: 'Buscar por número, título ou aluno' })` | PASS |
| C24 | nada aos 299 ms, uma chamada aos 300 ms, `q=boleto` na URL | Vitest `busca 300 ms depois` passed | `filtros.test.tsx:50` `toBe(0)`; `:55` `toBe(1)`; `:56` `expect(params(roteador).get('q')).toBe('boleto')` | PASS |
| C25 | 5 caixas, Setor, Categoria, Responsável, Período, Ordem; "Fechado" não busca até Aplicar | Vitest `painel aplica em lote` passed | `filtros.test.tsx:64` `toEqual(['Aberto', 'Em atendimento', 'Aguardando aluno', 'Resolvido', 'Fechado'])`; `:67` Responsável `['Qualquer', 'Eu', 'Sem responsável']`; `:68` Ordem; `:69` grupo "Período de abertura"; `:75` `toBe(antes)` | PASS |
| C26 | Aplicar fecha, grava os 7 na URL e pede os 7 à API | Vitest `aplicar grava na url` passed | `filtros.test.tsx:96` `toEqual([['FECHADO'], '2', '5', 'eu', '2026-11-01', '2026-11-30', 'recentes'])`; `:97` `expect(buscas(chamadas, filtrada)).toBe(1)` | PASS |
| C27 | URL inicial pede os 4 filtros e preenche campo e painel | Vitest `url inicial preenche` passed | `filtros.test.tsx:104` `toBe(1)`; `:105` `toHaveValue('boleto')`; `:109` `toHaveDisplayValue('Sem responsável')`; `:110` `toHaveDisplayValue('Mais recentes primeiro')` | PASS |
| C28 | etiquetas, "Filtros (2)", remover uma, limpar todas | Vitest `etiquetas dos filtros` passed | `filtros.test.tsx:120` `toHaveTextContent('Estado: Fechado')`; `:122` `'Filtros (2)'`; `:126` `getAll('estado')).toEqual([])`; `:128` busca só com `responsavel=eu`; `:133` `toString()).toBe('')` | PASS |
| C29 | "73 chamados", "1 chamado", "0 chamados" | Vitest `contagem do total` passed | `filtros.test.tsx:148` `expect(await screen.findByText(texto)).toBeInTheDocument()` sobre os 3 pares de `:140` | PASS |
| C30 | vazio com filtro + "Limpar filtros" na linha; sem filtro a frase da fila | Vitest `vazio com e sem filtro` passed | `filtros.test.tsx:155` célula `td` com "Nenhum chamado com esses filtros." e o botão; `:157` `findByText('Nenhum chamado na fila dos seus setores.')` | PASS |
| C31 | total 60: pede `pagina=2`, 60 linhas, repetido uma vez | Vitest `mostrar mais pede a pagina seguinte` passed | `filtros.test.tsx:170` `toBe(1)`; `:171` `toHaveLength(60)`; `:172` `getAllByText('ESC-000050')).toHaveLength(1)` | PASS |
| C32 | `FilaMudou` refaz a busca com `estado=FECHADO` | Vitest `FilaMudou refaz a busca filtrada` passed | `web/src/chamados/tempoReal.test.tsx:81` `expect(leituras(chamadas, filtrada)).toBe(2)`; `:82` `leituras(chamadas, FILA)).toBe(0)` | PASS |
| C33 | 500 → aviso e "Tentar de novo" com os mesmos filtros | Vitest `erro com tentar de novo` passed | `filtros.test.tsx:181` `toHaveTextContent('Não foi possível carregar a fila. Verifique sua conexão.')`; `:184` `buscas(chamadas, url)).toBe(2)` | PASS |
| C34 | lista anterior visível com `aria-busy="true"`; 3 esqueletos na primeira carga | Vitest `mantem a lista enquanto carrega`, `lista carregando mostra 3 esqueletos e nada mais` passed | `filtros.test.tsx:195` `toHaveAttribute('aria-busy', 'true')`; `:199` `not.toHaveAttribute('aria-busy')`; `web/src/chamados/listas.test.tsx:83` `toHaveLength(3)` | PASS |
| C35 | `text/csv; charset=utf-8` e `filename="chamados-<slug>-2026-11-10.csv"` às 03:30Z de 11/11 | TRX `ExportarChamadosTests.Cabecalhos_do_arquivo` Passed | `Esclari.Api.Tests/ExportarChamadosTests.cs:49` `Assert.Equal("text/csv; charset=utf-8", ...)`; `:50` `Assert.Equal($"attachment; filename=\"chamados-{slug}-2026-11-10.csv\"", ...)` | PASS |
| C36 | BOM, cabeçalho literal, CRLF, 55 linhas na ordem `recentes` | TRX `Formato_do_arquivo` Passed | `ExportarChamadosTests.cs:65` `Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3])`; `:67` `EndsWith("\r\n")`; `:69` `Assert.Equal(Cabecalho, linhas[0])` (literal em `:15`); `:71` 55; `:72` `Assert.Equal(esperados, ...)` | PASS |
| C37 | valores de cada coluna do fechado; aberto com 3 vazias | TRX `Valores_de_cada_coluna` Passed | `ExportarChamadosTests.cs:96-97` `Assert.Equal([fechado, "Boleto em dobro", "Fechado", "Financeiro", "Financeiro", "Não classificado", ..., "Análise e Desenvolvimento de Sistemas", "3", "10/11/2026 23:30", l[10], ..., "Confirmado pelo aluno", "0", "1"], l)`; `:101` `Assert.Equal(("Aberto", "", "", ""), ...)` | PASS |
| C38 | primeira resposta pública da equipe, não nota nem aluno; vazia sem resposta | TRX `Primeira_resposta_da_equipe` Passed | `ExportarChamadosTests.cs:122` `Assert.Equal(Data(primeira), ...[10])`; `:123` `Assert.Equal("", ...[10])` | PASS |
| C39 | sem CPF, matrícula, nome, e-mail, texto de mensagem ou nota | TRX `Sem_dado_pessoal_do_aluno` Passed | `ExportarChamadosTests.cs:140` `Assert.DoesNotContain(proibido, csv)` sobre os 7 valores de `:139` | PASS |
| C40 | `;`, `"`, CR, LF entre aspas com `"` dobrada; simples sem aspas | TRX `CsvDeChamadosTests.Aspas_quando_precisa` x6 Passed | `Esclari.Api.Tests/CsvDeChamadosTests.cs:14` `Assert.Equal(esperado, CsvDeChamados.Celula(valor))` sobre os `InlineData` de `:8-13` | PASS |
| C41 | `'` antes de `=`, `+`, `-`, `@`, TAB, CR; `'=HYPERLINK(1)` no arquivo | TRX `Prefixo_contra_formula` x7, `ExportarChamadosTests.Titulo_com_formula_sai_como_texto` Passed | `CsvDeChamadosTests.cs:24` `Assert.Equal(esperado, ...)` sobre `:17-23`; `ExportarChamadosTests.cs:151` `Assert.Equal("'=HYPERLINK(1)", ...[1])` | PASS |
| C42 | 10 000 → 200 com 10 000 linhas; 10 001 → 400 com o detail | TRX `Limite_de_10000` Passed | `ExportarChamadosTests.cs:162` `Assert.Equal(10_000, linhas.Length - 1)`; `:167` `Assert.Equal("A exportação passa de 10.000 chamados. Refine os filtros.", ...detail)` | PASS |
| C43 | liderança → 403 com o detail; busca dela 200 | TRX `Lideranca_nao_exporta` Passed | `ExportarChamadosTests.cs:179` `Assert.Equal("A exportação não está disponível para a liderança de turma.", ...)`; `:180` `Assert.Equal(HttpStatusCode.OK, ...busca)` | PASS |
| C44 | 5 exportações 200, a sexta 429, com os limites reais | TRX `RateLimitTests.Seis_exportacoes_em_60s_429` Passed | `Esclari.Api.Tests/RateLimitTests.cs:57` `Assert.Equal(HttpStatusCode.OK, ...)` x5; `:59` `Assert.Equal(HttpStatusCode.TooManyRequests, ...)` | PASS |
| C45 | log `Information` com o modelo, ids, linhas, filtros, sem "boleto" | TRX `Log_da_exportacao_sem_o_texto` Passed | `ExportarChamadosTests.cs:195` `Assert.Equal(LogLevel.Information, log.Nivel)`; `:197` `Assert.Equal(2, log.Campos["Linhas"])`; `:199-200` `q`, `estado`; `:201` `DoesNotContain(... "boleto" ...)` | PASS |
| C46 | pede `exportar?escopo=fila&estado=FECHADO`, botão desabilitado com indicador, baixa com o nome; 400 vira aviso | Vitest `exporta com os filtros da tela`, `recusa vira aviso` passed | `web/src/chamados/exportar.test.tsx:48` `toBeDisabled()`; `:50` spinner `not.toBeNull()`; `:51` `toContain(Exportacao)`; `:56` `toEqual(['chamados-univag-2026-11-10.csv blob:csv'])`; `:68` `findByText('A exportação passa de 10.000 chamados. Refine os filtros.')` | PASS |
| C47 | administrador: 3 setores em busca e exportar com `todos`; `fila` total 0 | TRX `LeituraDoAdministradorTests.Todos_os_setores` Passed | `Esclari.Api.Tests/LeituraDoAdministradorTests.cs:33` `Assert.Equal([naTriagem, noFinanceiro, naSecretaria], ...)`; `:36` idem no CSV; `:37` `Assert.Equal(0, fila...total)` | PASS |
| C48 | filtros `todos` com todos os setores | TRX `Filtros_com_todos_os_setores` Passed | `LeituraDoAdministradorTests.cs:50` `Assert.Equal(3, todos.Count)`; `:51` `Assert.Equal(todos.Order(), ...setores ids...Order())` | PASS |
| C49 | detalhe 200 com nota, fontes, eventos e `acoes` vazio | TRX `Le_o_detalhe_sem_acoes` Passed | `LeituraDoAdministradorTests.cs:75` `Assert.Equal(HttpStatusCode.OK, ...)`; `:77` nota `NOTA-SECRETA`; `:79` fonte; `:81` eventos ≠ 0; `:82` `Assert.Empty(j.GetProperty("acoes")...)` | PASS |
| C50 | 403 com a frase nas 10 rotas POST; chamado intacto | TRX `Nenhuma_acao_em_leitura` Passed | `LeituraDoAdministradorTests.cs:96` `Assert.Equal(10, posts.Count)`; `:100` `Forbidden`; `:101` `Assert.Equal("Seu papel neste chamado não permite esta ação.", ...)`; `:104-105` estado, responsável, setor, categoria e eventos iguais. Conferi as 10 `MapPost("/chamados/{numero}/...")` em `Esclari.Api/Chamados/Acoes/` (assumir, cancelar, confirmar, encaminhar, duplicado, reclassificar, rejeitar, resolver, mensagens, subir) | PASS |
| C51 | "Chamados" no grupo Atendimento do administrador; ausente para atendente | Vitest `menu do administrador` passed | `web/src/chamados/chamadosAdmin.test.tsx:16` `toHaveAttribute('href', '/univag/chamados')`; `:22` `not.toBeInTheDocument()` | PASS |
| C52 | `/univag/chamados` pede busca e filtros `todos`, título, 7 colunas, vazio | Vitest `tela chamados` passed | `chamadosAdmin.test.tsx:28` `findByText('Nenhum chamado ainda.')`; `:29` `heading level 1 'Chamados'`; `:30-31` as 2 URLs; `:37` `toEqual(['Número', 'Título', 'Setor', 'Categoria', 'Responsável', 'Aberto', 'Estado'])` | PASS |
| C53 | `acoes` vazio: sem campo de resposta, sem Enviar nem botões de ação | Vitest `detalhe em leitura` passed | `chamadosAdmin.test.tsx:45` `querySelector('textarea')).toBeNull()`; `:46` os 5 botões `not.toBeInTheDocument()` | PASS |
| C54 | 360: campo na largura toda, botões abaixo, sem rolagem horizontal | Playwright `360 busca na largura toda` ✓ | `web/e2e/filtros.spec.ts:40` `expect(campo.width).toBeCloseTo(barra.width, 0)`; `:42` mesma linha; `:44` `scrollWidth).toBeLessThanOrEqual(360)` | PASS |
| C55 | 360: painel na borda de baixo, rodapé visível nos dois extremos, Fechado aplica | Playwright `360 painel de baixo com rodape fixo` ✓ | `filtros.spec.ts:51` `expect(p.y + p.height).toBeCloseTo(780, 0)`; `:54`, `:56` `rodapeVisivel`; `:63` `getAll('estado')).toEqual(['FECHADO'])` | PASS |
| C56 | 844×390: painel rola por dentro, Aplicar sempre visível | Playwright `deitado o painel rola` ✓ | `filtros.spec.ts:73` `expect(rola).toBeGreaterThan(visivel)`; `:76` `rodapeVisivel(page, 390)` em 0,5 e 1 | PASS |
| C57 | `type="search"`, `enterkeyhint="search"`, envio imediato e sem foco | Vitest `teclado busca na hora` passed | `filtros.test.tsx:206` `toHaveAttribute('type', 'search')`; `:207` `('enterkeyhint', 'search')`; `:213` `toBe(1)` sem esperar 300 ms; `:215` `activeElement).not.toBe(busca)`. Precisão: o teste dispara `fireEvent.submit`, não a tecla Enter (gap 10) | PASS |
| C58 | 44 px nos 5 controles da tela e nos 13 do painel a 360 | Playwright `toque de 44 px` ✓ | `filtros.spec.ts:83` `toBeGreaterThanOrEqual(44)` nos 5 alvos; `:94` `expect(doPainel.length).toBe(5 + 4 + 4)`; `:97` `toBeGreaterThanOrEqual(44)` | PASS |
| C59 | build .NET 0 (CA1502) e `ArquiteturaTests` inteiro com as contagens novas | `dotnet build Esclari.slnx --no-incremental` → `0 Erro(s)`, exit 0; TRX `ArquiteturaTests` 82 Passed | `ArquiteturaTests.cs:364` `}, 48);` (mapa de Chamados); `:490` `Assert.Equal(91, mensagens.Count)`; `:1022` `["Acoes", "Aluno", "Atendente", "Estados", "Leitura", "Lider"]` | PASS |
| C60 | suítes API e web inteiras; build web; MCP e `GET /api/chamados` com o JSON da base | `dotnet test Esclari.Api.Tests` 1069/1069; Vitest 1095/1095; `npm --prefix web run build` exit 0 | TRX `McpFerramentasTests` 14 Passed, `VisibilidadeTests` 4 Passed (arquivos não tocados pelo diff: `git diff --stat 7005230..HEAD` não lista `McpFerramentasTests.cs` nem `VisibilidadeTests.cs`); `BuscaDeChamadosTests.cs:47` compara com `GET /api/chamados?escopo=fila` | PASS |
| C61 | crapper < 10 em `web/src`; dryer sem candidato a 0.8 (E2) | `npm run coverage` exit 0; `crapper --source-root src --use-existing-coverage --threshold 10` exit 0; `dryer.cmd -s src/{acesso,assistente,chamados,conhecimento,indicadores,administracao}` + 30 arquivos da raiz de `web/src` `--threshold 0.8` exit 0 | `.metrics/crap.edn` regravado: maior nota `Sidebar ui.sidebar 9.8`; código novo `baixar chamados.Busca 9.6`; dryer imprime `No duplicate candidates found.` | PASS |
| C62 | (E3, sem "ok" do dono) os 5 specs passam no HEAD, exceto as 15 falhas da base, e nenhuma outra | HEAD: `npx playwright test e2e/ux.spec.ts e2e/casca.spec.ts e2e/identidade.spec.ts e2e/filtros.spec.ts e2e/arranjo.spec.ts` → `15 failed, 175 passed`; base `7005230` (imagem e specs da base, worktree): 4 specs → `16 failed, 170 passed` | as 15 do HEAD são exatamente as 15 do E3: `arranjo.spec.ts:227`, `:458`, `:938`, `:1188`; `casca.spec.ts:119`, `:589`, `:630`, `:673`, `:890`; `identidade.spec.ts:115`, `:721`, `:744`, `:786`, `:814`, `:1199`. Todas falham também na base (a 16ª da base, `identidade.spec.ts:288` "fontes latin no precache", passa no HEAD). `identidade.spec.ts:115` falha nas duas com a mesma mensagem "Raiz a 390: IMG em serifa" | PASS |

## Coverage

Recalculado da autoridade de cada conjunto: rotas e status do `## Surface`, as portas do `## Landing`, o `## Impact`, os critérios e, para os rótulos, os estados e motivos do domínio (`EstadosChamado`, `MotivosFechamento`).

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| `GET /api/chamados/busca` status (4) | Surface | 200 C1 · 400 C12, C16 · 401 C19 · 403 C9, C17, C18 | - |
| `GET /api/chamados/exportar` status (5) | Surface | 200 C35 · 400 C42 · 401 C19 · 403 C9, C17, C18, C43 · 429 C44 | - |
| causas de 400 em `exportar` (2) | Surface ("os mesmos de busca") + AC 13 + AC 37 | limite C42 | parâmetro inválido (`estado`, `ordem`, `de`>`ate`, `q` > 100) em `exportar`: nenhuma prova; C12 e C16 batem só em `busca` |
| `GET /api/chamados/filtros` status (3) | Surface | 200 C21 · 401 C19 · 403 C17, C18 | - |
| `GET /api/chamados/{numero}` status mudado (1) | Surface | 200 para administrador fora do setor C49 | - |
| filtros da API (9) | Surface In | `escopo` C18, C47 · `q` C3 · `estado` C2 · `setorId` C8 · `categoriaId` C7 · `responsavel` C10 · `de`/`ate` C11 · `ordem` C13 · `pagina` C14 | - |
| campos onde `q` procura (3) | AC 3 | título C3 · número C4 · nome do solicitante C5 | - |
| caracteres escapados na porta 3 (3) | Landing 3 ("`%`, `_` e `\` do texto escapados") | `%` C6 · `_` C6 | `\`: nenhum teste busca um texto com barra invertida |
| valores recusados (6) | AC 13 | `estado`, `ordem`, `responsavel`, `escopo`, `pagina`, `q` C16 (`BuscaDeChamadosTests.cs:265-279`) | - |
| porta 1, chaves (12) | Landing 1 | C15 nas 12 (`BuscaDeChamadosTests.cs:251`, `:253`) | - |
| porta 2, colunas (16) | Landing 2 | C36 cabeçalho literal; C37 13 valores; C38 "Primeira resposta em"; C41 "Título" | - |
| rótulo de estado no CSV (5) | AC 32 + `EstadosChamado` | Fechado C37 · Aberto C37 | Em atendimento, Aguardando aluno, Resolvido: nenhum CSV com esses estados é lido (`ExportarChamados.cs`, `RotuloDoEstado`) |
| rótulo de motivo de fechamento (4) | AC 32 + `MotivosFechamento` (CONFIRMADO, CANCELADO, DUPLICADO, FECHADO_AUTOMATICAMENTE) | Confirmado pelo aluno C37 | Cancelado pelo aluno, Duplicado, Fechado automaticamente |
| porta 2, aspas (4) | Landing 2 | `;` `"` CR LF C40 | - |
| porta 2, fórmula (6) | Landing 2 | `=` `+` `-` `@` TAB CR C41 | - |
| porta 2, forma do arquivo (5) | Landing 2 | BOM, `;`, CRLF C36 · Content-Type e nome C35 | - |
| dado pessoal fora do CSV (5) | AC 34 | CPF, matrícula, nome, e-mail, textos C39 | - |
| porta 3, extensão (1) | Landing 3 | `unaccent` C22, C3 | - |
| quem ramifica no papel `LEITURA` (6) | Impact ("`DetalheChamado.Montar`, `PermissoesChamado.Acoes/Estados`, `AgirNoChamado`, `TempoRealHub.EntrarNoChamado`, `BaixarAnexo`") | `Montar` C49 · `Acoes` C49 · `Estados` C50 (estado não muda) · `AgirNoChamado` C50 · `EntrarNoChamado` `TempoRealEntradaTests.cs:140` (E1) | `BaixarAnexo`: com `Carregar` devolvendo `LEITURA`, o administrador passa a baixar anexos de qualquer chamado (`Esclari.Api/Chamados/Consulta/BaixarAnexo.cs`, linha com `ChamadoDoUsuario.Carregar`), e nenhum teste prova esse 200 |
| campos do painel (7) | Telas 3 + AC 20/21 | Estado C25, C26 · Setor C26 · Categoria C26 · Responsável C26, C27 · Período C26 · Ordem C26, C27 · `q` C24, C27 | - |
| estados da tela (6) | Observable | carregando C34 · recarregando C34 · erro C33 · vazio sem filtro C30, C52 · vazio com filtro C30 · com itens C29, C31 | - |
| linhas do celular (4) | Observable | 360×780 C54, C55 · 844×390 C56 · teclado C57 · toque C58 | - |
| barra inferior do administrador (4) | Impact | Chamados, Indicadores, Gerir, Conta: `web/src/acesso/barra.test.tsx` (tabela de papéis alterada no diff), na suíte C60 | - |
| montagem do limite `exportar` (1) | `Esclari.Api/Seguranca.cs:20-21` (padrão 5), `Esclari.Api/Chamados/Consulta/ExportarChamados.cs:21` (`RequireRateLimiting("exportar")`), `Program.cs:48` `AddSeguranca()`; nenhum `appsettings*.json` nem o `docker-compose.yml` define `Limites:ExportarPorMinuto` | C44 boota o mesmo `Program` com `LimitesReais => true` | - |

## Test policy rows

O `checks.md` não tem seção `Test policy`; diz que o `AGENTS.md` (seção Testes) decide o nível. Contra essa convenção: toda afirmação de status, rota ou forma de resposta (C1, C9, C12, C14-C22, C35, C42-C44, C47-C50) tem prova que bate na rota HTTP por `WebApplicationFactory`; C40 e a primeira prova de C41 são unitárias sobre `CsvDeChamados.Celula`, e a segunda prova de C41 cruza a rota. Nenhum gap de nível.

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| AGENTS.md "teste de integração bate na rota HTTP" | `BuscarChamados.cs`, `ExportarChamados.cs`, `FiltrosDaBusca.cs`, `ChamadoDoUsuario.cs` | testes em `BuscaDeChamadosTests`, `ExportarChamadosTests`, `LeituraDoAdministradorTests`, `RateLimitTests` por `HttpClient` | yes |

## Faults injected

Todas as faltas foram feitas num worktree descartável (`git worktree add --detach D:\esclari-wt HEAD`), nunca na árvore real. As de e2e foram construídas desse worktree (`docker build -t esclari-api D:\esclari-wt` + `docker compose up -d --no-build --force-recreate api`) e rodadas com os specs do `HEAD`. Porcelain da árvore real antes: `?? .tmp-apresentacao/`, `?? .tmp-capturas/`. Depois de remover os worktrees: o mesmo, e `git diff --stat` vazio. A imagem da API foi reconstruída da árvore real (`docker compose up -d --build api`, imagem `7147a5d72375`) e `e2e/filtros.spec.ts` voltou a dar `4 passed`.

| Mutation | Location | Killed |
| --- | --- | --- |
| busca: tirar o escape de `\`, `%` e `_` do padrão | `Esclari.Api/Chamados/FiltroDeChamados.cs`, `PorTexto` | yes - `Curinga_e_texto_comum` falha em `BuscaDeChamadosTests.cs:122` (`Expected: 0 Actual: 2`) |
| CSV: arquivo sem o BOM UTF-8 | `Esclari.Api/Chamados/CsvDeChamados.cs`, `Arquivo` | yes - `Formato_do_arquivo` falha em `ExportarChamadosTests.cs:65` (`[239, 187, 191]` vs `[78, 195, 186]`) |
| papel: administrador recebe `ATENDENTE` em vez de `LEITURA` | `Esclari.Api/Chamados/Consulta/ChamadoDoUsuario.cs` | yes - `Le_o_detalhe_sem_acoes` (`LeituraDoAdministradorTests.cs:82`, `acoes` não vazio) e `Nenhuma_acao_em_leitura` (`:100`) falham |
| papel: `LEITURA` deixa de ver o que a equipe vê | `Esclari.Api/Chamados/Consulta/DetalheChamado.cs`, `Montar` (`equipe` só para `Atendente`) | yes - `Le_o_detalhe_sem_acoes` falha em `LeituraDoAdministradorTests.cs:77` (nota não encontrada) |
| tela Fila: espera da busca 300 → 250 ms | `web/src/chamados/Busca.tsx`, `CampoDeBusca` | yes - `busca 300 ms depois` falha em `filtros.test.tsx:50` (`expected 1 to be +0`) |
| exportar: recusa da API sem aviso | `web/src/chamados/Busca.tsx`, `BotoesDaBusca` (`onError` sem `toast.error`) | yes - `recusa vira aviso` falha (`Unable to find ... Refine os filtros.`) |
| exportar: URL sem os filtros da tela | `web/src/chamados/Chamados.tsx`, `exportar=` só com `escopo` | yes - `exporta com os filtros da tela` falha em `exportar.test.tsx:48` |
| tela Chamados: sem a coluna "Setor" | `web/src/chamados/Chamados.tsx`, `TodosOsChamados` | yes - `tela chamados` falha em `chamadosAdmin.test.tsx:37` |
| menu: "Chamados" também para `ATENDENTE` | `web/src/Autenticado.tsx`, `Menu` | yes - `menu do administrador` falha em `chamadosAdmin.test.tsx:22` |
| e2e: painel sempre à direita | `web/src/chamados/Busca.tsx`, `side=` | yes - `360 painel de baixo com rodape fixo` falha em `filtros.spec.ts:51` (`Expected: 780 Received: 663`) |
| e2e: campo de busca com 36 px | `web/src/chamados/Busca.tsx`, `Input` com `style={{ height: 36, minHeight: 0 }}` | yes - `toque de 44 px` falha em `filtros.spec.ts:83` (`Received: 36`) |
| e2e: barra em linha no celular | `web/src/chamados/Chamados.tsx`, `data-barra-da-busca` sem `flex-col` | yes - `360 busca na largura toda` falha em `filtros.spec.ts:40` (`Expected: 328 Received: 81.09`) |
| e2e: corpo do painel sem rolagem própria | `web/src/chamados/Busca.tsx`, `data-painel="corpo"` sem `min-h-0 overflow-y-auto` | yes - `360 painel de baixo com rodape fixo` (`:54`, 1019 > 780,5) e `deitado o painel rola` (`:73`, 784 não > 784) falham |

Mutação por ferramenta (`mutator`): suspensa por decisão do usuário (AD-025); não rodou.

## Gate

- `dotnet test Esclari.Api.Tests` - 1069 passed, 0 failed
- `npx vitest run` (web) - 1095 passed, 0 failed
- `npx playwright test e2e/filtros.spec.ts` - 4 passed, 0 failed
- `npx playwright test e2e/ux.spec.ts e2e/casca.spec.ts e2e/identidade.spec.ts e2e/filtros.spec.ts e2e/arranjo.spec.ts` - 175 passed, 15 failed (as 15 do E3, todas falhando também na base)
- `dotnet build Esclari.slnx --no-incremental`, `npm --prefix web run build`, `crapper ... --threshold 10`, `dryer ... --threshold 0.8` - exit 0
- `python validate_verification.py filtros-e-relatorios --root D:\esclari` - exit 1 (`verdict is FAIL`), coerente com o veredito

## Ranked gaps

1. Painel "Filtros" à direita a partir de 768 px (Telas 3) sem check. O código faz (caixa `{x:896, y:0, width:384, height:800}` a 1280 px), mas trocar o lado em telas largas passaria pela suíte. - nenhum check - `web/src/chamados/Busca.tsx` (`side={estreito ? 'bottom' : 'right'}`)
2. Tela Chamados sem prova da barra e do painel (Telas 2, AC 47 "a mesma barra, painel e tabela"). C52 prova título, colunas e vazio. - C52 - `web/src/chamados/chamadosAdmin.test.tsx:25-38`
3. Arranjo da Fila sem prova: barra → etiquetas com "Limpar filtros" → contagem → tabela, e a barra em uma linha a partir de 768 px (Telas 1; AC 24 "acima da tabela"). - C23, C29 - `web/src/chamados/filtros.test.tsx:40`, `:148`
4. A contagem mostra "N chamados · Mais antigos primeiro". O sufixo da ordem não está no desenho ("a contagem 'N chamados'") e não tem check. É um elemento sem decisão: o dono confirma ou sai. - nenhum check - `web/src/chamados/Chamados.tsx` (`Contagem`)
5. Ordem dos 6 campos do painel (Estado, Setor, Categoria, Responsável, Período, Ordem) sem check. O código segue a ordem do desenho. - C25 - `filtros.test.tsx:59-76`
6. CSV: rótulos de 3 dos 5 estados e de 3 dos 4 motivos de fechamento sem prova (AC 32). Um erro de digitação em "Aguardando aluno" ou "Duplicado" passaria. - C37 - `ExportarChamadosTests.cs:96-101`
7. `BaixarAnexo` com `LEITURA`: o Impact nomeia esse ponto como ramificado pelo papel e o administrador passa a baixar anexos de qualquer chamado da instituição. Nenhum teste prova o 200 (nem confirma que é o comportamento pedido). - nenhum check - `Esclari.Api/Chamados/Consulta/BaixarAnexo.cs`
8. Barra invertida escapada (Landing 3) sem prova; só `%` e `_` (C6). - C6 - `BuscaDeChamadosTests.cs:122-126`
9. 400 por parâmetro inválido em `exportar` sem prova; C12 e C16 só batem em `busca`, embora as duas rotas usem o mesmo `FiltroDeChamados.Invalido`. - C12, C16 - `BuscaDeChamadosTests.cs:204`, `:279`
10. Precisão, sem peso no veredito: (a) C57 diz "Enter busca na hora" e o teste dispara `fireEvent.submit` (`filtros.test.tsx:211`), não a tecla; (b) AC 23 fala em "uma etiqueta por filtro", e `ordem=recentes` não gera etiqueta, não conta em "Filtros (N)" e sobrevive a "Limpar filtros" (`Busca.tsx`, `Definicoes`; `Chamados.tsx`, `limpar`); os checks não decidem se a ordem é filtro; (c) `TodosOsChamados` não chama `useFilaAberta`, então a tela Chamados do administrador não recebe `FilaMudou` (o Flow 6 fala em "todas as consultas de busca"; o AC 27 só cobre a Fila); (d) C62 aceita 15 falhas, e `identidade.spec.ts:115` para na Raiz antes de varrer as telas seguintes, então esse teste não vê a Fila no `HEAD`; (e) o E3 não tem o "ok" do dono.
