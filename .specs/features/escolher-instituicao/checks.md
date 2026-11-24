# Escolher a instituição no app instalado - checks

Profile: ui
Plan: `.specs/features/escolher-instituicao/plan.md`

14 checks em 4 slices · 2 one-way doors · 0 perguntas abertas (plano aprovado pelo usuário em 24/11: "aprovado pode construir")

**Como as provas rodam.** Na raiz `D:\esclari`. API: `docker compose up -d postgres mailpit minio` e uma execução de `dotnet test` por vez. Vitest: `npx --prefix web vitest run <arquivo> -t "<nome>"` a partir de `web/`. Um filtro que não seleciona teste sai com 0 (L-002): conferir a contagem. Mutação suspensa (AD-025).

## Checks

### S1 - API de busca

**C1** - [x] `GET /api/instituicoes?busca=varzea` anônimo responde `200` com `itens` de `{slug, nome, nomeCurto}` e nada mais, achando "Várzea" por `nome`, `nomeCurto` ou `slug`, ordenado por `nome`, sem diferenciar caixa nem acento (AC 1, AC 2, door 1)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeInstituicoesTests.Acha_por_nome_sigla_e_codigo_sem_acento"`

**C2** - [x] Com 7 instituições que casam, `itens` traz 5; a inativa que casa nunca vem (AC 2, AC 3)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeInstituicoesTests.Limita_a_cinco_e_nunca_devolve_inativa"`

**C3** - [x] `busca` ausente, com 2 caracteres ou com 61 responde `400` ProblemDetails com `errors.busca` = "Digite de 3 a 60 caracteres."; 3 caracteres com espaços nas pontas aparados vale pelo texto aparado (AC 4)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeInstituicoesTests.Valida_o_tamanho_do_texto"`

**C4** - [x] `busca=%%%` e `busca=a_b` respondem `200`; `%%%` não casa tudo e `_` não casa qualquer letra (AC 5)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeInstituicoesTests.Curinga_vira_texto_literal"`

**C5** - [x] Com `Limites:BuscaDeInstituicaoPorMinuto` = 2, a 3ª chamada do mesmo IP responde `429` (AC 6)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~BuscaDeInstituicoesTests.Passou_do_limite_responde_429"`

**C6** - [x] A rota entra nos retratos (`rotas.txt` com `anonimo=True limite=instituicoes`) e na lista `Publicas` de `SessaoTests`; as 107 rotas privadas seguem `401` (Impact)
Proof: `dotnet test Esclari.Api.Tests --filter "FullyQualifiedName~RetratoTests|FullyQualifiedName~SessaoTests"`

### S2 - escolha na raiz

**C7** - [x] Sem instituição lembrada, `/` mostra o logo no `h1`, o campo "Sua instituição", a dica "Digite o nome ou a sigla da sua instituição." e nenhum resultado; com menos de 3 caracteres não chama a busca (AC 7, AC 9)
Proof: `cd web && npx vitest run src/acesso/escolha.test.tsx -t "estado inicial"`

**C8** - [x] Com 3 ou mais caracteres, chama a busca uma vez 300 ms depois da última tecla e mostra cada item como link `/{slug}` com `nome` e `nomeCurto`; "Buscando…" em `aria-live="polite"` durante a chamada (AC 8, AC 10, AC 14)
Proof: `cd web && npx vitest run src/acesso/escolha.test.tsx -t "busca com espera"`

**C9** - [x] Lista vazia mostra "Nenhuma instituição encontrada para “{texto}”. Confira o nome ou peça o endereço à sua instituição."; falha de rede ou `5xx` mostra "Não foi possível buscar agora. Verifique a conexão e tente de novo." e a tecla seguinte busca de novo; `429` mostra "Muitas buscas agora. Aguarde um minuto e tente de novo." (AC 11, AC 12, AC 13)
Proof: `cd web && npx vitest run src/acesso/escolha.test.tsx -t "estados da busca"`

### S3 - instituição lembrada

**C10** - [x] `200` em `/:slug` grava `localStorage['esclari-instituicao']`; `404` do slug lembrado apaga a chave; `404` de outro slug não apaga (AC 15, AC 17, door 2)
Proof: `cd web && npx vitest run src/acesso/lembrada.test.tsx -t "grava e apaga"`

**C11** - [x] Com slug lembrado, `/` vai para `/{slug}` com `replace` (Voltar não retorna a `/`); se ler ou gravar o `localStorage` lança, mostra a escolha e segue (AC 16, AC 18)
Proof: `cd web && npx vitest run src/acesso/lembrada.test.tsx -t "volta sozinho"`

**C12** - [x] O Login mostra "Trocar de instituição", que apaga a chave e abre a escolha; "Instituição não encontrada." mostra também "Escolher instituição" para `/` (AC 19, AC 20)
Proof: `cd web && npx vitest run src/acesso/lembrada.test.tsx -t "trocar de instituição"`

### S4 - celular

**C13** - [x] Em 360×780 com 5 resultados, campo e 5 resultados cabem sem rolagem; em 360×420 com foco, campo e primeiro resultado ficam na área visível; em 844×390, campo sem rolagem e sem rolagem horizontal; resultados e links com 44 px ou mais de altura (AC 21 a 24)
Proof: `cd web && npx playwright test e2e/escolher-instituicao.spec.ts`

### Fechamento

**C14** - [x] As suítes seguem verdes: API inteira, Vitest inteiro, builds, `crapper` abaixo de 10, `dryer` sem candidato a 0.8, e os e2e que ramificam na `Raiz` (Impact)
Proof: `dotnet test Esclari.Api.Tests`
Proof: `npm --prefix web test`
Proof: `dotnet build Esclari.slnx && npm --prefix web run build`

## Coverage

| Set (size) | Member -> proof | Unproven |
| --- | --- | --- |
| `GET /api/instituicoes` statuses (3) | 200 C1 · 400 C3 · 429 C5 | - |
| door 1 contrato (campos, ordem, 5, ativas) | C1 · C2 | - |
| door 2 chave `esclari-instituicao` | gravar/apagar C10 · ler C11 · apagar ao trocar C12 | - |
| estados da `Raiz` (5) | vazio C7 · carregando C8 · sem resultado C9 · erro C9 · 429 C9 | - |
| telas do Observable (4 linhas celular) | C13 | - |
