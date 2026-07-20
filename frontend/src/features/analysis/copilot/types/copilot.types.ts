import type { IComparisonCopilotSelection } from '../../types'

export type TCopilotContextMode = 'auto' | 'workspace' | 'general'
export type TCopilotAnswerMode = 'workspace' | 'general' | 'web' | 'guidance'
export type TAgentActivityKind = 'plan' | 'routing' | 'tool' | 'evidence_check' | 'fallback' | 'completion' | 'failure'
export type TAgentActivityStatus = 'running' | 'completed' | 'failed'
export type TAgentEvidenceSufficiencyStatus = 'supported' | 'partial' | 'missing' | 'contradicted' | 'unavailable'
export type TAgentStructuredObservationStatus = 'complete' | 'limited' | 'mixed'

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
  structuredObservations?: IAgentStructuredObservation[]
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

export interface IAgentObservationScope {
  kind: 'full_duration' | 'roi'
  startTimeSeconds: number | null
  endTimeSeconds: number | null
}

export interface IAgentEvidenceReference {
  referenceId: string
  evidenceType: 'comparison_metric' | 'signal_finding'
  recordingIds: string[]
  signalIds: string[]
  metricKey: string | null
  scope: IAgentObservationScope
}

interface IAgentStructuredObservationBase {
  observationId: string
  status: TAgentStructuredObservationStatus
  scope: IAgentObservationScope
  limitationCodes: string[]
  evidenceReferences: IAgentEvidenceReference[]
}

export interface IAgentComparisonMetricObservation extends IAgentStructuredObservationBase {
  kind: 'comparison_metric'
  comparisonMetric: {
    metricKey: 'peakAmplitudeDelta' | 'rmsAmplitudeDelta' | 'crestFactorDelta' | 'clippingSampleCountDelta'
    metricLabel: string
    unit: 'FS' | 'ratio' | 'samples'
    aggregate: {
      comparedPairCount: number
      missingValueCount: number
      meanDifference: number
      medianDifference: number
      minimumDifference: number
      maximumDifference: number
      spread: number
    }
    selectedPair: {
      recordingIdA: string
      recordingFileNameA: string
      signalIdA: string
      signalDisplayNameA: string
      valueA: number
      recordingIdB: string
      recordingFileNameB: string
      signalIdB: string
      signalDisplayNameB: string
      valueB: number
      difference: number
    }
  }
  signalFinding: null
}

export interface IAgentSignalFindingObservation extends IAgentStructuredObservationBase {
  kind: 'signal_finding'
  comparisonMetric: null
  signalFinding: {
    side: 'A' | 'B'
    recordingId: string
    recordingFileName: string
    signalId: string
    signalDisplayName: string
    category: string
    severity: string
    label: string
    detail: string | null
  }
}

export type IAgentStructuredObservation =
  | IAgentComparisonMetricObservation
  | IAgentSignalFindingObservation

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
