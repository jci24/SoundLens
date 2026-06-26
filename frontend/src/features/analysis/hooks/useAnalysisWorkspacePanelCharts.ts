import { useMemo } from 'react'
import type { IFrequencySpectrumSignal, ITimeWaveformSignal } from '../types'
import type { IAnalysisWorkspacePanel } from './useAnalysisWorkspacePanels'
import type { TSignalChartMode } from './useTimeWaveformWorkspace'

interface IAnalysisWorkspacePanelChartItem {
  chartKey: string
  spectrumSignals: IFrequencySpectrumSignal[]
  title: string | null
  waveformSignals: ITimeWaveformSignal[]
}

interface IUseAnalysisWorkspacePanelChartsOptions {
  panel: IAnalysisWorkspacePanel
  signalChartMode: TSignalChartMode
  spectrumSignals: IFrequencySpectrumSignal[]
  waveformSignals: ITimeWaveformSignal[]
}

const useAnalysisWorkspacePanelCharts = ({
  panel,
  signalChartMode,
  spectrumSignals,
  waveformSignals,
}: IUseAnalysisWorkspacePanelChartsOptions) =>
  useMemo<IAnalysisWorkspacePanelChartItem[]>(() => {
    if (panel.surface === 'waveform') {
      if (signalChartMode === 'split' && waveformSignals.length > 1) {
        return waveformSignals.map((signal) => ({
          chartKey: signal.signalId,
          spectrumSignals: [],
          title: `${signal.recordingFileName} · ${signal.displayName}`,
          waveformSignals: [signal],
        }))
      }

      return [
        {
          chartKey: panel.surface,
          spectrumSignals: [],
          title: null,
          waveformSignals,
        },
      ]
    }

    if (signalChartMode === 'split' && spectrumSignals.length > 1) {
      return spectrumSignals.map((signal) => ({
        chartKey: signal.signalId,
        spectrumSignals: [signal],
        title: `${signal.recordingFileName} · ${signal.displayName}`,
        waveformSignals: [],
      }))
    }

    return [
      {
        chartKey: panel.surface,
        spectrumSignals,
        title: null,
        waveformSignals: [],
      },
    ]
  }, [panel, signalChartMode, spectrumSignals, waveformSignals])

export { useAnalysisWorkspacePanelCharts }
