import type { IAgentEvidenceItem } from '../types/copilot.types'
import './CopilotEvidenceBadge.scss'

interface ICopilotEvidenceBadgeProps {
  item: IAgentEvidenceItem
}

const TOOL_LABELS: Record<string, string> = {
  get_signal_metrics: 'Metrics',
  get_signal_findings: 'Findings',
  get_spectrum_summary: 'Spectrum',
  compare_signals: 'Compare',
}

const CopilotEvidenceBadge = ({ item }: ICopilotEvidenceBadgeProps) => {
  const label = TOOL_LABELS[item.toolName] ?? item.toolName

  return (
    <span className="copilot-evidence-badge" title={item.summary}>
      <span className="copilot-evidence-badge__tool">{label}</span>
      {item.signalId && (
        <span className="copilot-evidence-badge__signal">{item.signalId}</span>
      )}
    </span>
  )
}

export { CopilotEvidenceBadge }
