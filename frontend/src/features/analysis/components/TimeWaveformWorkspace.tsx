import { useEffect, useRef, useState } from 'react'
import { AlertCircle, ChevronRight, Loader2, SlidersHorizontal } from 'lucide-react'
import {
  Combobox,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxInput,
  ComboboxItem,
  ComboboxList,
} from '@/components/ui/combobox'
import { Checkbox } from '@/components/ui/checkbox'
import { Field, FieldLabel } from '@/components/ui/field'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useTimeWaveformWorkspace } from '../hooks/useTimeWaveformWorkspace'
import {
  formatHoveredFrequency,
  formatHoveredSpectrumValue,
  formatFrequencyTick,
  formatSpectrumTick,
  getSpectrumChartModel,
} from '../utils/spectrumChart'
import { useSpectrumChartHover } from '../hooks/useSpectrumChartHover'
import {
  formatAmplitudeTick,
  formatTimeTick,
  getAmplitudeUnitLabel,
  getWaveformChartModel,
} from '../utils/waveformChart'
import type {
  IFrequencySpectrumAxis,
  IFrequencySpectrumSignal,
  IImportedFileSummary,
  ITimeWaveformAxis,
  ITimeWaveformSignal,
} from '../../import/types'
import './TimeWaveformWorkspace.scss'

interface ITimeWaveformWorkspaceProps {
  importedFiles: IImportedFileSummary[]
}

const TimeWaveformWorkspace = ({ importedFiles }: ITimeWaveformWorkspaceProps) => {
  const [isSpectrumControlsOpen, setIsSpectrumControlsOpen] = useState(false)
  const spectrumControlsRef = useRef<HTMLDivElement | null>(null)
  const {
    activeSurface,
    chartRef,
    chartWidth,
    error,
    expandedRecordings,
    isInitialLoading,
    isRefreshing,
    spectrumFftSizeOptions,
    spectrumMaximumHz,
    spectrumRangeEndHz,
    spectrumRangeStartHz,
    spectrumViewport,
    selectedSpectrumPreset,
    spectrum,
    spectrumXAxis,
    spectrumSignals,
    waveformSignals,
    recordings,
    selectedSignalIds,
    waveforms,
    onRecordingToggle,
    onSignalSelection,
    onSpectrumPresetChange,
    onSpectrumRangeEndChange,
    onSpectrumRangeReset,
    onSpectrumRangeStartChange,
    onSurfaceChange,
  } = useTimeWaveformWorkspace(importedFiles)

  const waveformYAxis = waveforms?.yAxis ?? null
  const spectrumYAxis = spectrum?.yAxis ?? null
  const activeTitle = activeSurface === 'waveform' ? 'Waveform overview' : 'Spectrum overview'
  const activeEyebrow = activeSurface === 'waveform' ? 'Time analysis' : 'Frequency analysis'
  const isSpectrumRangeFiltered =
    activeSurface === 'spectrum' &&
    spectrumViewport !== null &&
    (Math.round(spectrumRangeStartHz) > 0 || Math.round(spectrumRangeEndHz) < Math.round(spectrumMaximumHz))
  const hasActiveChart =
    activeSurface === 'waveform'
      ? waveformSignals.length > 0 && waveformYAxis !== null && chartWidth > 0
      : spectrumSignals.length > 0 && spectrumXAxis !== null && spectrumYAxis !== null && chartWidth > 0

  useEffect(() => {
    if (!isSpectrumControlsOpen) {
      return
    }

    const handlePointerDown = (event: MouseEvent) => {
      const target = event.target

      if (!(target instanceof Node)) {
        return
      }

      if (!spectrumControlsRef.current?.contains(target)) {
        setIsSpectrumControlsOpen(false)
      }
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsSpectrumControlsOpen(false)
      }
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [isSpectrumControlsOpen])

  return (
    <section
      className={`time-waveform-workspace${hasActiveChart ? ' time-waveform-workspace--revealed' : ''}`}
      aria-label="Analysis workspace"
    >
      <header className="time-waveform-workspace__header">
        <div>
          <p className="time-waveform-workspace__eyebrow">{activeEyebrow}</p>
          <h2 className="time-waveform-workspace__title">{activeTitle}</h2>
        </div>
      </header>

      <div className="time-waveform-workspace__surface-bar">
        <Tabs
          className="time-waveform-workspace__surface-tabs"
          value={activeSurface}
          onValueChange={(value) => onSurfaceChange(value as 'waveform' | 'spectrum')}
        >
          <TabsList>
            <TabsTrigger value="waveform">Waveform</TabsTrigger>
            <TabsTrigger value="spectrum">Spectrum</TabsTrigger>
          </TabsList>
        </Tabs>

        {activeSurface === 'spectrum' && spectrumViewport && (
          <div className="time-waveform-workspace__surface-controls">
            {isSpectrumRangeFiltered && (
              <button
                className="time-waveform-workspace__range-badge"
                type="button"
                onClick={() => setIsSpectrumControlsOpen(true)}
              >
                {formatFrequencyRange(spectrumRangeStartHz, spectrumRangeEndHz)}
              </button>
            )}

            <div className="time-waveform-workspace__controls-popover" ref={spectrumControlsRef}>
              <button
                aria-expanded={isSpectrumControlsOpen}
                aria-haspopup="dialog"
                aria-label="Spectrum settings"
                className={`time-waveform-workspace__controls-trigger${isSpectrumControlsOpen ? ' time-waveform-workspace__controls-trigger--open' : ''}`}
                type="button"
                onClick={() => setIsSpectrumControlsOpen((current) => !current)}
              >
                <SlidersHorizontal size={15} />
              </button>

              <div
                className={`time-waveform-workspace__controls-panel${isSpectrumControlsOpen ? ' time-waveform-workspace__controls-panel--open' : ''}`}
                hidden={!isSpectrumControlsOpen}
                role="dialog"
                aria-label="Spectrum settings"
              >
                <div className="time-waveform-workspace__controls-group">
                  <span className="time-waveform-workspace__controls-caption">FFT</span>
                  <Combobox
                    items={spectrumFftSizeOptions}
                    value={selectedSpectrumPreset}
                    onValueChange={onSpectrumPresetChange}
                  >
                    <ComboboxInput placeholder="FFT size" className="time-waveform-workspace__parameter-combobox" />
                    <ComboboxContent>
                      <ComboboxEmpty>No FFT size options found.</ComboboxEmpty>
                      <ComboboxList>
                        {(item) => (
                          <ComboboxItem key={item} value={item}>
                            {item}
                          </ComboboxItem>
                        )}
                      </ComboboxList>
                    </ComboboxContent>
                  </Combobox>
                </div>

                <div className="time-waveform-workspace__controls-group">
                  <span className="time-waveform-workspace__controls-caption">Range</span>
                  <div className="time-waveform-workspace__zoom-panel" aria-label="Spectrum range">
                    <label className="time-waveform-workspace__zoom-input-group">
                      <input
                        className="time-waveform-workspace__zoom-input"
                        max={Math.max(1, Math.floor(spectrumMaximumHz - 1))}
                        min={0}
                        step={1}
                        type="number"
                        value={Math.round(spectrumRangeStartHz)}
                        onChange={(event) => onSpectrumRangeStartChange(event.target.value)}
                      />
                      <span>Hz</span>
                    </label>
                    <label className="time-waveform-workspace__zoom-input-group">
                      <input
                        className="time-waveform-workspace__zoom-input"
                        max={Math.ceil(spectrumMaximumHz)}
                        min={1}
                        step={1}
                        type="number"
                        value={Math.round(spectrumRangeEndHz)}
                        onChange={(event) => onSpectrumRangeEndChange(event.target.value)}
                      />
                      <span>Hz</span>
                    </label>
                    <button
                      className="time-waveform-workspace__zoom-reset"
                      type="button"
                      onClick={onSpectrumRangeReset}
                    >
                      Reset
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>

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
                    const isSelected = selectedSignalIds.includes(signal.signalId)

                    return (
                      <Field
                        className={`time-waveform-workspace__signal-row${isSelected ? ' time-waveform-workspace__signal-row--selected' : ''}`}
                        data-selected={isSelected}
                        key={signal.signalId}
                        orientation="horizontal"
                      >
                        <Checkbox
                          checked={isSelected}
                          className="time-waveform-workspace__signal-checkbox"
                          id={signal.signalId}
                          onCheckedChange={() => onSignalSelection(signal.signalId)}
                        />
                        <FieldLabel
                          className="time-waveform-workspace__signal-label"
                          htmlFor={signal.signalId}
                        >
                          {signal.displayName}
                        </FieldLabel>
                      </Field>
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
              <span>{activeSurface === 'waveform' ? 'Generating waveform bins' : 'Generating spectrum bins'}</span>
            </div>
          )}

          {isRefreshing && !isInitialLoading && (
            <div className="time-waveform-workspace__loading-pill" aria-live="polite">
              <Loader2 className="time-waveform-workspace__spinner" size={14} />
              <span>{activeSurface === 'waveform' ? 'Updating overlay' : 'Updating spectrum'}</span>
            </div>
          )}

          {error && (
            <div className="time-waveform-workspace__state time-waveform-workspace__state--error">
              <AlertCircle size={20} />
              <span>{error}</span>
            </div>
          )}

          {!error && activeSurface === 'waveform' && waveformYAxis && chartWidth > 0 && waveformSignals.length > 0 && (
            <WaveformChart
              signals={waveformSignals}
              yAxis={waveformYAxis}
              width={chartWidth}
            />
          )}

          {!error &&
            activeSurface === 'spectrum' &&
            spectrumXAxis &&
            spectrumYAxis &&
            Array.isArray(spectrumXAxis.ticks) &&
            Array.isArray(spectrumYAxis.ticks) &&
            chartWidth > 0 &&
            spectrumSignals.length > 0 && (
              <SpectrumChart
                signals={spectrumSignals}
                xAxis={spectrumXAxis}
                yAxis={spectrumYAxis}
                width={chartWidth}
              />
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

interface ISpectrumChartProps {
  signals: IFrequencySpectrumSignal[]
  xAxis: IFrequencySpectrumAxis
  yAxis: IFrequencySpectrumAxis
  width: number
}

const SpectrumChart = ({ signals, xAxis, yAxis, width }: ISpectrumChartProps) => {
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
      className="time-waveform-workspace__chart"
      role="img"
      aria-label="Frequency spectrum overlay with frequency on the X axis and relative amplitude on the Y axis"
      ref={svgRef}
      viewBox={`0 0 ${width} ${chartHeight}`}
      onPointerLeave={onPointerLeave}
      onPointerMove={onPointerMove}
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
        const y = yForValue(tick)
        return (
          <g key={tick}>
            <line className="time-waveform-workspace__grid-line" x1={chartPadding.left} x2={width - chartPadding.right} y1={y} y2={y} />
            <text className="time-waveform-workspace__axis-label" x={chartPadding.left - 10} y={y + 4} textAnchor="end">
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
            <text className="time-waveform-workspace__axis-label" x={x} y={chartPadding.top + plotHeight + 22} textAnchor="middle">
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
        x={chartPadding.left}
        y={chartPadding.top}
        width={plotWidth}
        height={plotHeight}
        rx="6"
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
              rx="12"
              width="176"
              height={hoverState.tooltipHeight}
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
                <text className="time-waveform-workspace__hover-tooltip-value" x="164" y="0" textAnchor="end">
                  {formatHoveredSpectrumValue(seriesItem.value, yAxis.unit)}
                </text>
              </g>
            ))}
          </g>
        </g>
      )}

      <text className="time-waveform-workspace__axis-title" x={chartPadding.left + plotWidth / 2} y={chartHeight - 6} textAnchor="middle">
        Hz
      </text>
      <text
        className="time-waveform-workspace__axis-title"
        x="18"
        y={chartPadding.top + plotHeight / 2}
        textAnchor="middle"
        transform={`rotate(-90 18 ${chartPadding.top + plotHeight / 2})`}
      >
        {yAxis.unit}
      </text>
    </svg>
  )
}

const formatFrequencyRange = (startHz: number, endHz: number) =>
  `${formatCompactFrequency(startHz)} - ${formatCompactFrequency(endHz)}`

const formatCompactFrequency = (frequencyHz: number) =>
  frequencyHz >= 1000
    ? `${(frequencyHz / 1000).toFixed(frequencyHz >= 10000 ? 0 : 1)}k Hz`
    : `${Math.round(frequencyHz)} Hz`

export { TimeWaveformWorkspace }
