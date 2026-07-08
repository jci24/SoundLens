export interface IAgentQueryRequest {
  question: string
  signalIds?: string[]
  startTimeSeconds?: number
  endTimeSeconds?: number
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
