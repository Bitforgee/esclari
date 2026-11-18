# Parâmetros por instituição checks

Profile: standard
Plan: `.specs/features/parametros-por-instituicao/plan.md`

16 checks in 3 slices · 3 one-way doors · 0 open

Provas: API `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~<Classe>.<Teste>"` (precisa de `docker compose up -d postgres mailpit minio`); web `npm --prefix web test -- <arquivo> -t "<nome>"` (Vitest). Aprovação do plano: pedido do dono em 18/11 ("pode dar push nos commits e seguir com a implementação deste novo plano").

## Checks

### S1 - Valores próprios e padrão · API

**C1** - (door 1) `instituicao` tem as sete colunas `prazo_abandono_minutos`, `prazo_confirmacao_dias`, `prazo_lembrete_aguardando_dias`, `prazo_resolver_aguardando_dias`, `prazo_validade_conhecimento_meses`, `prazo_primeira_resposta_horas` e `prazo_resolucao_horas`, todas `integer` anuláveis, e uma instituição nova nasce com as sete em `NULL` (AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ParametrosTests.Sete_colunas_anulaveis_e_nulas_na_criacao"`

**C2** - `GET .../parametros` responde `200` com `valor`, `padrao` e `efetivo` de cada um dos sete; sem valor próprio `valor` é `null` e `efetivo` é o padrão (30, 7, 3, 7, 12, 24, 120) (AC 2)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ParametrosTests.Ver_valor_padrao_e_efetivo"`

**C3** - `PUT .../parametros` válido grava exatamente o que veio, `null` restaura o padrão, um parâmetro ausente no corpo vale `null`, grava `PARAMETROS_ALTERADOS` com o nome e o valor novo de cada parâmetro que mudou e responde `200` com o estado novo (AC 3, AC 7)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ParametrosTests.Alterar_grava_restaura_e_audita"`

**C4** - Cada valor fora da faixa (abandono 4 e 1441; confirmação 0 e 61; lembrete e resolver 0 e 91; validade 0 e 61; horas 0 e 8761) responde `400` com o campo do parâmetro, e os limites (5, 1440, 1, 60, 90, 8760) são aceitos (AC 4)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ParametrosTests.Faixas_400_por_campo"`

**C5** - `ResolucaoHoras` efetivo menor que `PrimeiraRespostaHoras` efetivo, ou `LembreteAguardandoDias` efetivo maior ou igual a `ResolverAguardandoDias` efetivo, responde `400` e não grava nada, inclusive quando um dos dois vem do padrão (AC 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ParametrosTests.Conjunto_efetivo_400_sem_gravar"`

**C6** - `slug` desconhecido responde `404` e, sem sessão da plataforma, as duas rotas respondem `401` (AC 6)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ParametrosTests.Slug_404_e_sem_sessao_401"`

**C7** - (door 3) `PARAMETROS_ALTERADOS` está na lista fechada de ações da auditoria (11 ações)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAuditoriaTests.Acoes_fechadas_e_resumo_curto"`

### S2 - Cada instituição usa o seu valor · API

**C8** - (door 2) `Prazos.Para` devolve o valor próprio quando existe e o padrão global quando é `null`, para cada um dos sete (AC 8, AC 15)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosPorInstituicaoTests.Para_usa_o_proprio_ou_o_padrao"`

**C9** - Com `PrimeiraRespostaHoras` 1 numa instituição, um chamado dela de 2 horas sem resposta aparece no filtro de atrasados e na seção de prazos dos indicadores com meta 1, e o mesmo chamado em outra instituição não (AC 9)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosPorInstituicaoTests.Metas_proprias_no_atraso_e_nos_indicadores"`

**C10** - Com `ConfirmacaoDias` 2 numa instituição, o e-mail "foi resolvida" dela diz "(até 2 dias)" e o fechamento automático fecha o chamado resolvido há 3 dias dela e não o de outra instituição (AC 10)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosPorInstituicaoTests.Confirmacao_propria_no_email_e_no_fechamento"`

**C11** - Com `LembreteAguardandoDias` 1 e `ResolverAguardandoDias` 2 numa instituição, o agendador lembra o aluno dela aguardando há 1,5 dia e resolve o que aguarda há 3 dias, e não mexe nos chamados iguais de outra instituição (AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosPorInstituicaoTests.Aguardando_proprio_lembra_e_resolve"`

**C12** - Com `AbandonoMinutos` 10 numa instituição, a conversa dela parada há 15 minutos é abandonada e a de outra instituição não (AC 12)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosPorInstituicaoTests.Abandono_proprio"`

**C13** - Com `ValidadeConhecimentoMeses` 3 numa instituição, um artigo novo dela nasce com `valido_ate` daqui a 3 meses, e o artigo já gravado mantém o `valido_ate` de antes (AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosPorInstituicaoTests.Validade_propria_so_no_que_nasce_depois"`

**C14** - Um `PUT` muda o valor e a requisição seguinte da instituição já usa o novo, sem reiniciar a API (AC 14)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PrazosPorInstituicaoTests.Valor_novo_vale_na_proxima_leitura"`

### S3 - A tela de parâmetros · web

**C15** - O detalhe da instituição tem a aba "Parâmetros" com os sete campos, cada um com o padrão no texto de apoio e o campo vazio quando herda; "Salvar" envia os sete e mostra "Parâmetros salvos." (AC 16, AC 17)
Proof: `npm --prefix web test -- src/plataforma/parametros.test.tsx -t "aba parametros mostra o padrao e salva"`

**C16** - Um `400` mostra o erro junto ao campo, e "Usar padrão" esvazia o campo e só envia `null` ao apertar "Salvar" (AC 17, AC 18)
Proof: `npm --prefix web test -- src/plataforma/parametros.test.tsx -t "erro no campo e usar padrao so ao salvar"`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| linhas da tabela `Surface` (2) | `GET /api/plataforma/instituicoes/{slug}/parametros` C2 C6 · `PUT /api/plataforma/instituicoes/{slug}/parametros` C3 C4 C5 C6 | - |
| status do `GET` (3) | 200 C2 · 401 C6 · 404 C6 | - |
| status do `PUT` (4) | 200 C3 · 400 C4 C5 · 401 C6 · 404 C6 | - |
| parâmetros (7) | `AbandonoMinutos` C12 · `ConfirmacaoDias` C10 · `LembreteAguardandoDias` C11 · `ResolverAguardandoDias` C11 · `ValidadeConhecimentoMeses` C13 · `PrimeiraRespostaHoras` C9 · `ResolucaoHoras` C9 | - |

## Existing assertions changed

- `PlataformaAuditoriaTests`: a lista fechada passa de 10 para 11 ações, e os fluxos que gravam todas as ações ganham um `PUT .../parametros`
- `ArquiteturaTests`, `RetratoTests`, `SessaoTests`, `MediatorTests`, `ServicosTests` e os retratos: contagens ganham os dois casos de uso, as rotas e o serviço `PrazosAtuais`; o construtor de `AgirNoChamado` passa a receber `PrazosAtuais` no lugar de `Prazos`
- `web/src/test/estrutura.test.ts`: a fatia `plataforma` ganha os arquivos novos

## Swept

- validation: C4, C5
- failure modes: C5 (nada gravado)
- idempotency: C3 (mesmo `PUT` duas vezes deixa o mesmo estado)
- authorization: C6
- concurrency: n/a - uma ou duas pessoas na equipe da plataforma
- data lifecycle: C13 (`valido_ate` já gravado não muda)
- dependency failure: n/a - sem serviço externo
- state transitions: C3 (próprio e herdado)
- observability: C3 (auditoria)

## Out of scope

- Ver `plan.md`. O passo "Parâmetros" dentro da aba "Implantação" (AC 26 de `implantacao-de-instituicao`) fica como a aba "Parâmetros" do detalhe, sem um passo a mais na aba "Implantação".

## Handoff

- Mechanism: one builder
- Verificação enxuta por decisão do dono (18/11): Verifier independente, perfil `standard`, uma rodada, no máximo 3 falhas injetadas
- Base da verificação: o commit que leva `checks.md`
