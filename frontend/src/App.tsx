import { useState } from 'react'
import { Navigate, Route, Routes } from 'react-router'
import { Toaster } from '@/components/ui/sonner'
import { AppShell } from './features/layout/components/AppShell'
import { useCurrentImportSession } from './features/import/hooks/useCurrentImportSession'
import { EvidencePage } from './features/workflow/components/EvidencePage'
import { HomePage } from './features/workflow/components/HomePage'
import { ImportPage } from './features/workflow/components/ImportPage'
import { RouteState } from './features/workflow/components/RouteState'
import './App.scss'

const App = () => {
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false)
  const session = useCurrentImportSession()
  const hasRecordings = session.status === 'ready' && session.files.length > 0

  const evidenceRoute = session.status === 'loading'
    ? <RouteState title="Restoring temporary workspace" />
    : session.status === 'error'
      ? <RouteState error={session.error} onRetry={session.retry} title="Workspace restoration failed" />
      : hasRecordings
        ? <EvidencePage importedRecordingCount={session.files.length} />
        : <Navigate replace to="/import" />
  const importRoute = session.status === 'loading'
    ? <RouteState title="Restoring temporary workspace" />
    : session.status === 'error'
      ? <RouteState error={session.error} onRetry={session.retry} title="Workspace restoration failed" />
      : (
          <ImportPage
            hasRecordings={hasRecordings}
            onImportedFiles={session.acceptImportedFiles}
          />
        )

  return (
    <div className="app">
      <Routes>
        <Route
          element={
            <AppShell
              hasRecordings={hasRecordings}
              isSidebarCollapsed={isSidebarCollapsed}
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
          <Route path="evidence" element={evidenceRoute} />
          <Route path="*" element={<Navigate replace to="/" />} />
        </Route>
      </Routes>
      <Toaster closeButton position="top-right" />
    </div>
  )
}

export { App }
