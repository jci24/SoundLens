import { beforeEach, describe, expect, it } from 'vitest'
import { useAnalysisWorkspaceStore } from './useAnalysisWorkspaceStore'

describe('useAnalysisWorkspaceStore session boundaries', () => {
  beforeEach(() => useAnalysisWorkspaceStore.getState().resetImportedSessionState())

  it('removes selectors and evidence owned by a replaced import session', () => {
    useAnalysisWorkspaceStore.setState({
      selectedSignalIds: ['old-signal'],
      expandedRecordings: ['old-recording'],
      recordingGroupAssignments: { 'old-recording': 'A' },
      layoutMode: 'compare',
      regionOfInterest: { startTimeSeconds: 1, endTimeSeconds: 2, durationSeconds: 1 },
      comparisonCopilotContext: {
        recordingIdA: 'old-a',
        recordingIdB: 'old-b',
        metricKey: 'rmsAmplitudeDelta',
        signalIdA: 'old-a-signal',
        signalIdB: 'old-b-signal',
      },
    })

    useAnalysisWorkspaceStore.getState().resetImportedSessionState()

    const state = useAnalysisWorkspaceStore.getState()
    expect(state.selectedSignalIds).toEqual([])
    expect(state.expandedRecordings).toEqual([])
    expect(state.recordingGroupAssignments).toEqual({})
    expect(state.layoutMode).toBe('focused')
    expect(state.regionOfInterest).toBeNull()
    expect(state.comparisonCopilotContext).toBeNull()
  })
})
