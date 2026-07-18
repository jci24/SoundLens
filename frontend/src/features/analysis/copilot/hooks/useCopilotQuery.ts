import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { streamAgentQuery } from '../services/copilotService'
import type { IAgentActivityEvent, IAgentQueryRequest, IAgentQueryResponse } from '../types/copilot.types'

interface ICopilotConversationTurn {
  id: string
  question: string
  response: IAgentQueryResponse | null
  error: string | null
  isLoading: boolean
  activity: IAgentActivityEvent[]
}

interface ICopilotConversationTurnState extends ICopilotConversationTurn {
  request: IAgentQueryRequest
}

interface IUseCopilotQueryResult {
  turns: ICopilotConversationTurn[]
  isLoading: boolean
  submit: (request: IAgentQueryRequest) => Promise<void>
  retry: (turnId: string) => Promise<void>
  reset: () => void
}

const useCopilotQuery = (): IUseCopilotQueryResult => {
  const [turns, setTurns] = useState<ICopilotConversationTurnState[]>([])
  const nextTurnIdRef = useRef(0)
  const activeRequestRef = useRef<AbortController | null>(null)

  useEffect(() => () => activeRequestRef.current?.abort(), [])

  const executeRequest = useCallback(async (request: IAgentQueryRequest, existingTurnId?: string) => {
    activeRequestRef.current?.abort()
    const controller = new AbortController()
    activeRequestRef.current = controller
    const turnId = existingTurnId ?? `turn-${nextTurnIdRef.current++}`

    setTurns((currentTurns) => {
      if (existingTurnId) {
        return currentTurns.map((turn) =>
          turn.id === existingTurnId
            ? {
                ...turn,
                question: request.question,
                request,
                isLoading: true,
                error: null,
                response: null,
                activity: [],
              }
            : turn
        )
      }

      return [
        ...currentTurns,
        {
          id: turnId,
          question: request.question,
          request,
          response: null,
          error: null,
          isLoading: true,
          activity: [],
        },
      ]
    })

    try {
      const result = await streamAgentQuery(
        request,
        (activity) => {
          setTurns((currentTurns) =>
            currentTurns.map((turn) => {
              if (turn.id !== turnId) return turn
              const existingIndex = turn.activity.findIndex((step) => step.sequence === activity.sequence)
              const nextActivity = existingIndex >= 0
                ? turn.activity.map((step, index) => index === existingIndex ? activity : step)
                : [...turn.activity, activity].sort((left, right) => left.sequence - right.sequence)
              return { ...turn, activity: nextActivity }
            })
          )
        },
        controller.signal
      )
      setTurns((currentTurns) =>
        currentTurns.map((turn) =>
          turn.id === turnId
            ? {
                ...turn,
                response: result,
                error: null,
                isLoading: false,
              }
            : turn
        )
      )
    } catch (err) {
      if (controller.signal.aborted) return
      setTurns((currentTurns) =>
        currentTurns.map((turn) =>
          turn.id === turnId
            ? {
                ...turn,
                error: err instanceof Error ? err.message : 'The investigation could not be completed.',
                isLoading: false,
              }
            : turn
        )
      )
    } finally {
      if (activeRequestRef.current === controller) {
        activeRequestRef.current = null
      }
    }
  }, [])

  const submit = useCallback(
    async (request: IAgentQueryRequest) => {
      await executeRequest(request)
    },
    [executeRequest]
  )

  const retry = useCallback(
    async (turnId: string) => {
      const turn = turns.find((item) => item.id === turnId)
      if (!turn) {
        return
      }

      await executeRequest(turn.request, turnId)
    },
    [executeRequest, turns]
  )

  const reset = useCallback(() => {
    activeRequestRef.current?.abort()
    activeRequestRef.current = null
    setTurns([])
  }, [])

  const publicTurns = useMemo<ICopilotConversationTurn[]>(
    () =>
      turns.map((turn) => ({
        id: turn.id,
        question: turn.question,
        response: turn.response,
        error: turn.error,
        isLoading: turn.isLoading,
        activity: turn.activity,
      })),
    [turns]
  )
  const isLoading = turns.some((turn) => turn.isLoading)

  return { turns: publicTurns, isLoading, submit, retry, reset }
}

export { useCopilotQuery }
export type { ICopilotConversationTurn }
