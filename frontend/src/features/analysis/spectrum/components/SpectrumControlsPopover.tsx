import type { RefObject } from 'react'
import { SlidersHorizontal } from 'lucide-react'
import { SpectrumControlsPanel } from './SpectrumControlsPanel'

interface ISpectrumControlsPopoverProps {
  isOpen: boolean
  isRangeFiltered: boolean
  onOpenRangeBadge: () => void
  onSpectrumPresetChange: (preset: string) => void
  onSpectrumRangeEndChange: (value: string) => void
  onSpectrumRangeReset: () => void
  onSpectrumRangeStartChange: (value: string) => void
  onToggle: () => void
  popoverRef: RefObject<HTMLDivElement | null>
  rangeLabel: string
  selectedSpectrumPreset: string
  spectrumFftSizeOptions: string[]
  spectrumMaximumHz: number
  spectrumRangeEndHz: number
  spectrumRangeStartHz: number
  triggerRef: RefObject<HTMLButtonElement | null>
}

const SpectrumControlsPopover = ({
  isOpen,
  isRangeFiltered,
  onOpenRangeBadge,
  onSpectrumPresetChange,
  onSpectrumRangeEndChange,
  onSpectrumRangeReset,
  onSpectrumRangeStartChange,
  onToggle,
  popoverRef,
  rangeLabel,
  selectedSpectrumPreset,
  spectrumFftSizeOptions,
  spectrumMaximumHz,
  spectrumRangeEndHz,
  spectrumRangeStartHz,
  triggerRef,
}: ISpectrumControlsPopoverProps) => (
  <div className="time-waveform-workspace__surface-controls">
    {isRangeFiltered && (
      <button
        className="time-waveform-workspace__range-badge"
        type="button"
        onClick={onOpenRangeBadge}
      >
        {rangeLabel}
      </button>
    )}

    <div className="time-waveform-workspace__controls-popover" ref={popoverRef}>
      <button
        aria-expanded={isOpen}
        aria-haspopup="dialog"
        aria-label="Spectrum settings"
        className={`time-waveform-workspace__controls-trigger${isOpen ? ' time-waveform-workspace__controls-trigger--open' : ''}`}
        ref={triggerRef}
        type="button"
        onClick={onToggle}
      >
        <SlidersHorizontal size={15} />
      </button>

      <div
        aria-label="Spectrum settings"
        className={`time-waveform-workspace__controls-panel${isOpen ? ' time-waveform-workspace__controls-panel--open' : ''}`}
        hidden={!isOpen}
        role="dialog"
      >
        <SpectrumControlsPanel
          onSpectrumPresetChange={onSpectrumPresetChange}
          onSpectrumRangeEndChange={onSpectrumRangeEndChange}
          onSpectrumRangeReset={onSpectrumRangeReset}
          onSpectrumRangeStartChange={onSpectrumRangeStartChange}
          selectedSpectrumPreset={selectedSpectrumPreset}
          spectrumFftSizeOptions={spectrumFftSizeOptions}
          spectrumMaximumHz={spectrumMaximumHz}
          spectrumRangeEndHz={spectrumRangeEndHz}
          spectrumRangeStartHz={spectrumRangeStartHz}
        />
      </div>
    </div>
  </div>
)

export { SpectrumControlsPopover }
