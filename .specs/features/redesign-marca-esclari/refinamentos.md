# Refinamentos dos conceitos

As três direções foram desenhadas em preto antes de qualquer decisão de cor. Os exemplos externos foram usados para comparar nível de acabamento e saturação de categoria; nenhuma geometria externa foi traçada ou reutilizada.

Geometria externa reutilizada: nenhuma

### Fonte embutida

Tipo: wordmark-led combination

Ideia monocromática: a clareza com procedência aparece dentro da própria letra, onde um recorte de margem aponta para a origem antes de a palavra seguir.

Defeito v1: o marcador era pequeno demais para sobreviver a 16 px, a abertura interna ficava apertada e o contorno parecia só um `e` pesado.

Correção v2: o marcador ganhou largura e profundidade, o contraespaço abriu e o terminal inferior recebeu mais distância do eixo para manter a leitura pequena.

Escala 64/32/16: PASS
Preto e invertido: PASS
Espelho: PASS
Rotação 90/180: PASS
Desfoque: PASS
Leitura indevida: nenhuma
Familiaridade: a letra `e` é familiar e pode lembrar marcas de navegador, mas o recorte de referência nasce do contorno e não corresponde à forma da marca atual nem às referências da prateleira.
Inspeção 512: fonte-embutida-symbol.png PASS
Inspeção 512: fonte-embutida-lockup.png PASS
Preview: preview-fonte-embutida.html PASS

A auditoria deu 100/100 ao símbolo. O lockup ficou em 97/100; os pontos informativos são a complexidade do contorno tipográfico herdado da palavra em curvas e o recorte óptico apertado do arquivo exploratório. Não houve `WARN` nem `FAIL`.

### Manícula contemporânea

Tipo: pictorial combination

Ideia monocromática: a clareza com procedência aparece como uma mão de margem reduzida, que destaca a passagem importante e aponta a próxima ação humana.

Defeito v1: a primeira silhueta parecia uma letra `F`, uma chave ou uma pistola e não mostrava o gesto histórico com clareza.

Correção v2: a forma ganhou punho, polegar diagonal, dedo indicador horizontal e dois cortes para os dedos recolhidos; a leitura passou a ser de gesto que aponta, sem contorno de arma.

Escala 64/32/16: PASS
Preto e invertido: PASS
Espelho: PASS
Rotação 90/180: PASS
Desfoque: PASS
Leitura indevida: nenhuma
Familiaridade: a mão que aponta é um sinal histórico reconhecível, mas a construção geométrica com dois cortes, punho quadrado e polegar diagonal não coincide com cursor, emoji ou referência da prateleira.
Inspeção 512: manicula-symbol.png PASS
Inspeção 512: manicula-lockup.png PASS
Preview: preview-manicula.html PASS

A direção é a mais literal. Em 16 px os dois cortes viram uma única pista clara, enquanto o indicador e o polegar preservam a silhueta. O símbolo auditado não teve `WARN` nem `FAIL`; a complexidade do lockup vem da palavra em curvas.

### Fio de procedência

Tipo: abstract combination

Ideia monocromática: a clareza com procedência aparece num fio contínuo que sai da origem, percorre a resposta e permanece aberto para a passagem a uma pessoa.

Defeito v1: a rota horizontal terminava numa base reta e podia ser lida como o algarismo `2`.

Correção v2: o percurso virou uma espiral aberta assimétrica, com entrada longa, volta interna e saída lateral; ele deixou de ter base de algarismo e ganhou movimento contínuo.

Escala 64/32/16: PASS
Preto e invertido: PASS
Espelho: PASS
Rotação 90/180: PASS
Desfoque: PASS
Leitura indevida: nenhuma
Familiaridade: a forma pode sugerir ouvido, fio ou `e` cursivo, associações compatíveis com escuta e esclarecimento; não coincide com o símbolo atual nem com Zendesk, Intercom ou Khan Academy.
Inspeção 512: fio-procedencia-symbol.png PASS
Inspeção 512: fio-procedencia-lockup.png PASS
Preview: preview-fio-procedencia.html PASS

O símbolo usa um traço de 28 unidades nesta etapa para preservar a lógica editável do percurso. A auditoria informa que o traço deve ser expandido no master final; isso será obrigação do plano final, caso esta direção seja escolhida. O centro fica opticamente acima do centro geométrico por causa da entrada aberta. Não houve `WARN` nem `FAIL`.

## Comparação

No teste de prateleira, a marca atual se mistura ao grupo de iniciais geométricas. Fonte embutida é mais memorável que a atual e ensina o nome. Manícula é a referência cultural mais rara, porém exige que a mão seja aprendida. Fio de procedência tem a silhueta mais distinta, o melhor desempenho em uma cor e o maior potencial para gerar padrão e movimento sem recorrer a sinais de IA.

## Informações das auditorias

- Os três lockups usam a palavra em contornos e, por isso, passam da mediana de âncoras da biblioteca. A versão final deverá redesenhar e simplificar a palavra, em vez de reutilizar o contorno exploratório.
- O Fio de procedência conserva o traço editável nesta etapa. A versão final deverá expandi-lo para preenchimento.
- As diferenças de margem dos lockups são consequência do canvas comum de comparação; o master final terá `viewBox` recortado e alinhamento óptico.
