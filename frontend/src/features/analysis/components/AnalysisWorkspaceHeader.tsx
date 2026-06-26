import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { SpectrumControlsPopover } from './SpectrumControlsPopover'
import { useAnalysisWorkspaceHeader } from '../hooks/useAnalysisWorkspaceHeader'
import { useSpectrumControlsPopover } from '../hooks/useSpectrumControlsPopover'
import type { TAnalysisLayoutMode, TAnalysisSurface, TSignalChartMode } from '../hooks/useTimeWaveformWorkspace'
import type { ISpectrumViewport } from '../utils/spectrumChart'
import './AnalysisWorkspaceHeader.scss'

interface IAnalysisWorkspaceHeaderProps {
  activeSurface: TAnalysisSurface
  layoutMode: TAnalysisLayoutMode
  onSpectrumPresetChange: (preset: string) => void
  onSpectrumRangeEndChange: (value: string) => void
  onSpectrumRangeReset: () => void
  onSpectrumRangeStartChange: (value: string) => void
  onLayoutModeChange: (mode: TAnalysisLayoutMode) => void
  onSignalChartModeChange: (mode: TSignalChartMode) => void
  onSurfaceChange: (surface: TAnalysisSurface) => void
  selectedSignalCount: number
  selectedSpectrumPreset: string
  signalChartMode: TSignalChartMode
  showSpectrumPanel: boolean
  spectrumFftSizeOptions: string[]
  spectrumMaximumHz: number
  spectrumRangeEndHz: number
  spectrumRangeStartHz: number
  spectrumViewport: ISpectrumViewport | null
}

const AnalysisWorkspaceHeader = ({
  activeSurface,
  layoutMode,
  onLayoutModeChange,
  onSignalChartModeChange,
  onSpectrumPresetChange,
  onSpectrumRangeEndChange,
  onSpectrumRangeReset,
  onSpectrumRangeStartChange,
  onSurfaceChange,
  selectedSignalCount,
  selectedSpectrumPreset,
  signalChartMode,
  showSpectrumPanel,
  spectrumFftSizeOptions,
  spectrumMaximumHz,
  spectrumRangeEndHz,
  spectrumRangeStartHz,
  spectrumViewport,
}: IAnalysisWorkspaceHeaderProps) => {
  const {
    activeEyebrow,
    activeTitle,
    isSpectrumRangeFiltered,
    spectrumRangeLabel,
  } = useAnalysisWorkspaceHeader({
    activeSurface,
    layoutMode,
    spectrumMaximumHz,
    spectrumRangeEndHz,
    spectrumRangeStartHz,
    spectrumViewport,
  })
  const {
    isOpen,
    open,
    popoverRef,
    toggle,
  } = useSpectrumControlsPopover()

  return (
    <>
      <header className="time-waveform-workspace__header">
        <div>
          <p className="time-waveform-workspace__eyebrow">{activeEyebrow}</p>
          <h2 className="time-waveform-workspace__title">{activeTitle}</h2>
        </div>
      </header>

      <div className="time-waveform-workspace__surface-bar">
        <div className="time-waveform-workspace__surface-nav">
          <Tabs
            className="time-waveform-workspace__surface-tabs"
            onValueChange={(value) => onSurfaceChange(value as TAnalysisSurface)}
            value={activeSurface}
          >
            <TabsList>
              <TabsTrigger value="waveform">Waveform</TabsTrigger>
              <TabsTrigger value="spectrum">Spectrum</TabsTrigger>
            </TabsList>
          </Tabs>

          <Tabs
            className="time-waveform-workspace__layout-tabs"
            onValueChange={(value) => onLayoutModeChange(value as TAnalysisLayoutMode)}
            value={layoutMode}
          >
            <TabsList>
              <TabsTrigger value="focused">Focused</TabsTrigger>
              <TabsTrigger value="compare">Compare</TabsTrigger>
            </TabsList>
          </Tabs>

          {layoutMode === 'focused' && selectedSignalCount > 1 && (
            <Tabs
              className="time-waveform-workspace__layout-tabs"
              onValueChange={(value) => onSignalChartModeChange(value as TSignalChartMode)}
              value={signalChartMode}
            >
              <TabsList>
                <TabsTrigger value="overlay">Overlay</TabsTrigger>
                <TabsTrigger value="split">Split</TabsTrigger>
              </TabsList>
            </Tabs>
          )}
        </div>

        {showSpectrumPanel && spectrumViewport && (
          <SpectrumControlsPopover
            isOpen={isOpen}
            isRangeFiltered={isSpectrumRangeFiltered}
            onOpenRangeBadge={open}
            onSpectrumPresetChange={onSpectrumPresetChange}
            onSpectrumRangeEndChange={onSpectrumRangeEndChange}
            onSpectrumRangeReset={onSpectrumRangeReset}
            onSpectrumRangeStartChange={onSpectrumRangeStartChange}
            onToggle={toggle}
            popoverRef={popoverRef}
            rangeLabel={spectrumRangeLabel}
            selectedSpectrumPreset={selectedSpectrumPreset}
            spectrumFftSizeOptions={spectrumFftSizeOptions}
            spectrumMaximumHz={spectrumMaximumHz}
            spectrumRangeEndHz={spectrumRangeEndHz}
            spectrumRangeStartHz={spectrumRangeStartHz}
          />
        )}
      </div>
    </>
  )
}

export { AnalysisWorkspaceHeader }
