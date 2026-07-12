import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { AnalysisWorkspaceHeader } from './AnalysisWorkspaceHeader'

const createProps = () => ({
  activeSurface: 'waveform' as const,
  canEnterCompareMode: true,
  isCopilotOpen: false,
  isExporting: false,
  layoutMode: 'focused' as const,
  onCopilotToggle: vi.fn(),
  onExportReport: vi.fn(),
  onLayoutModeChange: vi.fn(),
  onSignalChartModeChange: vi.fn(),
  onSpectrumPresetChange: vi.fn(),
  onSpectrumRangeEndChange: vi.fn(),
  onSpectrumRangeReset: vi.fn(),
  onSpectrumRangeStartChange: vi.fn(),
  onSurfaceChange: vi.fn(),
  selectedSignalCount: 2,
  selectedSpectrumPreset: '4096',
  signalChartMode: 'overlay' as const,
  showSpectrumPanel: true,
  spectrumFftSizeOptions: ['1024', '4096'],
  spectrumMaximumHz: 22_050,
  spectrumRangeEndHz: 8_000,
  spectrumRangeStartHz: 125,
  spectrumViewport: {
    startHz: 125,
    endHz: 8_000,
  },
})

describe('AnalysisWorkspaceHeader', () => {
  it('renders workspace navigation controls and dispatches tab changes', () => {
    const props = createProps()

    render(<AnalysisWorkspaceHeader {...props} />)

    expect(screen.getByText('Time analysis')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Waveform overview' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Export report' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Waveform' })).toHaveAttribute('data-state', 'active')
    expect(screen.getByRole('tab', { name: 'Overlay' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Split' })).toBeInTheDocument()

    fireEvent.mouseDown(screen.getByRole('tab', { name: 'Spectrum' }), { button: 0 })
    fireEvent.mouseDown(screen.getByRole('tab', { name: 'Compare' }), { button: 0 })
    fireEvent.mouseDown(screen.getByRole('tab', { name: 'Split' }), { button: 0 })

    expect(props.onSurfaceChange).toHaveBeenCalledWith('spectrum')
    expect(props.onLayoutModeChange).toHaveBeenCalledWith('compare')
    expect(props.onSignalChartModeChange).toHaveBeenCalledWith('split')
  })

  it('shows filtered spectrum controls and opens the settings panel', () => {
    render(<AnalysisWorkspaceHeader {...createProps()} activeSurface="spectrum" />)

    expect(screen.getByText('125 Hz - 8.0k Hz')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Spectrum settings' }))

    expect(screen.getByRole('dialog', { name: 'Spectrum settings' })).toBeInTheDocument()
    expect(screen.getByText('FFT')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Reset' })).toBeInTheDocument()
  })

  it('disables export while the report context is being prepared', () => {
    render(<AnalysisWorkspaceHeader {...createProps()} isExporting />)

    expect(screen.getByRole('button', { name: 'Preparing export...' })).toBeDisabled()
  })

  it('keeps compare mode unavailable until both groups are ready', () => {
    render(<AnalysisWorkspaceHeader {...createProps()} canEnterCompareMode={false} />)

    expect(screen.getByRole('tab', { name: 'Compare' })).toBeDisabled()
  })
})
