import type { PropsWithChildren, RefObject } from 'react'
import type {
  IAnalysisRegionOfInterest,
  ITimeWaveformRecording,
  TAnalysisLayoutMode,
  TComparisonGroupAssignment,
} from '../../types'
import { useRecordingPlayback } from '../hooks/useRecordingPlayback'
import { useChannelAuditionRouting } from '../hooks/useChannelAuditionRouting'
import { RecordingPlaybackContext } from '../contexts/recordingPlaybackContext'
import './RecordingPlaybackProvider.scss'

interface IRecordingPlaybackProviderProps extends PropsWithChildren {
  layoutMode?: TAnalysisLayoutMode
  recordings: ITimeWaveformRecording[]
  recordingGroupAssignments: Record<string, TComparisonGroupAssignment>
  regionOfInterest: IAnalysisRegionOfInterest | null
  workspaceRef: RefObject<HTMLElement | null>
}

const RecordingPlaybackProvider = ({
  children,
  layoutMode = 'focused',
  recordings,
  recordingGroupAssignments,
  regionOfInterest,
  workspaceRef,
}: IRecordingPlaybackProviderProps) => {
  const playback = useRecordingPlayback(
    recordings,
    regionOfInterest,
    workspaceRef,
    recordingGroupAssignments,
    layoutMode === 'compare'
  )
  const channelAudition = useChannelAuditionRouting(
    playback.audioRef,
    playback.selectedRecording
  )
  const contextValue = {
    ...playback,
    ...channelAudition,
    clearRecording: () => {
      channelAudition.clearChannelAuditionRoute()
      playback.clearRecording()
    },
    recordings,
    recordingGroupAssignments,
    selectAuditionSide: (side: 'A' | 'B') => {
      const recording = playback.auditionPair?.[side]
      if (recording) {
        channelAudition.prepareChannelRouteForRecording(recording, 'audition')
      }
      playback.selectAuditionSide(side)
    },
    selectRecording: (recordingId: string) => {
      const recording = recordings.find((candidate) => candidate.recordingId === recordingId)
      if (recording) {
        channelAudition.prepareChannelRouteForRecording(recording, 'general')
      }
      playback.selectRecording(recordingId)
    },
  }

  return (
    <RecordingPlaybackContext.Provider value={contextValue}>
      <div className="recording-playback-scope">
        {children}
      </div>
    </RecordingPlaybackContext.Provider>
  )
}

export { RecordingPlaybackProvider }
