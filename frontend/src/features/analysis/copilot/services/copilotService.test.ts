import { afterEach, describe, expect, it, vi } from 'vitest'
import { postAgentQuery } from './copilotService'

describe('postAgentQuery', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('surfaces FastEndpoints general errors instead of replacing them with a generic failure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
      statusCode: 400,
      message: 'One or more errors occurred!',
      errors: {
        generalErrors: ['Import at least one audio file before requesting a comparison contract.'],
      },
    }), {
      status: 400,
      headers: { 'Content-Type': 'application/json' },
    })))

    await expect(postAgentQuery({ question: 'Compare these signals.' })).rejects.toThrow(
      'Import at least one audio file before requesting a comparison contract.'
    )
  })

  it('retains a plain-text backend failure when JSON is unavailable', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('Backend session expired.', { status: 409 })))

    await expect(postAgentQuery({ question: 'Compare these signals.' })).rejects.toThrow(
      'Backend session expired.'
    )
  })
})
