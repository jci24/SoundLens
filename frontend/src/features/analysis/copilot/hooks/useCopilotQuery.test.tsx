import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import { useCopilotQuery } from './useCopilotQuery'

const mockStreamAgentQuery = vi.fn()

vi.mock('../services/copilotService', () => ({
  streamAgentQuery: (...args: unknown[]) => mockStreamAgentQuery(...args),
}))

describe('useCopilotQuery', () => {
  beforeEach(() => {
    mockStreamAgentQuery.mockReset()
  })

  it('preserves earlier turns when a new question is submitted', async () => {
    mockStreamAgentQuery
      .mockResolvedValueOnce({
        answer: 'First answer',
        citedEvidence: [],
        limitations: [],
        nextSteps: ['First next step'],
        toolsUsed: ['get_signal_metrics'],
      })
      .mockResolvedValueOnce({
        answer: 'Second answer',
        citedEvidence: [],
        limitations: [],
        nextSteps: ['Second next step'],
        toolsUsed: ['compare_signals'],
      })

    const { result } = renderHook(() => useCopilotQuery())

    await act(async () => {
      await result.current.submit({
        question: 'First question',
        routeContext: { route: 'configure' },
      })
    })

    await act(async () => {
      await result.current.submit({
        question: 'Second question',
        routeContext: { route: 'evidence' },
      })
    })

    await waitFor(() => {
      expect(result.current.turns).toHaveLength(2)
      expect(result.current.turns[0]?.question).toBe('First question')
      expect(result.current.turns[0]?.response?.answer).toBe('First answer')
      expect(result.current.turns[1]?.question).toBe('Second question')
      expect(result.current.turns[1]?.response?.answer).toBe('Second answer')
    })
    expect(mockStreamAgentQuery.mock.calls[1]?.[0]).toMatchObject({
      question: 'Second question',
      conversationHistory: [
        expect.objectContaining({
          question: 'First question',
          answer: 'First answer',
          answerMode: 'workspace',
          requestSnapshot: expect.objectContaining({
            routeContext: { route: 'configure' },
          }),
        }),
      ],
    })
  })

  it('re-runs an existing turn in place instead of clearing the conversation', async () => {
    mockStreamAgentQuery
      .mockResolvedValueOnce({
        answer: 'Initial answer',
        citedEvidence: [],
        limitations: [],
        nextSteps: [],
        toolsUsed: ['get_signal_metrics'],
      })
      .mockResolvedValueOnce({
        answer: 'Updated answer',
        citedEvidence: [],
        limitations: [],
        nextSteps: ['Try a narrower ROI'],
        toolsUsed: ['get_spectrum_summary'],
      })

    const { result } = renderHook(() => useCopilotQuery())

    await act(async () => {
      await result.current.submit({
        question: 'Why is this sharp?',
        contextMode: 'workspace',
        signalIds: ['signal-1'],
      })
    })

    const firstTurnId = result.current.turns[0]?.id
    expect(firstTurnId).toBeTruthy()

    await act(async () => {
      await result.current.retry(firstTurnId!)
    })

    await waitFor(() => {
      expect(result.current.turns).toHaveLength(1)
      expect(result.current.turns[0]?.response?.answer).toBe('Updated answer')
      expect(result.current.turns[0]?.response?.nextSteps).toEqual(['Try a narrower ROI'])
    })
    expect(mockStreamAgentQuery.mock.calls.at(-1)?.[0]).toEqual({
      question: 'Why is this sharp?',
      contextMode: 'workspace',
      signalIds: ['signal-1'],
    })
  })

  it('upserts streamed activity and resets it when re-running the original request', async () => {
    mockStreamAgentQuery
      .mockImplementationOnce(async (_request, onActivity) => {
        onActivity({ sequence: 1, kind: 'routing', status: 'running', title: 'Selecting', summary: 'Checking.' })
        onActivity({ sequence: 1, kind: 'routing', status: 'completed', title: 'Selecting', summary: 'Selected.' })
        return { answer: 'Answer', citedEvidence: [], limitations: [], nextSteps: [], toolsUsed: [], activityTrace: [] }
      })
      .mockImplementationOnce(async () => ({
        answer: 'Updated', citedEvidence: [], limitations: [], nextSteps: [], toolsUsed: [], activityTrace: [],
      }))

    const { result } = renderHook(() => useCopilotQuery())
    await act(async () => result.current.submit({ question: 'Investigate this.' }))

    expect(result.current.turns[0]?.activity).toEqual([
      expect.objectContaining({ sequence: 1, status: 'completed', summary: 'Selected.' }),
    ])

    await act(async () => result.current.retry(result.current.turns[0]!.id))
    expect(result.current.turns[0]?.activity).toEqual([])
    expect(mockStreamAgentQuery.mock.calls[1]?.[0]).toEqual({ question: 'Investigate this.' })
  })

  it('aborts the active stream when the hook unmounts', async () => {
    let capturedSignal: AbortSignal | undefined
    mockStreamAgentQuery.mockImplementation((_request, _onActivity, signal) => {
      capturedSignal = signal
      return new Promise(() => undefined)
    })

    const { result, unmount } = renderHook(() => useCopilotQuery())
    act(() => {
      void result.current.submit({ question: 'Research this.' })
    })

    await waitFor(() => expect(capturedSignal).toBeDefined())
    expect(capturedSignal?.aborted).toBe(false)
    unmount()
    expect(capturedSignal?.aborted).toBe(true)
  })

  it('excludes failed turns from history and allows the next submission to recover', async () => {
    mockStreamAgentQuery
      .mockRejectedValueOnce(new Error('Failed'))
      .mockResolvedValueOnce({
        answer: 'Recovered', citedEvidence: [], limitations: [], nextSteps: [], toolsUsed: [], answerMode: 'general',
      })

    const { result } = renderHook(() => useCopilotQuery())
    await act(async () => result.current.submit({ question: 'Failed question' }))
    await act(async () => result.current.submit({ question: 'Recovery question' }))

    expect(mockStreamAgentQuery.mock.calls[1]?.[0]).toEqual({ question: 'Recovery question' })
    expect(result.current.turns).toHaveLength(2)
    expect(result.current.turns[1]?.response?.answer).toBe('Recovered')
  })

  it('re-running an earlier turn truncates every later dependent turn', async () => {
    mockStreamAgentQuery
      .mockResolvedValueOnce({ answer: 'First', citedEvidence: [], limitations: [], nextSteps: [], toolsUsed: [] })
      .mockResolvedValueOnce({ answer: 'Second', citedEvidence: [], limitations: [], nextSteps: [], toolsUsed: [] })
      .mockResolvedValueOnce({ answer: 'First revised', citedEvidence: [], limitations: [], nextSteps: [], toolsUsed: [] })

    const { result } = renderHook(() => useCopilotQuery())
    await act(async () => result.current.submit({ question: 'First question', signalIds: ['signal-a'] }))
    await act(async () => result.current.submit({ question: 'Second question', signalIds: ['signal-b'] }))
    const firstTurnId = result.current.turns[0]!.id

    await act(async () => result.current.retry(firstTurnId))

    expect(result.current.turns).toHaveLength(1)
    expect(result.current.turns[0]?.response?.answer).toBe('First revised')
    expect(mockStreamAgentQuery.mock.calls[2]?.[0]).toEqual({
      question: 'First question',
      signalIds: ['signal-a'],
    })
  })

  it('sends only the six most recent successful completed turns', async () => {
    mockStreamAgentQuery.mockImplementation(async (request: { question: string }) => ({
      answer: `Answer to ${request.question}`,
      answerMode: 'general',
      citedEvidence: [],
      limitations: [],
      nextSteps: [],
      toolsUsed: [],
    }))
    const { result } = renderHook(() => useCopilotQuery())

    for (let index = 0; index < 8; index += 1) {
      await act(async () => result.current.submit({ question: `Question ${index}` }))
    }

    const finalHistory = mockStreamAgentQuery.mock.calls[7]?.[0].conversationHistory
    expect(finalHistory).toHaveLength(6)
    expect(finalHistory.map((turn: { question: string }) => turn.question)).toEqual([
      'Question 1',
      'Question 2',
      'Question 3',
      'Question 4',
      'Question 5',
      'Question 6',
    ])
  })
})
