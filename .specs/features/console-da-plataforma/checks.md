# Console da plataforma checks

Profile: standard
Plan: `.specs/features/console-da-plataforma/plan.md`

48 checks in 6 slices · 7 one-way doors · 3 open, 2 block go-live

Provas: API `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~<Classe>.<Teste>"` (VSTest; precisa de `docker compose up -d postgres mailpit minio`); web `npm --prefix web test -- <arquivo> -t "<nome>"` (Vitest). `TotpTests` não usa banco.

## Checks

### S1 - A conta da plataforma e a entrada · API · ~40k

**C1** - A tabela `operador_plataforma` não tem a coluna `instituicao_id`, e um operador criado não aparece em `GET /api/usuarios` de nenhuma instituição (AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Operador_nao_pertence_a_instituicao"`

**C2** - `plataforma operador --email ana@equipe.esclari --nome "Ana Dev"` cria o operador sem senha, grava `OPERADOR_ALTERADO` na auditoria e imprime o link `.../plataforma/definir-senha?token=` válido por 60 minutos (AC 2)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaComandoTests.Criar_imprime_o_link_de_60_minutos_e_audita"`

**C3** - O mesmo comando para um operador sem senha gera convite novo, invalida o anterior e imprime o link; para um operador com senha sai com código 1 e a frase "Esse operador já existe." (AC 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaComandoTests.Convite_novo_invalida_o_anterior_e_operador_com_senha_e_recusado"`

**C4** - `--desativar`, `--reativar` e `--redefinir-2fa` aplicam a mudança, gravam `OPERADOR_ALTERADO` e imprimem confirmação; operador inexistente e desativar o último operador ativo saem com código 1 sem gravar nada (AC 4)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaComandoTests.Desativar_reativar_e_redefinir_2fa_com_as_recusas"`

**C5** - `POST /api/plataforma/auth/definir-senha` com o token do convite e a senha de 8 caracteres responde `204`, grava o hash e invalida o token; o mesmo token de novo responde `400` (AC 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Definir_senha_204_e_token_de_uso_unico"`

**C6** - `definir-senha` responde `400` para token inválido, token vencido, senha de 7 caracteres e senha de 129 caracteres (AC 6)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Definir_senha_400_nos_quatro_casos"`

**C7** - `entrar` com e-mail e senha corretos de operador com segundo fator responde `200` com `etapa` `CODIGO`, grava o cookie `esclari_plataforma_etapa` e não grava `esclari_plataforma` (AC 7)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Entrar_com_segundo_fator_pede_o_codigo"`

**C8** - `entrar` de operador sem segundo fator responde `200` com `etapa` `CADASTRAR`, `segredo` de 32 caracteres base32 e `uri` igual a `otpauth://totp/Esclari:<e-mail>?secret=<segredo>&issuer=Esclari`, e grava o cookie de etapa (AC 8)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Entrar_sem_segundo_fator_devolve_segredo_e_uri"`

**C9** - E-mail desconhecido, senha errada e operador desativado respondem `401` com a mesma frase "E-mail ou senha inválidos." (AC 9)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Entrar_401_com_a_mesma_frase_nos_tres_casos"`

**C10** - `codigo` com o TOTP do passo atual, do anterior ou do seguinte responde `204`, abre a sessão, apaga o cookie de etapa, grava `totp_ultimo_passo` e `OPERADOR_ENTROU`; na etapa `CADASTRAR` grava também o segredo protegido (AC 10)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Codigo_abre_a_sessao_nos_tres_passos_e_grava_o_segredo_no_cadastro"`

**C11** - `codigo` errado, repetido no mesmo passo, sem cookie de etapa ou com o cookie de etapa de 6 minutos atrás responde `401` e não abre a sessão (AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Codigo_401_errado_repetido_sem_etapa_e_etapa_vencida"`

**C12** - `codigo` com 5 dígitos ou com letras e `entrar` com e-mail vazio respondem `400` (AC 12)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Corpo_invalido_400"`

**C13** - A sexta chamada no mesmo minuto a `entrar`, a `codigo` ou a `definir-senha` responde `429`, porque o limitador `auth` é um só por endereço (AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Limitador_auth_429_na_sexta_tentativa"`

**C14** - A sessão da plataforma responde `401` depois de 31 minutos sem requisição, e depois de 8 horas e 1 minuto mesmo com uso a cada 20 minutos (AC 14)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Sessao_expira_aos_30_minutos_sem_uso_e_aos_8_horas"`

**C15** - `sair` responde `204` e envia `Set-Cookie` que expira `esclari_plataforma` e `esclari_plataforma_etapa` (AC 15)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Sair_apaga_os_dois_cookies"`

**C16** - `GET /api/plataforma/eu` responde `200` com exatamente `id`, `nome` e `email` (AC 16)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Eu_devolve_so_id_nome_e_email"`

**C17** - `GET /api/plataforma/instituicoes` com o cookie de um ADMINISTRADOR de instituição responde `401`, e `GET /api/chamados/busca` com o cookie da plataforma responde `401` (AC 17, AC 18)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Cada_cookie_so_vale_no_proprio_mundo"`

**C18** - Depois de `--desativar`, a requisição seguinte de um operador com sessão aberta responde `401` (AC 19)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Operador_desativado_perde_a_sessao_na_requisicao_seguinte"`

### S2 - Saúde de cada instituição · API · ~20k

**C19** - Numa instituição com 2 chamados abertos (o mais antigo de 5 dias), 1 resolvido, 3 conversas de 10 dias atrás e 1 de 40, 4 respostas do assistente (1 `sem_base`) nos últimos 30 dias, 2 alunos e 1 atendente ativos e 1 aluno inativo, `saude` traz `chamadosAbertos` 2, `chamadoMaisAntigoAbertoDias` 5, `conversas30Dias` 3, `respostas30Dias` 4, `semBaseTaxa30Dias` 0,25, `alunosAtivos` 2, `equipeAtiva` 1 e `ultimaAtividadeEm` igual ao `criado_em` mais recente (AC 20, AC 21, AC 22, AC 23)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaSaudeTests.Contagens_e_datas_batem_com_os_dados_semeados"`

**C20** - Uma instituição sem conversa nem chamado devolve `ultimaAtividadeEm` `null`, `chamadoMaisAntigoAbertoDias` `null` e `semBaseTaxa30Dias` `null` (AC 21, AC 22)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaSaudeTests.Instituicao_vazia_devolve_nulos"`

**C21** - `emailsComFalha` conta só os `email_saida` sem envio com as tentativas esgotadas, e `conteudoVencido` conta só artigos e documentos ativos com `valido_ate` no passado (AC 23)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaSaudeTests.Emails_com_falha_e_conteudo_vencido_so_contam_o_que_deve"`

**C22** - O objeto `saude` tem exatamente as dez chaves do plano e nenhum texto, nome, CPF ou e-mail de chamado, mensagem ou usuário aparece no JSON da lista (AC 24)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaSaudeTests.Saude_so_tem_contagens_uma_fracao_e_uma_data"`

**C23** - `atencao` traz `PARADA` com atividade de 14 dias atrás e não com 13; `MUITOS_SEM_BASE` com taxa acima de um terço e 20 respostas e não com 19; `EMAILS_FALHANDO` com 1 falha; `CONTEUDO_VENCIDO` com 1 item; `FILA_ANTIGA` com o chamado mais antigo acima de `ResolucaoHoras / 24` dias (AC 25, AC 26)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaSaudeTests.Atencao_nos_limites_de_cada_etiqueta"`

**C24** - A lista põe as ativas com `atencao` antes das ativas sem `atencao`, e as desativadas por último, cada grupo por nome (AC 27)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaSaudeTests.Ordem_da_lista_atencao_ativas_desativadas"`

**C25** - `situacao` é `DESATIVADA` para instituição desativada, `AGUARDANDO_PRIMEIRO_ACESSO` para ativa cujos administradores ativos não têm senha, e `ATIVA` nos demais casos (AC 28)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaSaudeTests.Situacao_nos_tres_valores"`

### S3 - Ciclo de vida da instituição · API · ~25k

**C26** - Listar, criar, importar a carga (`POST /instituicoes/{slug}/carga`), desativar, reativar (`POST /instituicoes/{slug}/reativar`) e reenviar acesso (`POST /instituicoes/{slug}/reenviar-acesso`) mantêm corpo e statuses de hoje com a sessão da plataforma, e sem ela respondem `401` no lugar do `403` (AC 29)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaInstituicoesTests.Rotas_de_hoje_mantem_o_contrato_com_a_sessao_da_plataforma"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaInstituicoesTests.Rotas_sem_sessao_da_plataforma_401"`

**C27** - Criar uma instituição com o slug `plataforma` responde `400` com o erro no campo `slug` (AC 30)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaInstituicoesTests.Slug_plataforma_e_reservado"`

**C28** - `desativar` com motivo de 3 a 200 caracteres desativa e grava o motivo no `resumo`; sem motivo, com 2 e com 201 caracteres responde `400` e não desativa (AC 31)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaInstituicoesTests.Desativar_exige_motivo_de_3_a_200_caracteres"`

**C29** - `GET /instituicoes/{slug}` responde `200` com os campos da lista e `administradoresDaInstituicao` com `emailMascarado` (`a***@dominio`) e `primeiroAcessoPendente`; slug desconhecido responde `404` (AC 32)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaInstituicoesTests.Detalhe_200_mascarado_e_404"`

**C30** - `PUT /instituicoes/{slug}` com nome de 3 a 120 caracteres e `fuso` `America/Sao_Paulo` altera só nome e fuso, ignora um `slug` no corpo e responde `200` (AC 33)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaInstituicoesTests.Editar_altera_so_nome_e_fuso"`

**C31** - `PUT` com nome de 2 caracteres, nome de 121 e fuso `Marte/Olimpo` responde `400` com o campo; slug desconhecido responde `404` (AC 34)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaInstituicoesTests.Editar_400_por_campo_e_404"`

**C32** - Ao criar uma instituição na tela, o operador vê o passo de importar a carga JSON com as contagens do resultado, sem sair da tela (AC 35)
Proof: `npm --prefix web test -- src/plataforma/instituicoes.test.tsx -t "criar leva a importar a carga na mesma tela"`

### S4 - Registro do que a equipe fez · API · ~15k

**C33** - Cada uma das oito ações grava uma linha com o operador, a ação, a instituição alvo quando há e um `resumo`: `OPERADOR_ENTROU` (C10), `INSTITUICAO_CRIADA`, `INSTITUICAO_ALTERADA`, `INSTITUICAO_DESATIVADA`, `INSTITUICAO_REATIVADA`, `ACESSO_REENVIADO`, `CARGA_IMPORTADA` e `OPERADOR_ALTERADO` (C2 e C4) (AC 36)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAuditoriaTests.Cada_acao_grava_uma_linha_na_mesma_transacao"`

**C34** - Uma criação que falha (slug repetido) não deixa linha de auditoria (AC 37)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAuditoriaTests.Acao_que_volta_nao_deixa_linha"`

**C35** - Não há rota `PUT`, `PATCH` nem `DELETE` sob `/api/plataforma/auditoria`, e `AuditoriaPlataforma` não tem setter público nem método que altere (AC 38)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAuditoriaTests.Auditoria_so_inclusao"`

**C36** - O `resumo` das oito ações não contém CPF, senha nem token e tem no máximo 200 caracteres, inclusive com um motivo de desativação de 200 caracteres (AC 39)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAuditoriaTests.Resumo_sem_dado_pessoal_e_ate_200_caracteres"`

**C37** - `GET /api/plataforma/auditoria` devolve 50 itens por página, do mais novo ao mais antigo, e o `total`, e filtra por `instituicao` (AC 40)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAuditoriaTests.Pagina_de_50_do_mais_novo_ao_mais_antigo_com_filtro"`

**C38** - `pagina=0` e `instituicao=inexistente` respondem `400` (AC 41)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAuditoriaTests.Pagina_e_instituicao_invalidas_400"`

### S5 - O fim do papel `OPERADOR` · API e web · ~15k

**C39** - A migration apaga de `usuario_papel` as linhas `OPERADOR` e deixa as de `ALUNO`, `ATENDENTE` e `ADMINISTRADOR` (AC 42)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MigracaoOperadorTests.Apaga_so_as_linhas_operador"`

**C40** - Criar usuário com o papel `OPERADOR` responde `400` com o campo `papeis`, e `Papeis.Operador` não existe mais (AC 43)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaComandoTests.Papel_operador_nao_existe_mais"`

**C41** - `instituicao operador --slug demo --cpf 123` imprime o uso com `criar`, `carga` e `avaliar-busca` e sai com código 1 (AC 44)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaComandoTests.Instituicao_operador_nao_existe_mais"`

**C42** - O menu de um administrador de instituição não tem o grupo "Plataforma" nem o item "Instituições" (AC 45)
Proof: `npm --prefix web test -- src/plataforma/menu.test.tsx -t "menu da instituicao nao tem o grupo Plataforma"`

### S6 - Telas de `/plataforma` · web · ~25k

**C43** - Sem sessão, `/plataforma` mostra e-mail e senha; com `CODIGO` pede 6 dígitos; com `CADASTRAR` mostra a chave em grupos de 4, o link "Abrir no aplicativo" para a `uri` e o campo do código (AC 46)
Proof: `npm --prefix web test -- src/plataforma/entrada.test.tsx -t "entrada em tres etapas"`

**C44** - Com sessão, a casca tem a marca do Esclari (`/marca/logo-horizontal.svg`), e só os itens "Instituições" e "Auditoria" (AC 47)
Proof: `npm --prefix web test -- src/plataforma/instituicoes.test.tsx -t "casca da plataforma com a marca do Esclari e dois itens"`

**C45** - A lista mostra por linha o nome, a situação, a última atividade, a taxa de "não encontrei" em porcentagem e as etiquetas "Parada", "Muitos 'não encontrei'", "E-mails falhando", "Conteúdo vencido" e "Fila antiga" (AC 48)
Proof: `npm --prefix web test -- src/plataforma/instituicoes.test.tsx -t "lista com saude e etiquetas"`

**C46** - Lista de instituições e de auditoria vazias mostram a frase de estado vazio, e a de instituições mostra o botão "Nova instituição" (AC 49)
Proof: `npm --prefix web test -- src/plataforma/instituicoes.test.tsx -t "estados vazios"`

**C47** - Um `401` com a sessão aberta leva a `/plataforma` com "Sua sessão terminou. Entre de novo." (AC 50)
Proof: `npm --prefix web test -- src/plataforma/entrada.test.tsx -t "401 leva a entrada com a frase de sessao encerrada"`

**C48** - Desativar uma instituição abre a confirmação com o nome dela e o campo do motivo, e só envia com o motivo preenchido (AC 51)
Proof: `npm --prefix web test -- src/plataforma/instituicoes.test.tsx -t "desativar pede confirmacao e motivo"`

### Portas

**C49** - (door 1) `operador_plataforma` e `auditoria_plataforma` são as únicas tabelas sem `instituicao_id` não nulo, `auditoria_plataforma` tem `instituicao_alvo_id` anulável com chave para `instituicao`, e existe o índice único `ux_operador_plataforma_email` sobre `lower(email)`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArquiteturaTests.Tabelas_da_plataforma_sao_as_unicas_sem_instituicao"`

**C50** - (door 2) O `Set-Cookie` da sessão tem `esclari_plataforma`, `path=/api/plataforma`, `httponly`, `samesite=strict` e `secure` em `https`; o do cookie de etapa tem 5 minutos de vida
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAcessoTests.Atributos_dos_cookies_da_plataforma"`

**C51** - (door 3) Os vetores do RFC 6238 (SHA-1, tempos 59 s e 1111111109 s, 8 dígitos, segredo `12345678901234567890`) saem corretos, o código de 6 dígitos é o sufixo deles, a janela aceita um passo para cada lado e recusa dois, e um passo já usado é recusado
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TotpTests"`

**C52** - (door 4) `acao` só aceita as oito constantes de `AcoesDaPlataforma`, e a tabela guarda `resumo` de no máximo 200 caracteres
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAuditoriaTests.Acoes_fechadas_e_resumo_curto"`

**C53** - (door 5) `Plataforma` é uma fatia do `Esclari.Api` e do `web/src`, nada fora dela referencia `Esclari.Api.Plataforma`, e `Esclari.Api/Instituicoes/Plataforma/` não existe mais
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArquiteturaTests.Fatias_do_api"`
Proof: `npm --prefix web test -- src/test/estrutura.test.ts -t "estrutura em fatias"`

**C54** - (door 6) O slug `plataforma` é recusado (C27) e `/plataforma` renderiza o console antes de qualquer `/:slug` (C43)
Proof: `npm --prefix web test -- src/plataforma/entrada.test.tsx -t "plataforma nao e uma instituicao"`

**C55** - (door 7) A migration do papel está no histórico do EF (`MigracaoOperadorTests`, C39) e `Papeis` lista só `ALUNO`, `ATENDENTE` e `ADMINISTRADOR` (C40)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaComandoTests.Papel_operador_nao_existe_mais"`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| linhas da tabela `Surface` (12) | `POST /auth/entrar` C7 C8 C9 C12 C13 · `POST /auth/codigo` C10 C11 C12 · `POST /auth/definir-senha` C5 C6 · `POST /auth/sair` C15 · `GET /eu` C16 · `GET /instituicoes` C19 C24 · `GET /instituicoes/{slug}` C29 · `PUT /instituicoes/{slug}` C30 C31 · `POST /instituicoes` C26 C27 · `POST .../desativar` C28 · `POST .../reativar`, `.../reenviar-acesso`, `.../carga` C26 · `GET /auditoria` C37 C38 | - |
| status `POST /auth/entrar` (4) | 200 C7 C8 · 400 C12 · 401 C9 · 429 C13 | - |
| status `POST /auth/codigo` (4) | 204 C10 · 400 C12 · 401 C11 · 429 C13 | - |
| status `POST /auth/definir-senha` (3) | 204 C5 · 400 C6 · 429 C13 | - |
| status `GET /instituicoes/{slug}` (3) | 200 C29 · 401 C26 · 404 C29 | - |
| status `PUT /instituicoes/{slug}` (4) | 200 C30 · 400 C31 · 401 C26 · 404 C31 | - |
| status `GET /auditoria` (3) | 200 C37 · 400 C38 · 401 C26 | - |
| status `POST /instituicoes/{slug}/desativar` (4) | 200 C28 · 400 C28 · 401 C26 · 404 C26 | - |
| ações da auditoria (8) | `OPERADOR_ENTROU` C33 · `INSTITUICAO_CRIADA` C33 · `INSTITUICAO_ALTERADA` C33 · `INSTITUICAO_DESATIVADA` C33 · `INSTITUICAO_REATIVADA` C33 · `ACESSO_REENVIADO` C33 · `CARGA_IMPORTADA` C33 · `OPERADOR_ALTERADO` C33 | - |
| chaves de `saude` (10) | `ultimaAtividadeEm` C19 · `chamadosAbertos` C19 · `chamadoMaisAntigoAbertoDias` C19 · `conversas30Dias` C19 · `respostas30Dias` C19 · `semBaseTaxa30Dias` C19 · `alunosAtivos` C19 · `equipeAtiva` C19 · `emailsComFalha` C21 · `conteudoVencido` C21 | - |
| etiquetas de `atencao` (5) | `PARADA` C23 · `MUITOS_SEM_BASE` C23 · `EMAILS_FALHANDO` C23 · `CONTEUDO_VENCIDO` C23 · `FILA_ANTIGA` C23 | - |
| valores de `situacao` (3) | `ATIVA` C25 · `AGUARDANDO_PRIMEIRO_ACESSO` C25 · `DESATIVADA` C25 | - |
| etapas de entrada (2) | `CODIGO` C7 · `CADASTRAR` C8 | - |
| casos de 401 em `entrar` (3) | e-mail desconhecido C9 · senha errada C9 · desativado C9 | - |
| casos de 400 em `definir-senha` (4) | token inválido C6 · token vencido C6 · senha curta C6 · senha longa C6 | - |
| casos de 401 em `codigo` (4) | errado C11 · repetido C11 · sem etapa C11 · etapa vencida C11 | - |
| bordas do TOTP (4) | passo anterior C10 · passo atual C10 · passo seguinte C10 · dois passos fora C51 | - |
| flags do comando (4) | criar C2 · convite novo C3 · `--desativar` e `--reativar` C4 · `--redefinir-2fa` C4 | - |
| cookies (2) | sessão C50 · etapa C50 | - |
| tabelas sem `instituicao_id` (2) | `operador_plataforma` C49 · `auditoria_plataforma` C49 | - |
| temas de tela (telas do console) (3) | entrada C43 · lista C45 · detalhe de desativação C48 | - |

- Claims naming a status code, route or response shape: C5 a C18, C26 a C31, C37 e C38 - cada uma tem uma prova HTTP real contra a API, com banco, Mailpit e MinIO do compose
- Nenhum outro check afirma mais do que o caso que a prova exercita

## Existing assertions changed

Mudam por consequência direta do plano (`## Impact`, linha "tests"), e só estas. O Verifier confere que nenhuma outra asserção mudou de valor:

- `PlataformaTests` (todas): a forma de entrar passa de "usuário de instituição com o papel `OPERADOR`" para "operador da plataforma com sessão", e o `403` de `NaoEOperador` vira `401`; os corpos e os demais statuses não mudam
- `ArquiteturaTests.Toda_entidade_tem_instituicao_e_filtro`: passa a excluir `OperadorPlataforma` e `AuditoriaPlataforma` (C49 afirma o que sobra); `Fatias`, `FatiasPlanas`, `Raiz_sem_fatias` e a lista de arquivos da raiz ganham `Plataforma` e os arquivos novos, e `IgnoreQueryFilters_so_no_agendador_e_no_comando` segue como está
- `Papeis`/`ValidacaoDeUsuario`: testes que listavam `OPERADOR` entre os papéis perdem esse valor
- `web/src/administracao/instituicoes/instituicoes.test.tsx`: muda para `web/src/plataforma/instituicoes.test.tsx`, com as mesmas afirmações sobre a tabela e os diálogos
- `web/src/test/estrutura.test.ts`: a lista de fatias ganha `plataforma` e a contagem de arquivos de `administracao` perde os de `instituicoes`
- `web/e2e/instituicoes*.spec.ts`, `casca.spec.ts`, `ux.spec.ts` e `simulada.ts`: trocam o operador-papel pelo operador da plataforma (Playwright, fora das provas desta feature)

## Swept

- validation: C6, C12, C27, C28, C31, C38
- failure modes: C11 (etapa vencida), C34 (transação que volta), C20 (instituição vazia)
- idempotency: C3 (convite novo substitui o anterior), C5 (token de uso único), C11 (código repetido)
- authorization: C17, C26 (`401` sem sessão da plataforma), C18 (operador desativado)
- concurrency: n/a - a conta de uma ou duas pessoas não disputa escrita; o passo do TOTP já usado é a única corrida que importa e C11 a cobre
- data lifecycle: C39 (papel apagado), C35 e C52 (auditoria só inclusão), C28 (desativar não apaga)
- dependency failure: n/a - o console não chama serviço externo; a fila de e-mail da instituição continua com as tentativas de hoje
- state transitions: C7, C8, C10 (etapa para sessão), C25 (situação)
- observability: C33 (cada ação gravada na auditoria)

## Out of scope

- Ver `plan.md`: parâmetros por instituição, carga assistida, criar usuários de instituição, tela de operadores, motivo de cada "não encontrei", custo da IA, suporte por personificação, QR code.

## Handoff

- API ~50k (plataforma/, migration, ajustes de `ArquiteturaTests`, testes novos), web ~35k (`web/src/plataforma/`, menu, estrutura, testes), ~85k no total, abaixo do orçamento de 150k - one builder
- Mechanism: one builder
- Verificação enxuta por decisão do dono (18/11): Verifier independente, perfil `standard`, uma rodada, no máximo 3 falhas injetadas e uma só invocação por alvo de prova
- Base da verificação: o commit que leva `checks.md` (o SHA entra aqui quando existir)
