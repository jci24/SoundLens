import { useSyncExternalStore } from 'react'

const getMediaQueryList = (query: string) => (
  typeof window === 'undefined' || typeof window.matchMedia !== 'function'
    ? null
    : window.matchMedia(query)
)

const useMediaQuery = (query: string) => useSyncExternalStore(
  (onStoreChange) => {
    const mediaQueryList = getMediaQueryList(query)

    mediaQueryList?.addEventListener('change', onStoreChange)
    return () => mediaQueryList?.removeEventListener('change', onStoreChange)
  },
  () => getMediaQueryList(query)?.matches ?? false,
  () => false
)

export { useMediaQuery }
