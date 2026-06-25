const formatFrequencyRange = (startHz: number, endHz: number) =>
  `${formatCompactFrequency(startHz)} - ${formatCompactFrequency(endHz)}`

const formatCompactFrequency = (frequencyHz: number) =>
  frequencyHz >= 1000
    ? `${(frequencyHz / 1000).toFixed(frequencyHz >= 10000 ? 0 : 1)}k Hz`
    : `${Math.round(frequencyHz)} Hz`

export { formatCompactFrequency, formatFrequencyRange }
