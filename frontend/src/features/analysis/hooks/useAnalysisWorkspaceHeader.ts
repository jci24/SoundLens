import { useMemo } from 'react'
import type { TAnalysisSurface } from './useTimeWaveformWorkspace'
import type { ISpectrumViewport } from '../utils/spectrumChart'
import { formatFrequencyRange } from '../utils/analysisWorkspaceFormatting'

interface IUseAnalysisWorkspaceHeaderOptions {
  activeSurface: TAnalysisSurface
  spectrumMaximumHz: number
  spectrumRangeEndHz: number
  spectrumRangeStartHz: number
  spectrumViewport: ISpectrumViewport | null
}

interface IUseAnalysisWorkspaceHeaderResult {
  activeEyebrow: string
  activeTitle: string
  isSpectrumRangeFiltered: boolean
  spectrumRangeLabel: string
}

const useAnalysisWorkspaceHeader = ({
  activeSurface,
  spectrumMaximumHz,
  spectrumRangeEndHz,
  spectrumRangeStartHz,
  spectrumViewport,
}: IUseAnalysisWorkspaceHeaderOptions): IUseAnalysisWorkspaceHeaderResult =>
  useMemo(() => ({
    activeEyebrow: activeSurface === 'waveform' ? 'Time analysis' : 'Frequency analysis',
    activeTitle: activeSurface === 'waveform' ? 'Waveform overview' : 'Spectrum overview',
    isSpectrumRangeFiltered:
      activeSurface === 'spectrum' &&
      spectrumViewport !== null &&
      (Math.round(spectrumRangeStartHz) > 0 || Math.round(spectrumRangeEndHz) < Math.round(spectrumMaximumHz)),
    spectrumRangeLabel: formatFrequencyRange(spectrumRangeStartHz, spectrumRangeEndHz),
  }), [activeSurface, spectrumMaximumHz, spectrumRangeEndHz, spectrumRangeStartHz, spectrumViewport])

export { useAnalysisWorkspaceHeader }
