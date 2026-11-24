# Escolher a instituição no app instalado

Profile `ui` (feature com tela). Base: `49664aa`.

## Problem

O app instalado abre numa tela sem saída, e não há como sair dela por endereço. O navegador só deixa instalar o site base (observação do dono, 24/11). Qualquer que seja a página aberta na hora de instalar, o app instalado é o do manifesto padrão (`web/vite.config.ts`): nome "Esclari", `start_url` e `scope` em `/`. O manifesto por instituição que o `AplicaAparencia` troca por JavaScript (AD-032) não chega ao app instalado. A pesquisa também não achou documentação de que o Chrome ou o Safari do iOS apliquem essa troca. Todo aluno que instala cai na `Raiz` (`web/src/acesso/Acesso.tsx:52`). Ela só diz "Abra o endereço da sua instituição, por exemplo /univag.", e no modo `standalone` não há onde digitar endereço. O aluno fica preso na tela da marca, e reinstalar não resolve, porque o app instalado é sempre o mesmo.

Quem paga é todo aluno que instala pelo celular, e a equipe da instituição, que recebe o pedido de ajuda. O dono do projeto relatou em 24/11: "quando instalo o pwa ele apresenta pra raiz onde não tem nenhuma universidade [...] isso inviabiliza o uso" e "o pwa só deixa instalar o site base". Não há número de quantos alunos foram afetados; o produto ainda não está em produção.

Depois desta feature, a raiz deixa de ser uma tela sem saída e vira a escolha da instituição, como o "Find my school" do app do Canvas e o campo "Your site" do app do Moodle. O aluno digita parte do nome, toca na instituição e vai ao Login dela. Nas próximas aberturas o app vai direto para a última instituição usada. O Login ganha "Trocar de instituição" para voltar à escolha.

## Flow

Reusa a rota pública `GET /api/instituicoes/{slug}` e o `useInstituicao` que já carregam os dados públicos em toda rota `/:slug`. Reusa também o `unaccent` com `ILIKE` escapado de `FiltroDeChamados` (AD-066) e o `AddRateLimiter` de `Seguranca.cs`.

1. aluno abre o app instalado, que sempre começa em `/` -> `Raiz` (exists) lê `localStorage['esclari-instituicao']` (door 2)
2. com uma instituição lembrada -> `Raiz` (exists) troca a rota para `/{slug}` com `replace`, e o Login (exists) abre
3. sem uma instituição lembrada -> `Raiz` (exists) mostra o campo "Sua instituição"; 300 ms depois da última tecla, com 3 ou mais caracteres, chama `GET /api/instituicoes?busca=` (door 1)
4. `BuscarInstituicoes` (new, door 1) em `Esclari.Api/Acesso/` -> lê `instituicao` (exists, sem filtro de instituição, AD-008) pelas ativas que casam o texto e devolve até 5 itens, sob a política de limite `instituicoes` (new, no door - placement per conventions)
5. aluno toca num resultado -> `/{slug}` (dentro do `scope` `/` do app instalado, sem barra do navegador) -> `useInstituicao` (exists) recebe `200` e grava o slug em `localStorage['esclari-instituicao']` (door 2); com `404` para o slug lembrado, apaga a chave
6. Login (exists) -> link "Trocar de instituição" apaga a chave e abre `/`, que volta ao passo 3, também dentro do app instalado

## Impact

| Front | What changes |
| --- | --- |
| domain | novo termo: `instituição lembrada`. É o slug da última instituição cujos dados públicos carregaram neste aparelho, e mora só no navegador. |
| domain | termo existente: `Raiz` era uma tela de marca com uma frase de instrução; passa a ser a escolha da instituição. Quem ramifica nela hoje: `e2e/simulada.ts:211` (espera a frase antiga), `e2e/ux.spec.ts` (`SemCasca`, tamanho do `h1`), `e2e/arranjo.spec.ts:319`, `:355`, `:459` e `:993`, `e2e/identidade.spec.ts:147` e `:564`, `e2e/marca.spec.ts:29` e `:39` |
| API | uma rota anônima nova, `GET /api/instituicoes`; nenhuma rota existente muda de corpo. Os retratos `rotas.txt`, `RetratoTests` e a lista `Publicas` de `SessaoTests` ganham a linha nova |
| stored data | nada no banco. No aparelho do aluno, uma chave nova de `localStorage` |
| tests | `arranjo.spec.ts:355` visita `/univag` e depois `/` na mesma página. Com o passo 2, a segunda visita vai para `/univag`. O teste precisa abrir `/` com o armazenamento limpo; a asserção sobre a `Raiz` não muda |
| manifest | nenhum. O padrão (`/`) continua sendo o do app instalado. O da instituição (`/{slug}/`, AD-032) continua servido e trocado pelo `AplicaAparencia`, mas na prática não chega ao app instalado. Esta feature não o remove (Out of scope) |
| exposição | a busca pública deixa descobrir os nomes das instituições ativas com tentativas curtas. Hoje isso só se faz adivinhando o slug em `GET /api/instituicoes/{slug}`. Ver door 1 e Assumptions |
| docs | AD novo no `STATE.md` (rota pública de busca e chave lembrada), `GLOSSARIO.md` |

## Relations

None - nenhuma entidade nova e nenhuma mudança de dados gravados no banco.

## Surface

| Route | In | Out | Status |
| --- | --- | --- | --- |
| `GET /api/instituicoes` | query `busca` (texto, 3 a 60 caracteres depois de aparar) | `itens`: lista de até 5 `{ slug, nome, nomeCurto }` | `200`, `400`, `429` |

## Landing

| One-way door | Literal shape | Alternative rejected |
| --- | --- | --- |
| 1. contrato público da busca | `GET /api/instituicoes?busca=<texto>` anônima devolve `{ "itens": [ { "slug": "univag", "nome": "Centro Universitário Várzea Grande", "nomeCurto": "Univag" } ] }`, sem outro campo (sem logo, cor, id ou contato), ativas apenas, ordem por `nome`, no máximo 5, com 3 a 60 caracteres; `400` em ProblemDetails com `errors.busca` | Devolver o logo: a versão da imagem está em `imagem_instituicao`, que tem filtro global por instituição. Ler várias de uma vez exige `IgnoreQueryFilters`, permitido só em `ITarefaAgendada` (AGENTS.md, AD-008). Listar todas sem texto: o dono escolheu a busca (24/11). Só o código, sem API nova, foi preterido porque o aluno precisa saber o código. A busca também revela as instituições ativas a quem varre combinações de 3 letras. O mínimo de 3 letras, os 5 itens e o limite por IP só tornam essa varredura mais lenta, não a impedem. Aceitar isso depende de a lista de clientes não ser sigilosa (Assumptions). Se for, a saída é "só o código", e a S1 sai do plano |
| 2. chave lembrada no aparelho | `localStorage['esclari-instituicao'] = '<slug>'`, no padrão `esclari-*` de `esclari-tema` e `esclari-barra` (AD-058 door 3) | cookie: iria em toda requisição e o servidor não o usa. Lembrar só no modo instalado (`display-mode: standalone`): o iOS informa isso por `navigator.standalone`, fora do padrão, e o navegador comum também ganha com a volta direta |

- Nada mais nesta mudança é difícil de desfazer. Textos, o tempo de espera de 300 ms, o limite de taxa e a ordem dos resultados mudam por refatoração.

## Criteria

### S1: A API encontra a instituição pelo nome (P1)

**Acceptance Criteria**

1. WHEN alguém sem sessão chama `GET /api/instituicoes?busca=univ` THEN the system SHALL responder `200` com `itens` contendo as instituições ativas cujo `nome`, `nomeCurto` ou `slug` contém o texto, sem diferenciar maiúsculas nem acentos (`busca=varzea` acha "Várzea"), ordenadas por `nome`
2. The system SHALL devolver em cada item exatamente os campos `slug`, `nome` e `nomeCurto`, e no máximo 5 itens
3. The system SHALL nunca devolver uma instituição com `Ativa` falso
4. IF `busca` falta ou tem menos de 3 ou mais de 60 caracteres depois de aparar THEN the system SHALL responder `400` com ProblemDetails e `errors.busca` = "Digite de 3 a 60 caracteres."
5. WHEN `busca` contém `%`, `_` ou `\` THEN the system SHALL tratá-los como texto literal (`busca=%%%` responde `200` com `itens` vazio quando nenhum nome contém `%%%`)
6. IF o mesmo IP faz mais de 60 chamadas a `GET /api/instituicoes` em 60 s THEN the system SHALL responder `429` (configurável por `Limites:BuscaDeInstituicaoPorMinuto`)

**Independent test:** `curl "/api/instituicoes?busca=univ"` com uma instituição ativa e uma inativa de nomes parecidos.

### S2: A raiz vira a escolha da instituição (P1)

**Acceptance Criteria**

7. WHILE nenhuma instituição está lembrada, WHEN `/` abre THEN the system SHALL mostrar o logo do Esclari no `h1`, o campo com rótulo "Sua instituição", a dica "Digite o nome ou a sigla da sua instituição." e nenhum resultado
8. WHEN o aluno digita 3 ou mais caracteres THEN the system SHALL chamar a busca 300 ms depois da última tecla e mostrar cada item como um link para `/{slug}` com o `nome` e, quando houver, o `nomeCurto`
9. WHILE o campo tem menos de 3 caracteres the system SHALL não chamar a busca e não mostrar a lista
10. WHILE a busca está em andamento the system SHALL mostrar "Buscando…" numa região `aria-live="polite"`
11. IF a busca responde `200` com `itens` vazio THEN the system SHALL mostrar "Nenhuma instituição encontrada para “{texto}”. Confira o nome ou peça o endereço à sua instituição."
12. IF a busca falha por rede ou `5xx` THEN the system SHALL mostrar "Não foi possível buscar agora. Verifique a conexão e tente de novo.", e a próxima tecla SHALL buscar de novo
13. IF a busca responde `429` THEN the system SHALL mostrar "Muitas buscas agora. Aguarde um minuto e tente de novo."
14. WHEN o aluno toca num resultado THEN the system SHALL abrir `/{slug}` na mesma janela

**Independent test:** abrir `/` com o armazenamento limpo, digitar "univ", tocar em "Univag" e cair no Login da Univag.

### S3: O app volta sozinho para a última instituição (P1)

**Acceptance Criteria**

15. WHEN `GET /api/instituicoes/{slug}` responde `200` em qualquer rota `/:slug` THEN the system SHALL gravar `{slug}` em `localStorage['esclari-instituicao']`
16. WHILE `localStorage['esclari-instituicao']` tem um slug, WHEN `/` abre THEN the system SHALL ir para `/{slug}` com `replace`, sem mostrar a escolha, de modo que "Voltar" não retorna a `/`
17. IF `GET /api/instituicoes/{slug}` responde `404` e o slug lembrado é `{slug}` THEN the system SHALL apagar `localStorage['esclari-instituicao']`
18. IF ler ou gravar o `localStorage` lança exceção THEN the system SHALL mostrar a escolha em `/` e seguir sem lembrar nada
19. The Login SHALL mostrar o link "Trocar de instituição", que apaga `localStorage['esclari-instituicao']` e abre `/` com a escolha
20. WHEN o Login mostra "Instituição não encontrada." THEN the system SHALL mostrar também o link "Escolher instituição" para `/`

**Independent test:** escolher a Univag, fechar e abrir `/` de novo (cai no Login da Univag); tocar em "Trocar de instituição" (volta à escolha).

### S4: A escolha funciona no celular (P1)

**Acceptance Criteria**

21. WHEN `/` abre em 360×780 com 5 resultados na lista THEN the system SHALL mostrar o campo e os 5 resultados inteiros sem rolagem
22. WHEN o campo recebe foco com a área visível reduzida a 360×420 (teclado aberto) THEN the system SHALL manter o campo e o primeiro resultado inteiros dentro da área visível
23. WHEN `/` abre em 844×390 THEN the system SHALL mostrar o campo sem rolagem e não ter rolagem horizontal
24. The system SHALL dar a cada resultado e aos links "Trocar de instituição" e "Escolher instituição" um alvo de toque de pelo menos 44 px de altura

**Independent test:** Playwright em 360×780, 360×420 e 844×390 com a API simulada devolvendo 5 itens.

## Out of scope

| Excluded | Why |
| --- | --- |
| nome e ícone da instituição no app instalado | o navegador só instala o site base, então o app instalado se chama "Esclari" e usa o ícone do Esclari. A marca da instituição aparece dentro do app depois da escolha (AD-032) |
| remover ou corrigir o manifesto por instituição (`GET /api/instituicoes/{slug}/manifest.webmanifest` e a troca em `aparencia.tsx`) | não causa a tela sem saída; remover é decisão sobre o AD-032 e mexe em `ManifestoTests`. Fica registrado no Impact para uma feature própria |
| login por QR code (Canvas, Moodle) | ninguém pediu; o Login já é por CPF e senha |
| lembrar mais de uma instituição ou trocar dentro do app logado | um aluno com duas instituições (AD-009) é raro; "Sair" leva ao Login, que tem "Trocar de instituição" |
| logo da instituição nos resultados | exige ler `imagem_instituicao` de várias instituições de uma vez (door 1) |
| mudar o manifesto padrão ou o da instituição | o problema está na tela de `/`, não no manifesto |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| volta automática também no navegador comum | sim, sempre que `/` abre com uma instituição lembrada | `/` não tem outro conteúdo; detectar o modo instalado difere no iOS (`navigator.standalone`) | n |
| instituição ativa mas ainda em `CONFIGURANDO` (AD-074) aparece na busca | sim, o mesmo critério de `GET /api/instituicoes/{slug}` (`Ativa`) | o Login dela já abre hoje pelo endereço; esconder na busca criaria duas regras | n |
| a lista de clientes não é sigilosa | aceitar que os nomes das instituições ativas possam ser descobertos pela busca. Mínimo de 3 caracteres, 5 itens e 60 buscas por minuto por IP só tornam a varredura mais lenta | o nome da instituição já é público na página de Login dela (`/univag`), e a busca não devolve logo, cor, contato nem id. Se a lista for sigilosa, o plano troca para "só o código" (campo conferido por `GET /api/instituicoes/{slug}`) e a S1 sai | n |
| limite de taxa | 60 chamadas por minuto por IP, política nova `instituicoes` | uma turma no Wi-Fi do campus sai com um IP só. 40 alunos fazendo 2 a 6 buscas cada passam de 30 por minuto. A política `auth` (5 por minuto) bloquearia até um aluno sozinho. Cada aluno busca uma vez só, porque o app lembra | n |
| `localStorage` apagado pelo iOS | a escolha aparece de novo, sem perda de dado | o app da tela de início não compartilha armazenamento com o Safari (WebKit bug 181849), então a escolha aparece uma vez no primeiro uso do app instalado | n |

**Open questions:** none - all resolved or logged above.

## Observable

| Surface | Decision | Landing |
| --- | --- | --- |
| screen `Raiz` | empty state | AC 7 (sem texto), AC 11 (sem resultado) |
| screen `Raiz` | loading state | AC 10 |
| screen `Raiz` | error state | AC 12, AC 13 |
| screen `Raiz` | unauthorised state | n/a - tela pública, sem sessão |
| screen `Raiz` | density and ordering | AC 1 (ordem por nome), AC 2 (até 5) |
| screen `Raiz` | destructive action confirms | n/a - escolher instituição não apaga nada |
| screen `Raiz` celular 360×780 | layout | AC 21 |
| screen `Raiz` celular deitado 844×390 | layout | AC 23 |
| screen `Raiz` teclado aberto | layout | AC 22 |
| screen `Raiz` toque | alvos | AC 24 |
| screen `Login` | empty, loading, error, unauthorised | existing - sem mudança; AC 19 e AC 20 só acrescentam os links |
| screen `Login` celular 360×780, deitado 844×390, teclado aberto | layout | existing - o link entra no fim da coluna já rolável do formulário |
| screen `Login` toque | alvos | AC 24 |
| API `GET /api/instituicoes` | error shape and codes | AC 4 (ProblemDetails), AC 6 (`429`) |
| API `GET /api/instituicoes` | who may call | AC 1 (anônima, como `GET /api/instituicoes/{slug}`) |
| API `GET /api/instituicoes` | rate limits | AC 6 |
| API `GET /api/instituicoes` | versioning | n/a - a API não versiona rotas |

### As nove dimensões (resumo; o registro com checks vai em `checks.md` `## Swept`)

| Dimension | Landing |
| --- | --- |
| Validation and bounds | AC 4, AC 5, AC 2 |
| Failure and partial failure | AC 12, AC 13, AC 18 |
| Idempotency, retry, duplicates | n/a - leitura sem efeito; buscar de novo devolve o mesmo |
| Authorization and rate limits | AC 1, AC 2, AC 3, AC 6 (inclui a exposição dos nomes, ver Assumptions) |
| Concurrency and ordering | AC 8: só a resposta do último texto digitado aparece (a chave da consulta é o texto, pelo TanStack Query) |
| Data lifecycle | AC 17 (slug lembrado que deixou de existir), AC 19 (apagar ao trocar) |
| External-dependency failure | AC 12 (rede), AC 18 (`localStorage` bloqueado) |
| State transitions | AC 16, AC 17, AC 19 (lembrada -> escolha -> lembrada) |
| Observability | n/a - leitura pública sem efeito; o `429` já sai pelo limitador existente |

## Sources

- Relato do dono, 24/11/2026: o app instalado abre na raiz e não há como trocar de instituição; "o pwa só deixa instalar o site base"
- Decisão do dono, 24/11/2026: busca por nome (opção recomendada entre busca, só código e lista completa)
- Padrão de mercado: Canvas Student "Find my school" (busca por nome, URL como reserva) e Moodle app "Your site" (busca por nome ou URL) - [Canvas](https://community.canvaslms.com/docs/DOC-26648-955611233831), [Moodle](https://docs.moodle.org/404/en/Moodle_app_FAQ)
