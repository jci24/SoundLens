import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { EvidencePage } from './EvidencePage'

vi.mock('../../analysis/workspace/components/TimeWaveformWorkspace', () => ({
  TimeWaveformWorkspace: ({
    importedRecordingCount,
    isCopilotOpen,
    onCopilotToggle,
  }: {
    importedRecordingCount: number
    isCopilotOpen: boolean
    onCopilotToggle: () => void
  }) => (
    <button type="button" onClick={onCopilotToggle}>
      {isCopilotOpen ? 'Close test Copilot' : `Open test Copilot for ${importedRecordingCount}`}
    </button>
  ),
}))

vi.mock('../../analysis/copilot/components/CopilotSidebar', () => ({
  CopilotSidebar: ({ isOpen }: { isOpen: boolean }) => <div>Copilot is {isOpen ? 'open' : 'closed'}</div>,
}))

describe('EvidencePage', () => {
  it('keeps Copilot state local so route unmount closes the utility surface', async () => {
    const firstRender = render(<EvidencePage importedRecordingCount={2} />)

    fireEvent.click(await screen.findByRole('button', { name: 'Open test Copilot for 2' }))
    expect(screen.getByText('Copilot is open')).toBeInTheDocument()

    firstRender.unmount()
    render(<EvidencePage importedRecordingCount={2} />)

    expect(await screen.findByText('Copilot is closed')).toBeInTheDocument()
  })
})
