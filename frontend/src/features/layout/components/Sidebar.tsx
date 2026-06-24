import { FileAudio } from 'lucide-react'
import './Sidebar.scss'

interface ISidebarProps {
  activeItem?: string
}

const Sidebar = ({ activeItem = 'files' }: ISidebarProps) => {
  return (
    <aside className="sidebar">
      <div className="sidebar__header">
        <h1 className="sidebar__logo">SoundLens</h1>
      </div>

      <nav className="sidebar__nav">
        <button className={`sidebar__nav-item ${activeItem === 'files' ? 'sidebar__nav-item--active' : ''}`}>
          <FileAudio size={18} />
          <span>Files</span>
        </button>
      </nav>
    </aside>
  )
}

export { Sidebar }
