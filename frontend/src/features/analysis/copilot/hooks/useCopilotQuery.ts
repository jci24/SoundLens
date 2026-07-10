import { useCallback, useMemo, useRef, useState } from 'react'
import { postAgentQuery } from '../services/copilotService'
import type { IAgentQueryRequest, IAgentQueryResponse } from '../types/copilot.types'

interface ICopilotConversationTurn {
  id: string
  question: string
  response: IAgentQueryResponse | null
  error: string | null
  isLoading: boolean
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

  const executeRequest = useCallback(async (request: IAgentQueryRequest, existingTurnId?: string) => {
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
        },
      ]
    })

    try {
      const result = await postAgentQuery(request)
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
      })),
    [turns]
  )
  const isLoading = turns.some((turn) => turn.isLoading)

  return { turns: publicTurns, isLoading, submit, retry, reset }
}

export { useCopilotQuery }
export type { ICopilotConversationTurn }
