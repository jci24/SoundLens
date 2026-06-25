import { AlertCircle, CheckSquare, ChevronRight, Loader2, Square } from 'lucide-react'
import { useTimeWaveformWorkspace } from '../hooks/useTimeWaveformWorkspace'
import {
  formatAmplitudeTick,
  formatTimeTick,
  getAmplitudeUnitLabel,
  getWaveformChartModel,
} from '../utils/waveformChart'
import type {
  IImportedFileSummary,
  ITimeWaveformAxis,
  ITimeWaveformSignal,
} from '../../import/types'
import './TimeWaveformWorkspace.scss'

interface ITimeWaveformWorkspaceProps {
  importedFiles: IImportedFileSummary[]
}

const TimeWaveformWorkspace = ({ importedFiles }: ITimeWaveformWorkspaceProps) => {
  const {
    chartRef,
    chartWidth,
    error,
    expandedRecordings,
    isInitialLoading,
    isRefreshing,
    isWaveformVisible,
    recordings,
    selectedSignals,
    yAxis,
    onRecordingToggle,
    onSignalSelection,
  } = useTimeWaveformWorkspace(importedFiles)

  return (
    <section
      className={`time-waveform-workspace${isWaveformVisible ? ' time-waveform-workspace--revealed' : ''}`}
      aria-label="Time waveform analysis"
    >
      <header className="time-waveform-workspace__header">
        <div>
          <p className="time-waveform-workspace__eyebrow">Time analysis</p>
          <h2 className="time-waveform-workspace__title">Waveform overview</h2>
        </div>
      </header>

      <div className="time-waveform-workspace__body">
        <aside className="time-waveform-workspace__recording-rail" aria-label="Imported recordings and channels">
          {recordings.map((recording) => (
            <section className="time-waveform-workspace__recording-group" key={recording.recordingId}>
              <button
                className="time-waveform-workspace__recording-toggle"
                type="button"
                onClick={() => onRecordingToggle(recording.recordingId)}
              >
                <span className="time-waveform-workspace__recording-heading">
                  <ChevronRight
                    className={`time-waveform-workspace__recording-chevron${expandedRecordings.includes(recording.recordingId) ? ' time-waveform-workspace__recording-chevron--open' : ''}`}
                    size={14}
                  />
                  <span className="time-waveform-workspace__recording-name">{recording.fileName}</span>
                </span>
              </button>

              {expandedRecordings.includes(recording.recordingId) && (
                <div className="time-waveform-workspace__signal-list">
                  {recording.signals.map((signal) => {
                    const isSelected = selectedSignals.some(
                      (selectedSignal) => selectedSignal.signalId === signal.signalId
                    )

                    return (
                      <button
                        className={`time-waveform-workspace__signal-row${isSelected ? ' time-waveform-workspace__signal-row--selected' : ''}`}
                        key={signal.signalId}
                        type="button"
                        onClick={(event) => onSignalSelection(signal.signalId, event)}
                      >
                        <span className="time-waveform-workspace__signal-label">
                          {isSelected ? <CheckSquare size={13} /> : <Square size={13} />}
                          {signal.displayName}
                        </span>
                      </button>
                    )
                  })}
                </div>
              )}
            </section>
          ))}
        </aside>

        <div className="time-waveform-workspace__chart-shell" ref={chartRef}>
          {isInitialLoading && (
            <div className="time-waveform-workspace__state">
              <Loader2 className="time-waveform-workspace__spinner" size={20} />
              <span>Generating waveform bins</span>
            </div>
          )}

          {isRefreshing && !isInitialLoading && (
            <div className="time-waveform-workspace__loading-pill" aria-live="polite">
              <Loader2 className="time-waveform-workspace__spinner" size={14} />
              <span>Updating overlay</span>
            </div>
          )}

          {error && (
            <div className="time-waveform-workspace__state time-waveform-workspace__state--error">
              <AlertCircle size={20} />
              <span>{error}</span>
            </div>
          )}

          {!error && selectedSignals.length > 0 && yAxis && chartWidth > 0 && (
            <WaveformChart signals={selectedSignals} yAxis={yAxis} width={chartWidth} />
          )}
        </div>
      </div>
    </section>
  )
}

interface IWaveformChartProps {
  signals: ITimeWaveformSignal[]
  yAxis: ITimeWaveformAxis
  width: number
}

const WaveformChart = ({ signals, yAxis, width }: IWaveformChartProps) => {
  const {
    chartHeight,
    chartPadding,
    plotHeight,
    plotWidth,
    xForTime,
    xTicks,
    yForAmplitude,
    yTicks,
  } = getWaveformChartModel(signals, yAxis, width)

  return (
    <svg
      className="time-waveform-workspace__chart"
      role="img"
      aria-label="Time-domain waveform overlay with seconds on the X axis and normalized amplitude on the Y axis"
      viewBox={`0 0 ${width} ${chartHeight}`}
    >
      <rect
        className="time-waveform-workspace__plot-background"
        x={chartPadding.left}
        y={chartPadding.top}
        width={plotWidth}
        height={plotHeight}
        rx="6"
      />

      {yTicks.map((tick) => {
        const y = yForAmplitude(tick)
        return (
          <g key={tick}>
            <line className="time-waveform-workspace__grid-line" x1={chartPadding.left} x2={width - chartPadding.right} y1={y} y2={y} />
            <text className="time-waveform-workspace__axis-label" x={chartPadding.left - 10} y={y + 4} textAnchor="end">
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
            <text className="time-waveform-workspace__axis-label" x={x} y={chartPadding.top + plotHeight + 22} textAnchor="middle">
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
          {signal.points.map((point, pointIndex) => {
            const x = xForTime(point.timeSeconds)
            return (
              <line
                className={`time-waveform-workspace__waveform-line time-waveform-workspace__waveform-line--${signalIndex % 4}`}
                key={`${signal.signalId}-${pointIndex}`}
                x1={x}
                x2={x}
                y1={yForAmplitude(point.minAmplitude)}
                y2={yForAmplitude(point.maxAmplitude)}
              />
            )
          })}
        </g>
      ))}

      <text className="time-waveform-workspace__axis-title" x={chartPadding.left + plotWidth / 2} y={chartHeight - 6} textAnchor="middle">
        s
      </text>
      <text
        className="time-waveform-workspace__axis-title"
        x="18"
        y={chartPadding.top + plotHeight / 2}
        textAnchor="middle"
        transform={`rotate(-90 18 ${chartPadding.top + plotHeight / 2})`}
      >
        {getAmplitudeUnitLabel(yAxis)}
      </text>
    </svg>
  )
}

export { TimeWaveformWorkspace }
