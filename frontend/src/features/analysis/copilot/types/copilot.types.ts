import type { IComparisonCopilotSelection } from '../../types'

export type TCopilotContextMode = 'auto' | 'workspace' | 'general'
export type TCopilotAnswerMode = 'workspace' | 'general'

export interface IAgentQueryRequest {
  question: string
  contextMode?: TCopilotContextMode
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
  answerMode?: TCopilotAnswerMode
  citedEvidence: IAgentEvidenceItem[]
  limitations: string[]
  nextSteps: string[]
  toolsUsed: string[]
}
