import type {
  IFrequencySpectrumPoint,
  IFrequencySpectrumAxis,
  IFrequencySpectrumSignal,
} from '../../types'

type TChartDensity = 'default' | 'compact'

const getSpectrumChartDimensions = (width: number, density: TChartDensity) => {
  if (density === 'compact') {
    if (width <= 720) {
      return {
        chartHeight: 168,
        chartPadding: {
          top: 12,
          right: 12,
          bottom: 28,
          left: 46,
        },
      }
    }

    if (width <= 1080) {
      return {
        chartHeight: 184,
        chartPadding: {
          top: 14,
          right: 14,
          bottom: 30,
          left: 54,
        },
      }
    }

    return {
      chartHeight: 196,
      chartPadding: {
        top: 16,
        right: 16,
        bottom: 32,
        left: 58,
      },
    }
  }

  if (width <= 720) {
    return {
      chartHeight: 220,
      chartPadding: {
        top: 16,
        right: 14,
        bottom: 36,
        left: 54,
      },
    }
  }

  if (width <= 1080) {
    return {
      chartHeight: 248,
      chartPadding: {
        top: 18,
        right: 16,
        bottom: 38,
        left: 64,
      },
    }
  }

  return {
    chartHeight: 276,
    chartPadding: {
      top: 20,
      right: 18,
      bottom: 40,
      left: 72,
    },
  }
}

const getSpectrumChartModel = (
  xAxis: IFrequencySpectrumAxis,
  yAxis: IFrequencySpectrumAxis,
  width: number,
  density: TChartDensity = 'default'
) => {
  const { chartHeight, chartPadding } = getSpectrumChartDimensions(width, density)
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

  const getRenderablePoints = (signal: IFrequencySpectrumSignal) =>
    simplifySpectrumPoints(signal.points ?? [], minFrequency, maxFrequency, plotWidth)

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
      getRenderablePoints(signal)
        .map((point, index) => {
          const prefix = index === 0 ? 'M' : 'L'
          return `${prefix}${xForFrequency(point.frequencyHz)} ${yForValue(point.value)}`
        })
        .join(' '),
  }
}

const simplifySpectrumPoints = (
  points: IFrequencySpectrumPoint[],
  minimumFrequencyHz: number,
  maximumFrequencyHz: number,
  plotWidth: number
) => {
  const targetPointCount = Math.max(64, Math.floor(plotWidth * 1.5))
  if (points.length <= targetPointCount || plotWidth <= 1) {
    return points
  }

  const frequencySpan = Math.max(maximumFrequencyHz - minimumFrequencyHz, 1)
  const bucketCount = Math.max(1, Math.floor(plotWidth))
  const buckets = Array.from({ length: bucketCount }, () => ({
    totalFrequencyHz: 0,
    totalValue: 0,
    count: 0,
  }))

  for (const point of points) {
    const normalizedFrequency = (point.frequencyHz - minimumFrequencyHz) / frequencySpan
    const bucketIndex = Math.min(
      bucketCount - 1,
      Math.max(0, Math.floor(normalizedFrequency * bucketCount))
    )
    const bucket = buckets[bucketIndex]

    bucket.totalFrequencyHz += point.frequencyHz
    bucket.totalValue += point.value
    bucket.count += 1
  }

  return buckets
    .filter((bucket) => bucket.count > 0)
    .map((bucket) => ({
      frequencyHz: bucket.totalFrequencyHz / bucket.count,
      value: bucket.totalValue / bucket.count,
    }))
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
  simplifySpectrumPoints,
}
export type { ISpectrumViewport }
