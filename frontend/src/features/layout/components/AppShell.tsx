import { Outlet, useLocation, useNavigate } from 'react-router'
import { Sidebar } from './Sidebar'
import { MainContent } from './MainContent'
import { BreadcrumbBar } from './BreadcrumbBar'
import { CopilotSidebar } from '../../analysis/copilot/components/CopilotSidebar'
import { useAnalysisWorkspaceStore } from '../../analysis/stores/useAnalysisWorkspaceStore'
import type { TCopilotRouteName } from '../../analysis/copilot/types/copilot.types'
import './AppShell.scss'

interface IAppShellProps {
  hasRecordings: boolean
  isSidebarCollapsed: boolean
  onToggleSidebar: () => void
  isCopilotOpen: boolean
  onCopilotToggle: () => void
}

const routeNames: Record<string, TCopilotRouteName> = {
  '/': 'home',
  '/import': 'import',
  '/setup': 'configure',
  '/analysis': 'analysis',
  '/evidence': 'evidence',
}

const AppShell = ({ hasRecordings, isSidebarCollapsed, onToggleSidebar, isCopilotOpen, onCopilotToggle }: IAppShellProps) => {
  const { pathname } = useLocation()
  const navigate = useNavigate()
  const selectedSignalIds = useAnalysisWorkspaceStore((state) => state.selectedSignalIds)
  const regionOfInterest = useAnalysisWorkspaceStore((state) => state.regionOfInterest)
  const recordings = useAnalysisWorkspaceStore((state) => state.recordings)

  return <div className="app-shell">
    <Sidebar
      hasRecordings={hasRecordings}
      isCollapsed={isSidebarCollapsed}
      onToggleCollapse={onToggleSidebar}
    />
    <MainContent>
      <BreadcrumbBar isCopilotOpen={isCopilotOpen} onCopilotToggle={onCopilotToggle} />
      <div className="app-shell__route-content">
        <Outlet />
      </div>
    </MainContent>
    <CopilotSidebar
      hasRecordings={hasRecordings}
      isOpen={isCopilotOpen}
      onClose={onCopilotToggle}
      onNavigate={navigate}
      recordings={recordings}
      regionOfInterest={regionOfInterest}
      routeName={routeNames[pathname] ?? 'home'}
      selectedSignalIds={selectedSignalIds}
    />
  </div>
}

export { AppShell }
