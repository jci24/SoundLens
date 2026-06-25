import {
  formatAmplitudeTick,
  formatTimeTick,
  getAmplitudeUnitLabel,
  getWaveformChartModel,
} from '../utils/waveformChart'
import type { ITimeWaveformAxis, ITimeWaveformSignal } from '../types'
import './WaveformChart.scss'

interface IWaveformChartProps {
  signals: ITimeWaveformSignal[]
  width: number
  yAxis: ITimeWaveformAxis
}

const WaveformChart = ({ signals, width, yAxis }: IWaveformChartProps) => {
  const {
    chartHeight,
    chartPadding,
    plotHeight,
    plotWidth,
    xForBinIndex,
    xForTime,
    xTicks,
    yForAmplitude,
    yTicks,
  } = getWaveformChartModel(signals, yAxis, width)

  return (
    <svg
      aria-label="Time-domain waveform overlay with seconds on the X axis and normalized amplitude on the Y axis"
      className="time-waveform-workspace__chart"
      role="img"
      viewBox={`0 0 ${width} ${chartHeight}`}
    >
      <rect
        className="time-waveform-workspace__plot-background"
        height={plotHeight}
        rx="6"
        width={plotWidth}
        x={chartPadding.left}
        y={chartPadding.top}
      />

      {yTicks.map((tick) => {
        const y = yForAmplitude(tick)
        return (
          <g key={tick}>
            <line className="time-waveform-workspace__grid-line" x1={chartPadding.left} x2={width - chartPadding.right} y1={y} y2={y} />
            <text className="time-waveform-workspace__axis-label" textAnchor="end" x={chartPadding.left - 10} y={y + 4}>
              {formatAmplitudeTick(tick)}
            </text>
          </g>
        )
      })}

      {xTicks.map((tick) => {
        const x = xForTime(tick)
        return (
          <g key={tick}>
            <line className="time-waveform-workspace__tick-line" x1={x} x2={x} y1={chartPadding.top} y2={chartPadding.top + plotHeight} />
            <text className="time-waveform-workspace__axis-label" textAnchor="middle" x={x} y={chartPadding.top + plotHeight + 22}>
              {formatTimeTick(tick)}
            </text>
          </g>
        )
      })}

      <line className="time-waveform-workspace__axis-line" x1={chartPadding.left} x2={chartPadding.left} y1={chartPadding.top} y2={chartPadding.top + plotHeight} />
      <line className="time-waveform-workspace__axis-line" x1={chartPadding.left} x2={width - chartPadding.right} y1={chartPadding.top + plotHeight} y2={chartPadding.top + plotHeight} />

      {signals.map((signal, signalIndex) => (
        <g className="time-waveform-workspace__series" key={signal.signalId}>
          <title>{`${signal.recordingFileName}, ${signal.displayName}, ${signal.durationSeconds.toFixed(2)} seconds, ${signal.sampleRate} Hz`}</title>
          {signal.bins.map((bin, binIndex) => {
            const x = xForBinIndex(binIndex, signal.bins.length)
            return (
              <line
                className={`time-waveform-workspace__waveform-line time-waveform-workspace__waveform-line--${signalIndex % 4}`}
                key={`${signal.signalId}-${binIndex}`}
                x1={x}
                x2={x}
                y1={yForAmplitude(bin[0])}
                y2={yForAmplitude(bin[1])}
              />
            )
          })}
        </g>
      ))}

      <text className="time-waveform-workspace__axis-title" textAnchor="middle" x={chartPadding.left + plotWidth / 2} y={chartHeight - 6}>
        s
      </text>
      <text
        className="time-waveform-workspace__axis-title"
        textAnchor="middle"
        transform={`rotate(-90 18 ${chartPadding.top + plotHeight / 2})`}
        x="18"
        y={chartPadding.top + plotHeight / 2}
      >
        {getAmplitudeUnitLabel(yAxis)}
      </text>
    </svg>
  )
}

export { WaveformChart }
