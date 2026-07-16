type TChannelAuditionRoute =
  | { mode: 'original' }
  | { channelIndex: number; mode: 'isolated' }

type TPlaybackSelectionOrigin = 'audition' | 'general'

type TChannelRoutingStatus = 'idle' | 'ready' | 'error' | 'unsupported'

export type {
  TChannelAuditionRoute,
  TChannelRoutingStatus,
  TPlaybackSelectionOrigin,
}
