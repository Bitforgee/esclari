import { describe, expect, it } from 'vitest'
import { cpfMascara, enderecoMascara, protocoloMascara, soDigitos, telefoneMascara } from './mascaras'

describe('máscaras', () => {
  it('cpf agrupa aos poucos', () => {
    expect(['', '0', '046', '0464', '046473', '0464733', '046473331', '0464733316', '04647333167'].map(cpfMascara)).toEqual([
      '',
      '0',
      '046',
      '046.4',
      '046.473',
      '046.473.3',
      '046.473.331',
      '046.473.331-6',
      '046.473.331-67',
    ])
  })

  it('cpf ignora o que não é dígito', () => {
    expect(cpfMascara('0a4b6')).toBe('046')
    expect(cpfMascara('abc')).toBe('')
  })

  it('cpf colado com ou sem pontos', () => {
    expect(cpfMascara('046.473.331-67')).toBe('046.473.331-67')
    expect(cpfMascara('04647333167')).toBe('046.473.331-67')
    expect(soDigitos('046.473.331-67')).toBe('04647333167')
  })

  it('cpf corta em 11', () => {
    expect(cpfMascara('046473331679999')).toBe('046.473.331-67')
  })

  it('telefone com 10 e 11 dígitos', () => {
    expect(telefoneMascara('6533211234')).toBe('(65) 3321-1234')
    expect(telefoneMascara('65999991234')).toBe('(65) 99999-1234')
    expect(telefoneMascara('6')).toBe('(6')
    expect(telefoneMascara('65')).toBe('(65')
    expect(telefoneMascara('659')).toBe('(65) 9')
    expect(telefoneMascara('6533211')).toBe('(65) 3321-1')
    expect(telefoneMascara('')).toBe('')
  })

  it('telefone tira o 55', () => {
    expect(telefoneMascara('+55 65 99999-1234')).toBe('(65) 99999-1234')
    expect(telefoneMascara('556533211234')).toBe('(65) 3321-1234')
  })

  it('telefone corta em 11', () => {
    expect(telefoneMascara('659999912349999')).toBe('(65) 99999-1234')
  })

  it('protocolo', () => {
    expect(protocoloMascara('esc123456')).toBe('ESC-123456')
    expect(protocoloMascara('1234567')).toBe('ESC-123456')
    expect(protocoloMascara('ESC-')).toBe('')
    expect(protocoloMascara('abc')).toBe('')
    expect(protocoloMascara('ESC-0007')).toBe('ESC-0007')
  })

  it('endereço', () => {
    expect(enderecoMascara('Faculdade São João')).toBe('faculdade-sao-joao')
    expect(enderecoMascara('A_b!c')).toBe('abc')
    expect(enderecoMascara('a'.repeat(40))).toBe('a'.repeat(30))
    expect(enderecoMascara('univag-sul')).toBe('univag-sul')
  })
})
