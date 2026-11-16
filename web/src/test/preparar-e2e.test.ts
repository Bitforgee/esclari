import { describe, expect, it } from 'vitest'
import { preparar } from '../../scripts/preparar-e2e.mjs'

function ambiente(respostaDoBanco: string, quebra?: (args: string[]) => boolean) {
  const chamadas: string[][] = []
  const saida: string[] = []
  const erro: string[] = []
  const exec = (_: string, args: string[]) => {
    chamadas.push(args)
    if (quebra?.(args)) throw new Error('sem contêiner')
    return args.includes('psql') ? respostaDoBanco : ''
  }
  return { chamadas, saida, erro, exec, executar: (env = {}) => preparar(exec, env, (l) => saida.push(l), (l) => erro.push(l)) }
}

const criacoes = (chamadas: string[][]) => chamadas.filter((a) => a.includes('criar'))

describe('preparar e2e', () => {
  it('cria demo e e2e', () => {
    const a = ambiente('0\n')
    expect(a.executar()).toBe(0)
    expect(a.saida).toEqual(['criada demo', 'criada e2e'])
    const [demo, e2e] = criacoes(a.chamadas)
    expect(demo).toContain('esclari-api-1')
    expect(demo.join(' ')).toContain('--slug demo')
    expect(demo).toContain('admin@demo.esclari')
    expect(e2e.join(' ')).toContain('--slug e2e')
    expect(e2e).toContain('admin@e2e.esclari')
  })

  it('segunda rodada ja existe', () => {
    const a = ambiente('1\n')
    expect(a.executar()).toBe(0)
    expect(a.saida).toEqual(['ja existe demo', 'ja existe e2e'])
    expect(criacoes(a.chamadas)).toEqual([])
  })

  it('conteiner ausente sai 1', () => {
    const a = ambiente('0\n', () => true)
    expect(a.executar({ E2E_CONTAINER: 'meu-conteiner-api-1' })).toBe(1)
    expect(a.erro.join(' ')).toContain('meu-conteiner-api-1')
    expect(a.saida).toEqual([])
    expect(criacoes(a.chamadas)).toEqual([])
  })
})
