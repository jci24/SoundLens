import { CopilotPanel } from './CopilotPanel'
import type { IAnalysisRegionOfInterest } from '../../types'
import './CopilotSidebar.scss'

interface ICopilotSidebarProps {
  isOpen: boolean
  selectedSignalIds: string[]
  regionOfInterest: IAnalysisRegionOfInterest | null
}

const CopilotSidebar = ({ isOpen, selectedSignalIds, regionOfInterest }: ICopilotSidebarProps) => {
  return (
    <aside className={`copilot-sidebar${isOpen ? '' : ' copilot-sidebar--closed'}`} aria-label="Copilot panel">
      <CopilotPanel
        regionOfInterest={regionOfInterest}
        selectedSignalIds={selectedSignalIds}
      />
    </aside>
  )
}

export { CopilotSidebar }
