# Tela de criar senha do operador checks

Profile: standard

## Intent

O convite do operador (`plataforma operador --email --nome`) imprime o link `https://<site>/plataforma/definir-senha?token=...`, e a API já tem `POST /api/plataforma/auth/definir-senha`, mas a web não tinha essa tela: o link caía na entrada do console e o operador não conseguia criar a senha. Achado no primeiro deploy de produção (18/11). A correção acrescenta a rota `/plataforma/definir-senha` fora da guarda da sessão, com "Nova senha" e "Repita a senha", que envia `{ token, senha }` e depois manda para a entrada do console. Sem decisão difícil de desfazer.

## Checks

**C1** - `/plataforma/definir-senha?token=abc123` mostra "Nova senha" e "Repita a senha" (sem o campo de e-mail), recusa senhas diferentes com "As senhas não são iguais.", envia `{ token: "abc123", senha }` e mostra "Senha criada. Agora entre no console e cadastre o aplicativo autenticador." com o link para `/plataforma`
Proof: `npm --prefix web test -- src/plataforma/entrada.test.tsx -t "definir senha da plataforma pelo link do convite"`

**C2** - Um `400` de token vencido mostra "O link é inválido ou venceu. Peça um convite novo."
Proof: `npm --prefix web test -- src/plataforma/entrada.test.tsx -t "link vencido mostra a mensagem da API"`

## Handoff

- Correção publicada antes da verificação para não vencer o convite de 60 minutos do primeiro operador; verificação independente feita depois, com PASS (`verification.md`)
