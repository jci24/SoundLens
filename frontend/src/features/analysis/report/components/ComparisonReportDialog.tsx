import * as Dialog from '@radix-ui/react-dialog'
import { X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { formatCompactDuration } from '../../utils/analysisWorkspaceFormatting'
import { formatComparisonIntegrityDetails } from '../../utils/comparisonEvidence'
import type { IAnalysisRegionOfInterest, IRecordingComparisonIntegrityAssessment } from '../../types'
import type { IComparisonReportExcludedRecording, TComparisonReportFormat } from '../types/reportExport'
import './ComparisonReportDialog.scss'

interface IComparisonReportDialogProps {
  excludedRecordings: IComparisonReportExcludedRecording[]
  fileNameA: string
  fileNameB: string
  format: TComparisonReportFormat
  isExporting: boolean
  isOpen: boolean
  integrityAssessment: IRecordingComparisonIntegrityAssessment | null
  onExport: () => void
  onFormatChange: (format: TComparisonReportFormat) => void
  onOpenChange: (isOpen: boolean) => void
  onTitleChange: (title: string) => void
  regionOfInterest: IAnalysisRegionOfInterest | null
  title: string
}

const assignmentLabels = {
  A: 'Compare A',
  B: 'Compare B',
  unassigned: 'Unassigned',
} as const

const ComparisonReportDialog = ({
  excludedRecordings,
  fileNameA,
  fileNameB,
  format,
  isExporting,
  isOpen,
  integrityAssessment,
  onExport,
  onFormatChange,
  onOpenChange,
  onTitleChange,
  regionOfInterest,
  title,
}: IComparisonReportDialogProps) => (
  <Dialog.Root onOpenChange={onOpenChange} open={isOpen}>
    <Dialog.Portal>
      <Dialog.Overlay className="comparison-report-dialog__overlay" />
      <Dialog.Content className="comparison-report-dialog__content">
        <header className="comparison-report-dialog__header">
          <div>
            <Dialog.Title className="comparison-report-dialog__title">Export comparison report</Dialog.Title>
            <Dialog.Description className="comparison-report-dialog__description">
              Review the report scope and choose an export format.
            </Dialog.Description>
          </div>
          <Dialog.Close asChild>
            <Button aria-label="Close report preview" size="icon-sm" type="button" variant="ghost">
              <X />
            </Button>
          </Dialog.Close>
        </header>

        <div className="comparison-report-dialog__body">
          <label className="comparison-report-dialog__field">
            <span>Report title</span>
            <input
              autoFocus
              maxLength={160}
              onChange={(event) => onTitleChange(event.target.value)}
              required
              type="text"
              value={title}
            />
          </label>

          <dl className="comparison-report-dialog__summary">
            <div>
              <dt>Active comparison</dt>
              <dd>{fileNameA} vs {fileNameB}</dd>
            </div>
            <div>
              <dt>Scope</dt>
              <dd>{formatReportScope(regionOfInterest)}</dd>
            </div>
            <div>
              <dt>AI interpretation</dt>
              <dd>Automatic when available, with deterministic fallback</dd>
            </div>
            <div>
              <dt>Comparison context</dt>
              <dd>
                {integrityAssessment
                  ? formatComparisonIntegrityDetails(integrityAssessment)
                  : 'Available after comparison completes'}
              </dd>
            </div>
          </dl>

          <fieldset className="comparison-report-dialog__format">
            <legend>Format</legend>
            <label>
              <input
                checked={format === 'markdown'}
                name="comparison-report-format"
                onChange={() => onFormatChange('markdown')}
                type="radio"
                value="markdown"
              />
              <span>
                <strong>Markdown</strong>
                <small>Editable plain-text report (.md)</small>
              </span>
            </label>
            <label>
              <input
                checked={format === 'pdf'}
                name="comparison-report-format"
                onChange={() => onFormatChange('pdf')}
                type="radio"
                value="pdf"
              />
              <span>
                <strong>PDF</strong>
                <small>Portable document with selectable text (.pdf)</small>
              </span>
            </label>
          </fieldset>

          <section className="comparison-report-dialog__excluded" aria-labelledby="comparison-report-excluded-title">
            <div className="comparison-report-dialog__section-heading">
              <h3 id="comparison-report-excluded-title">Excluded recordings</h3>
              <span>{excludedRecordings.length}</span>
            </div>
            {excludedRecordings.length === 0 ? (
              <p>No other loaded recordings are excluded.</p>
            ) : (
              <ul>
                {excludedRecordings.map((recording) => (
                  <li key={recording.recordingId}>
                    <span>{recording.fileName}</span>
                    <span>{assignmentLabels[recording.assignment]}</span>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </div>

        <footer className="comparison-report-dialog__footer">
          <Dialog.Close asChild>
            <Button disabled={isExporting} type="button" variant="ghost">Cancel</Button>
          </Dialog.Close>
          <Button disabled={isExporting || title.trim().length === 0} onClick={onExport} type="button">
            {isExporting ? 'Preparing report...' : `Export ${format === 'pdf' ? 'PDF' : 'Markdown'}`}
          </Button>
        </footer>
      </Dialog.Content>
    </Dialog.Portal>
  </Dialog.Root>
)

const formatReportScope = (regionOfInterest: IAnalysisRegionOfInterest | null) => {
  if (!regionOfInterest) {
    return 'Full duration'
  }

  return `${formatCompactDuration(regionOfInterest.startTimeSeconds)} to ${formatCompactDuration(regionOfInterest.endTimeSeconds)}`
}

export { ComparisonReportDialog }
