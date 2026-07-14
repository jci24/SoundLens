export const downloadTextFile = (fileName: string, content: string, mimeType = 'text/markdown;charset=utf-8'): void => {
  const blob = new Blob([content], { type: mimeType })
  downloadBlobFile(fileName, blob)
}

export const downloadBlobFile = (fileName: string, blob: Blob): void => {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')

  anchor.href = url
  anchor.download = fileName
  anchor.click()

  URL.revokeObjectURL(url)
}
