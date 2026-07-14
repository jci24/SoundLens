import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { ComparisonReportDialog } from './ComparisonReportDialog'

const createProps = () => ({
  excludedRecordings: [
    { recordingId: 'recording-c', fileName: 'gamma.wav', assignment: 'unassigned' as const },
  ],
  fileNameA: 'alpha.wav',
  fileNameB: 'beta.wav',
  isExporting: false,
  isOpen: true,
  onExport: vi.fn(),
  onOpenChange: vi.fn(),
  onTitleChange: vi.fn(),
  regionOfInterest: {
    startTimeSeconds: 0.2,
    endTimeSeconds: 0.8,
    durationSeconds: 0.6,
  },
  title: 'Alpha vs beta comparison',
})

describe('ComparisonReportDialog', () => {
  it('shows the active pair, ROI, exclusions, AI policy, and editable title', () => {
    const props = createProps()
    render(<ComparisonReportDialog {...props} />)

    expect(screen.getByRole('dialog', { name: 'Export comparison report' })).toBeInTheDocument()
    expect(screen.getByText('alpha.wav vs beta.wav')).toBeInTheDocument()
    expect(screen.getByText('0.20 s to 0.80 s')).toBeInTheDocument()
    expect(screen.getByText('gamma.wav')).toBeInTheDocument()
    expect(screen.getByText('Unassigned')).toBeInTheDocument()
    expect(screen.getByText('Automatic when available, with deterministic fallback')).toBeInTheDocument()

    fireEvent.change(screen.getByRole('textbox', { name: 'Report title' }), {
      target: { value: 'Edited comparison' },
    })
    expect(props.onTitleChange).toHaveBeenCalledWith('Edited comparison')
  })

  it('supports cancellation and disables actions while exporting', () => {
    const props = createProps()
    const { rerender } = render(<ComparisonReportDialog {...props} />)

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(props.onOpenChange).toHaveBeenCalledWith(false)

    rerender(<ComparisonReportDialog {...props} isExporting />)
    expect(screen.getByRole('button', { name: 'Preparing report...' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
  })
})
