# Papel do próprio gestor checks

Profile: standard

## Intent

Hoje o administrador de uma instituição consegue, pela tela de usuários, tirar o próprio papel `ADMINISTRADOR` (`PUT /api/admin/usuarios/{id}`) ou desativar a própria conta (`POST /api/admin/usuarios/{id}/desativar`), e a instituição pode ficar sem nenhum administrador ativo. Depois desta mudança a API recusa com `409` e mensagem em português: mudar o próprio conjunto de papéis, desativar a si mesmo e tirar o papel ou desativar o último administrador ativo. Editar o próprio nome, e-mail ou CPF com os mesmos papéis continua funcionando. Na tela, o administrador logado não vê os papéis editáveis nem "Desativar" na própria linha, com uma frase curta explicando; qualquer outro erro da API aparece como já aparece. A escolha entre `409` e `400` está feita: `409`, porque o corpo é válido e o conflito vem do estado (mesmo padrão de `ErrosDeAdmin.Repetido`). Sem `plan.md`: 4 arquivos de código, sem decisão difícil de desfazer (nenhum schema, contrato novo ou migration).

9 checks em 1 slice · 0 one-way doors · 0 perguntas abertas

**Como as provas rodam.** Na raiz `D:\esclari`, com `docker compose up -d postgres mailpit minio`. Um filtro que não seleciona teste sai com 0 (L-002): conferir a contagem no relatório. A regra do último administrador não é alcançável por HTTP, porque a rota exige um administrador ativo (`CookieDeSessao` recusa usuário inativo) e esse chamador sempre conta como outro administrador ativo; a prova é direta sobre `ValidacaoDeUsuario.UltimoAdministrador` com o banco real, e a regra vale como guarda contra corrida entre dois administradores.

## Checks

### S1 - guarda do próprio papel e do último administrador · 5 arquivos de código + 3 de teste · ~70 KB · ~18k

**C1** - `PUT /api/admin/usuarios/{id}` com `id` igual ao do chamador e `papeis` diferentes dos atuais como conjunto (`[ADMINISTRADOR]` para `[ALUNO]`) devolve `409` com `detail` "Você não pode mudar o próprio papel. Peça a outro administrador.", e os papéis no banco continuam `[ADMINISTRADOR]`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AdminTests.Admin_nao_muda_o_proprio_papel"`

**C2** - O mesmo `PUT` do chamador em si mesmo com os mesmos papéis em outra ordem (`[ATENDENTE, ADMINISTRADOR]` contra `[ADMINISTRADOR, ATENDENTE]`) e outro nome devolve `200` e grava o novo nome
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AdminTests.Admin_edita_o_proprio_nome_com_os_mesmos_papeis"`

**C3** - Um administrador muda os papéis de outro administrador para `[ALUNO]` e recebe `200`; o outro fica sem `ADMINISTRADOR` no banco
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AdminTests.Admin_muda_o_papel_de_outro_administrador"`

**C4** - `POST /api/admin/usuarios/{id}/desativar` com `id` igual ao do chamador devolve `409` com `detail` "Você não pode desativar a própria conta.", e `Ativo` continua `true`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AdminTests.Admin_nao_desativa_a_si_mesmo"`

**C5** - Um administrador desativa outro administrador e recebe `200`; o outro fica com `Ativo` `false`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AdminTests.Admin_desativa_outro_administrador"`

**C6** - `ValidacaoDeUsuario.UltimoAdministrador` para o único administrador ativo da instituição, quando ele deixaria de ser administrador (ou seria desativado), devolve um problema `409` com `detail` "A instituição precisa de ao menos um administrador ativo."; com um segundo administrador ativo devolve `null`; com o segundo administrador inativo devolve `409`; quando o alvo não é administrador ativo (inativo, ou sem o papel), devolve `null`; quando o alvo continua administrador, devolve `null`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~AdminTests.Ultimo_administrador_ativo_nao_se_perde"`

**C7** - Na tela, a linha do administrador logado (`me.id` igual a `usuario.id`) não oferece "Desativar" no menu (só "Editar") e a janela de edição dele não tem as caixas de papel, mostra a frase "Você não pode mudar o próprio papel. Peça a outro administrador." e envia os papéis atuais no `PUT`
Proof: `npm --prefix web test -- src/administracao/cadastros/usuarios.test.tsx -t "o próprio administrador"`

**C8** - Na tela, a linha de outro usuário continua com "Editar" e "Desativar", e a janela de edição dele continua com as três caixas de papel
Proof: `npm --prefix web test -- src/administracao/cadastros/usuarios.test.tsx -t "ações de usuário no menu"`
Proof: `npm --prefix web test -- src/administracao/cadastros/usuarios.test.tsx -t "editar troca papéis, setores e a senha temporária"`

**C9** - Quando a API devolve `409` com `detail` "A instituição precisa de ao menos um administrador ativo." ao salvar ou desativar, a tela mostra essa frase no alerta, sem fechar a janela de edição
Proof: `npm --prefix web test -- src/administracao/cadastros/usuarios.test.tsx -t "mostra o erro da API"`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| `PUT /api/admin/usuarios/{id}` statuses tocados (3) | 200 C2 · 200 de outro C3 · 409 C1 | - |
| `POST /api/admin/usuarios/{id}/desativar` statuses tocados (2) | 200 C5 · 409 C4 | - |
| decisão do `UltimoAdministrador` (5) | único admin ativo perde o papel C6 · segundo admin ativo C6 · segundo admin inativo C6 · alvo não é admin ativo C6 · alvo continua admin C6 | - |
| quem edita (2) | a si mesmo C1 C2 · a outro C3 | - |
| quem desativa (2) | a si mesmo C4 · a outro C5 | - |
| linha na tela (2) | a própria C7 · de outro C8 | - |
| origem do erro mostrado (1) | `409` do último administrador C9 | - |

- Afirmam status ou corpo da API: C1 a C5, todos com prova que passa pela rota HTTP; C6 tem prova direta porque a rota não alcança o caso (ver acima)
- C7 a C9 são afirmações de tela; o `409` simulado de C9 tem o mesmo formato do que C1, C4 e C6 afirmam

## Test policy

| Code | Required proofs | Coverage expectation |
| --- | --- | --- |
| Decide, atravessa a rota HTTP (`AlterarUsuarioHandler`, `DesativarUsuarioHandler`) | uma na rota | um caso por ramo novo: a si mesmo recusado, a si mesmo permitido com os mesmos papéis, a outro permitido |
| Decide, sem rota que o alcance (`UltimoAdministrador`) | uma na própria camada | um caso por linha da tabela de 5 |
| Decide, na tela (`LinhaUsuario`, `PapeisESetores`) | uma na própria camada | a própria linha e a de outro |

Evidence:

- `Esclari.Api/Administracao/Usuarios/AlterarUsuario.cs`: 2 desvios novos (próprio papel, último administrador) -> decide
- `Esclari.Api/Administracao/Usuarios/DesativarUsuario.cs`: 2 desvios novos (a si mesmo, último administrador) -> decide
- `Esclari.Api/Administracao/Usuarios/ValidacaoDeUsuario.cs`: `UltimoAdministrador` com 4 condições encadeadas e 1 consulta -> decide
- análogo no repositório: `ValidacaoDeUsuario.Repetido`, provado pela rota em `AdminTests.Cpf_ou_email_repetido_409`

Cost: 6 testes de API e 3 de tela novos, todos em arquivos existentes.

## Existing assertions changed

- `web/src/administracao/cadastros/usuarios.test.tsx`: a constante `admin` (`me({ papeis: ['ADMINISTRADOR'], matriculas: [] })`) passa a ter `id: 99`. O `me()` padrão tem `id: 1`, o mesmo da usuária Carla da fixture, e a tela agora trata a linha com o `id` do usuário logado como a própria. Nenhuma asserção muda: só o dado de entrada deixa de colidir.
- `Esclari.Api.Tests/ArquiteturaTests.cs`: a lista `Excecoes_Mensagem_sem_entidade` ganha `Administracao.Usuarios.AlterarUsuario`, porque a mensagem passa a levar o `Usuario` chamador, como já faz `DesativarUsuario`. Nenhuma outra regra de arquitetura muda.
- `web/src/administracao/cadastros/textos.test.tsx`: a mesma constante `admin` passa a ter `id: 99`, pelo mesmo motivo (a fixture `Carla` tem `id: 1`). Sem ela o teste `textos Usuários` falhava porque a linha da Carla virava a própria e perdia `Desativar`; nenhuma lista de textos muda.
- Outros que a suíte completa apontar entram aqui antes do commit de código.

## Swept

- validation: C1 (papéis diferentes como conjunto), C2 (mesmos papéis em outra ordem); o corpo inválido continua `400` pela `ValidacaoDeUsuario` existente, que roda antes das guardas novas
- failure modes: C1, C4, C6, C9
- idempotency: n/a - a guarda decide só pelo estado atual; repetir o mesmo `PUT` ou `POST` legítimo grava o mesmo resultado
- authorization: C1, C4 (o chamador vem da sessão por `ctx.Usuario()`, não do corpo); `AdminTests.Sem_papel_admin_403` continua valendo
- concurrency: C6 (a guarda do último administrador cobre dois administradores agindo ao mesmo tempo um sobre o outro); a corrida em si não é reproduzida em teste, porque a janela entre a consulta e o `SaveChanges` é de milissegundos e fechá-la exige serializar a transação, o que fica fora do escopo
- data lifecycle: n/a - nenhum dado novo; usuário desativado continua guardado como hoje
- dependency failure: n/a - nenhuma dependência nova
- state transitions: C4, C5, C6 (ativo para inativo; com e sem o papel `ADMINISTRADOR`)
- observability: n/a - sem log novo; a recusa é a resposta `409` da própria rota

## Out of scope

- Reativar usuário, criar usuário e importar matrículas (`CriarUsuario`, `ImportarMatriculas`) não mudam.
- Serializar a gravação para fechar a corrida entre dois administradores (transação serializável).
- Console da plataforma (`Plataforma/`) e o contrato do `PUT` de papéis de outras fatias.
- O trabalho paralelo em `.specs/features/tipos-de-usuario/`.

## Handoff

Um builder. Estimativa: `AlterarUsuario.cs` 2 KB + `DesativarUsuario.cs` 1.5 KB + `ValidacaoDeUsuario.cs` 3.5 KB + `ErrosDeAdmin.cs` 0.3 KB + `ArquiteturaTests.cs` ~90 KB lido por trechos + `AdminTests.cs` ~60 KB + `Usuarios.tsx` 9 KB + `usuarios.test.tsx` 8 KB, cerca de 175 KB lidos, ~44k tokens de leitura mais ~18k de escrita, ~62k, abaixo do budget de 150k. Sem pergunta de mecanismo. Base deste trabalho: o commit que congela este arquivo, gravado ao final do relatório.
