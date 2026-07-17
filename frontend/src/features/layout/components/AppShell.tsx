import { Outlet } from 'react-router'
import { Sidebar } from './Sidebar'
import { MainContent } from './MainContent'
import { BreadcrumbBar } from './BreadcrumbBar'
import './AppShell.scss'

interface IAppShellProps {
  hasRecordings: boolean
  isSidebarCollapsed: boolean
  onToggleSidebar: () => void
}

const AppShell = ({ hasRecordings, isSidebarCollapsed, onToggleSidebar }: IAppShellProps) => (
  <div className="app-shell">
    <Sidebar
      hasRecordings={hasRecordings}
      isCollapsed={isSidebarCollapsed}
      onToggleCollapse={onToggleSidebar}
    />
    <MainContent>
      <BreadcrumbBar />
      <div className="app-shell__route-content">
        <Outlet />
      </div>
    </MainContent>
  </div>
)

export { AppShell }
