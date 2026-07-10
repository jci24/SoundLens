import { Suspense, lazy, useEffect, useRef, useState } from 'react'
import { Toaster } from '@/components/ui/sonner'
import { Sidebar } from './features/layout/components/Sidebar'
import { MainContent } from './features/layout/components/MainContent'
import { ImportWorkspace } from './features/import/components/ImportWorkspace'
import { useAnalysisWorkspaceStore } from './features/analysis/stores/useAnalysisWorkspaceStore'
import type { IImportedFileSummary } from './common/contracts/import'
import './App.scss'

const TimeWaveformWorkspace = lazy(async () => {
  const module = await import('./features/analysis/workspace/components/TimeWaveformWorkspace')
  return { default: module.TimeWaveformWorkspace }
})

const CopilotSidebar = lazy(async () => {
  const module = await import('./features/analysis/copilot/components/CopilotSidebar')
  return { default: module.CopilotSidebar }
})

const App = () => {
  const [importedFiles, setImportedFiles] = useState<IImportedFileSummary[]>([])
  const [isEnteringAnalysis, setIsEnteringAnalysis] = useState(false)
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false)
  const [isCopilotOpen, setIsCopilotOpen] = useState(false)
  const enterAnimationTimeoutRef = useRef<number | null>(null)
  const selectedSignalIds = useAnalysisWorkspaceStore((state) => state.selectedSignalIds)
  const regionOfInterest = useAnalysisWorkspaceStore((state) => state.regionOfInterest)
  const recordings = useAnalysisWorkspaceStore((state) => state.recordings)

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
      <MainContent isCopilotOpen={isCopilotOpen}>
        <div className={`app__workspace-stage${isEnteringAnalysis ? ' app__workspace-stage--entering' : ''}`}>
          {importedFiles.length > 0 ? (
            <Suspense fallback={null}>
              <TimeWaveformWorkspace
                importedFiles={importedFiles}
                isCopilotOpen={isCopilotOpen}
                onCopilotToggle={() => setIsCopilotOpen((prev) => !prev)}
              />
            </Suspense>
          ) : (
            <ImportWorkspace onImportedFiles={handleImportedFiles} />
          )}
        </div>
      </MainContent>
      {importedFiles.length > 0 && (
        <Suspense fallback={null}>
          <CopilotSidebar
            isOpen={isCopilotOpen}
            recordings={recordings}
            regionOfInterest={regionOfInterest}
            selectedSignalIds={selectedSignalIds}
          />
        </Suspense>
      )}
      <Toaster position="top-right" closeButton />
    </div>
  )
}

export { App }
