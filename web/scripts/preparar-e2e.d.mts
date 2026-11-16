type Exec = (comando: string, args: string[]) => string
export function preparar(exec?: Exec, env?: Record<string, string | undefined>, saida?: (linha: string) => void, erro?: (linha: string) => void): number
