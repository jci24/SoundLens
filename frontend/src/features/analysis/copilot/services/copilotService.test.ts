import { afterEach, describe, expect, it, vi } from 'vitest'
import { readAgentStream, streamAgentQuery } from './copilotService'

const encodeChunks = (...chunks: string[]) => new ReadableStream<Uint8Array>({
  start(controller) {
    chunks.forEach((chunk) => controller.enqueue(new TextEncoder().encode(chunk)))
    controller.close()
  },
})

describe('streamAgentQuery', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('parses fragmented activity updates and one final result', async () => {
    const onActivity = vi.fn()
    const stream = encodeChunks(
      'event: agent-activity\ndata: {"eventType":"activity","activity":{"sequence":1,"kind":"routing",',
      '"status":"running","title":"Selecting","summary":"Checking."}}\n\n',
      'data: {"eventType":"activity","activity":{"sequence":1,"kind":"routing","status":"completed","title":"Selecting","summary":"Selected."}}\n\n',
      'data: {"eventType":"result","response":{"answer":"Ready","citedEvidence":[],"limitations":[],"nextSteps":[],"toolsUsed":[]}}\n\n'
    )

    await expect(readAgentStream(stream, onActivity)).resolves.toMatchObject({ answer: 'Ready' })
    expect(onActivity).toHaveBeenCalledTimes(2)
    expect(onActivity).toHaveBeenLastCalledWith(expect.objectContaining({ sequence: 1, status: 'completed' }))
  })

  it('rejects malformed events and streams without a final result', async () => {
    await expect(readAgentStream(encodeChunks('data: not-json\n\n'), vi.fn())).rejects.toThrow('malformed')
    await expect(readAgentStream(encodeChunks(
      'data: {"eventType":"activity","activity":{"sequence":1,"kind":"routing","status":"completed","title":"Done","summary":"Done."}}\n\n'
    ), vi.fn())).rejects.toThrow('ended before a validated response')
  })

  it('surfaces FastEndpoints general errors before reading the stream', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
      errors: { generalErrors: ['Import at least one audio file before requesting a comparison contract.'] },
    }), { status: 400 })))

    await expect(streamAgentQuery({ question: 'Compare these signals.' }, vi.fn())).rejects.toThrow(
      'Import at least one audio file before requesting a comparison contract.'
    )
  })
})
