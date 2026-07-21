import { lazy, Suspense } from 'react'
import { RouteState } from './RouteState'
import './EvidencePage.scss'

const TimeWaveformWorkspace = lazy(async () => {
  const module = await import('../../analysis/workspace/components/TimeWaveformWorkspace')
  return { default: module.TimeWaveformWorkspace }
})

interface IEvidencePageProps {
  importedRecordingCount: number
  isCopilotOpen: boolean
  onCopilotToggle: () => void
}

const EvidencePage = ({ importedRecordingCount, isCopilotOpen, onCopilotToggle }: IEvidencePageProps) => {
  return (
    <div className="evidence-page">
      <div className="evidence-page__workspace">
        <Suspense fallback={<RouteState title="Loading evidence workspace" />}>
          <TimeWaveformWorkspace
            importedRecordingCount={importedRecordingCount}
            isCopilotOpen={isCopilotOpen}
            onCopilotToggle={onCopilotToggle}
          />
        </Suspense>
      </div>
    </div>
  )
}

export { EvidencePage }
