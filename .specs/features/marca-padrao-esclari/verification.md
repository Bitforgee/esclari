# Marca padrão do Esclari verification

**Verdict**: FAIL
**Profile**: standard
**Diff range**: 6444f4c..HEAD (HEAD = 5fdd54c)
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

## Binding sources

n/a - perfil `standard`, o plano não marca fonte vinculante.

## Checks

Provas rodadas em HEAD, uma invocação por alvo. API: `dotnet test Esclari.Api.Tests --filter "ManifestoTests|EmailMarcaTests|MarcaDoEmailTests|AparenciaAdminTests.Icone_nao_quadrado_ou_pequeno_400"` - 22 aprovados, 0 falhas, cada teste nomeado aparece como aprovado. Web: `vitest run` nos 3 arquivos com `--reporter=verbose` - 3 arquivos, 22 testes aprovados, cada teste nomeado aparece. (Os checks citam `src/marcaPadrao.test.tsx`; o arquivo real é `src/acesso/marcaPadrao.test.tsx`.) O autor informou API 1219/1219 e Vitest 1140/1140; isso não conta como prova.

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | sem ícone: `/marca/favicon.svg`, alt vazio, sem iniciais | vitest "sem icone o cabecalho mostra o simbolo do Esclari" exit 0 | `web/src/acesso/marcaPadrao.test.tsx:26` `expect(cabecalho.querySelector('img')).toHaveAttribute('src', '/marca/favicon.svg')`; `:27` alt `''`; `:29` `expect(screen.queryByText('U')).toBeNull()` | PASS |
| C2 | com ícone: `.../icone?v=2` | vitest "com icone mostra o icone da instituicao" | `marcaPadrao.test.tsx:36` `expect(imagens()).toContain('/api/instituicoes/univag/imagens/icone?v=2')` | PASS |
| C3 | entrada sem logo: logo Esclari por tema, `h1` com o nome | vitest "entrada sem logo mostra a logo do Esclari" | `marcaPadrao.test.tsx:45` `findByRole('heading',{level:1,name:'Univag'})`; `:47` `toHaveAttribute('src', '/marca/' + arquivo)` para light/dark/black; `:48` `alt 'Esclari'` | PASS |
| C4 | com logo: só a logo da instituição | vitest "com logo mostra so a logo da instituicao" | `marcaPadrao.test.tsx:57` `toContain('.../logo?v=3')`; `:58` e `:64` `filter(startsWith('/marca/'))` `toEqual([])` | PASS |
| C5 | placa `bg-white` no Escuro e no Preto, não no claro | vitest "logo propria no tema escuro fica sobre placa branca" | `marcaPadrao.test.tsx:73` `expect(logo.classList.contains('bg-white'), tema).toBe(placa)` (light false, dark true, black true) | PASS |
| C6 | avatar do assistente: favicon sem foto, foto com foto | vitest "assistente sem foto usa o simbolo do Esclari" | `marcaPadrao.test.tsx:82` `toContain('/marca/favicon.svg')`; `:87` `toContain('.../assistente?v=4')` | PASS |
| C7 | cor própria não troca a logo; `--marca` = cor | vitest "cor propria nao troca a logo do Esclari" | `marcaPadrao.test.tsx:94` `src '/marca/logo-horizontal.svg'`; `:95` `getPropertyValue('--marca')).toBe(Cor)` | PASS |
| C8 | iniciais só no avatar da pessoa | vitest "iniciais so no avatar da pessoa" | `marcaPadrao.test.tsx:104` `expect(avatares).toContain('AS')`; `:105` `not.toContain('U')` | PASS |
| C9 | `theme_color` `#f7f5ee` sem cor, `#8a1538` com cor | dotnet `ManifestoTests.Manifesto_padrao_sem_aparencia`, `Manifesto_da_instituicao` | `Esclari.Api.Tests/ManifestoTests.cs:49` `Assert.Equal("#f7f5ee", ...theme_color)`; `:26` tupla com `"#8a1538"`; `:140` `"#8a1538"` | PASS |
| C10 | ícones Esclari sem ícone; só o da instituição com ícone | dotnet `Sem_icone_proprio_lista_o_maskable`, `Com_icone_proprio_so_lista_any` | `ManifestoTests.cs:105-106` lista 192/512/maskable-512; `:116` `Equal(2, icones.Count)`; `:119` src `.../icone?v=2`; `:122` `DoesNotContain(... "maskable")` | PASS |
| C11 | `id`, `scope`, `start_url` iguais depois de trocar cor e ícone | dotnet `Trocar_a_marca_nao_muda_id_scope_nem_start_url` | `ManifestoTests.cs:142` `Assert.Equal(antes.GetProperty(campo)..., depois.GetProperty(campo)...)` para id/scope/start_url; `:140` `theme_color` mudou. Lacuna de precisão: o setup (`:134-135`) grava cor, nome curto e ícone, não envia logo; o claim cita "logo e ícone" | PASS (com lacuna) |
| C12 | sem `nomeCurto`, `name` e `short_name` = `nome` | dotnet `Manifesto_padrao_sem_aparencia` | `ManifestoTests.cs:50` `Assert.Equal(i.Nome, ...name)` fixa `name`. `:48` `Assert.Equal("CV", ...short_name)`: o teste fixa as iniciais, não o `nome`. Código: `CarregarManifesto` usa `i.NomeCurto ?? ManifestoInstituicao.Iniciais(i.Nome)`. O claim e o AC 12 ("`short_name` = `nome`, como hoje") descrevem um comportamento que o código não tem, e nenhuma prova o afirma | FAIL |
| C13 | sem logo e com MinIO fora: parte `logo` com PNG do Esclari, `cid:logo`, `max-height:40px`, nome em 20 px negrito | dotnet `EmailMarcaTests.Sem_logo_propria_leva_a_logo_do_Esclari_e_o_nome`, `Minio_fora_envia_sem_logo` | `EmailMarcaTests.cs:117` `Contains("<img src=\"cid:logo\" alt=\"Esclari\"")`; `:118` `max-height:40px`; `:119` `Matches("font-size:20px[^\"]*\">Univag &amp; Cia<")`; `:121-124` ContentID `logo`, `image/png`, bytes = `EnviadorSmtp.LogoDoEsclari()`; MinIO fora `:104` `cid:logo ... alt="Esclari"`. Lacuna: "negrito" não é asserido (o regex pega `font-size:20px` e o texto, não `font-weight`) | PASS (com lacuna) |
| C14 | com logo própria: bytes da instituição, alt = nome, sem `alt="Esclari"` | dotnet `Html_com_cor_logo_e_corpo_escapado` | `EmailMarcaTests.cs:69` `Contains("<img src=\"cid:logo\" alt=\"Univag &amp; Cia\"")`; `:76` ContentID `logo`; `:78` `Assert.Equal(logo, bytes)`; `:84` `DoesNotContain("alt=\"Esclari\"", html)` | PASS |
| C15 | PNG 348x80, assinatura, sem alfa | dotnet `MarcaDoEmailTests.Png_do_Esclari_tem_348_por_80_sem_canal_alfa` | `Esclari.Api.Tests/MarcaDoEmailTests.cs:13` assinatura; `:14` `Equal(348u, ...)`; `:15` `Equal(80u, ...)`; `:16` `Contains(png[25], new byte[] {2, 3})`. O `Content-Type` `image/png` está em `EmailMarcaTests.cs:122` | PASS |
| C16 | `EmbeddedResource` no csproj e fluxo existe | dotnet `MarcaDoEmailTests.Png_do_Esclari_esta_embutido_no_projeto` | `MarcaDoEmailTests.cs:23` `Contains("logo-horizontal-email.png", recursos)`; `:27` `Contains("<EmbeddedResource Include=\"Instituicoes\\Aparencia\\logo-horizontal-email.png\" LogicalName=...")` | PASS |
| C17 | Aparência sem imagens mostra "Marca do Esclari (padrão)" e "Imagem padrão do Esclari" | vitest "sem imagens a Aparencia mostra a marca do Esclari como padrao" | `web/src/administracao/aparencia/identidade.test.tsx:97` `getByText('Marca do Esclari (padrão)')`; `:98`, `:102` src `/marca/...`; `:105` `getByText('Imagem padrão do Esclari')` | PASS |
| C18 | arquivo escolhido: miniatura vira o arquivo, "Enviada por você", sem chamada | vitest "imagem escolhida mostra Enviada por voce sem salvar" | `identidade.test.tsx:113` `findByText('Enviada por você')`; `:115` `toHaveAttribute('src', 'blob:logo-novo')`; `:116` `expect(chamadas.filter((c) => c.metodo !== 'GET')).toEqual([])` | PASS |
| C19 | "Usar a marca do Esclari" chama DELETE e volta ao padrão; "Usar a cor padrão" esvazia a cor | vitest "usar a marca do Esclari remove a imagem e usar a cor padrao esvazia a cor" | `identidade.test.tsx:130` `queryByRole('button',{name:'Remover imagem: Logo'})` ausente; `:134` `toHaveValue('')`; `:139` DELETE `['/api/admin/aparencia/imagens/logo']`; `:140` `findByText('Marca do Esclari (padrão)')` | PASS |
| C20 | ícone fora da regra e contraste < 4,5 recusados, marca não muda | dotnet `AparenciaAdminTests.Icone_nao_quadrado_ou_pequeno_400` (4 casos); vitest "contraste ao lado da cor e Salvar desabilitado" | `AparenciaAdminTests.cs:272-275` `BadRequest`, mensagem do ícone, `Assert.Empty(await Linhas(i.Id))`, `Equal(antes, Chaves().Count)`; `aparencia.test.tsx:156,163` `1,40:1` com `toBeDisabled()`. Lacuna: o contraste é provado só na web; a recusa de cor fraca pela API não tem prova nesta feature | PASS (com lacuna) |
| C21 | (door 1) `NULL` em logo, ícone, login, assistente e cor; GET devolve `null` | dotnet `ManifestoTests.Instituicao_nova_nao_grava_a_marca_do_Esclari` | `ManifestoTests.cs:154` `Assert.Null(linha.Cor)`; `:155` `Assert.Equal(0, imagens)`; `:156` e `:159` `JsonValueKind.Null` para `cor` e as 4 figuras | PASS |
| C22 | (door 2) PNG `EmbeddedResource` e `cid:logo` igual ao de hoje | dotnet `MarcaDoEmailTests.Png_do_Esclari_esta_embutido_no_projeto` | `MarcaDoEmailTests.cs:27` linha do csproj; `cid:logo` em `EmailMarcaTests.cs:76` e `:121` | PASS |

## Coverage

Recomputado a partir do código (`Autenticado.tsx`, `Acesso.tsx`, `Assistente.tsx`, `PreviaAparencia.tsx`, `CampoImagem.tsx`, `aparencia.tsx`, `EnviadorSmtp.cs`, `LayoutEmail.cs`, `ManifestoInstituicao.cs`).

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| lugares onde entra a marca do Esclari (7) | código | cabeçalho barra/celular C1 · entrada C3 · avatar do assistente C6 · prévia da Aparência C17 (e `aparencia.test.tsx` "prévia e login mostram a mesma marca") · e-mail C13 · manifest `theme_color` C9 · manifest ícones C10 | - |
| partes da marca (4) | plano | logo C3/C4 · ícone C1/C2 · foto C6 · cor C7/C9 | - |
| temas (3) | `useTema` | claro/Escuro/Preto C3 e C5 | - |
| estados da logo no e-mail (3) | `EnviadorSmtp.cs:31` | sem logo C13 · com logo C14 · MinIO fora C13 | - |
| ações da gestão (4) | `CampoImagem.tsx` | escolher C18 · usar a marca C19 · usar a cor padrão C19 · recusas C20 | - |
| campos anuláveis (5) | entidade | logo/ícone/login/assistente/cor C21 | - |
| campos do manifest que não mudam (3) | `ManifestoInstituicao.cs` | id/scope/start_url C11 | - |
| `name` e `short_name` sem `nomeCurto` (2) | `ManifestoInstituicao.cs` / AC 12 | `name` C12; `short_name` não casa com o AC (teste fixa `"CV"`) | `short_name` = `nome` (AC 12) |

## Existing assertions

`git diff 6444f4c..HEAD -- web/src Esclari.Api.Tests`: os valores mudados são os listados (`#C83A23` -> `#f7f5ee` em `ManifestoTests.cs:49`; os dois `DoesNotContain("cid:")` em `EmailMarcaTests`; `Remover imagem` -> `Usar a marca do Esclari` em `aparencia.test.tsx`) e consequências diretas do selo de iniciais e das imagens `/marca/`: `acesso/identidade.test.tsx` (`getByText('CG')` -> logo do Esclari), `aparencia.test.tsx` (selo `U`), `assistente/boasvindas.test.tsx` e `assistente/identidade.test.tsx` (contagem de `img` ignora `/marca/`). Fora da lista de "Existing assertions changed": o teste `iniciais da prévia iguais às do login` (`administracao/aparencia/aparencia.test.tsx:231`) foi renomeado e reescrito para `prévia e login mostram a mesma marca do Esclari sem logo`. É consequência do selo, mas o `checks.md` não o lista. Nenhum teste apagado, pulado ou enfraquecido encontrado.

## Faults injected

| Mutation | Location | Killed |
| --- | --- | --- |
| `CorPadrao` `#f7f5ee` -> `#c83a23` | `Esclari.Api/Instituicoes/Carga/ManifestoInstituicao.cs:10` (worktree `git worktree add`, removido) | yes - `ManifestoTests.Manifesto_padrao_sem_aparencia`: `Expected: "#f7f5ee"` |
| `logoDoEsclari: propria is null` -> `false` | `Esclari.Api/EnviadorSmtp.cs:36` (mesmo worktree) | yes - `EmailMarcaTests.Sem_logo_propria_leva_a_logo_do_Esclari_e_o_nome`: `Sub-string not found` |
| `usePlaca` devolve sempre `''` | `web/src/aparencia.tsx:164` (cópia no repo, desfeita com `git checkout --`; `git status --porcelain` igual ao baseline) | yes - `marcaPadrao.test.tsx` "logo propria no tema escuro...": `dark: expected false to be true` |

## Gate

`dotnet test` (22 testes) e `vitest run` (22 testes) em HEAD: 44 passed, 0 failed. Os 44 passam; o veredito FAIL vem de C12 (claim sem prova que bata com o comportamento).
