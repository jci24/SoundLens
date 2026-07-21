import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { App } from './App'

vi.mock('./features/workflow/components/EvidencePage', () => ({
  EvidencePage: ({ importedRecordingCount }: { importedRecordingCount: number }) => (
    <div>Evidence workspace with {importedRecordingCount} recordings</div>
  ),
}))

vi.mock('./features/workflow/components/InvestigationSetupPage', () => ({
  InvestigationSetupPage: () => <div>Configure comparison workspace</div>,
}))

vi.mock('./features/workflow/components/AnalysisReviewPage', () => ({
  AnalysisReviewPage: () => <div>Analysis review workspace</div>,
}))

vi.mock('./features/analysis/copilot/components/CopilotSidebar', () => ({
  CopilotSidebar: ({ isOpen, routeName }: { isOpen: boolean; routeName: string }) => (
    <div>{`Shell Copilot ${isOpen ? 'open' : 'closed'} on ${routeName}`}</div>
  ),
}))

vi.mock('./features/import/components/ImportWorkspace', () => ({
  ImportWorkspace: ({ onImportedFiles }: { onImportedFiles: (files: unknown[]) => void }) => (
    <>
      <button
        type="button"
        onClick={() => onImportedFiles([
          { fileName: 'new.wav', sizeBytes: 42, contentType: 'audio/wav' },
        ])}
      >
        Complete import
      </button>
      <button
        type="button"
        onClick={() => onImportedFiles([
          { fileName: 'baseline.wav', sizeBytes: 42, contentType: 'audio/wav' },
          { fileName: 'candidate.wav', sizeBytes: 43, contentType: 'audio/wav' },
        ])}
      >
        Complete multi-file import
      </button>
    </>
  ),
}))

const populatedSession = {
  files: [
    { fileName: 'baseline.wav', sizeBytes: 123, contentType: 'audio/wav' },
    { fileName: 'candidate.wav', sizeBytes: 456, contentType: 'audio/wav' },
  ],
}

const renderApp = (initialEntry = '/') => render(
  <MemoryRouter initialEntries={[initialEntry]}>
    <App />
  </MemoryRouter>
)

describe('App workflow routes', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('restores a temporary session and presents functional Home navigation', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => populatedSession }))

    renderApp()

    expect(screen.getByText('Restoring temporary workspace')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Turn repeated recordings into reviewable evidence.' })).not.toBeInTheDocument()
    expect(await screen.findByRole('heading', { name: 'Current investigation' })).toBeInTheDocument()
    expect(screen.getByText('baseline.wav')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Evidence' })).toHaveAttribute('href', '/evidence')
    expect(screen.getByRole('link', { name: 'Evidence' })).toHaveAttribute('title', 'Evidence')
    expect(screen.getByRole('link', { name: 'Analysis setup' })).toHaveAttribute('href', '/analysis')
    expect(screen.getAllByRole('link', { name: 'Configure comparison' })).toHaveLength(2)
    expect(screen.getAllByRole('link', { name: 'Configure comparison' })).toEqual(
      expect.arrayContaining([expect.objectContaining({ pathname: '/setup' })])
    )
    expect(screen.getByText(/not yet a saved project/i)).toBeInTheDocument()
  })

  it('keeps Copilot open across routes and closes it when recordings are replaced', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => populatedSession }))

    renderApp()
    await screen.findByRole('heading', { name: 'Current investigation' })
    fireEvent.click(screen.getByRole('button', { name: 'Open Copilot' }))
    expect(screen.getByText('Shell Copilot open on home')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('link', { name: 'Import recordings' }))
    expect(await screen.findByText('Shell Copilot open on import')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Complete import' }))

    expect(await screen.findByText('Shell Copilot closed on evidence')).toBeInTheDocument()
  })

  it('waits for session bootstrap before allowing direct Evidence navigation', async () => {
    let resolveRequest: ((value: unknown) => void) | null = null
    vi.stubGlobal('fetch', vi.fn().mockReturnValue(new Promise((resolve) => {
      resolveRequest = resolve
    })))

    renderApp('/evidence')

    expect(screen.getByText('Restoring temporary workspace')).toBeInTheDocument()

    await act(async () => {
      resolveRequest?.({ ok: true, json: async () => populatedSession })
    })

    expect(await screen.findByText('Evidence workspace with 2 recordings')).toBeInTheDocument()
    expect(screen.getByText('Evidence', { selector: '[aria-current="page"]' })).toBeInTheDocument()
  })

  it('redirects an empty direct Evidence route to Import and keeps Evidence unavailable', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => ({ files: [] }) }))

    renderApp('/evidence')

    expect(await screen.findByRole('heading', { name: 'Import recordings' })).toBeInTheDocument()
    expect(screen.getByLabelText('Evidence unavailable until recordings are imported')).toHaveAttribute('aria-disabled', 'true')
  })

  it('navigates to Evidence after a successful replacement import', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => populatedSession }))

    renderApp('/import')

    expect(await screen.findByRole('heading', { name: 'Replace recordings' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Complete import' }))

    expect(await screen.findByText('Evidence workspace with 1 recordings')).toBeInTheDocument()
  })

  it('suggests optional comparison configuration after a multi-file import', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => populatedSession }))

    renderApp('/import')
    expect(await screen.findByRole('heading', { name: 'Replace recordings' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Complete multi-file import' }))

    expect(await screen.findByText('Configure comparison workspace')).toBeInTheDocument()
    expect(screen.getByText('Configure', { selector: '[aria-current="page"]' })).toBeInTheDocument()
  })

  it('redirects an empty direct Configure route to Import', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => ({ files: [] }) }))

    renderApp('/setup')

    expect(await screen.findByRole('heading', { name: 'Import recordings' })).toBeInTheDocument()
    expect(screen.getByLabelText('Configure unavailable until recordings are imported')).toHaveAttribute('aria-disabled', 'true')
  })

  it('guards Analysis until recordings exist and restores it for a populated session', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => ({ files: [] }) }))

    const emptyRender = renderApp('/analysis')
    expect(await screen.findByRole('heading', { name: 'Import recordings' })).toBeInTheDocument()
    expect(screen.getByLabelText('Analysis unavailable until recordings are imported')).toHaveAttribute('aria-disabled', 'true')
    emptyRender.unmount()

    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => populatedSession }))
    renderApp('/analysis')

    expect(await screen.findByText('Analysis review workspace')).toBeInTheDocument()
    expect(screen.getByText('Analysis', { selector: '[aria-current="page"]' })).toBeInTheDocument()
  })

  it('shows a retryable bootstrap failure and recovers without treating the session as empty', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: false })
      .mockResolvedValueOnce({ ok: true, json: async () => populatedSession })
    vi.stubGlobal('fetch', fetchMock)

    renderApp('/evidence')

    expect(await screen.findByRole('heading', { name: 'Workspace restoration failed' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Import recordings' })).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }))

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2))
    expect(await screen.findByText('Evidence workspace with 2 recordings')).toBeInTheDocument()
  })

  it('redirects unknown routes to Home', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => ({ files: [] }) }))

    renderApp('/does-not-exist')

    expect(await screen.findByRole('heading', { name: 'Turn repeated recordings into reviewable evidence.' })).toBeInTheDocument()
    expect(screen.getByText('Home', { selector: '[aria-current="page"]' })).toBeInTheDocument()
  })
})
