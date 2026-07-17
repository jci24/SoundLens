import type { IComparisonCopilotSelection } from '../../types'

export interface IAgentQueryRequest {
  question: string
  signalIds?: string[]
  startTimeSeconds?: number
  endTimeSeconds?: number
  comparisonContext?: IComparisonCopilotSelection
  comparisonPair?: {
    recordingIdA: string
    recordingIdB: string
  }
}

export interface IAgentEvidenceItem {
  toolName: string
  signalId: string
  summary: string
}

export interface IAgentQueryResponse {
  answer: string
  citedEvidence: IAgentEvidenceItem[]
  limitations: string[]
  nextSteps: string[]
  toolsUsed: string[]
}
