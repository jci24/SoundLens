import { act, renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useSpectrumControlsPopover } from './useSpectrumControlsPopover'

describe('useSpectrumControlsPopover', () => {
  it('opens and toggles the popover state', () => {
    const { result } = renderHook(() => useSpectrumControlsPopover())

    expect(result.current.isOpen).toBe(false)

    act(() => {
      result.current.open()
    })

    expect(result.current.isOpen).toBe(true)

    act(() => {
      result.current.toggle()
    })

    expect(result.current.isOpen).toBe(false)
  })

  it('closes on escape', () => {
    const { result } = renderHook(() => useSpectrumControlsPopover())

    act(() => {
      result.current.open()
    })

    act(() => {
      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    })

    expect(result.current.isOpen).toBe(false)
  })

  it('closes on outside pointer down and stays open for inside clicks', () => {
    const { result } = renderHook(() => useSpectrumControlsPopover())
    const popoverElement = document.createElement('div')
    const insideElement = document.createElement('button')
    const outsideElement = document.createElement('button')

    popoverElement.appendChild(insideElement)
    document.body.append(popoverElement, outsideElement)

    act(() => {
      ;(result.current.popoverRef as { current: HTMLDivElement | null }).current = popoverElement
      result.current.open()
    })

    act(() => {
      insideElement.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }))
    })

    expect(result.current.isOpen).toBe(true)

    act(() => {
      outsideElement.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }))
    })

    expect(result.current.isOpen).toBe(false)

    popoverElement.remove()
    outsideElement.remove()
  })
})
