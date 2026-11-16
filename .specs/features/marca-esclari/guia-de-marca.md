# Guia de marca do Esclari

Uma página para quem precisa pôr o Esclari em um slide, um e-mail, o GitHub ou uma tela. Os arquivos estão em `web/public/marca/`. A interface que o aluno vê é da instituição (AD-032): esta marca aparece só onde não há instituição, como a raiz `/`, e como reserva do ícone do app.

## Nome

O produto se chama Esclari, com "E" maiúsculo e o resto em minúsculas quando escrito em texto. No logo a palavra vai toda em minúsculas ("esclari"), porque é desenho, não texto. Só há três formas escritas: o nome "Esclari", o protocolo `ESC-NNNNNN` (por exemplo `ESC-000123`) e o domínio `esclari.com.br`.

## Símbolo

Um E de duas peças: a coluna e os dois braços em Ultramar, e o braço do meio solto, em marca-texto. A ideia é a do produto: a resposta cita a fonte, e o trecho citado aparece destacado, como num livro de estudo. Arquivo: `simbolo.svg` (`viewBox` 64 por 64; o E é pintado com `currentColor` e herda a cor do texto ao redor, o braço do meio é sempre `#ffd43b`). Sem degradê, sem sombra, sem contorno.

## Logo horizontal

Símbolo à esquerda e a palavra "esclari" à direita, com o pingo do "i" também em marca-texto. A palavra está em curvas (Sora SemiBold, licença OFL), então não depende de fonte instalada. Há dois arquivos: `logo-horizontal.svg` para fundo claro (E `#2438d6`, letras `#0e1330`) e `logo-horizontal-escuro.svg` para fundo escuro (E e letras `#ffffff`). O marca-texto fica igual nos dois. O ícone do app e o favicon usam o E branco com o braço marca-texto sobre uma placa `#2438d6` (`favicon.svg` e os PNG em `web/public/icons/`, gerados por `npm --prefix web run icones`).

## Cor

Quatro cores, cada uma com um papel:

- Ultramar `#2438d6`: a cor da marca. Contraste de 8,0 para 1 com branco.
- Marca-texto `#ffd43b`: só o braço do meio do E e o pingo do "i".
- Tinta `#0e1330`: as letras do logo e o texto sobre fundo claro.
- Papel `#eef0fb`: fundo claro da marca.

Quando uma instituição define a própria cor (AD-032), ela vale dentro do produto da instituição; as cores do Esclari valem em tudo que é do Esclari. A interface sem cor de instituição ainda usa o azul antigo `#224fa7`, até um passo separado.

## Área de respeito

Deixe livre, em volta do logo e do símbolo, uma margem de metade da altura do símbolo. Nada de texto, borda, imagem ou outro logo entra nessa margem.

## Tamanho mínimo

O símbolo sozinho não desce de 16 px de altura (o tamanho da aba do navegador). O logo horizontal não desce de 96 px de largura; abaixo disso a palavra perde a leitura, e então se usa só o símbolo.

## Usos proibidos

- Trocar o Ultramar por outra cor, porque a cor é o que liga o símbolo ao produto e uma terceira cor competiria com a da instituição.
- Usar o marca-texto como cor de texto, de botão ou de fundo, porque o contraste dele com branco é de 1,4 para 1 e a graça do símbolo vem de ele ser raro.
- Esticar, comprimir ou inclinar o logo, porque o E perde a proporção entre a coluna, os braços e o braço solto.
- Pôr sombra, brilho, degradê ou contorno, porque o símbolo foi desenhado para cor chapada e some em 16 px com efeito.
- Inverter as cores com filtro de CSS no tema escuro, porque o marca-texto vira azul; use `logo-horizontal-escuro.svg`.
- Pôr o logo sobre foto ou fundo com desenho, porque a margem e o contraste de 4,5 para 1 deixam de valer.
- Escrever o nome com todas as letras maiúsculas ou separado ("Es Clari"), porque quem busca o nome por voz ou por texto não o acharia.
- Pôr o símbolo no lugar do logo ou do ícone de uma instituição, porque a identidade dela prevalece no produto dela.

## Tipografia

O produto usa Public Sans no texto e Source Serif 4 apenas nos títulos de conteúdo (AD-034). A palavra do logo é Sora SemiBold, em minúsculas, e não se digita: use o arquivo.

## Voz

Frases curtas, em português do Brasil, que dizem o que acontece e o que o aluno pode fazer em seguida. A frase de apresentação é a que já está na raiz do produto: "Dúvidas, solicitações e respostas da sua instituição em um só lugar." O Esclari não tem slogan próprio. Não promete o que o assistente não faz: ele responde só com fontes publicadas da instituição e leva a uma pessoa quando precisa.
