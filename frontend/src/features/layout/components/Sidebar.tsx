import { FileAudio, PanelLeftClose, PanelLeftOpen } from 'lucide-react'
import './Sidebar.scss'

interface ISidebarProps {
  activeItem?: string
  isCollapsed: boolean
  onToggleCollapse: () => void
}

const Sidebar = ({ activeItem = 'files', isCollapsed, onToggleCollapse }: ISidebarProps) => {
  const ToggleIcon = isCollapsed ? PanelLeftOpen : PanelLeftClose

  return (
    <aside className={`sidebar${isCollapsed ? ' sidebar--collapsed' : ''}`}>
      <div className="sidebar__header">
        <h1 className={`sidebar__logo${isCollapsed ? ' sidebar__logo--collapsed' : ''}`}>SoundLens</h1>
        <button
          aria-label={isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          className="sidebar__collapse-button"
          type="button"
          onClick={onToggleCollapse}
        >
          <ToggleIcon size={18} />
        </button>
      </div>

      <nav className="sidebar__nav">
        <button
          aria-label="Files"
          className={`sidebar__nav-item ${activeItem === 'files' ? 'sidebar__nav-item--active' : ''}`}
          title={isCollapsed ? 'Files' : undefined}
        >
          <FileAudio size={18} />
          <span className={`sidebar__nav-label${isCollapsed ? ' sidebar__nav-label--collapsed' : ''}`}>Files</span>
        </button>
      </nav>
    </aside>
  )
}

export { Sidebar }
