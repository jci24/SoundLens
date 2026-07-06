import type { ITimeWaveformAxis, ITimeWaveformSignal } from '../../types'

const getWaveformChartDimensions = (width: number) => {
  if (width <= 720) {
    return {
      chartHeight: 264,
      chartPadding: {
        top: 18,
        right: 14,
        bottom: 34,
        left: 48,
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
        left: 52,
      },
    }
  }

  return {
    chartHeight: 360,
    chartPadding: {
      top: 22,
      right: 18,
      bottom: 38,
      left: 54,
    },
  }
}

const getWaveformChartModel = (
  signals: ITimeWaveformSignal[],
  yAxis: ITimeWaveformAxis,
  width: number
) => {
  const { chartHeight, chartPadding } = getWaveformChartDimensions(width)
  const plotWidth = Math.max(1, width - chartPadding.left - chartPadding.right)
  const plotHeight = chartHeight - chartPadding.top - chartPadding.bottom
  const maxDuration = Math.max(...signals.map((signal) => signal.durationSeconds), 1)

  return {
    chartHeight,
    chartPadding,
    plotHeight,
    plotWidth,
    xTicks: buildTimeTicks(maxDuration),
    yTicks: yAxis.ticks,
    xForBinIndex: (binIndex: number, binCount: number) =>
      chartPadding.left +
      (binCount <= 1 ? 0 : (binIndex / (binCount - 1)) * plotWidth),
    xForTime: (timeSeconds: number) =>
      chartPadding.left + (timeSeconds / maxDuration) * plotWidth,
    yForAmplitude: (amplitude: number) =>
      chartPadding.top +
      ((yAxis.maximum - amplitude) / (yAxis.maximum - yAxis.minimum || 1)) * plotHeight,
  }
}

const getAmplitudeUnitLabel = (yAxis: ITimeWaveformAxis) => yAxis.unit

const buildTimeTicks = (durationSeconds: number) => {
  const tickCount = 5
  return Array.from({ length: tickCount }, (_, index) =>
    (durationSeconds / (tickCount - 1)) * index
  )
}

const formatTimeTick = (seconds: number) => `${seconds.toFixed(seconds >= 10 ? 0 : 1)}`
const formatAmplitudeTick = (amplitude: number) => amplitude.toFixed(1)

export {
  formatAmplitudeTick,
  formatTimeTick,
  getAmplitudeUnitLabel,
  getWaveformChartModel,
}
