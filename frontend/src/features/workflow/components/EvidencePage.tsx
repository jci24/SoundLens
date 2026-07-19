import { lazy, Suspense, useState } from 'react'
import { useAnalysisWorkspaceStore } from '../../analysis/stores/useAnalysisWorkspaceStore'
import { CopilotSidebar } from '../../analysis/copilot/components/CopilotSidebar'
import { RouteState } from './RouteState'
import './EvidencePage.scss'

const TimeWaveformWorkspace = lazy(async () => {
  const module = await import('../../analysis/workspace/components/TimeWaveformWorkspace')
  return { default: module.TimeWaveformWorkspace }
})

interface IEvidencePageProps {
  importedRecordingCount: number
}

const EvidencePage = ({ importedRecordingCount }: IEvidencePageProps) => {
  const [isCopilotOpen, setIsCopilotOpen] = useState(false)
  const selectedSignalIds = useAnalysisWorkspaceStore((state) => state.selectedSignalIds)
  const regionOfInterest = useAnalysisWorkspaceStore((state) => state.regionOfInterest)
  const recordings = useAnalysisWorkspaceStore((state) => state.recordings)

  return (
    <div className="evidence-page">
      <div className="evidence-page__workspace">
        <Suspense fallback={<RouteState title="Loading evidence workspace" />}>
          <TimeWaveformWorkspace
            importedRecordingCount={importedRecordingCount}
            isCopilotOpen={isCopilotOpen}
            onCopilotToggle={() => setIsCopilotOpen((current) => !current)}
          />
        </Suspense>
      </div>
      <CopilotSidebar
        isOpen={isCopilotOpen}
        onClose={() => setIsCopilotOpen(false)}
        recordings={recordings}
        regionOfInterest={regionOfInterest}
        selectedSignalIds={selectedSignalIds}
      />
    </div>
  )
}

export { EvidencePage }
