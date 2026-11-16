# Pendências do rename para Esclari

## Problem

O rename `renomear-esclari` (AD-058) deixou quatro itens abertos no ambiente e na identidade visual, listados no relatório de entrega de 15/11/2026:

1. Os três ícones do PWA (`web/public/icons/icon-192.png`, `icon-512.png`, `icon-maskable-512.png`) são um anel branco sobre azul, a letra "O" do One. Quem instala o app vê a marca antiga.
2. A pasta local é `D:\one-system` e o remoto é `https://github.com/Bitforgee/One`. O Docker Compose nomeia projeto, contêineres e volumes pela pasta: hoje existem `one-system-api-1`, `one-system_pgdata`, `one-system_miniodata` e `one-nuget`, mais o banco `one` antigo dentro do volume.
3. O banco de desenvolvimento `esclari` ficou com linhas que os testes deixaram: instituições `demo-*` e `operadora-*` criadas pelo e2e, mais `demo` e `e2e` criadas à mão pelo Verificador. Os specs `*-real` precisam de `demo` e `e2e` com os administradores `admin@demo.esclari` e `admin@e2e.esclari`, e hoje só passam se alguém criar essas instituições à mão. O e2e usa `E2E_CONTAINER` com padrão `esclari-plataforma`, nome que nenhum contêiner tem.
4. Os dois itens de higiene do repositório já foram resolvidos antes deste plano: o push foi feito (`ad87150..a30c067` em `main`) e `.specs/relatorio-nome.md` está rastreado. Eles não geram critério.

Depois desta feature, o app instalado mostra a marca nova, qualquer clone sobe o Compose com os mesmos nomes de projeto e volume, o e2e roda do zero com um comando, e o repositório e a pasta se chamam Esclari.

## Flow

Reaproveita o comando `instituicao criar` que já existe na API (`InstituicaoComando.cs`), o Playwright que já é dependência do `web/` e o `docker-compose.yml` atual; não entra dependência nova.

1. `web/scripts/gerar-icones.mjs` (new, door 2) - desenha o glifo "E" em SVG, renderiza no Chromium do Playwright e grava os três PNGs em `web/public/icons/`
2. `vite-plugin-pwa` em `web/vite.config.ts` (exists) - continua apontando para os mesmos três caminhos; build copia para `Esclari.Api/wwwroot`
3. `docker-compose.yml` (exists) - ganha `name: esclari` no topo (door 1); contêineres viram `esclari-<serviço>-1` e volumes `esclari_<volume>`
4. `web/scripts/preparar-e2e.mjs` (new, door 3) - chama `docker compose exec api dotnet Esclari.Api.dll instituicao criar ...` para `demo` e `e2e` se ainda não existirem
5. `web/e2e/instituicoes-real.spec.ts` (exists) - lê `E2E_CONTAINER`, novo padrão `esclari-api-1`
6. `.git/config` (exists, fora do repositório, ação do dono) - `origin` aponta para o nome novo depois do rename no GitHub
7. `AGENTS.md` (exists), `ROADMAP.md` (exists), `STATE.md` (exists) e `NomeDoProdutoTests` (exists) - citam URL e pasta novas; a lista de exceções do guarda perde `github.com/Bitforgee/One` e `D:\one-system`

## Impact

| Front | What changes |
| --- | --- |
| domain | termo `projeto Compose`: era derivado do nome da pasta (`one-system`), passa a ser fixo `esclari`. Quem ramifica hoje: `web/e2e/instituicoes-real.spec.ts` (`E2E_CONTAINER`), `web/apresentacao/preparar.ts` e `tts.ts` (`docker compose exec`), qualquer comando que cite `one-system-api-1` |
| domain | termo `nome do repositório`: era `One`, passa a `Esclari`. Quem ramifica: `ROADMAP.md` (links), `STATE.md` (AD-005, AD-043 e Handoff citam o remoto), `NomeDoProdutoTests` (exceções) |
| stored data | volumes `one-system_pgdata` e `one-system_miniodata`: dados de desenvolvimento, sem produção; saem só com o "ok" do dono. O volume novo `esclari_pgdata` nasce vazio e a API aplica as migrations na subida |
| stored data | `one-nuget` é cache de pacotes NuGet; saem junto, o cache se recria no próximo build |
| stored data | PWAs já instalados guardam o ícone antigo até o navegador revalidar o manifesto; nada migra |
| stored data | a pasta `D:\one-system` e o `.git/config` ficam fora do repositório; não há dado a migrar |

## Relations

None - no stored-data shape change

## Surface

Comandos e arquivos que outros consomem; nenhuma rota HTTP muda. Para comando, `200` quer dizer exit `0` e `500` exit `1`.

| Route | In | Out | Status |
| --- | --- | --- | --- |
| `npm --prefix web run icones` | nenhuma | `icon-192.png` 192×192, `icon-512.png` 512×512, `icon-maskable-512.png` 512×512; uma linha por arquivo gravado | `200` (exit `0`); `500` (exit `1`) se o Chromium do Playwright não abre |
| `npm --prefix web run e2e:preparar` | `E2E_CONTAINER` opcional (padrão `esclari-api-1`) | uma linha por instituição: `criada <slug>` ou `ja existe <slug>` | `200` (exit `0`); `500` (exit `1`) se o contêiner não responde |
| `manifest.webmanifest` do app (`icons[].src`) | nenhuma | `/icons/icon-192.png`, `/icons/icon-512.png`, `/icons/icon-maskable-512.png`, `purpose` `any` e `maskable` | `200` |

## Landing

| One-way door | Literal shape | Alternative rejected |
| --- | --- | --- |
| 1. Nome do projeto Compose | `name: esclari` na primeira linha de `docker-compose.yml`; volumes `esclari_pgdata`, `esclari_miniodata` | `COMPOSE_PROJECT_NAME` em `.env`: o arquivo fica fora do Git e é por máquina, então cada clone teria nomes diferentes e perderia os dados ao mudar |
| 2. Caminhos e tamanhos dos ícones | mesmos nomes `web/public/icons/icon-192.png`, `icon-512.png`, `icon-maskable-512.png`; `purpose: maskable` com o glifo dentro do círculo central de 80% da largura | nomes novos com hash: o manifesto e todo PWA instalado apontam para o caminho atual e a mudança de nome quebra o cache do instalador |
| 3. Nome do comando de preparo | `npm --prefix web run e2e:preparar`; instituições `demo` (admin `admin@demo.esclari`) e `e2e` (admin `admin@e2e.esclari`), criadas só se ausentes | fixture dentro de cada spec: duplica a criação em 8 specs e deixa linhas `demo-*` no banco a cada rodada |
| 4. Nome do repositório e da pasta | repositório `Bitforgee/Esclari`; pasta `D:\esclari-system` | manter `One` no remoto com redirecionamento: o nome antigo fica em todo clone e em todo link novo; GitHub redireciona o nome velho, então renomear não quebra clones existentes |

- Nada mais nesta mudança é difícil de desfazer

## Criteria

### S1: o app instalado mostra a marca Esclari (P1)

Os três ícones deixam de ser o anel do One e passam a ser o glifo "E" do Esclari, gerado por script.

**Acceptance Criteria**

1. WHEN `npm --prefix web run icones` roda THEN the system SHALL gravar `icon-192.png` 192×192, `icon-512.png` 512×512 e `icon-maskable-512.png` 512×512 em `web/public/icons/`
2. WHEN o ícone `maskable` é lido THEN the system SHALL ter todo pixel do glifo branco dentro do círculo central de diâmetro 80% da largura (204 px em 256, ou 410 px em 512) e o resto preenchido com a cor da marca
3. WHEN o ícone `any` de 192 px é lido THEN the system SHALL ter o glifo branco sobre a cor de `theme_color` do manifesto, sem o anel do One
4. IF o Chromium do Playwright não abre THEN the system SHALL sair com código `1` e uma mensagem em português, sem apagar os PNGs anteriores
5. WHEN o app é compilado THEN the system SHALL listar no manifesto os mesmos três caminhos de ícone, com `purpose` `any` para 192 e 512 e `maskable` para o terceiro

### S2: o ambiente de desenvolvimento sobe do zero com um comando (P1)

`docker compose up` tem nomes estáveis e o e2e tem dados prontos.

6. WHEN `docker compose config` roda em qualquer pasta THEN the system SHALL mostrar o nome do projeto `esclari`
7. WHEN `docker compose up -d` roda THEN the system SHALL criar os volumes `esclari_pgdata` e `esclari_miniodata` e a API `esclari-api-1`
8. WHEN `npm --prefix web run e2e:preparar` roda com a API no ar THEN the system SHALL criar as instituições `demo` e `e2e` com os administradores `admin@demo.esclari` e `admin@e2e.esclari` e imprimir `criada demo` e `criada e2e`
9. WHEN `npm --prefix web run e2e:preparar` roda de novo THEN the system SHALL imprimir `ja existe demo` e `ja existe e2e`, sair com `0` e não criar linha
10. IF o contêiner da API não responde THEN the system SHALL sair com código `1` e dizer qual contêiner tentou
11. WHEN o e2e `instituicoes-real` roda sem `E2E_CONTAINER` THEN the system SHALL usar `esclari-api-1`
12. WHEN os volumes e o contêiner antigos são removidos com o "ok" do dono THEN the system SHALL deixar `docker volume ls` sem nenhum nome começando por `one` e o banco `esclari` novo sem as instituições `demo-*` e `operadora-*`

### S3: repositório e pasta se chamam Esclari (P2)

Depois que o dono renomeia o repositório e a pasta, o código e os documentos acompanham.

13. WHEN o repositório é renomeado no GitHub e o dono roda `git remote set-url` THEN the system SHALL responder `git remote get-url origin` com `https://github.com/Bitforgee/Esclari` e `git ls-remote origin HEAD` com exit `0`
14. WHEN `ROADMAP.md`, `AGENTS.md` e `STATE.md` são lidos THEN the system SHALL citar o remoto `Bitforgee/Esclari` e a pasta `D:\esclari-system` nas linhas vivas, e um AD novo SHALL registrar a mudança
15. WHEN `NomeDoProdutoTests.Nenhum_arquivo_rastreado_cita_One_fora_do_historico` roda THEN the system SHALL passar com as exceções `github.com/Bitforgee/One` e `D:\one-system` removidas da lista
16. WHILE o dono não confirmar que renomeou o repositório e a pasta, the system SHALL deixar as linhas de remoto e pasta como estão e S3 não começa

## Out of scope

| Excluded | Why |
| --- | --- |
| Logotipo completo, wordmark e identidade visual do Esclari | Precisa de um designer; este plano gera só o glifo do ícone do app |
| Registro de `esclari.com.br`, DNS e certificado | Infra fora do repositório |
| Reescrever histórico `.specs` fechado | Preserva a evidência de verificação (decisão de AD-058) |
| Apagar volumes ou renomear a pasta sem o "ok" do dono | Ação destrutiva e fora do repositório; só com autorização explícita |
| Corrigir `crapper` (`useModo` 11.0 e `Usuarios` 10.0) e `chat.spec.ts:47` | Falhas que já existiam na base; viram feature própria |

## Assumptions

| Assumption | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| Glifo do ícone | "E" maiúsculo geométrico em branco, fonte do projeto (Geist, se carregada pelo `web/`, senão a pilha `system-ui` bold), sobre a cor de `theme_color` | Mantém branco sobre azul do ícone atual; troca só o anel | n |
| Referência visual do ícone | web.dev "Maskable icon" (zona segura: círculo central de 80%) e o guia de ícones do Chrome para instalação | AGENTS.md exige referência pesquisada para decisão de UI nova; a fonte será citada na tela de verificação | n |
| Ferramenta de render | Chromium do Playwright, já instalado no `web/` | Sem dependência nova (YAGNI) | n |
| Nome do repositório e pasta | `Bitforgee/Esclari` e `D:\esclari-system` | Segue o padrão `<produto>-system` atual | n |
| Quem renomeia | O dono renomeia a pasta (processo aberto nela trava o rename) e o repositório no GitHub, ou autoriza `gh repo rename`; o agente faz só o lado do repositório | Rename de pasta e de remoto está fora do alcance e do escopo autorizado do agente | n |
| Limpeza do Docker | Remover `one-system-*` (4 contêineres), `one-system_pgdata`, `one-system_miniodata` e `one-nuget` só depois do "ok" explícito | Ação destrutiva; os dados são só de desenvolvimento | n |
| `univag` no banco novo | Fora do preparo do e2e; o `instituicao carga` existente continua sendo o caminho | Não faz parte do pedido | n |

**Open questions:**

| # | Kind | Question | Until answered |
| --- | --- | --- | --- |
| 1 | blocks | O dono autoriza apagar os volumes e contêineres `one-*` do Docker? | AC 12 |
| 2 | blocks | O dono confirma que a pasta e o repositório já foram renomeados (e para quais nomes)? | AC 13 a 16 |
| 3 | open | O glifo "E" simples serve, ou o dono quer enviar arte própria? | AC 1 a 3; escrito com o "E" simples enquanto isso |

## Observable

| Surface | Decision | Landing |
| --- | --- | --- |
| comando `icones` | formato de saída, flags, códigos de saída, falha no meio | AC 1, 4; sem flags (n/a - um único modo, nada a escolher) |
| comando `e2e:preparar` | formato de saída, flags, códigos de saída, falha no meio | AC 8, 9, 10; sem flags (n/a - o contêiner vem de `E2E_CONTAINER`) |
| manifesto PWA | caminhos e `purpose` dos ícones | AC 5 |
| docker compose | nome do projeto, volumes, contêineres | AC 6, 7 |
| ícone instalado no celular 360×780 | glifo legível em 192 px e na máscara circular | AC 2, 3 |
| ícone no celular deitado 844×390 | n/a - o ícone não depende da orientação | n/a - não é tela |
| ícone com teclado aberto | n/a - o ícone não aparece dentro do app | n/a - não é tela |
| ícone e toque | n/a - nenhum alvo de toque novo | n/a - nenhum controle |
| documento (`AGENTS.md`, `ROADMAP.md`, `STATE.md`) | estrutura, tom e o próximo passo do leitor | AC 14, 15; formato existente, só trocam URL e pasta |

## Sources

- `.specs/features/renomear-esclari/verification.md` - lista as pendências e o que o Verificador deixou no banco de desenvolvimento
- `.specs/STATE.md` AD-058 - nome, protocolo e identificadores persistidos do rename
- web.dev, "Maskable icon" - zona segura do ícone `maskable` (círculo central de 80%)
