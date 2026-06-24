import { Sidebar } from './features/layout/components/Sidebar'
import { MainContent } from './features/layout/components/MainContent'
import { DropZone } from './features/layout/components/DropZone'
import './App.scss'

const App = () => {
  return (
    <div className="app">
      <Sidebar activeItem="files" />
      <MainContent>
        <DropZone />
      </MainContent>
    </div>
  )
}

export { App }
