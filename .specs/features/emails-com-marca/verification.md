# E-mails com a cara da instituição verification

**Verdict**: PASS
**Profile**: standard
**Diff range**: 8162b29..2f1c0ef (HEAD)
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

Provas rodadas em `HEAD` (2f1c0ef), VSTest, docker postgres/mailpit/minio no ar. Uma única invocação com todos os nomes: 55 testes, 55 Aprovado, cada nome aparece individualmente na saída (`Aprovado Esclari.Api.Tests.<Classe>.<Teste>`). Todos os nomes existem na árvore (`LayoutEmailTests`, `EmailTituloEAcaoTests`, `EmailMarcaTests`, `EmailsMigracaoTests`, `EmailSaidaDominioTests` são arquivos novos ou tocados no diff; `EmailTests` vive em `EmailAnexoTests.cs:18`).

## Binding sources

Plano não marca fonte vinculante de design (profile standard, não `ui`). Fontes de `## Sources` são pesquisa, não vinculantes: n/a.

## Checks

Comando comum: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~<Classe>.<Teste>"` (rodado em lote), exit 0, 55/55. Arquivos: `L` = `Esclari.Api.Tests/LayoutEmailTests.cs`, `T` = `EmailTituloEAcaoTests.cs`, `M` = `EmailMarcaTests.cs`.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | lang, title, viewport, color-scheme | `LayoutEmailTests.Documento_declara_...` ok | `L:29-32` `Assert.Contains("<html lang=\"pt-BR\">", html)`; `L:30` `Assert.Contains("<title>[Univag &amp; Cia] Aviso</title>", html)`; viewport e color-scheme com o literal do check | PASS |
| C2 | `role="presentation"` em toda tabela, 600px, 100%, sem script/form/link/recurso remoto | `...Tabelas_de_layout_...` ok | `L:42` `Assert.Equal(tabelas, Regex.Matches(html, "<table[^>]*role=\"presentation\"").Count)`; `L:43` `max-width:600px`; `L:48` `Assert.DoesNotMatch` de `src=` ou `url(` seguido de `http(s):` (regex de alternância) | PASS |
| C3 | 16px, 1.5, Arial, `#0e1330` sobre `#ffffff` | `...Texto_16px_entrelinha_e_tinta` ok | `L:56-60` `font-size:16px`, `line-height:1.5`, `font-family:Arial,Helvetica,sans-serif`, `color:#0e1330`, `background-color:#ffffff` | PASS (ver gap P3) |
| C4 | logo `cid:logo`, alt, 40px, cabeçalho branco, filete 4px antes; parte inline com os bytes | `...Logo_no_cabecalho_...` e `EmailMarcaTests.Html_com_cor_logo_e_corpo_escapado` ok | `L:68-73` `IndexOf("<img src=\"cid:logo\" alt=\"Univag &amp; Cia\"")`, `Assert.True(filete.Index < imagem)`; `L:74` `class="cabecalho"...background-color:#ffffff`; `M:69`, `M:76` `Assert.Equal("logo", ContentID)`, `M:78` `Assert.Equal(logo, bytes)` | PASS |
| C5 | sem logo: nome 20px negrito, sem `cid:`; MinIO fora envia e marca enviado | `...Sem_logo_...` e `EmailMarcaTests.Minio_fora_envia_sem_logo` ok | `L:82` `font-size:20px...font-weight:bold...>Univag &amp; Cia<`; `L:83` `DoesNotContain("cid:")`; `M:98` `using (f.MinioFora())`, `M:100` `Assert.NotNull(...EnviadoEm...)`, `M:103` `DoesNotContain("cid:", html)` | PASS |
| C6 | cor `#8a1538` / `#0e1330` no filete e botão; nunca `#C83A23` | `...Cor_da_instituicao_...` ok | `L:95` `Assert.Equal(2, Regex.Matches(com, "background-color:#8a1538").Count)`; `L:96` idem `#0e1330`; `L:97-98` `DoesNotContain("#C83A23")` | PASS |
| C7 | prévia oculta, 90 caracteres exatos, antes do título | `...Pre_visualizacao_oculta_...` ok | `L:108` `Assert.Equal(new string('a', 90), oculto.Groups[1].Value)` (entrada de 200); `L:109` posição antes do título | PASS |
| C8 | 4 cores do tema escuro; sem alterar cabeçalho nem botão | `...Tema_escuro_...` ok | `L:120-123` `.fundo{background-color:#0e0e18!important}`, `.cartao{...#151620...}`, `.texto{color:#f0eee9...}`, `.suave{color:#a0a1a8...}`; `L:124-126` `DoesNotContain(".cabecalho")`, `(".botao")`, `("#ffffff")` no bloco escuro | PASS |
| C9 | rodapé com remetente e aviso, 14px, `#5c5d64` | `...Rodape_com_remetente_...` ok | `L:134` `Enviado pelo Esclari para Univag &amp; Cia.`; `L:135` `Este aviso é automático; não responda este e-mail.`; `L:136` regex `class="suave"...font-size:14px...color:#5c5d64` | PASS (ver gap P3) |
| C10 | ≤480px: padding 32→20, botão 100% | `...Celular_reduz_...` ok | `L:146` `.miolo{padding-left:20px!important;padding-right:20px!important}`; `L:147` `.botao{display:block!important;width:100%!important}`; `L:148` base com `32px` | PASS |
| C11 | `text/plain` = corpo da fila, sem título/rótulo/rodapé | `EmailMarcaTests.Texto_puro_igual_ao_corpo_mesmo_com_titulo_e_acao` ok | `M:116` `Assert.Equal(corpo, Texto(msg))` lido no Mailpit; `M:117-119` `DoesNotContain` de título, rótulo e `Enviado pelo Esclari` | PASS |
| C12 | `<script>`, `&`, aspas escapados em corpo, título, rótulo, nome | `...Escapa_corpo_titulo_rotulo_e_nome` e `Html_com_cor_logo_e_corpo_escapado` ok | `L:158` `DoesNotContain("<script", html)`; `L:159` `Regex.Matches(html, "&lt;script&gt;alert\\(1\\)&lt;/script&gt; &amp; &quot;").Count >= 4`; `M:71-73` `&lt;script&gt;`, `Ana &amp; `, `DoesNotContain("<script>")` | PASS (ver gap P2) |
| C13 | 50 itens x 120 chars + endereço ≤ 100.000 bytes | `...Cinquenta_itens_cabem_em_100_kb` ok | `L:170` `Assert.True(Encoding.UTF8.GetByteCount(html) <= 100_000, ...)` | PASS |
| C14 | `<h1` 24px negrito antes do texto; sem título, sem `<h1` | `...Titulo_em_h1_de_24px_...` ok | `L:179` regex `<h1[^>]*font-size:24px[^>]*font-weight:bold`; `L:180` `IndexOf("<h1") < IndexOf("<p class=\"texto\"")`; `L:181` `DoesNotContain("<h1", sem)` | PASS |
| C15 | botão com 44px, branco 16px negrito, 6px, fundo da cor, VML mso | `...Botao_de_44px_...` ok | `L:191-192` loop `Assert.Contains(estilo, botao)` com `min-height:44px`, `color:#ffffff`, `font-size:16px`, `font-weight:bold`, `border-radius:6px`, `background-color:#8a1538`; `L:194-195` `<!--[if mso]>`, `v:roundrect` | PASS |
| C16 | texto de reserva + endereço como link abaixo do botão; linha repetida some | `...Endereco_por_extenso_...` ok | `L:205` `botao > 0 && reserva > botao`; `L:206` `Assert.Equal(2, Regex.Matches(html, "<a [^>]*href=\"<url>\"").Count)`; `L:207` `Assert.Single(">" + url + "</a>")`; `L:209` `DoesNotContain(">" + Chamado + "</p>")` | PASS |
| C17 | URL fora da base: sem botão nem endereço, sem erro | `...Acao_fora_da_base_nao_vira_botao` (3 casos) ok | `L:220-224` `DoesNotContain("Abrir agora")`, `("class=\"botao\"")`, `("Se o botão não abrir")`, `("outro.exemplo")`, `("javascript:")`; casos `https://outro.exemplo/x`, `javascript:alert(1)`, `http://localhost:8080.outro.exemplo/x` | PASS |
| C18 | `<p>`, `<ul>/<li>`, URL sozinha vira `<a>`, `javascript:` fica texto | `...Paragrafos_listas_e_links_do_corpo` ok | `L:232` `Primeiro.<br>Continua.`; `L:233-234` `Assert.Single("<ul")`, `Assert.Equal(2, "<li")`; `L:236` `<a href="https://exemplo.com/a?x=1&amp;y=2"`; `L:237-238` `Contains("javascript:alert(1)")`, `DoesNotContain("href=\"javascript")` | PASS |
| C19 | `> ` vira `<blockquote` com `border-left`; `Resolvido` grava com `> ` e mantém o texto | `...Bloco_com_maior_que_vira_citacao` e `EmailTituloEAcaoTests.Resolvido_grava_a_resolucao_como_citacao` ok | `L:246` `<blockquote[^>]*border-left[^>]*>[^<]*Linha um\.<br>Linha dois\.`; `L:247-248`; `T:55` `Assert.Contains("> Boleto reemitido.\n> Acesse o portal.\n", corpo)`; `T:56-57` texto inteiro | PASS |
| C20 | NovaResposta: título, rótulo, URL do chamado | `...Nova_resposta_grava_titulo_e_acao` ok | `T:32` `Confere(..., "Sua solicitação recebeu uma resposta", "Ver a resposta", Link(b, numero))`, `T:22` `Assert.Equal((titulo, rotulo, url), (e.Titulo, e.AcaoRotulo, e.AcaoUrl))` | PASS |
| C21 | Resolvido: título, rótulo, URL | `...Resolvido_grava_titulo_e_acao` ok | `T:43` `Confere(..., "Sua solicitação foi resolvida", "Confirmar ou rejeitar", Link(b, numero))` | PASS |
| C22 | AguardaResposta: título, rótulo, URL | `...Aguarda_resposta_grava_titulo_e_acao` ok | `T:70` `Confere(e, "A equipe aguarda a sua resposta", "Responder", Link(b, numero))` | PASS |
| C23 | Atrasos setor e admin: título, rótulo, URL = endereço do corpo | `...Atrasos_gravam_titulo_e_acao_do_setor_e_do_administrador` ok | `T:84` `Confere(doSetor, "Chamados em atraso", "Ver chamados em atraso", base + "/fila?atraso=true")`; `T:85` `Contains(url, corpo)`; `T:91-92` admin `"Resumo de chamados em atraso"`, `/chamados?atraso=true` | PASS |
| C24 | Validade: título, rótulo, URL = primeiro "Abrir:" | `...Validade_grava_titulo_e_acao` ok | `T:106` `Regex.Match(e.Corpo, @"Abrir: (\S+)")`; `T:108` `Confere(e, "Conteúdo para renovar", "Abrir a lista", primeiro)` | PASS |
| C25 | primeiro acesso (Mailpit) e criação da instituição (fila): título, botão, aviso, 60 min | `...Primeiro_acesso_envia_...` e `...Criacao_da_instituicao_grava_...` ok | `T:121-126` HTML do Mailpit: `<h1`, `Crie a sua senha`, `Criar senha</a>`, `.../definir-senha?token=`, `Se você não pediu isso, ignore este e-mail.`, `60 minutos`; `T:137` `Confere(e, "Crie a sua senha", "Criar senha", ".../demo-titulo/definir-senha?token=TOKEN123")`, `T:138-139` | PASS |
| C26 | linha antiga: molde novo, sem `<h1`/botão, `EnviadoEm` preenchido | `EmailMarcaTests.Linha_antiga_sai_no_molde_novo_...` ok | `M:131-134` `Contains("<html lang=\"pt-BR\">")`, `DoesNotContain("<h1")`, `DoesNotContain("class=\"botao\"")`, `Assert.NotNull(...EnviadoEm...)` | PASS |
| C27 | assuntos dos sete e-mails inalterados | 7 provas listadas ok (+ `PrimeiroAcessoTests` inteira, 5 testes) | `EmailAnexoTests.cs:26` `"[Univag] {numero}: nova resposta"`; `:39` `"[Univag] {numero} foi resolvida"`; `T:69` `x.Assunto == "[Univag] {numero} aguarda sua resposta"`; `AvisarAtrasosTests.cs:62,146`; `AvisarValidadeTests.cs:134`; `PrimeiroAcessoTests.cs:69` `"[Univag] Crie sua senha"`; `PlataformaTests.cs:205,426` `"[Faculdade Demo] Crie sua senha"`. Nenhuma dessas linhas aparece no diff de `Esclari.Api.Tests` (só 5 arquivos tocados) | PASS |
| C28 | `email_saida` ganha 3 colunas `text`/`YES`, nenhuma outra | `EmailsMigracaoTests.Colunas_titulo_acao_rotulo_e_acao_url_anulaveis` ok | `EmailsMigracaoTests.cs:14-16` `Assert.Contains("titulo,text,YES"/"acao_rotulo,text,YES"/"acao_url,text,YES", colunas)`; `:17-18` `Assert.Equal([lista exata das 11 colunas], ...)` | PASS |
| C29 | `Criar` sem/com título e ação; ida e volta do banco | `EmailSaidaDominioTests.Criar_sem_...`, `...Criar_com_...`, `EmailTituloEAcaoTests.Titulo_e_acao_voltam_do_banco_iguais` ok | `EmailSaidaDominioTests.cs:51-ish` `Assert.Equal((null, null, null), (e.Titulo, e.AcaoRotulo, e.AcaoUrl))`; `...:59-ish` `Assert.Equal(("T", "Abrir", "http://x/y"), ...)` com `Criar(..., new("T", new("Abrir", "http://x/y")))`; `T:155` `Confere(e, "Meu título", "Abrir", "http://localhost:8080/x/y")` lido com `AsNoTracking().SingleAsync` | PASS (ver gap P1: forma de `Criar` do texto do check difere) |
| C30 | nenhum pacote entra | `git diff --exit-code 8162b29..HEAD -- Esclari.Api/Esclari.Api.csproj Esclari.Api.Tests/Esclari.Api.Tests.csproj` exit 0, saída vazia | exit code 0 | PASS |
| C31 | `html-check` do Mailpit `Total.Unsupported` ≤ 15 | `EmailMarcaTests.Html_check_do_mailpit_...` ok | `M:146-147` `Total.Unsupported` lido de `/html-check`; `Assert.True(semSuporte <= 15, ...)` com título, botão e lista | PASS |

## Coverage

Recomputado do código (`LayoutEmail.cs`, `Avisos.cs`, `AvisarAtrasos.cs`, `AvisarValidade.cs`, `PrimeiroAcesso.cs`, `Criacao.cs`, `EmailSaida.cs`) e das seções Landing/Relations/Surface do plano (Surface = None; Relations = 1 relação `INSTITUICAO ||--o{ EMAIL_SAIDA`, coberta pelo query filter e pelos testes de fila).

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| emissores com título e botão (7) | chamadas a `EmailSaida.Criar(..., new(titulo, ...))` / `EnviadorSmtp.Enviar(..., titulo, acao)`: `Avisos.NovaResposta`, `Avisos.Resolvido`, `Avisos.AguardaResposta`, `AvisarAtrasos`, `AvisarValidade`, `PrimeiroAcesso`, `Criacao.EmailDeAcesso` | C20, C21, C22, C23, C24, C25 (Mailpit), C25 (`EmailDeAcesso`) | - |
| variantes de atraso (2) | `doSetor ? "Chamados em atraso" : "Resumo..."` | C23 `T:84` e `T:91` | - |
| cores do tema escuro (4) | `LayoutEmail.cs:30` | C8 `L:120-123` | - |
| colunas novas (3) | `EmailSaida.cs` + migration `20261118145227_EmailsComTituloEAcao` | C28 `EmailsMigracaoTests.cs:14-16`, mais lista exata das colunas | - |
| estados da linha (3) | `EmailSaida.Acao` (nula se faltar rótulo ou URL) | sem título e ação C26; com C14/C15/C25; do banco C29 `T:155` | - |
| endereço da ação (3) | `StartsWith(urlBase + "/")` `LayoutEmail.cs:24` | dentro C15; `https` fora C17; `javascript:` C17; bônus `localhost:8080.outro.exemplo` C17 | - |
| blocos do corpo (5) | `Tipo()` `LayoutEmail.cs:107-108` e `Linha()` | p, ul, link, `javascript:`, citação: C18 `L:232-238`, C19 `L:246` | - |
| origens escapadas (4) | `Escapar` em assunto/título/rótulo/nome/corpo | C12 `L:156-159` (as quatro no mesmo `perigo`) | - |
| logo (3) | `Cabecalho(..., logo)` e `EnviadorSmtp` | com C4, sem C5, MinIO fora C5 `M:98` | - |
| cor da instituição (2) | `CorValida() ? cor : Tinta` | definida e não definida C6 `L:95-96` | - |
| partes da mensagem (2) | `BodyBuilder` | `text/plain` C11 `M:116`; `text/html` C1 e `M:50` | - |
| assuntos (7) | os sete emissores | C27: nova resposta, resolvido (`EmailAnexoTests.cs:26,39`), aguarda (`T:69`), atraso (`AvisarAtrasosTests.cs:62`), validade (`AvisarValidadeTests.cs:134`), primeiro acesso (`PrimeiroAcessoTests.cs:69`), criação (`PlataformaTests.cs:205`) | - |
| tamanhos de tela (2) | `@media (max-width:480px)` e `min-height:44px` | C10, C15 | - |
| dependências novas (1) | csproj | C30 exit 0 | - |
| portas do Landing (3) | Landing 1 (colunas), 2 (função em C#, sem pacote), 3 (assinatura `Criar(..., ApresentacaoDoEmail?)`) | porta 1 C28/C29; porta 2 C30; porta 3 `EmailSaida.cs` + `EmailSaidaDominioTests`/`T:149` usam a forma nova, e `ArquiteturaTests` (inclui `Dados_ate_5_parametros`) passa na suíte inteira | - |

Existing assertions changed (`git diff 8162b29..HEAD -- Esclari.Api.Tests`; 5 arquivos, 3 novos `A`, 2 `M`, nenhum removido): em `EmailMarcaTests.cs` só mudam `<br>`→`<p`, `#C83A23`→`#0e1330`, `color:#ffffff`→`background-color:#ffffff` e o helper `Enfileirar` (parâmetros `titulo`/`acao` e `Criar(..., titulo is null ? null : new(titulo, acao))`), exatamente a lista do checks. `EmailSaidaDominioTests.cs` só ganha 2 testes. Nenhum `[Fact(Skip)]`, nenhum teste apagado, nenhuma asserção afrouxada. `EmailMarcaTests.cs:68` (`background-color:#8a1538`) segue igual e passa pelo filete.

## Test policy rows

`checks.md` não tem seção `Test policy`: vale a convenção do repo (AGENTS.md: integração pela rota/Mailpit, teste unitário na entidade, sem LLM nem e-mail real). Cumprida: Mailpit real nos checks que cruzam SMTP (C4, C5, C11, C25, C26, C31), unitário na entidade (C29).

## Faults injected

Worktree em `%TEMP%\wt-verif` (fora do repo), `git worktree add --detach`; árvore real com `git status --porcelain` igual antes e depois (só `?? .tmp-apresentacao/` e `?? .tmp-capturas/`, preexistentes); worktree removido e `git worktree prune` rodado.

| Mutation | Location | Killed |
| --- | --- | --- |
| guarda de prefixo removida: `var botao = acao;` | `Esclari.Api/LayoutEmail.cs:24` | yes - `LayoutEmailTests.Acao_fora_da_base_nao_vira_botao`, 3 de 3 casos com falha |
| tema escuro: `.texto{color:#f0eee9` → `#f0eee8` | `Esclari.Api/LayoutEmail.cs:30` | yes - `LayoutEmailTests.Tema_escuro_troca_as_quatro_cores_e_preserva_cabecalho_e_botao` |
| prévia: `TamanhoDaPrevia = 90` → `91` | `Esclari.Api/LayoutEmail.cs:12` | yes - `LayoutEmailTests.Pre_visualizacao_oculta_com_90_caracteres` |
| `Citar` sem o prefixo: `Select(l => "> " + l)` → `Select(l => l)` | `Esclari.Api/Chamados/Avisos.cs` (`Citar`) | yes - `EmailTituloEAcaoTests.Resolvido_grava_a_resolucao_como_citacao` |
| `AvisarValidade`: `primeira ??= url` → `primeira = url` (ação aponta para o último endereço) | `Esclari.Api/Conhecimento/AvisarValidade.cs` | yes - `EmailTituloEAcaoTests.Validade_grava_titulo_e_acao` |

## Deviations recorded by the author (julgamento)

1. **Porta 3** (`Criar` recebe `ApresentacaoDoEmail`): consistente com o plano, que já traz a linha 3 do Landing (adicionada ao `plan.md` no commit, com o motivo `Dados_ate_5_parametros` e a alternativa rejeitada). As colunas da porta 1 não mudam (C28 passa com lista exata). Não contradiz AC nem check; ver P1 sobre o texto literal de C29.
2. **Convenção `> `** para AC 19: consistente. O AC pede "bloco de citação com filete à esquerda" e não fixa o marcador; C19 foi escrito já com `"> "` e o teste afirma `border-left` e o texto inteiro.
3. **URL do resumo de atrasos em linha própria**: consistente com AC 16 (omitir a linha que repete só o endereço) e com C23 ("igual ao endereço 'Ver todos os chamados em atraso' do corpo"). Muda o texto puro do resumo (a URL passa de mesma linha para a linha de baixo); nenhum teste preexistente de `AvisarAtrasosTests` mudou e todos passam. Efeito colateral: o parágrafo "Ver todos os chamados em atraso:" fica sem o endereço no HTML, mas o botão "Ver chamados em atraso" vem logo abaixo, então a leitura se completa.

## Precision gaps (não bloqueiam; nenhum check está sem prova)

- P1. `checks.md` C29 e o Landing 1 ainda escrevem `Criar(..., titulo: "T", acao: new("Abrir", "http://x/y"))`, enquanto o código e os testes usam `Criar(..., new("T", new("Abrir", "http://x/y")))` (Landing 3). A nota "Settled mid-build" cobre isso, mas o texto do check aprovado ficou desatualizado.
- P2. `L:159` (C12) conta `>= 4` ocorrências do texto escapado na página inteira. A página tem mais de 4 ocorrências (assunto, alt, h1, rótulo x3, rodapé, corpo), então o teste não prende cada origem a um escape próprio. A falha mais perigosa (`<script` cru) é pega por `L:158`; aspas ou `&` crus em uma só origem não seriam.
- P3. Pontos soltos: C9 (`L:136`) casa `class="suave"` com 14px e `#5c5d64` sem ligar ao parágrafo do rodapé (o texto do rodapé é afirmado à parte em `L:134-135`); C5 (`L:82`) não prende o nome em 20px ao `class="cabecalho"`; C15 não afirma a cor do VML (`fillcolor`), só `v:roundrect`; C3 afirma os estilos por substring na página, não no parágrafo do corpo. AC 3 (contraste 17,7:1) e AC 9 (6,0:1) não têm asserção de razão de contraste, só das cores.
- P4. AC 13 (100 KB) é provado só em `LayoutEmail.Html` com corpo sintético (`L:163-171`), não pela fila real do `AvisarAtrasos` com 50 chamados.
- P5. Docs com texto corrompido por escape de shell (não afetam comportamento): `.specs/features/emails-com-marca/checks.md` Handoff "Settled mid-build" traz `	itulo e cao` (onde devia ser `titulo e acao`) e `>  viram` com marcador perdido; `.specs/GLOSSARIO.md` (+4 linhas) escreve `mail_saida.acao_rotulo` e `mail_saida.titulo` (faltou o `e` de `email_saida`).
- P6. C25 "criação da instituição (linha na fila)" é provado chamando `Criacao.EmailDeAcesso` direto (`T:135`), não pela rota de criação; o assunto desse caminho sai em `PlataformaTests.cs:205` e o `titulo`/ação gravados pela rota não são lidos no banco. A única chamada ao método é a própria `Criacao`, então o risco é baixo.

## Gate

`dotnet test Esclari.Api.Tests` (suíte inteira, `--no-build` sobre o build de `HEAD`) - 1214 passed, 0 failed, 0 skipped (3 m 1 s). Lote dos 55 testes do feature - 55 passed, 0 failed. `git diff --exit-code 8162b29..HEAD -- <csprojs>` - exit 0.
