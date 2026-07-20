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

  it('accepts a valid sufficiency contract and rejects malformed statuses', async () => {
    const validResponse = {
      answer: 'Bounded answer',
      citedEvidence: [],
      limitations: [],
      nextSteps: [],
      toolsUsed: [],
      evidenceSufficiency: {
        intent: 'digital_level_difference',
        status: 'partial',
        label: 'Partial evidence',
        reason: 'Coverage is limited.',
        requiredEvidence: ['Aligned observations'],
        availableEvidence: ['Selected pair'],
        limitationCodes: ['LowCoverage'],
      },
    }
    const validStream = encodeChunks(`data: ${JSON.stringify({ eventType: 'result', response: validResponse })}\n\n`)
    await expect(readAgentStream(validStream, vi.fn())).resolves.toMatchObject({
      evidenceSufficiency: { status: 'partial' },
    })

    const malformedStream = encodeChunks(`data: ${JSON.stringify({
      eventType: 'result',
      response: {
        ...validResponse,
        evidenceSufficiency: { ...validResponse.evidenceSufficiency, status: 'confident' },
      },
    })}\n\n`)
    await expect(readAgentStream(malformedStream, vi.fn())).rejects.toThrow('invalid event')
  })

  it('accepts a valid preview plan and rejects executable or forward-dependent plans', async () => {
    const plan = {
      planId: 'plan_v1_1234567890abcdef12345678',
      version: '1',
      status: 'preview',
      objective: 'Inspect complementary evidence.',
      scope: { kind: 'full_duration', startTimeSeconds: null, endTimeSeconds: null },
      steps: [{
        stepId: 'step-1',
        order: 1,
        title: 'Inspect waveform evidence',
        purpose: 'Review event shape and timing.',
        capabilityId: 'waveform',
        capabilityLabel: 'Waveform inspection',
        category: 'analysis',
        dependsOnStepIds: [],
        parameterKeys: ['scope', 'signals'],
        requiredEvidence: ['imported_recordings'],
        completionCriteria: ['Waveform evidence is available for review.'],
        costClass: 'interactive',
        requiresApproval: false,
      }],
    }
    const response = {
      answer: 'Plan ready.',
      citedEvidence: [],
      limitations: [],
      nextSteps: [],
      toolsUsed: [],
      investigationPlan: plan,
    }
    await expect(readAgentStream(encodeChunks(
      `data: ${JSON.stringify({ eventType: 'result', response })}\n\n`,
    ), vi.fn())).resolves.toMatchObject({ investigationPlan: { status: 'preview' } })

    const invalid = { ...plan, status: 'executable' }
    await expect(readAgentStream(encodeChunks(
      `data: ${JSON.stringify({ eventType: 'result', response: { ...response, investigationPlan: invalid } })}\n\n`,
    ), vi.fn())).rejects.toThrow('invalid event')
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
