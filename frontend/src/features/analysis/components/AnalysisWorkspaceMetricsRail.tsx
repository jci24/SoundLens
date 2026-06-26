import {
  formatAmplitude,
  formatClippingState,
  formatCompactDuration,
  formatCompactSampleRate,
  formatCrestFactor,
} from '../utils/analysisWorkspaceFormatting'
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
        {signals.map((signal) => (
          <article
            className="time-waveform-workspace__metrics-signal"
            key={signal.signalId}
          >
            <header className="time-waveform-workspace__metrics-signal-header">
              <span className="time-waveform-workspace__metrics-recording">
                {signal.recordingFileName}
              </span>
              <span className="time-waveform-workspace__metrics-channel">
                {signal.displayName}
              </span>
            </header>

            <dl className="time-waveform-workspace__metrics-grid">
              <div className="time-waveform-workspace__metrics-item">
                <dt>Peak</dt>
                <dd>{formatAmplitude(signal.peakAmplitude)}</dd>
              </div>
              <div className="time-waveform-workspace__metrics-item">
                <dt>RMS</dt>
                <dd>{formatAmplitude(signal.rmsAmplitude)}</dd>
              </div>
              <div className="time-waveform-workspace__metrics-item">
                <dt>Crest</dt>
                <dd>{formatCrestFactor(signal.crestFactor)}</dd>
              </div>
              <div className="time-waveform-workspace__metrics-item">
                <dt>Clip</dt>
                <dd className={signal.hasClipping ? 'time-waveform-workspace__metrics-value--warning' : ''}>
                  {formatClippingState(signal.clippingSampleCount)}
                </dd>
              </div>
              <div className="time-waveform-workspace__metrics-item">
                <dt>Fs</dt>
                <dd>{formatCompactSampleRate(signal.sampleRate)}</dd>
              </div>
              <div className="time-waveform-workspace__metrics-item">
                <dt>Dur</dt>
                <dd>{formatCompactDuration(signal.durationSeconds)}</dd>
              </div>
            </dl>
          </article>
        ))}
      </div>
    </section>
  )
}

export { AnalysisWorkspaceMetricsRail }
