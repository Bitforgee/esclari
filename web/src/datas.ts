export function formatarData(iso: string, fuso: string): string {
  const { dia, hora } = diaEHora(iso, fuso)
  return `${dia}/${new Intl.DateTimeFormat('pt-BR', { timeZone: fuso, year: 'numeric' }).format(new Date(iso))} ${hora}`
}

export function diaEHora(iso: string, fuso: string) {
  const partes = Object.fromEntries(
    new Intl.DateTimeFormat('pt-BR', { timeZone: fuso, day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' })
      .formatToParts(new Date(iso))
      .map((p) => [p.type, p.value]),
  )
  return { dia: `${partes.day}/${partes.month}`, hora: `${partes.hour}:${partes.minute}` }
}

const diaNoFuso = (instante: Date, fuso: string) => {
  const [ano, mes, dia] = new Intl.DateTimeFormat('en-CA', { timeZone: fuso }).format(instante).split('-').map(Number)
  return Date.UTC(ano, mes - 1, dia) / 86_400_000
}

export function abertoHa(iso: string, fuso: string, agora = new Date()): string {
  const n = diaNoFuso(agora, fuso) - diaNoFuso(new Date(iso), fuso)
  return n <= 0 ? 'aberto hoje' : n === 1 ? 'aberto ontem' : `aberto há ${n} dias`
}
