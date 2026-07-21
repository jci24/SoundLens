import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { ComparisonReportDialog } from './ComparisonReportDialog'

const createProps = () => ({
  excludedRecordings: [
    { recordingId: 'recording-c', fileName: 'gamma.wav', assignment: 'unassigned' as const },
  ],
  fileNameA: 'alpha.wav',
  fileNameB: 'beta.wav',
  format: 'markdown' as const,
  isExporting: false,
  isOpen: true,
  integrityAssessment: {
    status: 'limited' as const,
    limitedCheckCount: 1,
    unknownCheckCount: 1,
    checks: [
      {
        code: 'DurationScope' as const,
        status: 'limited' as const,
        label: 'Time scope',
        detail: 'The full-duration evidence covers different durations.',
      },
      {
        code: 'Calibration' as const,
        status: 'unknown' as const,
        label: 'Calibration',
        detail: 'Validated acoustic calibration is unavailable.',
      },
    ],
  },
  onExport: vi.fn(),
  onFormatChange: vi.fn(),
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
    expect(screen.getByText('1 limited · Calibration unknown')).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: /Markdown/ })).toBeChecked()

    fireEvent.change(screen.getByRole('textbox', { name: 'Report title' }), {
      target: { value: 'Edited comparison' },
    })
    expect(props.onTitleChange).toHaveBeenCalledWith('Edited comparison')
  })

  it('selects PDF accessibly and updates the export action', () => {
    const props = createProps()
    const { rerender } = render(<ComparisonReportDialog {...props} />)

    fireEvent.click(screen.getByRole('radio', { name: /PDF/ }))
    expect(props.onFormatChange).toHaveBeenCalledWith('pdf')

    rerender(<ComparisonReportDialog {...props} format="pdf" />)
    expect(screen.getByRole('radio', { name: /PDF/ })).toBeChecked()
    expect(screen.getByRole('button', { name: 'Export PDF' })).toBeEnabled()
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
