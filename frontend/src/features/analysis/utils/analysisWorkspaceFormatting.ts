const formatFrequencyRange = (startHz: number, endHz: number) =>
  `${formatCompactFrequency(startHz)} - ${formatCompactFrequency(endHz)}`

const formatCompactFrequency = (frequencyHz: number) =>
  frequencyHz >= 1000
    ? `${(frequencyHz / 1000).toFixed(frequencyHz >= 10000 ? 0 : 1)}k Hz`
    : `${Math.round(frequencyHz)} Hz`

const formatAmplitude = (amplitude: number) => `${amplitude.toFixed(3)} FS`

const formatCrestFactor = (crestFactor: number) =>
  crestFactor === 0 ? '0.00' : crestFactor.toFixed(2)

const formatCompactDuration = (durationSeconds: number) =>
  durationSeconds >= 10
    ? `${durationSeconds.toFixed(1)} s`
    : `${durationSeconds.toFixed(2)} s`

const formatCompactSampleRate = (sampleRate: number) =>
  sampleRate >= 1000
    ? `${(sampleRate / 1000).toFixed(sampleRate % 1000 === 0 ? 0 : 1)}k Hz`
    : `${sampleRate} Hz`

const formatClippingState = (clippingSampleCount: number) =>
  clippingSampleCount === 0 ? 'Clean' : `${clippingSampleCount} samp`

export {
  formatAmplitude,
  formatClippingState,
  formatCompactDuration,
  formatCompactFrequency,
  formatCompactSampleRate,
  formatCrestFactor,
  formatFrequencyRange,
}
