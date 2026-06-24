export const isInWebView = (): boolean => {
  return typeof window !== 'undefined' && !!(window as unknown as { __SOUNDLENS_WEBVIEW__?: unknown }).__SOUNDLENS_WEBVIEW__
}
