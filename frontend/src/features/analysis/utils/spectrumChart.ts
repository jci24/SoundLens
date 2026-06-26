import type {
  IFrequencySpectrumPoint,
  IFrequencySpectrumAxis,
  IFrequencySpectrumSignal,
} from '../types'

const getSpectrumChartDimensions = (width: number) => {
  if (width <= 720) {
    return {
      chartHeight: 264,
      chartPadding: {
        top: 18,
        right: 14,
        bottom: 34,
        left: 54,
      },
    }
  }

  if (width <= 1080) {
    return {
      chartHeight: 312,
      chartPadding: {
        top: 20,
        right: 16,
        bottom: 36,
        left: 64,
      },
    }
  }

  return {
    chartHeight: 360,
    chartPadding: {
      top: 22,
      right: 18,
      bottom: 38,
      left: 72,
    },
  }
}

const getSpectrumChartModel = (
  xAxis: IFrequencySpectrumAxis,
  yAxis: IFrequencySpectrumAxis,
  width: number
) => {
  const { chartHeight, chartPadding } = getSpectrumChartDimensions(width)
  const plotWidth = Math.max(1, width - chartPadding.left - chartPadding.right)
  const plotHeight = chartHeight - chartPadding.top - chartPadding.bottom
  const minFrequency = xAxis.minimum
  const maxFrequency = Math.max(xAxis.maximum, minFrequency + 1)
  const frequencySpan = Math.max(maxFrequency - minFrequency, 1)
  const minPlotX = chartPadding.left
  const maxPlotX = chartPadding.left + plotWidth
  const xForFrequency = (frequencyHz: number) =>
    chartPadding.left + ((frequencyHz - minFrequency) / frequencySpan) * plotWidth
  const frequencyForX = (x: number) =>
    minFrequency +
    (((Math.min(Math.max(x, minPlotX), maxPlotX) - chartPadding.left) / plotWidth) * frequencySpan)
  const yForValue = (value: number) => {
    const clampedValue = Math.min(Math.max(value, yAxis.minimum), yAxis.maximum)
    return (
      chartPadding.top +
      ((yAxis.maximum - clampedValue) / (yAxis.maximum - yAxis.minimum || 1)) * plotHeight
    )
  }

  return {
    chartHeight,
    chartPadding,
    plotHeight,
    plotWidth,
    minPlotX,
    maxPlotX,
    xTicks: xAxis.ticks ?? [],
    yTicks: yAxis.ticks ?? [],
    frequencyForX,
    xForFrequency,
    yForValue,
    pathForSignal: (signal: IFrequencySpectrumSignal) =>
      (signal.points ?? [])
        .map((point, index) => {
          const prefix = index === 0 ? 'M' : 'L'
          return `${prefix}${xForFrequency(point.frequencyHz)} ${yForValue(point.value)}`
        })
        .join(' '),
  }
}

interface ISpectrumViewport {
  startHz: number
  endHz: number
}

const getSpectrumViewport = (
  xAxis: IFrequencySpectrumAxis,
  startHz: number,
  endHz: number
): ISpectrumViewport => {
  const minimum = xAxis.minimum
  const maximum = xAxis.maximum
  const clampedStart = Math.min(Math.max(startHz, minimum), maximum - 1)
  const clampedEnd = Math.max(clampedStart + 1, Math.min(Math.max(endHz, minimum + 1), maximum))

  return {
    startHz: clampedStart,
    endHz: clampedEnd,
  }
}

const getVisibleSpectrumSignals = (
  signals: IFrequencySpectrumSignal[],
  viewport: ISpectrumViewport
) =>
  signals.map((signal) => ({
    ...signal,
    points: signal.points.filter(
      (point) =>
        point.frequencyHz >= viewport.startHz &&
        point.frequencyHz < viewport.endHz
    ),
  }))

const getVisibleSpectrumXAxis = (
  xAxis: IFrequencySpectrumAxis,
  viewport: ISpectrumViewport
): IFrequencySpectrumAxis => ({
  ...xAxis,
  minimum: viewport.startHz,
  maximum: viewport.endHz,
  ticks: buildLinearTicks(viewport.startHz, viewport.endHz, 5),
})

const buildLinearTicks = (minimum: number, maximum: number, count: number) => {
  if (count <= 1) {
    return [minimum]
  }

  return Array.from({ length: count }, (_, index) =>
    minimum + (((maximum - minimum) / (count - 1)) * index)
  )
}

const getNearestSpectrumPoint = (
  points: IFrequencySpectrumPoint[],
  frequencyHz: number
) => {
  if (points.length === 0) {
    return null
  }

  const firstPoint = points[0]
  const lastPoint = points[points.length - 1]

  if (!firstPoint || !lastPoint) {
    return null
  }

  const span = lastPoint.frequencyHz - firstPoint.frequencyHz
  if (span <= 0 || points.length === 1) {
    return firstPoint
  }

  const approximateIndex = Math.round(
    ((frequencyHz - firstPoint.frequencyHz) / span) * (points.length - 1)
  )
  const clampedIndex = Math.min(points.length - 1, Math.max(0, approximateIndex))

  return points[clampedIndex] ?? null
}

const formatFrequencyTick = (frequencyHz: number) => {
  if (frequencyHz >= 1000) {
    return `${(frequencyHz / 1000).toFixed(frequencyHz >= 10000 ? 0 : 1)}k`
  }

  return `${frequencyHz.toFixed(frequencyHz >= 100 ? 0 : 1)}`
}

const formatSpectrumTick = (value: number) => value.toFixed(0)
const formatHoveredFrequency = (frequencyHz: number) => `${frequencyHz.toFixed(1)} Hz`
const formatHoveredSpectrumValue = (value: number, unit: string) => `${value.toFixed(1)} ${unit}`

export {
  formatHoveredFrequency,
  formatHoveredSpectrumValue,
  formatFrequencyTick,
  formatSpectrumTick,
  getNearestSpectrumPoint,
  getSpectrumChartModel,
  getSpectrumViewport,
  getVisibleSpectrumSignals,
  getVisibleSpectrumXAxis,
}
export type { ISpectrumViewport }
