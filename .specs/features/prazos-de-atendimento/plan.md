# Prazos de atendimento, estouro e acompanhamento da gestão

## Problem

Hoje o prazo existe só em pedaços. A etapa de um fluxo pode ter `PrazoHoras` opcional e, vencido, o chamado sobe para a etapa seguinte (`SubirPorPrazo`). Em `AGUARDANDO_ALUNO` o aluno é lembrado aos 3 dias e o chamado vira `RESOLVIDO` aos 7 (`LembrarAguardando`, `ResolverSemResposta`), e um `RESOLVIDO` fecha em 7 dias (`FecharResolvidos`). Fora disso não há prazo: um chamado sem fluxo (Triagem), na última etapa ou numa etapa sem prazo fica `ABERTO` ou `EM_ATENDIMENTO` para sempre, ninguém é avisado de que passou do tempo, o aluno não sabe quando esperar resposta e o painel mostra mediana e percentil 90 sem meta, então "a equipe responde a tempo?" não tem como ser respondido (o AD-060 rejeitou uma meta configurável porque "não existe meta no produto").

Quem paga: o aluno, que fica sem previsão, e a gestão, que só descobre o chamado esquecido quando o aluno reclama. Evidência: a devolutiva da banca (17/11/2026) pergunta "existe prazo para atendimento?", "quando o prazo estoura, o que ocorre?", "como evitar que solicitações fiquem abertas indefinidamente?" e registra que "no vídeo não foi evidenciado como a gestão consegue acompanhar". Não há medida de volume, porque não há produção. A prática de helpdesk separa a meta de primeira resposta da de resolução, pausa o relógio enquanto se espera o cliente e avisa antes e depois do estouro ([Jitbit](https://www.jitbit.com/helpdesk/sla/), [ONES](https://ones.com/blog/support-ticket-sla-policy-examples-response-resolution-escalation-and-ownership/), [DeskHero](https://www.deskhero.com/articles/sla-reminders-automation)).

Depois disto, o aluno vê "a equipe responde em até 24 horas" ao abrir o chamado, a equipe vê quais chamados estão atrasados, o responsável ou o setor recebe e-mail no estouro, o administrador recebe um resumo diário enquanto houver atraso, e o painel mostra a meta, o percentual dentro dela e quantos chamados estão atrasados agora.

## Flow

Reusa o `Agendador` com `ITarefaAgendada`, a fila `EmailSaida`, o cálculo de primeira resposta e de resolução com pausa em `AGUARDANDO_ALUNO` que `CalculoIndicadores` já faz, `Prazos.cs` para as metas, o filtro `FiltroDeChamados`/`GET /api/chamados/busca` e a tela `Indicadores` por perguntas do gestor (AD-060).

1. `Prazos` (exists) - lê `Prazos:PrimeiraRespostaHoras` (24) e `Prazos:ResolucaoHoras` (120), door 2
2. `TransferirConversa` (exists) e `DetalheChamado` (exists) - devolvem ao aluno a previsão de primeira resposta
3. `BuscarChamados`/`ListarChamados` com `FiltroDeChamados` (exist) - calculam `atraso` por chamado e aceitam `atraso=true` (door 3)
4. `AvisarAtrasos` (new, `ITarefaAgendada`, door 1) - acha chamados em atraso, avisa o setor no primeiro estouro, resume ao administrador a cada 24 h, grava `atraso_avisado_em` e os `EmailSaida` numa transação
5. `ConsultarIndicadoresDetalhe` e `CalculoDetalhe` (exist) - acrescentam o bloco `prazos` (door 3)
6. telas `Fila`/chamados e `Indicadores` (exist) - mostram a etiqueta de atraso, o filtro e a seção com meta

## Impact

| Front | What changes |
| --- | --- |
| domain | novo termo: `atraso` - estado calculado do chamado (`PRIMEIRA_RESPOSTA` ou `RESOLUCAO`) que nunca é gravado como `Estado`; vive em `Chamados/Consulta` |
| domain | novo termo: `meta` - as horas de `Prazos:PrimeiraRespostaHoras` e `Prazos:ResolucaoHoras`; não é prazo de etapa |
| domain | termo existente: `PrazoHoras` da etapa continua sendo o prazo de subir de etapa (`SubirPorPrazo`); não muda e não substitui a meta. Quem ramifica nele hoje: `SubirPorPrazo`, o editor de fluxo |
| domain | termo existente: o AD-060 diz que não há meta de SLA no produto; passa a haver (novo AD no `STATE.md` ao aprovar, supersede essa cláusula) |
| API | `GET /api/chamados/busca` ganha `atraso` na entrada e em cada item; `GET /api/chamados/{numero}` ganha `prazoPrimeiraRespostaEm` para o aluno; `POST /api/conversas/{id}/transferir` ganha `prazoPrimeiraRespostaHoras`; `GET /api/indicadores/detalhe` ganha `prazos` (todos aditivos) |
| API | `GET /api/indicadores`, `GET /api/chamados?escopo=...` e as ferramentas MCP `ver_indicadores` e `listar_chamados` ficam como estão (AD-055) |
| stored data | `chamado` ganha uma marca anulável; chamados já em atraso na subida recebem o primeiro aviso no mesmo dia, em resumo único por destinatário, nunca um e-mail por chamado |
| tests | os retratos da API (`Retratos/sucessos.txt`) ganham os campos novos nas rotas listadas; nenhuma asserção existente muda de valor |

## Relations

None - no stored-data shape change além de uma marca anulável em `chamado` (door 1); nenhuma entidade nova e nenhuma cardinalidade nova.

## Surface

| Route | In | Out | Status |
| --- | --- | --- | --- |
| `GET /api/chamados/busca` | `atraso` (`true`), além dos filtros atuais | `itens[].atraso` e o resto atual | `200`, `400`, `401`, `403` |
| `GET /api/chamados/{numero}` | `numero` | `prazoPrimeiraRespostaEm` (só para o aluno dono do chamado) e o resto atual | `200`, `401`, `404` |
| `POST /api/conversas/{id}/transferir` | `matriculaId` como hoje | `prazoPrimeiraRespostaHoras` e o resto atual | `200`, `401`, `404`, `409` |
| `GET /api/indicadores/detalhe` | como hoje | `prazos` e o resto atual | `200`, `400`, `401`, `403` |

## Landing

| One-way door | Literal shape | Alternative rejected |
| --- | --- | --- |
| 1. marca de aviso no chamado | `atraso_avisado_em timestamptz NULL` em `chamado`; nula = nunca avisado; o setor é avisado quando está nula, e o administrador quando está nula ou tem 24 h ou mais; toda inclusão atualiza para o instante da execução | evento `PRAZO_ESTOURADO` na linha do tempo: um evento por lembrete diário enche o histórico que o atendente lê e `TiposEvento` é vocabulário gravado (glossário), e um valor novo exige migração de leitura; tabela de avisos como a de validade: o chamado já tem o token de concorrência que impede o aviso duplo, e uma tabela nova só ganharia o histórico que ninguém pediu |
| 2. chaves de configuração | `Prazos:PrimeiraRespostaHoras` = `24` e `Prazos:ResolucaoHoras` = `120`, inteiros de 1 a 8760, lidos em `Prazos.cs`; valores fora da faixa recusam a subida | colunas de meta em `instituicao` com tela de edição: nenhuma instituição pediu metas diferentes, a migração e a tela custariam mais que a chave, e acrescentar depois é aditivo |
| 3. contrato de leitura | `itens[].atraso` = `"PRIMEIRA_RESPOSTA"` \| `"RESOLUCAO"` \| `null`; `detalhe.prazos` = `{ "primeiraRespostaMetaHoras": 24, "resolucaoMetaHoras": 120, "primeiraRespostaNoPrazo": 0.82\|null, "resolucaoNoPrazo": 0.74\|null, "emAtrasoAgora": 3, "maisAntigoEmAtrasoDias": 5\|null }`; as taxas são `null` com menos de 10 casos | gravar `atraso` como estado do chamado: ficaria defasado entre duas rodadas do agendador e criaria uma transição que a máquina de estados não tem; acrescentar os campos a `GET /api/indicadores`: o MCP `ver_indicadores` serializa o mesmo `Resultado` e o AD-055 fixa essa saída |

- Nada mais nesta mudança é difícil de desfazer. Os 24 h de repetição do resumo, o texto dos e-mails e as etiquetas mudam por refatoração.

## Criteria

### S1: O aluno sabe quando esperar (P1)

**Acceptance Criteria**

1. WHEN o aluno transfere a conversa para um atendente THEN the system SHALL devolver `prazoPrimeiraRespostaHoras` igual a `Prazos:PrimeiraRespostaHoras` (24 por padrão) e a tela de confirmação SHALL mostrar "A equipe responde em até 24 horas"
2. WHEN o aluno abre um chamado `ABERTO` ou `EM_ATENDIMENTO` sem mensagem pública da equipe THEN the system SHALL devolver `prazoPrimeiraRespostaEm` igual a `criadoEm` mais a meta e a tela SHALL mostrar "Resposta prevista até DD/MM às HH:mm" no fuso da instituição
3. WHEN a equipe já escreveu uma mensagem pública, ou o estado é `AGUARDANDO_ALUNO`, `RESOLVIDO` ou `FECHADO` THEN the system SHALL devolver `prazoPrimeiraRespostaEm` nulo e a tela SHALL não mostrar previsão
4. IF o chamado é de outro aluno THEN the system SHALL responder `404` como hoje, sem revelar o prazo

### S2: A equipe vê o que está atrasado (P1)

**Acceptance Criteria**

5. The system SHALL tratar como atraso de primeira resposta o chamado `ABERTO` ou `EM_ATENDIMENTO` sem mensagem pública de quem não é o solicitante e com `criadoEm` mais `PrimeiraRespostaHoras` menor ou igual a agora
6. The system SHALL tratar como atraso de resolução o chamado `ABERTO` ou `EM_ATENDIMENTO` com `criadoEm` mais `ResolucaoHoras` mais o tempo já passado em `AGUARDANDO_ALUNO` menor ou igual a agora, e nunca tratar como atrasado um chamado `AGUARDANDO_ALUNO`, `RESOLVIDO` ou `FECHADO`
7. WHEN o chamado tem os dois atrasos THEN the system SHALL devolver `atraso` igual a `PRIMEIRA_RESPOSTA`
8. WHEN a equipe lista chamados THEN the system SHALL devolver `atraso` em cada item e as telas SHALL mostrar a etiqueta "Atrasado: 1ª resposta" ou "Atrasado: resolução" com texto, nunca só cor
9. WHEN a requisição traz `atraso=true` THEN the system SHALL listar só os chamados atrasados, a tela SHALL contar o filtro em "Filtros (N)" e o link do e-mail e do painel SHALL abrir a lista já filtrada
10. IF `atraso` vier com valor diferente de `true` ou `false` THEN the system SHALL responder `400` com ProblemDetails

### S3: O estouro aciona alguém e não se repete sem fim (P1)

**Acceptance Criteria**

11. WHEN a tarefa `AvisarAtrasos` roda e há chamado em atraso com `atraso_avisado_em` nulo THEN the system SHALL enfileirar um e-mail para o responsável do chamado, ou, sem responsável, para cada atendente ativo do setor, um por destinatário com a lista dos chamados dele (protocolo, título, tipo e tempo de atraso, link), e gravar `atraso_avisado_em` com o instante da execução
12. WHEN há chamado em atraso com `atraso_avisado_em` nulo ou de 24 horas ou mais atrás THEN the system SHALL incluí-lo num único e-mail de resumo por `ADMINISTRADOR` ativo da instituição, com assunto `[<Instituição>] N chamados em atraso`, e atualizar `atraso_avisado_em`
13. WHEN a tarefa roda de novo antes de 24 horas sem chamado novo em atraso THEN the system SHALL não enfileirar e-mail e não alterar nenhuma marca
14. WHEN a tarefa roda pela primeira vez sobre chamados que já estavam atrasados THEN the system SHALL enviar um e-mail por destinatário com todos os chamados dele, nunca um e-mail por chamado
15. IF não há destinatário ativo com e-mail THEN the system SHALL não gravar `atraso_avisado_em` e registrar um log de aviso com o id da instituição
16. IF duas execuções tentam avisar o mesmo chamado ao mesmo tempo THEN the system SHALL manter uma marca e um e-mail por destinatário, pelo token de concorrência do chamado
17. WHEN o e-mail é enfileirado THEN the system SHALL gravar a marca e os `EmailSaida` na mesma transação
18. WHEN um chamado atrasado recebe resposta pública da equipe, é resolvido, cancelado ou passa a `AGUARDANDO_ALUNO` THEN the system SHALL deixar de listá-lo como atrasado e de incluí-lo em e-mail
19. The system SHALL não fechar, cancelar nem resolver por atraso um chamado `ABERTO` ou `EM_ATENDIMENTO`, e SHALL manter inalteradas as regras de `SubirPorPrazo`, `LembrarAguardando`, `ResolverSemResposta` e `FecharResolvidos`
20. WHEN a tarefa termina uma instituição THEN the system SHALL registrar um log informativo com o id da instituição, a contagem de chamados em atraso por tipo e o número de e-mails enfileirados

### S4: A gestão acompanha no painel (P1)

**Acceptance Criteria**

21. WHEN o administrador ou o atendente abre `Indicadores` THEN the system SHALL mostrar na seção "A equipe responde a tempo?" a meta ("Meta: primeira resposta em 24 h; resolução em 120 h"), o percentual dentro da meta de cada uma com a variação contra o período anterior, e ocultar o percentual com menos de 10 casos, como o AD-060 já faz
22. The system SHALL calcular `primeiraRespostaNoPrazo` sobre os chamados criados no período que receberam resposta da equipe e `resolucaoNoPrazo` sobre os resolvidos no período, com o mesmo tempo de pausa em `AGUARDANDO_ALUNO` que `TempoResolucaoHoras` já usa
23. WHEN há chamados em atraso THEN the system SHALL mostrar "Em atraso agora: N" e "O mais antigo está atrasado há N dias" com link para a lista filtrada, e WHEN não há THEN mostrar "Nenhum chamado em atraso"
24. WHILE o painel carrega the system SHALL mostrar o esqueleto da seção, e IF a leitura falha THEN the system SHALL mostrar o erro padrão com "Tentar de novo" sem esconder as outras seções
25. WHEN o painel é filtrado por setor THEN the system SHALL calcular `prazos` só com os chamados do setor
26. The system SHALL oferecer "Ver como tabela" no gráfico novo, com contraste 3:1 nos 4 temas, como o AD-060

### S5: Valores de configuração seguros (P2)

**Acceptance Criteria**

27. IF `Prazos:PrimeiraRespostaHoras` ou `Prazos:ResolucaoHoras` for menor que 1 ou maior que 8760 THEN the system SHALL recusar a configuração na subida, com mensagem que cita a chave
28. IF `Prazos:ResolucaoHoras` for menor que `Prazos:PrimeiraRespostaHoras` THEN the system SHALL recusar a configuração na subida

## Out of scope

| Excluded | Why |
| --- | --- |
| meta por instituição, setor, categoria ou prioridade | o pedido é saber se existe prazo e o que acontece; uma meta global responde, e níveis de prioridade são produto novo |
| calendário de horas úteis e feriados | exige cadastro de calendário por instituição; as metas contam horas corridas e a pausa só acontece em `AGUARDANDO_ALUNO` |
| fechar ou cancelar sozinho chamado atrasado | resolver um pedido do aluno sem a equipe esconderia o problema que o prazo existe para mostrar |
| aviso por push ou in-app ao atendente | o push do produto é do aluno; para a equipe, e-mail, etiqueta e filtro cobrem o pedido |
| escalonamento automático além do que `SubirPorPrazo` já faz | o resumo diário ao administrador é a escada; subir de setor por meta geral muda quem atende |
| exportar o relatório de atraso | o CSV de `filtros-e-relatorios` já exporta a lista filtrada e passa a respeitar `atraso=true` |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| metas | 24 h para a primeira resposta e 120 h (5 dias) para a resolução, horas corridas | a primeira resposta é o que o aluno sente; a resolução cobre casos que passam por mais de um setor; pesquisas de helpdesk usam metas separadas e pausa durante a espera do cliente | y - user delegated (17/11) |
| pausa do relógio | só em `AGUARDANDO_ALUNO`, como `TempoResolucaoHoras` já faz | mantém o painel e o aviso com a mesma conta | y - user delegated (17/11) |
| quem recebe o aviso do estouro | o responsável do chamado, ou os atendentes do setor se não houver responsável; o resumo diário vai aos `ADMINISTRADOR` | "gestor" na base é o `ADMINISTRADOR`; a fila já pertence ao setor | y - user delegated (17/11) |
| repetição | o resumo ao administrador repete a cada 24 h enquanto houver atraso | responde "como evitar que fique aberto indefinidamente" sem criar escalonamento novo | y - user delegated (17/11) |
| a meta de resolução vale para chamado resolvido pela equipe | o relógio para no primeiro `RESOLVIDO`; a rejeição do aluno reabre e reinicia a conta como hoje | usa o evento que o painel já usa | y - user delegated (17/11) |
| a primeira resposta conta mensagem pública de quem não é o solicitante | nota interna não conta | é o que `SemRespostaDaEquipe` e o painel já assumem | y - user delegated (17/11) |

**Open questions:** none - all resolved or logged above.
## Observable

| Surface | Decision | Landing |
| --- | --- | --- |
| screen chamado do aluno | empty, loading, error state | existing - a tela do chamado já trata os três; AC 3 define quando a previsão some |
| screen chamado do aluno | unauthorised state | AC 4 e existing - `404` para chamado de outro |
| screen chamado do aluno | density and ordering | AC 2: a previsão fica numa linha abaixo do estado, sem novo bloco |
| screen chamado do aluno | destructive action confirms | n/a - a previsão só informa |
| screen chamado do aluno celular 360×780 | layout | AC 2: a frase quebra em duas linhas sem cortar a data |
| screen chamado do aluno celular deitado 844×390 | layout | AC 2: mesma frase, a tela rola na vertical |
| screen chamado do aluno teclado aberto | layout | n/a - a previsão não adiciona campo de texto |
| screen chamado do aluno toque | alvos | n/a - texto sem ação |
| screen filas e busca de chamados | empty, loading, error state | existing - `busca` já define os três; o filtro `atraso` sem resultado mostra o vazio de filtro atual |
| screen filas e busca de chamados | unauthorised state | existing - `busca` devolve `403` a quem não é da equipe |
| screen filas e busca de chamados | density and ordering | AC 8: a etiqueta entra na linha existente; a ordem atual não muda |
| screen filas e busca de chamados | destructive action confirms | n/a - filtrar e etiquetar não alteram dados |
| screen filas e busca de chamados celular 360×780 | layout | AC 8: etiqueta na segunda linha do cartão, sem cortar o protocolo |
| screen filas e busca de chamados celular deitado 844×390 | layout | AC 8 |
| screen filas e busca de chamados teclado aberto | layout | n/a - o filtro de atraso é uma chave, sem texto; a caixa de busca já existe |
| screen filas e busca de chamados toque | alvos | AC 9: a chave de filtro tem 44 px de altura no celular |
| screen `Indicadores` | empty, loading, error | AC 23, AC 24 |
| screen `Indicadores` | unauthorised state | existing - `ConsultarIndicadoresDetalhe` devolve `403` e a rota só aparece para `ADMINISTRADOR` e `ATENDENTE` |
| screen `Indicadores` | density and ordering | AC 21, AC 23: a meta e o atraso entram na seção 4 já existente, antes dos tempos |
| screen `Indicadores` | destructive action confirms | n/a - a tela só lê |
| screen `Indicadores` celular 360×780, celular deitado 844×390 | layout | AC 21, AC 26: herda o layout do AD-060; a meta quebra em duas linhas |
| screen `Indicadores` teclado aberto | layout | n/a - a única entrada são os campos de data do topo, como hoje |
| screen `Indicadores` toque | alvos | AC 26: "Ver como tabela" tem 44 px |
| API `GET /api/chamados/busca` | error shape and codes | AC 10 (ProblemDetails, `400`) |
| API `GET /api/indicadores/detalhe` e `GET /api/chamados/{numero}` | error shape and codes, who may call | existing - sem mudança de status; AC 4 |
| all changed routes | versioning, rate limits | n/a - a API não versiona rotas e nenhuma rota de leitura tem limite de taxa hoje |
| e-mail de estouro e resumo | structure, tone, next step | AC 11, AC 12: assunto, lista com protocolo e tempo, link para a lista filtrada; português do Brasil, sem nome ou matrícula do aluno no corpo |
| e-mail de estouro e resumo | failure | AC 17 e a fila `EmailSaida` já repete com atraso crescente |

### As nove dimensões (resumo; o registro com checks vai em `checks.md` `## Swept`)

| Dimension | Landing |
| --- | --- |
| Validation and bounds | AC 10, AC 27, AC 28 |
| Failure and partial failure | AC 15, AC 17 |
| Idempotency, retry, duplicates | AC 13, AC 14, AC 16 |
| Authorization and rate limits | AC 4; rate limit n/a (ver Observable) |
| Concurrency and ordering | AC 16 (duas execuções ao mesmo tempo) |
| Data lifecycle | AC 18, AC 19 (nenhum fechamento automático novo) |
| External-dependency failure | AC 17 e a fila de e-mail com tentativas; sem dependência externa nova |
| State transitions | AC 6, AC 18, AC 19 (`atraso` nunca é estado gravado) |
| Observability | AC 20 |

## Sources

- Devolutiva da banca, 17/11/2026 - prazo de atendimento, o que ocorre no estouro, solicitações abertas indefinidamente, acompanhamento da gestão
- `.specs/STATE.md` AD-055 (saída do MCP), AD-060 (painel e a cláusula "sem meta"), AD-066 (busca e CSV de chamados)
- [Jitbit - SLA](https://www.jitbit.com/helpdesk/sla/), [ONES - SLA policy](https://ones.com/blog/support-ticket-sla-policy-examples-response-resolution-escalation-and-ownership/), [DeskHero - SLA reminders](https://www.deskhero.com/articles/sla-reminders-automation) (conteúdo parafraseado)
