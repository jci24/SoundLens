import {
  Combobox,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxInput,
  ComboboxItem,
  ComboboxList,
} from '@/components/ui/combobox'
import './SpectrumControlsPanel.scss'

interface ISpectrumControlsPanelProps {
  onSpectrumPresetChange: (preset: string) => void
  onSpectrumRangeEndChange: (value: string) => void
  onSpectrumRangeReset: () => void
  onSpectrumRangeStartChange: (value: string) => void
  selectedSpectrumPreset: string
  spectrumFftSizeOptions: string[]
  spectrumMaximumHz: number
  spectrumRangeEndHz: number
  spectrumRangeStartHz: number
}

const SpectrumControlsPanel = ({
  onSpectrumPresetChange,
  onSpectrumRangeEndChange,
  onSpectrumRangeReset,
  onSpectrumRangeStartChange,
  selectedSpectrumPreset,
  spectrumFftSizeOptions,
  spectrumMaximumHz,
  spectrumRangeEndHz,
  spectrumRangeStartHz,
}: ISpectrumControlsPanelProps) => (
  <div className="spectrum-controls-panel">
    <div className="time-waveform-workspace__controls-group">
      <span className="time-waveform-workspace__controls-caption">FFT</span>
      <Combobox
        items={spectrumFftSizeOptions}
        onValueChange={onSpectrumPresetChange}
        value={selectedSpectrumPreset}
      >
        <ComboboxInput
          className="time-waveform-workspace__parameter-combobox"
          placeholder="FFT size"
        />
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
      <div aria-label="Spectrum range" className="time-waveform-workspace__zoom-panel">
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
)

export { SpectrumControlsPanel }
