import { create } from 'zustand'
import {
  areSignalIdsEqual,
  getNextExpandedRecordings,
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
  setLayoutMode: (mode: TAnalysisLayoutMode) => void
  setSignalChartMode: (mode: TSignalChartMode) => void
  setRegionOfInterest: (regionOfInterest: IAnalysisRegionOfInterest | null) => void
  setRecordings: (recordings: ITimeWaveformRecording[]) => void
  setComparisonCopilotContext: (comparisonCopilotContext: IComparisonCopilotSelection | null) => void
  syncSignalIds: (responseSignalIds: string[]) => void
}

const useAnalysisWorkspaceStore = create<IAnalysisWorkspaceStore>((set) => ({
  selectedSignalIds: [],
  expandedRecordings: [],
  recordingGroupAssignments: {},
  activeSurface: 'waveform',
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
      if (state.selectedSignalIds.length === 0) {
        return {}
      }

      const nextSignalIds = state.selectedSignalIds.filter((signalId) =>
        responseSignalIds.includes(signalId)
      )

      return areSignalIdsEqual(state.selectedSignalIds, nextSignalIds)
        ? {}
        : { selectedSignalIds: nextSignalIds }
    }),
}))

export { useAnalysisWorkspaceStore }
export type { IAnalysisWorkspaceStore }
