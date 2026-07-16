import { createContext, useContext } from 'react'
import type { ITimeWaveformRecording, TComparisonGroupAssignment } from '../../types'
import type { useRecordingPlayback } from '../hooks/useRecordingPlayback'

type TRecordingPlaybackContext = ReturnType<typeof useRecordingPlayback> & {
  recordings: ITimeWaveformRecording[]
  recordingGroupAssignments: Record<string, TComparisonGroupAssignment>
}

const RecordingPlaybackContext = createContext<TRecordingPlaybackContext | null>(null)

const useRecordingPlaybackContext = () => {
  const context = useContext(RecordingPlaybackContext)

  if (!context) {
    throw new Error('Recording playback must be used within RecordingPlaybackProvider.')
  }

  return context
}

const useOptionalRecordingPlaybackContext = () => useContext(RecordingPlaybackContext)

export {
  RecordingPlaybackContext,
  useOptionalRecordingPlaybackContext,
  useRecordingPlaybackContext,
}
