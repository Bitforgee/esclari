# Guia de marca do Esclari

Uma página para quem precisa pôr o Esclari em um slide, um e-mail, o GitHub ou uma tela. Os arquivos estão em `web/public/marca/`. A interface que o aluno vê é da instituição (AD-032): esta marca aparece só onde não há instituição, como a raiz `/`, e como reserva do ícone do app.

## Nome

O produto se chama Esclari, com "E" maiúsculo e o resto em minúsculas. Em frase, vem sem artigo no começo de um título ("Esclari: atendimento ao estudante"). Só há três formas escritas: o nome "Esclari", o protocolo `ESC-NNNNNN` (por exemplo `ESC-000123`) e o domínio `esclari.com.br`.

## Símbolo

Um balão de conversa cujos dois cortes à direita desenham a letra E. Arquivo: `simbolo.svg` (`viewBox` 64 por 64, um único caminho, pintado com `currentColor`, então herda a cor do texto ao redor). É uma forma só, sem degradê, sem sombra e sem contorno. Em qualquer fundo claro use a cor `#224fa7`; em fundo escuro, branco.

## Logo horizontal

Símbolo à esquerda e a palavra "Esclari" à direita, com a altura das letras maiúsculas em cerca de 60% da altura do símbolo. Arquivo: `logo-horizontal.svg`, com a palavra já em curvas (Public Sans Bold), então não depende de fonte instalada. O arquivo vem na cor `#224fa7`. Em fundo escuro, mostre-o em branco (na web, `dark:brightness-0 dark:invert`; em outros meios, exporte uma cópia branca do mesmo desenho). O ícone do app e o favicon usam o símbolo branco sobre uma placa `#224fa7` (`favicon.svg` e os PNG em `web/public/icons/`, gerados por `npm --prefix web run icones`).

## Cor

A cor da marca é `#224fa7`, o azul que o produto já usa de padrão. Branco é a única outra cor permitida. Quando uma instituição define a própria cor (AD-032), ela vale dentro do produto da instituição; a cor do Esclari continua `#224fa7` em tudo que é do Esclari.

## Área de respeito

Deixe livre, em volta do logo e do símbolo, uma margem de metade da altura do símbolo. Nada de texto, borda, imagem ou outro logo entra nessa margem.

## Tamanho mínimo

O símbolo sozinho não desce de 16 px de altura (o tamanho da aba do navegador). O logo horizontal não desce de 96 px de largura; abaixo disso a palavra perde a leitura, e então se usa só o símbolo.

## Usos proibidos

- Trocar a cor por outra que não seja `#224fa7` ou branco, porque a cor é o que liga o símbolo ao produto e uma terceira cor competiria com a da instituição.
- Esticar, comprimir ou inclinar o logo, porque o corte em E deixa de ler como letra quando a proporção muda.
- Pôr sombra, brilho, degradê ou contorno, porque o símbolo foi desenhado para uma cor chapada e some em 16 px com efeito.
- Pôr o logo sobre foto ou fundo com desenho, porque a margem e o contraste de 4,5 para 1 deixam de valer.
- Escrever o nome com todas as letras maiúsculas ou separado ("Es Clari"), porque quem busca o nome por voz ou por texto não o acharia.
- Substituir a palavra por outra fonte, porque a palavra é parte do desenho e foi entregue em curvas.
- Pôr o símbolo no lugar do logo ou do ícone de uma instituição, porque a identidade dela prevalece no produto dela.

## Tipografia

O produto usa Public Sans no texto e Source Serif 4 apenas nos títulos de conteúdo (AD-034). A palavra do logo é Public Sans Bold com entreletras de menos 2%, e não se digita: use o arquivo.

## Voz

Frases curtas, em português do Brasil, que dizem o que acontece e o que o aluno pode fazer em seguida. A frase de apresentação é a que já está na raiz do produto: "Dúvidas, solicitações e respostas da sua instituição em um só lugar." O Esclari não tem slogan próprio. Não promete o que o assistente não faz: ele responde só com fontes publicadas da instituição e leva a uma pessoa quando precisa.
