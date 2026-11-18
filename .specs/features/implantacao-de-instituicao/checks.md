# Implantação de instituição checks

Profile: standard
Plan: `.specs/features/implantacao-de-instituicao/plan.md`

24 checks in 3 slices · 2 one-way doors · 2 open, 1 blocks go-live

Provas: API `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~<Classe>.<Teste>"` (precisa de `docker compose up -d postgres mailpit minio`); web `npm --prefix web test -- <arquivo> -t "<nome>"` (Vitest). Aprovação do plano: pedido do dono em 18/11 ("siga com o que for recomendado").

## Checks

### S1 - Carga assistida · API

**C1** - `POST /instituicoes/{slug}/carga?simular=true` com arquivo válido responde `200` com as contagens e não grava setor, categoria nem auditoria (AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoCargaTests.Simular_conta_e_nao_grava"`

**C2** - Arquivo inválido responde `400` com todos os erros, tanto em `simular=true` quanto na importação, e nada é gravado (AC 2)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoCargaTests.Arquivo_invalido_400_com_todos_os_erros"`

**C3** - Arquivo que não é UTF-8 responde `400` com "O arquivo deve estar em UTF-8." (AC 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoCargaTests.Arquivo_fora_de_utf8_400"`

**C4** - Importar o mesmo arquivo duas vezes cria o conteúdo na primeira e `0` itens novos de setor e categoria na segunda, sem duplicar nada (AC 4)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoCargaTests.Importar_duas_vezes_nao_duplica"`

**C5** - `GET /modelos-de-carga` lista `padrao-esclari` com 3 setores, 6 categorias e 6 regras, e `401` sem sessão (AC 5, AC 8)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoCargaTests.Modelos_listam_o_padrao_esclari"`

**C6** - `POST /instituicoes/{slug}/carga/modelo/padrao-esclari` aplica o modelo, grava `CARGA_IMPORTADA` e responde `200` com as contagens; modelo ou `slug` desconhecido responde `404` e nenhum artigo nasce (AC 6, AC 7, AC 8)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoCargaTests.Aplicar_modelo_grava_e_audita"`

### S2 - Equipe inicial · API

**C7** - `GET /instituicoes/{slug}/setores` lista `id` e `nome` dos setores ativos da instituição alvo e `404` para `slug` desconhecido (AC 9)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Setores_da_instituicao_alvo"`

**C8** - `POST /equipe` válido cria o usuário sem senha, enfileira o e-mail de primeiro acesso na fila da instituição alvo na mesma transação, grava `USUARIO_CRIADO` e responde `201` com `id` (AC 10)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Criar_membro_convida_e_audita"`

**C9** - `papeis` vazio, com `ALUNO`, `OPERADOR` ou valor desconhecido responde `400` no campo `papeis` e não cria nada (AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Papeis_invalidos_400"`

**C10** - Um campo `senha` ou `senhaTemporaria` no corpo é ignorado: o usuário nasce sem senha e a resposta não traz senha (AC 12)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Senha_no_corpo_e_ignorada"`

**C11** - Atendente sem setor, ou com setor de outra instituição, responde `400` no campo `setores` (AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Atendente_exige_setor_da_propria_instituicao"`

**C12** - CPF ou e-mail já existente na instituição alvo responde `409`; CPF ou e-mail inválido responde `400` (AC 14)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Repetido_409_e_invalido_400"`

**C13** - `GET /equipe` lista só administradores e atendentes, com e-mail e CPF mascarados, e nenhum aluno (AC 15)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Lista_so_equipe_mascarada"`

**C14** - `POST .../convite` para quem ainda não definiu a senha invalida o convite anterior, enfileira outro e responde `204`; para quem já definiu responde `409` (AC 16)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Convite_novo_invalida_o_anterior"`

**C15** - `POST .../desativar` marca inativo, grava `USUARIO_ALTERADO` e responde `204`; o último administrador ativo responde `409` (AC 17)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Desativar_protege_o_ultimo_administrador"`

**C16** - `POST .../reativar` marca ativo, grava `USUARIO_ALTERADO` e responde `204` (AC 18)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Reativar_volta_a_ativo"`

**C17** - Convite, desativar e reativar de `id` de aluno ou de outra instituição respondem `404` (AC 19)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Aluno_e_outra_instituicao_404"`

**C18** - O `resumo` de `USUARIO_CRIADO` e `USUARIO_ALTERADO` tem o `id` e os papéis e nunca CPF nem e-mail (AC 20)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Resumo_sem_cpf_nem_email"`

**C19** - Sem sessão da plataforma, todas as rotas novas respondem `401` (AC 21)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Sem_sessao_401_em_toda_rota_nova"`

### S3 - Prontidão e tela · API e web

**C20** - O detalhe traz `prontidao` com `ADMINISTRADOR_COM_SENHA`, `ATENDENTE_NO_SETOR`, `CATEGORIA_COM_REGRA` e `ARTIGO_ATIVO` conforme as definições do AC 23, e `pronta` só com os quatro `ok` (AC 22, AC 23)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoProntidaoTests.Quatro_itens_e_pronta"`

**C21** - Instituição ativa e não pronta tem `situacao` `CONFIGURANDO`, pronta tem `ATIVA` e desativada tem `DESATIVADA` (AC 24)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoProntidaoTests.Situacao_configurando_ativa_desativada"`

**C22** - Depois de criar, a tela leva ao detalhe na aba "Implantação" com os quatro itens, o estado de cada um e o botão do passo que resolve cada pendente (AC 25)
Proof: `npm --prefix web test -- src/plataforma/implantacao.test.tsx -t "criar leva a implantacao com os quatro itens"`

**C23** - A aba mostra os passos "Carga inicial" e "Equipe inicial"; na carga, "Simular" vem antes de "Importar" e mostra as contagens ou os erros (AC 26, AC 27)
Proof: `npm --prefix web test -- src/plataforma/implantacao.test.tsx -t "carga inicial simula antes de importar"`

**C24** - A equipe vazia mostra "Nenhum administrador ou atendente além do primeiro. Crie a equipe para a instituição atender." com "Novo usuário", e desativar pede confirmação com o nome (AC 28, AC 29)
Proof: `npm --prefix web test -- src/plataforma/implantacao.test.tsx -t "equipe inicial vazia e desativar com confirmacao"`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| linhas da tabela `Surface` (9) | `GET /modelos-de-carga` C5 · `POST /instituicoes/{slug}/carga/modelo/{id}` C6 · `POST .../carga?simular` C1 C2 C3 · `GET /instituicoes/{slug}/setores` C7 · `GET /instituicoes/{slug}/equipe` C13 · `POST /instituicoes/{slug}/equipe` C8 C9 C10 C11 C12 · `POST /instituicoes/{slug}/equipe/{id}/convite` C14 · `POST /instituicoes/{slug}/equipe/{id}/desativar` C15 · `POST /instituicoes/{slug}/equipe/{id}/reativar` C16 · `GET /instituicoes/{slug}` C20 | - |
| status `POST .../equipe` (5) | 201 C8 · 400 C9 C11 C12 · 401 C19 · 404 C17 · 409 C12 | - |
| status convite, desativar, reativar (4) | 204 C14 C15 C16 · 401 C19 · 404 C17 · 409 C14 C15 | - |
| ações novas da auditoria (2) | `USUARIO_CRIADO` C8 C18 · `USUARIO_ALTERADO` C15 C16 C18 | - |
| itens da prontidão (4) | `ADMINISTRADOR_COM_SENHA` C20 · `ATENDENTE_NO_SETOR` C20 · `CATEGORIA_COM_REGRA` C20 · `ARTIGO_ATIVO` C20 | - |
| `situacao` (3) | `CONFIGURANDO` C21 · `ATIVA` C21 · `DESATIVADA` C21 | - |

## Existing assertions changed

- `PlataformaSaudeTests`, `PlataformaInstituicoesTests`, `PlataformaTests`: a lista e o detalhe ganham o campo `prontidao`, e uma instituição sem equipe, setor com atendente, regra ou artigo passa de `ATIVA` para `CONFIGURANDO` (AC 24); `AGUARDANDO_PRIMEIRO_ACESSO` continua tendo precedência quando o administrador ainda não definiu a senha
- `PlataformaAuditoriaTests.Resumo_sem_dado_pessoal_e_ate_200_caracteres` e `AuditoriaDaPlataforma`: a lista fechada passa de 8 para 10 ações
- `ArquiteturaTests`, `RetratoTests`, `SessaoTests`, `MediatorTests` e os retratos: contagens ganham os oito casos de uso e as rotas novas
- `web/src/test/estrutura.test.ts` e os testes da fatia `plataforma`: ganham os arquivos novos

## Swept

- validation: C3, C9, C11, C12
- failure modes: C2 (nada gravado quando há erro), C15 (último administrador)
- idempotency: C4, C14
- authorization: C17, C19
- concurrency: n/a - uma equipe de uma ou duas pessoas; o `409` do CPF repetido vem do índice único
- data lifecycle: C15, C16 (nada se apaga)
- dependency failure: n/a - o e-mail vai para a fila da instituição com as tentativas de hoje
- state transitions: C21
- observability: C8, C15, C16 (cada ação na auditoria)

## Out of scope

- Ver `plan.md`: alunos, marca, artigos de outra instituição, CPF, senha do operador, validade do convite diferente de 60 minutos, editor de modelos, parâmetros (`parametros-por-instituicao`).

## Handoff

- Mechanism: one builder
- Verificação enxuta por decisão do dono (18/11): Verifier independente, perfil `standard`, uma rodada, no máximo 3 falhas injetadas
- Base da verificação: o commit que leva `checks.md` (o SHA entra aqui quando existir)
- O passo "Parâmetros" da aba só aparece quando `parametros-por-instituicao` existir; hoje a aba mostra Carga inicial e Equipe inicial
