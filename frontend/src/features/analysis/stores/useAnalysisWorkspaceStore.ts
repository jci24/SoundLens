import { create } from 'zustand'
import {
  areSignalIdsEqual,
  defaultEnabledAnalysisSurfaces,
  getNextExpandedRecordings,
  getNextEnabledAnalysisSurfaces,
  getNextRecordingGroupAssignments,
  getNextSingleRecordingGroupAssignment,
  getNextRequestedSignalIds,
  getSwappedRecordingGroupAssignments,
} from '../utils/analysisWorkspaceState'
import type {
  IAnalysisRegionOfInterest,
  IComparisonCopilotSelection,
  ITimeWaveformRecording,
  TAnalysisLayoutMode,
  TAnalysisSurface,
  TComparisonGroupAssignment,
  TSignalChartMode,
} from '../types'

interface IAnalysisWorkspaceStore {
  selectedSignalIds: string[]
  expandedRecordings: string[]
  recordingGroupAssignments: Record<string, TComparisonGroupAssignment>
  activeSurface: TAnalysisSurface
  enabledAnalysisSurfaces: TAnalysisSurface[]
  layoutMode: TAnalysisLayoutMode
  signalChartMode: TSignalChartMode
  regionOfInterest: IAnalysisRegionOfInterest | null
  recordings: ITimeWaveformRecording[]
  comparisonCopilotContext: IComparisonCopilotSelection | null
  selectSignal: (signalId: string) => void
  toggleRecording: (recordingId: string) => void
  setRecordingGroupAssignment: (recordingId: string, assignment: TComparisonGroupAssignment) => void
  swapComparisonTargets: () => void
  setActiveSurface: (surface: TAnalysisSurface) => void
  toggleAnalysisSurface: (surface: TAnalysisSurface) => void
  setLayoutMode: (mode: TAnalysisLayoutMode) => void
  setSignalChartMode: (mode: TSignalChartMode) => void
  setRegionOfInterest: (regionOfInterest: IAnalysisRegionOfInterest | null) => void
  setRecordings: (recordings: ITimeWaveformRecording[]) => void
  setComparisonCopilotContext: (comparisonCopilotContext: IComparisonCopilotSelection | null) => void
  syncSignalIds: (responseSignalIds: string[]) => void
  resetImportedSessionState: () => void
}

const useAnalysisWorkspaceStore = create<IAnalysisWorkspaceStore>((set) => ({
  selectedSignalIds: [],
  expandedRecordings: [],
  recordingGroupAssignments: {},
  activeSurface: 'waveform',
  enabledAnalysisSurfaces: [...defaultEnabledAnalysisSurfaces],
  layoutMode: 'focused',
  signalChartMode: 'overlay',
  regionOfInterest: null,
  recordings: [],
  comparisonCopilotContext: null,

  selectSignal: (signalId) =>
    set((state) => ({
      selectedSignalIds: getNextRequestedSignalIds(state.selectedSignalIds, signalId),
    })),

  toggleRecording: (recordingId) =>
    set((state) => ({
      expandedRecordings: getNextExpandedRecordings(state.expandedRecordings, recordingId),
    })),

  setRecordingGroupAssignment: (recordingId, assignment) =>
    set((state) => ({
      recordingGroupAssignments: getNextSingleRecordingGroupAssignment(
        state.recordingGroupAssignments,
        recordingId,
        assignment
      ),
    })),

  swapComparisonTargets: () =>
    set((state) => ({
      recordingGroupAssignments: getSwappedRecordingGroupAssignments(state.recordingGroupAssignments),
    })),

  setActiveSurface: (surface) => set({ activeSurface: surface }),

  toggleAnalysisSurface: (surface) =>
    set((state) => {
      const enabledAnalysisSurfaces = getNextEnabledAnalysisSurfaces(
        state.enabledAnalysisSurfaces,
        surface
      )

      return {
        activeSurface: enabledAnalysisSurfaces.includes(state.activeSurface)
          ? state.activeSurface
          : (enabledAnalysisSurfaces[0] ?? state.activeSurface),
        enabledAnalysisSurfaces,
      }
    }),

  setLayoutMode: (mode) =>
    set((state) => ({
      layoutMode: mode,
      signalChartMode: mode === 'compare' && state.signalChartMode !== 'overlay' ? 'overlay' : state.signalChartMode,
    })),

  setSignalChartMode: (mode) => set({ signalChartMode: mode }),

  setRegionOfInterest: (regionOfInterest) => set({ regionOfInterest }),

  setComparisonCopilotContext: (comparisonCopilotContext) => set({ comparisonCopilotContext }),

  setRecordings: (recordings) =>
    set((state) => ({
      recordings,
      recordingGroupAssignments: getNextRecordingGroupAssignments(
        state.recordingGroupAssignments,
        recordings.map((recording) => recording.recordingId)
      ),
    })),

  syncSignalIds: (responseSignalIds) =>
    set((state) => {
      const nextSignalIds = state.selectedSignalIds.length === 0
        ? responseSignalIds
        : state.selectedSignalIds.filter((signalId) => responseSignalIds.includes(signalId))

      return areSignalIdsEqual(state.selectedSignalIds, nextSignalIds)
        ? {}
        : { selectedSignalIds: nextSignalIds }
    }),

  resetImportedSessionState: () => set({
    selectedSignalIds: [],
    expandedRecordings: [],
    recordingGroupAssignments: {},
    layoutMode: 'focused',
    signalChartMode: 'overlay',
    regionOfInterest: null,
    recordings: [],
    comparisonCopilotContext: null,
  }),
}))

export { useAnalysisWorkspaceStore }
export type { IAnalysisWorkspaceStore }
