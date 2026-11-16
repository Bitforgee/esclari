# renomear-esclari checks

Profile: standard
Plan: `.specs/features/renomear-esclari/plan.md`

21 checks in 3 slices · 3 one-way doors · 0 open, 0 block

Os caminhos já usam o nome novo (`Esclari.Api.Tests`, `Esclari.slnx`). Os testes são escritos a partir destes checks, nunca lendo a implementação. Teste existente que cita `One`/`ONE-n` só troca o nome e o valor esperado, nunca a força da asserção.

## Checks

### S1 - o produto se chama Esclari para quem usa · ~30 arquivos · 300 KB · ~75k

**C1** - A tela de acesso mostra o título `Esclari` e nenhum texto `One` (AC 1)
Proof: `npm --prefix web test -- src/acesso/marca.test.tsx -t "titulo da tela de acesso"`

**C2** - O shell autenticado mostra `Esclari` na barra lateral (aberta) e no cabeçalho do celular 360 px (AC 2)
Proof: `npm --prefix web test -- src/acesso/marca.test.tsx -t "marca no shell"`

**C3** - `vite.config.ts` declara `name` e `short_name` iguais a `Esclari` e `index.html` tem `<title>Esclari</title>` (AC 3)
Proof: `npm --prefix web test -- src/test/marca.test.ts -t "manifesto e titulo"`

**C4** - Os e-mails de primeiro acesso e de administrador de instituição citam `Esclari`, a mensagem de falha do Moodle cita `Esclari`, e o remetente padrão é `nao-responda@esclari.com.br` (AC 4)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MarcaTests.Emails_citam_Esclari_e_remetente_padrao"`

**C5** - Integrações mostra `claude mcp add --transport http esclari <url>`, o JSON com a chave `esclari` e o texto `atividade Esclari` do Moodle, sem `One` (AC 5)
Proof: `npm --prefix web test -- src/administracao/marca.test.tsx -t "integracoes"`

**C6** - A lista de cores da aparência chama a cor padrão de `Azul Esclari` e a deixa marcada na parada inicial (AC 6)
Proof: `npm --prefix web test -- src/administracao/marca.test.tsx -t "azul esclari"`

**C7** - Escolher o tema grava `esclari-tema` e recolher a barra grava `esclari-barra`; nenhuma chave `one-*` é escrita (AC 7)
Proof: `npm --prefix web test -- src/acesso/marca.test.tsx -t "chaves esclari"`

**C8** - O login emite o cookie `esclari.auth` e nenhum cookie `one.auth` (AC 8)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MarcaTests.Cookie_de_sessao_se_chama_esclari_auth"`

**C9** - Um token MCP novo começa com `esclarimcp_` e passa no formato; `onemcp_<slug>.<43 caracteres>` recebe `401` (AC 9)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TokenMcpDominioTests.Token_novo_usa_prefixo_esclarimcp"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~McpAcessoTests.Recusa_token_com_prefixo_antigo_onemcp"`

### S2 - protocolo novo · ~12 arquivos · 120 KB · ~30k

**C10** - Na instituição com contador em 122, o próximo chamado grava `numero` igual a `ESC-000123`; no primeiro, `ESC-000001` (AC 10)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TransferenciaTests.Numero_por_instituicao"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TransferenciaTests.Numero_com_seis_digitos_apos_o_contador_122"`

**C11** - A descrição da ferramenta MCP que abre chamado cita o exemplo `ESC-000012` (AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~McpFerramentasTests.Descricao_cita_exemplo_ESC_000012"`

**C12** - O campo do chamado original do duplicado mostra o placeholder `Ex.: ESC-000123…` (AC 12)
Proof: `npm --prefix web test -- src/chamados/marca.test.tsx -t "placeholder do original"`

**C13** - Marcar duplicado de `ESC-000007` responde `200` e grava `numero_original` igual a `ESC-000007`; um número que não existe responde `400` (AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ExtrasTests.Duplicado_grava_numero_original_no_formato_novo"`

**C14** - O cabeçalho que mostra `ESC-000123` leva `translate="no"` e um título comum não leva (AC 14)
Proof: `npm --prefix web test -- src/acesso/marca.test.tsx -t "protocolo sem traducao"`

**C15** - Vinte aberturas paralelas na mesma instituição gravam vinte números distintos no formato `ESC-NNNNNN` (AC 15)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TransferenciaTests.Vinte_transferencias_paralelas_numeros_distintos"`

### S3 - nenhum "One" no código, na infra e nos documentos vivos · ~560 arquivos (script) · ~40k lidos

**C16** - `dotnet build Esclari.slnx` sai com 0 e `ArquiteturaTests` passa com os namespaces `Esclari.Api.*` (AC 16)
Proof: `dotnet build Esclari.slnx`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArquiteturaTests"`

**C17** - O modelo do EF não tem mudança pendente depois do rename (AC 17)
Proof: `dotnet ef migrations has-pending-model-changes --project Esclari.Api`

**C18** - O compose cria o banco, o usuário e o bucket `esclari` e a API lê `ConnectionStrings__Esclari` (AC 18)
Proof: `docker compose up -d postgres minio ; docker compose exec -T postgres psql -U postgres -d esclari -At -c "select 1"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MarcaTests.Compose_usa_nome_esclari"`

**C19** - A suíte da API, a suíte web e os gates `crapper` e `dryer` passam sem teste apagado, pulado ou com asserção enfraquecida (AC 19)
Proof: `dotnet test Esclari.Api.Tests`
Proof: `npm --prefix web test`
Proof: `npm --prefix web run build`
Proof: `cd web ; npx crapper` e `cd web ; npx dryer` (os comandos dos gates do AGENTS.md, AD-018)

**C20** - Nenhum arquivo rastreado cita `One`/`ONE`/`one` como nome do produto, salvo a lista de histórico do plano (AC 20)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~NomeDoProdutoTests.Nenhum_arquivo_rastreado_cita_One_fora_do_historico"`

**C21** - O `STATE.md` tem um AD novo que cita `Esclari`, `ESC-NNNNNN` e `esclarimcp_` (AC 21)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~NomeDoProdutoTests.State_registra_o_AD_do_nome"`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| `GET /api/chamados/{numero}` statuses (2) | 200 C10 · 404 C13 | - |
| `POST /api/chamados/{numero}/duplicado` statuses (3) | 200 C13 · 404 C13 · 400 C13 | - |
| Landing door 1: formato do protocolo (2 casos) | contador 1 C10 · contador 123 C10 | - |
| Landing door 2: prefixo do token MCP (2) | `esclarimcp_` emitido C9 · `onemcp_` recusado C9 | - |
| Landing door 3: identificadores persistidos (6) | cookie C8 · `esclari-tema` C7 · `esclari-barra` C7 · banco C18 · bucket e usuário C18 · `UserSecretsId` C20 | - |
| texto da marca por tela (5) | acesso C1 · shell C2 · integrações C5 · aparência C6 · cabeçalho do chamado C14 | - |
| e-mails com a marca (3) | primeiro acesso C4 · administrador de instituição C4 · falha do Moodle C4 | - |
| ocorrências de `One` no repositório (7) | `One.Api` C16 · `One.Api.Tests` C16 · `One.slnx` C16 · `OneDb`/snapshot C17 · infra (compose, Dockerfile, ignore) C18 · web (src, e2e, apresentação) C20 · documentos vivos (AGENTS, ROADMAP, GLOSSARIO) C20 | - |
| startup config: cookie e política de sessão (1 assembly de teste e o app) | `OneFactory`→`EsclariFactory` usa `Program.cs` do app, C8 | - |
| Surface: manifesto PWA (3 campos) | `name` C3 · `short_name` C3 · `<title>` C3 | - |
| superfícies no celular com check (1) | 360×780 C2 | - |

- Celular deitado 844×390, teclado aberto e toque: n/a, só troca de texto, sem campo nem alvo novo (plan.md, Observable)
- Claims que citam status ou formato de fronteira: C10, C13, C15, C9. Cada um tem prova que cruza a fronteira HTTP
- Nenhum outro check afirma mais do que o caso que a prova exercita

## Test policy

| Code | Required proofs | Coverage expectation |
| --- | --- | --- |
| Decide, atravessa fronteira (`AbrirChamado` formata protocolo; `TokenMcp` valida prefixo) | uma no limite HTTP **e** uma na própria camada | contrato na fronteira; um caso por linha da tabela de decisão na própria camada |
| Decide, sem fronteira (`Chamado.MarcarDuplicado`) | uma na entidade | um caso por linha da tabela de decisão |
| Instrumentação: troca de texto sem condicional | nenhuma própria | coberta pelo teste da tela ou pelo `NomeDoProdutoTests` |

Evidence:

- `Esclari.Api/Chamados/Acoes/AbrirChamado.cs`: formata um número, 1 ponto de decisão (`Single`) -> decisão (formato do protocolo, door 1)
- `Esclari.Api/Dados/Mcp/TokenMcp.cs`: regex com prefixo e tamanho, 2 pontos (prefixo, tamanho do slug) -> decisão
- `ChamadoDominioTests.cs`: já prova `MarcarDuplicado` por tabela com 16 linhas; só troca o valor `ONE-9` por `ESC-000009`
- Tela com só troca de texto: instrumentação

Cost: 2 provas na própria camada em 2 arquivos novos (`MarcaTests`, `NomeDoProdutoTests`).

## Swept

- validation: C9 (prefixo e tamanho do token), C13 (número do duplicado inexistente retorna 400)
- failure modes: C13 (`404` e `400`); a falha de rename no build é pega por C16
- idempotency: n/a - o rename é determinístico e reexecutável; abrir chamado já é transacional (C15)
- authorization: existing - o grupo `/chamados` já exige sessão; o rename não muda guarda nem papel (C19 roda a suíte inteira)
- concurrency: C15
- data lifecycle: n/a - nenhum dado existe (sem produção, decisão do dono); sem migration de dados
- dependency failure: n/a - nenhuma chamada externa nova; o remetente padrão é só texto de configuração (C4)
- state transitions: C13 (duplicado fecha o chamado sem mudar a máquina de estados; C19 roda `ChamadoDominioTests`)
- observability: n/a - nenhum log ou métrica referencia o nome; `EsclariDb` e os nomes de hub/log mudam só de texto (C16)

## Out of scope

- Novo ícone, logotipo e arte do app - precisa de arte; entra em feature própria
- Reescrever histórico fechado em `.specs` - preserva a evidência de verificação
- Renomear a pasta `D:\one-system` e o remoto Git - fora do repositório

## Handoff

Estimativa: arquivos lidos à mão, `wc -c / 4`. O rename em massa é feito por script e não conta como leitura.

- S3 (rename mecânico + infra + docs): 560 arquivos tocados por script, ~40k lidos (compose, Dockerfile, csproj, AGENTS, ROADMAP, STATE)
- S2 (protocolo + token): `AbrirChamado`, `TokenMcp`, `FerramentasMcp`, `ChamadoDetalhe`, 5 arquivos de teste = ~30k
- S1 (textos web e e-mails): 86 arquivos web (980 KB) com regex, ~25 lidos = ~75k
- Total ~145k: dentro do orçamento de 150k, mas sem folga; por isso o corte é por slice, em lotes sequenciais, cada um só depois do verde do anterior (pedido do dono: usar subagentes). Ordem: S3 mecânico, depois S2, depois S1.
- Mechanism: handoff (um subagente construtor por slice, em sequência) - decisão do dono, "usar subagentes"
