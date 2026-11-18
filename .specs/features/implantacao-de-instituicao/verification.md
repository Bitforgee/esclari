# Implantação de instituição verification

**Verdict**: FAIL
**Profile**: standard
**Diff range**: f2c6391..3ed7386
**Round**: 1 - full
**Verifier**: independent sub-agent (author != verifier)

Os 24 checks têm prova que roda e passa em `HEAD`, mas uma falha injetada sobreviveu: `ARTIGO_ATIVO` ignorando a validade (AC 23, C20) não derruba nenhum teste. Por isso o veredito é FAIL. Verificação enxuta por decisão do dono (18/11): uma rodada, 3 falhas injetadas no máximo, sem Playwright e sem `mutator` (AD-025).

## Checks

| Check | Claim | Proof run | Evidence | Result |
| --- | --- | --- | --- | --- |
| C1 | `simular=true` responde 200 com contagens e não grava | `dotnet test Esclari.Api.Tests` (suíte inteira), `Aprovado ImplantacaoCargaTests.Simular_conta_e_nao_grava` | `Esclari.Api.Tests/ImplantacaoCargaTests.cs:33` - `Assert.Equal(HttpStatusCode.OK, r.StatusCode)`; `:35-37` contagens 2, 1, 1; `:38-40` setor 1, categoria 1, auditoria 0 | PASS |
| C2 | arquivo inválido 400 com todos os erros, simular e importar, nada gravado | `Aprovado ...Arquivo_invalido_400_com_todos_os_erros` | `ImplantacaoCargaTests.cs:53` - `Assert.Equal(HttpStatusCode.BadRequest, ...)` nos dois modos; `:54` - `errors.arquivo.GetArrayLength() >= 2`; `:56` setores continua 1 | PASS |
| C3 | fora de UTF-8 responde 400 "O arquivo deve estar em UTF-8." | `Aprovado ...Arquivo_fora_de_utf8_400` | `ImplantacaoCargaTests.cs:69` - `Assert.Equal("O arquivo deve estar em UTF-8.", ...errors.arquivo[0])` | PASS |
| C4 | segunda importação cria 0 setor e 0 categoria, sem duplicar | `Aprovado ...Importar_duas_vezes_nao_duplica` | `ImplantacaoCargaTests.cs:83-85` - primeira 2 setores, segunda 0 setores e 0 categorias; `:86-87` totais 3 e 2 | PASS |
| C5 | `GET /modelos-de-carga` lista `padrao-esclari` 3/6/6 e 401 sem sessão | `Aprovado ...Modelos_listam_o_padrao_esclari` | `ImplantacaoCargaTests.cs:100` - `Assert.Equal([3, 6, 6], ...)`; `:101` - `Assert.Equal(HttpStatusCode.Unauthorized, ...)` | PASS |
| C6 | aplicar modelo grava, audita `CARGA_IMPORTADA`, 200; 404 para modelo ou slug desconhecido, sem artigo | `Aprovado ...Aplicar_modelo_grava_e_audita` | `ImplantacaoCargaTests.cs:114` - `Assert.Equal([3, 6, 6, 0], ...)`; `:115` artigos 0; `:116` auditoria 1; `:117-118` 404 nos dois casos | PASS |
| C7 | `GET .../setores` lista `id` e `nome` dos ativos; 404 para slug desconhecido | `Aprovado ImplantacaoEquipeTests.Setores_da_instituicao_alvo` | `Esclari.Api.Tests/ImplantacaoEquipeTests.cs:33` - `Assert.Equal(["Financeiro", "Triagem"], ...)`; `:35` - 404 | PASS |
| C8 | `POST /equipe` cria sem senha, e-mail na fila da instituição, `USUARIO_CRIADO`, 201 com `id` | `Aprovado ...Criar_membro_convida_e_audita` | `ImplantacaoEquipeTests.cs:48` - `Assert.Equal(HttpStatusCode.Created, ...)`; `:51` - `Assert.Null(usuario.SenhaHash)`; `:54` - `Assert.Single(emails)`; `:56` auditoria 1 | PASS |
| C9 | `papeis` vazio, `ALUNO`, `OPERADOR` ou desconhecido responde 400 em `papeis`, nada criado | `Aprovado ...Papeis_invalidos_400` | `ImplantacaoEquipeTests.cs:68-69` - 400 e `errors.papeis` nos 5 casos; `:71` - `Assert.Equal(0, ...Usuarios.CountAsync())` | PASS |
| C10 | `senha` e `senhaTemporaria` no corpo são ignorados | `Aprovado ...Senha_no_corpo_e_ignorada` | `ImplantacaoEquipeTests.cs:83` - `Assert.DoesNotContain("senha", ...)`; `:84` - `Assert.Null(...SenhaHash)` | PASS |
| C11 | atendente sem setor ou com setor de outra instituição responde 400 em `setores` | `Aprovado ...Atendente_exige_setor_da_propria_instituicao` | `ImplantacaoEquipeTests.cs:98-99` - 400 e `errors.setores` para vazio, setor de outra e id inexistente | PASS |
| C12 | CPF ou e-mail repetido 409; CPF ou e-mail inválido 400 | `Aprovado ...Repetido_409_e_invalido_400` | `ImplantacaoEquipeTests.cs:112-113` - `Conflict` por CPF e por e-mail; `:114-115` - `BadRequest` | PASS |
| C13 | `GET /equipe` só equipe, e-mail e CPF mascarados, sem aluno | `Aprovado ...Lista_so_equipe_mascarada` | `ImplantacaoEquipeTests.cs:129` - `Assert.Single(...)` com um aluno semeado; `:130` - `"***.456.789-**"`; `:131` - `^.\*\*\*@` | PASS |
| C14 | convite novo invalida o anterior e responde 204; quem já tem senha 409 | `Aprovado ...Convite_novo_invalida_o_anterior` | `ImplantacaoEquipeTests.cs:146` - `NoContent`; `:149-150` - token 0 inválido, token 1 válido; `:153` - `Conflict` | PASS (lacuna de precisão: ver Findings 2) |
| C15 | desativar marca inativo, `USUARIO_ALTERADO`, 204; último administrador 409 | `Aprovado ...Desativar_protege_o_ultimo_administrador` | `ImplantacaoEquipeTests.cs:163` - `Conflict` para o único; `:165` - `NoContent`; `:167` - `Assert.False(...Ativo)`; `:168` auditoria 1 | PASS |
| C16 | reativar marca ativo, `USUARIO_ALTERADO`, 204 | `Aprovado ...Reativar_volta_a_ativo` | `ImplantacaoEquipeTests.cs:182` - `NoContent`; `:183` - `Assert.True(...Ativo)`; `:184` auditoria 2 (desativar + reativar) | PASS |
| C17 | convite, desativar, reativar de aluno, de outra instituição e de id inexistente respondem 404 | `Aprovado ...Aluno_e_outra_instituicao_404` | `ImplantacaoEquipeTests.cs:196-198` - `NotFound` para 3 ids x 3 ações; `:199` aluno segue ativo | PASS |
| C18 | `resumo` tem `id` e papéis, nunca CPF nem e-mail | `Aprovado ...Resumo_sem_cpf_nem_email` | `ImplantacaoEquipeTests.cs:216` - 3 linhas; `:219-222` - contém `usuário {id}` e `ADMINISTRADOR, ATENDENTE`, não contém e-mail nem CPF; `:223` - sem padrão de CPF | PASS |
| C19 | sem sessão, todas as rotas novas respondem 401 | `Aprovado ...Sem_sessao_401_em_toda_rota_nova` | `ImplantacaoEquipeTests.cs:235` - as 9 rotas; `:238` - `Assert.Equal(HttpStatusCode.Unauthorized, ...)` | PASS |
| C20 | `prontidao` com os 4 itens conforme AC 23; `pronta` só com os 4 `ok` | `Aprovado ImplantacaoProntidaoTests.Quatro_itens_e_pronta` | `Esclari.Api.Tests/ImplantacaoProntidaoTests.cs:28` - ordem dos 4 códigos; `:29-30` todos falsos e `pronta` falso; `:33,46,50,56` cada item vira `true`; `:58-59` todos `ok` e `pronta` | PASS (lacuna de nível: ver Findings 1; o item `ARTIGO_ATIVO` não prova a validade) |
| C21 | `CONFIGURANDO`, `ATIVA`, `DESATIVADA` | `Aprovado ...Situacao_configurando_ativa_desativada` | `ImplantacaoProntidaoTests.cs:78` - `"CONFIGURANDO"`; `:83` - `"ATIVA"`; `:85` - `"DESATIVADA"` | PASS |
| C22 | depois de criar, abre o detalhe na aba "Implantação" com 4 itens, estado e botão do passo | `npm --prefix web test -- src/plataforma/implantacao.test.tsx -t "criar leva a implantacao com os quatro itens"` 1 passed, 3 skipped | `web/src/plataforma/implantacao.test.tsx:52` - `findByRole('tab', { name: 'Implantação', selected: true })`; `:56-61` - os 4 textos com `Feito`/`Pendente` e botões "Ir para a equipe inicial" e "Ir para a carga inicial" | PASS |
| C23 | passos "Carga inicial" e "Equipe inicial"; "Simular" antes de "Importar" com contagens ou erros | `... -t "carga inicial simula antes de importar"` 1 passed, 3 skipped | `implantacao.test.tsx:77` - `compareDocumentPosition(...) & DOCUMENT_POSITION_FOLLOWING`; `:83` - texto das contagens; `:84` - `expect(gravacoes(chamadas)).toEqual([])`; `:93-94` - os dois erros em linhas diferentes | PASS |
| C24 | equipe vazia com texto e "Novo usuário"; desativar pede confirmação com o nome | `... -t "equipe inicial vazia e desativar com confirmacao"` 1 passed, 3 skipped | `implantacao.test.tsx:101` - `'Nenhum administrador ou atendente além do primeiro. Crie a equipe para a instituição atender.'`; `:102` - botão "Novo usuário"; `:111` - `'Desativar Carla Atendente? Ele deixa de entrar na instituição até você reativar.'`; `:113` - Voltar não grava | PASS |

## Coverage

| Set (size) | Recomputed from | Member -> proof | Unproven |
| --- | --- | --- | --- |
| linhas da tabela `Surface` (9) | `Esclari.Api.Tests/Retratos/rotas.txt` (+8 linhas) e `ImplantacaoEquipeTests.cs:235` | modelos C5 · modelo C6 · carga C1 C2 C3 · setores C7 · `GET /equipe` C13 · `POST /equipe` C8 C9 C10 C11 C12 · convite C14 · desativar C15 · reativar C16 · detalhe C20 | - |
| status `POST .../equipe` (5) | `CriarMembro.cs`, `EquipeDaInstituicao.cs:39-58` | 201 C8 · 400 C9 C11 C12 · 401 C19 · 404 C17 (rota alvo desconhecida: `EquipeDaInstituicao.Alvo`, sem teste direto no POST) · 409 C12 | - |
| status convite, desativar, reativar (4) | `ConvidarMembro.cs`, `DesativarMembro.cs:18-22`, `ReativarMembro.cs` | 204 C14 C15 C16 · 401 C19 · 404 C17 · 409 C14 C15 | - |
| ações novas da auditoria (2) | `Esclari.Api/Dados/Operadores/AcoesDaPlataforma.cs` | `USUARIO_CRIADO` C8 C18 · `USUARIO_ALTERADO` C15 C16 C18 | - |
| itens da prontidão (4) | `ConsultaDasInstituicoes.cs` `Prontidao.De` | `ADMINISTRADOR_COM_SENHA` C20 · `ATENDENTE_NO_SETOR` C20 · `CATEGORIA_COM_REGRA` C20 · `ARTIGO_ATIVO` C20 só no caso feliz | `ARTIGO_ATIVO` dentro da validade: nenhum teste o prova (falha 3) |
| `situacao` (3) | `ConsultaDasInstituicoes.cs` `Situacao` | `CONFIGURANDO` C21 · `ATIVA` C21 · `DESATIVADA` C21 · precedência de `AGUARDANDO_PRIMEIRO_ACESSO` em `PlataformaSaudeTests.Situacao_nos_tres_valores` (diff `PlataformaSaudeTests.cs:188-190`) | - |

## Test policy rows

Nenhuma linha `Test policy` em `checks.md`. Vale a convenção do repositório (AGENTS.md): teste de integração bate na rota HTTP, e todos os checks de API acima o fazem.

## Faults injected

Isolamento: `git worktree add ..\esclari-verify-impl HEAD`, removido com `git worktree remove --force`. `git status --porcelain` do repositório real igual antes e depois (só `?? .tmp-capturas/`, anterior à verificação).

| Mutation | Location | Killed |
| --- | --- | --- |
| `PapeisPermitidos` passa a aceitar `ALUNO` | `Esclari.Api/Plataforma/EquipeDaInstituicao.cs:8` | yes - `ImplantacaoEquipeTests.Papeis_invalidos_400` com falha |
| remover o 409 do último administrador (`&& false &&`) | `Esclari.Api/Plataforma/DesativarMembro.cs:21` | yes - `ImplantacaoEquipeTests.Desativar_protege_o_ultimo_administrador` com falha (`Expected: Conflict, Actual: NoContent`), e também `ImplantacaoProntidaoTests.Quatro_itens_e_pronta` na linha 35 |
| `ARTIGO_ATIVO` ignora a validade (`a.valido_ate >= {hoje}` removido do SQL) | `Esclari.Api/Plataforma/ConsultaDasInstituicoes.cs:75` | **no** - `ImplantacaoProntidaoTests` (2 testes), `PlataformaSaudeTests` e `PlataformaInstituicoesTests`: 16 de 16 passam |

A primeira rodada rodou as três falhas juntas e a segunda falha mascarou a terceira; a terceira foi reinjetada sozinha, e foi essa que sobreviveu.

## Findings

1. **Falha 3 sobrevive** (bloqueia o PASS) - C20, AC 23: "ARTIGO_ATIVO como ao menos um artigo ativo e dentro da validade". `ImplantacaoProntidaoTests.cs:53-56` e `:79-82` só criam um artigo válido e esperam `true`; nenhum teste cria artigo vencido ou rascunho e espera `false`. O mesmo vale para os outros itens, que têm só o caso feliz: administrador ativo sem senha, atendente inativo ou sem setor, e a categoria "Não classificado" (`NOT c.padrao` em `ConsultaDasInstituicoes.cs:74`) não têm caso negativo. Correção sugerida (precisa de OK humano, porque os checks são congelados): um caso de artigo vencido com `ok: false` dentro de `Quatro_itens_e_pronta` ou teste novo ligado ao C20.
2. Lacuna de precisão em C14: o check diz "enfileira outro", mas `ImplantacaoEquipeTests.cs:147-150` só prova os dois tokens; nenhuma asserção conta o segundo e-mail em `EmailsSaida` após o convite (o C8 conta um e-mail, `:54`). Remover o enfileiramento do `ConvidarMembro` não derrubaria o teste (não injetado nesta rodada).
3. C8 diz "na mesma transação"; o teste não observa transação, só o resultado. Aceitável (não observável por HTTP).

## Landing, mudanças em asserções existentes e console-da-plataforma

- Door 1 (escrita entre fronteiras): `EquipeDaInstituicao.cs:8` limita os papéis a ADMINISTRADOR e ATENDENTE (falha 1 morreu); `CriarMembro` não lê senha (C10); setores validados contra a instituição alvo (`:50`, C11); `ArquiteturaTests` ganha `CriarMembro` na lista de entradas e `PlataformaTests.InstituicaoAtual_so_onde_ja_era_e_na_plataforma` troca a lista nominal por `!p.StartsWith("Plataforma/")` (`PlataformaTests.cs:449`), como declarado em `## Existing assertions changed`. Isso afrouxa a regra de nomes dentro da pasta `Plataforma/`, mas a regra para o resto do projeto fica igual (`fora` continua esperando só `TempoRealHub`, `AutenticacaoMcp`, `ContextoMcp`). Sem enfraquecimento.
- Door 2 (auditoria): `AcoesDaPlataforma.Todas` passa de 8 para 10 (`PlataformaAuditoriaTests.cs:167`) e o teste de ciclo grava as duas ações novas com `InstituicaoAlvoId` do alvo (`:39-41`, `:50`); `Resumo_sem_dado_pessoal_e_ate_200_caracteres` ganha a criação e a desativação (`:102-104`). Obrigações mantidas.
- Mudanças listadas e conferidas no diff: contagens de `ArquiteturaTests`, `MediatorTests`, `RetratoTests`, `SessaoTests` (+8 casos de uso e +8 rotas, todos ligados à feature); `PlataformaSaudeTests.cs:188-190` troca `ATIVA` por `CONFIGURANDO` e mantém `AGUARDANDO_PRIMEIRO_ACESSO` e `DESATIVADA`; `PlataformaTests.cs:141` ganha `prontidao`. Todas declaradas em `checks.md`. A mudança de `checks.md` depois da base (`f2c6391`) só acrescenta a lista de asserções alteradas e o SHA da base (`git diff f2c6391..HEAD -- checks.md`, 6 linhas); nenhum check foi editado.
- `console-da-plataforma` C32: `npm --prefix web test -- src/plataforma/instituicoes.test.tsx -t "criar leva a importar a carga na mesma tela"` roda (1 passed, 11 skipped) e passa. O teste "criar fecha, avisa e abre a instituição criada" (`instituicoes.test.tsx`, diff) troca a espera da linha na lista pela do `heading` e da aba "Implantação", conforme AC 25; o aviso e o link continuam asseridos.

## Gate

`dotnet build Esclari.slnx` - 0 erros, 11 avisos (exit 0)
`dotnet test Esclari.Api.Tests` - 1291 passed, 0 failed
`cd web; npx vitest run` - 1142 passed, 0 failed
`npm --prefix web test -- src/plataforma/implantacao.test.tsx -t "<nome>"` (3 provas) - 1 passed, 3 skipped cada
`npm --prefix web test -- src/plataforma/instituicoes.test.tsx -t "criar leva a importar a carga na mesma tela"` - 1 passed, 11 skipped
