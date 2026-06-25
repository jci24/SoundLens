import { useSpectrumChartHover } from '../hooks/useSpectrumChartHover'
import {
  formatFrequencyTick,
  formatHoveredFrequency,
  formatHoveredSpectrumValue,
  formatSpectrumTick,
  getSpectrumChartModel,
} from '../utils/spectrumChart'
import type { IFrequencySpectrumAxis, IFrequencySpectrumSignal } from '../types'
import './SpectrumChart.scss'

interface ISpectrumChartProps {
  signals: IFrequencySpectrumSignal[]
  width: number
  xAxis: IFrequencySpectrumAxis
  yAxis: IFrequencySpectrumAxis
}

const SpectrumChart = ({ signals, width, xAxis, yAxis }: ISpectrumChartProps) => {
  const {
    chartHeight,
    chartPadding,
    pathForSignal,
    plotHeight,
    plotWidth,
    xForFrequency,
    xTicks,
    yForValue,
    yTicks,
  } = getSpectrumChartModel(xAxis, yAxis, width)
  const {
    hoverState,
    onPointerLeave,
    onPointerMove,
    svgRef,
  } = useSpectrumChartHover(signals, xAxis, yAxis, width)

  return (
    <svg
      aria-label="Frequency spectrum overlay with frequency on the X axis and relative amplitude on the Y axis"
      className="time-waveform-workspace__chart"
      ref={svgRef}
      role="img"
      viewBox={`0 0 ${width} ${chartHeight}`}
      onPointerLeave={onPointerLeave}
      onPointerMove={onPointerMove}
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
        const y = yForValue(tick)
        return (
          <g key={tick}>
            <line className="time-waveform-workspace__grid-line" x1={chartPadding.left} x2={width - chartPadding.right} y1={y} y2={y} />
            <text className="time-waveform-workspace__axis-label" textAnchor="end" x={chartPadding.left - 10} y={y + 4}>
              {formatSpectrumTick(tick)}
            </text>
          </g>
        )
      })}

      {xTicks.map((tick) => {
        const x = xForFrequency(tick)
        return (
          <g key={tick}>
            <line className="time-waveform-workspace__tick-line" x1={x} x2={x} y1={chartPadding.top} y2={chartPadding.top + plotHeight} />
            <text className="time-waveform-workspace__axis-label" textAnchor="middle" x={x} y={chartPadding.top + plotHeight + 22}>
              {formatFrequencyTick(tick)}
            </text>
          </g>
        )
      })}

      <line className="time-waveform-workspace__axis-line" x1={chartPadding.left} x2={chartPadding.left} y1={chartPadding.top} y2={chartPadding.top + plotHeight} />
      <line className="time-waveform-workspace__axis-line" x1={chartPadding.left} x2={width - chartPadding.right} y1={chartPadding.top + plotHeight} y2={chartPadding.top + plotHeight} />

      {signals.map((signal, signalIndex) => (
        <g className="time-waveform-workspace__series" key={signal.signalId}>
          <title>{`${signal.recordingFileName}, ${signal.displayName}, ${signal.sampleRate} Hz, ${signal.points.length} bins`}</title>
          <path
            className={`time-waveform-workspace__spectrum-line time-waveform-workspace__spectrum-line--${signalIndex % 4}`}
            d={pathForSignal(signal)}
          />
        </g>
      ))}

      <rect
        className="time-waveform-workspace__hover-target"
        height={plotHeight}
        width={plotWidth}
        x={chartPadding.left}
        y={chartPadding.top}
      />

      {hoverState && (
        <g className="time-waveform-workspace__hover-layer">
          <line
            className="time-waveform-workspace__hover-guide"
            x1={hoverState.guideX}
            x2={hoverState.guideX}
            y1={chartPadding.top}
            y2={chartPadding.top + plotHeight}
          />

          {hoverState.series.map((seriesItem) => (
            <circle
              className={`time-waveform-workspace__hover-marker time-waveform-workspace__hover-marker--${seriesItem.signalIndex % 4}`}
              cx={seriesItem.x}
              cy={seriesItem.y}
              key={seriesItem.signalId}
              r="3.5"
            />
          ))}

          <g transform={`translate(${hoverState.tooltipX} ${hoverState.tooltipY})`}>
            <rect
              className="time-waveform-workspace__hover-tooltip"
              height={hoverState.tooltipHeight}
              rx="12"
              width="176"
            />
            <text className="time-waveform-workspace__hover-tooltip-title" x="12" y="18">
              {formatHoveredFrequency(hoverState.frequencyHz)}
            </text>
            {hoverState.series.map((seriesItem, index) => (
              <g key={seriesItem.signalId} transform={`translate(0 ${30 + index * 16})`}>
                <circle
                  className={`time-waveform-workspace__hover-tooltip-swatch time-waveform-workspace__hover-tooltip-swatch--${seriesItem.signalIndex % 4}`}
                  cx="12"
                  cy="-4"
                  r="3.5"
                />
                <text className="time-waveform-workspace__hover-tooltip-label" x="22" y="0">
                  {seriesItem.label}
                </text>
                <text className="time-waveform-workspace__hover-tooltip-value" textAnchor="end" x="164" y="0">
                  {formatHoveredSpectrumValue(seriesItem.value, yAxis.unit)}
                </text>
              </g>
            ))}
          </g>
        </g>
      )}

      <text className="time-waveform-workspace__axis-title" textAnchor="middle" x={chartPadding.left + plotWidth / 2} y={chartHeight - 6}>
        Hz
      </text>
      <text
        className="time-waveform-workspace__axis-title"
        textAnchor="middle"
        transform={`rotate(-90 18 ${chartPadding.top + plotHeight / 2})`}
        x="18"
        y={chartPadding.top + plotHeight / 2}
      >
        {yAxis.unit}
      </text>
    </svg>
  )
}

export { SpectrumChart }
