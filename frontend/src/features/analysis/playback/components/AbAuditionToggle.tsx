import type { ITimeWaveformRecording } from '../../types'
import './AbAuditionToggle.scss'

interface IAbAuditionToggleProps {
  activeSide: 'A' | 'B' | null
  pair: { A: ITimeWaveformRecording; B: ITimeWaveformRecording }
  onSelect: (side: 'A' | 'B') => void
}

const AbAuditionToggle = ({ activeSide, pair, onSelect }: IAbAuditionToggleProps) => (
  <fieldset className="ab-audition-toggle">
    <legend className="sr-only">Position-aligned A/B audition</legend>
    {(['A', 'B'] as const).map((side) => (
      <button
        aria-label={`Audition Compare ${side}: ${pair[side].fileName}`}
        aria-pressed={activeSide === side}
        className="ab-audition-toggle__button"
        key={side}
        title={`Compare ${side}: ${pair[side].fileName}`}
        type="button"
        onClick={() => onSelect(side)}
      >
        {side}
      </button>
    ))}
  </fieldset>
)

export { AbAuditionToggle }
