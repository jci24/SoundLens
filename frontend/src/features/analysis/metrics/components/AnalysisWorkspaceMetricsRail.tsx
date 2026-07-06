import {
  formatAmplitude,
  formatClippingState,
  formatCompactDuration,
  formatCompactSampleRate,
  formatCrestFactor,
} from '../../utils/analysisWorkspaceFormatting'
import type { IMetricSignalItem } from '../hooks/useAnalysisWorkspaceMetrics'
import './AnalysisWorkspaceMetricsRail.scss'

interface IAnalysisWorkspaceMetricsRailProps {
  hasMetricsPending: boolean
  signals: IMetricSignalItem[]
}

const AnalysisWorkspaceMetricsRail = ({
  hasMetricsPending,
  signals,
}: IAnalysisWorkspaceMetricsRailProps) => {
  if (signals.length === 0) {
    return null
  }

  return (
    <section className="time-waveform-workspace__metrics-rail" aria-label="Selected signal metrics">
      <div className="time-waveform-workspace__metrics-rail-header">
        <span className="time-waveform-workspace__metrics-rail-title">Signal metrics</span>
        {hasMetricsPending && (
          <span className="time-waveform-workspace__metrics-rail-hint">
            Restart backend to load live metrics
          </span>
        )}
      </div>

      <div className="time-waveform-workspace__metrics-scroll">
        <table className="time-waveform-workspace__metrics-table" aria-label="Signal metrics table">
          <thead>
            <tr>
              <th scope="col">Signal</th>
              <th scope="col">Peak</th>
              <th scope="col">RMS</th>
              <th scope="col">Crest</th>
              <th scope="col">Clip</th>
              <th scope="col">Fs</th>
              <th scope="col">Dur</th>
            </tr>
          </thead>
          <tbody>
            {signals.map((signal) => (
              <tr key={signal.signalId}>
                <td className="time-waveform-workspace__metrics-signal-cell">
                  <span className="time-waveform-workspace__metrics-recording">{signal.recordingFileName}</span>
                  <span className="time-waveform-workspace__metrics-channel">{signal.displayName}</span>
                </td>
                <td>{formatAmplitude(signal.peakAmplitude)}</td>
                <td>{formatAmplitude(signal.rmsAmplitude)}</td>
                <td>{formatCrestFactor(signal.crestFactor)}</td>
                <td className={signal.hasClipping ? 'time-waveform-workspace__metrics-value--warning' : ''}>
                  {formatClippingState(signal.clippingSampleCount)}
                </td>
                <td>{formatCompactSampleRate(signal.sampleRate)}</td>
                <td>{formatCompactDuration(signal.durationSeconds)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

export { AnalysisWorkspaceMetricsRail }
