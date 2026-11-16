# Renomear One para Esclari

## Problem

O produto se chama "One" em código, banco, cookie, interface, e-mails, token MCP e protocolo (`ONE-n`). O dono decidiu o novo nome, Esclari (`esclari.com.br`), porque "One" não tem domínio nem marca registrável (`.specs/relatorio-nome.md`). Enquanto o nome antigo estiver no produto, aluno e equipe veem a marca errada e o protocolo `ONE-n` carrega o nome abandonado. O pedido também troca o formato do número de protocolo. Ainda não foi para produção, então não há chamado a converter (decisão do dono, 15/11/2026). Fonte não traz números de impacto.

Depois da entrega, nenhuma tela, e-mail, rota, identificador, variável de ambiente ou documento vivo cita "One", e todo chamado novo nasce com o protocolo no formato novo.

## Flow

Reaproveita o contador `instituicao.ultimo_numero_chamado` e o `AbrirChamado`; o rename é mecânico, o único ponto com lógica é a formatação do protocolo.

1. `AbrirChamado` (exists) - incrementa `ultimo_numero_chamado` e monta o protocolo no formato novo (door 1), persiste `Chamado.Numero`
2. `FerramentasMcp` (exists) e a busca de duplicado (exists) leem `Chamado.Numero` sem formatar
3. `Autenticado.tsx` (exists) e `ChamadoDetalhe.tsx` (exists) reconhecem o formato novo (regex `translate="no"` e placeholder)
4. `TokenMcp` (exists) gera e valida o prefixo novo (door 2)

## Impact

| Front | What changes |
| --- | --- |
| domain | termo `One` vira `Esclari` em: namespace `One.Api`, `One.Api.Tests`, `One.slnx`, `OneDb`, `OneDbModelSnapshot`, `OneDbDesignTime`, `OneFactory*`, `ConnectionStrings:One`, `UserSecretsId`, textos, e-mails, manifesto PWA, comando e nome do servidor MCP. Quem ramifica hoje: `ArquiteturaTests.cs` (namespaces), `web/src/test/estrutura.test.ts`, Dockerfile, `.dockerignore`, `.gitignore`, `web/scripts/entrada-sem-canvas.mjs`, `web/apresentacao/*`, `web/e2e/*` |
| domain | termo "protocolo/número do chamado": era `ONE-<n>`, passa a `ESC-<n com 6 dígitos>`. Quem ramifica: `Autenticado.tsx` (`/^ONE-\d+$/`), `ChamadoDetalhe.tsx` (placeholder), `MarcarDuplicado`, `FerramentasMcp` (descrição), `web/apresentacao/cenas/*` (regex), e2e |
| stored data | nome do banco `one`, bucket e usuário MinIO `one`, container `one-plataforma` mudam no compose; um banco/bucket já criado com o nome antigo fica órfão em dev |
| stored data | cookie `one.auth` vira `esclari.auth` (todos fazem login de novo uma vez); `localStorage` `one-tema` e `one-barra` viram `esclari-tema`/`esclari-barra` (preferência volta ao padrão uma vez) |
| stored data | tokens MCP `onemcp_*` já emitidos deixam de valer; precisam ser emitidos de novo |
| stored data | migrations já aplicadas: só identificadores mudam (namespace, `OneDb` e as strings de tipo no snapshot e nos Designer); nenhum `Up`/`Down` muda |

## Relations

None - no stored-data shape change

## Surface

Only routes this adds or whose signature changes.

| Route | In | Out | Status |
| --- | --- | --- | --- |
| `GET /api/chamados/{numero}` | `numero` no formato novo | chamado com `numero` novo | `200`, `404` |
| `POST /api/chamados/{numero}/duplicado` | `numeroOriginal` no formato novo | corpo vazio, e `numero_original` novo no chamado | `200`, `404`, `400` |

As rotas existem hoje com `ONE-n`; só o formato do segmento muda.

## Landing

| One-way door | Literal shape | Alternative rejected |
| --- | --- | --- |
| 1. Formato do protocolo | `ESC-` + contador por instituição preenchido com zeros a 6 dígitos: `ESC-000123`; `chamado.numero` segue `varchar(20)` | `ESC-AAAA-NNNNNN` com zeragem anual: exige contador por ano (coluna ou tabela nova) e muda a unicidade; mais caro de reverter |
| 2. Prefixo do token MCP | `esclarimcp_<slug>.<43 caracteres base64url>`; regex `^esclarimcp_...` | aceitar `onemcp_` e `esclarimcp_`: mantém "One" no código e dois formatos para sempre |
| 3. Identificadores persistidos fora do banco | cookie `esclari.auth`, `esclari-tema`, `esclari-barra`, banco/bucket/usuário `esclari`, `UserSecretsId` `esclari-api` | ler a chave antiga como fallback: mantém "One" no código e só adia a troca |

- Nada mais nesta mudança é difícil de desfazer

## Criteria

### S1: o produto se chama Esclari para quem usa (P1)

Aluno e equipe veem só "Esclari": telas, manifesto, e-mails e ferramentas MCP.

**Acceptance Criteria**

1. WHEN a tela de acesso abre THEN the system SHALL mostrar o título `Esclari` e nenhum texto `One`
2. WHEN o shell autenticado abre THEN the system SHALL mostrar `Esclari` na barra lateral e no cabeçalho
3. WHEN o app é compilado THEN the system SHALL gerar o manifesto PWA com `name` e `short_name` iguais a `Esclari` e o `<title>` igual a `Esclari`
4. WHEN o sistema envia e-mail de primeiro acesso, de administrador de instituição ou de entrada pelo Moodle THEN the system SHALL citar `Esclari` e usar o remetente padrão `nao-responda@esclari.com.br`
5. WHEN o usuário abre Integrações THEN the system SHALL mostrar os comandos `claude mcp add --transport http esclari ...` e o servidor `esclari` no JSON, e a atividade `Esclari` do Moodle
6. WHEN a lista de cores da aparência abre THEN the system SHALL chamar a cor padrão de `Azul Esclari`
7. WHEN o navegador grava o tema e a barra lateral THEN the system SHALL usar as chaves `esclari-tema` e `esclari-barra`
8. WHEN o login grava a sessão THEN the system SHALL emitir o cookie `esclari.auth`
9. WHEN um token MCP novo é emitido THEN the system SHALL gerar o prefixo `esclarimcp_` e rejeitar com `401` um token `onemcp_`

### S2: protocolo novo (P1)

Todo chamado tem um protocolo `ESC-NNNNNN`.

10. WHEN uma instituição abre o chamado de contador 123 THEN the system SHALL gravar `numero` igual a `ESC-000123`
11. WHEN a ferramenta MCP de abrir chamado é descrita THEN the system SHALL citar o exemplo `ESC-000012`
12. WHEN o campo de chamado original do duplicado abre THEN the system SHALL mostrar o placeholder `Ex.: ESC-000123…`
13. WHEN a equipe marca um chamado como duplicado de `ESC-000007` THEN the system SHALL aceitar o número e gravar `numero_original` igual a `ESC-000007`
14. WHEN o título do chamado `ESC-000123` é exibido THEN the system SHALL marcá-lo `translate="no"`
15. IF duas aberturas simultâneas ocorrem na mesma instituição THEN the system SHALL gravar dois números distintos (o `UPDATE ... RETURNING` atual continua sendo o único gerador)

### S3: nenhum "One" no código, na infra e nos documentos vivos (P1)

O repositório compila, testa e sobe sem o nome antigo.

16. The system SHALL compilar `Esclari.slnx` com os projetos `Esclari.Api` e `Esclari.Api.Tests` e o namespace `Esclari.Api`
17. WHEN `dotnet ef migrations has-pending-model-changes` roda THEN the system SHALL sair com código 0 (nenhuma mudança de modelo pendente)
18. WHEN `docker compose up -d` roda THEN the system SHALL criar o banco, o bucket e o usuário MinIO com o nome `esclari` e usar `ConnectionStrings__Esclari`
19. The system SHALL passar `dotnet test Esclari.Api.Tests`, `npm --prefix web test` e os gates `crapper` e `dryer` sem alterar nem apagar teste
20. The system SHALL ter zero ocorrências de `One`/`ONE`/`one` como nome do produto em arquivos rastreados, exceto em `.specs/features/**` fechadas, `.specs/lessons.json`, `.specs/LESSONS.md`, `.specs/relatorio-nome.md`, `.specs/prompt-*.md` e linhas já escritas do `STATE.md`
21. WHEN o `STATE.md` é lido THEN the system SHALL conter um AD novo (`AD-NNN`) que registra o nome Esclari, o protocolo `ESC-NNNNNN` e o prefixo `esclarimcp_`

## Out of scope

| Excluded | Why |
| --- | --- |
| Novo ícone e logotipo (`web/public/icons/*.png` é um anel branco sobre azul, a letra "O" do One) | Precisa de arte; entra em feature própria. O manifesto troca só o nome |
| Renomear a pasta `D:\one-system` e o remoto Git | Fora do repositório; o dono faz |
| Reescrever histórico `.specs` fechado (features, verificações, lições, relatório de nome) | Evidência de verificação cita caminhos antigos; reescrever invalida a prova. Esses arquivos mantêm o nome da época |
| Trocar a atividade "One" já instalada em cada Moodle | Configuração externa de cada instituição; só o texto do nosso produto muda |
| Aceitar token `onemcp_` e protocolo `ONE-n` por compatibilidade | Mantém o nome antigo no código (door 2) |
| Registro DNS, domínio e certificado de `esclari.com.br` | Infra; fora do código |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| Grafia da marca | `Esclari` (identificadores `esclari`, `Esclari`, protocolo `ESC`) | Nome do pedido; `ESC` tem 3 letras como `ONE` | n |
| Formato do protocolo | `ESC-000123` | Door 1; recomendação aceita (`user delegated`) | y |
| Documentos históricos em `.specs` | Não reescritos (S3 critério 20) | Preserva a evidência | n |
| Segredos de desenvolvimento | `UserSecretsId` vira `esclari-api`; o arquivo `secrets.json` da pasta antiga é copiado sem ser lido | Evita perder a chave de IA local | n |
| Remetente padrão | `nao-responda@esclari.com.br` | Domínio informado pelo dono | n |
| Domínios de teste | `one.teste` vira `esclari.teste`; `@e2e.one` vira `@e2e.esclari`; `@demo.one` vira `@demo.esclari` | Mantém o formato `.teste` | n |
| Edição das migrations aplicadas | Só identificadores (namespace, `OneDb`, nomes de tipo em string do snapshot e Designer); prova: critério 17 | Sem isso o projeto não compila e o EF veria entidades novas | n |

**Open questions:**

| # | Kind | Question | Until answered |
| --- | --- | --- | --- |
| 1 | open | Nenhuma: sem produção, sem chamados e sem ambiente implantado a renomear (resposta do dono) | nada |

## Observable

| Surface | Decision | Landing |
| --- | --- | --- |
| screen acesso, shell autenticado, integrações, aparência | texto da marca em todos os estados (carregando, erro, vazio) | AC 1, 2, 5, 6 |
| screen celular 360×780 | marca `Esclari` cabe no cabeçalho sem truncar | AC 2 (cabe: texto de 7 letras contra 3 do antigo; e2e `arranjo.spec.ts` mede a caixa) |
| screen celular deitado 844×390 | n/a - só troca de texto, sem mudança de arranjo | n/a - nenhum elemento novo |
| screen teclado aberto | n/a - nenhuma tela de entrada nova | n/a - nenhum campo novo |
| screen toque | n/a - nenhum alvo de toque novo | n/a - sem controle novo |
| API `GET /api/chamados/{numero}` e `POST .../duplicado` | formato e erros do número | AC 10, 13; `404` com ProblemDetails existente |
| manifesto PWA do app (`vite.config.ts`, `index.html`) | `name`, `short_name`, `<title>` | AC 3 |
| MCP `esclarimcp_` | token com prefixo antigo | AC 9 |
| tarefa/CLI de plataforma (`criar`) | texto de e-mail ao administrador | AC 4 |

## Sources

- `.specs/relatorio-nome.md` - por que "One" sai
- Pedido do dono (15/11/2026): produto vira Esclari (`esclari.com.br`), protocolo novo, uso de tlc-spec-lean, ponytail e subagentes
- `AGENTS.md` - SDD, migrations já aplicadas, perfil `standard`
