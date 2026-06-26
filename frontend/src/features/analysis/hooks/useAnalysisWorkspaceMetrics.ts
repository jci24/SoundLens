import { useMemo } from 'react'
import type { IFrequencySpectrumSignal, ITimeWaveformSignal } from '../types'

interface IMetricSignalItem {
  signalId: string
  recordingFileName: string
  displayName: string
  durationSeconds: number
  sampleRate: number
  peakAmplitude: number
  rmsAmplitude: number
  crestFactor: number
  clippingSampleCount: number
  hasClipping: boolean
}

interface IUseAnalysisWorkspaceMetricsOptions {
  spectrumSignals: IFrequencySpectrumSignal[]
  waveformSignals: ITimeWaveformSignal[]
}

interface IUseAnalysisWorkspaceMetricsResult {
  hasMetricsPending: boolean
  metricSignals: IMetricSignalItem[]
}

const defaultMetrics = {
  peakAmplitude: 0,
  rmsAmplitude: 0,
  crestFactor: 0,
  clippingSampleCount: 0,
  hasClipping: false,
}

const useAnalysisWorkspaceMetrics = ({
  spectrumSignals,
  waveformSignals,
}: IUseAnalysisWorkspaceMetricsOptions): IUseAnalysisWorkspaceMetricsResult =>
  useMemo(() => {
    const sourceSignals = waveformSignals.length > 0 ? waveformSignals : spectrumSignals

    return {
      hasMetricsPending: sourceSignals.some((signal) => signal.metrics === undefined),
      metricSignals: sourceSignals.map((signal) => ({
        ...defaultMetrics,
        signalId: signal.signalId,
        recordingFileName: signal.recordingFileName,
        displayName: signal.displayName,
        durationSeconds: signal.durationSeconds,
        sampleRate: signal.sampleRate,
        peakAmplitude: signal.metrics?.peakAmplitude ?? defaultMetrics.peakAmplitude,
        rmsAmplitude: signal.metrics?.rmsAmplitude ?? defaultMetrics.rmsAmplitude,
        crestFactor: signal.metrics?.crestFactor ?? defaultMetrics.crestFactor,
        clippingSampleCount: signal.metrics?.clippingSampleCount ?? defaultMetrics.clippingSampleCount,
        hasClipping: signal.metrics?.hasClipping ?? defaultMetrics.hasClipping,
      })),
    }
  }, [spectrumSignals, waveformSignals])

export { useAnalysisWorkspaceMetrics }
export type { IMetricSignalItem }
