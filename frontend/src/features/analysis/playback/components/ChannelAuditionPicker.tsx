import { useState } from 'react'
import * as Popover from '@radix-ui/react-popover'
import { Check, ChevronDown, HeadphoneOff } from 'lucide-react'
import type { IChannelAuditionOption } from '../hooks/useChannelAuditionRouting'
import type { TChannelAuditionRoute } from '../types/channelAudition'
import './ChannelAuditionPicker.scss'

interface IChannelAuditionPickerProps {
  error: string | null
  isSupported: boolean
  onSelect: (route: TChannelAuditionRoute) => Promise<void>
  options: IChannelAuditionOption[]
  route: TChannelAuditionRoute
}

const ChannelAuditionPicker = ({
  error,
  isSupported,
  onSelect,
  options,
  route,
}: IChannelAuditionPickerProps) => {
  const [isOpen, setIsOpen] = useState(false)
  const activeLabel = route.mode === 'isolated'
    ? options.find((option) => option.channelIndex === route.channelIndex)?.label ?? 'Original'
    : 'Original'

  if (options.length === 0) {
    return null
  }

  const handleSelect = (nextRoute: TChannelAuditionRoute) => {
    setIsOpen(false)
    void onSelect(nextRoute)
  }

  return (
    <Popover.Root open={isOpen} onOpenChange={setIsOpen}>
      <Popover.Trigger asChild>
        <button
          aria-label="Choose playback channel"
          className="channel-audition-picker__trigger"
          disabled={!isSupported}
          title={!isSupported ? error ?? 'Channel routing unavailable for this recording.' : undefined}
          type="button"
        >
          {!isSupported && <HeadphoneOff aria-hidden="true" size={13} />}
          <span>{isSupported ? activeLabel : 'Original'}</span>
          <ChevronDown aria-hidden="true" size={13} />
        </button>
      </Popover.Trigger>

      <Popover.Portal>
        <Popover.Content
          align="start"
          aria-label="Select playback channel"
          className="channel-audition-picker__popover"
          collisionPadding={12}
          role="dialog"
          sideOffset={7}
        >
          <p className="channel-audition-picker__heading">Audition channel</p>
          <button
            aria-pressed={route.mode === 'original'}
            className="channel-audition-picker__option"
            type="button"
            onClick={() => handleSelect({ mode: 'original' })}
          >
            <span>Original</span>
            {route.mode === 'original' && <Check aria-hidden="true" size={14} />}
          </button>
          {options.map((option) => {
            const isSelected = route.mode === 'isolated'
              && route.channelIndex === option.channelIndex

            return (
              <button
                aria-pressed={isSelected}
                className="channel-audition-picker__option"
                key={option.channelIndex}
                title={option.label}
                type="button"
                onClick={() => handleSelect({
                  channelIndex: option.channelIndex,
                  mode: 'isolated',
                })}
              >
                <span>{option.label}</span>
                {isSelected && <Check aria-hidden="true" size={14} />}
              </button>
            )
          })}
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  )
}

export { ChannelAuditionPicker }
