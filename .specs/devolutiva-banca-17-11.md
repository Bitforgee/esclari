# Devolutiva da banca (17/11/2026): triagem

Cada observação vira código (SDD), documento ou nada a fazer. Documento e roteiro ficam fora do SDD (AGENTS.md).

| # | Observação | Destino | Onde |
| --- | --- | --- | --- |
| 1 | Envio atrasado, desconto de 30% | nada a fazer | só informa |
| 2 | Desenho da arquitetura, camadas e como front e back se comunicam | documento | `docs/arquitetura.md` com um diagrama mermaid: PWA React, API ASP.NET minimal APIs, PostgreSQL com pgvector, MinIO, e-mail, provedor de IA, SignalR; só o essencial |
| 3 | Hospedagem gratuita ou VPS na AWS (créditos de 1 ano, lembrar de cancelar) | documento | seção "Hospedagem" em `docs/arquitetura.md`: `docker-compose.yml` já sobe tudo numa VPS; registrar a escolha e o lembrete de cancelamento |
| 4 | Documentação e implementação no mesmo repositório | nada a fazer | já é assim |
| 5 | Validar telas e usabilidade com o modo live do Google AI Studio | documento | roteiro de teste de usabilidade em `docs/`; as capturas `demo-live-*.png` da raiz já são desse tipo |
| 6 | `https://bitforgee.github.io/prototipo-v1-one/` devolve 404 | corrigir fora do código | o protótipo está num repositório e Pages do grupo, não aqui; republicar o Pages ou trocar o link no documento |
| 7 | Narração do vídeo por um membro do grupo | gravação | roteiro e quem narra; regravar o vídeo com a narração do grupo |
| 8 | Dados pessoais só quando o assunto exige; com integração ao sistema acadêmico podem deixar de ser necessários; exibir só em casos como recuperação de conta; exemplo de abono de falta no manual do aluno | documento e demonstração | seção "Dados pessoais e LGPD" no documento: o login institucional e o Moodle (LTI) dispensam pedir CPF, o painel e o CSV já omitem CPF, matrícula e nome (AD-066); incluir o artigo de abono de falta (do manual do aluno) na base da demonstração |
| 9 | Chamado: existe prazo? | código | `prazos-de-atendimento` (S1, S5) |
| 10 | Chamado: quando o prazo estoura, o que ocorre? | código | `prazos-de-atendimento` (S3) |
| 11 | Como evitar solicitações abertas para sempre? | código | `prazos-de-atendimento` (S3, resumo diário ao administrador) |
| 12 | Se a IA gera uma resposta padrão, por que não a gera no chat? | decisão do dono | ver "Pergunta 2" abaixo |
| 13 | Vídeo não mostrou como a gestão acompanha, sem dashboards de decisão | código e roteiro | `prazos-de-atendimento` (S4) e `validade-do-conhecimento` (S5) enriquecem o painel que já existe (AD-060); o roteiro do vídeo precisa passar pela tela `Indicadores` |
| 14 | Validade de 1 ano para documentos e artigos, com aviso aos gestores | código | `validade-do-conhecimento` |

## Pergunta 2: a resposta padrão da equipe

Hoje o assistente só responde com fonte publicada. Quando não há fonte, a conversa vira chamado, e a equipe responde com uma resposta pronta (`RespostaPronta` do setor). A resposta é a mesma de sempre, mas não existe como artigo, então o chat não a conhece. A recomendação é fechar esse ciclo: ao resolver um chamado, o atendente aciona "Criar artigo a partir desta resposta", que cria um `RASCUNHO` com o texto, a categoria do chamado e a validade de 1 ano, para o administrador revisar e ativar. O rascunho nunca entra no assistente sozinho. Isso é uma capacidade nova e só vira plano com a sua confirmação.

## Ordem sugerida

1. `validade-do-conhecimento` (independente, muda conhecimento e e-mail)
2. `prazos-de-atendimento` (muda chamados e painel)
3. Documento de arquitetura, hospedagem, LGPD e roteiro do vídeo (fora do SDD)
