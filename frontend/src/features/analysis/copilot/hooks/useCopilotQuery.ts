import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { streamAgentQuery } from '../services/copilotService'
import type {
  IAgentActivityEvent,
  IAgentConversationRequestSnapshot,
  IAgentConversationTurn,
  IAgentQueryRequest,
  IAgentQueryResponse,
} from '../types/copilot.types'

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

const MAX_HISTORY_TURNS = 6
const MAX_HISTORY_CHARACTERS = 16_000
const MAX_HISTORY_ANSWER_CHARACTERS = 4_000

const buildRequestSnapshot = (request: IAgentQueryRequest): IAgentConversationRequestSnapshot => ({
  signalIds: request.signalIds,
  startTimeSeconds: request.startTimeSeconds,
  endTimeSeconds: request.endTimeSeconds,
  comparisonContext: request.comparisonContext,
  comparisonPair: request.comparisonPair,
  contextMode: request.contextMode,
  routeContext: request.routeContext,
})

const buildConversationHistory = (turns: ICopilotConversationTurnState[]): IAgentConversationTurn[] => {
  const candidates = turns
    .filter((turn) => !turn.isLoading && !turn.error && turn.response)
    .map((turn) => ({
      question: turn.question,
      answer: turn.response!.answer.slice(0, MAX_HISTORY_ANSWER_CHARACTERS),
      answerMode: turn.response!.answerMode ?? 'workspace',
      requestSnapshot: buildRequestSnapshot(turn.request),
    }))
    .slice(-MAX_HISTORY_TURNS)

  const bounded: IAgentConversationTurn[] = []
  let characters = 0
  for (const turn of candidates.toReversed()) {
    const turnCharacters = turn.question.length + turn.answer.length
    if (characters + turnCharacters > MAX_HISTORY_CHARACTERS) continue
    bounded.unshift(turn)
    characters += turnCharacters
  }
  return bounded
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
      const conversationHistory = buildConversationHistory(turns)
      await executeRequest(conversationHistory.length > 0
        ? { ...request, conversationHistory }
        : request)
    },
    [executeRequest, turns]
  )

  const retry = useCallback(
    async (turnId: string) => {
      const turn = turns.find((item) => item.id === turnId)
      if (!turn) {
        return
      }

      setTurns((currentTurns) => {
        const turnIndex = currentTurns.findIndex((item) => item.id === turnId)
        return turnIndex < 0 ? currentTurns : currentTurns.slice(0, turnIndex + 1)
      })
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
export type { ICopilotConversationTurn, IUseCopilotQueryResult }
