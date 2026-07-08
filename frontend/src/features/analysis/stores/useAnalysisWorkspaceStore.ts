import { create } from 'zustand'
import {
  areSignalIdsEqual,
  getNextExpandedRecordings,
  getNextRequestedSignalIds,
} from '../utils/analysisWorkspaceState'
import type { IAnalysisRegionOfInterest, ITimeWaveformRecording, TAnalysisLayoutMode, TAnalysisSurface, TSignalChartMode } from '../types'

interface IAnalysisWorkspaceStore {
  selectedSignalIds: string[]
  expandedRecordings: string[]
  activeSurface: TAnalysisSurface
  layoutMode: TAnalysisLayoutMode
  signalChartMode: TSignalChartMode
  regionOfInterest: IAnalysisRegionOfInterest | null
  recordings: ITimeWaveformRecording[]
  selectSignal: (signalId: string) => void
  toggleRecording: (recordingId: string) => void
  setActiveSurface: (surface: TAnalysisSurface) => void
  setLayoutMode: (mode: TAnalysisLayoutMode) => void
  setSignalChartMode: (mode: TSignalChartMode) => void
  setRegionOfInterest: (regionOfInterest: IAnalysisRegionOfInterest | null) => void
  setRecordings: (recordings: ITimeWaveformRecording[]) => void
  syncSignalIds: (responseSignalIds: string[]) => void
}

const useAnalysisWorkspaceStore = create<IAnalysisWorkspaceStore>((set) => ({
  selectedSignalIds: [],
  expandedRecordings: [],
  activeSurface: 'waveform',
  layoutMode: 'focused',
  signalChartMode: 'overlay',
  regionOfInterest: null,
  recordings: [],

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

  setRegionOfInterest: (regionOfInterest) => set({ regionOfInterest }),

  setRecordings: (recordings) => set({ recordings }),

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
