import { CopilotPanel } from './CopilotPanel'
import { useCopilotSidebarResize } from '../hooks/useCopilotSidebarResize'
import type { IAnalysisRegionOfInterest, ITimeWaveformRecording } from '../../types'
import './CopilotSidebar.scss'

interface ICopilotSidebarProps {
  isOpen: boolean
  selectedSignalIds: string[]
  regionOfInterest: IAnalysisRegionOfInterest | null
  recordings: ITimeWaveformRecording[]
}

const CopilotSidebar = ({ isOpen, selectedSignalIds, regionOfInterest, recordings }: ICopilotSidebarProps) => {
  const { width, onMouseDown } = useCopilotSidebarResize()

  return (
    <aside
      className={`copilot-sidebar${isOpen ? '' : ' copilot-sidebar--closed'}`}
      style={isOpen ? { width } : undefined}
      aria-label="Copilot panel"
    >
      <div
        className="copilot-sidebar__resize-handle"
        role="separator"
        aria-orientation="vertical"
        aria-label="Resize copilot panel"
        onMouseDown={onMouseDown}
      />
      <CopilotPanel
        recordings={recordings}
        regionOfInterest={regionOfInterest}
        selectedSignalIds={selectedSignalIds}
      />
    </aside>
  )
}

export { CopilotSidebar }
