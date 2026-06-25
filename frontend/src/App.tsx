import { useEffect, useRef, useState } from 'react'
import { Toaster } from '@/components/ui/sonner'
import { Sidebar } from './features/layout/components/Sidebar'
import { MainContent } from './features/layout/components/MainContent'
import { ImportWorkspace } from './features/import/components/ImportWorkspace'
import { TimeWaveformWorkspace } from './features/analysis/components/TimeWaveformWorkspace'
import type { IImportedFileSummary } from './features/import/types'
import './App.scss'

const App = () => {
  const [importedFiles, setImportedFiles] = useState<IImportedFileSummary[]>([])
  const [isEnteringAnalysis, setIsEnteringAnalysis] = useState(false)
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false)
  const enterAnimationTimeoutRef = useRef<number | null>(null)

  useEffect(() => {
    return () => {
      if (enterAnimationTimeoutRef.current !== null) {
        window.clearTimeout(enterAnimationTimeoutRef.current)
      }
    }
  }, [])

  const handleImportedFiles = (files: IImportedFileSummary[]) => {
    if (enterAnimationTimeoutRef.current !== null) {
      window.clearTimeout(enterAnimationTimeoutRef.current)
    }

    setIsEnteringAnalysis(true)
    setImportedFiles(files)

    enterAnimationTimeoutRef.current = window.setTimeout(() => {
      setIsEnteringAnalysis(false)
      enterAnimationTimeoutRef.current = null
    }, 420)
  }

  return (
    <div className="app">
      <Sidebar
        activeItem="files"
        isCollapsed={isSidebarCollapsed}
        onToggleCollapse={() => setIsSidebarCollapsed((currentValue) => !currentValue)}
      />
      <MainContent>
        <div className={`app__workspace-stage${isEnteringAnalysis ? ' app__workspace-stage--entering' : ''}`}>
          {importedFiles.length > 0 ? (
            <TimeWaveformWorkspace importedFiles={importedFiles} />
          ) : (
            <ImportWorkspace onImportedFiles={handleImportedFiles} />
          )}
        </div>
      </MainContent>
      <Toaster position="top-right" closeButton />
    </div>
  )
}

export { App }
