import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { CopilotInput } from './CopilotInput'

const renderInput = (onSubmit = vi.fn()) => {
  render(
    <CopilotInput
      isLoading={false}
      recordings={[]}
      showSuggestions={false}
      onSubmit={onSubmit}
    />
  )
  return onSubmit
}

describe('CopilotInput', () => {
  it('submits a question without exposing an internal routing control', () => {
    const onSubmit = renderInput()
    fireEvent.change(screen.getByRole('textbox', { name: 'Investigation question' }), {
      target: { value: 'What is the Nyquist theorem?' },
    })

    expect(screen.queryByRole('combobox', { name: 'Context' })).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Investigate' }))

    expect(onSubmit).toHaveBeenCalledWith('What is the Nyquist theorem?')
  })

  it('disables question submission while a request is running', () => {
    render(
      <CopilotInput
        isLoading
        recordings={[]}
        showSuggestions={false}
        onSubmit={vi.fn()}
      />
    )

    expect(screen.getByRole('textbox', { name: 'Investigation question' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Investigate' })).toBeDisabled()
  })
})
