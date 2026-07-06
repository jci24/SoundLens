import type { ISignalFinding } from '../../types'
import './SignalFindingsStrip.scss'

interface ISignalFindingsStripProps {
  findings: ISignalFinding[]
}

const SignalFindingsStrip = ({ findings }: ISignalFindingsStripProps) => {
  if (findings.length === 0) {
    return null
  }

  return (
    <div className="signal-findings-strip">
      {findings.map((finding, index) => (
        <div
          key={`${finding.category}-${index}`}
          className={`signal-findings-strip__badge signal-findings-strip__badge--${finding.severity.toLowerCase()}`}
          title={finding.detail ?? finding.label}
        >
          <span className="signal-findings-strip__dot" aria-hidden="true" />
          <span className="signal-findings-strip__label">{finding.label}</span>
          {finding.detail && (
            <span className="signal-findings-strip__detail">{finding.detail}</span>
          )}
        </div>
      ))}
    </div>
  )
}

export { SignalFindingsStrip }
