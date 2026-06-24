import { Toaster } from '@/components/ui/sonner'
import { Sidebar } from './features/layout/components/Sidebar'
import { MainContent } from './features/layout/components/MainContent'
import { ImportWorkspace } from './features/import/components/ImportWorkspace'
import './App.scss'

const App = () => {
  return (
    <div className="app">
      <Sidebar activeItem="files" />
      <MainContent>
        <ImportWorkspace />
      </MainContent>
      <Toaster position="top-right" closeButton />
    </div>
  )
}

export { App }
