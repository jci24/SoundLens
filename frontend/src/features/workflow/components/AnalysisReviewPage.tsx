import { ArrowLeft, ArrowRight, AudioWaveform, ChartNoAxesCombined } from 'lucide-react'
import { Link, useNavigate } from 'react-router'
import { Button } from '../../../components/ui/button'
import { Checkbox } from '../../../components/ui/checkbox'
import { useAnalysisWorkspaceStore } from '../../analysis/stores/useAnalysisWorkspaceStore'
import type { TAnalysisSurface } from '../../analysis/types'
import { getComparisonSetupSummary } from '../../analysis/utils/analysisWorkspaceState'
import { useImportedRecordingInventory } from '../../import/hooks/useImportedRecordingInventory'
import { RouteState } from './RouteState'
import './AnalysisReviewPage.scss'

const analysisOptions: Array<{
  description: string
  label: string
  outputs: string
  surface: TAnalysisSurface
}> = [
  {
    description: 'Inspect amplitude over time and select a time region directly on the waveform.',
    label: 'Time waveform',
    outputs: 'Waveform bins, peak, RMS, crest factor, clipping, and time-domain findings',
    surface: 'waveform',
  },
  {
    description: 'Inspect frequency magnitude using the existing deterministic FFT analysis.',
    label: 'Frequency spectrum',
    outputs: 'FFT magnitude, frequency controls, tonal components, and harmonic findings',
    surface: 'spectrum',
  },
]

const AnalysisReviewPage = () => {
  const navigate = useNavigate()
  const { error, inventory, retry, status } = useImportedRecordingInventory()
  const enabledAnalysisSurfaces = useAnalysisWorkspaceStore((state) => state.enabledAnalysisSurfaces)
  const layoutMode = useAnalysisWorkspaceStore((state) => state.layoutMode)
  const recordingGroupAssignments = useAnalysisWorkspaceStore((state) => state.recordingGroupAssignments)
  const regionOfInterest = useAnalysisWorkspaceStore((state) => state.regionOfInterest)
  const toggleAnalysisSurface = useAnalysisWorkspaceStore((state) => state.toggleAnalysisSurface)
  const recordings = inventory?.recordings ?? []
  const comparisonSetup = getComparisonSetupSummary(recordings, recordingGroupAssignments)
  const recordingA = recordings.find(
    (recording) => recordingGroupAssignments[recording.recordingId] === 'A'
  )
  const recordingB = recordings.find(
    (recording) => recordingGroupAssignments[recording.recordingId] === 'B'
  )
  const hasValidComparison = comparisonSetup.state === 'valid' && recordingA && recordingB
  const canRun = layoutMode !== 'compare' || Boolean(hasValidComparison)
  const scopeLabel = regionOfInterest
    ? `${regionOfInterest.startTimeSeconds.toFixed(2)}–${regionOfInterest.endTimeSeconds.toFixed(2)} s ROI`
    : 'Full duration'

  if (status === 'loading') {
    return <RouteState title="Loading analysis configuration" />
  }

  if (status === 'error') {
    return <RouteState error={error} onRetry={retry} title="Analysis configuration unavailable" />
  }

  return (
    <main className="analysis-review">
      <header className="analysis-review__header">
        <div>
          <p className="analysis-review__eyebrow">Analysis setup</p>
          <h2>Select analyses</h2>
          <p>Choose the deterministic views to prepare in the evidence workspace.</p>
        </div>
        <span className="analysis-review__selection-count sl-data">
          {enabledAnalysisSurfaces.length} selected
        </span>
      </header>

      <div className="analysis-review__body">
        <section className="analysis-review__methods" aria-labelledby="analysis-methods-title">
          <div className="analysis-review__section-heading">
            <p className="analysis-review__eyebrow">Available now</p>
            <h3 id="analysis-methods-title">Analysis methods</h3>
          </div>

          <div className="analysis-review__method-list">
            {analysisOptions.map((option) => {
              const isEnabled = enabledAnalysisSurfaces.includes(option.surface)
              const Icon = option.surface === 'waveform' ? AudioWaveform : ChartNoAxesCombined

              return (
                <label className="analysis-review__method" key={option.surface}>
                  <Checkbox
                    aria-label={`Include ${option.label}`}
                    checked={isEnabled}
                    onCheckedChange={() => toggleAnalysisSurface(option.surface)}
                  />
                  <Icon aria-hidden="true" className="analysis-review__method-icon" size={20} />
                  <span className="analysis-review__method-copy">
                    <strong>{option.label}</strong>
                    <span>{option.description}</span>
                    <small>{option.outputs}</small>
                  </span>
                </label>
              )
            })}
          </div>

          <p className="analysis-review__selection-note">
            At least one analysis remains selected. Additional methods appear only after their deterministic backend capability is validated.
          </p>
        </section>

        <aside className="analysis-review__summary" aria-labelledby="analysis-summary-title">
          <div className="analysis-review__section-heading">
            <p className="analysis-review__eyebrow">Review</p>
            <h3 id="analysis-summary-title">Execution summary</h3>
          </div>

          <dl>
            <div>
              <dt>Workspace mode</dt>
              <dd>{layoutMode === 'compare' ? 'A/B comparison' : 'Focused inspection'}</dd>
            </div>
            {layoutMode === 'compare' && (
              <div>
                <dt>Active pair</dt>
                <dd>
                  {hasValidComparison
                    ? `${recordingA.fileName} vs ${recordingB.fileName}`
                    : 'Comparison pair incomplete'}
                </dd>
              </div>
            )}
            <div>
              <dt>Scope</dt>
              <dd>{scopeLabel}</dd>
            </div>
            <div>
              <dt>Calibration</dt>
              <dd>Digital full scale; no validated physical SPL calibration</dd>
            </div>
          </dl>

          {layoutMode === 'compare' && (
            <div className="analysis-review__automatic-output">
              <span>Included automatically</span>
              <strong>Comparison metrics</strong>
              <p>Peak, RMS, crest factor, and clipping differences over backend-aligned channels.</p>
            </div>
          )}

          {!canRun && (
            <p className="analysis-review__blocking-note" role="alert">
              Choose one Compare A and one Compare B recording before running comparison analyses.
            </p>
          )}
        </aside>
      </div>

      <footer className="analysis-review__footer">
        <Button asChild variant="ghost">
          <Link to="/setup">
            <ArrowLeft aria-hidden="true" />
            Back to configuration
          </Link>
        </Button>
        <div>
          <Button asChild variant="outline">
            <Link to="/evidence">Open current evidence</Link>
          </Button>
          <Button disabled={!canRun} onClick={() => navigate('/evidence')}>
            Run selected analyses
            <ArrowRight aria-hidden="true" />
          </Button>
        </div>
      </footer>
    </main>
  )
}

export { AnalysisReviewPage }
