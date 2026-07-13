import type { ITimeWaveformAxis, ITimeWaveformSignal } from '../../types'

type TChartDensity = 'default' | 'compact'

const getWaveformChartDimensions = (width: number, density: TChartDensity) => {
  if (density === 'compact') {
    if (width <= 720) {
      return {
        chartHeight: 168,
        chartPadding: {
          top: 12,
          right: 12,
          bottom: 28,
          left: 42,
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
          left: 46,
        },
      }
    }

    return {
      chartHeight: 196,
      chartPadding: {
        top: 16,
        right: 16,
        bottom: 32,
        left: 48,
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
        left: 48,
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
        left: 52,
      },
    }
  }

  return {
    chartHeight: 276,
    chartPadding: {
      top: 20,
      right: 18,
      bottom: 40,
      left: 54,
    },
  }
}

const getWaveformChartModel = (
  signals: ITimeWaveformSignal[],
  yAxis: ITimeWaveformAxis,
  width: number,
  density: TChartDensity = 'default'
) => {
  const { chartHeight, chartPadding } = getWaveformChartDimensions(width, density)
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
    timeForX: (x: number) =>
      ((Math.min(Math.max(x, chartPadding.left), chartPadding.left + plotWidth) - chartPadding.left) / plotWidth) * maxDuration,
    yForAmplitude: (amplitude: number) =>
      chartPadding.top +
      ((yAxis.maximum - amplitude) / (yAxis.maximum - yAxis.minimum || 1)) * plotHeight,
  }
}

const buildWaveformPath = (
  signal: ITimeWaveformSignal,
  xForBinIndex: (binIndex: number, binCount: number) => number,
  yForAmplitude: (amplitude: number) => number
) =>
  signal.bins
    .map((bin, binIndex) => {
      const x = xForBinIndex(binIndex, signal.bins.length)
      return `M${x} ${yForAmplitude(bin[0])} L${x} ${yForAmplitude(bin[1])}`
    })
    .join(' ')

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
  buildWaveformPath,
  formatAmplitudeTick,
  formatTimeTick,
  getAmplitudeUnitLabel,
  getWaveformChartModel,
}
