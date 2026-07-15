import { API_BASE_URL } from '../../../../common/api/config'

const getPlaybackRecordingUrl = (recordingId: string) =>
  `${API_BASE_URL}/api/playback/recordings/${encodeURIComponent(recordingId)}`

export { getPlaybackRecordingUrl }
