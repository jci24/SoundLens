import { useState } from 'react'
import { Navigate, Route, Routes } from 'react-router'
import { Toaster } from '@/components/ui/sonner'
import { AppShell } from './features/layout/components/AppShell'
import { useCurrentImportSession } from './features/import/hooks/useCurrentImportSession'
import { EvidencePage } from './features/workflow/components/EvidencePage'
import { AnalysisReviewPage } from './features/workflow/components/AnalysisReviewPage'
import { HomePage } from './features/workflow/components/HomePage'
import { ImportPage } from './features/workflow/components/ImportPage'
import { InvestigationSetupPage } from './features/workflow/components/InvestigationSetupPage'
import { RouteState } from './features/workflow/components/RouteState'
import { CopilotConversationProvider } from './features/analysis/copilot/context/CopilotConversationContext'
import { useAnalysisWorkspaceStore } from './features/analysis/stores/useAnalysisWorkspaceStore'
import type { IImportedFileResult } from './common/contracts/import'
import './App.scss'

const App = () => {
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false)
  const [isCopilotOpen, setIsCopilotOpen] = useState(false)
  const [conversationRevision, setConversationRevision] = useState(0)
  const session = useCurrentImportSession()
  const resetImportedSessionState = useAnalysisWorkspaceStore((state) => state.resetImportedSessionState)
  const hasRecordings = session.status === 'ready' && session.files.length > 0

  const evidenceRoute = session.status === 'loading'
    ? <RouteState title="Restoring temporary workspace" />
    : session.status === 'error'
      ? <RouteState error={session.error} onRetry={session.retry} title="Workspace restoration failed" />
      : hasRecordings
        ? <EvidencePage
            importedRecordingCount={session.files.length}
            isCopilotOpen={isCopilotOpen}
            onCopilotToggle={() => setIsCopilotOpen((current) => !current)}
          />
        : <Navigate replace to="/import" />
  const importRoute = session.status === 'loading'
    ? <RouteState title="Restoring temporary workspace" />
    : session.status === 'error'
      ? <RouteState error={session.error} onRetry={session.retry} title="Workspace restoration failed" />
      : (
          <ImportPage
            hasRecordings={hasRecordings}
            onImportedFiles={(files: IImportedFileResult[]) => {
              resetImportedSessionState()
              session.acceptImportedFiles(files)
              setConversationRevision((current) => current + 1)
              setIsCopilotOpen(false)
            }}
          />
        )
  const setupRoute = session.status === 'loading'
    ? <RouteState title="Restoring temporary workspace" />
    : session.status === 'error'
      ? <RouteState error={session.error} onRetry={session.retry} title="Workspace restoration failed" />
      : hasRecordings
        ? <InvestigationSetupPage />
        : <Navigate replace to="/import" />
  const analysisRoute = session.status === 'loading'
    ? <RouteState title="Restoring temporary workspace" />
    : session.status === 'error'
      ? <RouteState error={session.error} onRetry={session.retry} title="Workspace restoration failed" />
      : hasRecordings
        ? <AnalysisReviewPage />
        : <Navigate replace to="/import" />

  return (
    <div className="app">
      <CopilotConversationProvider key={conversationRevision}>
        <Routes>
          <Route
            element={
              <AppShell
                hasRecordings={hasRecordings}
                isSidebarCollapsed={isSidebarCollapsed}
                isCopilotOpen={isCopilotOpen}
                onCopilotToggle={() => setIsCopilotOpen((current) => !current)}
                onToggleSidebar={() => setIsSidebarCollapsed((current) => !current)}
              />
            }
          >
            <Route
              index
              element={
                <HomePage
                  error={session.error}
                  files={session.files}
                  onRetry={session.retry}
                  status={session.status}
                />
              }
            />
            <Route path="import" element={importRoute} />
            <Route path="setup" element={setupRoute} />
            <Route path="analysis" element={analysisRoute} />
            <Route path="evidence" element={evidenceRoute} />
            <Route path="*" element={<Navigate replace to="/" />} />
          </Route>
        </Routes>
      </CopilotConversationProvider>
      <Toaster closeButton position="top-right" />
    </div>
  )
}

export { App }
