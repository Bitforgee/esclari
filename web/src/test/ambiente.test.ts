import { execFileSync } from 'node:child_process'
import { readFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

const raizDoRepositorio = resolve(process.cwd(), '..')

describe('ambiente de desenvolvimento', () => {
  it('nome do projeto compose', () => {
    const compose = resolve(raizDoRepositorio, 'docker-compose.yml')
    expect(readFileSync(compose, 'utf8').split(/\r?\n/)[0]).toBe('name: esclari')
    const json = execFileSync('docker', ['compose', '-f', compose, 'config', '--format', 'json'], { cwd: tmpdir(), encoding: 'utf8' })
    expect(JSON.parse(json).name).toBe('esclari')
  }, 60_000)

  it('container padrao do e2e', () => {
    const spec = readFileSync(resolve(process.cwd(), 'e2e/instituicoes-real.spec.ts'), 'utf8')
    expect(spec).toContain("process.env.E2E_CONTAINER ?? 'esclari-api-1'")
  })
})
