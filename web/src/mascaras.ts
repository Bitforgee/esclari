export const soDigitos = (texto: string) => texto.replace(/\D/g, '')

export function cpfMascara(texto: string) {
  const d = soDigitos(texto).slice(0, 11)
  const grupos = [d.slice(0, 3), d.slice(3, 6), d.slice(6, 9)].filter(Boolean).join('.')
  return d.length > 9 ? `${grupos}-${d.slice(9)}` : grupos
}

function digitosDoTelefone(texto: string) {
  const d = soDigitos(texto)
  return (d.length > 11 && d.startsWith('55') ? d.slice(2) : d).slice(0, 11)
}

export function telefoneMascara(texto: string) {
  const d = digitosDoTelefone(texto)
  if (d.length <= 2) return d && `(${d}`
  const numero = d.slice(2)
  const corte = numero.length > 8 ? 5 : 4
  return `(${d.slice(0, 2)}) ${numero.length > corte ? `${numero.slice(0, corte)}-${numero.slice(corte)}` : numero}`
}

export function protocoloMascara(texto: string) {
  const d = soDigitos(texto).slice(0, 6)
  return d && `ESC-${d}`
}

export const enderecoMascara = (texto: string) =>
  texto
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
    .toLowerCase()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9-]/g, '')
    .slice(0, 30)

export const PropsDoCpf = { inputMode: 'numeric', maxLength: 14, spellCheck: false, placeholder: 'Ex.: 000.000.000-00…' } as const
