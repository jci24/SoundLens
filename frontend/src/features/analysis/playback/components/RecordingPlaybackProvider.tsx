import type { PropsWithChildren, RefObject } from 'react'
import type {
  IAnalysisRegionOfInterest,
  ITimeWaveformRecording,
  TComparisonGroupAssignment,
} from '../../types'
import { useRecordingPlayback } from '../hooks/useRecordingPlayback'
import { RecordingPlaybackContext } from '../contexts/recordingPlaybackContext'
import './RecordingPlaybackProvider.scss'

interface IRecordingPlaybackProviderProps extends PropsWithChildren {
  recordings: ITimeWaveformRecording[]
  recordingGroupAssignments: Record<string, TComparisonGroupAssignment>
  regionOfInterest: IAnalysisRegionOfInterest | null
  workspaceRef: RefObject<HTMLElement | null>
}

const RecordingPlaybackProvider = ({
  children,
  recordings,
  recordingGroupAssignments,
  regionOfInterest,
  workspaceRef,
}: IRecordingPlaybackProviderProps) => {
  const playback = useRecordingPlayback(recordings, regionOfInterest, workspaceRef)
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
