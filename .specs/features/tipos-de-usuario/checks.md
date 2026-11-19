# Tipos de usuário checks

Profile: standard
Plan: `.specs/features/tipos-de-usuario/plan.md`

103 checks em 3 lotes · 6 one-way doors · 0 perguntas abertas (nenhuma bloqueia)

**Como as provas rodam.** Na raiz `D:\esclari`, com `docker compose up -d postgres mailpit minio` (os testes da API usam 5432, 9000 e 1025). Os testes da API são xUnit v3 com o VSTest: o filtro é `--filter "FullyQualifiedName~<Classe>.<Teste>"` (`--filter-method` não existe). Um filtro que não seleciona teste sai com 0 (L-002): conferir a contagem de testes executados no relatório. Provas do web: `npm --prefix web test -- <arquivo> -t "<nome>"`; as de celular rodam com `docker compose up -d --build`, `npm --prefix web run e2e:preparar` e `npm --prefix web run e2e -- <arquivo> -g "<nome>"`. Os nomes de teste abaixo são os que o construtor cria (ou os existentes que ele mantém); o teste é escrito a partir do check, nunca a partir do código.

**Como ler as mensagens.** As mensagens em português entre aspas são o valor exato de `detail` (ProblemDetails) ou do texto da tela. Os campos citados em `400` aparecem em `errors` do ProblemDetails, como `Problemas.Invalido(mensagem, campo)` faz hoje. Fora de `/admin` e de `GET /api/conhecimento/validade`, as mensagens `403` que existem hoje não mudam (AC 9): `A lista de chamados é para a equipe.`, `Só o administrador vê os chamados de todos os setores.`, `Os indicadores são para administradores e atendentes.`, `A fila é só para atendentes.`; só a mensagem de `SoAdministrador` vira a do critério 7.

**Por que o lote 1 mantém uma ponte.** Para o lote 1 terminar verde sem mudar as rotas que gravam usuário, essas rotas continuam recebendo `papeis` na entrada e um único arquivo (`Esclari.Api/Dados/Usuarios/PonteDePapeis.cs`) traduz a lista para o tipo na mesma ordem da migration (door 3); toda leitura e toda autorização já são por função. O lote 2 troca a entrada por `tipoId`, apaga a ponte e `Papeis.cs`. O modelo guarda a entidade `UsuarioPapel` e a tabela `usuario_papel` até a migration de limpeza futura, para que o próximo `migrations add` não gere um `DROP TABLE`.

## Checks

### L1 - domínio, migração, política central e pontos de decisão da API · critérios 1 a 13, 15 (parcial), 56 a 59, 62 · 38 arquivos de código + 13 de teste existentes, mais ~40 KB de código novo e ~70 KB de teste novo · ~453 KB · ~113k

**C1** - O catálogo tem exatamente os 12 códigos `assistente.usar`, `fila.atender`, `chamados.ver_todos`, `indicadores.ver`, `conhecimento.gerenciar`, `cadastros.gerenciar`, `usuarios.gerenciar`, `matriculas.importar`, `tipos.gerenciar`, `aparencia.gerenciar`, `integracoes.gerenciar`, `mcp.usar`; `Funcoes.Valida` recusa `relatorios.ver`, `FILA.ATENDER` (maiúsculas), `""` e `null`, e aceita cada um dos 12 (FEAT, AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~FuncoesDoTipoTests.Catalogo_tem_os_12_codigos_e_recusa_outro"`

**C2** - As funções do tipo de sistema `GESTOR` são calculadas como os 12 códigos menos `assistente.usar`; para um gestor sem setor, as `permissoes` efetivas são as 10 sem `assistente.usar` e sem `fila.atender`, e incluem `tipos.gerenciar` e `usuarios.gerenciar` (FEAT, AC 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~FuncoesDoTipoTests.Gestor_calcula_todas_menos_assistente_usar"`

**C3** - As funções do tipo de sistema `ALUNO` são calculadas como só `assistente.usar`, com matrícula ou sem (FEAT, AC 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~FuncoesDoTipoTests.Aluno_calcula_so_assistente_usar"`

**C4** - Depois de criar uma instituição, `tipo_usuario_funcao` tem 0 linhas para os tipos `GESTOR` e `ALUNO` e 3 linhas (`fila.atender`, `indicadores.ver`, `mcp.usar`) para `ATENDENTE` (FEAT, AC 5, door 6)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposNoBancoTests.Instituicao_nova_grava_so_as_funcoes_do_atendente"`

**C5** - A função efetiva `fila.atender` some das `permissoes` de quem não tem nenhuma linha em `atendente_setor` (tipo `ATENDENTE` e tipo próprio com a função gravada, ambos sem setor) e volta com 1 vínculo; `GESTOR` com 0 setores não a tem e com 1 setor tem (FEAT, AC 6)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~FuncoesDoTipoTests.Fila_atender_exige_setor"`

**C6** - Quem chama `GET /api/admin/usuarios`, `GET /api/admin/setores`, `GET /api/admin/artigos` ou `GET /api/conhecimento/validade` com um tipo que não tem a função da rota recebe `403` com `detail` "Seu tipo de usuário não tem acesso a esta área."; a frase "Só administradores podem usar esta área." não aparece em nenhuma resposta (FEAT, AC 7)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AutorizacaoPorFuncaoTests.Sem_a_funcao_403_com_a_mensagem"`

**C7** - Depois que a função `indicadores.ver` sai do tipo próprio de um usuário já logado, o mesmo cookie recebe `403` em `GET /api/indicadores` na requisição seguinte (antes recebia `200`); depois que o usuário passa a outro tipo, o mesmo cookie recebe a resposta do tipo novo na requisição seguinte, sem novo login (FEAT, AC 8)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AutorizacaoPorFuncaoTests.Mudanca_vale_na_requisicao_seguinte"`

**C8** - Para os três tipos de sistema (`ALUNO` com matrícula ativa, `ATENDENTE` com 1 setor e 1 chamado nele, `GESTOR` com 0 setores), os status de seis rotas são os de hoje para os papéis equivalentes: `GET /api/chamados?escopo=fila` 200 · 200 (1 chamado) · 200 (`[]`); `GET /api/chamados/busca?escopo=todos` 403 · 403 · 200; `GET /api/indicadores` 403 · 200 · 200; `GET /api/admin/usuarios` 403 · 403 · 200; `POST /api/conversas` com mensagem válida e provedor falso 201 · 403 · 403; `GET /api/auth/me` 200 · 200 · 200 (FEAT, AC 9)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AutorizacaoPorFuncaoTests.Matriz_de_status_por_tipo_de_sistema"`

**C9** - Um `GESTOR` com 2 setores (um comum, um de liderança com curso e semestre) tem o papel de chamado `ATENDENTE` nos chamados do setor comum, `LIDER` nos do setor de liderança e `LEITURA` num chamado de um terceiro setor; num chamado seu como solicitante tem o papel `ALUNO` (FEAT, AC 10)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AutorizacaoPorFuncaoTests.Gestor_com_setor_atende_nos_seus_e_le_nos_demais"`

**C10** - O resumo de atrasos vai só para os usuários ativos, com e-mail, cujo tipo tem `chamados.ver_todos`: um tipo próprio com essa função recebe (1 e-mail), um `GESTOR` inativo, um `ATENDENTE` e um tipo próprio sem a função não recebem (FEAT, AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Resumo_vai_a_quem_tem_chamados_ver_todos"`

**C11** - Sem responsável, o aviso de atraso vai aos usuários ativos, com e-mail e vínculo no setor do chamado cujo tipo tem `fila.atender`: um tipo próprio com a função recebe, um usuário vinculado ao setor cujo tipo não tem a função não recebe (FEAT, AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarAtrasosTests.Atendentes_do_setor_pela_funcao_fila_atender"`

**C12** - O e-mail de validade do conhecimento vai só para os usuários ativos, com e-mail, cujo tipo tem `conhecimento.gerenciar` (1 e-mail por pessoa); um tipo próprio com a função recebe, um `ATENDENTE` não; sem ninguém com a função nada é gravado em `aviso_validade` (FEAT, AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AvisarValidadeTests.Avisa_quem_tem_conhecimento_gerenciar"`

**C13** - `POST /api/plataforma/instituicoes/{slug}/reenviar-acesso` envia o link só para os usuários ativos cujo tipo tem `usuarios.gerenciar` (um tipo próprio com a função recebe, um `ATENDENTE` não) e responde `409` quando não há nenhum (FEAT, AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaTests.Reenviar_acesso_para_quem_tem_usuarios_gerenciar"`

**C14** - Um token MCP válido de usuário ativo, sem troca de senha pendente, cujo tipo tem `mcp.usar` (testado com `ATENDENTE`, `GESTOR` e um tipo próprio só com `mcp.usar`) autentica a chamada `tools/list` com `200` (FEAT, AC 12)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~McpAcessoTests.Autentica_com_mcp_usar"`

**C15** - Com o mesmo token, depois de `mcp.usar` sair do tipo (ou para um usuário de tipo `ALUNO`), a chamada seguinte recebe `401`; o token continua gravado e volta a valer quando a função volta (FEAT, AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~McpAcessoTests.Sem_mcp_usar_401_na_chamada_seguinte"`

**C16** - Nenhum arquivo de `Esclari.Api` (fora de `Dados/Migracoes/` e de `Dados/Usuarios/PonteDePapeis.cs`) contém `Papeis.` ou `.Tem(`; só a política central decide por função, e nenhum arquivo fora dela compara texto de papel (FEAT, AC 15)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArquiteturaTests.Autorizacao_so_pela_politica"`

**C17** - O banco recusa (SQLSTATE `23505`) um segundo tipo `coordenador DE curso` na instituição que já tem `Coordenador de Curso` e aceita o mesmo nome em outra instituição (door 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposNoBancoTests.Nome_unico_por_instituicao_sem_diferenciar_maiusculas"`

**C18** - O banco recusa (`23505`) um segundo tipo com `sistema` `GESTOR` na mesma instituição, aceita dois tipos com `sistema` nulo e aceita `GESTOR` em outra instituição (door 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposNoBancoTests.Sistema_unico_por_instituicao"`

**C19** - O banco recusa (`23502`) gravar um usuário com `tipo_usuario_id` nulo, e a coluna `usuario.tipo_usuario_id` existe como `bigint` não nulo (door 1, Relations)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposNoBancoTests.Usuario_exige_tipo"`

**C20** - O banco recusa (`23503`) apagar um tipo que tem 1 usuário, e o usuário continua com o mesmo tipo (door 1, Relations)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposNoBancoTests.Tipo_em_uso_nao_apaga"`

**C21** - O banco recusa (`23505`) a mesma função gravada duas vezes no mesmo tipo e aceita a mesma função em dois tipos; apagar um tipo sem usuários apaga as linhas de função dele (door 2, Relations)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposNoBancoTests.Funcao_unica_por_tipo_e_some_com_o_tipo"`

**C22** - Com a instituição A em `InstituicaoAtual`, a consulta de tipos devolve só os da A, e gravar um tipo com `instituicao_id` da B recusa (AD-008, Relations)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~IsolamentoTests.Tipos_de_outra_instituicao_nao_aparecem"`

**C23** - `tipo_usuario` e `tipo_usuario_funcao` têm `instituicao_id` não nulo e query filter global, `TipoDeUsuario` e `FuncaoDoTipo` estão uma por arquivo em `Dados/Usuarios/`, e as tabelas e colunas novas são `snake_case` com datas `timestamptz` (AD-008, AD-017)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArquiteturaTests.Toda_entidade_tem_instituicao_e_filtro"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArquiteturaTests.Tabelas_snake_case_e_datas_timestamptz"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArquiteturaTests.Uma_entidade_por_arquivo_por_agregado"`

**C24** - A migration dá a cada usuário existente o tipo da ordem da door 3, nas 8 combinações de papéis: `ADMINISTRADOR` -> `GESTOR`; `ADMINISTRADOR`+`ATENDENTE` -> `GESTOR`; `ADMINISTRADOR`+`ALUNO` -> `GESTOR`; `ADMINISTRADOR`+`ALUNO`+`ATENDENTE` -> `GESTOR`; `ALUNO`+`ATENDENTE` -> "Líder de semestre"; `ATENDENTE` -> `ATENDENTE`; `ALUNO` -> `ALUNO`; nenhum papel -> `ALUNO` (FEAT, AC 2)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MigracaoTiposTests.Ordem_de_conversao_por_papeis"`

**C25** - Depois da migration, as 4 linhas de `atendente_setor`, as 3 matrículas e as 6 linhas de `usuario_papel` semeadas antes continuam iguais (mesmos ids e valores), e toda instituição semeada tem exatamente 3 tipos de sistema (FEAT, AC 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MigracaoTiposTests.Preserva_setores_matriculas_e_usuario_papel"`

**C26** - Com duas instituições semeadas, uma com 2 usuários `ALUNO`+`ADMINISTRADOR` e outra com 0, a migration emite um `RAISE NOTICE` que contém o id da primeira instituição e o total 2, e nenhum aviso para a segunda (FEAT, AC 4)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MigracaoTiposTests.Avisa_o_total_de_aluno_com_administrador_por_instituicao"`

**C27** - Numa instituição com 2 usuários `ALUNO`+`ATENDENTE` (e 1 `ALUNO`+`ATENDENTE`+`ADMINISTRADOR`), a migration cria 1 tipo "Líder de semestre" com `sistema` nulo e as 4 funções gravadas `assistente.usar`, `fila.atender`, `indicadores.ver`, `mcp.usar`, dá o tipo aos 2 usuários (não ao terceiro) e mantém os setores e as matrículas deles (FEAT, AC 56)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MigracaoTiposTests.Lider_de_semestre_nasce_com_as_quatro_funcoes"`

**C28** - Numa instituição sem usuário `ALUNO`+`ATENDENTE` fora de `ADMINISTRADOR`, a migration deixa 3 tipos (nenhum chamado "Líder de semestre"); na mesma execução outra instituição com esse usuário recebe o tipo (FEAT, AC 57)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MigracaoTiposTests.Sem_lider_nao_cria_o_tipo"`

**C29** - Com um usuário órfão (sem instituição correspondente) semeado antes, a migration falha, a tabela `tipo_usuario` não existe depois e `__EFMigrationsHistory` não lista a migration nova: não sobra tipo pela metade (Observable, failure halfway)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MigracaoTiposTests.Falha_no_meio_desfaz_tudo"`

**C30** - O snapshot do modelo acompanha o modelo: nenhuma diferença pendente, e a tabela `usuario_papel` continua no modelo (door 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MigracaoTiposTests.Snapshot_acompanha_o_modelo"`

**C31** - Um usuário migrado para "Líder de semestre", com matrícula `ATIVA` e vínculo num setor de liderança, abre conversa (`POST /api/conversas` `201`), lista as próprias solicitações (`GET /api/chamados` 200), vê a fila da turma (`GET /api/chamados?escopo=fila` 200 com o chamado da turma) e, no detalhe desse chamado, tem papel `LIDER`: `transcricao` vazia e sem número de matrícula nem anexos da conversa (FEAT, AC 58)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LiderDeSemestreTests.Lider_migrado_segue_com_assistente_fila_e_solicitacoes"`

**C32** - Um usuário de tipo com `assistente.usar` e só matrícula `INATIVA` recebe `403` em `POST /api/conversas` com "O assistente é só para alunos com matrícula ativa." e em `POST /api/atendimento-direto` com "O atendimento é só para alunos com matrícula ativa."; com a matrícula `ATIVA` os dois recebem `201` (FEAT, AC 59)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LiderDeSemestreTests.Sem_matricula_ativa_403_nas_duas_rotas"`

**C33** - Quando o líder é o único atendente do setor de liderança da própria turma e abre uma solicitação, ela não vai para esse setor: `ConsultasDeFluxo.Lideranca` devolve `null` e o roteamento registra "Sem liderança disponível"; com um segundo atendente ativo no setor, vai para o setor (FEAT, AC 62)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LiderDeSemestreTests.Lider_nao_recebe_a_propria_solicitacao"`

**C34** - Cada uma das 22 rotas da door 5 libera só quem tem a função dela: um usuário com um tipo próprio que tem só a função da linha recebe um status diferente de `403` na rota da linha (200, 201 ou 400 por corpo vazio) e `403` em uma rota de outra função: `GET /api/admin/usuarios` usuarios.gerenciar · `POST /api/admin/matriculas/importacao` matriculas.importar · `GET /api/admin/setores`, `/api/admin/categorias`, `/api/admin/cursos`, `/api/admin/respostas-prontas` e `/api/admin/fluxos` cadastros.gerenciar · `GET /api/admin/artigos`, `/api/admin/documentos` e `/api/conhecimento/validade` conhecimento.gerenciar · `GET /api/admin/aparencia` aparencia.gerenciar · `GET /api/admin/moodle` e `/api/admin/mcp` integracoes.gerenciar · `GET /api/chamados/busca?escopo=todos` e `GET /api/chamados/exportar?escopo=todos` chamados.ver_todos · `GET /api/indicadores` e `GET /api/indicadores/detalhe` indicadores.ver · `GET /api/chamados?escopo=fila`, `GET /api/respostas-prontas?setorId=` e `POST /api/chamados/{numero}/artigo` fila.atender · `POST /api/conversas` e `POST /api/atendimento-direto` assistente.usar (FEAT, door 5, AC 7)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AutorizacaoPorFuncaoTests.Cada_rota_libera_so_a_sua_funcao"`

**C35** - O hub de tempo real recusa `EntrarNaFila` com "A fila é só para atendentes." para um tipo sem `fila.atender` (e para quem tem a função e nenhum setor) e entra na fila de cada setor do usuário para um tipo próprio com `fila.atender` e 2 setores; a função é relida a cada chamada (FEAT, door 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TempoRealEntradaTests.EntrarNaFila_recusa_quem_nao_e_atendente"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TempoRealEntradaTests.EntrarNaFila_entra_tipo_proprio_com_fila_atender"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TempoRealEntradaTests.Papel_e_setor_sao_relidos_a_cada_chamada"`

**C36** - `GET /api/artigos/versoes/{id}` e `GET /api/documentos/versoes/{id}` respondem `200` para quem tem `conhecimento.gerenciar` ou `fila.atender` com um setor comum, e `404` ("Versão de artigo não encontrada." e "Documento não encontrado.") para quem só tem setor de liderança e para quem não tem nenhuma das duas funções (FEAT, door 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LiderSemVazamentoTests.Versao_de_artigo_e_documento_por_funcao"`

**C37** - `SoLideranca` é falso para quem tem `conhecimento.gerenciar`, mesmo com setores de liderança, e verdadeiro para quem só tem `fila.atender` e setores com curso (e nenhum setor comum ativo); o líder continua sem acesso aos indicadores, ao relatório e às versões (FEAT, door 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LiderSemVazamentoTests.So_lideranca_por_funcao"`

### L2 - rotas da API: tipos, usuários, sessão, plataforma, Moodle e CSV · critérios 14, 15 (final), 16 a 30, 31 a 46, 54, 60 · 22 arquivos de código + 17 de teste existentes, mais ~30 KB de código novo e ~80 KB de teste novo · ~442 KB · ~110k

**C38** - `GET /api/admin/tipos` de um gestor numa instituição nova devolve `200` com `tipos` em 3 itens ordenados `ALUNO`, `ATENDENTE`, `GESTOR` (`sistema` com os mesmos valores, `usuarios` com a contagem de cada um) e `catalogo` com 12 itens, cada um com `codigo`, `rotulo` e `descricao` não vazios; `expoeDadosPessoais` é `true` só em `fila.atender`, `chamados.ver_todos`, `usuarios.gerenciar`, `matriculas.importar` e `mcp.usar`, e `deAluno` é `true` só em `assistente.usar` (FEAT, AC 28)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Listar_devolve_tipos_e_catalogo"`

**C39** - `GET /api/admin/tipos` responde `200` com a lista completa para um usuário de tipo próprio que só tem `usuarios.gerenciar` e para outro que só tem `tipos.gerenciar`, e `403` para um de tipo próprio que não tem nenhuma das duas (FEAT, AC 29)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Listar_aceita_tipos_gerenciar_ou_usuarios_gerenciar"`

**C40** - `POST /api/admin/tipos` com `{"nome":"Coordenador de curso","funcoes":["indicadores.ver","cadastros.gerenciar"]}` responde `201` com `nome` "Coordenador de curso", `sistema` nulo, `funcoes` `["cadastros.gerenciar","indicadores.ver"]` (ordem alfabética), `usuarios` 0; o tipo fica na instituição da sessão e as 2 linhas de função são gravadas junto (FEAT, AC 16)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Criar_201_com_funcoes_gravadas_juntas"`

**C41** - `POST` e `PUT /api/admin/tipos` com `nome` `""`, `"   "` ou de 61 caracteres respondem `400` com o campo `nome`; com 60 caracteres respondem `201`/`200`; nada é gravado nos `400` (FEAT, AC 17)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Nome_de_0_a_60_caracteres"`

**C42** - `POST` e `PUT /api/admin/tipos` com `funcoes` `["relatorios.ver"]` ou `["indicadores.ver","FILA.ATENDER"]` respondem `400` com o campo `funcoes`, e o conjunto gravado não muda (FEAT, AC 18, AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Funcao_fora_do_catalogo_400"`

**C43** - `POST /api/admin/tipos` com `nome` "coordenador DE curso" quando existe "Coordenador de curso" responde `409`; o mesmo vale contra o nome de um tipo de sistema ("gestor"); `PUT` de um tipo para o nome de outro responde `409`; mudar a caixa do próprio nome responde `200` (FEAT, AC 19)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Nome_repetido_409"`

**C44** - Com 29 tipos na instituição (3 de sistema e 26 próprios), `POST` responde `201` (o 30º) e o `POST` seguinte responde `409` com "A instituição já tem 30 tipos de usuário."; o limite não conta tipos de outra instituição (FEAT, AC 20)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Limite_de_30_tipos_409"`

**C45** - `PUT /api/admin/tipos/{id}` de um tipo próprio responde `200` e troca nome e conjunto de funções por completo (as removidas somem, as novas entram); `PUT` do tipo `ATENDENTE` com o mesmo nome e `["fila.atender","indicadores.ver"]` responde `200` e grava só essas 2 (FEAT, AC 21)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Alterar_tipo_proprio_e_funcoes_do_atendente_200"`

**C46** - `PUT /api/admin/tipos/{id}` responde `409` com "Este tipo é de sistema e não pode ser alterado." ao renomear `ATENDENTE` e a qualquer `PUT` em `GESTOR` e em `ALUNO` (funções diferentes, nome igual ou diferente); nome e funções no banco ficam iguais (FEAT, AC 22)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Tipo_de_sistema_intocavel_409"`

**C47** - Um usuário de tipo próprio com `tipos.gerenciar` e `indicadores.ver` (sem `usuarios.gerenciar`) que envia `POST` com `["usuarios.gerenciar"]` recebe `403` e nada é gravado; o mesmo vale para `PUT` de um tipo que acrescenta `usuarios.gerenciar`; com `["indicadores.ver"]` recebe `201` (FEAT, AC 23)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.So_da_o_que_tem_403"`

**C48** - `DELETE /api/admin/tipos/{id}` de um tipo próprio sem usuários responde `204` e o tipo e suas linhas de função somem; um `GET` seguinte não o lista (FEAT, AC 24)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Apagar_tipo_sem_usuarios_204"`

**C49** - `DELETE /api/admin/tipos/{id}` de um tipo com 2 usuários responde `409` com "2 usuário(s) usam este tipo." e o tipo continua; depois de mover os 2 usuários, o `DELETE` responde `204` (FEAT, AC 25)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Apagar_tipo_em_uso_409_com_a_quantidade"`

**C50** - `DELETE /api/admin/tipos/{id}` dos tipos de sistema `ALUNO`, `GESTOR` e `ATENDENTE` responde `409` com "Este tipo é de sistema e não pode ser apagado." (FEAT, AC 26)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Apagar_tipo_de_sistema_409"`

**C51** - `PUT` e `DELETE /api/admin/tipos/{id}` com id `999999` e com o id de um tipo de outra instituição respondem `404`, e o tipo da outra instituição não muda (FEAT, AC 27)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Id_inexistente_ou_de_outra_instituicao_404"`

**C52** - `POST`, `PUT` e `DELETE /api/admin/tipos` respondem `403` para um usuário de tipo `ATENDENTE` e para um tipo próprio que só tem `usuarios.gerenciar`; `GET` responde `403` para o `ATENDENTE` e para um `ALUNO`; a função `tipos.gerenciar` libera as 4 rotas (FEAT, AC 29, AC 7)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Sem_tipos_gerenciar_403_nas_quatro_rotas"`

**C53** - As 4 rotas de `/api/admin/tipos` respondem `401` sem cookie de sessão (FEAT, Surface)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Sem_sessao_401_nas_quatro_rotas"`

**C54** - `DELETE /api/admin/tipos/{id}` e `PUT /api/admin/usuarios/{id}` com esse `tipoId` disparados juntos terminam sempre em um de dois estados: `DELETE` 204 e `PUT` 400 no campo `tipoId`, ou `PUT` 200 e `DELETE` 409; em 20 repetições nenhum usuário fica com `tipo_usuario_id` apontando para tipo inexistente ou nulo (FEAT, AC 30)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Apagar_e_atribuir_juntos_nunca_deixam_usuario_sem_tipo"`

**C55** - Um usuário de tipo próprio com `tipos.gerenciar` e sem `assistente.usar` envia `POST` com `["assistente.usar"]` e recebe `201`; para cada um dos outros 11 códigos que o tipo dele não tem, o mesmo `POST` recebe `403` (FEAT, AC 60, AC 23)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TiposDeUsuarioTests.Funcao_de_aluno_e_a_unica_isenta"`

**C56** - `POST /api/admin/usuarios` com `tipoId` de um tipo próprio responde `201` e `PUT /api/admin/usuarios/{id}` com outro `tipoId` responde `200`; as duas respostas trazem `tipo` com `id` e `nome` e nenhum campo `papeis`, e o banco guarda 1 único `tipo_usuario_id` (FEAT, AC 31)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AtribuirTipoTests.Criar_e_alterar_com_tipo_devolvem_tipo_sem_papeis"`

**C57** - `POST` e `PUT /api/admin/usuarios` sem `tipoId`, com `tipoId` `999999` e com o `tipoId` de uma instituição vizinha respondem `400` com o campo `tipoId`; nada é gravado (FEAT, AC 32)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AtribuirTipoTests.Tipo_ausente_inexistente_ou_de_outra_instituicao_400"`

**C58** - Para o tipo `ATENDENTE` (e para um tipo próprio com `fila.atender`) com `setores` `[]`, `POST` e `PUT` respondem `400` com o campo `setores` ("Escolha ao menos um setor para quem atende a fila."); para `GESTOR` com `setores` `[]` respondem `201`/`200` (FEAT, AC 33, door 6)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AtribuirTipoTests.Tipo_com_fila_atender_exige_setor_menos_o_gestor"`

**C59** - Um usuário `ATENDENTE` com 2 setores que passa a `ALUNO` (e a um tipo próprio sem `fila.atender`) fica com 0 linhas em `atendente_setor` (FEAT, AC 34)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AtribuirTipoTests.Trocar_para_tipo_sem_fila_atender_remove_os_setores"`

**C60** - Um usuário de tipo próprio com `usuarios.gerenciar` e sem `indicadores.ver` recebe `403` com "Você não pode atribuir ou retirar um tipo com funções que o seu tipo não tem." ao atribuir um tipo que tem `indicadores.ver` e ao trocar um usuário que hoje tem um tipo com `indicadores.ver`; atribuir um tipo que só difere pela função `assistente.usar` responde `200` (FEAT, AC 35)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AtribuirTipoTests.So_atribui_tipo_com_funcoes_que_tem_menos_a_de_aluno"`

**C61** - O gestor que envia `PUT /api/admin/usuarios/{id}` com o próprio `id` e outro `tipoId` recebe `409` com "Você não pode mudar o próprio tipo. Peça a outro gestor." e o tipo no banco não muda; com o mesmo `tipoId` e outro nome recebe `200` e grava o nome; o mesmo gestor muda o tipo de outro gestor com `200` (FEAT, AC 36)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AtribuirTipoTests.Gestor_nao_muda_o_proprio_tipo"`

**C62** - `ValidacaoDeUsuario.UltimoGestor` devolve `409` com "A instituição precisa de ao menos um gestor ativo." quando o alvo é o único `GESTOR` ativo e deixaria de ser gestor ou seria desativado; devolve `null` com um segundo gestor ativo, com o segundo gestor inativo devolve `409`, quando o alvo não é gestor ativo devolve `null` e quando continua gestor devolve `null` (FEAT, AC 36)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AdminTests.Ultimo_gestor_ativo_nao_se_perde"`

**C63** - `POST /api/admin/usuarios` com CPF inválido, e-mail vazio ou nome vazio responde `400` com o campo do erro, e o `PUT` do mesmo corpo também (FEAT, Surface `400`)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AdminTests.Campos_invalidos_400"`

**C64** - `PUT /api/admin/usuarios/999999` responde `404` (FEAT, Surface `404`)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AdminTests.Usuario_inexistente_404"`

**C65** - `POST` e `PUT /api/admin/usuarios` com CPF ou e-mail já usado por outro usuário respondem `409` (FEAT, Surface `409`)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AdminTests.Cpf_ou_email_repetido_409"`

**C66** - `GET`, `POST` e `PUT /api/admin/usuarios` respondem `401` sem sessão e `403` para o tipo `ATENDENTE` e para um tipo próprio sem `usuarios.gerenciar` (FEAT, Surface, AC 7)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AtribuirTipoTests.Sem_sessao_401_e_sem_funcao_403_nas_rotas_de_usuario"`

**C67** - `GET /api/admin/usuarios` devolve cada usuário com `tipo` (`id`, `nome`) e nenhum campo `papeis` (FEAT, AC 40)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AtribuirTipoTests.Listar_usuarios_com_tipo"`

**C68** - `GET /api/admin/usuarios?tipoId=<id>` devolve só os usuários desse tipo (2 de 5), `tipoId=999999` devolve `200` com lista vazia, e sem o parâmetro devolve os 5 (FEAT, AC 54)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AtribuirTipoTests.Filtrar_usuarios_por_tipo"`

**C69** - A importação de matrículas com um CPF novo cria o usuário no tipo `ALUNO` (e a matrícula) (FEAT, AC 37)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AdminTests.Csv_cpf_novo_nasce_no_tipo_aluno"`

**C70** - A importação com o CPF de um usuário "Líder de semestre" e outro de um `GESTOR` mantém o tipo de cada um e só soma a matrícula (FEAT, AC 38)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AdminTests.Csv_cpf_existente_mantem_o_tipo"`

**C71** - O lançamento do Moodle redireciona com o motivo `use-senha` para um usuário de tipo `ATENDENTE`, `GESTOR` e "Líder de semestre" (com matrícula e vínculo), e abre a sessão para o tipo `ALUNO` (FEAT, AC 39)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MoodleLancamentoTests.Equipe_nao_entra_pelo_moodle"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MoodleLancamentoTests.Token_valido_de_aluno_vinculado_abre_a_sessao"`

**C72** - `GET /api/auth/me` devolve `200` com `tipo` (`id`, `nome`), `permissoes` ordenadas pela ordem alfabética ordinal e nenhum campo `papeis`: gestor sem setor `["aparencia.gerenciar","cadastros.gerenciar","chamados.ver_todos","conhecimento.gerenciar","indicadores.ver","integracoes.gerenciar","matriculas.importar","mcp.usar","tipos.gerenciar","usuarios.gerenciar"]`, aluno `["assistente.usar"]`, "Líder de semestre" com setor `["assistente.usar","fila.atender","indicadores.ver","mcp.usar"]` (FEAT, AC 14)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~SessaoTests.Me_devolve_tipo_e_permissoes_sem_papeis"`

**C73** - `POST /api/auth/login` com credenciais válidas devolve `200` com os mesmos campos `tipo` e `permissoes` do `me` do mesmo usuário e nenhum `papeis` (FEAT, AC 14)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LoginTests.Login_valido_200_com_tipo_e_permissoes"`

**C74** - `GET /api/auth/me` sem cookie e `POST /api/auth/login` com senha errada ou conta desativada respondem `401` (FEAT, Surface)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~SessaoTests.Sem_cookie_401_em_toda_rota_privada"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LoginTests.Conta_desativada_ou_sem_senha_401_sem_cookie"`

**C75** - A sexta tentativa de `POST /api/auth/login` no mesmo minuto responde `429` (FEAT, Surface)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~RateLimitTests.Login_e_primeiro_acesso_somam_5_por_minuto"`

**C76** - `Esclari.Api/Dados/Usuarios/PonteDePapeis.cs` e `Papeis.cs` não existem, e nenhum arquivo de `Esclari.Api` (fora de `Dados/Migracoes/`) contém `Papeis.`, `.Tem(` ou `DefinirPapeis`; só a entidade `UsuarioPapel` e o mapeamento seguem no modelo (FEAT, AC 15)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArquiteturaTests.Sem_ponte_de_papeis"`

**C77** - Depois do lote 2 o snapshot continua sem diferença pendente e `usuario_papel` continua no modelo (door 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~MigracaoTiposTests.Snapshot_acompanha_o_modelo"`

**C78** - Criar uma instituição pelo console cria, na mesma transação, os 3 tipos de sistema e o primeiro administrador com o tipo `GESTOR`; quando a gravação do e-mail falha, não sobra instituição, tipo nem usuário (FEAT, AC 41)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaTests.Criar_instituicao_201_com_tipos_e_gestor"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaTests.Falha_ao_gravar_o_email_nao_cria_a_instituicao"`

**C79** - `POST /api/plataforma/instituicoes/{slug}/equipe` com `tipo` `GESTOR` e com `tipo` `ATENDENTE` (este com 1 setor) responde `201`, o membro tem o tipo pedido e a resposta traz `tipo` no lugar de `papeis`; o e-mail de primeiro acesso traz link de 60 minutos e diz "gestor" para `GESTOR` e "atendente" para `ATENDENTE` (FEAT, AC 42)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Criar_membro_por_tipo_convida_e_audita"`

**C80** - `POST .../equipe` com `tipo` `ALUNO`, `"x"` ou ausente responde `400` com o campo `tipo`; um corpo só com `papeis` também responde `400` (FEAT, AC 43)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Tipo_invalido_400"`

**C81** - `POST .../equipe` com `tipo` `ATENDENTE` e `setores` `[]` responde `400` com o campo `setores`; com um setor de outra instituição também responde `400` (FEAT, AC 44)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Atendente_exige_setor_da_propria_instituicao"`

**C82** - `GET .../equipe` devolve `200` com os usuários de qualquer tipo menos `ALUNO` (gestor, atendente e um "Líder de semestre"), cada um com `tipo` e CPF e e-mail mascarados; reconvidar, desativar e reativar valem para o líder e nenhum aluno aparece (FEAT, AC 45)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Lista_so_equipe_por_tipo_mascarada"`

**C83** - As rotas `GET` e `POST .../equipe` respondem `401` sem a sessão da plataforma (FEAT, Surface)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Sem_sessao_401_em_toda_rota_nova"`

**C84** - `GET` e `POST .../equipe` respondem `404` para um slug que não existe (FEAT, Surface)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaTests.Slug_inexistente_404"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Aluno_e_outra_instituicao_404"`

**C85** - `POST .../equipe` com CPF ou e-mail já usado na instituição responde `409` (FEAT, Surface)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Repetido_409_e_invalido_400"`

**C86** - Criar, reconvidar, desativar e reativar um membro grava `USUARIO_CRIADO`, `USUARIO_ALTERADO` ou a ação já existente, com o nome do tipo no resumo ("Gestor", "Atendente"), sem CPF nem e-mail, e `auditoria_plataforma` continua com as mesmas 11 ações (FEAT, AC 46)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaAuditoriaTests.Resumo_da_equipe_traz_o_nome_do_tipo"`

**C87** - `POST .../equipe/{id}/desativar` do único `GESTOR` ativo responde `409` ("A instituição precisa de ao menos um gestor ativo."); com um segundo gestor ativo responde `200` (FEAT, AC 36, AC 45)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ImplantacaoEquipeTests.Desativar_protege_o_ultimo_gestor"`

### L3 - web: menu por função, tela de tipos, cadastro de usuário e equipe da plataforma · critérios 47 a 53, 55, 61 e o web de 42 a 45 e 54 · 21 arquivos existentes, mais ~55 KB novos · ~295 KB · ~74k

**C88** - Com `permissoes` contendo uma única função, o menu mostra só os itens dela: `assistente.usar` "Assistente" e "Minhas solicitações" · `fila.atender` "Fila" · `chamados.ver_todos` "Chamados" · `indicadores.ver` "Indicadores" · `conhecimento.gerenciar` "Artigos" e "Documentos" · `usuarios.gerenciar` "Usuários" · `matriculas.importar` "Matrículas" · `cadastros.gerenciar` "Cadastros" · `aparencia.gerenciar` "Aparência" · `integracoes.gerenciar` "Integrações" · `tipos.gerenciar` "Tipos de usuário" · `mcp.usar` nenhum item (FEAT, AC 47)
Proof: `npm --prefix web test -- src/acesso/menuPorFuncao.test.tsx -t "menu por função"`

**C89** - A tela de entrada abre `assistente` com `["assistente.usar","fila.atender"]`, `fila` com `["fila.atender","indicadores.ver"]`, `chamados` com `["chamados.ver_todos","usuarios.gerenciar"]`, `indicadores` com `["indicadores.ver","cadastros.gerenciar"]`, `artigos` com `["conhecimento.gerenciar","usuarios.gerenciar"]` e `usuarios` com `["usuarios.gerenciar"]`; um endereço de uma tela sem a função volta para a tela de entrada (FEAT, AC 48)
Proof: `npm --prefix web test -- src/acesso/menuPorFuncao.test.tsx -t "tela de entrada por função"`

**C90** - Com `permissoes` `[]` e com `["mcp.usar"]` a pessoa vê "Seu tipo de usuário ainda não tem funções liberadas. Fale com o gestor da sua instituição.", nenhum item de menu além de "Conta" e o botão de sair (FEAT, AC 49)
Proof: `npm --prefix web test -- src/acesso/menuPorFuncao.test.tsx -t "sem funções liberadas"`

**C91** - A tela "Tipos de usuário" lista os 3 tipos de sistema primeiro e depois os próprios por nome, cada linha com nome, quantidade de usuários e as funções; com só os tipos de sistema mostra os 3 e o botão "Novo tipo"; um `403` mostra a mensagem do servidor e não a lista (FEAT, AC 50)
Proof: `npm --prefix web test -- src/administracao/cadastros/tipos.test.tsx -t "lista os tipos"`

**C92** - O formulário do tipo mostra, para cada uma das 12 funções, o `rotulo` e a `descricao` do servidor, e o aviso "Mostra dados pessoais dos alunos." só nas 5 com `expoeDadosPessoais`; `assistente.usar` aparece marcada como função de aluno; salvar envia `{nome, funcoes}` e um `409` mostra a mensagem do servidor no formulário sem fechá-lo (FEAT, AC 51, AC 17 a 20)
Proof: `npm --prefix web test -- src/administracao/cadastros/tipos.test.tsx -t "formulário do tipo"`

**C93** - "Apagar" abre a confirmação "Apagar o tipo Coordenador de curso?" com o nome do tipo, e só o botão de confirmar envia `DELETE /api/admin/tipos/{id}`; cancelar não envia nada; um `409` mostra "2 usuário(s) usam este tipo." (FEAT, AC 52)
Proof: `npm --prefix web test -- src/administracao/cadastros/tipos.test.tsx -t "apagar pede confirmação"`

**C94** - O cadastro de usuário tem o campo "Tipo" de escolha única no lugar das caixas de papel, preenchido por `GET /api/admin/tipos`, e só mostra os setores quando o tipo escolhido tem `fila.atender` e não é `GESTOR`; o `PUT` e o `POST` enviam `tipoId` e não enviam `papeis`; o gestor logado não vê o campo editável na própria linha e vê "Você não pode mudar o próprio tipo. Peça a outro gestor." (FEAT, AC 53, AC 36)
Proof: `npm --prefix web test -- src/administracao/cadastros/usuarios.test.tsx -t "campo Tipo"`
Proof: `npm --prefix web test -- src/administracao/cadastros/usuarios.test.tsx -t "o próprio gestor"`

**C95** - A lista de usuários tem o filtro "Tipo" e, ao escolher um tipo, pede `GET /api/admin/usuarios?tipoId=<id>` e mostra só esses usuários (FEAT, AC 54)
Proof: `npm --prefix web test -- src/administracao/cadastros/usuarios.test.tsx -t "filtra por tipo"`

**C96** - Clicar na contagem de usuários de um tipo na tela "Tipos de usuário" abre a lista de usuários com `tipoId=<id>` no endereço e a lista já filtrada; uma contagem 0 não é link (FEAT, AC 55)
Proof: `npm --prefix web test -- src/administracao/cadastros/tipos.test.tsx -t "contagem leva à lista"`

**C97** - Com `permissoes` `["assistente.usar","fila.atender","indicadores.ver","mcp.usar"]` o menu mostra "Assistente", "Minhas solicitações", "Fila" e "Indicadores" (nessa ordem), a tela de entrada é `assistente` e o botão de avisos de push do aluno aparece; a lista de entrada de solicitações é a fila (FEAT, AC 61)
Proof: `npm --prefix web test -- src/acesso/menuPorFuncao.test.tsx -t "líder de semestre"`

**C98** - A tela de equipe do console da plataforma escolhe `Gestor` ou `Atendente` num campo "Tipo", envia `tipo` `GESTOR` ou `ATENDENTE` (sem `papeis`), mostra o nome do tipo de cada membro e só pede setor para `ATENDENTE` (FEAT, AC 42 a 45)
Proof: `npm --prefix web test -- src/plataforma/implantacao.test.tsx -t "equipe por tipo"`

**C99** - O rodapé da barra lateral e o da conta mostram `tipo.nome` ("Gestor", "Atendente", "Aluno", "Líder de semestre") no lugar do papel de maior precedência (FEAT, AC 14)
Proof: `npm --prefix web test -- src/acesso/acesso.test.tsx -t "tipo do usuário"`

**C100** - No celular 360×780, a tela "Tipos de usuário" e o formulário do tipo usam uma coluna, a lista de funções é vertical e a página não tem rolagem horizontal (`scrollWidth` igual a `clientWidth`) (Observable: celular 360×780)
Proof: `npm --prefix web run e2e -- e2e/tipos-de-usuario.spec.ts -g "celular 360×780"`

**C101** - No celular deitado 844×390, o formulário rola dentro da tela e o botão "Salvar" fica dentro da janela visível depois de rolar até ele, sem cobrir nenhum campo (Observable: celular deitado 844×390)
Proof: `npm --prefix web run e2e -- e2e/tipos-de-usuario.spec.ts -g "celular deitado 844×390"`

**C102** - Com a janela de 360×400 (teclado aberto), o campo "Nome" do tipo continua visível ao digitar e o botão "Salvar" é alcançável sem ser coberto pelo campo (Observable: teclado aberto)
Proof: `npm --prefix web run e2e -- e2e/tipos-de-usuario.spec.ts -g "teclado aberto 360×400"`

**C103** - As caixas de função, os botões "Novo tipo", "Salvar" e "Apagar" e o seletor "Tipo" do cadastro de usuário têm altura e largura de ao menos 44 px, e nenhuma ação depende de passar o mouse (Observable: toque)
Proof: `npm --prefix web run e2e -- e2e/tipos-de-usuario.spec.ts -g "alvos de toque de 44 px"`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| códigos de função do catálogo (12) | `assistente.usar` C34 · `fila.atender` C34 · `chamados.ver_todos` C34 · `indicadores.ver` C34 · `conhecimento.gerenciar` C34 · `cadastros.gerenciar` C34 · `usuarios.gerenciar` C34 · `matriculas.importar` C34 · `tipos.gerenciar` C52 · `aparencia.gerenciar` C34 · `integracoes.gerenciar` C34 · `mcp.usar` C14 C15 | - |
| códigos aceitos e recusados na entrada (12) | C1, table-driven over all 12 | - |
| funções no menu (12) | C88, table-driven over all 12 | - |
| rotas da door 5 (22) | C34, table-driven over all 22 | - |
| fora das 22 rotas, a função decide também em (3) | hub de tempo real C35 · versões de artigo e documento C36 · acesso por token MCP C14 | - |
| `GET /api/admin/tipos` statuses (3) | 200 C38 C39 · 401 C53 · 403 C52 | - |
| `POST /api/admin/tipos` statuses (5) | 201 C40 · 400 C41 · 401 C53 · 403 C47 · 409 C43 | - |
| `PUT /api/admin/tipos/{id}` statuses (6) | 200 C45 · 400 C42 · 401 C53 · 403 C47 · 404 C51 · 409 C46 | - |
| `DELETE /api/admin/tipos/{id}` statuses (5) | 204 C48 · 401 C53 · 403 C52 · 404 C51 · 409 C49 | - |
| `POST /api/admin/usuarios` statuses (5) | 201 C56 · 400 C57 · 401 C66 · 403 C66 · 409 C65 | - |
| `PUT /api/admin/usuarios/{id}` statuses (6) | 200 C56 · 400 C63 · 401 C66 · 403 C60 · 404 C64 · 409 C61 | - |
| `GET /api/admin/usuarios` statuses (3) | 200 C67 · 401 C66 · 403 C66 | - |
| `GET /api/auth/me` statuses (2) | 200 C72 · 401 C74 | - |
| `POST /api/auth/login` statuses (3) | 200 C73 · 401 C74 · 429 C75 | - |
| `POST /api/plataforma/instituicoes/{slug}/equipe` statuses (5) | 201 C79 · 400 C80 · 401 C83 · 404 C84 · 409 C85 | - |
| `GET /api/plataforma/instituicoes/{slug}/equipe` statuses (3) | 200 C82 · 401 C83 · 404 C84 | - |
| campos de `400` (5) | `nome` C41 · `funcoes` C42 · `tipoId` C57 · `setores` C58 · `tipo` C80 | - |
| entidades e relações de Relations (5) | Instituicao-TipoDeUsuario C17 C22 · TipoDeUsuario-FuncaoDoTipo C21 · TipoDeUsuario-Usuario C19 C20 · Usuario-AtendenteSetor C25 · Setor-AtendenteSetor C25 | - |
| door 1: schema do tipo e do vínculo único (5) | nome único C17 · sistema único C18 · usuário exige tipo C19 · FK `RESTRICT` C20 · filtro por instituição C22 | - |
| door 2: catálogo fechado (2) | códigos fechados C1 · função única por par C21 | - |
| door 3: conversão (8) | C24, table-driven over all 8 | - |
| door 3: obrigações da migration (6) | preserva dados C25 · aviso C26 · líder criado C27 · sem líder C28 · falha inteira C29 · snapshot C30 | - |
| door 4: contrato (6) | `tipo` em me C72 · `permissoes` em me C72 · `tipo` em login C73 · `tipo` em usuários C56 C67 · `tipo` em equipe C79 C82 · sem `papeis` em lugar nenhum C76 | - |
| door 5: destinatários por função (4) | atrasos C10 · atendentes do setor C11 · validade C12 · reenvio C13 | - |
| door 5: arquitetura (2) | só a política, com ponte C16 · sem ponte C76 | - |
| door 6: tipos de sistema e função de aluno (6) | `GESTOR` calculado C2 · `ALUNO` calculado C3 · `ATENDENTE` editável C45 · zero setores só no `GESTOR` C58 · `fila.atender` exige vínculo C5 · `assistente.usar` isenta e a única C55 | - |
| tipos de sistema intocáveis (3) | `ALUNO` C46 C50 · `GESTOR` C46 C50 · `ATENDENTE` C46 C50 | - |
| combinações de papel -> tipo no Moodle e CSV (3) | CSV novo C69 · CSV existente C70 · Moodle C71 | - |
| critérios do líder de semestre (7) | AC 56 C27 · AC 57 C28 · AC 58 C31 · AC 59 C32 · AC 60 C55 · AC 61 C97 · AC 62 C33 | - |
| critérios da plataforma (6) | AC 41 C78 · AC 42 C79 · AC 43 C80 · AC 44 C81 · AC 45 C82 · AC 46 C86 | - |
| celular e toque, do Observable (4) | 360×780 C100 · deitado 844×390 C101 · teclado aberto C102 · toque C103 | - |
| startup configuration: a política central e os tipos de sistema por instituição (1 assembly) | `Program` + `EsclariFactory`, a mesma montagem das rotas C34 | - |

- Afirmam status ou corpo de rota HTTP, e cada prova atravessa a rota: C6, C8, C31 a C34, C38 a C75, C78 a C87 (C35 atravessa o hub; C62 e C33 têm prova direta porque a regra não é alcançável por uma rota só, como no `papel-do-proprio-gestor`)
- C1 a C5, C17 a C23 e C24 a C30 são provas de domínio e de banco, no próprio nível; nenhuma afirma status
- C88 a C103 são afirmações de tela e de celular; os corpos simulados em C91 a C95 e C98 têm o formato que C38, C56 e C79 afirmam na API
- Nenhum outro check afirma mais do que o caso que a própria prova exercita

## Test policy

| Code | Required proofs | Coverage expectation |
| --- | --- | --- |
| Decide, atravessa a rota HTTP (`CriarTipo`, `AlterarTipo`, `ApagarTipo`, `ListarTipos`, `CriarUsuario`, `AlterarUsuario`, `Eu`, `Login`, `CriarMembro`) | uma na rota | um caso por status da tabela de Surface e um por ramo de decisão novo (nome, catálogo, limite, sistema, só dá o que tem, em uso) |
| Decide, tabela de conversão e função efetiva (`Funcoes`, `PoliticaDeAcesso`, migration `TiposDeUsuario`) | uma na própria camada **e** uma na rota ou no banco real | um caso por linha: 8 combinações de papel, 12 códigos, 3 formas de calcular (gravada, calculada, exige setor) |
| Decide, sem rota que o alcance (`ValidacaoDeUsuario.UltimoGestor`) | uma na própria camada com o banco real | um caso por linha da tabela de 5 (como `papel-do-proprio-gestor` C6) |
| Decide, na tela (`menuPorFuncao`, `TiposDeUsuario`, campo "Tipo", `Equipe`) | uma na própria camada | um caso por função do menu (12), por ordem de tela de entrada (6) e por estado (vazio, `403`, `409`) |
| Layout de celular | uma no Playwright, nas medidas do plano | as 4 linhas do Observable |
| Instrumentation: DTOs de resposta, mapeamentos EF sem condicional | nenhuma própria | coberta pelo consumidor (rota ou banco) |

Evidence:

- `Esclari.Api/EquipeDeSetor.cs`, `Chamados/FiltroDeChamados.cs`, `Chamados/Consulta/ChamadoDoUsuario.cs`, `Indicadores/LeitorIndicadores.cs`, `Mcp/AutenticacaoMcp.cs`: 5 arquivos com 2 a 4 desvios por papel cada, 14 desvios em conjunto que viram função -> decide
- `Esclari.Api/Administracao/Usuarios/ValidacaoDeUsuario.cs`: 3 regras (papéis válidos, atendente exige setor, último administrador) que viram tipo existente, setor e último gestor -> decide
- `Esclari.Api/Acesso/LancamentoMoodle.cs:57` e `Assistente/Conversas/ConversaDoAluno.cs:18`: 1 desvio cada -> decide
- `web/src/Autenticado.tsx`: 13 itens de menu com `papeis`, 3 ramos em `perfilDoModo` e 3 em `telaInicial` (`web/src/sessao.ts`) -> decide
- análogo mais próximo no repo: `Esclari.Api/Chamados/Permissao/PermissoesChamado.cs`, tabela por papel e estado, já provada por `PermissoesChamadoTests.Acoes_por_papel_e_estado` e `Subir_so_para_atendente_e_lider` com um caso por linha; a migration segue `MigracaoOperadorTests.Apaga_so_as_linhas_operador` (migra até a anterior, semeia, migra até a última, consulta)
- as regras do `AGENTS.md` (testes de integração batem na rota HTTP; regra de domínio tem teste unitário) já respondem nível e profundidade; as linhas acima só nomeiam os membros

Cost: ~16 provas na própria camada e no banco real (`FuncoesDoTipoTests`, `TiposNoBancoTests`, `MigracaoTiposTests`, `LiderDeSemestreTests`, 2 de `ArquiteturaTests`) além das provas de rota. Sem essas linhas, a conversão de 8 combinações e a função efetiva de `fila.atender` seriam provadas só por um caminho que passa por elas. As linhas não pedem mudança em nenhum guia do repo: o `AGENTS.md` já as cobre, então nada é escrito nele.

## Existing assertions changed

Mudanças legítimas, previstas pelo plano. Nenhuma enfraquece uma asserção: cada uma troca o campo ou a mensagem que o plano troca, ou o contrato de entrada que o plano muda.

| Arquivo | O que muda | Lote | Por quê |
| --- | --- | --- | --- |
| `Esclari.Api.Tests/Infra/Semente.cs` (`Usuario(...)`, `Instituicao(...)`, `AlunoLogado`) | a assinatura com `string[] papeis` fica; passa a semear os 3 tipos de sistema e a mapear a lista para o tipo na ordem da door 3 | L1 | evita mexer em ~55 arquivos de teste que só montam usuário |
| `ArquiteturaTests` (`TabelasDaInstituicao`, mapa `["Usuarios"]`, `Subpastas_das_fatias` com `Administracao/Usuarios` de 6 para 12 arquivos, contagem de objeto anônimo) | entram `tipo_usuario`, `tipo_usuario_funcao`, `TipoDeUsuario`, `FuncaoDoTipo` e os arquivos de rota de tipos; regras novas `Autorizacao_so_pela_politica` (L1) e `Sem_ponte_de_papeis` (L2) | L1, L2 | AD-017, AD-019, AD-056 e a door 5: mudou a regra, o AD-076 foi escrito primeiro |
| `DocumentoTests`, `RetratoTests` | "Só administradores podem usar esta área." -> "Seu tipo de usuário não tem acesso a esta área." | L1 | critério 7 |
| `AssistenteTests.Sem_papel_aluno_ou_matricula_ativa_403` | passa a montar o caso sem a função `assistente.usar` em vez de sem o papel; os status e a mensagem seguem | L1 | critério 59 |
| `UsuarioDominioTests` (`Criar_traz_os_dados_os_papeis_e_nenhuma_senha`, `Definir_papeis_remove_os_ausentes_e_nao_duplica`) | `Usuario.Criar` recebe o tipo; `DefinirPapeis` e `DefinirAcesso(papeis, ...)` saem no L2 | L1, L2 | door 1 e door 4 |
| `AdminTests` (54 ocorrências), `OrdemDasRecusasTests` (`Cadeia_do_usuario`, `Acao_por_papel_errado_403_mesmo_com_corpo_invalido`) | corpos com `papeis` -> `tipoId`; `Sem_papel_ou_setor_400` -> `Sem_tipo_ou_setor_400`; `Admin_nao_muda_o_proprio_papel` e `Ultimo_administrador_ativo_nao_se_perde` do `papel-do-proprio-gestor` -> `Gestor_nao_muda_o_proprio_tipo` e `Ultimo_gestor_ativo_nao_se_perde` com as mensagens de C61 e C62 | L2 | critérios 31 a 36; o plano manda estender a regra de `papeis` para `tipoId` |
| `LoginTests.Login_valido_200_com_cookie`, `SessaoTests.Me_200_com_matriculas` | afirmam `tipo` e `permissoes` no lugar de `papeis` | L2 | critério 14 |
| `ImplantacaoEquipeTests` (28 ocorrências), `ImplantacaoProntidaoTests`, `PlataformaTests`, `PlataformaAuditoriaTests`, `PlataformaComandoTests`, `InstituicaoComandoTests` | corpos e respostas de `/equipe` com `tipo`; `Papeis_invalidos_400` -> `Tipo_invalido_400`; `Desativar_protege_o_ultimo_administrador` -> `..._gestor`; criação da instituição semeia os tipos | L2 | critérios 41 a 46 |
| `MoodleLancamentoTests.Equipe_nao_entra_pelo_moodle(string papeis)`, `MoodleDadosTests` | os dados da teoria continuam a lista de papéis mapeada pela `Semente`; o líder entra como caso novo | L2 | critério 39 |
| `TempoRealEntradaTests.EntrarNaFila_recusa_quem_nao_e_atendente` | a mensagem e o status seguem; o usuário recusado é o de tipo sem `fila.atender` | L1 | door 5 |
| `MigracaoOperadorTests.Apaga_so_as_linhas_operador` | nenhuma asserção muda; ela migra até a última migration e agora passa também pela de tipos, e `usuario_papel` mantém as 3 linhas que afirma | L1 | critério 3 |
| `web/src/test/util.tsx` (`me()`), `web/src/api.ts` (tipo `Me`) | `me({ papeis })` mantém a assinatura e deriva `tipo` e `permissoes` da lista; o tipo `Me` troca `papeis` por `tipo` e `permissoes` | L3 | evita mexer em ~100 arquivos de teste do web |
| `web/src/acesso/acesso.test.tsx` (`grupos por papel`, `botões do cabeçalho por papel`, `papel do usuário por precedência`, mocks de `/api/auth/login`), `barra.test.tsx`, `textos.test.tsx`, `web/src/ux.test.tsx` (18 ocorrências) | os mocks de login e `me` trazem `tipo` e `permissoes`; o rótulo do papel de maior precedência vira `tipo.nome` | L3 | critérios 14, 47 |
| `web/src/administracao/cadastros/usuarios.test.tsx` (`editar troca papéis, setores e a senha temporária`, `o próprio administrador`, `ações de usuário no menu`, `mostra o erro da API`) | corpos `papeis` -> `tipoId`; as caixas de papel -> campo "Tipo"; as frases do `papel-do-proprio-gestor` -> as de C61 e C62 | L3 | critérios 36 e 53 |
| `web/src/plataforma/implantacao.test.tsx`, `menu.test.tsx` | membros e corpo de `/equipe` com `tipo` | L3 | critérios 42 a 45 |
| `web/e2e/casca.spec.ts` (38 ocorrências), `ux.spec.ts` (14), `identidade.spec.ts` (9), `simulada.ts` (5) e as demais specs que simulam `/api/auth/me` | a simulação de `me` ganha `tipo` e `permissoes` (função auxiliar única derivada da lista de papéis) | L3 | critério 14; as specs não rodam no `npm --prefix web test`, mas quebram o `e2e` se ficarem com `papeis` |

## Swept

- validation: C1, C41, C42, C44, C57, C58, C80, C81
- failure modes: C29, C78, C87
- idempotency: C43, C65, C85
- authorization: C6, C34, C47, C52, C55, C60, C61, C66
- concurrency: C54
- data lifecycle: C20, C48, C49, C50
- dependency failure: n/a - nenhuma chamada externa nova; o convite de equipe continua na fila de e-mail do AD-074 (C79 prova o e-mail gravado) e o provedor de IA e o push seguem falsos nos testes
- state transitions: C24, C45, C46, C59, C61
- observability: n/a - sem log ou métrica nova; a auditoria de quem muda tipos e funções está fora do escopo por decisão do dono (18/11/2026); a auditoria do operador mantém as mesmas 11 ações (C86)

## Out of scope

- Mais de um tipo por usuário - pedido do dono: um só
- Funções criadas pelos gestores - o catálogo de 12 é fechado no código (decisão do dono)
- Matriz módulo × ação e escopo por curso ou semestre (E5 completo) - os 12 pontos de decisão cabem em 12 funções
- Histórico e auditoria de quem deu ou tirou função e tipo - feature própria depois (decisão do dono, 18/11/2026)
- Apagar a tabela `usuario_papel` e a entidade `UsuarioPapel` - migration de limpeza futura, com backup (decisão do dono)
- Mudar a regra de liderança (AD-042) - só o líder passa a ser um tipo
- Usuário `ALUNO`+`ADMINISTRADOR` manter o assistente - vira `GESTOR`; o aviso da migration conta quantos (C26)
- Operador da plataforma convidar para tipo próprio - o convite fica em `GESTOR` e `ATENDENTE`
- Clonar tipo, ações em lote e filtros avançados - sem pedido do dono
- Teste de carga ou de desempenho da política - a função é lida do mesmo `Include` que os papéis usavam, sem consulta nova por requisição além do tipo e das funções

## Handoff

Tamanho, com a conta (soma dos bytes dos arquivos que cada lote lê ou edita, mais a estimativa do código e dos testes novos, dividida por 4; `Get-Item .Length`, o equivalente a `wc -c` no Windows):

- L1 = 38 arquivos de código existentes (97.561 B) + 13 arquivos de teste existentes (245.090 B) = 342.651 B; mais ~40 KB de código novo (`TipoDeUsuario`, `FuncaoDoTipo`, `Funcoes`, política central, `PonteDePapeis`, migration sem o `.Designer`) e ~70 KB de teste novo (`FuncoesDoTipoTests`, `TiposNoBancoTests`, `MigracaoTiposTests`, `AutorizacaoPorFuncaoTests`, `LiderDeSemestreTests`) = ~453 KB / 4 = ~113k. Tudo na API
- L2 = 22 arquivos de código existentes (51.501 B) + 17 de teste existentes (280.484 B) = 331.985 B; mais ~30 KB de código novo (6 arquivos de tipos em `Administracao/Usuarios`) e ~80 KB de teste novo (`TiposDeUsuarioTests`, `AtribuirTipoTests`) = ~442 KB / 4 = ~110k. Entra na API pelas rotas, com L1 já commitado
- L3 = 21 arquivos existentes (240.076 B, `web/src` e `web/e2e`) + ~55 KB novos (tela de tipos, `menuPorFuncao.test.tsx`, `tipos.test.tsx`, `tipos-de-usuario.spec.ts`) = ~295 KB / 4 = ~74k. Entra no `web/` pelo contrato que L2 deixou
- L1 + L2 + L3 = ~297k, acima do orçamento de 150k -> corte entre superfícies: nenhum lote sozinho passa de 150k, então nenhum lote foi cortado no meio de um resultado. O corte L1 | L2 é onde o contrato HTTP muda (L1 só decide por função e converte dados; as rotas de gravação seguem com `papeis` na entrada, via a ponte); o corte L2 | L3 é onde o `web/` começa

- Mechanism: handoff (owner asked for sub-agents on 18/11) - lotes sequenciais, cada um só começa com o anterior verde; o seguinte lê este `checks.md` e o diff do que entrou, nunca um resumo; o Verificador é despachado por quem tem a feature inteira depois do último commit do L3, sobre `<base>..HEAD` com os 103 checks
- Fecha o L1 quando: C1 a C37 verdes e `dotnet build Esclari.slnx` sem erro (CA1502 incluído); a suíte da API segue verde com as mudanças listadas em `## Existing assertions changed` para L1; `dotnet test Esclari.Api.Tests` inteiro como regressão, não como prova de check
- Fecha o L2 quando: C38 a C87 verdes, `dotnet build Esclari.slnx` sem erro, `PonteDePapeis.cs` e `Papeis.cs` apagados, `dotnet test Esclari.Api.Tests` inteiro verde
- Fecha o L3 quando: C88 a C103 verdes, `npm --prefix web test` verde, `npm --prefix web run build` sem erro, `crapper` abaixo de 10 e `dryer` sem candidato dentro de `web/` (AD-018); `mutator` suspenso (AD-025), registrar "suspenso por decisão do usuário"
- Commits coerentes por lote (`feat(tipos): ...`, em inglês, Conventional Commits); nada de `git push`

Cada construtor acrescenta três linhas ao terminar:

- **Boundary:** C1 a C37 fechados em `<sha>`
- **Settled mid-build:** <toda decisão que o dono ou o construtor anterior tomou e que não virou linha de Landing nem check editado>
- **Abandoned:** <o que foi tentado, descartado e por quê>

- **Boundary:** C1 a C37 fechados em `2efe3a7` (C20 com a ressalva abaixo)
- **Settled mid-build (L1):** (1) C20 espera SQLSTATE `23503`, mas a FK `ON DELETE RESTRICT` da door 1 devolve `23001` (`restrict_violation`) no PostgreSQL; o teste afirma `23001` e a door 7 do `plan.md` registra isso; o L2 (C30, C54) trata `23001`; o dono decide se prefere `NO ACTION` (daria `23503`). (2) `VerVersaoArtigo` e `BaixarVersaoDocumento` usam a função do **tipo** (`Usuario.TipoLibera`), sem exigir setor, para manter os 200 do atendente sem setor (door 8). (3) A ponte guarda `usuario_papel` e as respostas seguem com `papeis`, lidos de `usuario_papel`; `ConsultaDasInstituicoes` (SQL cru de prontidão e saúde) ainda lê `usuario_papel`: o L2 precisa trocá-lo por `tipo_usuario` antes de parar de gravar `usuario_papel`. (4) Também mudaram, sem constar da lista: `MapeamentoTests.Uma_configuracao_por_tipo_de_entidade` (37 para 39 entidades), o `Fatias_do_api` (arquivo raiz `PoliticaDeAcesso`), `Dados_sem_EfCore` não mudou; `STATE.md` (AD-076) foi reescrito sem "ONE"/"One" porque `NomeDoProdutoTests` falhava no HEAD.
- **Abandoned:** `PoliticaDeAcesso` e as consultas por `EsclariDb` dentro de `Dados/Usuarios`: `ArquiteturaTests.Dados_sem_EfCore` proíbe, foram para `Esclari.Api/PoliticaDeAcesso.cs`

- **Boundary:** C38 a C87 fechados em `6badda0`
- **Settled mid-build (L2):** (1) Fica no modelo a entidade `UsuarioPapel`, a tabela `usuario_papel` e a navegação `Usuario.Papeis` (só leitura, sem método que escreva): o snapshot não muda (C77) e os testes antigos que leem `u.Papeis` (`AvisarAtrasosTests`, `AvisarValidadeTests`, `LiderSemVazamentoTests`) seguem sem edição; a API não escreve nem lê `usuario_papel`. `Usuario.Criar` perdeu o parâmetro `papeis`. As constantes `Papeis.*` e o mapa papel -> tipo (`TipoPelosPapeis`) viraram helper de teste em `Esclari.Api.Tests/Infra/Papeis.cs`, e `Semente.Usuario` ainda grava `usuario_papel` por SQL só para esses testes. (2) "Só dá o que tem" (C23, C35, C47, C60) compara com as funções do **tipo** de quem chama (`FuncoesDoTipo()`, a mesma base da door 8), não com as efetivas: um tipo com `fila.atender` e sem setor continua podendo dar `fila.atender`. No `PUT` de tipo a regra vale para o conjunto inteiro enviado, não só para o que foi acrescentado. (3) `ConsultaDasInstituicoes` e `DetalheDaInstituicao` (SQL cru) leem `tipo_usuario`/`tipo_usuario_funcao`: aluno ativo = tipo `ALUNO` ou com `assistente.usar`; equipe = `GESTOR`, `ATENDENTE` ou função gravada diferente de `assistente.usar`; administrador = `GESTOR`; "atendente no setor" = `GESTOR` ou `fila.atender` gravada, com vínculo em `atendente_setor`. (4) `TipoResposta` virou o 10º record `Shared` da fatia (usos: `ListarTipos`, `CriarTipo`, `AlterarTipo`): `ArquiteturaTests.OsNoveShared` virou `OsDezShared`, e a lista da `ListarTipos` usa o wrapper `Lista` (não `Resposta`). `CriarTipo`, `AlterarTipo` e `CriarUsuario` levam `IReadOnlyList<string> FuncoesDoAdmin` na mensagem, não `Usuario`, para não entrar na lista de `Mensagem_sem_entidade`. (5) Também mudaram, sem constar da lista: contagens de `ArquiteturaTests` (115 mensagens, 120 `Map*`, 46 entradas e corpos, `Administracao` 32, 95 respostas JSON, 46 em `doShared`, mapa de `Administracao` com 47 arquivos), `MediatorTests` (115; 105 não-ação), `SessaoTests.Sem_cookie_401_em_toda_rota_privada` (107), `RetratoTests` (123 rotas, 119 de método único, 117 com `Authorize`, 47 rotas `/api/admin`), `AdminTests.Sem_papel_admin_403` (47), `Retratos/rotas.txt` (+4 linhas de `/api/admin/tipos`) e `Retratos/sucessos.txt` (6 linhas: `papeis` -> `tipo` e `permissoes` em login, `me` e `/api/admin/usuarios`); entrada dos corpos que mandavam `papeis` em `RetratoTests.Sucessos`, `SenhaTests.Limites_7_8_128_129` e `IsolamentoTests` (agora `tipoId`); `Autorizacao_so_pela_politica` perdeu a exceção da ponte. O e-mail de `Criacao` e de `ReenviarAcesso` mantém "administrador" (`MarcaTests.Emails_citam_Esclari_e_remetente_padrao` afirma esse texto); só o convite de equipe (`CriarMembro`, `ConvidarMembro`) diz "gestor", "atendente" ou o nome do tipo em minúsculas. (6) `POST .../equipe` responde `{ id, tipo: { id, nome, sistema } }`; `GET .../equipe` traz o mesmo `tipo` em cada membro. (7) A corrida de C54 termina em `DELETE` 204 + `PUT` 400 em `tipoId` (FK `23503` ou consulta prévia) ou `PUT` 200 + `DELETE` 409 (FK `23001` ou contagem), as duas recusas tratadas.
- **Abandoned:** apagar a navegação `Usuario.Papeis` e a entidade: quebraria o snapshot (C77) ou obrigaria a editar testes fora da lista; passar `Usuario` nas mensagens de tipo: a lista de `Mensagem_sem_entidade` é fechada; devolver `ListarTipos` como objeto anônimo na agregação de contagem: `Objetos_anonimos_so_fora_das_respostas` conta.