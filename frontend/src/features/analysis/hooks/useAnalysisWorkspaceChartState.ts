import { useMemo } from 'react'
import type {
  IFrequencySpectrumAxis,
  IFrequencySpectrumResponse,
  IFrequencySpectrumSignal,
  ITimeWaveformSignal,
  ITimeWaveformResponse,
} from '../types'
import type { TAnalysisSurface } from './useTimeWaveformWorkspace'

interface IUseAnalysisWorkspaceChartStateOptions {
  activeSurface: TAnalysisSurface
  chartWidth: number
  spectrum: IFrequencySpectrumResponse | null
  spectrumSignals: IFrequencySpectrumSignal[]
  spectrumXAxis: IFrequencySpectrumAxis | null
  waveforms: ITimeWaveformResponse | null
  waveformSignals: ITimeWaveformSignal[]
}

interface IUseAnalysisWorkspaceChartStateResult {
  hasActiveChart: boolean
  loadingLabel: string
  refreshingLabel: string
  spectrumYAxis: IFrequencySpectrumResponse['yAxis'] | null
  waveformYAxis: ITimeWaveformResponse['yAxis'] | null
}

const useAnalysisWorkspaceChartState = ({
  activeSurface,
  chartWidth,
  spectrum,
  spectrumSignals,
  spectrumXAxis,
  waveforms,
  waveformSignals,
}: IUseAnalysisWorkspaceChartStateOptions): IUseAnalysisWorkspaceChartStateResult =>
  useMemo(() => {
    const waveformYAxis = waveforms?.yAxis ?? null
    const spectrumYAxis = spectrum?.yAxis ?? null
    const hasActiveChart =
      activeSurface === 'waveform'
        ? waveformSignals.length > 0 && waveformYAxis !== null && chartWidth > 0
        : spectrumSignals.length > 0 && spectrumXAxis !== null && spectrumYAxis !== null && chartWidth > 0

    return {
      hasActiveChart,
      loadingLabel: activeSurface === 'waveform' ? 'Generating waveform bins' : 'Generating spectrum bins',
      refreshingLabel: activeSurface === 'waveform' ? 'Updating overlay' : 'Updating spectrum',
      spectrumYAxis,
      waveformYAxis,
    }
  }, [activeSurface, chartWidth, spectrum, spectrumSignals, spectrumXAxis, waveforms, waveformSignals])

export { useAnalysisWorkspaceChartState }
