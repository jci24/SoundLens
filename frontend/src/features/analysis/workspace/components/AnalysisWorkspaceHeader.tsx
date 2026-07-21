import { FileAudio, FileText } from 'lucide-react'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import { SonaTrigger } from '../../copilot/components/SonaTrigger'
import { SpectrumControlsPopover } from '../../spectrum/components/SpectrumControlsPopover'
import { useAnalysisWorkspaceHeader } from '../hooks/useAnalysisWorkspaceHeader'
import { useSpectrumControlsPopover } from '../../spectrum/hooks/useSpectrumControlsPopover'
import type { TAnalysisLayoutMode, TAnalysisSurface, TSignalChartMode } from '../../types'
import type { ISpectrumViewport } from '../../spectrum/utils/spectrumChart'
import './AnalysisWorkspaceHeader.scss'

interface IAnalysisWorkspaceHeaderProps {
  activeSurface: TAnalysisSurface
  canEnterCompareMode: boolean
  canExportReport: boolean
  enabledAnalysisSurfaces: readonly TAnalysisSurface[]
  isCopilotOpen: boolean
  isExporting: boolean
  layoutMode: TAnalysisLayoutMode
  onCopilotToggle: () => void
  onExportReport: () => void
  onRecordingsOpen?: () => void
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
  canEnterCompareMode,
  canExportReport,
  enabledAnalysisSurfaces,
  isCopilotOpen,
  isExporting,
  layoutMode,
  onCopilotToggle,
  onExportReport,
  onRecordingsOpen,
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
    activeTitle,
    isSpectrumRangeFiltered,
    spectrumRangeLabel,
  } = useAnalysisWorkspaceHeader({
    activeSurface,
    enabledAnalysisSurfaces,
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
    triggerRef,
    toggle,
  } = useSpectrumControlsPopover()

  return (
    <header className="time-waveform-workspace__toolbar">
      <h2 className="time-waveform-workspace__visually-hidden">{activeTitle}</h2>
      <div className="time-waveform-workspace__view-controls-nav">
          {enabledAnalysisSurfaces.length > 1 && (
            <>
              <Tabs
                className="time-waveform-workspace__surface-tabs"
                onValueChange={(value) => onSurfaceChange(value as TAnalysisSurface)}
                value={activeSurface}
              >
                <TabsList>
                  {enabledAnalysisSurfaces.includes('waveform') && (
                    <TabsTrigger value="waveform">Waveform</TabsTrigger>
                  )}
                  {enabledAnalysisSurfaces.includes('spectrum') && (
                    <TabsTrigger value="spectrum">Spectrum</TabsTrigger>
                  )}
                </TabsList>
              </Tabs>

              <span aria-hidden="true" className="time-waveform-workspace__toolbar-divider" />
            </>
          )}

          <Tabs
            className="time-waveform-workspace__layout-tabs"
            onValueChange={(value) => onLayoutModeChange(value as TAnalysisLayoutMode)}
            value={layoutMode}
          >
            <TabsList>
              <TabsTrigger value="focused">Focused</TabsTrigger>
              <TabsTrigger disabled={!canEnterCompareMode} value="compare">
                Compare
              </TabsTrigger>
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

      <div className="time-waveform-workspace__toolbar-actions">
        <Button
          aria-label="Open recordings drawer"
          className="time-waveform-workspace__recordings-toggle"
          onClick={onRecordingsOpen}
          type="button"
          variant="ghost"
        >
          <FileAudio aria-hidden="true" size={14} />
          Recordings
        </Button>
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
            triggerRef={triggerRef}
          />
        )}
        <span aria-hidden="true" className="time-waveform-workspace__toolbar-divider" />
        <Button
          aria-label={isExporting ? 'Preparing export...' : 'Export report'}
          className="time-waveform-workspace__export-button"
          disabled={!canExportReport || isExporting}
          onClick={onExportReport}
          type="button"
          variant="ghost"
        >
          <FileText aria-hidden="true" size={14} />
          {isExporting ? 'Preparing...' : 'Report'}
        </Button>
        <SonaTrigger
          className={`time-waveform-workspace__copilot-toggle${isCopilotOpen ? ' time-waveform-workspace__copilot-toggle--active' : ''}`}
          isOpen={isCopilotOpen}
          variant={isCopilotOpen ? 'secondary' : 'ghost'}
          onClick={onCopilotToggle}
        />
      </div>
    </header>
  )
}

export { AnalysisWorkspaceHeader }
