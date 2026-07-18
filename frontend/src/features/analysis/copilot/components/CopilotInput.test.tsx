import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { CopilotInput } from './CopilotInput'

const renderInput = (onSubmit = vi.fn()) => {
  render(
    <CopilotInput
      isLoading={false}
      recordings={[]}
      showSuggestions={false}
      workspaceContextLabel="Selected comparison attached"
      onSubmit={onSubmit}
    />
  )
  return onSubmit
}

describe('CopilotInput', () => {
  it('defaults to Auto and exposes the attached workspace context', () => {
    renderInput()

    expect(screen.getByRole('combobox', { name: 'Context' })).toHaveValue('auto')
    expect(screen.getByText('Selected comparison attached')).toBeInTheDocument()
  })

  it('submits the selected context mode and explains General isolation', () => {
    const onSubmit = renderInput()
    fireEvent.change(screen.getByRole('combobox', { name: 'Context' }), {
      target: { value: 'general' },
    })
    fireEvent.change(screen.getByRole('textbox', { name: 'Investigation question' }), {
      target: { value: 'What is the Nyquist theorem?' },
    })

    expect(screen.getByText('Workspace context ignored')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Investigate' }))

    expect(onSubmit).toHaveBeenCalledWith('What is the Nyquist theorem?', 'general')
  })

  it('disables context changes while a request is running', () => {
    render(
      <CopilotInput
        isLoading
        recordings={[]}
        showSuggestions={false}
        workspaceContextLabel="No workspace evidence attached"
        onSubmit={vi.fn()}
      />
    )

    expect(screen.getByRole('combobox', { name: 'Context' })).toBeDisabled()
  })
})
