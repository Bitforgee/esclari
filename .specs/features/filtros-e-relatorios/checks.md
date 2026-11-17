# Filtros na lista de chamados e relatório em CSV - checks

Profile: ui
Plan: `.specs/features/filtros-e-relatorios/plan.md`

72 checks em 7 grupos (6 slices e a emenda E4) · 4 one-way doors · 0 perguntas abertas (plano aprovado pelo dono em 17/11: "aprovo pode seguir"; AD-066)

**Como as provas rodam.** Na raiz `D:\esclari`, salvo `cd web &&`. API: `docker compose up -d postgres mailpit minio` e uma execução de `dotnet test` por vez. Vitest: `cd web && npx vitest run <arquivo> -t "<nome>"`. Playwright: `docker compose up -d --build` e `cd web && npx playwright test <spec> -g "<nome>"`. Um filtro que não seleciona teste sai com 0 (L-002): conferir a contagem de testes executados. Mutação suspensa (AD-025).

**Emenda de construção E1 (Impact, porta 4).** `TempoRealEntradaTests.EntrarNoChamado_recusa_com_Chamado_nao_encontrado` tinha o caso `(admin, numero)`, que esperava a recusa do administrador ao entrar no grupo do chamado no hub. Com o papel `LEITURA` o administrador lê o chamado e entra no grupo, como o Impact previu para quem ramifica no papel (`TempoRealHub.EntrarNoChamado`). O caso saiu da lista de recusas e virou o teste `EntrarNoChamado_entra_o_administrador_em_leitura`, que afirma a entrada; os outros 5 casos de recusa ficam. As contagens de inventário de `ArquiteturaTests`, `MediatorTests`, `RetratoTests` (e `Retratos/rotas.txt`) e `SessaoTests` sobem com as 3 rotas novas (C59, C60).

**Emenda de construção E2 (C61, só a prova).** A prova escrita (`npm run coverage && crapper`, `dryer`) mede a pasta `web/` inteira, inclusive `e2e/` e `apresentacao/`, e não o escopo que a afirmação nomeia. As provas passam a ser as das features anteriores: `cd web && npm run coverage && crapper --source-root src --use-existing-coverage --threshold 10` (exit 0, maior nota 9,8 em `Sidebar`, código de `ui/` anterior) e o `dryer` sobre as 6 fatias e os arquivos da raiz de `web/src` com `--threshold 0.8`, que imprime "No duplicate candidates found." (o mesmo escopo do E3 de `interface-marca-esclari`). A afirmação não muda.

**Emenda de construção E3 (C62, pede o "ok" do dono).** Na base `7005230`, com a imagem da base no compose, já falham 15 testes e2e: `casca.spec.ts` "foco sem auréola no botão e no campo", "barras do ranking na cor calma e sem Recharts", "categoria longa inteira nas 5 larguras", "total do ranking em uma linha", "barra do ranking embaixo do rótulo no estreito"; `identidade.spec.ts` "serifa só em títulos de 20 px ou mais", "Indicador principal em serifa de 40 px com a variação ao lado", "3 destaques em faixa de 3 colunas sem caixa", "9 métricas em lista de rótulo e valor sem caixa", "gráficos sem cartão com o título", "variação abaixo do número a 360"; `arranjo.spec.ts` "arranjo Indicadores", "raiz com a marca e o texto centralizados", "quebras medidas dos dois lados", "valores de tipo e bordas". Todos são da tela Indicadores ou da Raiz, que esta feature não toca. O C62 passa a afirmar: os 5 specs passam no HEAD, exceto esses 15, que falham igual na base, e nenhum outro falha.

## Checks

### S1 - Busca e filtros na API · 8 arquivos · ~30k

**C1** - Sem filtro, `GET /api/chamados/busca?escopo=fila` de um atendente devolve os mesmos números, na mesma ordem, que `GET /api/chamados?escopo=fila`: os chamados dos setores dele em `ABERTO`, `EM_ATENDIMENTO`, `AGUARDANDO_ALUNO` e `RESOLVIDO`, do mais antigo ao mais novo, sem o `FECHADO` e sem o de outro setor (AC 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Sem_filtro_igual_a_fila"`

**C2** - Com `estado=<E>` vem só o chamado no estado `E`, para cada um dos 5 estados, e com `estado=ABERTO&estado=FECHADO` vêm os dois (AC 2)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Estado_filtra_cada_um"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Estado_repetido_soma"`

**C3** - `q=matricula` acha o chamado de título "Trancar a Matrícula" e `q=MATRÍCULA` também; o de título "Boleto" não vem (AC 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Busca_no_titulo_sem_acento_e_sem_caixa"`

**C4** - `q=` com os 3 últimos dígitos do número de um chamado (por exemplo `123` para `ESC-000123`) e `q=esc-000` acham esse chamado pelo número (AC 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Busca_no_numero"`

**C5** - `q=` com parte do nome do solicitante, sem acento, acha os chamados dele e não os de outro aluno (AC 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Busca_no_nome_do_solicitante"`

**C6** - `q=%` e `q=_` devolvem `total` 0 quando nenhum título tem esses caracteres, e `q=50%` acha só o título "Desconto de 50% na mensalidade" (AC 4)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Curinga_e_texto_comum"`

**C7** - `categoriaId=<id>` devolve só os chamados com essa categoria atual; um chamado reclassificado sai da categoria original e entra na nova (AC 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Filtra_por_categoria_atual"`

**C8** - `setorId=<id>` de um atendente com 2 setores devolve só os chamados desse setor (AC 6)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Filtra_por_setor"`

**C9** - Com `escopo=fila` e `setorId` de um setor que não é do atendente, `busca` e `exportar` respondem `403` com `detail` "Este setor não é seu." (AC 7)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Setor_alheio_403"`

**C10** - `responsavel=eu` devolve só os chamados com o usuário como responsável e `responsavel=ninguem` só os sem responsável (AC 8)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Responsavel_eu_e_ninguem"`

**C11** - Um chamado criado às 23:30 de 10/11/2026 em `America/Cuiaba` (03:30Z de 11/11) vem com `de=ate=2026-11-10` e não vem com `de=ate=2026-11-11` (AC 9)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Periodo_no_fuso_da_instituicao"`

**C12** - `de=2026-11-12&ate=2026-11-11` responde `400` com o campo `de` em `errors` (AC 10)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.De_depois_de_ate_400"`

**C13** - `ordem=recentes` devolve do mais novo ao mais antigo, e `ordem=antigos` igual à ausência de `ordem` (AC 11)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Ordem_recentes_e_antigos"`

**C14** - Com 51 chamados: sem `pagina` vem `pagina` 1 e 50 itens; `pagina=2` traz 1 item; `pagina=3` traz `itens` vazio; `total` é 51 e `porPagina` é 50 nas três (AC 12)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Pagina_de_50_com_total"`

**C15** - A resposta tem exatamente as chaves `itens`, `total`, `pagina`, `porPagina`, e cada item exatamente `numero`, `titulo`, `estado`, `setor`, `categoria`, `responsavel`, `criadoEm`, `dias`, com os valores do chamado (porta 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Forma_da_resposta"`

**C16** - Respondem `400` com o nome do campo em `errors`: `estado=PERDIDO`, `ordem=meio`, `responsavel=fulano`, `escopo=meus`, `pagina=0` e `q` de 101 caracteres; `q` de 100 caracteres mais espaços nas pontas responde `200` (AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Parametro_invalido_400"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Busca_de_100_caracteres_aparada_200"`

**C17** - Um aluno sem `ATENDENTE` e sem `ADMINISTRADOR` recebe `403` em `busca`, `exportar` e `filtros` (AC 14)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Aluno_403"`

**C18** - Um atendente sem `ADMINISTRADOR` recebe `403` com `escopo=todos` em `busca`, `exportar` e `filtros` (AC 15)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Todos_so_para_administrador"`

**C19** - Sem sessão, `busca`, `exportar` e `filtros` respondem `401` (Surface)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Sem_sessao_401"`

**C20** - Um chamado de outra instituição, com o mesmo nome de setor e o mesmo título, nunca aparece em `busca` nem em `exportar` de um administrador com `escopo=todos`, e o setor dela não aparece em `filtros` (AC 16)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Outra_instituicao_nunca_aparece"`

**C21** - `GET /api/chamados/filtros?escopo=fila` de um atendente devolve `setores` só com os setores dele e `categorias` com as categorias ativas da instituição (sem a inativa), cada uma `{ id, nome }`, em ordem de nome (AC 17)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Filtros_do_atendente"`

**C22** - A extensão `unaccent` está instalada no banco depois das migrations, e o modelo não tem mudança pendente (porta 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Extensao_unaccent_instalada"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArquiteturaTests.Modelo_sem_mudanca_pendente"`

### S2 - Filtros na tela Fila · 6 arquivos · ~35k

**C23** - Na Fila, antes da tabela, há o campo de busca com rótulo "Buscar por número, título ou aluno", o botão "Filtros" e o botão "Exportar CSV" (AC 18)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "barra da fila"`

**C24** - Digitar "boleto" não pede nada em 299 ms, pede `/api/chamados/busca?escopo=fila&q=boleto` aos 300 ms (uma chamada só) e a URL da tela passa a ter `q=boleto` (AC 19)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "busca 300 ms depois"`

**C25** - O painel "Filtros" mostra 5 caixas de Estado ("Aberto", "Em atendimento", "Aguardando aluno", "Resolvido", "Fechado"), Setor, Categoria, Responsável ("Qualquer", "Eu", "Sem responsável"), Período de abertura e Ordem ("Mais antigos primeiro", "Mais recentes primeiro"), e marcar "Fechado" não faz nenhuma busca nova até tocar "Aplicar" (AC 20)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "painel aplica em lote"`

**C26** - Ao tocar "Aplicar" com Fechado, um setor, uma categoria, "Eu", um período e "Mais recentes primeiro", o painel fecha, a URL fica com `estado=FECHADO`, `setor`, `categoria`, `responsavel=eu`, `de`, `ate` e `ordem=recentes`, e a busca pede os mesmos 7 parâmetros à API (AC 21)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "aplicar grava na url"`

**C27** - Abrir `/univag/fila?q=boleto&estado=FECHADO&responsavel=ninguem&ordem=recentes` pede a busca com esses 4 filtros, mostra "boleto" no campo e, no painel, "Fechado" marcado, "Sem responsável" e "Mais recentes primeiro" escolhidos (AC 22)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "url inicial preenche"`

**C28** - Com `estado=FECHADO` e `responsavel=eu` ativos aparecem as etiquetas "Estado: Fechado" e "Responsável: Eu", o botão "Filtros (2)", "Limpar filtros", e "Remover filtro Estado" tira só o estado da URL e da busca; "Limpar filtros" tira todos (AC 23)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "etiquetas dos filtros"`

**C29** - Com `total` 73 aparece "73 chamados", com 1 "1 chamado" e com 0 "0 chamados" (AC 24)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "contagem do total"`

**C30** - Com filtro ativo e `total` 0 aparece "Nenhum chamado com esses filtros." com o botão "Limpar filtros" na linha vazia; sem filtro e `total` 0 continua "Nenhum chamado na fila dos seus setores." (AC 25)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "vazio com e sem filtro"`

**C31** - Com `total` 60, "Mostrar mais 50" pede `pagina=2` e a tabela passa a ter 60 linhas; se a página 2 repete um número da página 1, ele aparece uma vez só (AC 26)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "mostrar mais pede a pagina seguinte"`

**C32** - Com a Fila aberta em `?estado=FECHADO`, um `FilaMudou` do hub faz uma nova chamada a `/api/chamados/busca?escopo=fila&estado=FECHADO` (AC 27)
Proof: `cd web && npx vitest run src/chamados/tempoReal.test.tsx -t "FilaMudou refaz a busca filtrada"`

**C33** - Com a busca respondendo 500, aparece "Não foi possível carregar a fila. Verifique sua conexão." e "Tentar de novo" pede a busca de novo com os mesmos filtros (AC 28)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "erro com tentar de novo"`

**C34** - Enquanto a busca com um filtro novo não responde, as linhas da busca anterior continuam na tabela e a tabela tem `aria-busy="true"`; quando responde, `aria-busy` sai; na primeira carga continuam os 3 esqueletos de hoje (AC 29, Observable carregando)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "mantem a lista enquanto carrega"`
Proof: `cd web && npx vitest run src/chamados/listas.test.tsx -t "lista carregando mostra 3 esqueletos e nada mais"`

### S3 - Exportar CSV · 6 arquivos · ~30k

**C35** - `GET /api/chamados/exportar?escopo=fila` responde `200` com `Content-Type` `text/csv; charset=utf-8` e `Content-Disposition` `attachment` com `filename="chamados-<slug>-2026-11-10.csv"` quando o relógio marca 2026-11-11T03:30Z (ainda 10/11 em Cuiabá) (AC 30)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ExportarChamadosTests.Cabecalhos_do_arquivo"`

**C36** - O corpo começa com os bytes `EF BB BF`, a primeira linha é `Número;Título;Estado;Setor;Categoria;Categoria original;Responsável;Curso;Semestre;Aberto em;Primeira resposta em;Resolvido em;Fechado em;Motivo de fechamento;Reaberturas;Encaminhamentos`, as linhas terminam em CRLF e com 55 chamados há 55 linhas de dados, na ordem de `ordem=recentes` (porta 2, AC 31)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ExportarChamadosTests.Formato_do_arquivo"`

**C37** - Uma linha de chamado fechado traz o estado "Fechado", o motivo com o rótulo em português, "Aberto em" como `dd/MM/aaaa HH:mm` no fuso de Cuiabá, setor, categoria, categoria original, responsável, curso, semestre, reaberturas e encaminhamentos com os valores do chamado; um chamado aberto traz "Resolvido em", "Fechado em" e "Motivo de fechamento" vazios (AC 32)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ExportarChamadosTests.Valores_de_cada_coluna"`

**C38** - "Primeira resposta em" é a data da primeira mensagem pública de um atendente, não a nota interna anterior a ela nem a mensagem do aluno; sem resposta da equipe fica vazia (AC 33)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ExportarChamadosTests.Primeira_resposta_da_equipe"`

**C39** - O arquivo de um chamado com nota interna e mensagens não contém o CPF, o número de matrícula, o nome nem o e-mail do aluno, nem o texto de nenhuma mensagem ou nota (AC 34)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ExportarChamadosTests.Sem_dado_pessoal_do_aluno"`

**C40** - Uma célula com `;`, com `"`, com CR ou com LF sai entre aspas com a `"` dobrada; uma célula sem nenhum deles sai sem aspas (porta 2, AC 35)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CsvDeChamadosTests.Aspas_quando_precisa"`

**C41** - Uma célula que começa com `=`, `+`, `-`, `@`, TAB ou CR sai com `'` na frente, e o título "=HYPERLINK(1)" de um chamado sai como `'=HYPERLINK(1)` no arquivo exportado (porta 2, AC 36)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~CsvDeChamadosTests.Prefixo_contra_formula"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ExportarChamadosTests.Titulo_com_formula_sai_como_texto"`

**C42** - Com 10 000 chamados que casam a exportação responde `200` com 10 000 linhas de dados; com 10 001 responde `400` com `detail` "A exportação passa de 10.000 chamados. Refine os filtros." (AC 37)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ExportarChamadosTests.Limite_de_10000"`

**C43** - Um líder de turma cujos setores são todos de liderança recebe `403` com `detail` "A exportação não está disponível para a liderança de turma.", e a `busca` dele responde `200` (AC 38)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ExportarChamadosTests.Lideranca_nao_exporta"`

**C44** - Com os limites reais, o mesmo usuário faz 5 exportações `200` e a sexta, no mesmo minuto, responde `429` (AC 39)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~RateLimitTests.Seis_exportacoes_em_60s_429"`

**C45** - Uma exportação com `q=boleto&estado=FECHADO` grava uma linha `Information` com o modelo `Exportação de chamados: usuário {UsuarioId}, instituição {InstituicaoId}, {Linhas} linhas, filtros {Filtros}`, os ids certos, o número de linhas e `Filtros` com `q` e `estado`, e o texto "boleto" não aparece no log (AC 40)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ExportarChamadosTests.Log_da_exportacao_sem_o_texto"`

**C46** - Na Fila com `?estado=FECHADO`, tocar "Exportar CSV" pede `/api/chamados/exportar?escopo=fila&estado=FECHADO`, o botão fica desabilitado com um indicador de progresso até a resposta e baixa um arquivo com o nome do `Content-Disposition`; com a API respondendo `400` aparece um aviso com o `detail` (AC 41)
Proof: `cd web && npx vitest run src/chamados/exportar.test.tsx -t "exporta com os filtros da tela"`
Proof: `cd web && npx vitest run src/chamados/exportar.test.tsx -t "recusa vira aviso"`

### S4 - Chamados do administrador · 7 arquivos · ~30k

**C47** - Um `ADMINISTRADOR` sem `ATENDENTE` com `escopo=todos` recebe em `busca` e em `exportar` os chamados de 3 setores diferentes, e com `escopo=fila` recebe `total` 0 (AC 42)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LeituraDoAdministradorTests.Todos_os_setores"`

**C48** - `GET /api/chamados/filtros?escopo=todos` de um administrador devolve todos os setores da instituição (AC 43)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LeituraDoAdministradorTests.Filtros_com_todos_os_setores"`

**C49** - Um administrador que não é atendente do setor nem solicitante abre `GET /api/chamados/{numero}` e recebe `200` com a nota interna em `mensagens`, as fontes na transcrição, os `eventos` e `acoes` vazio (porta 4, AC 44)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LeituraDoAdministradorTests.Le_o_detalhe_sem_acoes"`

**C50** - Esse administrador recebe `403` com "Seu papel neste chamado não permite esta ação." em cada uma das 10 rotas `POST` de `VisibilidadeTests.RotasDoChamado`, e o chamado não muda de estado, responsável, setor, categoria nem número de eventos (porta 4, AC 45)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LeituraDoAdministradorTests.Nenhuma_acao_em_leitura"`

**C51** - O menu de um `ADMINISTRADOR` tem "Chamados" no grupo Atendimento com link para `/univag/chamados`, e o de um `ATENDENTE` sem `ADMINISTRADOR` não tem (AC 46)
Proof: `cd web && npx vitest run src/chamados/chamadosAdmin.test.tsx -t "menu do administrador"`

**C52** - `/univag/chamados` pede `/api/chamados/busca?escopo=todos` e `/api/chamados/filtros?escopo=todos`, tem o título "Chamados", as colunas "Número", "Título", "Setor", "Categoria", "Responsável", "Aberto", "Estado", e sem filtro e `total` 0 mostra "Nenhum chamado ainda." (AC 47)
Proof: `cd web && npx vitest run src/chamados/chamadosAdmin.test.tsx -t "tela chamados"`

**C53** - Um chamado com `acoes` vazio, aberto pela equipe, não mostra o campo de resposta, nem "Enviar", nem botão de ação (Assumir, Resolver, Encaminhar, Mais ações) (AC 48)
Proof: `cd web && npx vitest run src/chamados/chamadosAdmin.test.tsx -t "detalhe em leitura"`

### S5 - Celular e toque · 3 arquivos · ~15k

**C54** - Em 360×780 o campo de busca ocupa a largura do conteúdo, "Filtros" e "Exportar CSV" ficam numa linha abaixo dele, e `document.documentElement.scrollWidth` não passa de 360 (AC 49)
Proof: `cd web && npx playwright test e2e/filtros.spec.ts -g "360 busca na largura toda"`

**C55** - Em 360×780 o painel "Filtros" abre colado na borda de baixo, "Limpar" e "Aplicar" ficam visíveis no rodapé com o conteúdo rolado até o fim e até o começo, e marcar "Fechado" e aplicar mostra a lista filtrada (AC 50)
Proof: `cd web && npx playwright test e2e/filtros.spec.ts -g "360 painel de baixo com rodape fixo"`

**C56** - Em 844×390 o painel "Filtros" rola por dentro e "Aplicar" fica dentro da janela em toda a rolagem (AC 50, Observable deitado)
Proof: `cd web && npx playwright test e2e/filtros.spec.ts -g "deitado o painel rola"`

**C57** - O campo de busca tem `type="search"` e `enterkeyhint="search"`; Enter busca na hora (sem esperar 300 ms) e tira o foco do campo (AC 51)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "teclado busca na hora"`

**C58** - Em 360×780, campo, "Filtros", "Exportar CSV", cada rótulo de caixa de Estado, os selects, os botões "Remover filtro …", "Limpar filtros", "Limpar" e "Aplicar" têm pelo menos 44 px de altura (AC 52)
Proof: `cd web && npx playwright test e2e/filtros.spec.ts -g "toque de 44 px"`

### S6 - Portões do repositório · 0 arquivos novos · ~5k

**C59** - `dotnet build Esclari.slnx` sai com 0 (CA1502 abaixo de 11) e `ArquiteturaTests` passa inteiro com as contagens e os mapas atualizados para os 3 casos de uso novos (AD-018, AD-054, AD-056)
Proof: `dotnet build Esclari.slnx`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ArquiteturaTests"`

**C60** - A suíte da API e a do web passam inteiras, e `npm --prefix web run build` sai com 0; o `listar_chamados` do MCP e `GET /api/chamados?escopo=fila|meus` continuam com o JSON da base (`McpFerramentasTests`, `VisibilidadeTests`) (Impact)
Proof: `dotnet test Esclari.Api.Tests`
Proof: `npm --prefix web test`
Proof: `npm --prefix web run build`

**C61** - `crapper` não acha função com CRAP 10 ou mais em `web/src` e `dryer` não acha candidato a 0.8 (AD-018; `mutator` suspenso pelo AD-025)
Proof: `cd web && npm run coverage && crapper`
Proof: `cd web && dryer`

**C62** - Os e2e que leem a fila passam com o servidor do compose: `ux.spec.ts`, `casca.spec.ts`, `identidade.spec.ts`, `filtros.spec.ts`, e `arranjo.spec.ts` com as mesmas falhas da base `7005230` e nenhuma a mais (Impact)
Proof: `cd web && npx playwright test e2e/ux.spec.ts e2e/casca.spec.ts e2e/identidade.spec.ts e2e/filtros.spec.ts`
Proof: `cd web && npx playwright test e2e/arranjo.spec.ts`

### E4 - Emenda depois da rodada 1 do Verify · 6 arquivos · ~10k

A rodada 1 (`verification.md`, `3f7a342`) não achou defeito e marcou FAIL por cobertura: arranjos das Telas do plano e membros de conjuntos sem prova. Estes checks são aditivos; nenhum check anterior muda. O C57 passou a usar a tecla Enter de verdade (`userEvent.type(..., '{Enter}')`) e espera a busca em menos de 200 ms, antes dos 300 ms do atraso.

**C63** - `q=\` devolve `total` 0 quando nenhum título tem `\`, e depois de abrir "Arquivo em C:\temp\notas" `q=C:\temp` e `q=\` devolvem só esse chamado (AC 4, porta 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Barra_invertida_e_texto_comum"`

**C64** - `GET /api/chamados/exportar` responde `400` com o nome do campo em `errors` para `estado=PERDIDO`, `ordem=meio`, `responsavel=fulano`, `escopo=meus`, `de=2026-11-12&ate=2026-11-11` (campo `de`) e `q` de 101 caracteres (AC 10, AC 13)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeChamadosTests.Parametro_invalido_400_na_exportacao"`

**C65** - No CSV a coluna Estado traz "Aberto", "Em atendimento", "Aguardando aluno", "Resolvido" e "Fechado", e "Motivo de fechamento" traz "Confirmado pelo aluno", "Cancelado pelo aluno", "Duplicado" e "Fechado automaticamente" (AC 32)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~ExportarChamadosTests.Rotulos_de_estado_e_motivo"`

**C66** - Um administrador em `LEITURA` baixa o anexo de um chamado da instituição dele com `200` e os mesmos bytes; o administrador de outra instituição recebe `404` (porta 4, Impact `BaixarAnexo`)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~LeituraDoAdministradorTests.Baixa_o_anexo_em_leitura"`

**C67** - Na Fila a ordem é: a barra (busca, "Filtros", "Exportar CSV"), as etiquetas com "Limpar filtros", a contagem "1 chamado · Mais antigos primeiro" e a tabela; em 1280×800 o campo e os dois botões ficam numa linha só (Telas 1, AC 18, AC 23, AC 24)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "ordem da tela"`
Proof: `cd web && npx playwright test e2e/filtros.spec.ts -g "desktop painel à direita e barra numa linha"`

**C68** - Com `ordem=recentes` a contagem diz "2 chamados · Mais recentes primeiro" (Telas 1, AC 24)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "contagem diz a ordem"`

**C69** - Os campos do painel vêm na ordem Estado, Setor, Categoria, Responsável, Período de abertura, Ordem (Telas 3, AC 20)
Proof: `cd web && npx vitest run src/chamados/filtros.test.tsx -t "ordem dos campos do painel"`

**C70** - Em 1280×800 o painel "Filtros" abre colado na borda direita (borda direita em 1280, começa depois de 640), com altura 800 a partir do topo (Telas 3)
Proof: `cd web && npx playwright test e2e/filtros.spec.ts -g "desktop painel à direita e barra numa linha"`

**C71** - A tela Chamados tem o campo de busca, "Filtros" e "Exportar CSV", e o painel lista no Setor "Todos" e os setores de `filtros?escopo=todos` ("Financeiro", "Secretaria"), com as 5 caixas de Estado (Telas 2, AC 47)
Proof: `cd web && npx vitest run src/chamados/chamadosAdmin.test.tsx -t "tela chamados com a barra e o painel da fila"`

**C72** - Um administrador em `LEITURA` entra no grupo do chamado no hub e recebe o aviso enviado a esse grupo (porta 4, E1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TempoRealEntradaTests.EntrarNoChamado_entra_o_administrador_em_leitura"`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~TempoRealEntradaTests.EntrarNoChamado_recusa_com_Chamado_nao_encontrado"`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| `GET /api/chamados/busca` statuses (4) | 200 C1 · 400 C12, C16 · 401 C19 · 403 C9, C17, C18 | - |
| `GET /api/chamados/exportar` statuses (5) | 200 C35 · 400 C42 · 401 C19 · 403 C9, C17, C18, C43 · 429 C44 | - |
| `GET /api/chamados/filtros` statuses (3) | 200 C21 · 401 C19 · 403 C17, C18 | - |
| `GET /api/chamados/{numero}` status mudado (1) | 200 para administrador fora do setor C49 | - |
| filtros da API (8) | `q` C3 · `estado` C2 · `setorId` C8 · `categoriaId` C7 · `responsavel` C10 · `de`/`ate` C11 · `ordem` C13 · `pagina` C14 | - |
| campos onde `q` procura (3) | título C3 · número C4 · nome do solicitante C5 | - |
| valores de `estado` (5) | `ABERTO` C2 · `EM_ATENDIMENTO` C2 · `AGUARDANDO_ALUNO` C2 · `RESOLVIDO` C2 · `FECHADO` C2 | - |
| valores de `responsavel` (3) | ausente C1 · `eu` C10 · `ninguem` C10 | - |
| valores de `ordem` (3) | ausente C1 · `antigos` C13 · `recentes` C13 | - |
| parâmetros recusados (6) | `estado` C16 · `ordem` C16 · `responsavel` C16 · `escopo` C16 · `pagina` C16 · `q` C16 | - |
| escopo × papel (6) | aluno `fila` C17 · aluno `todos` C17 · atendente `fila` C1 · atendente `todos` C18 · administrador `todos` C47 · administrador sem setor `fila` C47 | - |
| caracteres curinga (2) | `%` C6 · `_` C6 | - |
| porta 1, chaves da resposta (12) | C15, table-driven over all 12 (4 do envelope e 8 do item) | - |
| porta 2, colunas do CSV (16) | `Número` C36, C37 · `Título` C36, C41 · `Estado` C37 · `Setor` C37 · `Categoria` C37 · `Categoria original` C37 · `Responsável` C37 · `Curso` C37 · `Semestre` C37 · `Aberto em` C37 · `Primeira resposta em` C38 · `Resolvido em` C37 · `Fechado em` C37 · `Motivo de fechamento` C37 · `Reaberturas` C37 · `Encaminhamentos` C37 | - |
| porta 2, caracteres que pedem aspas (4) | `;` C40 · `"` C40 · CR C40 · LF C40 | - |
| porta 2, caracteres de fórmula (6) | `=` C41 · `+` C41 · `-` C41 · `@` C41 · TAB C41 · CR C41 | - |
| porta 2, forma do arquivo (4) | BOM C36 · `;` C36 · CRLF C36 · nome do arquivo C35 | - |
| dado pessoal fora do CSV (5) | CPF C39 · matrícula C39 · nome C39 · e-mail C39 · texto de mensagem e nota C39 | - |
| porta 3, extensão (1) | `unaccent` C22, C3 | - |
| porta 4, papel `LEITURA` (2) | lê C49 · não age C50 | - |
| campos do painel (7) | Estado C25, C26 · Setor C26 · Categoria C26 · Responsável C26, C27 · Período C26 · Ordem C26, C27 · busca `q` C24, C27 | - |
| estados da tela Fila/Chamados (6) | carregando C34 · recarregando C34 · erro C33 · vazio sem filtro C30, C52 · vazio com filtro C30 · com itens C29, C31 | - |
| linhas do celular (4) | 360×780 C54, C55 · deitado 844×390 C56 · teclado C57 · toque C58 | - |
| telas do plano (3) | Fila: ordem C67, C68 · Chamados: barra e painel C71, C52 · painel: lado C55, C70, campos C69 | - |
| rótulos de estado no CSV (5) | `ABERTO` C65 · `EM_ATENDIMENTO` C65 · `AGUARDANDO_ALUNO` C65 · `RESOLVIDO` C65 · `FECHADO` C65 | - |
| rótulos de motivo no CSV (4) | `CONFIRMADO` C65 · `CANCELADO` C65 · `DUPLICADO` C65 · `FECHADO_AUTOMATICAMENTE` C65 | - |
| caracteres escapados na busca (3) | `%` C6 · `_` C6 · `\` C63 | - |
| parâmetros recusados na exportação (6) | `estado` C64 · `ordem` C64 · `responsavel` C64 · `escopo` C64 · `de` > `ate` C64 · `q` C64 | - |
| quem ramifica no papel `LEITURA` (5) | `DetalheChamado.Montar` C49 · `PermissoesChamado` C50 · `AgirNoChamado` C50 · `TempoRealHub.EntrarNoChamado` C72 · `BaixarAnexo` C66 | - |
| configuração de partida: política de limite `exportar` (1 montagem) | `Program` usado pelo `EsclariFactoryComLimites` C44 | - |

- Afirmações de status, rota ou forma de resposta: C1, C9, C12, C14 a C22, C35, C42 a C44, C47 a C50 - cada uma tem prova que bate na rota HTTP.
- Test policy: o `AGENTS.md` (seção Testes) já responde o nível - rota HTTP no teste de integração, regra de domínio em teste unitário na entidade - e esta feature segue isso; sem seção própria.
- C40 e a primeira prova de C41 são unitárias sobre a função de escrita da célula; a segunda prova de C41 cruza a rota.

## Swept

- validation: C12, C16, C6 (curinga escapado), C42 (limite de linhas)
- failure modes: C33 (busca falha na tela), C46 (exportação recusada vira aviso), C42 (recusa antes de gerar arquivo, nada parcial)
- idempotency: n/a - as três rotas só leem; repetir dá o mesmo resultado e não grava nada
- authorization: C9, C17, C18, C19, C43, C50, C51
- concurrency: C31 (páginas que mudam entre duas leituras não repetem número na tela); n/a para escrita - nada é gravado
- data lifecycle: n/a - nenhum dado novo é guardado; o CSV não fica no servidor nem no aparelho
- dependency failure: n/a - só o Postgres, que já derruba a rota inteira com 500 como em toda leitura; o erro na tela é C33
- state transitions: C50 (o papel `LEITURA` não muda nenhum estado)
- observability: C45

## Handoff

- Tamanho: arquivos existentes que o build lê ou muda somam 240 740 bytes (`ChamadosExtensoes`, `ChamadoDoUsuario`, `PermissoesChamado`, `DetalheChamado`, `ListarChamados`, `Seguranca`, `EsclariDb`, `ArquiteturaTests` 85 779, `RateLimitTests`, `AgendadorLogsTests`, `Chamados.tsx`, `Autenticado.tsx`, `App.tsx`, `tempo-real.ts`, `Tabela.tsx`, `test/util.tsx`, `ChamadoDetalhe.tsx`, `e2e/simulada.ts`, `listas.test.tsx`, `tempoReal.test.tsx`, `tabela.test.tsx`, `ux.test.tsx`) = ~60k; código e testes novos estimados em ~125 000 bytes = ~31k; os ~12 testes web que só trocam a URL da fila, ~10k. Total ~101k, abaixo do orçamento de 150k - um construtor.
- Mechanism: one builder (cabe no orçamento; sem pergunta).

- **Boundary:** C1-C62 construídos em `9ea3d2d..HEAD` (API em `bc799c6`; web no commit seguinte). Provas do autor: `dotnet test Esclari.Api.Tests` 1069/1069; Vitest 1095/1095; `npm --prefix web run build` e `dotnet build Esclari.slnx` exit 0; `crapper --source-root src --use-existing-coverage --threshold 10` exit 0 (máx. 9,8); `dryer` sem candidato; e2e `filtros.spec.ts` 4/4, `ux.spec.ts` inteiro verde, `casca`/`identidade`/`arranjo` com as 15 falhas da base e nenhuma a mais.
- **Settled mid-build:** nenhuma pergunta ao dono; E1-E3 acima.
- **Abandoned:** prova do `Cabecalhos_do_arquivo` com `Results.File` (o ASP.NET não põe aspas no `filename`); o arquivo sai por um `IResult` próprio com `filename="..."`.
