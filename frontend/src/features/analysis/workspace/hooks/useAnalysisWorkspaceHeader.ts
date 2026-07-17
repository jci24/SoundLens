import { useMemo } from 'react'
import type { TAnalysisLayoutMode, TAnalysisSurface } from '../../types'
import type { ISpectrumViewport } from '../../spectrum/utils/spectrumChart'
import { formatFrequencyRange } from '../../utils/analysisWorkspaceFormatting'

interface IUseAnalysisWorkspaceHeaderOptions {
  activeSurface: TAnalysisSurface
  enabledAnalysisSurfaces: readonly TAnalysisSurface[]
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
  enabledAnalysisSurfaces,
  layoutMode,
  spectrumMaximumHz,
  spectrumRangeEndHz,
  spectrumRangeStartHz,
  spectrumViewport,
}: IUseAnalysisWorkspaceHeaderOptions): IUseAnalysisWorkspaceHeaderResult =>
  useMemo(() => {
    const isMultiSurface = layoutMode === 'compare' && enabledAnalysisSurfaces.length > 1

    return {
      activeEyebrow:
        isMultiSurface
          ? 'Multi-surface analysis'
          : activeSurface === 'waveform'
            ? 'Time analysis'
            : 'Frequency analysis',
      activeTitle:
        isMultiSurface
          ? 'Waveform and spectrum overview'
          : activeSurface === 'waveform'
            ? 'Waveform overview'
            : 'Spectrum overview',
      isSpectrumRangeFiltered:
        enabledAnalysisSurfaces.includes('spectrum') &&
        (layoutMode === 'compare' || activeSurface === 'spectrum') &&
        spectrumViewport !== null &&
        (Math.round(spectrumRangeStartHz) > 0 || Math.round(spectrumRangeEndHz) < Math.round(spectrumMaximumHz)),
      spectrumRangeLabel: formatFrequencyRange(spectrumRangeStartHz, spectrumRangeEndHz),
    }
  }, [
    activeSurface,
    enabledAnalysisSurfaces,
    layoutMode,
    spectrumMaximumHz,
    spectrumRangeEndHz,
    spectrumRangeStartHz,
    spectrumViewport,
  ])

export { useAnalysisWorkspaceHeader }
