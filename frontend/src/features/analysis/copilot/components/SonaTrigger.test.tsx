import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { SonaTrigger } from './SonaTrigger'

describe('SonaTrigger', () => {
  it('renders an icon-only accessible trigger with a named tooltip', () => {
    const onClick = vi.fn()
    const { container } = render(<SonaTrigger isOpen={false} onClick={onClick} />)

    const trigger = screen.getByRole('button', { name: 'Open Sona' })
    const tooltip = screen.getByRole('tooltip', { name: 'Sona' })

    expect(trigger).toHaveAttribute('aria-expanded', 'false')
    expect(trigger).toHaveAttribute('aria-describedby', tooltip.id)
    expect(container.querySelector('svg[viewBox="0 0 44 24"]')).toBeInTheDocument()
    expect(screen.queryByText('Copilot')).not.toBeInTheDocument()

    fireEvent.click(trigger)
    expect(onClick).toHaveBeenCalledOnce()
  })

  it('announces the close action when Sona is open', () => {
    render(<SonaTrigger isOpen />)

    expect(screen.getByRole('button', { name: 'Close Sona' })).toHaveAttribute('aria-expanded', 'true')
  })
})
