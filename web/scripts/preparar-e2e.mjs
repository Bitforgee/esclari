import { execFileSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'

const INSTITUICOES = [
  { slug: 'demo', nome: 'Demo', cpf: '12345678909', adminNome: 'Admin Demo', email: 'admin@demo.esclari' },
  { slug: 'e2e', nome: 'E2E', cpf: '12345678909', adminNome: 'Admin E2E', email: 'admin@e2e.esclari' },
]

const execReal = (comando, args) => execFileSync(comando, args, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] })

export function preparar(exec = execReal, env = process.env, saida = console.log, erro = console.error) {
  const api = env.E2E_CONTAINER ?? 'esclari-api-1'
  const banco = env.E2E_POSTGRES ?? api.replace(/-api-1$/, '-postgres-1')
  try {
    exec('docker', ['exec', api, 'true'])
  } catch {
    erro(`e2e:preparar: não consegui falar com o contêiner "${api}". Suba o ambiente com "docker compose up -d --build" ou aponte E2E_CONTAINER para o contêiner da API.`)
    return 1
  }
  try {
    for (const i of INSTITUICOES) {
      const existe = exec('docker', ['exec', banco, 'psql', '-U', 'postgres', '-d', 'esclari', '-At', '-c', `select count(*) from instituicao where slug = '${i.slug}'`]).trim() !== '0'
      if (existe) {
        saida(`ja existe ${i.slug}`)
        continue
      }
      exec('docker', ['exec', api, 'dotnet', 'Esclari.Api.dll', 'instituicao', 'criar', '--slug', i.slug, '--nome', i.nome, '--fuso', 'America/Cuiaba', '--admin-cpf', i.cpf, '--admin-nome', i.adminNome, '--admin-email', i.email])
      saida(`criada ${i.slug}`)
    }
  } catch (e) {
    erro(`e2e:preparar: falhou ao preparar as instituições no contêiner "${api}" (banco "${banco}"): ${String(e.stderr ?? e.message).trim()}`)
    return 1
  }
  return 0
}

if (process.argv[1] === fileURLToPath(import.meta.url)) process.exit(preparar())
