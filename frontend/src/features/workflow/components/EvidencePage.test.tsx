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

describe('EvidencePage', () => {
  it('uses shell-owned Copilot state and forwards the workspace toggle', async () => {
    const onCopilotToggle = vi.fn()
    render(
      <EvidencePage
        importedRecordingCount={2}
        isCopilotOpen={false}
        onCopilotToggle={onCopilotToggle}
      />
    )

    fireEvent.click(await screen.findByRole('button', { name: 'Open test Copilot for 2' }))
    expect(onCopilotToggle).toHaveBeenCalledOnce()
  })
})
