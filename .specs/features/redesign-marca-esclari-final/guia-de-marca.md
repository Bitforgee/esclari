# Guia de marca do Esclari

A identidade configurada pela instituição sempre prevalece dentro do ambiente dela. Esta marca identifica o Esclari na raiz pública, no favicon e no PWA de reserva, em apresentações, no GitHub e em materiais próprios.

## Ideia

O Fio de procedência transforma o percurso do atendimento em uma forma contínua: ele parte de uma origem, percorre a resposta e permanece aberto para a passagem a uma pessoa. A espiral também pode sugerir escuta e um `e`, sem depender dessas leituras para funcionar.

O produto se chama Esclari. O protocolo é `ESC-NNNNNN` e o domínio é `esclari.com.br`. O símbolo não substitui esses nomes em texto.

## Arquivos

- `simbolo.svg`: master para 24 px ou mais, em `currentColor`.
- `simbolo-pequeno.svg`: corte reforçado para 16–32 px.
- `palavra-marca.svg`: “esclari” em contornos.
- `logo-horizontal.svg`: versão principal para fundo claro.
- `logo-horizontal-escuro.svg`: versão para Tinta ou outro fundo escuro uniforme.
- `logo-vertical.svg`: composição empilhada.
- `logo-preto.svg` e `logo-branco.svg`: reprodução em uma cor.
- `favicon.svg` e `web/public/icons/*.png`: ícones digitais gerados a partir do corte pequeno.

Use SVG sempre que o destino aceitar vetor. Os PNGs do kit existem para sistemas que não aceitam SVG.

## Paleta

| Cor | HEX | RGB | CMYK aproximado | Papel |
| --- | --- | --- | --- | --- |
| Origem | `#C83A23` | RGB 200, 58, 35 | CMYK 0, 63, 78, 22 | símbolo no logo claro, placa do favicon e fallback de e-mail/manifesto |
| Origem clara | `#F16A4B` | RGB 241, 106, 75 | CMYK 0, 56, 69, 5 | símbolo sobre Tinta |
| Tinta | `#15133A` | RGB 21, 19, 58 | CMYK 64, 67, 0, 77 | palavra-marca e fundo escuro |
| Papel | `#F7F5EE` | RGB 247, 245, 238 | CMYK 0, 1, 4, 3 | palavra sobre Tinta e fundo de apresentação |

O azul `#224fa7` continua sendo a cor de ação padrão da interface. Ele não faz parte da paleta do logo e evita usar o vermelho de marca como sinal de ação ou sucesso.

## Contraste

- Origem com branco: 5,14:1.
- Origem clara com Tinta: 5,82:1.
- Tinta com branco: 17,70:1.
- Tinta com Papel: 16,23:1.

Esses pares passam 4,5:1. Origem clara não recebe texto branco; ela existe para uso sobre Tinta.

## Área de respeito

Deixe em todos os lados uma margem mínima igual à metade da largura expandida do Fio, isto é, 14 unidades no grid de 256. Em co-branding, essa margem começa no limite visual de cada marca.

## Tamanho mínimo

- Corte pequeno: 16 px de altura.
- Símbolo master: 24 px de altura.
- Logo horizontal: 96 px de largura.
- Logo vertical: 72 px de largura.

Abaixo de 96 px de largura, não comprima o horizontal. Use o corte pequeno sozinho e escreva “Esclari” como texto acessível ao redor quando o contexto exigir o nome.

## Fundos

Use o logo principal sobre branco, Papel ou fundo claro uniforme. Use `logo-horizontal-escuro.svg` sobre Tinta ou fundo escuro uniforme. Sobre fotografia, coloque a marca numa área lisa com contraste medido ou dentro de um campo Tinta/Papel que preserve a área de respeito. Para impressão sem cor, use somente as versões preta ou branca.

## Co-branding

A marca da instituição vem primeiro quando o material é dirigido aos alunos dela. Separe as duas marcas por uma linha neutra ou por duas vezes a área de respeito e mantenha altura óptica equivalente. Não recolora o Esclari com a cor institucional e não aplique Origem ao logo da instituição. A assinatura textual permitida é “Atendimento por Esclari”.

## Usos proibidos

- Não estique ou comprima o Fio, porque a volta interna e as duas extremidades perdem a proporção.
- Não redesenhe o símbolo com `stroke`, porque ferramentas de impressão e corte podem mudar a espessura.
- Não feche a espiral nem transforme a abertura em círculo, porque a saída aberta representa a passagem humana.
- Não aplique gradiente, sombra, brilho ou contorno, porque o símbolo foi testado como forma chapada em 16 px.
- Não troque Origem e Origem clara entre fundos, porque os pares foram escolhidos por contraste.
- Não use o símbolo no lugar do logo de uma instituição, porque a identidade configurada por ela prevalece.
- Não recomponha “esclari” com uma fonte instalada, porque a palavra-marca tem letras e espaçamento próprios.
- Não use apenas o símbolo numa primeira apresentação sem escrever Esclari, porque a forma ainda precisa ser aprendida.

## Tipografia

A palavra-marca parte da estrutura da Public Sans 650, sob licença OFL-1.1, mas foi redesenhada e convertida em contornos. O `e`, o `a`, o `r` e o `i` repetem a abertura e os terminais do Fio. O pingo do `i` é monocromático e o vão entre `r` e `i` permanece aberto em 24 px.

No produto, Public Sans continua no texto e Source Serif 4 nos títulos definidos pela AD-034. Não use a palavra-marca como fonte de títulos.

## Impressão

Os valores CMYK são aproximações matemáticas e precisam de prova física no papel, tinta e fornecedor escolhidos. Não atribua equivalência Pantone antes dessa prova física. Para bordado, corte ou gravação, use a versão preta, transforme todos os contornos em uma única forma no software de produção e faça amostra no tamanho real.
