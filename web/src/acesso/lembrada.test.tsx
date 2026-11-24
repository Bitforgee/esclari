import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { abrir, api, instituicao } from '../test/util'

const Chave = 'esclari-instituicao'
const univag = { 'GET /api/instituicoes/univag': () => [200, instituicao()] as [number, unknown] }
const naoAchada = { 'GET /api/instituicoes/univag': () => [404, { status: 404, detail: 'Instituição não encontrada.' }] as [number, unknown] }

describe('instituição lembrada', () => {
  it('grava e apaga', async () => {
    api(univag)
    const montada = abrir('/univag')
    await screen.findByLabelText('CPF')
    expect(localStorage.getItem(Chave)).toBe('univag')
    montada.unmount()

    api({ 'GET /api/instituicoes/outra': () => [404, { status: 404, detail: 'Instituição não encontrada.' }] })
    abrir('/outra')
    await screen.findByText('Instituição não encontrada.')
    expect(localStorage.getItem(Chave)).toBe('univag')
  })

  it('grava e apaga quando a lembrada deixa de existir', async () => {
    localStorage.setItem(Chave, 'univag')
    api(naoAchada)
    abrir('/univag')
    await screen.findByText('Instituição não encontrada.')
    expect(localStorage.getItem(Chave)).toBeNull()
  })

  it('volta sozinho para a última instituição', async () => {
    localStorage.setItem(Chave, 'univag')
    api(univag)
    const { roteador } = abrir('/')
    await screen.findByLabelText('CPF')
    expect(roteador.state.location.pathname).toBe('/univag')
    expect(screen.queryByLabelText('Sua instituição')).not.toBeInTheDocument()
    expect(roteador.state.historyAction).toBe('REPLACE')
  })

  it('volta sozinho mostra a escolha quando o armazenamento falha', async () => {
    const original = { le: Storage.prototype.getItem, grava: Storage.prototype.setItem }
    const le = vi.spyOn(Storage.prototype, 'getItem').mockImplementation(function (this: Storage, k) {
      if (k === Chave) throw new Error('bloqueado')
      return original.le.call(this, k)
    })
    const grava = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(function (this: Storage, k, v) {
      if (k === Chave) throw new Error('bloqueado')
      original.grava.call(this, k, v)
    })
    api(univag)
    const montada = abrir('/')
    expect(screen.getByLabelText('Sua instituição')).toBeInTheDocument()
    montada.unmount()
    abrir('/univag')
    await screen.findByLabelText('CPF')
    le.mockRestore()
    grava.mockRestore()
  })

  it('trocar de instituição', async () => {
    api(univag)
    const { roteador } = abrir('/univag')
    await screen.findByLabelText('CPF')
    expect(localStorage.getItem(Chave)).toBe('univag')
    await userEvent.click(screen.getByRole('link', { name: 'Trocar de instituição' }))
    expect(await screen.findByLabelText('Sua instituição')).toBeInTheDocument()
    expect(localStorage.getItem(Chave)).toBeNull()
    expect(roteador.state.location.pathname).toBe('/')
  })

  it('trocar de instituição quando não encontrada', async () => {
    api(naoAchada)
    abrir('/univag')
    await userEvent.click(await screen.findByRole('link', { name: 'Escolher instituição' }))
    await waitFor(() => expect(screen.getByLabelText('Sua instituição')).toBeInTheDocument())
  })
})
