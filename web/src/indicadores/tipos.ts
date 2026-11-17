export type Linha = { categoria: string; total: number }
export type Ranking = Linha[] | null

export type Indicadores = {
  semDados: boolean
  alunosAtivos: number | null
  conversas: number | null
  taxaAutoatendimento: number | null
  taxaEscalonamento: number | null
  taxaAbandono: number | null
  taxaPerguntasSemArtigo: number | null
  chamadosAbertos: number
  chamadosResolvidos: number
  tempoPrimeiraRespostaHoras: number | null
  tempoResolucaoHoras: number | null
  roteamentoCorreto: number | null
  taxaReabertura: number | null
  principaisCategorias: { porConversas: Ranking; porChamados: Ranking }
  indicadorPrincipal: number | null
}

export type Ponto = { inicio: string; conversas: number | null; chamadosAbertos: number; chamadosResolvidos: number }
export type Desfechos = { resolvidas: number; transferidas: number; abandonadas: number; encerradas: number; semDesfecho: number }
export type Tempos = {
  primeiraRespostaMedianaHoras: number | null
  primeiraRespostaP90Horas: number | null
  resolucaoMedianaHoras: number | null
  resolucaoP90Horas: number | null
}
export type Prazos = {
  primeiraRespostaMetaHoras: number
  resolucaoMetaHoras: number
  primeiraRespostaNoPrazo: number | null
  resolucaoNoPrazo: number | null
  emAtrasoAgora: number
  maisAntigoEmAtrasoDias: number | null
}
export type Detalhe = {
  granularidade: 'dia' | 'semana'
  pontos: Ponto[]
  desfechos: Desfechos | null
  chamadosPorEstado: Record<string, number>
  maisAntigoDias: number | null
  tempos: Tempos
  prazos: Prazos
}
