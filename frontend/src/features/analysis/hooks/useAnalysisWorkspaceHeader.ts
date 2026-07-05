import { useMemo } from 'react'
import type { TAnalysisLayoutMode, TAnalysisSurface } from '../types'
import type { ISpectrumViewport } from '../utils/spectrumChart'
import { formatFrequencyRange } from '../utils/analysisWorkspaceFormatting'

interface IUseAnalysisWorkspaceHeaderOptions {
  activeSurface: TAnalysisSurface
  layoutMode: TAnalysisLayoutMode
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
  layoutMode,
  spectrumMaximumHz,
  spectrumRangeEndHz,
  spectrumRangeStartHz,
  spectrumViewport,
}: IUseAnalysisWorkspaceHeaderOptions): IUseAnalysisWorkspaceHeaderResult =>
  useMemo(() => ({
    activeEyebrow:
      layoutMode === 'compare'
        ? 'Multi-surface analysis'
        : activeSurface === 'waveform'
          ? 'Time analysis'
          : 'Frequency analysis',
    activeTitle:
      layoutMode === 'compare'
        ? 'Waveform and spectrum overview'
        : activeSurface === 'waveform'
          ? 'Waveform overview'
          : 'Spectrum overview',
    isSpectrumRangeFiltered:
      (layoutMode === 'compare' || activeSurface === 'spectrum') &&
      spectrumViewport !== null &&
      (Math.round(spectrumRangeStartHz) > 0 || Math.round(spectrumRangeEndHz) < Math.round(spectrumMaximumHz)),
    spectrumRangeLabel: formatFrequencyRange(spectrumRangeStartHz, spectrumRangeEndHz),
  }), [activeSurface, layoutMode, spectrumMaximumHz, spectrumRangeEndHz, spectrumRangeStartHz, spectrumViewport])

export { useAnalysisWorkspaceHeader }
