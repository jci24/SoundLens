import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { SpectrumControlsPopover } from './SpectrumControlsPopover'
import { useAnalysisWorkspaceHeader } from '../hooks/useAnalysisWorkspaceHeader'
import { useSpectrumControlsPopover } from '../hooks/useSpectrumControlsPopover'
import type { TAnalysisSurface } from '../hooks/useTimeWaveformWorkspace'
import type { ISpectrumViewport } from '../utils/spectrumChart'
import './AnalysisWorkspaceHeader.scss'

interface IAnalysisWorkspaceHeaderProps {
  activeSurface: TAnalysisSurface
  onSpectrumPresetChange: (preset: string) => void
  onSpectrumRangeEndChange: (value: string) => void
  onSpectrumRangeReset: () => void
  onSpectrumRangeStartChange: (value: string) => void
  onSurfaceChange: (surface: TAnalysisSurface) => void
  selectedSpectrumPreset: string
  spectrumFftSizeOptions: string[]
  spectrumMaximumHz: number
  spectrumRangeEndHz: number
  spectrumRangeStartHz: number
  spectrumViewport: ISpectrumViewport | null
}

const AnalysisWorkspaceHeader = ({
  activeSurface,
  onSpectrumPresetChange,
  onSpectrumRangeEndChange,
  onSpectrumRangeReset,
  onSpectrumRangeStartChange,
  onSurfaceChange,
  selectedSpectrumPreset,
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

        {activeSurface === 'spectrum' && spectrumViewport && (
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
