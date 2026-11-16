# renomear-esclari verification

**Verdict**: FAIL
**Profile**: standard
**Diff range**: f57b031..2ce962c
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

Provas rodadas em `2ce962c` (HEAD), na árvore real, com `postgres`, `mailpit` e `minio` do compose no ar. Porcelain inicial:
`?? .specs/relatorio-nome.md` (arquivo não rastreado que já existia; não é da feature). Porcelain final: igual.

Ranked gaps:

1. C20 FAIL: `NomeDoProdutoTests.Nenhum_arquivo_rastreado_cita_One_fora_do_historico` falha em HEAD. O guarda acha o nome antigo em
   `Esclari.Api.Tests/MarcaTests.cs:36`, `:51` e `:65` (asserções negativas do próprio C4, C8 e C18), e `MarcaTests.cs` não está
   em `ArquivosDeHistorico` (`NomeDoProdutoTests.cs:14-21`). A allowlist cobre `McpFerramentasTests`, `McpAcessoTests`,
   `TokenMcpDominioTests` e os três `marca.test.*` do web, mas esqueceu `MarcaTests.cs`.
2. C19 FAIL: `dotnet test Esclari.Api.Tests` fecha com 995 aprovados e 1 falha (a do item 1). Além disso `crapper` sai com 2
   (`useModo` 11.0 e `Usuarios` 10.0 passam do limite 10), mas isso já era assim na base `f57b031` (preexistente, ver Gate).
3. Risco (não FAIL isolado): o proof HTTP de C9 (`McpAcessoTests.Recusa_token_com_prefixo_antigo_onemcp`) passa pelo motivo errado.
   Com o regex do `TokenMcp` aceitando `onemcp_` e `esclarimcp_`, o teste continua verde (o `401` vem do hash que não existe no
   banco, não do prefixo). Só o teste de domínio `TokenMcpDominioTests.Token_novo_usa_prefixo_esclarimcp` mata esse mutante.
4. Risco: `web/src/test/estrutura.test.ts` abriu exceção para `administracao/marca.test.tsx` e a raiz de `administracao/` passou de 4 para 5 arquivos (ver Pontos do construtor).

## Binding sources

Nenhuma fonte marcada como binding no plano (rename sem desenho visual). `Sources` do plano: `.specs/relatorio-nome.md` (aberto: justifica a troca de nome, não decide valor concreto de check), pedido do dono e `AGENTS.md`. Sem contradição, sem elemento descoberto.

## Checks

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | tela de acesso mostra `Esclari`, sem `One` | `npm --prefix web test -- src/acesso/marca.test.tsx` exit 0, 5 testes, 1 é "titulo da tela de acesso" | `web/src/acesso/marca.test.tsx:38` `getByRole('heading',{level:1,name:'Esclari'})` e `:39` `not.toMatch(Antigo)` | PASS |
| C2 | shell mostra `Esclari` na barra aberta e no menu de 360 px | mesmo comando, teste "marca no shell" rodou e passou | `web/src/acesso/marca.test.tsx:47` `within(barra).getByText('Esclari')` e `:55` `within(menu).getByText('Esclari')` | PASS |
| C3 | manifesto `name`/`short_name` e `<title>` iguais a `Esclari` | `npm --prefix web test -- src/test/marca.test.ts` exit 0, 1 teste | `web/src/test/marca.test.ts:10` `toMatch(/\bname:\s*'Esclari'/)`, `:11` `short_name`, `:12` `<title>Esclari</title>` | PASS |
| C4 | e-mails de primeiro acesso, de administrador e falha do Moodle citam `Esclari`; remetente `nao-responda@esclari.com.br` | `dotnet test --filter MarcaTests.Emails_citam_Esclari_e_remetente_padrao` Aprovado (949 ms) | `Esclari.Api.Tests/MarcaTests.cs:24` `Contains("senha no Esclari")`, `:25` `Equal("nao-responda@esclari.com.br", ...From.Address)`, `:29` administrador, `:34` Moodle | PASS |
| C5 | Integrações mostra `claude mcp add ... esclari`, JSON com `esclari` e `atividade Esclari` | `src/administracao/marca.test.tsx` exit 0, 2 testes "integracoes" | `web/src/administracao/marca.test.tsx:22` `toContain('atividade Esclari')`, `:37` comando `claude mcp add --transport http esclari`, `:38` JSON `{"mcpServers":{"esclari":...` | PASS |
| C6 | cor padrão se chama `Azul Esclari` e fica na parada inicial | `src/administracao/marca.test.tsx` teste "azul esclari" passou | `web/src/administracao/marca.test.tsx:47` `getByRole('radio',{name:'Azul Esclari'})`, `:48` `tabIndex === 0` igual a `[azul]` | PASS |
| C7 | tema e barra gravam `esclari-tema`/`esclari-barra`, nenhuma `one-*` | `src/acesso/marca.test.tsx` teste "chaves esclari" passou | `web/src/acesso/marca.test.tsx:64` `getItem('esclari-tema')` `toBe('dark')`, `:67` `esclari-barra` `recolhida`, `:69` `filter(c => c.startsWith('one-'))` `toEqual([])` | PASS |
| C8 | login emite `esclari.auth` e nenhum `one.auth` | `dotnet test --filter MarcaTests.Cookie_de_sessao_se_chama_esclari_auth` Aprovado (94 ms) | `Esclari.Api.Tests/MarcaTests.cs:50` `Assert.Single(cookies, c => c.StartsWith("esclari.auth="))`, `:51` `DoesNotContain(... "one.auth=")`; app: `Esclari.Api/Acesso/AcessoExtensoes.cs:18` | PASS |
| C9 | token novo `esclarimcp_...`; `onemcp_...` recebe `401` | `--filter TokenMcpDominioTests.Token_novo_usa_prefixo_esclarimcp` e `McpAcessoTests.Recusa_token_com_prefixo_antigo_onemcp`, ambos Aprovado | `Esclari.Api.Tests/TokenMcpDominioTests.cs:62` `Matches(@"^esclarimcp_univag\.[A-Za-z0-9_-]{43}$")`, `:63` `Null(SlugDoMcp("onemcp_univag."...))`; `McpAcessoTests.cs:53` `Equal(Unauthorized, ...antigo)` | PASS (com a ressalva do item 3) |
| C10 | contador 122 grava `ESC-000123`; primeiro grava `ESC-000001` | `--filter TransferenciaTests.Numero_por_instituicao` e `...Numero_com_seis_digitos_apos_o_contador_122`, ambos Aprovado | `Esclari.Api.Tests/TransferenciaTests.cs:194` `Equal("ESC-000123", await Direto(aluno))`, `:173` `Equal("ESC-000001", ...)`, `:174` `ESC-000002`, `:175` demo `ESC-000001` | PASS |
| C11 | descrição da ferramenta MCP cita `ESC-000012` | `--filter McpFerramentasTests.Descricao_cita_exemplo_ESC_000012` Aprovado (1 s) | `Esclari.Api.Tests/McpFerramentasTests.cs:61` `Contains("ESC-000012", descricao)` e `:62` `DoesNotContain("ONE-", descricao)` | PASS |
| C12 | placeholder `Ex.: ESC-000123…` | `src/chamados/marca.test.tsx` exit 0, 1 teste | `web/src/chamados/marca.test.tsx:11` `findByPlaceholderText('Ex.: ESC-000123…')` | PASS |
| C13 | duplicado de `ESC-000007` responde `200` e grava `numero_original`; número inexistente responde `400` | `--filter ExtrasTests.Duplicado_grava_numero_original_no_formato_novo` Aprovado (1 s) | `Esclari.Api.Tests/ExtrasTests.cs:157` `Equal(OK, r.StatusCode)`, `:158` `Equal("ESC-000007", (await b.Ler(repetido)).NumeroOriginal)`, `:152` `Equal(BadRequest, inexistente.StatusCode)` | PASS |
| C14 | cabeçalho `ESC-000123` leva `translate="no"`, título comum não leva | `src/acesso/marca.test.tsx` 2 testes "protocolo sem traducao" passaram | `web/src/acesso/marca.test.tsx:22` `toHaveAttribute('translate','no')`, `:30` `some(h => h.hasAttribute('translate'))` `toBe(false)` | PASS |
| C15 | vinte aberturas paralelas dão vinte números distintos `ESC-NNNNNN` | `--filter TransferenciaTests.Vinte_transferencias_paralelas_numeros_distintos` Aprovado (221 ms) | `Esclari.Api.Tests/TransferenciaTests.cs:185` `Equal(Enumerable.Range(1,20).Select(n => $"ESC-{n:D6}").Order(), numeros.Order())` | PASS |
| C16 | `dotnet build Esclari.slnx` sai 0; `ArquiteturaTests` passa | `dotnet build Esclari.slnx` exit 0 (0 erros, 9 avisos); `--filter ArquiteturaTests` 82 aprovados, 0 falhas | `Esclari.Api.Tests/ArquiteturaTests.cs:92` `Contains("Esclari.Api/Esclari.Api.csproj", solucao)`, `:416` `esperado = "Esclari.Api." + pasta` com `SequenceEqual([esperado])` em `:418` | PASS |
| C17 | sem mudança de modelo pendente | `dotnet ef migrations has-pending-model-changes --project Esclari.Api` exit 0, "No changes have been made to the model since the last migration." | `Esclari.Api/Dados/Migracoes/EsclariDbModelSnapshot.cs:358` `modelBuilder.Entity("Esclari.Api.Dados.Chamados.Chamado", b =>` (o comando decide; a falta G o derruba) | PASS |
| C18 | compose cria banco, usuário e bucket `esclari`; API lê `ConnectionStrings__Esclari` | `docker compose exec -T postgres psql -U postgres -d esclari -At -c "select 1"` saída `1`, exit 0; `--filter MarcaTests.Compose_usa_nome_esclari` Aprovado | `Esclari.Api.Tests/MarcaTests.cs:58` `POSTGRES_DB:\s*esclari`, `:59` `MINIO_ROOT_USER`, `:61` `Armazenamento__Bucket`, `:62` `Contains("ConnectionStrings__Esclari:")` | PASS |
| C19 | suítes API e web, build web e gates sem teste apagado ou enfraquecido | `dotnet test Esclari.Api.Tests` exit 1 (995 aprovados, 1 falha); `npm --prefix web test` 1001 aprovados exit 0; `npm --prefix web run build` exit 0; `crapper.exe --use-existing-coverage --threshold 10 src` exit 2; `dryer --threshold 0.8` nas 6 fatias e raiz: "No duplicate candidates found." | falha em `Esclari.Api.Tests/NomeDoProdutoTests.cs:87` (`Assert.True(achados.Count == 0, ...)`); crapper: `useModo` 11.0 em `web/src/Autenticado.tsx` e `Usuarios` 10.0 (ambos já na base) | FAIL |
| C20 | nenhum arquivo rastreado cita `One` fora do histórico | `--filter NomeDoProdutoTests.Nenhum_arquivo_rastreado_cita_One_fora_do_historico` exit 1 (rodado 2 vezes, mesmo resultado) | `Esclari.Api.Tests/NomeDoProdutoTests.cs:87` `Assert.True(achados.Count == 0 ...)` falha listando `MarcaTests.cs:36`, `:51`, `:65` | FAIL |
| C21 | `STATE.md` tem AD novo com `Esclari`, `ESC-NNNNNN`, `esclarimcp_` | `--filter NomeDoProdutoTests.State_registra_o_AD_do_nome` Aprovado (6 ms) | `Esclari.Api.Tests/NomeDoProdutoTests.cs:95` `Contains("Esclari", ad)`, `:96` `ESC-NNNNNN`, `:97` `esclarimcp_`; linha `.specs/STATE.md:63` `| AD-058 |` | PASS |

Todos os testes nomeados existem e rodaram (cada um aparece individualmente na saída, ≥1 teste). Nenhum filtro casou zero testes.
Os testes nomeados são todos novos ou alterados na feature (`git diff f57b031..HEAD`), nenhum resolve a teste intocado.

## Coverage

Recomputado a partir das autoridades (plano `Surface`, `Landing`, e-mails e telas do AC, campos do manifesto), não do que o construtor escreveu.

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| `GET /api/chamados/{numero}` statuses (2) | plan Surface | 200 `ESC-000001` via fluxos existentes e `TransferenciaTests.cs:22` (corpo da criação); 404 em `VisibilidadeTests.cs:364` e `IsolamentoTests.cs:57`, não em C13 | - |
| `POST /api/chamados/{numero}/duplicado` statuses (3) | plan Surface | 200 `ExtrasTests.cs:157`; 400 `ExtrasTests.cs:152` e `:169`; 404 `IsolamentoTests.cs:42` + `:57` (atribuído a C13 no checks.md, mas o proof de C13 não afirma 404) | - |
| door 1 formato do protocolo (2 casos) | plan Landing | contador 1 `TransferenciaTests.cs:173`; contador 123 `TransferenciaTests.cs:194` | - |
| door 2 prefixo do token MCP (2) | plan Landing | `esclarimcp_` `TokenMcpDominioTests.cs:62`; `onemcp_` recusado `TokenMcpDominioTests.cs:63` (domínio) e `McpAcessoTests.cs:53` (HTTP, vazio quanto ao prefixo) | - |
| door 3 identificadores persistidos (6) | plan Landing | cookie `MarcaTests.cs:50`; `esclari-tema` `marca.test.tsx:64`; `esclari-barra` `marca.test.tsx:67`; banco/bucket/usuário `MarcaTests.cs:58-61`; `UserSecretsId` `esclari-api` só tem guarda negativa (`NomeDoProdutoTests.cs:9` regex) e nenhuma asserção do valor | `UserSecretsId` = `esclari-api` (`Esclari.Api/Esclari.Api.csproj:8`) |
| texto da marca por tela (5) | plan AC 1, 2, 5, 6, 14 | acesso `acesso/marca.test.tsx:38`; shell `:47`/`:55`; integrações `administracao/marca.test.tsx:22`/`:37`; aparência `:47`; cabeçalho do chamado `acesso/marca.test.tsx:22` | - |
| e-mails com a marca (3) | plan AC 4 | primeiro acesso `MarcaTests.cs:24`; administrador `:29`; falha do Moodle `:34` | - |
| campos do manifesto (3) | plan AC 3 | `name` `marca.test.ts:10`; `short_name` `:11`; `<title>` `:12` | - |
| ocorrências de `One` no repositório (7) | plan Impact | `One.Api`/`One.Api.Tests`/`One.slnx` C16 `ArquiteturaTests.cs:92`; `OneDb`/snapshot C17 (exit 0); infra C18 `MarcaTests.cs:64`; web, e2e, apresentação e documentos vivos C20, cujo guarda está vermelho | web/e2e/apresentação/documentos vivos: guarda C20 falha em HEAD (só por `MarcaTests.cs`, ver item 1); outros arquivos não foram listados como achado |
| startup config: cookie e sessão (1) | `Esclari.Api.Tests/Infra/EsclariFactory.cs:24` é `WebApplicationFactory<Program>`, o `Program` do app; cookie definido no app em `AcessoExtensoes.cs:18` | C8 `MarcaTests.cs:50` | - |
| superfícies no celular (1) | plan Observable | 360×780: `acesso/marca.test.tsx:55` afirma o diálogo Menu; o texto `Esclari` fica no `SidebarHeader` (`web/src/Autenticado.tsx:216`), não no cabeçalho de página (`:265`); `e2e/arranjo.spec.ts` 66 aprovados contra HEAD :8080 não mede a largura do rótulo | - |

Coluna Unproven: `UserSecretsId` conta como sem prova real, porque nenhum teste afirma o valor `esclari-api`, só proíbe `one`. Sozinho esse membro já reprova a cobertura. A linha de ocorrências de `One` depende do C20, que está vermelho só por causa da allowlist de `MarcaTests.cs`.

## Test policy rows

| Row | Files it classifies | Required proof | Expectation met |
| --- | --- | --- | --- |
| Decide, atravessa fronteira | `AbrirChamado.cs`, `TokenMcp.cs` | `AbrirChamado`: HTTP `TransferenciaTests.cs:22,173-175,194` e paralelo `:185`, sem teste unitário separado (o formato vive no handler). `TokenMcp`: domínio `TokenMcpDominioTests.cs:62-63` e HTTP `McpAcessoTests.cs:53` | yes (a prova HTTP de `onemcp_` é vazia, ver item 3) |
| Decide, sem fronteira | `Chamado.MarcarDuplicado` | `ChamadoDominioTests.cs:72-74` linha 16 da tabela com `ESC-000009`; 11 testes da classe aprovados | yes |
| Instrumentação: troca de texto | telas, e-mails, manifesto | coberta por `marca.test.*`, `MarcaTests` e `NomeDoProdutoTests` | yes |

## Faults injected

Mutações feitas à mão na árvore real e revertidas com `git checkout -- <arquivo>`; `git status --porcelain` voltou ao baseline depois de cada uma.

| Mutation | Location | Killed |
| --- | --- | --- |
| (a1) protocolo com 5 dígitos: `{numero.Single():D6}` para `D5` | `Esclari.Api/Chamados/Acoes/AbrirChamado.cs:22`; mortos `TransferenciaTests.Numero_com_seis_digitos_apos_o_contador_122`, `Numero_por_instituicao`, `Vinte_transferencias_paralelas_numeros_distintos`, `Transferir_201_aberto_sem_responsavel`, `ExtrasTests.Duplicado_grava_numero_original_no_formato_novo` (5 falhas) | yes |
| (a2) prefixo `ESC_` no lugar de `ESC-` | `Esclari.Api/Chamados/Acoes/AbrirChamado.cs:22`; os mesmos 5 testes falharam | yes |
| (b) prefixo do token volta para `onemcp_` | `Esclari.Api/Dados/Mcp/TokenMcp.cs:30,32`; mortos `TokenMcpDominioTests.Token_novo_usa_prefixo_esclarimcp` e `NovoMcp_e_SlugDoMcp_se_reconhecem_e_recusam_o_resto` | yes |
| (b2) regex do token aceita `onemcp_` e `esclarimcp_` | `Esclari.Api/Dados/Mcp/TokenMcp.cs:30`; morto só por `TokenMcpDominioTests.Token_novo_usa_prefixo_esclarimcp`; `McpAcessoTests` continuou verde | yes |
| (c) cookie volta para `one.auth` | `Esclari.Api/Acesso/AcessoExtensoes.cs:18`; morto `MarcaTests.Cookie_de_sessao_se_chama_esclari_auth` | yes |
| (d1) manifesto `name: 'One'` | `web/vite.config.ts:18`; morto `src/test/marca.test.ts` (`expected ... to match /\bname:\s*'Esclari'/`) | yes |
| (d2) manifesto `short_name: 'One'` | `web/vite.config.ts:19`; morto `src/test/marca.test.ts` | yes |
| (d3) `<title>One</title>` | `web/index.html:10`; morto `src/test/marca.test.ts` | yes |
| (e) e-mail de primeiro acesso volta a dizer `One` | `Esclari.Api/Acesso/PrimeiroAcesso.cs:42`; morto `MarcaTests.Emails_citam_Esclari_e_remetente_padrao` | yes |
| (f) comentário `// One` num arquivo rastreado | `Esclari.Api/Dados/Mcp/TokenMcp.cs` (linha 52 injetada); `NomeDoProdutoTests.Nenhum_arquivo_rastreado_cita_One_fora_do_historico` listou `TokenMcp.cs:52` além dos 3 achados antigos de `MarcaTests.cs` (o guarda já estava vermelho, mas aponta a injeção) | yes |
| (g) nome de tipo do snapshot muda (`Chamado` para `ChamadoX`) | `Esclari.Api/Dados/Migracoes/EsclariDbModelSnapshot.cs:358`; `dotnet ef migrations has-pending-model-changes` saiu 1 (exceção de relacionamento); restaurado e voltou a 0 | yes |
| (h) título do acesso `One` | `web/src/acesso/Acesso.tsx:55` `<H1>`; morto `acesso/marca.test.tsx` "titulo da tela de acesso" | yes |
| (i) placeholder `ESC-123` | `web/src/chamados/ChamadoDetalhe.tsx:356`; morto `chamados/marca.test.tsx` | yes |
| (j) regex do `translate` volta a `ONE` | `web/src/Autenticado.tsx:265`; morto `acesso/marca.test.tsx` "o cabeçalho com ESC-000123 leva translate no" | yes |
| (k) chave `esclari-barra` volta a `one-barra` | `web/src/Autenticado.tsx:330,334`; morto `acesso/marca.test.tsx` "chaves esclari" | yes |

Nenhum mutante sobreviveu. Fica a observação do item 3 (o `McpAcessoTests` sozinho não pega a falta b2).

## Pontos do construtor

- (i) `estrutura.test.ts` (`git diff f57b031..HEAD -- web/src/test/estrutura.test.ts`): a contagem sobe 52 para 53 (um arquivo novo, `marca.test.tsx`) e a lista dos arquivos na raiz de `administracao/` ganha `marca.test.tsx`. Isso mais fraco: `administracao/` passa de 4 para 5 arquivos na raiz (AD-056 fala em no máximo 4 na raiz da fatia) e o teste é excluído do laço que exige que cada `*.test.tsx` fique na subpasta da rota que abre (`testes.filter((a) => a !== 'marca.test.tsx')`). Nenhuma asserção foi apagada, mas a regra estrutural foi afrouxada para um arquivo. Risco, não bloqueia sozinho.
- (ii) `AparenciaAdminTests.Limites_dos_textos_400`: só muda o preenchimento do e-mail de `tamanho - 10 + "@teste.one"` para `tamanho - "@teste.esclari".Length + "@teste.esclari"`; o comprimento total continua `tamanho`, e `{ "contato.email", 120 }` segue em `Limites`. Limite 120 preservado. OK.
- (iii) `NumeracaoTests`: não existe no base `f57b031` como classe (só aparece em textos de `.specs`); os testes `Numero_por_instituicao` e `Vinte_transferencias_paralelas_numeros_distintos` já estavam em `TransferenciaTests`. Comparei os métodos `public ... Task/void` de `One.Api.Tests` (832) com `Esclari.Api.Tests` (842): nenhum sumiu (só os 5 métodos de `OneFactory` que viraram `EsclariFactory`); 10 novos.
- (iv) Allowlist de `NomeDoProdutoTests`: pastas `.specs/features/`, `web/node_modules/`, `Esclari.Api/wwwroot/`; 11 arquivos; 8 trechos permitidos (`github.com/Bitforgee/One`, `D:\One\One`, `D:\one-system`, `mvp-one`, `mcp-one`, `prompt-evolucao-one.md`, `One or more validation errors`, `=== 'one'`). Cada arquivo de teste de marca está justificado, mas o próprio `MarcaTests.cs` ficou de fora (item 1). A isenção por pasta `Esclari.Api/wwwroot/` é gerada; o trecho `D:\one-system` apaga qualquer menção ao caminho da pasta.
- (v) Varredura do diff: nenhum `Skip`, `it.skip`, `test.skip`, `xit`, `.todo(` ou `.only(` adicionado; as linhas de asserção `Assert.` sobem (51 removidas, 90 adicionadas nos testes C#) e `expect` sobe (63 removidas, 88 adicionadas no web). Não há teste apagado. Não fiz comparação semântica linha a linha de todas as 568 mudanças; as comparações por amostra mostram só troca de nome e valor.
- (vi) Migrations: 27 arquivos na base e 27 em HEAD, nenhuma adicionada; normalizando `One.Api`, `OneDb`, `ONE` as linhas removidas e adicionadas de `Migracoes/` se casam todas, nenhuma linha de `Up`/`Down` mudou.
- (vii) Segredos: nenhum `.env`, `secrets.json`, `.pem` ou `.pfx` em `git diff --name-only`; sem padrão de chave (`sk-`, `AKIA`, `PRIVATE KEY`) nas linhas adicionadas.

## Gate

- `dotnet build Esclari.slnx` - 0 erros.
- `dotnet test Esclari.Api.Tests` - 995 passed, 1 failed (`NomeDoProdutoTests.Nenhum_arquivo_rastreado_cita_One_fora_do_historico`).
- `npm --prefix web test` - 1001 passed, 0 failed. `npm --prefix web run build` - exit 0.
- `crapper.exe --use-existing-coverage --threshold 10 src` (depois de `npm --prefix web run coverage`) - exit 2, `useModo` 11.0 e `Usuarios` 10.0. Na base (worktree em `f57b031`, mesmo comando) o resultado é idêntico (`useModo` 11.0, `Usuarios` 10.0): preexistente, não é regressão da feature.
- `dryer --threshold 0.8` nas 6 fatias e nos arquivos da raiz de `web/src` (AD-023) - "No duplicate candidates found" em HEAD e na base. Rodado sobre `web/src` inteiro, o `dryer` lista candidatos só em `src/ui/` (código shadcn), fora do escopo do AD-023.
- e2e, classificação das 9 falhas relatadas (contra a API do compose em `:8080` rodando HEAD, `workers: 1`):
  - `chat.spec.ts:47` "campo alto" (altura 100, esperado >150): PREEXISTENTE. Roda o mesmo spec na base `f57b031` (Vite do worktree) e em HEAD (Vite do repo): ambos 9 passaram e 1 falhou, no mesmo teste.
  - `instituicoes-real.spec.ts:46`: AMBIENTE. O spec usa `docker exec ${E2E_CONTAINER ?? 'esclari-plataforma'}`; o compose não tem `container_name` na base nem em HEAD, o container local se chama `one-system-api-1`. Com `E2E_CONTAINER=one-system-api-1` o teste passou.
  - `casca-real`, `identidade-real`, `fluxos-real:141`: AMBIENTE. Falta a instituição `e2e` no banco novo `esclari`; depois de `docker exec one-system-api-1 dotnet Esclari.Api.dll instituicao criar --slug e2e ...` os três passaram.
  - `ux-real.spec.ts:76` e `:131`: após criar `e2e`, falham em `getByLabel('Senha')` (strict mode, 2 elementos: o campo e o botão "Mostrar senha"). Preexistente por evidência estática: `git show f57b031:web/e2e/ux-real.spec.ts` já usa `getByLabel('Senha')` e `Acesso.tsx` da base já tem `aria-label 'Mostrar senha'`. Não rodei na base (precisaria subir a API da base com banco `one`).
  - `aparencia-real.spec.ts:66` (precisa do admin de `demo`) e `fluxos-real.spec.ts:181`: não consegui provar a causa. Criar `demo` não resolveu. São AMBIENTE provável (dados demo ausentes), sem prova de preexistência.
  - Efeito colateral no ambiente local: criei as instituições `e2e` e `demo` no banco de desenvolvimento `esclari` do compose (CLI da própria API); nenhum arquivo rastreado mudou.
- `arranjo.spec.ts` (celular 360): 66 passed contra HEAD.
- `git worktree` temporário de `f57b031` removido.

`validate_verification.py renomear-esclari` sai com 1 com um único erro: "verdict is FAIL". Sem erro estrutural nem aviso (perfil `standard`, seções de Coverage, Faults e Test policy presentes).
