import { useState } from 'react'
import { toast } from 'sonner'
import { exportComparisonReportMarkdown, exportReportMarkdown } from '../services/exportReportMarkdown'
import { downloadTextFile } from '../utils/reportDownload'
import type { IMetricSignalItem } from '../../metrics/hooks/useAnalysisWorkspaceMetrics'
import type {
  IAnalysisRegionOfInterest,
  IComparisonCopilotSelection,
  ITimeWaveformRecording,
  TAnalysisLayoutMode,
  TAnalysisSurface,
  TComparisonGroupAssignment,
  TSignalChartMode,
} from '../../types'
import type { IComparisonReportExcludedRecording } from '../types/reportExport'

interface IUseReportExportOptions {
  activePairRecordingA: ITimeWaveformRecording | null
  activePairRecordingB: ITimeWaveformRecording | null
  activeSurface: TAnalysisSurface
  comparisonSelection: IComparisonCopilotSelection | null
  layoutMode: TAnalysisLayoutMode
  metricSignals: IMetricSignalItem[]
  recordingGroupAssignments: Record<string, TComparisonGroupAssignment>
  recordings: ITimeWaveformRecording[]
  regionOfInterest: IAnalysisRegionOfInterest | null
  selectedSignalIds: string[]
  signalChartMode: TSignalChartMode
}

const useReportExport = ({
  activePairRecordingA,
  activePairRecordingB,
  activeSurface,
  comparisonSelection,
  layoutMode,
  metricSignals,
  recordingGroupAssignments,
  recordings,
  regionOfInterest,
  selectedSignalIds,
  signalChartMode,
}: IUseReportExportOptions) => {
  const [isComparisonReportOpen, setIsComparisonReportOpen] = useState(false)
  const [isExporting, setIsExporting] = useState(false)
  const [comparisonReportTitle, setComparisonReportTitle] = useState('')

  const excludedRecordings: IComparisonReportExcludedRecording[] = activePairRecordingA && activePairRecordingB
    ? recordings
        .filter(
          (recording) =>
            recording.recordingId !== activePairRecordingA.recordingId &&
            recording.recordingId !== activePairRecordingB.recordingId
        )
        .map((recording) => ({
          assignment: recordingGroupAssignments[recording.recordingId] ?? 'unassigned',
          fileName: recording.fileName,
          recordingId: recording.recordingId,
        }))
    : []
  const canExportReport = layoutMode === 'focused' || comparisonSelection !== null

  const handleExportReport = () => {
    if (layoutMode === 'compare') {
      if (!comparisonSelection || !activePairRecordingA || !activePairRecordingB) {
        return
      }

      setComparisonReportTitle(`${activePairRecordingA.fileName} vs ${activePairRecordingB.fileName} comparison`)
      setIsComparisonReportOpen(true)
      return
    }

    void exportFocusedReport()
  }

  const exportFocusedReport = async () => {
    try {
      setIsExporting(true)
      const response = await exportReportMarkdown({
        activeSurface,
        layoutMode,
        signalChartMode,
        recordings: recordings.map((recording) => ({
          recordingId: recording.recordingId,
          fileName: recording.fileName,
          sizeBytes: recording.sizeBytes,
          durationSeconds: recording.durationSeconds,
          sampleRate: recording.sampleRate,
          channels: recording.channels,
          channelMode: recording.channelMode,
          signals: recording.signals.map((signal) => ({
            signalId: signal.signalId,
            channelIndex: signal.channelIndex,
            displayName: signal.displayName,
            fileName: recording.fileName,
          })),
        })),
        selectedSignalEvidence: metricSignals.map((signal) => ({
          signalId: signal.signalId,
          fileName: signal.recordingFileName,
          displayName: signal.displayName,
          durationSeconds: signal.durationSeconds,
          sampleRate: signal.sampleRate,
          metrics: {
            peakAmplitude: signal.peakAmplitude,
            rmsAmplitude: signal.rmsAmplitude,
            crestFactor: signal.crestFactor,
            clippingSampleCount: signal.clippingSampleCount,
            hasClipping: signal.hasClipping,
          },
          findings: signal.findings,
        })),
        selectedSignalIds: selectedSignalIds.length > 0 ? selectedSignalIds : undefined,
        startTimeSeconds: regionOfInterest?.startTimeSeconds,
        endTimeSeconds: regionOfInterest?.endTimeSeconds,
      })

      downloadTextFile(response.fileName, response.markdown)
      toast.success(`Downloaded ${response.fileName}.`)
    } catch {
      toast.error('The markdown report could not be prepared.')
    } finally {
      setIsExporting(false)
    }
  }

  const handleComparisonReportExport = async () => {
    if (!comparisonSelection || !activePairRecordingA || !activePairRecordingB || !comparisonReportTitle.trim()) {
      return
    }

    try {
      setIsExporting(true)
      const response = await exportComparisonReportMarkdown({
        reportTitle: comparisonReportTitle.trim(),
        recordingIdA: comparisonSelection.recordingIdA,
        recordingIdB: comparisonSelection.recordingIdB,
        metricKey: comparisonSelection.metricKey,
        signalIdA: comparisonSelection.signalIdA,
        signalIdB: comparisonSelection.signalIdB,
        excludedRecordings: excludedRecordings.map(({ assignment, recordingId }) => ({ assignment, recordingId })),
        startTimeSeconds: regionOfInterest?.startTimeSeconds,
        endTimeSeconds: regionOfInterest?.endTimeSeconds,
      })

      downloadTextFile(response.fileName, response.markdown)
      setIsComparisonReportOpen(false)
      toast.success(`Downloaded ${response.fileName}.`)
    } catch {
      toast.error('The comparison report could not be prepared.')
    } finally {
      setIsExporting(false)
    }
  }

  return {
    canExportReport,
    comparisonReportTitle,
    excludedRecordings,
    handleComparisonReportExport,
    handleExportReport,
    isComparisonReportOpen,
    isExporting,
    setComparisonReportTitle,
    setIsComparisonReportOpen,
  }
}

export { useReportExport }
