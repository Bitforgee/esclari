# Marca padrão do Esclari verification

**Verdict**: PASS
**Profile**: standard
**Diff range**: 6444f4c..HEAD (HEAD = 19e5e57; código da feature em 5fdd54c)
**Round**: 2 - scoped
**Verifier**: independent sub-agent (author != verifier)

Escopo da rodada 2: o diff do ajuste (`5fdd54c..19e5e57`: `plan.md` AC 12, `checks.md` C1-C8/C11/C12/C20, `EmailMarcaTests.cs:119`) e o único veredito que não era PASS (C12). Provas rodam inteiras em HEAD. O resto carrega da rodada 1.

## Binding sources

carried from 5fdd54c: n/a - perfil `standard`, o plano não marca fonte vinculante.

## Checks (proofs verified at 19e5e57; rows marked `carried` keep the round-1 evidence)

Provas em HEAD, uma invocação por alvo. API: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests|...EmailMarcaTests|...MarcaDoEmailTests|...AparenciaAdminTests"` - 52 aprovados, 0 falhas (inclui `Cor_invalida_ou_sem_contraste_400` com 6 casos); cada teste nomeado aparece individualmente como aprovado. Web: `npx vitest run --reporter=verbose` nos 3 arquivos (`src/acesso/marcaPadrao.test.tsx`, `src/administracao/aparencia/identidade.test.tsx`, `src/administracao/aparencia/aparencia.test.tsx`) - 3 arquivos, 22 testes aprovados; os 12 testes nomeados nos checks de C1-C8, C17-C20 aparecem individualmente com ✓. Os caminhos dos checks agora batem com o arquivo real.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | sem ícone: `/marca/favicon.svg`, alt vazio, sem iniciais | vitest "sem icone o cabecalho mostra o simbolo do Esclari" | carried from 5fdd54c; teste passou de novo em 19e5e57 | PASS (carried) |
| C2 | com ícone: `.../icone?v=2` | vitest "com icone mostra o icone da instituicao" | carried from 5fdd54c; passou em 19e5e57 | PASS (carried) |
| C3 | entrada sem logo: logo Esclari por tema, `h1` com o nome | vitest "entrada sem logo mostra a logo do Esclari" | carried from 5fdd54c; passou em 19e5e57 | PASS (carried) |
| C4 | com logo: só a logo da instituição | vitest "com logo mostra so a logo da instituicao" | carried from 5fdd54c; passou em 19e5e57 | PASS (carried) |
| C5 | placa `bg-white` no Escuro e no Preto, não no claro | vitest "logo propria no tema escuro fica sobre placa branca" | carried from 5fdd54c; passou em 19e5e57 | PASS (carried) |
| C6 | avatar do assistente: favicon sem foto, foto com foto | vitest "assistente sem foto usa o simbolo do Esclari" | carried from 5fdd54c; passou em 19e5e57 | PASS (carried) |
| C7 | cor própria não troca a logo; `--marca` = cor | vitest "cor propria nao troca a logo do Esclari" | carried from 5fdd54c; passou em 19e5e57 | PASS (carried) |
| C8 | iniciais só no avatar da pessoa | vitest "iniciais so no avatar da pessoa" | carried from 5fdd54c; passou em 19e5e57 | PASS (carried) |
| C9 | `theme_color` `#f7f5ee` sem cor, `#8a1538` com cor | dotnet `Manifesto_padrao_sem_aparencia`, `Manifesto_da_instituicao` | carried from 5fdd54c; ambos aprovados em 19e5e57 | PASS (carried) |
| C10 | ícones Esclari sem ícone; só o da instituição com ícone | dotnet `Sem_icone_proprio_lista_o_maskable`, `Com_icone_proprio_so_lista_any` | carried from 5fdd54c; ambos aprovados em 19e5e57 | PASS (carried) |
| C11 | `id`, `scope`, `start_url` iguais depois de trocar cor e nome curto e enviar o ícone | dotnet `ManifestoTests.Trocar_a_marca_nao_muda_id_scope_nem_start_url` exit 0 | verified at 19e5e57: `Esclari.Api.Tests/ManifestoTests.cs:134` `SetProperty(x => x.Cor, "#8a1538").SetProperty(x => x.NomeCurto, "Marca")` (e o ícone enviado no setup); `:140` `Assert.Equal("#8a1538", depois.GetProperty("theme_color").GetString())`; `:142` `Assert.Equal(antes.GetProperty(campo).GetString(), depois.GetProperty(campo).GetString())` para id/scope/start_url. O texto novo ("cor e o nome curto e enviar o ícone") bate com o setup; a lacuna da rodada 1 fechou | PASS |
| C12 | sem `nomeCurto`, `name` = `nome` e `short_name` = iniciais (`Centro Universitário Vale` -> `CV`) | dotnet `ManifestoTests.Manifesto_padrao_sem_aparencia` exit 0 | verified at 19e5e57: `ManifestoTests.cs:42` `f.Instituicao(nome: "Centro Universitário Vale")` sem `NomeCurto`; `:48` `Assert.Equal("CV", m.GetProperty("short_name").GetString())`; `:50` `Assert.Equal(i.Nome, m.GetProperty("name").GetString())`. O texto novo do C12 e o AC 12 (`plan.md`, "as iniciais do `nome` como `short_name`") batem com o teste e com o código (`i.NomeCurto ?? ManifestoInstituicao.Iniciais(i.Nome)`, inalterado: `git diff 5fdd54c..HEAD` não toca `Esclari.Api/`) | PASS |
| C13 | sem logo e com MinIO fora: parte `logo` com PNG do Esclari, `cid:logo`, `max-height:40px`, nome em 20 px negrito | dotnet `EmailMarcaTests.Sem_logo_propria_leva_a_logo_do_Esclari_e_o_nome`, `Minio_fora_envia_sem_logo` | verified at 19e5e57: `EmailMarcaTests.cs:119` `Assert.Matches("font-size:20px;font-weight:bold[^\"]*\">Univag &amp; Cia<", html)` agora afirma o negrito; casa com `Esclari.Api/LayoutEmail.cs:60` `font-size:20px;font-weight:bold`. `:117` `alt="Esclari"`, `:118` `max-height:40px`, `:121-124` ContentID `logo`, `image/png`, bytes do Esclari. Ambos aprovados. Lacuna de rodada 1 fechou | PASS |
| C14 | com logo própria: bytes da instituição, alt = nome, sem `alt="Esclari"` | dotnet `Html_com_cor_logo_e_corpo_escapado` | carried from 5fdd54c; aprovado em 19e5e57 | PASS (carried) |
| C15 | PNG 348x80, assinatura, sem alfa | dotnet `MarcaDoEmailTests.Png_do_Esclari_tem_348_por_80_sem_canal_alfa` | carried from 5fdd54c; aprovado em 19e5e57 | PASS (carried) |
| C16 | `EmbeddedResource` no csproj e fluxo existe | dotnet `MarcaDoEmailTests.Png_do_Esclari_esta_embutido_no_projeto` | carried from 5fdd54c; aprovado em 19e5e57 | PASS (carried) |
| C17 | Aparência sem imagens mostra "Marca do Esclari (padrão)" e "Imagem padrão do Esclari" | vitest "sem imagens a Aparencia mostra a marca do Esclari como padrao" | carried from 5fdd54c; passou em 19e5e57 | PASS (carried) |
| C18 | arquivo escolhido: miniatura vira o arquivo, "Enviada por você", sem chamada | vitest "imagem escolhida mostra Enviada por voce sem salvar" | carried from 5fdd54c; passou em 19e5e57 | PASS (carried) |
| C19 | "Usar a marca do Esclari" chama DELETE e volta ao padrão; "Usar a cor padrão" esvazia a cor | vitest "usar a marca do Esclari remove a imagem e usar a cor padrao esvazia a cor" | carried from 5fdd54c; passou em 19e5e57 | PASS (carried) |
| C20 | ícone fora da regra e contraste < 4,5 recusados, marca não muda | dotnet `AparenciaAdminTests.Icone_nao_quadrado_ou_pequeno_400` (4 casos), `Cor_invalida_ou_sem_contraste_400` (6 casos); vitest "contraste ao lado da cor e Salvar desabilitado" | verified at 19e5e57: `AparenciaAdminTests.cs:103-104` `[InlineData("#ffd700", false)]`, `[InlineData("#777777", false)]` (além de `#767676` aceita e formatos inválidos); `:121` `Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode)`; `:122` `Assert.Equal([RecusaDaCor], Erros(await r.Json(), "cor"))`; `:124-125` `Assert.Null(linha.Cor)`, `Assert.Null(linha.NomeCurto)` (marca não muda). Web: `aparencia.test.tsx` "contraste ao lado da cor e Salvar desabilitado" passou. A lacuna de rodada 1 (API sem prova) fechou | PASS |
| C21 | (door 1) `NULL` em logo, ícone, login, assistente e cor; GET devolve `null` | dotnet `ManifestoTests.Instituicao_nova_nao_grava_a_marca_do_Esclari` | carried from 5fdd54c; aprovado em 19e5e57 | PASS (carried) |
| C22 | (door 2) PNG `EmbeddedResource` e `cid:logo` igual ao de hoje | dotnet `MarcaDoEmailTests.Png_do_Esclari_esta_embutido_no_projeto` | carried from 5fdd54c; aprovado em 19e5e57 | PASS (carried) |

## Coverage

verified at 19e5e57 para a linha `name` e `short_name`; as demais linhas carried from 5fdd54c (o ajuste não tocou código de produção).

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| lugares onde entra a marca do Esclari (7) | carried from 5fdd54c | barra/celular C1 · entrada C3 · avatar do assistente C6 · prévia C17 · e-mail C13 · manifest `theme_color` C9 · manifest ícones C10 | - |
| partes da marca (4) | carried from 5fdd54c | logo C3/C4 · ícone C1/C2 · foto C6 · cor C7/C9 | - |
| temas (3) | carried from 5fdd54c | claro/Escuro/Preto C3 e C5 | - |
| estados da logo no e-mail (3) | carried from 5fdd54c | sem logo C13 · com logo C14 · MinIO fora C13 | - |
| ações da gestão (4) | carried from 5fdd54c | escolher C18 · usar a marca C19 · usar a cor padrão C19 · recusas C20 | - |
| campos anuláveis (5) | carried from 5fdd54c | logo/ícone/login/assistente/cor C21 | - |
| campos do manifest que não mudam (3) | carried from 5fdd54c | id/scope/start_url C11 | - |
| `name` e `short_name` sem `nomeCurto` (2) | `ManifestoInstituicao.cs` / AC 12 (verified at 19e5e57) | `name` C12 (`ManifestoTests.cs:50`) · `short_name` iniciais C12 (`:48`) | - |

## Existing assertions

carried from 5fdd54c, com uma atualização verified at 19e5e57: o único item que a rodada 1 apontou como fora da lista (`iniciais da prévia iguais às do login` renomeado para `prévia e login mostram a mesma marca do Esclari sem logo`) agora consta em "Existing assertions changed" do `checks.md` (`git diff 5fdd54c..HEAD -- .specs/.../checks.md`, item 6 de "Settled mid-build"). O ajuste mudou uma asserção de teste: `EmailMarcaTests.cs:119` ficou mais estrita (acrescentou `font-weight:bold`), não enfraqueceu. Nenhum teste apagado, pulado ou enfraquecido.

## Faults injected

| Mutation | Location | Killed |
| --- | --- | --- |
| `CorPadrao` `#f7f5ee` -> `#c83a23` | carried from 5fdd54c: `Esclari.Api/Instituicoes/Carga/ManifestoInstituicao.cs:10` | yes (carried) - `ManifestoTests.Manifesto_padrao_sem_aparencia` |
| `logoDoEsclari: propria is null` -> `false` | carried from 5fdd54c: `Esclari.Api/EnviadorSmtp.cs:36` | yes (carried) - `EmailMarcaTests.Sem_logo_propria_leva_a_logo_do_Esclari_e_o_nome` |
| `usePlaca` devolve sempre `''` | carried from 5fdd54c: `web/src/aparencia.tsx:164` | yes (carried) - `marcaPadrao.test.tsx` "logo propria no tema escuro..." |
| verified at 19e5e57: `font-weight:bold` -> `font-weight:normal` no cabeçalho | `Esclari.Api/LayoutEmail.cs:60` (worktree `git worktree add ..\esclari-wt-v2 HEAD`, removido com `git worktree remove --force`; `git status --porcelain` igual antes e depois) | yes - `EmailMarcaTests.Sem_logo_propria_leva_a_logo_do_Esclari_e_o_nome`: `Assert.Matches() Failure: Pattern not found in value`, `Regex: "font-size:20px;font-weight:bold[^\"]*\">Univag &am"` (1 reprovado, 0 aprovados) |

## Gate

verified at 19e5e57: `dotnet test` (52 testes, 0 falhas) e `vitest run` (22 testes, 0 falhas). Os 22 checks passam: 4 re-verificados com evidência nova (C11, C12, C13, C20), 18 com a evidência da rodada 1 e prova re-executada em HEAD. Nenhum check sem prova.
