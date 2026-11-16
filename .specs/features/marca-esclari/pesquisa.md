# Pesquisa: logo e marca do Esclari

Data: 16/11/2026. Conteúdo resumido com outras palavras. Cada linha diz o que a fonte decidiu no plano.

## Estado atual do repositório (fatos, não opinião)

- O "logo" é um `E` branco num quadrado azul `#224fa7` desenhado por `web/scripts/gerar-icones.mjs` (quatro retângulos). Não há logo vetorial, wordmark, favicon SVG, `apple-touch-icon` de 180 px nem guia de uso.
- A cor `#224fa7` está escrita em três lugares: `gerar-icones.mjs` (`MARCA`), `Esclari.Api/LayoutEmail.cs` (`CorPadrao`) e `ManifestoInstituicao.cs` (`CorPadrao`).
- O produto é multi-instituição (AD-008): a aluna vê o logo, o ícone e a cor da instituição (AD-032). A marca Esclari aparece só quando não há instituição: raiz `/`, aba do navegador antes de carregar, PWA sem ícone da instituição, push e e-mail sem imagem própria. `ROADMAP.md` deixou em aberto: "Logo e cores do Esclari: o grupo fornece ou aprova a proposta".
- Tipografia já decidida (AD-034): Public Sans no texto, Source Serif 4 em títulos de conteúdo.

## Referências externas

| Fonte | O que ela diz | O que decide no plano |
| --- | --- | --- |
| [web.dev, maskable icons](https://web.dev/articles/maskable-icon) | O ícone mascarável é opaco e o que importa fica num círculo central de raio 40% da largura; os 10% externos podem ser cortados. | O PNG `maskable` tem o símbolo inteiro dentro desse círculo (critério com medida). |
| [Apple QA1686](https://developer.apple.com/library/content/qa/qa1686/_index.html) e [Lighthouse](https://developer.chrome.com/docs/lighthouse/pwa/apple-touch-icon) | O ícone da tela inicial do iOS é PNG sem regiões transparentes e declarado por `<link rel="apple-touch-icon">`. | Novo `apple-touch-icon.png` 180×180 opaco, sem cantos arredondados no arquivo. |
| [favicon.now, wordmark para 16 px](https://favicon.now/guides/case-study-wordmark-to-favicon) e [favicondl](https://favicondl.com/blog/favicon-vs-logo.html) | Em 16 px, texto vira ruído; o favicon repete uma pista do logo (uma letra, uma silhueta), não o logo inteiro. | Favicon só com o símbolo, nunca com a palavra. |
| [logoworks, logos escaláveis](https://logoworks.com/what-makes-logos-scalable/) | Logo escalável tem uma ideia central e uma silhueta que se entende antes dos detalhes. | Símbolo de uma forma só, uma cor, sem degradê. |
| [Zendesk, diretrizes de marca](https://www.zendesk.com/company/brand-guidelines/) | Publica largura mínima do logo e exige permissão de terceiros. | O guia do Esclari fixa tamanho mínimo e usos proibidos com números. |
| [Notion, guia de marca](https://www.notion.com/blog/brand-guidelines-template) | Um guia útil cobre uso do logo (cores, tamanhos, espaço), tipografia e tom de texto. | Seções do guia (AC 12). |
| [ebaq, branding de IA em 2026](https://www.ebaqdesign.com/blog/ai-company-branding) | A marca de IA converge para wordmark neutro e um símbolo de "faísca" ou ponto; o resultado parece igual. | Rejeitada a faísca/estrela: o Esclari é atendimento com fonte, e o símbolo vem da conversa (balão), não do "brilho de IA". |
| [Intercom, identidades do Fin](https://www.intercom.com/help/en/articles/12426795-create-a-branded-experience-with-fin-identities) | O assistente de IA leva avatar e nome por marca/cliente. | A marca Esclari não substitui o avatar nem o nome que a instituição define (AD-032); o símbolo fica no produto, não no assistente. |

## Direções desenhadas (`direcoes.html` / `direcoes.png`)

| Direção | Ideia | Veredito |
| --- | --- | --- |
| **A. Balão cortado em E** | Balão de conversa com dois cortes à direita que desenham um E. Uma cor, um caminho. | Recomendada: lê-se E e lê-se conversa, funciona em branco sobre azul e em preto, sobrevive a 16 px. |
| B. Balão com linhas, uma em destaque | Três linhas de texto, a do meio amarela (a fonte citada). | Boa história (resposta com fonte), mas pede 2 cores e a 16 px vira só "um chat". Genérica. |
| C. E de traço | Refinamento do ícone atual, sem balão. | Legível, mas não diz nada sobre atendimento; parece letra de fonte. |

Ressalva honesta sobre A: balão de chat é um símbolo comum. O que a torna própria é o corte em E; o risco de confusão com outro balão-letra só se mede na busca do INPI (classes 9, 41 e 42), que este plano não faz.
