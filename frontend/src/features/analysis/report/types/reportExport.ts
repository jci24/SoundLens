import type { IAnalysisRegionOfInterest, TAnalysisLayoutMode, TAnalysisSurface, TSignalChartMode } from '../../types'
import type { ISignalDerivedMetrics, ISignalFinding } from '../../types'

export interface IReportExportRecording {
  recordingId: string
  fileName: string
  sizeBytes: number
  durationSeconds: number
  sampleRate: number
  channels: number
  channelMode: string
  signals: IReportExportSignal[]
}

export interface IReportExportRequest {
  activeSurface: TAnalysisSurface
  layoutMode: TAnalysisLayoutMode
  signalChartMode: TSignalChartMode
  recordings: IReportExportRecording[]
  selectedSignalEvidence?: IReportExportSignalEvidence[]
  selectedSignalIds?: string[]
  startTimeSeconds?: number
  endTimeSeconds?: number
}

export interface IReportExportSignal {
  signalId: string
  channelIndex: number
  displayName: string
  fileName: string
}

export interface IReportExportSignalEvidence {
  signalId: string
  fileName: string
  displayName: string
  durationSeconds: number
  sampleRate: number
  metrics?: ISignalDerivedMetrics
  findings: ISignalFinding[]
}

export interface IReportExportSummary {
  recordingCount: number
  totalSignalCount: number
  selectedSignalCount: number
  hasRegionOfInterest: boolean
}

export interface IReportExportResponse {
  reportTitle: string
  exportedAtUtc: string
  activeSurface: TAnalysisSurface
  layoutMode: TAnalysisLayoutMode
  signalChartMode: TSignalChartMode
  regionOfInterest: IAnalysisRegionOfInterest | null
  recordings: IReportExportRecording[]
  selectedSignals: IReportExportSignal[]
  selectedSignalEvidence: IReportExportSignalEvidence[]
  summary: IReportExportSummary
}
