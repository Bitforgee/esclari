# Parâmetros por instituição, ajustados no console da plataforma

Depende de `console-da-plataforma` (sessão da plataforma, tela de detalhe da instituição e registro de auditoria). Não começa antes dela.

## Problem

Os prazos do produto são uma configuração só para todas as instituições: `Prazos:AbandonoMinutos`, `ConfirmacaoDias`, `LembreteAguardandoDias`, `ResolverAguardandoDias`, `ValidadeConhecimentoMeses`, `PrimeiraRespostaHoras` e `ResolucaoHoras`, lidos uma vez na subida em `Esclari.Api/Prazos.cs` e entregues como um singleton a oito arquivos (`CalculoDetalhe`, `ConsultarIndicadoresDetalhe`, `Avisos`, `FiltroDeChamados`, `BuscarFontes` e as tarefas agendadas). Mudar um valor exige editar a configuração do servidor, reiniciar a API e vale para todos os clientes ao mesmo tempo. Quem paga é a equipe do Esclari, que não consegue atender a segunda instituição se ela tiver meta de atendimento, prazo de confirmação ou validade de conteúdo diferentes. O ROADMAP já lista "prazos, provedor de IA e marca por instituição" na fase da segunda instituição. A fonte não traz números.

Depois desta feature, o operador abre uma instituição no console, vê cada parâmetro com o padrão global e o valor próprio, muda ou restaura o que precisar, e o produto daquela instituição passa a usar o valor dela sem reiniciar nada. As instituições que não mudam nada continuam exatamente como hoje.

## Flow

Reusa o record `Prazos` e a leitura `Prazos.Ler` como o padrão global, a sessão da plataforma, a tela de detalhe e a auditoria do `console-da-plataforma`, e a instituição que o `CookieDeSessao` e o `Agendador` já carregam em cada requisição e em cada passo do agendador.

1. operador em `/plataforma/instituicoes/{slug}` -> `PUT /api/plataforma/instituicoes/{slug}/parametros` -> `AlterarParametros` (new, no door - placement per conventions) - valida cada valor e o conjunto efetivo
2. `Instituicao` (exists, door 1) - guarda os sete valores anuláveis; `null` herda o padrão global
3. `Plataforma/Auditoria` (exists after `console-da-plataforma`, door 3) - grava `PARAMETROS_ALTERADOS` na mesma transação
4. `Prazos.Para(instituicao)` (new, door 2) - devolve o `Prazos` efetivo da instituição: o próprio valor, ou o padrão global quando é `null`
5. os oito consumidores de `Prazos` (exist) - passam a receber o `Prazos` da instituição da requisição, ou da instituição que a tarefa agendada está processando
6. out: o produto daquela instituição usa os valores novos na próxima leitura

## Impact

| Front | What changes |
| --- | --- |
| domain | novo termo: `parâmetro` - um dos sete valores de `Prazos`, com padrão global e valor próprio opcional |
| domain | termo existente: `Prazos` era um singleton de configuração; passa a ser o valor efetivo de uma instituição, calculado por `Prazos.Para(instituicao)`. Quem ramifica nele hoje: `CalculoDetalhe`, `ConsultarIndicadoresDetalhe`, `Avisos.Resolvido`, `FiltroDeChamados`, `BuscarFontes.NovaValidade`, `AvisarAtrasos`, `AvisarValidade`, `SubirPorPrazo`, `LembrarAguardando`, `ResolverSemResposta` e `Persistencia` |
| domain | termo existente: `meta` de `prazos-de-atendimento` (AD-068) e `validade` de `validade-do-conhecimento` (AD-069) deixam de ser globais; os globais viram o padrão |
| API | uma rota nova de leitura e uma de escrita sob `/api/plataforma`; nenhuma rota de instituição muda de corpo |
| stored data | sete colunas anuláveis em `instituicao`, todas `NULL`; nenhuma linha existente muda de comportamento; conteúdo com `valido_ate` já gravado não é recalculado |
| tests | os testes que montam `Prazos` com a configuração continuam valendo (o padrão não muda); o `EsclariFactory` ganha um jeito de dar valores próprios a uma instituição |
| docs | AD novo no `STATE.md` que emenda o AD-068 e o AD-069; `GLOSSARIO.md` |

## Relations

None - nenhuma entidade nova e nenhuma cardinalidade nova. As colunas ficam em `instituicao` (door 1).

## Surface

| Route | In | Out | Status |
| --- | --- | --- | --- |
| `GET /api/plataforma/instituicoes/{slug}/parametros` | vazio | para cada um dos sete parâmetros: `valor` (inteiro ou `null`), `padrao` e `efetivo` | `200`, `401`, `404` |
| `PUT /api/plataforma/instituicoes/{slug}/parametros` | os sete parâmetros, cada um inteiro ou `null` (`null` restaura o padrão) | como o `GET` | `200`, `400`, `401`, `404` |

## Landing

| One-way door | Literal shape | Alternative rejected |
| --- | --- | --- |
| 1. onde ficam os valores | sete colunas anuláveis em `instituicao`: `prazo_abandono_minutos`, `prazo_confirmacao_dias`, `prazo_lembrete_aguardando_dias`, `prazo_resolver_aguardando_dias`, `prazo_validade_conhecimento_meses`, `prazo_primeira_resposta_horas`, `prazo_resolucao_horas`, todas `integer NULL` | tabela de chave e valor por instituição: sem tipo, aceita uma chave que o código não conhece, e cada leitura vira uma consulta; tabela `instituicao_parametro` com uma linha por instituição: uma junção a mais em cada requisição, e `instituicao` já é a linha que o `CookieDeSessao` e o `Agendador` carregam de qualquer jeito |
| 2. regra do valor efetivo | `efetivo = valor próprio ?? padrão global`, e as validações de `Prazos.Ler` valem sobre os efetivos: validade de 1 a 60 meses, horas de 1 a `8760`, `ResolucaoHoras >= PrimeiraRespostaHoras`, e `LembreteAguardandoDias < ResolverAguardandoDias` | exigir sempre o valor próprio, sem herdar: toda instituição existente ficaria com sete valores a preencher, e mudar o padrão global deixaria de valer para quem não pediu nada |
| 3. ação nova da auditoria | acrescenta `PARAMETROS_ALTERADOS` à lista fechada de ações de `auditoria_plataforma` (door 4 do `console-da-plataforma`), com o `resumo` listando nome e valor novo de cada parâmetro que mudou | gravar uma ação por parâmetro: sete linhas para uma alteração e nenhum ganho de consulta |

- Nada mais nesta mudança é difícil de desfazer. Os limites de cada parâmetro, os textos e a ordem na tela mudam por refatoração.

## Criteria

### S1: Valores próprios e padrão (P1)

**Acceptance Criteria**

1. The system SHALL guardar em cada instituição um valor opcional para cada um dos sete parâmetros, e tratar `null` como "usa o padrão global"
2. WHEN `GET /api/plataforma/instituicoes/{slug}/parametros` é chamado THEN the system SHALL responder `200` com `valor`, `padrao` e `efetivo` de cada parâmetro, de modo que `efetivo` é `valor` quando existe e `padrao` quando não
3. WHEN `PUT /api/plataforma/instituicoes/{slug}/parametros` recebe valores válidos THEN the system SHALL gravar exatamente o que veio, tratar `null` como restaurar o padrão, gravar `PARAMETROS_ALTERADOS` na auditoria na mesma transação e responder `200` com o estado novo
4. IF `ValidadeConhecimentoMeses` fica fora de 1 a 60, ou uma hora fica fora de 1 a 8760, ou `AbandonoMinutos` fica fora de 5 a 1440, ou `ConfirmacaoDias` fica fora de 1 a 60, ou `LembreteAguardandoDias` ou `ResolverAguardandoDias` ficam fora de 1 a 90 THEN the system SHALL responder `400` com o campo
5. IF o conjunto efetivo tem `ResolucaoHoras` menor que `PrimeiraRespostaHoras`, ou `LembreteAguardandoDias` maior ou igual a `ResolverAguardandoDias` THEN the system SHALL responder `400` e não gravar nada
6. IF o `slug` não existe THEN the system SHALL responder `404`; IF não há sessão da plataforma THEN `401`
7. WHEN o corpo não traz um dos sete parâmetros THEN the system SHALL tratá-lo como `null`

### S2: Cada instituição usa o seu valor (P1)

**Acceptance Criteria**

8. WHILE uma instituição não tem valor próprio, the system SHALL comportar-se como hoje em todos os oito consumidores, com os mesmos números do padrão global
9. WHEN uma instituição tem `PrimeiraRespostaHoras` ou `ResolucaoHoras` próprios THEN the system SHALL calcular `atraso`, os filtros de atraso, a previsão ao aluno, o resumo de atrasos e a seção de prazos dos indicadores com esses valores, e as outras instituições continuam com os delas
10. WHEN uma instituição tem `ConfirmacaoDias` próprio THEN the system SHALL usá-lo no prazo de rejeitar a resolução, no texto do e-mail "foi resolvida" e no fechamento automático
11. WHEN uma instituição tem `LembreteAguardandoDias` ou `ResolverAguardandoDias` próprios THEN the system SHALL usá-los no lembrete ao aluno e na resolução automática de `AGUARDANDO_ALUNO` dela, e só nela
12. WHEN uma instituição tem `AbandonoMinutos` próprio THEN the system SHALL usá-lo para marcar a conversa dela como abandonada
13. WHEN uma instituição tem `ValidadeConhecimentoMeses` próprio THEN the system SHALL usá-lo como a validade de todo artigo e documento novo, nova versão ou renovação dela; o `valido_ate` já gravado não muda
14. WHEN o valor de uma instituição muda THEN the system SHALL aplicá-lo na leitura seguinte, sem reiniciar a API
15. WHEN o padrão global `Prazos:*` muda e a API reinicia THEN the system SHALL aplicar o padrão novo a toda instituição que tem esse parâmetro em `null`, e manter o valor próprio das que não têm

### S3: A tela de parâmetros (P2)

**Acceptance Criteria**

16. WHEN o operador abre o detalhe de uma instituição THEN the system SHALL mostrar a aba "Parâmetros" com os sete valores, cada um com o padrão em texto de apoio e o valor próprio no campo, vazio quando herda
17. WHEN o operador salva THEN the system SHALL mostrar o estado novo e a frase "Parâmetros salvos."; WHEN a API responde `400` THEN the system SHALL mostrar o erro junto ao campo
18. WHEN o operador aperta "Usar padrão" ao lado de um parâmetro THEN the system SHALL esvaziar o campo e salvar `null` só depois de ele apertar "Salvar"

## Out of scope

| Excluded | Why |
| --- | --- |
| Provedor de IA e chave por instituição | segredo por cliente pede cofre e rotação; plano próprio |
| Marca, cores e textos | já são da instituição (`aparencia-instituicao`) |
| Fluxos de atendimento de outra instituição pelo console | escrever no interior de uma instituição quebra o isolamento; o administrador dela cadastra os fluxos, e modelos de fluxo são o épico E2 da evolução |
| O administrador da instituição ver ou editar os parâmetros | a decisão é da equipe do Esclari até alguém pedir |
| Recalcular `valido_ate` já gravado ao mudar a validade | mudaria o que o aluno vê e o aviso já enviado |
| Histórico de valores anteriores | a auditoria guarda o que mudou, e isso basta |
| Parâmetros novos (tamanho de anexo, limite de conversa, horário de atendimento) | cada um é decisão de produto própria |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| Quais parâmetros entram | os sete de `Prazos` | são os que já existem, validados e testados | n |
| Faixas dos que ainda não têm validação | abandono de 5 a 1440 minutos, confirmação de 1 a 60 dias, lembrete e resolução automática de 1 a 90 dias | valores do padrão (30, 7, 3, 7) bem dentro da faixa | n |
| Quem altera | só o operador da plataforma | decisão do dono: parametrização é do console | n |
| Efeito de mudar a validade | só vale para o que for criado, versionado ou renovado depois | não reescreve o que já foi avisado | n |
| Padrão global | continua em `Prazos:*` da configuração do servidor | é o que o produto faz hoje | n |

**Open questions:** none - all resolved or logged above.

## Observable

| Surface | Decision | Landing |
| --- | --- | --- |
| screen "Parâmetros" | empty state | AC 16 (campo vazio herda o padrão) |
| screen "Parâmetros" | loading state | AC 17 (botão desabilitado enquanto salva) |
| screen "Parâmetros" | error state | AC 17 |
| screen "Parâmetros" | unauthorised | existing - `401` leva à entrada do console (AC 50 do `console-da-plataforma`) |
| screen "Parâmetros" | destructive action confirms | n/a - nada se apaga e "Usar padrão" só vale ao salvar (AC 18) |
| API `GET`/`PUT .../parametros` | error shape and codes | AC 4, AC 5, AC 6 |
| API `GET`/`PUT .../parametros` | who may call | AC 6 |
| API `GET`/`PUT .../parametros` | versioning, rate limits | n/a - o produto não versiona rotas, e o limitador `auth` não cobre estas |
| celular 360×780 | campos em coluna, botões de 44 px | AC 16 |
| celular deitado 844×390 | n/a - a aba rola dentro da página | n/a - sem layout dependente da altura |
| teclado aberto | o campo em foco fica visível | AC 16 |
| toque | alvos de 44 px ou mais | AC 16, AC 18 |

## Sources

- `Esclari.Api/Prazos.cs` e `Persistencia.cs` - os sete parâmetros, os limites de hoje e o registro como singleton
- `.specs/STATE.md` AD-068 (metas de atendimento) e AD-069 (validade do conhecimento), que este plano emenda
- `ROADMAP.md`, fase "Segunda instituição": "provedor de IA, prazos e marca por instituição"
- `.specs/features/console-da-plataforma/plan.md` - sessão, tela de detalhe e auditoria que esta feature usa
- Pedido do dono, 18/11/2026: o intuito do console é parametrizar o que não é sensível
