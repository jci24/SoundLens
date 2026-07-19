import type { IComparisonCopilotSelection } from '../../types'

export type TCopilotContextMode = 'auto' | 'workspace' | 'general'
export type TCopilotAnswerMode = 'workspace' | 'general' | 'web' | 'guidance'
export type TAgentActivityKind = 'plan' | 'routing' | 'tool' | 'evidence_check' | 'fallback' | 'completion' | 'failure'
export type TAgentActivityStatus = 'running' | 'completed' | 'failed'
export type TAgentEvidenceSufficiencyStatus = 'supported' | 'partial' | 'missing' | 'contradicted' | 'unavailable'

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
  externalCitations?: IAgentExternalCitation[]
  activityTrace?: IAgentActivityEvent[]
  evidenceSufficiency?: IAgentEvidenceSufficiency
}

export interface IAgentEvidenceSufficiency {
  intent: string
  status: TAgentEvidenceSufficiencyStatus
  label: string
  reason: string
  requiredEvidence: string[]
  availableEvidence: string[]
  limitationCodes: string[]
}

export interface IAgentActivityEvent {
  sequence: number
  kind: TAgentActivityKind
  status: TAgentActivityStatus
  title: string
  summary: string
}

export interface IAgentExternalCitation {
  title: string
  url: string
  startIndex: number
  endIndex: number
}
