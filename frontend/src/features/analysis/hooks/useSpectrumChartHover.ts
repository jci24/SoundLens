import { useMemo, useRef, useState, type PointerEvent, type RefObject } from 'react'
import type { IFrequencySpectrumAxis, IFrequencySpectrumSignal } from '../../import/types'
import { getNearestSpectrumPoint, getSpectrumChartModel } from '../utils/spectrumChart'

interface ISpectrumHoverSeries {
  signalId: string
  signalIndex: number
  label: string
  frequencyHz: number
  value: number
  x: number
  y: number
}

interface ISpectrumHoverState {
  frequencyHz: number
  guideX: number
  tooltipX: number
  tooltipY: number
  tooltipHeight: number
  series: ISpectrumHoverSeries[]
}

interface IUseSpectrumChartHoverResult {
  hoverState: ISpectrumHoverState | null
  onPointerLeave: () => void
  onPointerMove: (event: PointerEvent<SVGSVGElement>) => void
  svgRef: RefObject<SVGSVGElement | null>
}

const tooltipWidth = 176
const tooltipHeaderHeight = 18
const tooltipRowHeight = 16
const tooltipTopOffset = 12

const useSpectrumChartHover = (
  signals: IFrequencySpectrumSignal[],
  xAxis: IFrequencySpectrumAxis,
  yAxis: IFrequencySpectrumAxis,
  width: number
): IUseSpectrumChartHoverResult => {
  const svgRef = useRef<SVGSVGElement | null>(null)
  const [hoverState, setHoverState] = useState<ISpectrumHoverState | null>(null)
  const chartModel = useMemo(() => getSpectrumChartModel(xAxis, yAxis, width), [xAxis, yAxis, width])

  const onPointerLeave = () => {
    setHoverState(null)
  }

  const onPointerMove = (event: PointerEvent<SVGSVGElement>) => {
    if (!svgRef.current || signals.length === 0) {
      return
    }

    const bounds = svgRef.current.getBoundingClientRect()
    if (bounds.width <= 0 || bounds.height <= 0) {
      return
    }

    const pointerX = ((event.clientX - bounds.left) / bounds.width) * width
    const frequencyHz = chartModel.frequencyForX(pointerX)
    const series = signals
      .map((signal, signalIndex) => {
        const nearestPoint = getNearestSpectrumPoint(signal.points ?? [], frequencyHz)
        if (!nearestPoint) {
          return null
        }

        return {
          signalId: signal.signalId,
          signalIndex,
          label: signal.displayName,
          frequencyHz: nearestPoint.frequencyHz,
          value: nearestPoint.value,
          x: chartModel.xForFrequency(nearestPoint.frequencyHz),
          y: chartModel.yForValue(nearestPoint.value),
        }
      })
      .filter((seriesItem): seriesItem is ISpectrumHoverSeries => seriesItem !== null)

    if (series.length === 0) {
      setHoverState(null)
      return
    }

    const primarySeries = series[0]
    if (!primarySeries) {
      setHoverState(null)
      return
    }

    const guideX = primarySeries.x
    const tooltipHeight = tooltipHeaderHeight + (series.length * tooltipRowHeight) + 12
    const tooltipX = Math.min(
      width - tooltipWidth - 8,
      Math.max(8, guideX + 12)
    )
    const tooltipY = chartModel.chartPadding.top + tooltipTopOffset

    setHoverState({
      frequencyHz: primarySeries.frequencyHz,
      guideX,
      tooltipX,
      tooltipY,
      tooltipHeight,
      series,
    })
  }

  return {
    hoverState,
    onPointerLeave,
    onPointerMove,
    svgRef,
  }
}

export type { ISpectrumHoverSeries, ISpectrumHoverState }
export { useSpectrumChartHover }
