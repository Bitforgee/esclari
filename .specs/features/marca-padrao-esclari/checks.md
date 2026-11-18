# Marca padrão do Esclari checks

Profile: standard
Plan: `.specs/features/marca-padrao-esclari/plan.md`

22 checks in 3 slices · 2 one-way doors · 0 open, 0 block

Provas: web `npm --prefix web test -- <arquivo> -t "<nome>"` (Vitest); API `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~<Classe>.<Teste>"` (testes de integração pedem `docker compose up -d postgres mailpit minio`).

## Checks

### S1 - Telas e aplicativo instalado · 5 arquivos web e 1 da API · ~14k

**C1** - Sem ícone da instituição, o cabeçalho da barra lateral mostra `<img src="/marca/favicon.svg">` ao lado do nome, `alt` vazio, e nenhum selo com as iniciais da instituição (AC 1, AC 8)
Proof: `npm --prefix web test -- src/marcaPadrao.test.tsx -t "sem icone o cabecalho mostra o simbolo do Esclari"`

**C2** - Com ícone, o cabeçalho mostra `/api/instituicoes/univag/imagens/icone?v=2` (AC 2)
Proof: `npm --prefix web test -- src/marcaPadrao.test.tsx -t "com icone mostra o icone da instituicao"`

**C3** - Sem logo, a tela de entrada mostra `<img alt="Esclari">` com `/marca/logo-horizontal.svg` no tema claro e `/marca/logo-horizontal-escuro.svg` nos temas Escuro e Preto, e o nome da instituição como `h1` (AC 3)
Proof: `npm --prefix web test -- src/marcaPadrao.test.tsx -t "entrada sem logo mostra a logo do Esclari"`

**C4** - Com logo, a tela de entrada e a barra lateral mostram `/api/instituicoes/univag/imagens/logo?v=3` e nenhuma imagem de `/marca/` (AC 4)
Proof: `npm --prefix web test -- src/marcaPadrao.test.tsx -t "com logo mostra so a logo da instituicao"`

**C5** - No tema Escuro e no Preto a logo própria fica dentro de um elemento com a classe `bg-white`; no tema claro, não (AC 5)
Proof: `npm --prefix web test -- src/marcaPadrao.test.tsx -t "logo propria no tema escuro fica sobre placa branca"`

**C6** - Sem foto, o avatar do assistente no cabeçalho da conversa é `/marca/favicon.svg`; com foto, é `/api/instituicoes/univag/imagens/assistente?v=4` (AC 6)
Proof: `npm --prefix web test -- src/marcaPadrao.test.tsx -t "assistente sem foto usa o simbolo do Esclari"`

**C7** - Com cor própria e sem logo, a tela de entrada ainda mostra a logo do Esclari e `--marca` vale a cor da instituição no documento (AC 7)
Proof: `npm --prefix web test -- src/marcaPadrao.test.tsx -t "cor propria nao troca a logo do Esclari"`

**C8** - O avatar da pessoa logada continua com as iniciais do nome dela (AC 8)
Proof: `npm --prefix web test -- src/marcaPadrao.test.tsx -t "iniciais so no avatar da pessoa"`

**C9** - O manifest sem cor própria devolve `theme_color` `#f7f5ee` e, com cor `#8a1538`, devolve `#8a1538` (AC 9)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests.Manifesto_padrao_sem_aparencia"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests.Manifesto_da_instituicao"`

**C10** - O manifest sem ícone lista `/icons/icon-192.png`, `/icons/icon-512.png` e `/icons/icon-maskable-512.png`, e com ícone lista só o da instituição (AC 10)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests.Sem_icone_proprio_lista_o_maskable"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests.Com_icone_proprio_so_lista_any"`

**C11** - Depois de a gestão trocar a cor e enviar logo e ícone, `id`, `scope`, `start_url` e o endereço do manifest são os mesmos de antes (AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests.Trocar_a_marca_nao_muda_id_scope_nem_start_url"`

**C12** - Sem `nomeCurto`, `name` e `short_name` do manifest são o `nome` da instituição (AC 12)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests.Manifesto_padrao_sem_aparencia"`

### S2 - E-mail · 3 arquivos · ~7k

**C13** - Sem logo própria, e também com o MinIO fora, a mensagem entregue ao Mailpit leva uma parte inline `logo` com o PNG do Esclari, e o HTML tem `<img src="cid:logo" alt="Esclari"`, `max-height:40px`, e o nome da instituição em texto de 20 px em negrito (AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailMarcaTests.Sem_logo_propria_leva_a_logo_do_Esclari_e_o_nome"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailMarcaTests.Minio_fora_envia_sem_logo"`

**C14** - Com logo própria lida, a parte inline `logo` tem os bytes enviados pela instituição, o `alt` é o nome dela e o HTML não contém `alt="Esclari"` (AC 14)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailMarcaTests.Html_com_cor_logo_e_corpo_escapado"`

**C15** - O PNG do Esclari do e-mail tem 348 por 80 px, assinatura PNG, `Content-Type` `image/png` na parte inline e tipo de cor sem canal alfa (tipo 2 ou 3 do IHDR) (AC 15)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MarcaDoEmailTests.Png_do_Esclari_tem_348_por_80_sem_canal_alfa"`

**C16** - O PNG vai como recurso embutido do projeto: o `Esclari.Api.csproj` declara `Marca/logo-horizontal-email.png` como `EmbeddedResource` e o fluxo do recurso existe em tempo de execução (AC 16)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MarcaDoEmailTests.Png_do_Esclari_esta_embutido_no_projeto"`

### S3 - Tela Aparência · 3 arquivos web · ~7k

**C17** - Na Aparência de uma instituição sem imagens, logo, ícone e foto do assistente mostram "Marca do Esclari (padrão)" com a miniatura do Esclari, e a imagem do login mostra "Imagem padrão do Esclari" (AC 17)
Proof: `npm --prefix web test -- src/administracao/aparencia/marcaPadrao.test.tsx -t "sem imagens a Aparencia mostra a marca do Esclari como padrao"`

**C18** - Ao escolher um arquivo, a miniatura vira o arquivo e a frase vira "Enviada por você", sem chamar a API antes de Salvar (AC 18)
Proof: `npm --prefix web test -- src/administracao/aparencia/marcaPadrao.test.tsx -t "imagem escolhida mostra Enviada por voce sem salvar"`

**C19** - Com imagem, o botão se chama "Usar a marca do Esclari" (e não "Remover"); ao confirmar, a API recebe o `DELETE` da imagem e o campo volta à frase padrão; ao lado da cor, "Usar a cor padrão" esvazia a cor (AC 19)
Proof: `npm --prefix web test -- src/administracao/aparencia/marcaPadrao.test.tsx -t "usar a marca do Esclari remove a imagem e usar a cor padrao esvazia a cor"`

**C20** - Ícone que não é PNG quadrado de 512 px ou mais e cor com contraste abaixo de 4,5 para 1 continuam recusados, e a marca atual não muda (AC 20)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AparenciaAdminTests.Icone_nao_quadrado_ou_pequeno_400"`
Proof: `npm --prefix web test -- src/administracao/aparencia/aparencia.test.tsx -t "contraste ao lado da cor e Salvar desabilitado"`

**C21** - (door 1) Uma instituição criada sem imagens e sem cor tem `NULL` em `logo`, `icone`, `login`, `assistente` e `cor`, e `GET /api/instituicoes/{slug}` devolve `null` nesses campos, sem copiar a marca do Esclari para a linha
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ManifestoTests.Instituicao_nova_nao_grava_a_marca_do_Esclari"`

**C22** - (door 2) O PNG do e-mail está no `Esclari.Api.csproj` como `EmbeddedResource` e o mesmo identificador `cid:logo` é o de hoje (C13, C14, C15, C16 provam os dois lados)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MarcaDoEmailTests.Png_do_Esclari_esta_embutido_no_projeto"`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| lugares onde falta marca própria e entra a do Esclari (7) | cabeçalho da barra e do celular C1 · entrada C3 · avatar do assistente C6 · pré-visualização da Aparência C17 · e-mail C13 · manifest `theme_color` C9 · manifest ícones C10 | - |
| partes da marca da instituição (4) | logo C3 e C4 · ícone C1 e C2 · foto do assistente C6 · cor C7 e C9 | - |
| temas da interface (3) | claro C3 · Escuro C3 e C5 · Preto C3 e C5 | - |
| estados da logo no e-mail (3) | sem logo C13 · com logo C14 · armazenamento fora C13 | - |
| ações da gestão na Aparência (4) | escolher imagem C18 · usar a marca do Esclari C19 · usar a cor padrão C19 · recusas C20 | - |
| campos anuláveis que significam "usa o Esclari" (5) | `logo` C21 · `icone` C21 · `login` C21 · `assistente` C21 · `cor` C21 | - |
| campos do manifest que não mudam ao trocar a marca (3) | `id` C11 · `scope` C11 · `start_url` C11 | - |

- Claims naming a response shape or a value that crosses the SMTP or HTTP boundary: C9, C10, C11, C13, C14, C15, C21 - cada um tem uma prova que lê a resposta da API ou a mensagem no Mailpit
- Nenhum outro check afirma mais do que o caso que a prova exercita

## Existing assertions changed

Mudam por consequência direta do plano (`## Impact`, linha "tests"), e só estas. O Verifier confere que nenhuma outra asserção mudou de valor:

- `ManifestoTests.Manifesto_padrao_sem_aparencia` (linha do `theme_color`): `#C83A23` passa a `#f7f5ee`
- `LayoutEmailTests.Sem_logo_mostra_o_nome_em_20px_negrito`: sem logo o HTML passa a ter `cid:` e `<img`; o nome continua em 20 px e negrito
- `EmailMarcaTests.Html_com_cor_logo_e_corpo_escapado` (trecho sem logo): `Assert.DoesNotContain("cid:", simples)` passa a afirmar o `cid:logo` do Esclari
- `EmailMarcaTests.Minio_fora_envia_sem_logo`: `Assert.DoesNotContain("cid:", html)` passa a afirmar o `cid:logo` do Esclari; o nome e o texto puro seguem iguais
- `aparencia.test.tsx` ("remover imagem pede confirmação", "as 4 imagens com miniatura, enviar e remover"): o nome do botão `Remover imagem: <rótulo>` passa a `Usar a marca do Esclari: <rótulo>`

## Swept

- validation: C20 (ícone e contraste recusados como hoje)
- failure modes: C13 (armazenamento fora), C16 (recurso ausente derruba o build)
- idempotency: n/a - a marca é leitura; "usar a marca do Esclari" repetido deixa o mesmo estado (C19)
- authorization: existing - só o administrador da instituição usa a Aparência (`/api/admin/aparencia`)
- concurrency: n/a - nenhuma escrita nova
- data lifecycle: C21 (nada é copiado para a instituição; limpar o campo volta ao padrão)
- dependency failure: C13 (MinIO fora), C15 (PNG embutido não depende de rede)
- state transitions: C18, C19 (padrão para enviada e de volta ao padrão)
- observability: n/a - nenhuma regra nova de log

## Out of scope

- Ver `plan.md` (atribuição do Esclari, versão escura da logo da instituição, redesenho da marca, fontes e textos por instituição, ícones de apps já instalados).

## Handoff

- S1 ~14k, S2 ~7k, S3 ~7k: arquivos tocados somam cerca de 60 KB (~15k tokens) mais testes novos, ~35k no total, abaixo do orçamento de 150k - one builder
- Mechanism: one builder
- Verificação enxuta por decisão do dono (18/11): Verifier independente, perfil `standard`, uma rodada, no máximo 3 falhas injetadas e uma só invocação por alvo de prova
- Base da verificação: o commit que leva `plan.md` e `checks.md` (o SHA entra aqui quando existir)
