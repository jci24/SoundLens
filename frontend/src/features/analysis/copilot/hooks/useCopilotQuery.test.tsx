import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import { useCopilotQuery } from './useCopilotQuery'

const mockPostAgentQuery = vi.fn()

vi.mock('../services/copilotService', () => ({
  postAgentQuery: (...args: unknown[]) => mockPostAgentQuery(...args),
}))

describe('useCopilotQuery', () => {
  beforeEach(() => {
    mockPostAgentQuery.mockReset()
  })

  it('preserves earlier turns when a new question is submitted', async () => {
    mockPostAgentQuery
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
      await result.current.submit({ question: 'First question' })
    })

    await act(async () => {
      await result.current.submit({ question: 'Second question' })
    })

    await waitFor(() => {
      expect(result.current.turns).toHaveLength(2)
      expect(result.current.turns[0]?.question).toBe('First question')
      expect(result.current.turns[0]?.response?.answer).toBe('First answer')
      expect(result.current.turns[1]?.question).toBe('Second question')
      expect(result.current.turns[1]?.response?.answer).toBe('Second answer')
    })
  })

  it('re-runs an existing turn in place instead of clearing the conversation', async () => {
    mockPostAgentQuery
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
      await result.current.submit({ question: 'Why is this sharp?' })
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
  })
})
