import { useCallback, useState } from 'react'
import { postAgentQuery } from '../services/copilotService'
import type { IAgentQueryRequest, IAgentQueryResponse } from '../types/copilot.types'

interface IUseCopilotQueryResult {
  response: IAgentQueryResponse | null
  isLoading: boolean
  error: string | null
  submit: (request: IAgentQueryRequest) => Promise<void>
  reset: () => void
}

const useCopilotQuery = (): IUseCopilotQueryResult => {
  const [response, setResponse] = useState<IAgentQueryResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const submit = useCallback(async (request: IAgentQueryRequest) => {
    setIsLoading(true)
    setError(null)

    try {
      const result = await postAgentQuery(request)
      setResponse(result)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'The investigation could not be completed.')
    } finally {
      setIsLoading(false)
    }
  }, [])

  const reset = useCallback(() => {
    setResponse(null)
    setError(null)
  }, [])

  return { response, isLoading, error, submit, reset }
}

export { useCopilotQuery }
