import { create } from 'zustand'
import {
  areSignalIdsEqual,
  getNextExpandedRecordings,
  getNextRequestedSignalIds,
} from '../utils/analysisWorkspaceState'
import type { TAnalysisLayoutMode, TAnalysisSurface, TSignalChartMode } from '../types'

interface IAnalysisWorkspaceStore {
  selectedSignalIds: string[]
  expandedRecordings: string[]
  activeSurface: TAnalysisSurface
  layoutMode: TAnalysisLayoutMode
  signalChartMode: TSignalChartMode
  selectSignal: (signalId: string) => void
  toggleRecording: (recordingId: string) => void
  setActiveSurface: (surface: TAnalysisSurface) => void
  setLayoutMode: (mode: TAnalysisLayoutMode) => void
  setSignalChartMode: (mode: TSignalChartMode) => void
  syncSignalIds: (responseSignalIds: string[]) => void
}

const useAnalysisWorkspaceStore = create<IAnalysisWorkspaceStore>((set) => ({
  selectedSignalIds: [],
  expandedRecordings: [],
  activeSurface: 'waveform',
  layoutMode: 'focused',
  signalChartMode: 'overlay',

  selectSignal: (signalId) =>
    set((state) => ({
      selectedSignalIds: getNextRequestedSignalIds(state.selectedSignalIds, signalId),
    })),

  toggleRecording: (recordingId) =>
    set((state) => ({
      expandedRecordings: getNextExpandedRecordings(state.expandedRecordings, recordingId),
    })),

  setActiveSurface: (surface) => set({ activeSurface: surface }),

  setLayoutMode: (mode) =>
    set((state) => ({
      layoutMode: mode,
      signalChartMode: mode === 'compare' && state.signalChartMode !== 'overlay' ? 'overlay' : state.signalChartMode,
    })),

  setSignalChartMode: (mode) => set({ signalChartMode: mode }),

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
