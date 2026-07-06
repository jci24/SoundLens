import { useMemo } from 'react'
import type {
  IFrequencySpectrumAxis,
  IFrequencySpectrumSignal,
  ITimeWaveformSignal,
  ITimeWaveformResponse,
} from '../../types'

type TAnalysisPanelSurface = 'waveform' | 'spectrum'

interface IAnalysisWorkspacePanel {
  error: string | null
  isInitialLoading: boolean
  isRefreshing: boolean
  loadingLabel: string
  refreshingLabel: string
  surface: TAnalysisPanelSurface
  title: string
}

interface IUseAnalysisWorkspacePanelsOptions {
  chartWidth: number
  isSpectrumInitialLoading: boolean
  isSpectrumRefreshing: boolean
  isWaveformInitialLoading: boolean
  isWaveformRefreshing: boolean
  showSpectrumPanel: boolean
  showWaveformPanel: boolean
  spectrumError: string | null
  spectrumSignals: IFrequencySpectrumSignal[]
  spectrumXAxis: IFrequencySpectrumAxis | null
  spectrumYAxis: IFrequencySpectrumAxis | null
  waveformError: string | null
  waveformSignals: ITimeWaveformSignal[]
  waveformYAxis: ITimeWaveformResponse['yAxis'] | null
}

interface IUseAnalysisWorkspacePanelsResult {
  hasActiveChart: boolean
  panels: IAnalysisWorkspacePanel[]
}

const useAnalysisWorkspacePanels = ({
  chartWidth,
  isSpectrumInitialLoading,
  isSpectrumRefreshing,
  isWaveformInitialLoading,
  isWaveformRefreshing,
  showSpectrumPanel,
  showWaveformPanel,
  spectrumError,
  spectrumSignals,
  spectrumXAxis,
  spectrumYAxis,
  waveformError,
  waveformSignals,
  waveformYAxis,
}: IUseAnalysisWorkspacePanelsOptions): IUseAnalysisWorkspacePanelsResult =>
  useMemo(() => {
    const panels: IAnalysisWorkspacePanel[] = []

    if (showWaveformPanel) {
      const hasWaveformChart = waveformSignals.length > 0 && waveformYAxis !== null && chartWidth > 0

      panels.push({
        error: waveformError,
        isInitialLoading: isWaveformInitialLoading,
        isRefreshing: isWaveformRefreshing && hasWaveformChart,
        loadingLabel: 'Generating waveform bins',
        refreshingLabel: 'Updating waveform',
        surface: 'waveform',
        title: 'Waveform',
      })
    }

    if (showSpectrumPanel) {
      const hasSpectrumChart =
        spectrumSignals.length > 0 && spectrumXAxis !== null && spectrumYAxis !== null && chartWidth > 0

      panels.push({
        error: spectrumError,
        isInitialLoading: isSpectrumInitialLoading,
        isRefreshing: isSpectrumRefreshing && hasSpectrumChart,
        loadingLabel: 'Generating spectrum bins',
        refreshingLabel: 'Updating spectrum',
        surface: 'spectrum',
        title: 'Spectrum',
      })
    }

    return {
      hasActiveChart:
        panels.length > 0 &&
        panels.some((panel) => {
          if (panel.surface === 'waveform') {
            return waveformSignals.length > 0 && waveformYAxis !== null && chartWidth > 0
          }

          return spectrumSignals.length > 0 && spectrumXAxis !== null && spectrumYAxis !== null && chartWidth > 0
        }),
      panels,
    }
  }, [
    chartWidth,
    isSpectrumInitialLoading,
    isSpectrumRefreshing,
    isWaveformInitialLoading,
    isWaveformRefreshing,
    showSpectrumPanel,
    showWaveformPanel,
    spectrumError,
    spectrumSignals,
    spectrumXAxis,
    spectrumYAxis,
    waveformError,
    waveformSignals,
    waveformYAxis,
  ])

export { useAnalysisWorkspacePanels }
export type { IAnalysisWorkspacePanel, TAnalysisPanelSurface }
