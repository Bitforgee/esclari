import { useMutation } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { BotaoAcao } from '../acao'
import { api } from '../api'
import { Campo } from '../Campo'
import { ErroDe, errosDeCampo } from '../ErroDe'
import { Alert } from '../ui/alert'
import { Moldura } from './Entrada'

function Formulario({ token, pronto }: { token: string; pronto: () => void }) {
  const [senha, setSenha] = useState('')
  const [repetida, setRepetida] = useState('')
  const definir = useMutation({ mutationFn: () => api('/api/plataforma/auth/definir-senha', { token, senha }), onSuccess: pronto })
  const campos = errosDeCampo(definir.error)
  const diferente = repetida !== '' && repetida !== senha ? 'As senhas não são iguais.' : undefined
  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(e) => {
        e.preventDefault()
        if (!diferente) definir.mutate()
      }}
    >
      <Campo id="nova-senha" rotulo="Nova senha" type="password" autoComplete="new-password" dica="De 8 a 128 caracteres." value={senha} erro={campos.senha} onChange={(e) => setSenha(e.target.value)} />
      <Campo id="repetir-senha" rotulo="Repita a senha" type="password" autoComplete="new-password" value={repetida} erro={diferente} onChange={(e) => setRepetida(e.target.value)} />
      {campos.token && <Alert variant="destructive">{campos.token}</Alert>}
      <ErroDe erro={definir.error} campos={['senha', 'token']} />
      <BotaoAcao type="submit" pendente={definir.isPending} disabled={!senha || !!diferente} className="h-11">
        Criar senha
      </BotaoAcao>
    </form>
  )
}

export function DefinirSenhaDaPlataforma() {
  const token = useSearchParams()[0].get('token') ?? ''
  const [pronta, setPronta] = useState(false)
  return (
    <Moldura>
      {pronta ? (
        <div className="flex flex-col items-start gap-3">
          <Alert role="status">Senha criada. Agora entre no console e cadastre o aplicativo autenticador.</Alert>
          <Link to="/plataforma" className="inline-flex min-h-11 items-center underline">
            Entrar no console
          </Link>
        </div>
      ) : (
        <Formulario token={token} pronto={() => setPronta(true)} />
      )}
    </Moldura>
  )
}
