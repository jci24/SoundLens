import { Sidebar } from './features/layout/components/Sidebar'
import { MainContent } from './features/layout/components/MainContent'
import { Upload } from './features/upload/components/Upload'
import './App.scss'

const App = () => {
  return (
    <div className="app">
      <Sidebar activeItem="files" />
      <MainContent>
        <Upload />
      </MainContent>
    </div>
  )
}

export { App }
