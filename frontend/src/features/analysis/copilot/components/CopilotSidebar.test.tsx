import { fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { CopilotSidebar } from './CopilotSidebar'

vi.mock('./CopilotPanel', () => ({
  CopilotPanel: () => <div>Copilot conversation</div>,
}))

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('CopilotSidebar', () => {
  it('uses a dismissible overlay sheet in a narrow workspace', () => {
    const onClose = vi.fn()

    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({
      matches: true,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    }))

    render(
      <CopilotSidebar
        isOpen
        onClose={onClose}
        recordings={[]}
        regionOfInterest={null}
        selectedSignalIds={[]}
      />
    )

    expect(screen.getByRole('dialog', { name: 'Copilot' })).toBeInTheDocument()
    expect(screen.getByText('Copilot conversation')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Close Copilot' }))
    expect(onClose).toHaveBeenCalledOnce()
  })
})
