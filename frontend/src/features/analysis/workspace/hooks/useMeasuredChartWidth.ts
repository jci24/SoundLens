import { useEffect, useState, type RefObject } from 'react'

const useMeasuredChartWidth = (chartRef: RefObject<HTMLDivElement | null>) => {
  const [chartWidth, setChartWidth] = useState(0)

  useEffect(() => {
    if (!chartRef.current) return

    const observer = new ResizeObserver(([entry]) => {
      setChartWidth(Math.floor(entry.contentRect.width))
    })

    observer.observe(chartRef.current)

    return () => observer.disconnect()
  }, [chartRef])

  return chartWidth
}

export { useMeasuredChartWidth }
