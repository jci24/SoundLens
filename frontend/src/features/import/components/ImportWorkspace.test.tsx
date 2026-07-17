import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { useImportFiles } from '../hooks/useImportFiles'
import { ImportWorkspace } from './ImportWorkspace'

vi.mock('../hooks/useImportFiles', () => ({
  useImportFiles: vi.fn(),
}))

describe('ImportWorkspace', () => {
  it('keeps an import failure visible in the dedicated workflow', () => {
    vi.mocked(useImportFiles).mockReturnValue({
      handleImportPaths: vi.fn(),
      handleUploadFiles: vi.fn(),
      importError: 'candidate.wav could not be imported.',
      isImporting: false,
    })

    render(<ImportWorkspace onImportedFiles={vi.fn()} />)

    expect(screen.getByRole('alert')).toHaveTextContent('candidate.wav could not be imported.')
  })
})
