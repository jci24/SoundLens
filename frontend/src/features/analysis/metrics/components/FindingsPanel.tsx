import type { IMetricSignalItem } from '../hooks/useAnalysisWorkspaceMetrics'
import { SignalFindingsStrip } from './SignalFindingsStrip'
import './FindingsPanel.scss'

interface IFindingsPanelProps {
  signals: IMetricSignalItem[]
}

const FindingsPanel = ({ signals }: IFindingsPanelProps) => {
  const signalsWithFindings = signals.filter((signal) => signal.findings.length > 0)

  if (signalsWithFindings.length === 0) {
    return null
  }

  return (
    <section className="findings-panel" aria-label="Signal findings">
      <div className="findings-panel__body">
        {signalsWithFindings.map((signal) => (
          <div className="findings-panel__signal-group" key={signal.signalId}>
            {signalsWithFindings.length > 1 && (
              <p className="findings-panel__signal-label">
                <span className="findings-panel__signal-filename">{signal.recordingFileName}</span>
                <span className="findings-panel__signal-channel">{signal.displayName}</span>
              </p>
            )}
            <SignalFindingsStrip findings={signal.findings} />
          </div>
        ))}
      </div>
    </section>
  )
}

export { FindingsPanel }
