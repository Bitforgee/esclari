# Filtros na lista de chamados e relatório em CSV - verificação

**Verdict**: FAIL
**Profile**: ui
**Diff range**: 7005230..3ff7c50 (correção da rodada 1: `3f7a342..3ff7c50`)
**Round**: 2 - scoped
**Verifier**: independent sub-agent (author != verifier); não escreveu o código, os checks nem a correção

O que a correção `3ff7c50` mudou: só testes (`BuscaDeChamadosTests`, `ExportarChamadosTests`, `LeituraDoAdministradorTests`, `filtros.test.tsx`, `chamadosAdmin.test.tsx`, `e2e/filtros.spec.ts`), mais `checks.md` (E4, C63-C72) e `plan.md` (uma linha em Out of scope e uma em Assumptions). `git diff --stat bb4bcb6..3ff7c50` sem os testes, os e2e e o `.specs/` sai vazio, então o código de produto é o mesmo da rodada 1.

Os 72 checks passam com prova localizada no `HEAD` (`3ff7c50`), e os 32 mutantes da rodada 2 morreram, um ou mais para cada asserção nova. A correção fechou 7 dos 9 gaps ranqueados da rodada 1. O veredito continua FAIL por dois motivos:

1. **A contagem contradiz a Tela 1.** O C67 e o C68 afirmam "1 chamado · Mais antigos primeiro" (regex com `^…$`), e a Tela 1 do plano desenha a contagem "N chamados". A linha nova de Assumptions que aceita o sufixo está com `Confirmed? n`, ou seja, nenhum humano decidiu. Ela também ficou depois de uma linha em branco, fora da tabela (`plan.md:208`).
2. **O C64 deixa um membro sem prova.** A Surface diz que `exportar` recebe "os mesmos de `busca`, sem `pagina`", e o AC 10 manda responder `400` quando `de` > `ate`. O C64 cobre só 5 das 6 causas de 400 por parâmetro. `de` > `ate` em `exportar` continua sem prova, e o gap 9 da rodada 1 nomeava exatamente esse caso.

**Emenda E3 ainda sem confirmação do dono.** O C62 foi julgado contra o texto do E3, que o `checks.md` marca como "pede o 'ok' do dono". Esse "ok" continua fora do `checks.md` e do AD-066 do `STATE.md` (o `## Handoff` repete "E3 pede o 'ok' do dono"). Se o dono recusar o E3, o C62 volta ao texto original e é FAIL, porque 15 testes falham no `HEAD`.

## Binding sources

Verified at `3ff7c50` para as linhas que os checks novos tocam (C67-C71). As capturas são carried from `3f7a342`, porque o código de produto não mudou: `D:\esclari-verif\fila-1280.png`, `fila-1280-painel.png`, `fila-360*.png`, `chamados-1280*.png`, `chamados-360*.png`. A caixa do painel foi medida de novo pelo e2e novo a 1280×800: borda direita em 1280, `x` > 640, `y` 0, altura 800 (`filtros.spec.ts:40-43`, verde).

Fonte aberta: `plan.md` `## Surface` > Telas (`plan.md:68-72`) e os critérios AC 18-29, 41, 46-52 (`plan.md:119-178`).

| Source | Opened | Contradiction | Uncovered |
| --- | --- | --- | --- |
| Telas 1 `/:slug/fila`: barra (busca, "Filtros", "Exportar CSV"), etiquetas com "Limpar filtros", contagem "N chamados", tabela | sim - `plan.md:70`; captura `fila-1280.png` (carried) | C67 (`filtros.test.tsx:72`, `toHaveTextContent(/^1 chamado · Mais antigos primeiro$/)`) e C68 (`:78`, `/^2 chamados · Mais recentes primeiro$/`) afirmam uma contagem que o desenho não tem ("a contagem 'N chamados'"). A decisão que aceita o sufixo é a linha de Assumptions "a ordem conta como filtro?", com `Confirmed? n`, escrita no build depois da rodada 1 (`plan.md:208`). Sem o "ok" do dono, o check soletra um rótulo diferente do da fonte | - (arranjo agora coberto: ordem barra → etiquetas → contagem → tabela C67 `filtros.test.tsx:67`; conteúdo da barra `:68-70`; "Limpar filtros" nas etiquetas `:71`; barra numa linha a 1280 `filtros.spec.ts:36`) |
| Telas 2 `/:slug/chamados` ("a mesma tela" com escopo `todos`, coluna "Setor", título "Chamados") | sim - `plan.md:71`; captura `chamados-1280.png` (carried) | nenhuma | - (barra C71 `chamadosAdmin.test.tsx:50-51` e o clique em "Filtros"; painel com Setor "Todos, Financeiro, Secretaria" `:54` e 5 caixas de Estado `:55`; título, colunas e vazio C52). A ordem das regiões em `/chamados` só está provada pelo componente comum `ListaBuscada` (`Chamados.tsx:164-224`; `Fila` `:212` e `TodosOsChamados` `:215-223` renderizam o mesmo). Ver nota 6 |
| Telas 3 painel "Filtros" (à direita a partir de 768 px, de baixo abaixo; Estado, Setor, Categoria, Responsável, Período de abertura, Ordem; rodapé "Limpar"/"Aplicar") | sim - `plan.md:72`; medido pelo e2e novo a 1280 | nenhuma | - (lado direito C70 `filtros.spec.ts:40-43`; de baixo C55 `:65`; ordem dos 6 campos C69 `filtros.test.tsx:87`; rodapé C55, C56, C58). O limite exato de 768 px não é medido (nota 5) |
| Critérios AC 18-29, 41, 46-52 | sim - `plan.md` Criteria | nenhuma | - |

## Checks

Verified at `3ff7c50`. As provas rodaram em uma invocação por alvo:
- API: `dotnet test Esclari.Api.Tests --logger "trx;LogFileName=D:\esclari-verif\r2-api-full.trx"` deu `Aprovado! – Com falha: 0, Aprovado: 1077, Ignorado: 0, Total: 1077`, com 8 testes a mais que na rodada 1 (`Barra_invertida` 1, `Parametro_invalido_400_na_exportacao` 5 casos, `Rotulos_de_estado_e_motivo` 1, `Baixa_o_anexo_em_leitura` 1). Cada nome citado abaixo foi lido no TRX com `outcome=Passed` (47 nomes; `Parametro_invalido_400` x6, `Parametro_invalido_400_na_exportacao` x5, `Aspas_quando_precisa` x6, `Prefixo_contra_formula` x7).
- Vitest: `cd web && npx vitest run --reporter=json` deu `total=1099 passed=1099 failed=0`. Os 23 títulos citados nos checks aparecem cada um com `passed`, inclusive os 4 novos ("ordem da tela", "contagem diz a ordem", "ordem dos campos do painel", "tela chamados com a barra e o painel da fila").
- Playwright: `npx playwright test e2e/filtros.spec.ts` deu `5 passed`, inclusive `filtros.spec.ts:31` "desktop painel à direita e barra numa linha". Os 5 specs do C62 deram `15 failed, 176 passed`.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | busca sem filtro = fila, 4 estados, sem FECHADO nem outro setor | TRX `BuscaDeChamadosTests.Sem_filtro_igual_a_fila` Passed | `Esclari.Api.Tests/BuscaDeChamadosTests.cs:47` `Assert.Equal(fila, busca)`; `:48` `Assert.Equal([aberto, atendimento, aguardando, resolvido], busca)` | PASS |
| C2 | cada um dos 5 estados; ABERTO+FECHADO somam | TRX `Estado_filtra_cada_um`, `Estado_repetido_soma` Passed | `BuscaDeChamadosTests.cs:61` `Assert.Equal([numero], ...$"&estado={estado}")`; `:72` `Assert.Equal([aberto, fechado], ..."&estado=ABERTO&estado=FECHADO")` | PASS |
| C3 | `matricula` e `MATRÍCULA` acham "Trancar a Matrícula", não "Boleto" | TRX `Busca_no_titulo_sem_acento_e_sem_caixa` Passed | `BuscaDeChamadosTests.cs:82`, `:83` `Assert.Equal([matricula], ...)`; `:84` `Assert.DoesNotContain(boleto, ...)` | PASS |
| C4 | 3 últimos dígitos e `esc-000` acham pelo número | TRX `Busca_no_numero` Passed | `BuscaDeChamadosTests.cs:94` `Assert.Equal([segundo], ..."&q=" + segundo[^3..])`; `:96` `Assert.Equal([primeiro, segundo], ..."&q=esc-000")` | PASS |
| C5 | parte do nome sem acento acha só os do solicitante | TRX `Busca_no_nome_do_solicitante` Passed | `BuscaDeChamadosTests.cs:111` `Assert.Equal([dele], achados)`; `:112` `Assert.DoesNotContain(doOutro, achados)` | PASS |
| C6 | `%` e `_` dão total 0; `50%` acha só o desconto | TRX `Curinga_e_texto_comum` Passed | `BuscaDeChamadosTests.cs:122` `Assert.Equal(0, ...EscapeDataString("%")...total)`; `:123` idem `q=_`; `:126` `Assert.Equal([desconto], ..."50%")` | PASS |
| C7 | categoria atual; reclassificado muda de categoria | TRX `Filtra_por_categoria_atual` Passed | `BuscaDeChamadosTests.cs:151` `Assert.Equal([reclassificado], ...categoriaId={financeiro.Id})`; `:152` `Assert.Equal([original], ...categoriaId={padrao.Id})` | PASS |
| C8 | `setorId` de atendente com 2 setores | TRX `Filtra_por_setor` Passed | `BuscaDeChamadosTests.cs:164` `Assert.Equal([noOutro], ...)`; `:165` `Assert.Equal([naTriagem], ...)` | PASS |
| C9 | setor alheio: 403 "Este setor não é seu." em busca e exportar | TRX `Setor_alheio_403` Passed | `BuscaDeChamadosTests.cs:177` `Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode)`; `:178` `Assert.Equal("Este setor não é seu.", ...detail)` | PASS |
| C10 | `responsavel=eu` e `ninguem` | TRX `Responsavel_eu_e_ninguem` Passed | `BuscaDeChamadosTests.cs:191` `Assert.Equal([meu], ..."&responsavel=eu")`; `:192` `Assert.Equal([semNinguem], ..."&responsavel=ninguem")` | PASS |
| C11 | 03:30Z de 11/11 vem com 10/11 em Cuiabá, não com 11/11 | TRX `Periodo_no_fuso_da_instituicao` Passed | `BuscaDeChamadosTests.cs:204` `Assert.Equal([noite], ..."&de=2026-11-10&ate=2026-11-10")`; `:205` `Assert.Equal([meiaNoite], ..."&de=2026-11-11&ate=2026-11-11")` | PASS |
| C12 | `de` > `ate` → 400 com `errors.de` | TRX `BuscaDeChamadosTests.De_depois_de_ate_400` Passed | `BuscaDeChamadosTests.cs:215` `Assert.Equal(HttpStatusCode.BadRequest, ...)`; `:216` `Assert.True(...GetProperty("errors").TryGetProperty("de", out _))` | PASS |
| C13 | `recentes` invertido; `antigos` = ausente | TRX `Ordem_recentes_e_antigos` Passed | `BuscaDeChamadosTests.cs:230` `Assert.Equal([novo, medio, velho], ..."&ordem=recentes")`; `:231`, `:232` `[velho, medio, novo]` com `antigos` e sem `ordem` | PASS |
| C14 | 51 chamados: 50/1/0, total 51, porPagina 50 | TRX `Pagina_de_50_com_total` Passed | `BuscaDeChamadosTests.cs:245` `Assert.Equal((1, 50), ...)`; `:247` `(3, 0)`; `:249` `Assert.Equal((51, 50), (total, porPagina))` | PASS |
| C15 | chaves exatas do envelope e do item, valores do chamado | TRX `Forma_da_resposta` Passed | `BuscaDeChamadosTests.cs:263` `Assert.Equal(["itens", "total", "pagina", "porPagina"], ...)`; `:265` `Assert.Equal(["numero", "titulo", "estado", "setor", "categoria", "responsavel", "criadoEm", "dias"], ...)`; `:273` `Assert.Equal(2, ...dias)` | PASS |
| C16 | 6 parâmetros inválidos → 400 com o campo; 100 + espaços → 200 | TRX `Parametro_invalido_400` x6 Passed, `Busca_de_100_caracteres_aparada_200` Passed | `BuscaDeChamadosTests.cs:290` `Assert.Equal(HttpStatusCode.BadRequest, ...)`; `:291` `Assert.True(...errors.TryGetProperty(campo, out _), campo)`; `:316` `Assert.Equal(HttpStatusCode.OK, r.StatusCode)` | PASS |
| C17 | aluno → 403 nas 3 rotas | TRX `BuscaDeChamadosTests.Aluno_403` Passed | `BuscaDeChamadosTests.cs:325` `Assert.Equal(HttpStatusCode.Forbidden, ...)` sobre `Rotas` (3) | PASS |
| C18 | atendente com `todos` → 403 nas 3 | TRX `Todos_so_para_administrador` Passed | `BuscaDeChamadosTests.cs:334` `Assert.Equal(HttpStatusCode.Forbidden, ...rota + "?escopo=todos")` | PASS |
| C19 | sem sessão → 401 nas 3 | TRX `Sem_sessao_401` Passed | `BuscaDeChamadosTests.cs:343` `Assert.Equal(HttpStatusCode.Unauthorized, ...)` | PASS |
| C20 | outra instituição nunca em busca, exportar, filtros | TRX `Outra_instituicao_nunca_aparece` Passed | `BuscaDeChamadosTests.cs:360` `Assert.Equal([minha], Numeros(busca))`; `:363` uma linha de dados no CSV; `:364` `Assert.DoesNotContain(filtros...setores, ...outra...)` | PASS |
| C21 | filtros do atendente: setores dele, categorias ativas por nome, `{id, nome}` | TRX `Filtros_do_atendente` Passed | `BuscaDeChamadosTests.cs:380` `Assert.Equal(["Acesso ao portal", "Boleto", "Não classificado"], ...)`; `:381` `Assert.Equal(["id", "nome"], ...)` | PASS |
| C22 | `unaccent` instalada; modelo sem mudança pendente | TRX `Extensao_unaccent_instalada`, `ArquiteturaTests.Modelo_sem_mudanca_pendente` Passed | `BuscaDeChamadosTests.cs:389` `Assert.Equal(1, instalada)`; `Esclari.Api.Tests/ArquiteturaTests.cs:759` `Assert.False(...HasPendingModelChanges())` | PASS |
| C23 | busca rotulada, "Filtros", "Exportar CSV" antes da tabela | Vitest `barra da fila` passed | `web/src/chamados/filtros.test.tsx:40` `expect(el.compareDocumentPosition(tabela) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()` para os 3 | PASS |
| C24 | nada aos 299 ms, uma chamada aos 300 ms, `q=boleto` na URL | Vitest `busca 300 ms depois` passed | `filtros.test.tsx:50` `toBe(0)`; `:55` `toBe(1)`; `:56` `expect(params(roteador).get('q')).toBe('boleto')` | PASS |
| C25 | 5 caixas, Setor, Categoria, Responsável, Período, Ordem; "Fechado" não busca até Aplicar | Vitest `painel aplica em lote` passed | `filtros.test.tsx:94` `toEqual(['Aberto', 'Em atendimento', 'Aguardando aluno', 'Resolvido', 'Fechado'])`; `:97` Responsável `['Qualquer', 'Eu', 'Sem responsável']`; `:98` Ordem; `:99` grupo "Período de abertura"; `:105` `toBe(antes)` | PASS |
| C26 | Aplicar fecha, grava os 7 na URL e pede os 7 à API | Vitest `aplicar grava na url` passed | `filtros.test.tsx:126` `toEqual([['FECHADO'], '2', '5', 'eu', '2026-11-01', '2026-11-30', 'recentes'])`; `:127` `expect(buscas(chamadas, filtrada)).toBe(1)` | PASS |
| C27 | URL inicial pede os 4 filtros e preenche campo e painel | Vitest `url inicial preenche` passed | `filtros.test.tsx:134` `toBe(1)`; `:135` `toHaveValue('boleto')`; `:139` `toHaveDisplayValue('Sem responsável')`; `:140` `toHaveDisplayValue('Mais recentes primeiro')` | PASS |
| C28 | etiquetas, "Filtros (2)", remover uma, limpar todas | Vitest `etiquetas dos filtros` passed | `filtros.test.tsx:150` `toHaveTextContent('Estado: Fechado')`; `:152` `'Filtros (2)'`; `:156` `getAll('estado')).toEqual([])`; `:158` busca só com `responsavel=eu`; `:163` `toString()).toBe('')` | PASS |
| C29 | "73 chamados", "1 chamado", "0 chamados" | Vitest `contagem do total` passed | `filtros.test.tsx:178` `expect(await screen.findByText(texto)).toBeInTheDocument()` sobre os 3 pares | PASS |
| C30 | vazio com filtro + "Limpar filtros" na linha; sem filtro a frase da fila | Vitest `vazio com e sem filtro` passed | `filtros.test.tsx:185` célula `td` com "Nenhum chamado com esses filtros." e o botão; `:187` `findByText('Nenhum chamado na fila dos seus setores.')` | PASS |
| C31 | total 60: pede `pagina=2`, 60 linhas, repetido uma vez | Vitest `mostrar mais pede a pagina seguinte` passed | `filtros.test.tsx:200` `toBe(1)`; `:201` `toHaveLength(60)`; `:202` `getAllByText('ESC-000050')).toHaveLength(1)` | PASS |
| C32 | `FilaMudou` refaz a busca com `estado=FECHADO` | Vitest `FilaMudou refaz a busca filtrada` passed | `web/src/chamados/tempoReal.test.tsx:81` `expect(leituras(chamadas, filtrada)).toBe(2)`; `:82` `leituras(chamadas, FILA)).toBe(0)` | PASS |
| C33 | 500 → aviso e "Tentar de novo" com os mesmos filtros | Vitest `erro com tentar de novo` passed | `filtros.test.tsx:211` `toHaveTextContent('Não foi possível carregar a fila. Verifique sua conexão.')`; `:214` `buscas(chamadas, url)).toBe(2)` | PASS |
| C34 | lista anterior visível com `aria-busy="true"`; 3 esqueletos na primeira carga | Vitest `mantem a lista enquanto carrega`, `lista carregando mostra 3 esqueletos e nada mais` passed | `filtros.test.tsx:225` `toHaveAttribute('aria-busy', 'true')`; `:229` `not.toHaveAttribute('aria-busy')`; `web/src/chamados/listas.test.tsx:83` `toHaveLength(3)` | PASS |
| C35 | `text/csv; charset=utf-8` e `filename="chamados-<slug>-2026-11-10.csv"` às 03:30Z de 11/11 | TRX `ExportarChamadosTests.Cabecalhos_do_arquivo` Passed | `Esclari.Api.Tests/ExportarChamadosTests.cs:50` `Assert.Equal("text/csv; charset=utf-8", ...)`; `:51` `Assert.Equal($"attachment; filename=\"chamados-{...Slug}-2026-11-10.csv\"", ...)` | PASS |
| C36 | BOM, cabeçalho literal, CRLF, 55 linhas na ordem `recentes` | TRX `Formato_do_arquivo` Passed | `ExportarChamadosTests.cs:66` `Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3])`; `:68` `EndsWith("\r\n")`; `:70` `Assert.Equal(Cabecalho, linhas[0])`; `:72` 55; `:73` `Assert.Equal(esperados, ...)` | PASS |
| C37 | valores de cada coluna do fechado; aberto com 3 vazias | TRX `Valores_de_cada_coluna` Passed | `ExportarChamadosTests.cs:97` `Assert.Equal([fechado, "Boleto em dobro", "Fechado", "Financeiro", "Financeiro", "Não classificado", ..., "Análise e Desenvolvimento de Sistemas", "3", ...], l)`; `:102` `Assert.Equal(("Aberto", "", "", ""), ...)` | PASS |
| C38 | primeira resposta pública da equipe, não nota nem aluno; vazia sem resposta | TRX `Primeira_resposta_da_equipe` Passed | `ExportarChamadosTests.cs:147` `Assert.Equal(Data(primeira), ...[10])`; `:148` `Assert.Equal("", ...[10])` | PASS |
| C39 | sem CPF, matrícula, nome, e-mail, texto de mensagem ou nota | TRX `Sem_dado_pessoal_do_aluno` Passed | `ExportarChamadosTests.cs:165` `Assert.DoesNotContain(proibido, csv)` | PASS |
| C40 | `;`, `"`, CR, LF entre aspas com `"` dobrada; simples sem aspas | TRX `CsvDeChamadosTests.Aspas_quando_precisa` x6 Passed | `Esclari.Api.Tests/CsvDeChamadosTests.cs:14` `Assert.Equal(esperado, CsvDeChamados.Celula(valor))` | PASS |
| C41 | `'` antes de `=`, `+`, `-`, `@`, TAB, CR; `'=HYPERLINK(1)` no arquivo | TRX `Prefixo_contra_formula` x7, `Titulo_com_formula_sai_como_texto` Passed | `CsvDeChamadosTests.cs:24` `Assert.Equal(esperado, ...)`; `ExportarChamadosTests.cs:176` `Assert.Equal("'=HYPERLINK(1)", ...[1])` | PASS |
| C42 | 10 000 → 200 com 10 000 linhas; 10 001 → 400 com o detail | TRX `Limite_de_10000` Passed | `ExportarChamadosTests.cs:187` `Assert.Equal(10_000, linhas.Length - 1)`; `:192` `Assert.Equal("A exportação passa de 10.000 chamados. Refine os filtros.", ...detail)` | PASS |
| C43 | liderança → 403 com o detail; busca dela 200 | TRX `Lideranca_nao_exporta` Passed | `ExportarChamadosTests.cs:204` `Assert.Equal("A exportação não está disponível para a liderança de turma.", ...)`; `:205` `Assert.Equal(HttpStatusCode.OK, ...busca)` | PASS |
| C44 | 5 exportações 200, a sexta 429, com os limites reais | TRX `RateLimitTests.Seis_exportacoes_em_60s_429` Passed | `Esclari.Api.Tests/RateLimitTests.cs:57` `Assert.Equal(HttpStatusCode.OK, ...)` x5; `:59` `Assert.Equal(HttpStatusCode.TooManyRequests, ...)` | PASS |
| C45 | log `Information` com o modelo, ids, linhas, filtros, sem "boleto" | TRX `Log_da_exportacao_sem_o_texto` Passed | `ExportarChamadosTests.cs:218` modelo literal; `:220` `Assert.Equal(LogLevel.Information, log.Nivel)`; `:222` `Assert.Equal(2, log.Campos["Linhas"])`; `:224-225` `q`, `estado`; `:226` `DoesNotContain(... "boleto" ...)` | PASS |
| C46 | pede `exportar?escopo=fila&estado=FECHADO`, botão desabilitado com indicador, baixa com o nome; 400 vira aviso | Vitest `exporta com os filtros da tela`, `recusa vira aviso` passed | `web/src/chamados/exportar.test.tsx:48` `toBeDisabled()`; `:50` spinner `not.toBeNull()`; `:51` `toContain(Exportacao)`; `:56` `toEqual(['chamados-univag-2026-11-10.csv blob:csv'])`; `:68` `findByText('A exportação passa de 10.000 chamados. Refine os filtros.')` | PASS |
| C47 | administrador: 3 setores em busca e exportar com `todos`; `fila` total 0 | TRX `LeituraDoAdministradorTests.Todos_os_setores` Passed | `Esclari.Api.Tests/LeituraDoAdministradorTests.cs:33` `Assert.Equal([naTriagem, noFinanceiro, naSecretaria], ...)`; `:36` idem no CSV; `:37` `Assert.Equal(0, fila...total)` | PASS |
| C48 | filtros `todos` com todos os setores | TRX `Filtros_com_todos_os_setores` Passed | `LeituraDoAdministradorTests.cs:50` `Assert.Equal(3, todos.Count)`; `:51` `Assert.Equal(todos.Order(), ...Order())` | PASS |
| C49 | detalhe 200 com nota, fontes, eventos e `acoes` vazio | TRX `Le_o_detalhe_sem_acoes` Passed | `LeituraDoAdministradorTests.cs:75` `Assert.Equal(HttpStatusCode.OK, ...)`; `:77` nota `NOTA-SECRETA`; `:79` fonte; `:81` eventos ≠ 0; `:82` `Assert.Empty(j.GetProperty("acoes")...)` | PASS |
| C50 | 403 com a frase nas 10 rotas POST; chamado intacto | TRX `Nenhuma_acao_em_leitura` Passed | `LeituraDoAdministradorTests.cs:113` `Assert.Equal(10, posts.Count)`; `:117` `Forbidden`; `:118` `Assert.Equal("Seu papel neste chamado não permite esta ação.", ...)`; `:121-122` estado, responsável, setor, categoria e eventos iguais | PASS |
| C51 | "Chamados" no grupo Atendimento do administrador; ausente para atendente | Vitest `menu do administrador` passed | `web/src/chamados/chamadosAdmin.test.tsx:17` `toHaveAttribute('href', '/univag/chamados')`; `:23` `not.toBeInTheDocument()` | PASS |
| C52 | `/univag/chamados` pede busca e filtros `todos`, título, 7 colunas, vazio | Vitest `tela chamados` passed | `chamadosAdmin.test.tsx:29` `findByText('Nenhum chamado ainda.')`; `:30` heading "Chamados"; `:31-32` as 2 URLs; `:38` `toEqual(['Número', 'Título', 'Setor', 'Categoria', 'Responsável', 'Aberto', 'Estado'])` | PASS |
| C53 | `acoes` vazio: sem campo de resposta, sem Enviar nem botões de ação | Vitest `detalhe em leitura` passed | `chamadosAdmin.test.tsx:61` `querySelector('textarea')).toBeNull()`; `:62` os 5 botões `not.toBeInTheDocument()` | PASS |
| C54 | 360: campo na largura toda, botões abaixo, sem rolagem horizontal | Playwright `360 busca na largura toda` ✓ | `web/e2e/filtros.spec.ts:54` `expect(campo.width).toBeCloseTo(barra.width, 0)`; `:55`, `:56` botões abaixo e na mesma linha; `:58` `scrollWidth).toBeLessThanOrEqual(360)` | PASS |
| C55 | 360: painel na borda de baixo, rodapé visível nos dois extremos, Fechado aplica | Playwright `360 painel de baixo com rodape fixo` ✓ | `filtros.spec.ts:65` `expect(p.y + p.height).toBeCloseTo(780, 0)`; `:68`, `:70` `rodapeVisivel` (asserções em `:26-27`); `:77` `getAll('estado')).toEqual(['FECHADO'])` | PASS |
| C56 | 844×390: painel rola por dentro, Aplicar sempre visível | Playwright `deitado o painel rola` ✓ | `filtros.spec.ts:87` `expect(rola).toBeGreaterThan(visivel)`; `:90` `rodapeVisivel(page, 390)` em 0,5 e 1 | PASS |
| C57 | `type="search"`, `enterkeyhint="search"`, Enter busca na hora e tira o foco | Vitest `teclado busca na hora` passed | `filtros.test.tsx:236` `toHaveAttribute('type', 'search')`; `:237` `('enterkeyhint', 'search')`; `:238` `userEvent.type(busca, 'boleto{Enter}')`; `:239` `waitFor(() => expect(buscas(...q=boleto)).toBe(1), { timeout: 200 })`, abaixo dos 300 ms; `:240` `activeElement).not.toBe(busca)`. A precisão (a) da rodada 1 fecha: agora é a tecla Enter | PASS |
| C58 | 44 px nos 5 controles da tela e nos 13 do painel a 360 | Playwright `toque de 44 px` ✓ | `filtros.spec.ts:97` `toBeGreaterThanOrEqual(44)` nos 5; `:108` `expect(doPainel.length).toBe(5 + 4 + 4)`; `:111` `toBeGreaterThanOrEqual(44)` | PASS |
| C59 | build .NET 0 (CA1502) e `ArquiteturaTests` inteiro | `dotnet build Esclari.slnx --no-incremental` → `0 Erro(s)`, exit 0; TRX `ArquiteturaTests` 82 Passed | `ArquiteturaTests.cs:364` `}, 48);`; `:490` `Assert.Equal(91, mensagens.Count)`; `:1022` subpastas de Chamados (arquivo fora do diff da correção; citações carried from `3f7a342`) | PASS |
| C60 | suítes API e web inteiras; build web; MCP e `GET /api/chamados` com o JSON da base | `dotnet test Esclari.Api.Tests` 1077/1077; Vitest 1099/1099; `npm --prefix web run build` exit 0 | TRX `McpFerramentasTests` 14 Passed, `VisibilidadeTests` 4 Passed (fora do diff `7005230..3ff7c50`); `BuscaDeChamadosTests.cs:47` compara com `GET /api/chamados?escopo=fila` | PASS |
| C61 | crapper < 10 em `web/src`; dryer sem candidato a 0.8 (E2) | `npm run coverage` exit 0; `crapper --source-root src --use-existing-coverage --threshold 10` exit 0; `dryer.cmd` com `-s` nas 6 fatias e nos 30 arquivos não-teste da raiz de `web/src`, `--threshold 0.8`, exit 0 | maior nota `Sidebar ui.sidebar 9.8`; código da feature `baixar chamados.Busca 9.6`; dryer imprime `No duplicate candidates found.` | PASS |
| C62 | (E3, sem "ok" do dono) os 5 specs passam no HEAD, exceto as 15 falhas da base, e nenhuma outra | `npx playwright test e2e/ux.spec.ts e2e/casca.spec.ts e2e/identidade.spec.ts e2e/filtros.spec.ts e2e/arranjo.spec.ts` → `15 failed, 176 passed` | as 15 falhas são as mesmas 15 da rodada 1 e do E3: `arranjo.spec.ts:227`, `:458`, `:938`, `:1188`; `casca.spec.ts:119`, `:589`, `:630`, `:673`, `:890`; `identidade.spec.ts:115`, `:721`, `:744`, `:786`, `:814`, `:1199`. A comparação com a base `7005230` (16 falhas lá, as 15 mais `identidade.spec.ts:288`) é carried from `3f7a342`, porque nem a base nem o código de produto mudaram | PASS |
| C63 | `q=\` dá total 0 sem `\`; com "Arquivo em C:\temp\notas", `q=C:\temp` e `q=\` dão só esse | TRX `BuscaDeChamadosTests.Barra_invertida_e_texto_comum` Passed | `BuscaDeChamadosTests.cs:135` `Assert.Equal(0, (await Buscar(..., EscapeDataString("\\"))).GetProperty("total").GetInt32())`; `:138` `Assert.Equal([pasta], ...EscapeDataString(@"C:\temp"))`; `:139` `Assert.Equal([pasta], ...EscapeDataString("\\"))` | PASS |
| C64 | `exportar` 400 com o campo em `errors` para `estado`, `ordem`, `responsavel`, `escopo`, `q` de 101 | TRX `Parametro_invalido_400_na_exportacao` x5 Passed | `BuscaDeChamadosTests.cs:295-299` os 5 `InlineData`; `:304` `GetAsync("/api/chamados/exportar?" + consulta)`; `:306` `Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode)`; `:307` `Assert.True(...errors.TryGetProperty(campo, out _), campo)`. A afirmação está provada como escrita, mas não cobre `de` > `ate` (gap 2) | PASS |
| C65 | CSV: 5 rótulos de estado e 4 de motivo | TRX `ExportarChamadosTests.Rotulos_de_estado_e_motivo` Passed | `ExportarChamadosTests.cs:123` `Assert.Equal(["Aberto", "Em atendimento", "Aguardando aluno", "Resolvido", "Fechado"], ...[2])`; `:124` `"Confirmado pelo aluno"`; `:125` `"Cancelado pelo aluno"`; `:126` `"Duplicado"`; `:127` `"Fechado automaticamente"` (todos na coluna `[13]`); `:118` pré-condição: o agendador fechou o resolvido antigo | PASS |
| C66 | administrador em `LEITURA` baixa o anexo com 200 e os mesmos bytes; o de outra instituição 404 | TRX `LeituraDoAdministradorTests.Baixa_o_anexo_em_leitura` Passed | `LeituraDoAdministradorTests.cs:98` `Assert.Equal(HttpStatusCode.OK, r.StatusCode)`; `:99` `Assert.Equal(png, await r.Content.ReadAsByteArrayAsync())`; `:100` `Assert.Equal(HttpStatusCode.NotFound, (await deOutra.GetAsync($"/api/anexos/{id}")).StatusCode)` | PASS |
| C67 | ordem barra → etiquetas com "Limpar filtros" → contagem → tabela; a 1280 a barra numa linha | Vitest `ordem da tela` passed; Playwright `desktop painel à direita e barra numa linha` ✓ | `filtros.test.tsx:67` `compareDocumentPosition(...) & Node.DOCUMENT_POSITION_FOLLOWING` sobre `[barra, etiquetas, contagem, tabela]`; `:68-70` busca, "Filtros" e "Exportar CSV" dentro de `[data-barra-da-busca]`; `:71` "Limpar filtros" dentro de `[data-etiquetas]`; `:72` `toHaveTextContent(/^1 chamado · Mais antigos primeiro$/)`; `filtros.spec.ts:36` `expect(c.y + c.height / 2).toBeCloseTo(campo.y + campo.height / 2, 0)`. A prova bate com o texto do check; o texto do check contradiz a Tela 1 (gap 1) | PASS |
| C68 | `ordem=recentes` → "2 chamados · Mais recentes primeiro" | Vitest `contagem diz a ordem` passed | `filtros.test.tsx:78` `toHaveTextContent(/^2 chamados · Mais recentes primeiro$/)` (gap 1) | PASS |
| C69 | campos do painel na ordem Estado, Setor, Categoria, Responsável, Período de abertura, Ordem | Vitest `ordem dos campos do painel` passed | `filtros.test.tsx:87` `expect(titulos).toEqual(['Estado', 'Setor', 'Categoria', 'Responsável', 'Período de abertura', 'Ordem'])` | PASS |
| C70 | a 1280×800 o painel encosta na direita, começa depois de 640, altura 800 do topo | Playwright `desktop painel à direita e barra numa linha` ✓ | `filtros.spec.ts:40` `expect(p.x + p.width).toBeCloseTo(1280, 0)`; `:41` `expect(p.x).toBeGreaterThan(640)`; `:42` `expect(p.y).toBeCloseTo(0, 0)`; `:43` `expect(p.height).toBeCloseTo(800, 0)` | PASS |
| C71 | tela Chamados com busca, "Filtros", "Exportar CSV"; painel com Setor "Todos", "Financeiro", "Secretaria" e 5 caixas de Estado | Vitest `tela chamados com a barra e o painel da fila` passed | `chamadosAdmin.test.tsx:50` `getByRole('searchbox', { name: 'Buscar por número, título ou aluno' })`; `:51` `getByRole('button', { name: 'Exportar CSV' })`; `:52` clique em `getByRole('button', { name: 'Filtros' })`; `:54` `toEqual(['Todos', 'Financeiro', 'Secretaria'])`; `:55` `toHaveLength(5)` | PASS |
| C72 | administrador em `LEITURA` entra no grupo do chamado e recebe o aviso; as 5 recusas continuam | TRX `TempoRealEntradaTests.EntrarNoChamado_entra_o_administrador_em_leitura`, `EntrarNoChamado_recusa_com_Chamado_nao_encontrado` Passed | `Esclari.Api.Tests/TempoRealEntradaTests.cs:140` `Assert.Equal([$"Sonda:{grupo}"], await c.Esperar(1))`; `:176` `Assert.Equal(NaoEncontrado, await c.Recusa(() => c.Entrar(n)))` sobre os 5 casos de `:165-172`; `:179` `Assert.Empty(await c.Quieto())` | PASS |

## Coverage

Recalculado a partir da autoridade de cada conjunto. As linhas cuja autoridade a correção tocou dizem `verified at 3ff7c50`. As outras dizem `carried from 3f7a342` e são as linhas da rodada 1 com as citações atualizadas.

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| causas de 400 em `exportar` (7) | verified at 3ff7c50 - Surface `plan.md:64` ("os mesmos de `busca`, sem `pagina`") + AC 10 + AC 13 + AC 37; código `FiltroDeChamados.cs:37-45` (6 recusas) chamado por `Consultar` `:64`, que `ExportarChamados.cs:42` usa | limite C42 · `estado` C64 · `ordem` C64 · `responsavel` C64 · `escopo` C64 · `q` C64 | `de` > `ate` em `exportar`: nenhuma prova. O C12 bate só em `busca` (`BuscaDeChamadosTests.cs:215`), e o C64 tem 5 `InlineData` sem `de=2026-11-12&ate=2026-11-11` (`:295-299`) |
| caracteres escapados na busca (3) | verified at 3ff7c50 - Landing 3; código `FiltroDeChamados.cs:103` (`\`, `%`, `_`) | `%` C6 · `_` C6 · `\` C63 | - |
| rótulo de estado no CSV (5) | verified at 3ff7c50 - AC 32 + `RotuloDoEstado` `ExportarChamados.cs:28` | Aberto, Em atendimento, Aguardando aluno, Resolvido, Fechado C65 (`ExportarChamadosTests.cs:123`) | - |
| rótulo de motivo de fechamento (4) | verified at 3ff7c50 - AC 32 + `RotuloDoMotivo` `ExportarChamados.cs:33-34` | Confirmado pelo aluno `:124` · Cancelado pelo aluno `:125` · Duplicado `:126` · Fechado automaticamente `:127` (C65) | - |
| quem ramifica no papel `LEITURA` (6) | verified at 3ff7c50 - Impact `plan.md:46` | `DetalheChamado.Montar` C49 · `PermissoesChamado.Acoes` C49 · `Estados` C50 · `AgirNoChamado` C50 · `TempoRealHub.EntrarNoChamado` C72 · `BaixarAnexo` C66 | - |
| `GET /api/anexos/{id}` com `LEITURA` (2) | verified at 3ff7c50 - `BaixarAnexo.cs` (ramo `Carregar` + filtro de instituição) | 200 com os bytes C66 · 404 de outra instituição C66 | - |
| arranjo da Tela 1 Fila (4 regiões + barra numa linha) | verified at 3ff7c50 - `plan.md:70` | barra C67 `:67-70` · etiquetas com "Limpar filtros" C67 `:71` · contagem C67 `:72`, C29 · tabela C67 `:67` · uma linha a 1280 C67 `filtros.spec.ts:36` · coluna a 360 C54 | - |
| texto da contagem (2) | verified at 3ff7c50 - `plan.md:70` + `Chamados.tsx:153-160` | número C29 · sufixo da ordem C67, C68 (contradição, ver Binding sources) | - |
| Tela 2 Chamados (barra, painel, tabela, título, coluna, vazio) | verified at 3ff7c50 - `plan.md:71`, AC 47 | barra C71 `:50-52` · painel C71 `:54-55` · tabela, título, coluna "Setor", vazio C52 | - |
| Tela 3 painel: lado por largura (2) | verified at 3ff7c50 - `plan.md:72` + `Busca.tsx:148` | abaixo de 768 de baixo C55 (360) · a partir de 768 à direita C70 (1280) | - |
| Tela 3 painel: campos e ordem (6) | verified at 3ff7c50 - `plan.md:72` + `Busca.tsx:113-136` | Estado, Setor, Categoria, Responsável, Período de abertura, Ordem C69 (`:87`), presença e opções C25 | - |
| `GET /api/chamados/busca` status (4) | carried from 3f7a342 - Surface | 200 C1 · 400 C12, C16 · 401 C19 · 403 C9, C17, C18 | - |
| `GET /api/chamados/exportar` status (5) | carried from 3f7a342 - Surface | 200 C35 · 400 C42, C64 · 401 C19 · 403 C9, C17, C18, C43 · 429 C44 | - |
| `GET /api/chamados/filtros` status (3) | carried from 3f7a342 - Surface | 200 C21 · 401 C19 · 403 C17, C18 | - |
| `GET /api/chamados/{numero}` status mudado (1) | carried from 3f7a342 - Surface | 200 para administrador fora do setor C49 | - |
| filtros da API (9) | carried from 3f7a342 - Surface In | `escopo` C18, C47 · `q` C3 · `estado` C2 · `setorId` C8 · `categoriaId` C7 · `responsavel` C10 · `de`/`ate` C11 · `ordem` C13 · `pagina` C14 | - |
| campos onde `q` procura (3) | carried from 3f7a342 - AC 3 | título C3 · número C4 · nome do solicitante C5 | - |
| valores recusados em `busca` (6 + `de`>`ate`) | carried from 3f7a342 - AC 13, AC 10 | `estado`, `ordem`, `responsavel`, `escopo`, `pagina`, `q` C16 (`BuscaDeChamadosTests.cs:291`) · `de` C12 | - |
| porta 1, chaves (12) | carried from 3f7a342 - Landing 1 | C15 nas 12 (`BuscaDeChamadosTests.cs:263`, `:265`) | - |
| porta 2, colunas (16) | carried from 3f7a342 - Landing 2 | C36 cabeçalho literal; C37 13 valores; C38 "Primeira resposta em"; C41 "Título"; C65 "Estado" e "Motivo de fechamento" | - |
| porta 2, aspas (4) | carried from 3f7a342 - Landing 2 | `;` `"` CR LF C40 | - |
| porta 2, fórmula (6) | carried from 3f7a342 - Landing 2 | `=` `+` `-` `@` TAB CR C41 | - |
| porta 2, forma do arquivo (5) | carried from 3f7a342 - Landing 2 | BOM, `;`, CRLF C36 · Content-Type e nome C35 | - |
| dado pessoal fora do CSV (5) | carried from 3f7a342 - AC 34 | CPF, matrícula, nome, e-mail, textos C39 | - |
| porta 3, extensão (1) | carried from 3f7a342 - Landing 3 | `unaccent` C22, C3 | - |
| campos do painel na URL e na API (7) | carried from 3f7a342 - Telas 3 + AC 20/21 | Estado C25, C26 · Setor C26 · Categoria C26 · Responsável C26, C27 · Período C26 · Ordem C26, C27 · `q` C24, C27 | - |
| estados da tela (6) | carried from 3f7a342 - Observable | carregando C34 · recarregando C34 · erro C33 · vazio sem filtro C30, C52 · vazio com filtro C30 · com itens C29, C31 | - |
| linhas do celular (4) | carried from 3f7a342 - Observable | 360×780 C54, C55 · 844×390 C56 · teclado C57 (agora com Enter de verdade) · toque C58 | - |
| barra inferior do administrador (4) | carried from 3f7a342 - Impact | Chamados, Indicadores, Gerir, Conta: `web/src/acesso/barra.test.tsx`, na suíte C60 | - |
| montagem do limite `exportar` (1) | carried from 3f7a342 - `Esclari.Api/Seguranca.cs:20-21`, `ExportarChamados.cs:21`, `Program.cs:48`; nenhum `appsettings*.json` nem o `docker-compose.yml` define `Limites:ExportarPorMinuto` | C44 boota o mesmo `Program` com `LimitesReais => true` | - |

## Test policy rows

Carried from `3f7a342`, re-julgado para os testes novos. O `checks.md` não tem seção `Test policy`, então a regra do `AGENTS.md` decide ("teste de integração bate na rota HTTP"). Os testes novos da API cruzam a rota: C63 e C64 por `HttpClient` em `/api/chamados/busca` e `/api/chamados/exportar`, C65 em `/api/chamados/exportar`, C66 em `/api/anexos/{id}`. O C72 cruza o hub SignalR real (`f.Abrir`, `c.Entrar`). Nenhum gap de nível.

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| AGENTS.md "teste de integração bate na rota HTTP" | `BuscarChamados.cs`, `ExportarChamados.cs`, `FiltroDeChamados.cs`, `FiltrosDaBusca.cs`, `ChamadoDoUsuario.cs`, `BaixarAnexo.cs`, `TempoRealHub.cs` | `BuscaDeChamadosTests`, `ExportarChamadosTests`, `LeituraDoAdministradorTests`, `RateLimitTests` por `HttpClient`; `TempoRealEntradaTests` pelo hub | yes |

## Faults injected

Verified at `3ff7c50`. Todas as faltas foram feitas num worktree descartável (`git worktree add --detach D:\esclari-wt2 HEAD`, com `web/node_modules` como junção para o da árvore real), nunca na árvore real.

- Faltas da API: `dotnet test D:\esclari-wt2\Esclari.Api.Tests --filter ...`, um teste por vez.
- Faltas do web: `npx vitest run <arquivo> -t "<nome>"` dentro do worktree.
- Faltas de e2e: o worktree foi construído com `npm run build`, e o `wwwroot` resultante foi copiado para dentro do container `esclari-api-1` em `/app/wwwroot` (a API serve com `UseStaticFiles`, `Program.cs:24`). Depois rodaram os specs do `HEAD`. Antes das faltas, um baseline com o build do worktree sem falta deu `5 passed`.

Porcelain da árvore real antes: `?? .tmp-apresentacao/`, `?? .tmp-capturas/`. Depois de remover a junção e o worktree: o mesmo, e `git diff --stat` vazio. O container da API foi recriado da imagem do compose (`docker compose up -d --force-recreate api`), e `e2e/filtros.spec.ts` voltou a dar `5 passed`.

| Mutation | Location | Killed |
| --- | --- | --- |
| busca: tirar o escape de `\` do padrão | `Esclari.Api/Chamados/FiltroDeChamados.cs:103` | yes - `Barra_invertida_e_texto_comum` falha em `BuscaDeChamadosTests.cs:138` (`Expected: ["ESC-000002"] Actual: []`) |
| busca: apagar `\` do termo antes de montar o padrão | `FiltroDeChamados.cs:103` | yes - `Barra_invertida_e_texto_comum` falha em `BuscaDeChamadosTests.cs:135` (`Expected: 0 Actual: 1`) |
| busca: termo igual a `\` devolve vazio | `FiltroDeChamados.cs:102` | yes - `Barra_invertida_e_texto_comum` falha em `BuscaDeChamadosTests.cs:139` (`Expected: ["ESC-000002"] Actual: []`) |
| exportar: `Consultar` sem a validação dos parâmetros | `FiltroDeChamados.cs:61-64`, `Consulta/ExportarChamados.cs:42` | yes - `Parametro_invalido_400_na_exportacao` 5 de 5 casos falham em `BuscaDeChamadosTests.cs:306` (`Expected: BadRequest Actual: OK`) |
| validação: campo `ordem` devolvido como `order` em `errors` | `FiltroDeChamados.cs:42` | yes - `Parametro_invalido_400_na_exportacao(ordem=meio)` falha em `BuscaDeChamadosTests.cs:307` |
| CSV: "Aguardando aluno" → "Aguardando o aluno" | `Consulta/ExportarChamados.cs:28` | yes - `Rotulos_de_estado_e_motivo` falha em `ExportarChamadosTests.cs:123` |
| CSV: "Confirmado pelo aluno" → "Confirmado" | `ExportarChamados.cs:33` | yes - falha em `ExportarChamadosTests.cs:124` |
| CSV: "Cancelado pelo aluno" → "Cancelado" | `ExportarChamados.cs:33` | yes - falha em `ExportarChamadosTests.cs:125` |
| CSV: "Duplicado" → "Duplicada" | `ExportarChamados.cs:34` | yes - falha em `ExportarChamadosTests.cs:126` |
| CSV: "Fechado automaticamente" → "Fechado pelo sistema" | `ExportarChamados.cs:34` | yes - falha em `ExportarChamadosTests.cs:127` |
| anexo: papel `LEITURA` recusado | `Consulta/BaixarAnexo.cs` (condição do `Carregar`) | yes - `Baixa_o_anexo_em_leitura` falha em `LeituraDoAdministradorTests.cs:98` (`Expected: OK Actual: NotFound`) |
| anexo: corpo trocado por 1 byte | `BaixarAnexo.cs` (`Results.Bytes`) | yes - falha em `LeituraDoAdministradorTests.cs:99` (`[137, 80, 78, 71, ...]` vs `[0]`) |
| anexo: administrador baixa sem filtro de instituição nem `Carregar` | `BaixarAnexo.cs` (`IgnoreQueryFilters` + atalho do administrador) | yes - falha em `LeituraDoAdministradorTests.cs:100` (`Expected: NotFound Actual: OK`) |
| hub: `LEITURA` recusado em `EntrarNoChamado` | `Chamados/TempoReal/TempoRealHub.cs:14-15` | yes - `EntrarNoChamado_entra_o_administrador_em_leitura` falha (`HubException: Chamado não encontrado.`, `TempoRealEntradaTests.cs:136`/`:140`) |
| hub: qualquer usuário da instituição entra no grupo | `TempoRealHub.cs:14` (`db.Chamados.FirstOrDefaultAsync` sem `Carregar`) | yes - `EntrarNoChamado_recusa_com_Chamado_nao_encontrado` falha em `TempoRealEntradaTests.cs:176` |
| tela: contagem antes das etiquetas | `web/src/chamados/Chamados.tsx:182`, `:185` | yes - `ordem da tela` falha em `filtros.test.tsx:67` (`2: expected +0 to be truthy`) |
| tela: campo de busca fora da barra | `Chamados.tsx:178-179` | yes - `ordem da tela` falha em `filtros.test.tsx:68` |
| tela: "Filtros" e "Exportar CSV" fora da barra | `Chamados.tsx:180-181` | yes - `ordem da tela` falha em `filtros.test.tsx:69` |
| tela: "Exportar CSV" só com `escopo=todos` | `web/src/chamados/Busca.tsx:209-212` | yes - `ordem da tela` falha em `filtros.test.tsx:70` |
| tela: "Limpar filtros" fora de `[data-etiquetas]` | `Busca.tsx:172-182` | yes - `ordem da tela` falha em `filtros.test.tsx:71` |
| tela: contagem sem a ordem | `Chamados.tsx:157` | yes - `ordem da tela` falha em `filtros.test.tsx:72` |
| tela: `rotuloDaOrdem` dá "Mais antigos primeiro" para `recentes` | `Busca.tsx:46` | yes - `contagem diz a ordem` falha em `filtros.test.tsx:78` |
| painel: Categoria antes de Setor | `Busca.tsx:123-124` | yes - `ordem dos campos do painel` falha em `filtros.test.tsx:87` |
| teclado: Enter sem busca imediata | `Busca.tsx:83` | yes - `teclado busca na hora` falha em `filtros.test.tsx:239` (`expected +0 to be 1` em 200 ms) |
| teclado: Enter sem `blur` | `Busca.tsx:84` | yes - `teclado busca na hora` falha em `filtros.test.tsx:240` |
| Chamados: campo de busca só na Fila | `Chamados.tsx:179` | yes - `tela chamados com a barra e o painel da fila` falha em `chamadosAdmin.test.tsx:50` |
| Chamados: "Exportar CSV" fora de `escopo=todos` | `Busca.tsx:209-212` | yes - falha em `chamadosAdmin.test.tsx:51` |
| Chamados: opções do painel lidas de `filtros?escopo=fila` | `Chamados.tsx:143` | yes - falha em `chamadosAdmin.test.tsx:54` (`[ 'Todos' ]`) |
| Chamados: sem a caixa "Fechado" com mais de um setor | `Busca.tsx:119` | yes - falha em `chamadosAdmin.test.tsx:55` (`length of 5 but got 4`) |
| e2e: barra em coluna também a partir de 768 px | `Chamados.tsx:178` (sem `md:flex-row md:items-center`) | yes - `desktop painel à direita e barra numa linha` falha em `filtros.spec.ts:36` (`Expected: 91 Received: 135`) |
| e2e: painel à esquerda a partir de 768 px | `Busca.tsx:148` (`side`) | yes - falha em `filtros.spec.ts:40` (`Expected: 1280 Received: 384`) |
| e2e: painel sempre de baixo | `Busca.tsx:148` (`side={'bottom'}`) | yes - falha em `filtros.spec.ts:41` (`> 640`, `Received: 0`); `deitado o painel rola` também falha (`:87`) |
| e2e: `max-h-[85dvh]` também à direita | `Busca.tsx:148` (`className`) | yes - falha em `filtros.spec.ts:43` (`Expected: 800 Received: 680`) |
| e2e: painel 64 px abaixo do topo | `Busca.tsx:148` (`top-16` quando largo) | yes - falha em `filtros.spec.ts:42` (`Expected: 0 Received: 64`) |

Carried from `3f7a342`: os 11 mutantes da rodada 1 mortos sobre código que a correção não tocou.

| Mutation | Location | Killed |
| --- | --- | --- |
| busca: tirar o escape de `\`, `%` e `_` | `FiltroDeChamados.cs`, `PorTexto` | yes - `Curinga_e_texto_comum` (`BuscaDeChamadosTests.cs:122`) |
| CSV sem o BOM | `CsvDeChamados.cs`, `Arquivo` | yes - `Formato_do_arquivo` (`ExportarChamadosTests.cs:66`) |
| administrador recebe `ATENDENTE` em vez de `LEITURA` | `ChamadoDoUsuario.cs` | yes - `Le_o_detalhe_sem_acoes` (`LeituraDoAdministradorTests.cs:82`), `Nenhuma_acao_em_leitura` (`:117`) |
| `LEITURA` deixa de ver o que a equipe vê | `DetalheChamado.cs`, `Montar` | yes - `Le_o_detalhe_sem_acoes` (`LeituraDoAdministradorTests.cs:77`) |
| espera da busca 300 → 250 ms | `Busca.tsx`, `CampoDeBusca` | yes - `busca 300 ms depois` (`filtros.test.tsx:50`) |
| recusa da exportação sem aviso | `Busca.tsx`, `BotoesDaBusca` | yes - `recusa vira aviso` (`exportar.test.tsx:68`) |
| URL de exportar só com `escopo` | `Chamados.tsx` | yes - `exporta com os filtros da tela` (`exportar.test.tsx:48`) |
| tela Chamados sem a coluna "Setor" | `Chamados.tsx`, `TodosOsChamados` | yes - `tela chamados` (`chamadosAdmin.test.tsx:38`) |
| "Chamados" também para `ATENDENTE` | `Autenticado.tsx`, `Menu` | yes - `menu do administrador` (`chamadosAdmin.test.tsx:23`) |
| painel sempre à direita | `Busca.tsx`, `side=` | yes - `360 painel de baixo com rodape fixo` (`filtros.spec.ts:65`) |
| campo de busca com 36 px; barra em linha a 360; corpo do painel sem rolagem | `Busca.tsx`, `Chamados.tsx` | yes - `toque de 44 px` (`:97`), `360 busca na largura toda` (`:54`), `360 painel...` (`:68`) e `deitado...` (`:87`) |

A mutação por ferramenta (`mutator`) está suspensa por decisão do usuário (AD-025) e não rodou.

## Gate

Verified at `3ff7c50`:
- `dotnet test Esclari.Api.Tests` - 1077 passed, 0 failed
- `npx vitest run` (web) - 1099 passed, 0 failed
- `npx playwright test e2e/filtros.spec.ts` - 5 passed, 0 failed
- `npx playwright test e2e/ux.spec.ts e2e/casca.spec.ts e2e/identidade.spec.ts e2e/filtros.spec.ts e2e/arranjo.spec.ts` - 176 passed, 15 failed (as 15 do E3)
- `dotnet build Esclari.slnx --no-incremental` (0 erros), `npm --prefix web run build`, `npm run coverage` + `crapper --source-root src --use-existing-coverage --threshold 10` (máx. 9,8), `dryer.cmd ... --threshold 0.8` ("No duplicate candidates found.") - exit 0
- `python validate_verification.py filtros-e-relatorios --root D:\esclari` - exit 1 (`verdict is FAIL`), coerente com o veredito

## Round 1 gaps, re-judged

| Gap da rodada 1 | Estado em `3ff7c50` |
| --- | --- |
| 1. painel à direita a partir de 768 px | fechado - C70 (`filtros.spec.ts:40-43`), 4 mutantes mortos; o limite exato fica na nota 5 |
| 2. tela Chamados sem barra e painel | fechado - C71 (`chamadosAdmin.test.tsx:50-55`), 4 mutantes mortos |
| 3. arranjo da Fila e barra numa linha | fechado - C67 (`filtros.test.tsx:67-72`, `filtros.spec.ts:36`), 7 mutantes mortos |
| 4. sufixo da ordem na contagem sem decisão | aberto - agora tem check (C67, C68), mas a decisão é `Confirmed? n` e contradiz a Tela 1 (gap 1 abaixo) |
| 5. ordem dos 6 campos do painel | fechado - C69 (`filtros.test.tsx:87`) |
| 6. rótulos de estado e motivo no CSV | fechado - C65 (`ExportarChamadosTests.cs:123-127`), 5 mutantes mortos |
| 7. `BaixarAnexo` com `LEITURA` | fechado - C66 (`LeituraDoAdministradorTests.cs:98-100`), 3 mutantes mortos; o comportamento segue o Impact (`plan.md:46`) |
| 8. barra invertida escapada | fechado - C63 (`BuscaDeChamadosTests.cs:135-139`), 3 mutantes mortos |
| 9. 400 por parâmetro inválido em `exportar` | parcial - C64 cobre `estado`, `ordem`, `responsavel`, `escopo`, `q`; `de` > `ate`, nomeado no gap, continua sem prova (gap 2 abaixo) |
| 10a. C57 com `fireEvent.submit` | fechado - `userEvent.type(..., 'boleto{Enter}')`, `filtros.test.tsx:238-239` |
| 10b. a ordem conta como filtro? | virou o gap 1 abaixo |
| 10c. tela Chamados sem `FilaMudou` | levado para Out of scope pelo builder (nota 4) |
| 10d. `identidade.spec.ts:115` para na Raiz | sem mudança: ainda falha no `HEAD` com "Raiz a 390: IMG em serifa" e não chega à Fila |
| 10e. E3 sem "ok" do dono | sem mudança (nota 3) |

## Ranked gaps

1. **A contagem contradiz a Tela 1 do plano (binding).** O C67 e o C68 afirmam o texto exato "N chamados · Mais antigos/recentes primeiro", e o desenho fixa a contagem "N chamados" (`plan.md:70`). A linha de Assumptions que aceita o sufixo e mantém a ordem fora das etiquetas e de "Limpar filtros" diz `Confirmed? n`, decidido no build depois da rodada 1. Ela também está separada da tabela por uma linha em branco, então nem renderiza como linha da tabela. Fecha de um destes dois jeitos: o dono dá o "ok" (a linha vira `y` e volta para dentro da tabela), ou o sufixo sai e o C67/C68 mudam com o "ok" dele. - C67, C68 - `plan.md:208`; `web/src/chamados/filtros.test.tsx:72`, `:78`; `web/src/chamados/Chamados.tsx:157`
2. **`de` > `ate` em `exportar` sem prova.** A Surface (`plan.md:64`) dá a `exportar` os mesmos parâmetros de `busca`, e o AC 10 manda responder `400` com `errors.de`. O C64 lista 5 das 6 causas de 400 por parâmetro e o C12 só bate em `busca`. Uma `exportar` que deixasse de validar o período passaria pela suíte. - C64, C12 - `Esclari.Api.Tests/BuscaDeChamadosTests.cs:295-307`, `:215-216`

Notas, sem peso no veredito:

3. O E3 continua sem o "ok" do dono. O C62 foi julgado contra ele. - `checks.md` E3; `.specs/STATE.md` `## Handoff`
4. A linha nova de Out of scope ("atualização em tempo real da tela Chamados do administrador", `plan.md:192`) estreita o Flow 6 ("invalida todas as consultas de busca abertas", `plan.md:39`) depois da aprovação e não registra o "ok" do dono. O AC 27 continua coberto pela Fila (C32).
5. O C70 mede o lado do painel só a 1280. O C55 mede de baixo a 360. Nenhum teste mede 767 contra 768, que é o limite que a Tela 3 decide (`useEstreito`).
6. A ordem das regiões na tela Chamados só está provada pelo componente comum `ListaBuscada` (`Chamados.tsx:164-224`). Um arranjo que dependesse do `escopo` passaria.
7. Na tabela `## Coverage` do `checks.md`, as linhas "quem ramifica no papel `LEITURA` (5)" e "configuração de partida" estão grudadas numa linha só (`| - || configuração ...`), então a segunda não é uma linha da tabela.
8. Lições candidatas para quem tiver a skill registrar com `lessons.py` (este Verify só podia commitar este arquivo):
   - (a) uma emenda de checks que fecha um conjunto deve listar os membros pela autoridade (Surface + ACs), não pela prova irmã; aqui a prova irmã de `busca` (C16) deixava `de` > `ate` de fora.
   - (b) uma decisão de UI tomada no build depois do Verify vira linha de Assumptions com `Confirmed? n`, e o check que depende dela continua contradizendo a fonte até o dono confirmar.
