export interface ISpectrumRange {
  startHz: number
  endHz: number
}

const spectrumFftSizeValues = [
  512,
  1024,
  2048,
  4096,
  8192,
  16384,
  32768,
  44100,
] as const

const defaultSpectrumFftSize = 44100
const defaultSpectrumRangeEndHz = 22050

const getWaveformRequestedBinCount = (chartWidth: number) => {
  if (chartWidth <= 0) {
    return 0
  }

  return Math.min(4000, Math.max(64, Math.ceil(chartWidth)))
}

const formatSpectrumFftSizeOption = (fftSize: number) => `FFT size: ${fftSize.toLocaleString()}`
const getOneSidedLineCount = (fftSize: number) => Math.floor(fftSize / 2) + 1

const spectrumFftSizeSelectOptions = spectrumFftSizeValues.map((value) => ({
  label: formatSpectrumFftSizeOption(value),
  value,
}))

// Selection order drives overlay order, so this intentionally compares sequences, not sets.
const areSignalIdsEqual = (left: string[], right: string[]) =>
  left.length === right.length && left.every((signalId, index) => signalId === right[index])

const getNextExpandedRecordings = (currentExpanded: string[], recordingId: string) =>
  currentExpanded.includes(recordingId)
    ? currentExpanded.filter((expandedPath) => expandedPath !== recordingId)
    : [...currentExpanded, recordingId]

const getNextRequestedSignalIds = (currentSignalIds: string[], signalId: string) => {
  if (currentSignalIds.length === 0) {
    return [signalId]
  }

  if (currentSignalIds.includes(signalId)) {
    return currentSignalIds.length === 1
      ? currentSignalIds
      : currentSignalIds.filter((currentSignalId) => currentSignalId !== signalId)
  }

  return [...currentSignalIds, signalId]
}

const clampSpectrumRange = (range: ISpectrumRange, maximumHz: number): ISpectrumRange => {
  const startHz = Math.min(Math.max(range.startHz, 0), maximumHz - 1)
  const endHz = Math.max(startHz + 1, Math.min(range.endHz, maximumHz))

  return {
    startHz,
    endHz,
  }
}

const getNextSpectrumRangeStart = (
  parsedValue: number,
  currentRange: ISpectrumRange,
  maximumHz: number
): ISpectrumRange => {
  const nextEndHz = Math.max(1, Math.min(currentRange.endHz, maximumHz))

  return {
    ...currentRange,
    startHz: Math.min(Math.max(parsedValue, 0), nextEndHz - 1),
  }
}

const getNextSpectrumRangeEnd = (
  parsedValue: number,
  currentRange: ISpectrumRange,
  maximumHz: number
): ISpectrumRange => {
  const nextStartHz = Math.min(Math.max(currentRange.startHz, 0), maximumHz - 1)

  return {
    ...currentRange,
    endHz: Math.max(nextStartHz + 1, Math.min(parsedValue, maximumHz)),
  }
}

export {
  areSignalIdsEqual,
  clampSpectrumRange,
  defaultSpectrumFftSize,
  defaultSpectrumRangeEndHz,
  formatSpectrumFftSizeOption,
  getNextExpandedRecordings,
  getNextRequestedSignalIds,
  getNextSpectrumRangeEnd,
  getNextSpectrumRangeStart,
  getOneSidedLineCount,
  getWaveformRequestedBinCount,
  spectrumFftSizeSelectOptions,
}
