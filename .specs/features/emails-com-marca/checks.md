# E-mails com a cara da instituição checks

Profile: standard
Plan: `.specs/features/emails-com-marca/plan.md`

31 checks in 3 slices · 2 one-way doors · 0 open, 0 block

Comando de prova: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~<Classe>.<Teste>"` (VSTest, medido em 18/11; os testes de integração pedem `docker compose up -d postgres mailpit minio`). Os testes de `LayoutEmailTests` não usam banco.

## Checks

### S1 - O molde novo · 5 arquivos · 33 KB · ~9k

**C1** - O HTML de todo e-mail tem `<html lang="pt-BR">`, `<title>` igual ao assunto, `<meta name="viewport" content="width=device-width, initial-scale=1">` e `<meta name="color-scheme" content="light dark">` (AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Documento_declara_idioma_titulo_viewport_e_esquema_de_cor"`

**C2** - Toda `<table` do HTML traz `role="presentation"`; há `max-width:600px` e `width="100%"`; o HTML não tem `<script`, `<form`, `<link`, nem `src=` ou `url(` com `http` (só `cid:logo`) (AC 2)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Tabelas_de_layout_coluna_unica_sem_script_nem_recurso_remoto"`

**C3** - O texto do corpo sai com `font-size:16px`, `line-height:1.5`, `font-family:Arial,Helvetica,sans-serif` e `color:#0e1330` sobre `#ffffff` (AC 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Texto_16px_entrelinha_e_tinta"`

**C4** - Com logo, o cabeçalho tem `<img src="cid:logo" alt="Univag &amp; Cia"` com `max-height:40px`, fundo `#ffffff` e, acima dele, um filete `height:4px` na cor da instituição; na mensagem entregue ao Mailpit o `cid:logo` aponta para a parte inline com os bytes enviados (AC 4)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Logo_no_cabecalho_branco_sob_filete_da_cor"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailMarcaTests.Html_com_cor_logo_e_corpo_escapado"`

**C5** - Sem logo, o cabeçalho mostra o nome da instituição em `font-size:20px` e `font-weight:bold`, e o HTML não tem `cid:`; com o MinIO fora, o e-mail sai igual e é marcado como enviado (AC 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Sem_logo_mostra_o_nome_em_20px_negrito"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailMarcaTests.Minio_fora_envia_sem_logo"`

**C6** - A cor do filete e do botão é `#8a1538` quando a instituição a define e `#0e1330` quando não define, e o HTML nunca contém `#C83A23` (AC 6)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Cor_da_instituicao_ou_tinta_no_filete_e_no_botao"`

**C7** - Antes do título há um elemento com `display:none` com o primeiro parágrafo do corpo cortado em 90 caracteres, e um parágrafo de 200 caracteres sai com exatamente 90 (AC 7)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Pre_visualizacao_oculta_com_90_caracteres"`

**C8** - O bloco `@media (prefers-color-scheme: dark)` troca fundo para `#0e0e18`, cartão para `#151620`, texto para `#f0eee9` e texto secundário para `#a0a1a8`, e não altera o fundo `#ffffff` do cabeçalho nem a cor do botão (AC 8)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Tema_escuro_troca_as_quatro_cores_e_preserva_cabecalho_e_botao"`

**C9** - O rodapé traz "Enviado pelo Esclari para Univag &amp; Cia." e "Este aviso é automático; não responda este e-mail." com `font-size:14px` e `color:#5c5d64` (AC 9)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Rodape_com_remetente_e_aviso_automatico"`

**C10** - Em `@media (max-width:480px)` o preenchimento lateral do cartão passa de 32 para 20 px e o botão ocupa 100% da largura (AC 10)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Celular_reduz_preenchimento_e_botao_inteiro"`

**C11** - A parte `text/plain` entregue ao Mailpit é igual ao corpo gravado na fila, e não contém o título, o rótulo do botão nem o rodapé, mesmo quando a linha tem título e ação (AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailMarcaTests.Texto_puro_igual_ao_corpo_mesmo_com_titulo_e_acao"`

**C12** - Um corpo, título, rótulo e nome de instituição com `<script>alert(1)</script>`, `&` e aspas saem escapados, e o HTML não contém `<script` (AC 12)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Escapa_corpo_titulo_rotulo_e_nome"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailMarcaTests.Html_com_cor_logo_e_corpo_escapado"`

**C13** - Um e-mail com 50 itens de lista, cada um com 120 caracteres e um endereço, gera um HTML de no máximo 100.000 bytes (AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Cinquenta_itens_cabem_em_100_kb"`

### S2 - Título, botão e lista · 6 arquivos · 40 KB · ~11k

**C14** - Com `titulo`, o HTML tem `<h1` com `font-size:24px` e `font-weight:bold` antes do primeiro parágrafo; sem `titulo`, não tem `<h1` (AC 14)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Titulo_em_h1_de_24px_antes_do_texto"`

**C15** - Com ação cujo endereço começa pela base da aplicação, o HTML tem um `<a href="<url>"` com `min-height:44px`, `color:#ffffff`, `font-size:16px`, `font-weight:bold`, `border-radius:6px`, `background-color:<cor>`, e um bloco `<!--[if mso]>` com `v:roundrect` (AC 15)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Botao_de_44px_com_versao_vml_para_outlook"`

**C16** - Com botão, abaixo dele vem "Se o botão não abrir, copie este endereço:" e o endereço como link, e a linha do corpo que repete só esse endereço não aparece no corpo do HTML (AC 16)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Endereco_por_extenso_abaixo_do_botao_sem_repetir_a_linha"`

**C17** - Com `acao_url` fora da base (`https://outro.exemplo/x` e `javascript:alert(1)`), o HTML não tem botão nem o endereço e o e-mail é gerado sem erro (AC 17)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Acao_fora_da_base_nao_vira_botao"`

**C18** - Blocos separados por linha em branco viram `<p>`, blocos de linhas com "- " viram `<ul>` com um `<li>` por linha, linha só com `http://` ou `https://` vira `<a href>`, e `javascript:alert(1)` sozinho numa linha continua texto (AC 18)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Paragrafos_listas_e_links_do_corpo"`

**C19** - Blocos de linhas com "> " viram `<blockquote` com `border-left` no HTML; `Avisos.Resolvido` grava a resolução em linhas com "> " e o texto inteiro dela continua no corpo (AC 19)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LayoutEmailTests.Bloco_com_maior_que_vira_citacao"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailTituloEAcaoTests.Resolvido_grava_a_resolucao_como_citacao"`

**C20** - `Avisos.NovaResposta` grava `titulo` "Sua solicitação recebeu uma resposta", `acao_rotulo` "Ver a resposta" e `acao_url` igual ao link do chamado (AC 20)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailTituloEAcaoTests.Nova_resposta_grava_titulo_e_acao"`

**C21** - `Avisos.Resolvido` grava `titulo` "Sua solicitação foi resolvida", `acao_rotulo` "Confirmar ou rejeitar" e `acao_url` igual ao link do chamado (AC 21)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailTituloEAcaoTests.Resolvido_grava_titulo_e_acao"`

**C22** - `Avisos.AguardaResposta` grava `titulo` "A equipe aguarda a sua resposta", `acao_rotulo` "Responder" e `acao_url` igual ao link do chamado (AC 22)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailTituloEAcaoTests.Aguarda_resposta_grava_titulo_e_acao"`

**C23** - `AvisarAtrasos` grava, ao setor, `titulo` "Chamados em atraso" e, ao administrador, "Resumo de chamados em atraso"; nos dois, `acao_rotulo` "Ver chamados em atraso" e `acao_url` igual ao endereço "Ver todos os chamados em atraso" do corpo (`/fila?atraso=true` ao setor, `/chamados?atraso=true` ao administrador) (AC 23)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailTituloEAcaoTests.Atrasos_gravam_titulo_e_acao_do_setor_e_do_administrador"`

**C24** - `AvisarValidade` grava `titulo` "Conteúdo para renovar", `acao_rotulo` "Abrir a lista" e `acao_url` igual ao primeiro endereço "Abrir:" do corpo (AC 24)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailTituloEAcaoTests.Validade_grava_titulo_e_acao"`

**C25** - O primeiro acesso (e-mail entregue ao Mailpit) e a criação da instituição (linha na fila) usam `titulo` "Crie a sua senha", botão "Criar senha" com o endereço de `definir-senha?token=`, e o texto traz "Se você não pediu isso, ignore este e-mail." e "60 minutos" (AC 25)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailTituloEAcaoTests.Primeiro_acesso_envia_titulo_botao_e_aviso"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailTituloEAcaoTests.Criacao_da_instituicao_grava_titulo_acao_e_aviso"`

### S3 - Fila e portas · 6 arquivos · 25 KB · ~7k

**C26** - Uma linha da fila sem `titulo` nem ação (como as anteriores à mudança) é enviada no molde novo, sem `<h1` e sem botão, e `EnviadoEm` fica preenchido (AC 26)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailMarcaTests.Linha_antiga_sai_no_molde_novo_sem_titulo_e_sem_botao"`

**C27** - Os assuntos dos sete e-mails continuam `[<instituição>] ...` como hoje: os testes que já os afirmam passam sem mudança de valor (AC 27)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailTests.Resposta_publica_gera_email"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailTests.Resolucao_gera_email"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ExtrasTests.Lembrete_no_3o_dia_uma_vez"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Primeiro_estouro_avisa_o_responsavel"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrimeiroAcessoTests"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaTests.Criar_grava_o_email_na_mesma_transacao"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeTests.Um_email_por_administrador_com_os_itens_por_marco"`

**C28** - (door 1) `email_saida` tem `titulo`, `acao_rotulo` e `acao_url`, os três `text` e `YES` anuláveis, e nenhuma outra coluna nova (AC porta 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailsMigracaoTests.Colunas_titulo_acao_rotulo_e_acao_url_anulaveis"`

**C29** - (door 1) `EmailSaida.Criar(para, assunto, corpo, agora)` deixa `Titulo`, `AcaoRotulo` e `AcaoUrl` nulos, e `Criar(..., titulo: "T", acao: new("Abrir", "http://x/y"))` os guarda; o que a fila guarda volta do banco igual (porta 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailSaidaDominioTests.Criar_sem_titulo_e_acao_deixa_nulos"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailSaidaDominioTests.Criar_com_titulo_e_acao_guarda_os_tres_campos"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailTituloEAcaoTests.Titulo_e_acao_voltam_do_banco_iguais"`

**C30** - (door 2) Nenhum pacote entra: o `Esclari.Api.csproj` não muda entre a base e `HEAD`
Proof: `git diff --exit-code <base>..HEAD -- Esclari.Api/Esclari.Api.csproj Esclari.Api.Tests/Esclari.Api.Tests.csproj`

**C31** - O HTML de um e-mail com título, botão e lista, visto pelo `html-check` do Mailpit, tem `Total.Unsupported` de no máximo 15 (por cento) (AC 2, AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~EmailMarcaTests.Html_check_do_mailpit_com_no_maximo_15_por_cento_sem_suporte"`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| e-mails com título e botão (7) | nova resposta C20 · resolvido C21 · aguarda resposta C22 · atraso C23 (setor e administrador, os dois no mesmo teste) · validade C24 · primeiro acesso C25 · criação da instituição C25 | - |
| variantes de atraso (2) | setor C23 · administrador C23 | - |
| cores do tema escuro (4) | fundo C8 · cartão C8 · texto C8 · texto secundário C8 | - |
| colunas novas de `email_saida` (3) | `titulo` C28 · `acao_rotulo` C28 · `acao_url` C28 | - |
| estados da linha da fila (3) | sem título e sem ação C26 · com título e ação C14 e C15 · devolvida do banco C29 | - |
| endereço da ação (3) | dentro da base C15 · fora da base com `https` C17 · `javascript:` C17 | - |
| blocos do corpo (5) | parágrafo C18 · lista C18 · endereço sozinho C18 · endereço perigoso sozinho C18 · citação C19 | - |
| texto escapado (4 origens) | corpo C12 · título C12 · rótulo C12 · nome da instituição C12 | - |
| logo (3 estados) | com logo C4 · sem logo C5 · armazenamento fora C5 | - |
| cor da instituição (2) | definida C6 · não definida C6 | - |
| partes da mensagem (2) | `text/plain` C11 · `text/html` C1 | - |
| assuntos (7) | nova resposta C27 · resolvido C27 · aguarda resposta C27 · atraso C27 · validade C27 · primeiro acesso C27 · criação da instituição C27 | - |
| tamanhos de tela (2) | 360 px e abaixo C10 · toque de 44 px C15 | - |
| dependências novas (1) | nenhuma C30 | - |

- Claims naming a response shape or a value that crosses the SMTP boundary: C4, C5, C11, C25, C26, C31 - cada um tem uma prova que lê a mensagem no Mailpit
- Nenhum outro check afirma mais do que o caso que a prova exercita

## Existing assertions changed

Mudam por consequência direta do plano (`## Impact`, linha "tests"), e só estas. O Verifier confere que nenhuma outra asserção de `Esclari.Api.Tests` mudou de valor:

- `EmailMarcaTests.Html_com_cor_logo_e_corpo_escapado`: `Assert.Contains("background-color:#8a1538", html)` (cabeçalho colorido) passa a afirmar o filete `height:4px`; `Assert.Contains("background-color:#C83A23", simples)` passa a `background-color:#0e1330`; `Assert.Contains("color:#ffffff", simples)` passa a afirmar o botão ou o cabeçalho branco, conforme C4 e C6; `Assert.Contains("<br>", html)` passa a `<p`

## Swept

- validation: C17 (endereço da ação), C12 (texto escapado), C18 (o que vira link)
- failure modes: C5 (logo fora), C17 (ação inválida), C26 (linha sem os campos novos)
- idempotency: n/a - o molde é uma função pura do que a fila guarda, e o reenvio de uma linha com falha gera o mesmo HTML; a fila e as tentativas não mudam
- authorization: n/a - nenhuma rota nova; o link do botão exige o login do app, como o endereço por extenso de hoje
- concurrency: n/a - o molde não guarda estado; a fila e o `Agendador` ficam como estão (AgendadorConcorrenciaTests segue verde, C27)
- data lifecycle: C26 (linhas antigas), C28 (colunas anuláveis, nada a migrar)
- dependency failure: C5 (MinIO fora envia sem logo)
- state transitions: n/a - o e-mail não tem estado novo; `EnviadoEm` segue a regra de hoje (C26)
- observability: n/a - o envio já registra falha e desistência em `EnviarEmails`; o molde não loga nada

## Out of scope

- Editar o molde pela tela de Aparência, rastrear abertura e clique, cancelar inscrição, e-mails novos, trocar assuntos, fonte web e marca no cabeçalho - ver `plan.md`

## Handoff

- S1 ~9k, S2 ~11k, S3 ~7k: arquivos tocados somam 33 KB (`wc -c` das 11 fontes, ~8k tokens) mais os testes novos e a leitura do repo, ~30k no total, abaixo do orçamento de 150k - one builder
- Mechanism: one builder
- Base da verificação: o commit que leva `plan.md` e `checks.md` (o SHA entra aqui quando existir)
- **Settled mid-build:** (1) porta 3 do plano: Criar recebe ApresentacaoDoEmail em vez de 	itulo e cao soltos, por causa de Dados_ate_5_parametros; o que a fila guarda (C28, C29) não mudou, e os testes de C29 usam a forma nova. (2) AC 19 pede citação sem dizer como o molde a reconhece: blocos de linhas com >  viram <blockquote>, e Avisos.Resolvido grava a resolução com >  (C19). (3) O resumo de atrasos passa a escrever o endereço de Ver todos os chamados em atraso: na linha de baixo, para o molde não repeti-lo (C16, C23)
