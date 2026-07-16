import { useMemo, useState } from 'react'
import {
  buildWaveformPath,
  formatAmplitudeTick,
  formatTimeTick,
  getAmplitudeUnitLabel,
  getSharedRegionMaximumDuration,
  getWaveformChartModel,
} from '../utils/waveformChart'
import type { IAnalysisRegionOfInterest, ITimeWaveformAxis, ITimeWaveformSignal } from '../../types'
import { useOptionalRecordingPlaybackContext } from '../../playback/contexts/recordingPlaybackContext'
import './WaveformChart.scss'

interface IWaveformChartProps {
  density?: 'default' | 'compact'
  onRegionOfInterestChange?: (regionOfInterest: IAnalysisRegionOfInterest | null) => void
  regionOfInterest?: IAnalysisRegionOfInterest | null
  signals: ITimeWaveformSignal[]
  width: number
  yAxis: ITimeWaveformAxis
}

type TRegionDragMode = 'create' | 'resize-start' | 'resize-end'

const WaveformChart = ({
  density = 'default',
  onRegionOfInterestChange,
  regionOfInterest = null,
  signals,
  width,
  yAxis,
}: IWaveformChartProps) => {
  const playback = useOptionalRecordingPlaybackContext()
  const {
    chartHeight,
    chartPadding,
    plotHeight,
    plotWidth,
    xForBinIndex,
    xForTime,
    timeForX,
    xTicks,
    yForAmplitude,
    yTicks,
  } = getWaveformChartModel(signals, yAxis, width, density)
  const [draftRegion, setDraftRegion] = useState<IAnalysisRegionOfInterest | null>(null)
  const [dragMode, setDragMode] = useState<TRegionDragMode | null>(null)
  const [dragAnchorTime, setDragAnchorTime] = useState<number | null>(null)
  const activeRegion = draftRegion ?? regionOfInterest
  const sharedRegionMaximumDuration = useMemo(
    () => getSharedRegionMaximumDuration(signals),
    [signals]
  )

  const buildRegion = (startTimeSeconds: number, endTimeSeconds: number): IAnalysisRegionOfInterest => {
    const safeStart = Math.min(
      sharedRegionMaximumDuration,
      Math.max(0, Math.min(startTimeSeconds, endTimeSeconds))
    )
    const safeEnd = Math.max(
      safeStart,
      Math.min(sharedRegionMaximumDuration, Math.max(startTimeSeconds, endTimeSeconds))
    )

    return {
      startTimeSeconds: safeStart,
      endTimeSeconds: safeEnd,
      durationSeconds: safeEnd - safeStart,
    }
  }

  const getPointerTime = (clientX: number, currentTarget: SVGSVGElement) => {
    const bounds = currentTarget.getBoundingClientRect()
    const svgX = ((clientX - bounds.left) / bounds.width) * width
    return timeForX(svgX)
  }

  const commitRegion = (nextRegion: IAnalysisRegionOfInterest | null) => {
    setDraftRegion(null)
    setDragMode(null)
    setDragAnchorTime(null)

    if (!onRegionOfInterestChange) {
      return
    }

    if (!nextRegion || nextRegion.durationSeconds <= 0) {
      onRegionOfInterestChange(null)
      return
    }

    onRegionOfInterestChange(nextRegion)
  }

  const handleCreateStart = (clientX: number, currentTarget: SVGSVGElement) => {
    const nextAnchorTime = getPointerTime(clientX, currentTarget)
    setDragAnchorTime(nextAnchorTime)
    setDragMode('create')
    setDraftRegion(buildRegion(nextAnchorTime, nextAnchorTime))
  }

  const updateDrag = (clientX: number, currentTarget: SVGSVGElement) => {
    if (dragMode === null) {
      return
    }

    const pointerTime = getPointerTime(clientX, currentTarget)

    if (dragMode === 'create' && dragAnchorTime !== null) {
      setDraftRegion(buildRegion(dragAnchorTime, pointerTime))
      return
    }

    if (!activeRegion) {
      return
    }

    if (dragMode === 'resize-start') {
      setDraftRegion(buildRegion(pointerTime, activeRegion.endTimeSeconds))
      return
    }

    setDraftRegion(buildRegion(activeRegion.startTimeSeconds, pointerTime))
  }

  const finalizeDrag = () => {
    if (draftRegion && draftRegion.durationSeconds > 0) {
      commitRegion(draftRegion)
      return
    }

    setDraftRegion(null)
    setDragMode(null)
    setDragAnchorTime(null)
  }

  const regionStartX = activeRegion ? xForTime(activeRegion.startTimeSeconds) : null
  const regionEndX = activeRegion ? xForTime(activeRegion.endTimeSeconds) : null
  const regionWidth = regionStartX !== null && regionEndX !== null ? Math.max(0, regionEndX - regionStartX) : 0
  const regionHandleWidth = 8
  const playbackPositionSeconds =
    playback?.selectedRecordingId &&
    signals.some((signal) => signal.recordingId === playback.selectedRecordingId)
      ? playback.currentTimeSeconds
      : null
  const playbackX = playbackPositionSeconds !== null
    ? xForTime(playbackPositionSeconds)
    : null

  return (
    <svg
      aria-label="Time-domain waveform overlay with seconds on the X axis and normalized amplitude on the Y axis"
      className="time-waveform-workspace__chart"
      role="img"
      viewBox={`0 0 ${width} ${chartHeight}`}
      onPointerLeave={() => {
        if (dragMode !== null) {
          finalizeDrag()
        }
      }}
      onPointerMove={(event) => updateDrag(event.clientX, event.currentTarget)}
      onPointerUp={() => finalizeDrag()}
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

      {activeRegion && regionStartX !== null && regionEndX !== null && (
        <g className="time-waveform-workspace__roi-layer">
          <rect
            className="time-waveform-workspace__roi-curtain"
            height={plotHeight}
            width={Math.max(0, regionStartX - chartPadding.left)}
            x={chartPadding.left}
            y={chartPadding.top}
          />
          <rect
            className="time-waveform-workspace__roi-curtain"
            height={plotHeight}
            width={Math.max(0, chartPadding.left + plotWidth - regionEndX)}
            x={regionEndX}
            y={chartPadding.top}
          />
          <rect
            className="time-waveform-workspace__roi-selection"
            height={plotHeight}
            width={regionWidth}
            x={regionStartX}
            y={chartPadding.top}
          />
          <rect
            className="time-waveform-workspace__roi-handle"
            height={plotHeight}
            width={regionHandleWidth}
            x={regionStartX - regionHandleWidth / 2}
            y={chartPadding.top}
            onPointerDown={(event) => {
              event.stopPropagation()
              setDragMode('resize-start')
            }}
          />
          <rect
            className="time-waveform-workspace__roi-handle"
            height={plotHeight}
            width={regionHandleWidth}
            x={regionEndX - regionHandleWidth / 2}
            y={chartPadding.top}
            onPointerDown={(event) => {
              event.stopPropagation()
              setDragMode('resize-end')
            }}
          />
        </g>
      )}

      {signals.map((signal, signalIndex) => (
        <g className="time-waveform-workspace__series" key={signal.signalId}>
          <title>{`${signal.recordingFileName}, ${signal.displayName}, ${signal.durationSeconds.toFixed(2)} seconds, ${signal.sampleRate} Hz`}</title>
          <path
            className={`time-waveform-workspace__waveform-line time-waveform-workspace__waveform-line--${signalIndex % 4}`}
            d={buildWaveformPath(signal, xForBinIndex, yForAmplitude)}
          />
        </g>
      ))}

      <rect
        className="time-waveform-workspace__roi-target"
        height={plotHeight}
        width={plotWidth}
        x={chartPadding.left}
        y={chartPadding.top}
        onDoubleClick={() => commitRegion(null)}
        onPointerDown={(event) => {
          const svgElement = event.currentTarget.ownerSVGElement
          if (!svgElement) {
            return
          }

          handleCreateStart(event.clientX, svgElement)
        }}
      />

      {playbackX !== null && (
        <g className="time-waveform-workspace__playhead" aria-hidden="true">
          <line
            className="time-waveform-workspace__playhead-line"
            x1={playbackX}
            x2={playbackX}
            y1={chartPadding.top}
            y2={chartPadding.top + plotHeight}
          />
          <path
            className="time-waveform-workspace__playhead-cap"
            d={`M${playbackX - 4} ${chartPadding.top} L${playbackX + 4} ${chartPadding.top} L${playbackX} ${chartPadding.top + 6} Z`}
          />
        </g>
      )}

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
