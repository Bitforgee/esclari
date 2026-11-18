# Implantação de uma instituição pelo console: carga, equipe inicial e prontidão

Depende de `console-da-plataforma` (sessão da plataforma, tela de detalhe, auditoria). A etapa de parâmetros usa `parametros-por-instituicao` quando ela existir; sem ela, o passo é pulado. A marca padrão vem de `marca-padrao-esclari` e não entra aqui: a gestão troca a identidade depois, pela tela Aparência.

## Problem

Colocar uma instituição para funcionar hoje exige o servidor ou pedaços soltos. A criação (`instituicao criar`) e a carga (`instituicao carga`) têm comando; a criação e a carga pela tela existem só para o papel `OPERADOR`. Os demais usuários (atendentes, outros administradores) só nascem quando o administrador da instituição entra e os cadastra um a um, com senha temporária que ele digita (`CriarUsuario.Entrada.SenhaTemporaria`). A carga só diz o que está errado depois de tentar importar, não há um arquivo-modelo, e ninguém vê o que falta para a instituição estar pronta para os alunos. Quem paga é a equipe do Esclari, que repete esse roteiro à mão em cada cliente, e o cliente, que recebe um sistema vazio sem saber o que fazer primeiro. Evidência no código: `ComandoCriar`, `ComandoCarga`, `ImportarCargaInstituicao` (devolve só as contagens ou o erro), `CriarUsuario` e a ausência de qualquer rota de equipe em `Esclari.Api/Instituicoes/Plataforma/`. O pedido é do dono, em 18/11/2026. A fonte não traz números.

Depois desta feature, o operador implanta um cliente inteiro pelo console: cria a instituição e o administrador, aplica um modelo de carga ou envia o arquivo depois de simular o resultado, cria os administradores e atendentes iniciais já vinculados à instituição e aos setores, e vê uma lista do que falta para ela estar pronta. Ninguém digita senha de outra pessoa: cada usuário recebe um convite por e-mail e escolhe a sua. O operador nunca vê nem cria alunos.

## Flow

Reusa `CriarInstituicao` e `Criacao` (instituição, setor "Triagem", categoria "Não classificado", administrador e e-mail de acesso na mesma transação), `ImportarCargaInstituicao` e `CargaValidacao`, `ValidacaoDeUsuario`, `TokenAcesso` e a fila `EmailSaida` da instituição com o layout de `emails-com-marca`, mais a sessão da plataforma, o detalhe e a auditoria do `console-da-plataforma`.

1. operador em `/plataforma/instituicoes/nova` -> `POST /api/plataforma/instituicoes` (exists) - cria a instituição e o primeiro administrador, e leva ao detalhe na aba "Implantação"
2. `ModelosDeCarga` (new, no door - placement per conventions) - lista e aplica modelos de carga embutidos no projeto
3. `ImportarCargaInstituicao` (exists) - ganha `simular`, que valida e conta sem gravar
4. `Plataforma/Equipe` (new, door 1) - cria, convida de novo, desativa e reativa administradores e atendentes de uma instituição, escrevendo na instituição alvo; o convite sai pela fila da instituição
5. `ProntidaoDaInstituicao` (new, no door - placement) - calcula, por SQL de agregação, os quatro itens da lista de prontidão
6. `Plataforma/Auditoria` (exists after `console-da-plataforma`, door 2) - grava `USUARIO_CRIADO` e `USUARIO_ALTERADO`
7. out: JSON da rota, e-mail de primeiro acesso do usuário na fila da instituição, e a linha de auditoria

## Impact

| Front | What changes |
| --- | --- |
| domain | novo termo: `equipe inicial` - os administradores e atendentes que o operador cria pelo console; o papel é sempre `ADMINISTRADOR` ou `ATENDENTE`, nunca `ALUNO` |
| domain | novo termo: `prontidão` - quatro itens calculados na leitura que dizem se a instituição já pode atender; nunca é gravada |
| domain | termo existente: `situacao` da lista (`console-da-plataforma`) ganha o valor `CONFIGURANDO`: instituição ativa com a prontidão incompleta. Quem ramifica nele: a lista e o detalhe do console |
| domain | termo existente: `CriarUsuario` do administrador da instituição continua como está, com senha temporária; a rota do operador não aceita senha |
| API | rotas novas só sob `/api/plataforma`; `POST /api/plataforma/instituicoes/{slug}/carga` ganha o parâmetro `simular`; nenhuma rota de instituição muda de corpo |
| stored data | nenhuma tabela nem coluna nova; o operador grava `usuario`, `usuario_papel` e `atendente_setor` da instituição alvo pelos mesmos casos de gravação que o administrador já usa |
| tests | `ArquiteturaTests` ganha a regra da door 1 (só a fatia `Plataforma` escreve em outra instituição) |
| docs | AD novo no `STATE.md` que fixa a regra de escrita entre fronteiras; `GLOSSARIO.md` |

## Relations

None - no stored-data shape change.

## Surface

Todas sob `/api/plataforma`, esquema `Plataforma`.

| Route | In | Out | Status |
| --- | --- | --- | --- |
| `GET /modelos-de-carga` | vazio | lista de `id`, `nome`, `descricao`, `setores`, `categorias`, `regras` | `200`, `401` |
| `POST /instituicoes/{slug}/carga/modelo/{id}` | vazio | `setores`, `cursos`, `categorias`, `regras`, `artigos` criados | `200`, `401`, `404` |
| `POST /instituicoes/{slug}/carga?simular=true` (a rota já existe) | arquivo de carga | as mesmas contagens do que seria criado, sem gravar | `200`, `400`, `401`, `404` |
| `GET /instituicoes/{slug}/setores` | vazio | lista de `id`, `nome` | `200`, `401`, `404` |
| `GET /instituicoes/{slug}/equipe` | vazio | lista de `id`, `nome`, `emailMascarado`, `cpfMascarado`, `papeis`, `ativo`, `primeiroAcessoPendente` | `200`, `401`, `404` |
| `POST /instituicoes/{slug}/equipe` | `nome`, `cpf`, `email`, `papeis`, `setores` | `id` | `201`, `400`, `401`, `404`, `409` |
| `POST /instituicoes/{slug}/equipe/{id}/convite` | vazio | vazio | `204`, `401`, `404`, `409` |
| `POST /instituicoes/{slug}/equipe/{id}/desativar` e `/reativar` | vazio | vazio | `204`, `401`, `404`, `409` |
| `GET /instituicoes/{slug}` (já existe no console) | vazio | ganha `prontidao` (`itens` com `codigo` e `ok`, e `pronta`) | `200`, `401`, `404` |

## Landing

| One-way door | Literal shape | Alternative rejected |
| --- | --- | --- |
| 1. escrita entre fronteiras | as rotas `.../equipe` aceitam só `papeis` dentro de `["ADMINISTRADOR","ATENDENTE"]`, recusam qualquer senha no corpo, só vinculam a `setores` da própria instituição alvo, e o usuário nasce sem senha com o convite na fila de e-mail da instituição; `ArquiteturaTests` exige que só as classes de `Esclari.Api/Plataforma/` atribuam `InstituicaoAtual` a partir do `slug` da rota | entrar como administrador da instituição (personificação) para usar as telas dela: o operador passaria a ver tudo, inclusive alunos, que o dono não quer; reaproveitar `CriarUsuario` com `SenhaTemporaria`: o operador conheceria a senha de outra pessoa |
| 2. ações novas da auditoria | acrescenta `USUARIO_CRIADO` e `USUARIO_ALTERADO` à lista fechada de ações (door 4 do `console-da-plataforma`), com `resumo` de `id` e papéis do usuário, sem CPF nem e-mail | uma ação por mudança (criar, convidar, desativar, reativar): quatro valores a manter por uma única família de eventos |

- Nada mais nesta mudança é difícil de desfazer. Os nomes dos itens da prontidão, o conteúdo do modelo e os textos mudam por refatoração.

## Criteria

### S1: Carga assistida (P1)

**Acceptance Criteria**

1. WHEN `POST /api/plataforma/instituicoes/{slug}/carga?simular=true` recebe um arquivo válido THEN the system SHALL responder `200` com as contagens do que seria criado e não gravar nada, nem auditoria
2. IF o arquivo é inválido THEN the system SHALL responder `400` com todos os erros encontrados, cada um com o campo ou a linha, e não gravar nada, tanto na simulação quanto na importação
3. IF o arquivo não está em UTF-8 THEN the system SHALL responder `400` com a frase "O arquivo deve estar em UTF-8."
4. WHEN o mesmo arquivo é importado duas vezes THEN the system SHALL criar o conteúdo na primeira e `0` itens novos na segunda, sem duplicar setor, categoria, regra, curso nem artigo
5. WHEN `GET /api/plataforma/modelos-de-carga` é chamado THEN the system SHALL listar os modelos embutidos no projeto, com `id`, `nome`, `descricao` e as contagens de setores, categorias e regras
6. WHEN `POST /api/plataforma/instituicoes/{slug}/carga/modelo/{id}` é chamado THEN the system SHALL aplicar o modelo como uma carga comum, gravar `CARGA_IMPORTADA` na auditoria e responder `200` com as contagens
7. IF o `id` do modelo ou o `slug` não existe THEN the system SHALL responder `404`
8. The system SHALL entregar o modelo `padrao-esclari` com 3 setores, 6 categorias e uma regra de roteamento por categoria, e nenhum artigo

### S2: Equipe inicial (P1)

**Acceptance Criteria**

9. WHEN `GET /api/plataforma/instituicoes/{slug}/setores` é chamado THEN the system SHALL responder `200` com `id` e `nome` dos setores ativos da instituição
10. WHEN `POST /api/plataforma/instituicoes/{slug}/equipe` recebe `nome`, `cpf` válido, `email` válido, `papeis` e `setores` válidos THEN the system SHALL criar o usuário na instituição alvo sem senha, enfileirar o e-mail de primeiro acesso (link de uso único, válido por 60 minutos) na fila da instituição na mesma transação, gravar `USUARIO_CRIADO` e responder `201` com o `id`
11. IF `papeis` está vazio, ou traz `ALUNO`, `OPERADOR` ou um valor desconhecido, THEN the system SHALL responder `400` com o campo `papeis`
12. IF o corpo traz um campo de senha, THEN the system SHALL ignorá-lo e nunca gravar nem devolver senha
13. WHEN `papeis` inclui `ATENDENTE` THEN the system SHALL exigir ao menos um setor da instituição alvo; IF algum `setor` não pertence a ela THEN the system SHALL responder `400` com o campo `setores`
14. IF o CPF ou o e-mail já existe na instituição alvo THEN the system SHALL responder `409`; IF o CPF ou o e-mail é inválido THEN `400`
15. WHEN `GET /api/plataforma/instituicoes/{slug}/equipe` é chamado THEN the system SHALL listar só usuários com papel `ADMINISTRADOR` ou `ATENDENTE`, com `emailMascarado` (primeira letra, `***`, domínio) e `cpfMascarado` (`***.456.789-**`), e nenhum aluno, mesmo que a instituição tenha alunos
16. WHEN `POST .../equipe/{id}/convite` é chamado para um usuário que ainda não definiu a senha THEN the system SHALL invalidar o convite anterior, enfileirar um novo e responder `204`; IF ele já definiu a senha THEN the system SHALL responder `409`
17. WHEN `POST .../equipe/{id}/desativar` é chamado THEN the system SHALL marcar o usuário como inativo, gravar `USUARIO_ALTERADO` e responder `204`; IF ele é o último administrador ativo da instituição THEN the system SHALL responder `409`
18. WHEN `POST .../equipe/{id}/reativar` é chamado THEN the system SHALL marcar o usuário como ativo, gravar `USUARIO_ALTERADO` e responder `204`
19. IF o `id` não é de um usuário `ADMINISTRADOR` ou `ATENDENTE` da instituição alvo THEN the system SHALL responder `404` em convite, desativar e reativar, mesmo que o `id` seja de um aluno ou de outra instituição
20. The system SHALL gravar o `resumo` de `USUARIO_CRIADO` e `USUARIO_ALTERADO` com o `id` e os papéis do usuário, sem CPF nem e-mail
21. IF não há sessão da plataforma THEN the system SHALL responder `401` em todas as rotas da equipe, da carga e dos modelos

### S3: Prontidão e implantação guiada (P1)

**Acceptance Criteria**

22. WHEN `GET /api/plataforma/instituicoes/{slug}` é chamado THEN the system SHALL devolver `prontidao` com os itens `ADMINISTRADOR_COM_SENHA`, `ATENDENTE_NO_SETOR`, `CATEGORIA_COM_REGRA` e `ARTIGO_ATIVO`, cada um com `ok`, e `pronta` verdadeiro só quando os quatro estão `ok`
23. The system SHALL definir `ADMINISTRADOR_COM_SENHA` como ao menos um administrador ativo com senha definida; `ATENDENTE_NO_SETOR` como ao menos um atendente ativo ligado a um setor; `CATEGORIA_COM_REGRA` como ao menos uma categoria, fora "Não classificado", com regra de roteamento; `ARTIGO_ATIVO` como ao menos um artigo ativo e dentro da validade
24. WHEN a instituição está ativa e `pronta` é falso THEN the system SHALL devolver `situacao` igual a `CONFIGURANDO`; WHEN está ativa e `pronta` THEN `ATIVA`; WHEN está desativada THEN `DESATIVADA`
25. WHEN o operador termina de criar a instituição na tela THEN the system SHALL levar ao detalhe na aba "Implantação", com os quatro itens, o estado de cada um e, ao lado de cada item pendente, o botão que leva ao passo que o resolve (carga, equipe, ou aviso de que o artigo é cadastrado pelo administrador da instituição)
26. WHEN o operador abre a aba "Implantação" THEN the system SHALL mostrar os passos "Carga inicial", "Parâmetros" (só quando `parametros-por-instituicao` existe) e "Equipe inicial", cada um podendo ser deixado e retomado depois
27. WHEN o operador escolhe um modelo ou um arquivo no passo "Carga inicial" THEN the system SHALL oferecer "Simular" antes de "Importar" e mostrar as contagens ou a lista de erros
28. WHEN a equipe inicial está vazia THEN the system SHALL mostrar o estado vazio "Nenhum administrador ou atendente além do primeiro. Crie a equipe para a instituição atender." com o botão "Novo usuário"
29. WHEN o operador desativa um usuário THEN the system SHALL pedir confirmação com o nome dele

## Out of scope

| Excluded | Why |
| --- | --- |
| Criar, importar ou ver alunos e matrículas | o operador nunca vê aluno; a gestão da instituição importa a matrícula por CSV |
| Aplicar identidade visual, contatos ou textos da instituição pelo console | a marca nasce com o padrão do Esclari (`marca-padrao-esclari`) e a gestão troca |
| Cadastrar artigos ou fluxos de outra instituição | escrever conteúdo no interior da instituição quebra o isolamento; o administrador dela cadastra, e o modelo não traz artigo (AD-012: só vale o que a instituição publica) |
| Alterar CPF de um usuário, transferir a posse, excluir usuário | nada se apaga; CPF é a chave de acesso |
| Senha definida pelo operador | o convite por e-mail resolve, e o operador nunca conhece a senha |
| Teste de atendimento ponta a ponta automático antes de ativar | a prontidão mede o que existe, não o comportamento do assistente |
| Validade do convite diferente de 60 minutos | o prazo do primeiro acesso é o do produto todo; a pesquisa cita 7 dias e merece decisão do dono |
| Editor de modelos de carga | o modelo é um recurso do projeto; mudar é um commit |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| O operador cria usuários na instituição | só administradores e atendentes, por convite | pedido do dono em 18/11; a pesquisa recomenda lista fechada de papéis e convite no lugar de senha | y |
| A parametrização inicial é carga, equipe e (depois) prazos | a identidade fica com o padrão do Esclari | pedido do dono em 18/11 | y |
| Convite | link de uso único válido por 60 minutos, reenviável | é o prazo do primeiro acesso do produto; WorkOS e Auth0 usam 7 dias | n |
| Convite pela fila da instituição | o e-mail sai com a marca e o nome da instituição | o usuário é da instituição, e a fila já tem tentativas e marca | n |
| Modelo `padrao-esclari` | 3 setores (Secretaria Acadêmica, Financeiro, Coordenação de Curso), 6 categorias (Boleto e mensalidade, Matrícula e rematrícula, Documentos e declarações, Calendário e provas, Estágio e TCC, Ouvidoria), uma regra por categoria | cobre o que o roadmap descreve para a Univag, sem artigo | n |
| Mascaramento | e-mail e CPF mascarados na lista, e CPF só digitado na criação | o operador precisa informar o CPF, que é a chave de acesso | n |
| Idempotência da carga | a importação ignora o que já existe pelo nome | `Existentes` já faz isso em `CargaValidacao` | n |

**Open questions:**

| # | Kind | Question | Until answered |
| --- | --- | --- | --- |
| 1 | blocks go-live | qual SMTP envia os convites (pergunta 4 do roadmap) | o Mailpit local recebe tudo |
| 2 | open | o conteúdo exato do modelo `padrao-esclari` (setores, categorias e regras) | vale o da tabela de premissas |

## Observable

| Surface | Decision | Landing |
| --- | --- | --- |
| screen "Implantação" | empty state | AC 28 |
| screen "Implantação" | loading state | existing - botão desabilitado enquanto envia |
| screen "Implantação" | error state | AC 2 (lista de erros), AC 14 |
| screen "Implantação" | unauthorised | AC 21 |
| screen "Implantação" | destructive action confirms | AC 29 |
| screen "Implantação" | density and ordering | AC 25 |
| API all new routes | error shape and codes | existing - ProblemDetails de `Problemas.cs`, status por critério |
| API all new routes | who may call | AC 21 |
| API all new routes | rate limit | n/a - rotas internas da equipe, atrás de sessão com segundo fator |
| API all new routes | versioning | n/a - o produto não versiona rotas |
| documento e-mail de primeiro acesso | structure, next step | existing - layout de `emails-com-marca` (título "Crie a sua senha" e botão) |
| celular 360×780 | listas em cartões, botões de 44 px | AC 25, AC 28 |
| celular deitado 844×390 | n/a - a aba rola dentro da página | n/a - sem layout dependente da altura |
| teclado aberto | os campos do formulário de usuário continuam visíveis | AC 10 |
| toque | alvos de 44 px ou mais | AC 25, AC 29 |

## Sources

- `Esclari.Api/Instituicoes/Plataforma/` e `Esclari.Api/Instituicoes/Carga/` - o que já existe de criação e de carga; `Administracao/Usuarios/CriarUsuario.cs` - a senha temporária que o operador não terá
- `.specs/features/console-da-plataforma/plan.md` - sessão, detalhe, auditoria e `situacao`
- Pedido do dono, 18/11/2026: criar instituições e usuários, vincular ao instituto e fazer a parametrização inicial pelo painel
- [Freshdesk: guia de início para administradores](https://support.freshdesk.com/en/support/solutions/articles/195383-the-getting-started-guide-for-admins) - mínimo obrigatório pequeno, o resto incremental; base do passo a passo da implantação
- [Atlassian JSM: modelos de projeto](https://confluence.atlassian.com/servicemanagementserver0516/setting-up-your-service-project-1364558366.html) - ponto de partida por modelo, só nome e chave obrigatórios
- [Microsoft: ciclo de vida do locatário](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/considerations/tenant-lifecycle) e [WebCheatSheet: onboarding de locatário](https://webcheatsheet.com/saas/saas-customer-onboarding) - estado pendente até validar a configuração, etapas gravadas antes da seguinte, repetir não duplica
- [Zendesk: importação em massa de usuários](https://support.zendesk.com/hc/en-us/articles/10077720640026-Bulk-importing-user-data-legacy) - ordem de dependência e relatório de erros por linha
- [Auth0: convites de organização](https://auth0.com/docs/manage-users/organizations/configure-organizations/send-membership-invitations), [WorkOS: membros e papéis](https://workos.com/docs/dashboard/members-and-roles) e [Stripe: links de conta](https://docs.stripe.com/api/account_links/create) - convite com link de uso único, papéis na criação, reenvio e revogação; o operador nunca define a senha de outra pessoa
- [OWASP: segurança multi-tenant](https://cheatsheetseries.owasp.org/cheatsheets/Multi_Tenant_Security_Cheat_Sheet.html) - caminho entre locatários explícito, autorizado à parte e auditável; dados sensíveis mascarados
- [ANPD: agentes de tratamento](https://www.gov.br/anpd/pt-br/assuntos/titular-de-dados) - a instituição é a controladora e o Esclari age por instrução dela
