import { useId, type ComponentProps } from 'react'
import { Button } from '@/components/ui/button'
import { SonaIcon } from './SonaIcon'
import './SonaTrigger.scss'

interface ISonaTriggerProps extends Omit<
  ComponentProps<typeof Button>,
  'aria-describedby' | 'aria-expanded' | 'aria-label' | 'children'
> {
  isOpen: boolean
}

const SonaTrigger = ({
  isOpen,
  size = 'icon-sm',
  type = 'button',
  variant = 'ghost',
  ...buttonProps
}: ISonaTriggerProps) => {
  const tooltipId = useId()

  return (
    <span className="sona-trigger">
      <Button
        {...buttonProps}
        aria-describedby={tooltipId}
        aria-expanded={isOpen}
        aria-label={isOpen ? 'Close Sona' : 'Open Sona'}
        size={size}
        type={type}
        variant={variant}
      >
        <SonaIcon className="sona-trigger__icon" />
      </Button>
      <span className="sona-trigger__tooltip" id={tooltipId} role="tooltip">
        Sona
      </span>
    </span>
  )
}

export { SonaTrigger }
