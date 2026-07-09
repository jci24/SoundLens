import { useAnalysisWorkspaceStore } from '../../stores/useAnalysisWorkspaceStore'
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
  const recordings = useAnalysisWorkspaceStore((state) => state.recordings)
  const normalizedToolName = item.toolName.replace(/^functions\./, '')
  const label = TOOL_LABELS[normalizedToolName] ?? normalizedToolName

  const signal = item.signalId
    ? recordings.flatMap((r) =>
        r.signals.map((s) => ({
          signalId: s.signalId,
          displayName: s.displayName,
          fileName: r.fileName,
        }))
      ).find((s) => s.signalId === item.signalId)
    : null

  const signalDisplay = signal
    ? `${signal.fileName} · ${signal.displayName}`
    : item.signalId || null

  return (
    <span className="copilot-evidence-badge" title={item.summary}>
      <span className="copilot-evidence-badge__tool">{label}</span>
      {signalDisplay && (
        <span className="copilot-evidence-badge__signal">{signalDisplay}</span>
      )}
    </span>
  )
}

export { CopilotEvidenceBadge }
