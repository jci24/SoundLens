import * as Dialog from '@radix-ui/react-dialog'
import { X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useMediaQuery } from '@/common/hooks/useMediaQuery'
import { CopilotPanel } from './CopilotPanel'
import { useCopilotSidebarResize } from '../hooks/useCopilotSidebarResize'
import type { IAnalysisRegionOfInterest, ITimeWaveformRecording } from '../../types'
import type { TCopilotRouteName } from '../types/copilot.types'
import './CopilotSidebar.scss'

interface ICopilotSidebarProps {
  isOpen: boolean
  onClose: () => void
  selectedSignalIds: string[]
  regionOfInterest: IAnalysisRegionOfInterest | null
  recordings: ITimeWaveformRecording[]
  routeName?: TCopilotRouteName
  hasRecordings?: boolean
  onNavigate?: (path: string) => void
}

const CopilotSidebar = ({
  isOpen,
  onClose,
  selectedSignalIds,
  regionOfInterest,
  recordings,
  routeName = 'evidence',
  hasRecordings = false,
  onNavigate,
}: ICopilotSidebarProps) => {
  const isNarrowWorkspace = useMediaQuery('(max-width: 900px)')
  const { width, onMouseDown } = useCopilotSidebarResize()
  const panel = (
    <CopilotPanel
      recordings={recordings}
      routeName={routeName}
      hasRecordings={hasRecordings}
      onNavigate={onNavigate}
      regionOfInterest={regionOfInterest}
      selectedSignalIds={selectedSignalIds}
    />
  )

  if (isNarrowWorkspace) {
    return (
      <Dialog.Root modal onOpenChange={(nextIsOpen) => !nextIsOpen && onClose()} open={isOpen}>
        <Dialog.Portal>
          <Dialog.Overlay className="copilot-sidebar__overlay" />
          <Dialog.Content className="copilot-sidebar__sheet">
            <header className="copilot-sidebar__sheet-header">
              <div>
                <Dialog.Title className="copilot-sidebar__sheet-title">Sona</Dialog.Title>
                <Dialog.Description className="copilot-sidebar__sheet-description">
                  Ask Sona about the investigation or general technical topics.
                </Dialog.Description>
              </div>
              <Dialog.Close asChild>
                <Button aria-label="Close Sona" size="icon-sm" type="button" variant="ghost">
                  <X aria-hidden="true" />
                </Button>
              </Dialog.Close>
            </header>
            {panel}
          </Dialog.Content>
        </Dialog.Portal>
      </Dialog.Root>
    )
  }

  return (
    <aside
      className={`copilot-sidebar${isOpen ? '' : ' copilot-sidebar--closed'}`}
      style={isOpen ? { width } : undefined}
      aria-label="Sona panel"
    >
      <div
        className="copilot-sidebar__resize-handle"
        role="separator"
        aria-orientation="vertical"
        aria-label="Resize Sona panel"
        onMouseDown={onMouseDown}
      />
      {panel}
    </aside>
  )
}

export { CopilotSidebar }
