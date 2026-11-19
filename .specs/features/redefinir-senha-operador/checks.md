# Redefinir a senha de um operador da plataforma - checks

Profile: standard
Plan: none - mudança em 2 arquivos, sem decisão difícil de desfazer

## Intent

O comando `plataforma operador --email <e-mail>` só emite link para quem ainda não tem senha; para um operador que já tem, responde "Esse operador já existe." Quem esquece a senha do único operador fica sem acesso ao console, e o único caminho hoje seria gravar um convite direto no banco, sem auditoria. Depois disto, `plataforma operador --email <e-mail> --redefinir-senha` imprime um link de 60 minutos para o operador escolher uma senha nova, grava a ação na auditoria da plataforma, e o segundo fator cadastrado continua valendo.

3 checks de comportamento + 2 de proteção · 0 one-way doors · 0 perguntas abertas (pedido do dono do projeto: "eu não lembro a senha")

## Checks

### S1 - link de nova senha · 2 arquivos · 20 KB · ~5k

**C1** - `operador --email <e-mail> --redefinir-senha` de um operador que já tem senha sai com 0, imprime `http://localhost:8080/plataforma/definir-senha?token=<token>`, grava `ConviteExpiraEm` em agora + 60 minutos, e o token aceito em `POST /api/plataforma/auth/definir-senha` (204) deixa o login com a senha nova abrir a etapa de código (200)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaComandoTests.Redefinir_senha_imprime_o_link_de_60_minutos_e_a_senha_nova_entra"`

**C2** - Depois de usar o link, o operador continua com o segundo fator: `TotpSegredo` segue preenchido e `TemSegundoFator` é verdadeiro
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaComandoTests.Redefinir_senha_mantem_o_segundo_fator"`

**C3** - O comando grava exatamente 1 linha de auditoria `OPERADOR_ALTERADO` para o operador, com o texto `Comando: novo link de senha para <nome>`
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaComandoTests.Redefinir_senha_audita_o_comando"`

**C4** - Pedir um link novo invalida o anterior: o primeiro token devolve 400 em `definir-senha` e o segundo devolve 204
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaComandoTests.Redefinir_senha_invalida_o_link_anterior"`

**C5** - Com e-mail que não existe, sai com 1 e escreve `--email: não há operador com esse e-mail`; sem e-mail válido, sai com 1 e escreve `--email: informe um e-mail válido`; em nenhum dos dois casos grava auditoria nem convite
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaComandoTests.Redefinir_senha_recusa_email_desconhecido_e_invalido"`

**C6** - As ações que já existiam continuam iguais: criar/convidar, desativar, reativar e redefinir o 2FA, com as recusas
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~PlataformaComandoTests"`

**C7** - O build não quebra por complexidade ciclomática (CA1502, AD-018)
Proof: `dotnet build Esclari.slnx`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| entradas do comando com `--redefinir-senha` (3) | operador existente C1 · e-mail desconhecido C5 · e-mail inválido C5 | - |
| efeitos no operador (4) | convite de 60 minutos C1 · senha nova entra C1 · 2FA preservado C2 · convite anterior invalidado C4 | - |
| ações antigas do comando (4) | criar C6 · convidar de novo C6 · desativar/reativar C6 · redefinir 2FA C6 | - |

- Startup config: n/a - o comando não depende de configuração nova; `Program.cs` já o despacha para `PlataformaComando.Executar`, o mesmo ponto que os testes chamam.
- C1 a C5 passam pela entrada real do comando (`PlataformaComando.Executar`) e, onde o link é usado, pela rota HTTP `definir-senha`.

## Test policy

| Code | Required proofs | Coverage expectation |
| --- | --- | --- |
| `ComandoOperadorDaPlataforma.Alterar` decide por ação | uma na própria camada, atravessando `PlataformaComando.Executar` | um caso por ação e por recusa |

Evidence:

- `PlataformaComando.cs`: `Alterar` decide por 4 ações (3 existentes e a nova) e 3 recusas; mesma forma que `PlataformaComandoTests.Desativar_reativar_e_redefinir_2fa_com_as_recusas` já prova.

Cost: 5 testes novos em `PlataformaComandoTests.cs`.

## Swept

- validation: C5
- failure modes: C5
- idempotency: C4 (cada pedido substitui o convite anterior)
- authorization: existing - o comando só roda no servidor, por quem tem acesso ao contêiner; não há rota nova
- concurrency: n/a - comando manual de uma pessoa, e `NovoConvite` grava o convite inteiro numa só linha
- data lifecycle: C1 (o convite vence em 60 minutos e `DefinirSenha` o apaga)
- dependency failure: n/a - o comando não chama serviço externo; não envia e-mail
- state transitions: C2
- observability: C3

## Out of scope

- Enviar o link por e-mail - o comando imprime, como o de criação; quem roda o comando entrega o link.
- Encerrar sessões abertas do operador ao trocar a senha - não existe hoje para nenhum fluxo; fica para quando alguém pedir.
- Redefinir a senha pelo próprio console ("esqueci a senha") - exige rota pública e e-mail; outro assunto.

## Handoff

- Um construtor: `PlataformaComando.cs` (4 KB) e `PlataformaComandoTests.cs` (8 KB) ≈ 3k, abaixo do orçamento de 150k.
- Mechanism: one builder
