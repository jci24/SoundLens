import { ChartNoAxesCombined, Home, PanelLeftClose, PanelLeftOpen, SlidersHorizontal, Upload } from 'lucide-react'
import { NavLink } from 'react-router'
import './Sidebar.scss'

interface ISidebarProps {
  hasRecordings: boolean
  isCollapsed: boolean
  onToggleCollapse: () => void
}

const Sidebar = ({ hasRecordings, isCollapsed, onToggleCollapse }: ISidebarProps) => {
  const ToggleIcon = isCollapsed ? PanelLeftOpen : PanelLeftClose
  const labelClassName = `sidebar__nav-label${isCollapsed ? ' sidebar__nav-label--collapsed' : ''}`

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

      <nav className="sidebar__nav" aria-label="Primary navigation">
        <NavLink
          aria-label="Home"
          className={({ isActive }) => `sidebar__nav-item${isActive ? ' sidebar__nav-item--active' : ''}`}
          end
          title={isCollapsed ? 'Home' : undefined}
          to="/"
        >
          <Home size={18} />
          <span className={labelClassName}>Home</span>
        </NavLink>
        <NavLink
          aria-label="Import recordings"
          className={({ isActive }) => `sidebar__nav-item${isActive ? ' sidebar__nav-item--active' : ''}`}
          title={isCollapsed ? 'Import' : undefined}
          to="/import"
        >
          <Upload size={18} />
          <span className={labelClassName}>Import</span>
        </NavLink>
        {hasRecordings ? (
          <>
            <NavLink
              aria-label="Configure comparison"
              className={({ isActive }) => `sidebar__nav-item${isActive ? ' sidebar__nav-item--active' : ''}`}
              title={isCollapsed ? 'Configure' : undefined}
              to="/setup"
            >
              <SlidersHorizontal size={18} />
              <span className={labelClassName}>Configure</span>
            </NavLink>
            <NavLink
              aria-label="Evidence"
              className={({ isActive }) => `sidebar__nav-item${isActive ? ' sidebar__nav-item--active' : ''}`}
              title={isCollapsed ? 'Evidence' : undefined}
              to="/evidence"
            >
              <ChartNoAxesCombined size={18} />
              <span className={labelClassName}>Evidence</span>
            </NavLink>
          </>
        ) : (
          <>
            <span
              aria-disabled="true"
              aria-label="Configure unavailable until recordings are imported"
              className="sidebar__nav-item sidebar__nav-item--disabled"
              title={isCollapsed ? 'Import recordings to configure a comparison' : undefined}
            >
              <SlidersHorizontal size={18} />
              <span className={labelClassName}>Configure</span>
            </span>
            <span
              aria-disabled="true"
              aria-label="Evidence unavailable until recordings are imported"
              className="sidebar__nav-item sidebar__nav-item--disabled"
              title={isCollapsed ? 'Import recordings to view evidence' : undefined}
            >
              <ChartNoAxesCombined size={18} />
              <span className={labelClassName}>Evidence</span>
            </span>
          </>
        )}
      </nav>
    </aside>
  )
}

export { Sidebar }
