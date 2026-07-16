import type { PropsWithChildren, RefObject } from 'react'
import type {
  IAnalysisRegionOfInterest,
  ITimeWaveformRecording,
  TAnalysisLayoutMode,
  TComparisonGroupAssignment,
} from '../../types'
import { useRecordingPlayback } from '../hooks/useRecordingPlayback'
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
  const contextValue = {
    ...playback,
    recordings,
    recordingGroupAssignments,
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
