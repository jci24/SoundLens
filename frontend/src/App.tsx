import { useEffect, useRef, useState } from 'react'
import { Toaster } from '@/components/ui/sonner'
import { Sidebar } from './features/layout/components/Sidebar'
import { MainContent } from './features/layout/components/MainContent'
import { ImportWorkspace } from './features/import/components/ImportWorkspace'
import { TimeWaveformWorkspace } from './features/analysis/workspace/components/TimeWaveformWorkspace'
import { CopilotSidebar } from './features/analysis/copilot/components/CopilotSidebar'
import { useAnalysisWorkspaceStore } from './features/analysis/stores/useAnalysisWorkspaceStore'
import type { IImportedFileSummary } from './common/contracts/import'
import './App.scss'

const App = () => {
  const [importedFiles, setImportedFiles] = useState<IImportedFileSummary[]>([])
  const [isEnteringAnalysis, setIsEnteringAnalysis] = useState(false)
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false)
  const [isCopilotOpen, setIsCopilotOpen] = useState(false)
  const enterAnimationTimeoutRef = useRef<number | null>(null)
  const selectedSignalIds = useAnalysisWorkspaceStore((state) => state.selectedSignalIds)
  const regionOfInterest = useAnalysisWorkspaceStore((state) => state.regionOfInterest)

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
            <TimeWaveformWorkspace
              importedFiles={importedFiles}
              isCopilotOpen={isCopilotOpen}
              onCopilotToggle={() => setIsCopilotOpen((prev) => !prev)}
            />
          ) : (
            <ImportWorkspace onImportedFiles={handleImportedFiles} />
          )}
        </div>
      </MainContent>
      {importedFiles.length > 0 && (
        <CopilotSidebar
          isOpen={isCopilotOpen}
          regionOfInterest={regionOfInterest}
          selectedSignalIds={selectedSignalIds}
        />
      )}
      <Toaster position="top-right" closeButton />
    </div>
  )
}

export { App }
