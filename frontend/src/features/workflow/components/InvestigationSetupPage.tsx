import { AlertCircle, ArrowRight } from 'lucide-react'
import { useNavigate } from 'react-router'
import { Button } from '../../../components/ui/button'
import { useImportedRecordingInventory } from '../../import/hooks/useImportedRecordingInventory'
import { ComparePairBuilder } from '../../analysis/recording-rail/components/ComparePairBuilder'
import { useAnalysisWorkspaceStore } from '../../analysis/stores/useAnalysisWorkspaceStore'
import { getComparisonSetupSummary } from '../../analysis/utils/analysisWorkspaceState'
import type { ITimeWaveformRecording } from '../../analysis/types'
import { RouteState } from './RouteState'
import './InvestigationSetupPage.scss'

const formatDuration = (seconds: number) => `${seconds.toFixed(seconds < 10 ? 2 : 1)} s`
const formatSampleRate = (sampleRate: number) => `${(sampleRate / 1000).toFixed(1)} kHz`

const InvestigationSetupPage = () => {
  const navigate = useNavigate()
  const { error, inventory, retry, status } = useImportedRecordingInventory()
  const assignments = useAnalysisWorkspaceStore((state) => state.recordingGroupAssignments)
  const setAssignment = useAnalysisWorkspaceStore((state) => state.setRecordingGroupAssignment)
  const setLayoutMode = useAnalysisWorkspaceStore((state) => state.setLayoutMode)
  const swapTargets = useAnalysisWorkspaceStore((state) => state.swapComparisonTargets)
  const recordings: ITimeWaveformRecording[] = inventory?.recordings ?? []
  const setup = getComparisonSetupSummary(recordings, assignments)

  if (status === 'loading') {
    return <RouteState title="Loading recording configuration" />
  }

  if (status === 'error') {
    return <RouteState error={error} onRetry={retry} title="Recording configuration unavailable" />
  }

  const openEvidence = (mode: 'focused' | 'compare') => {
    setLayoutMode(mode)
    navigate('/evidence')
  }

  return (
    <main className="investigation-setup">
      <header className="investigation-setup__header">
        <div>
          <p className="investigation-setup__eyebrow">Optional configuration</p>
          <h2>Configure comparison</h2>
          <p>Choose a recording for each side, or continue directly to focused evidence.</p>
        </div>
        <span className="investigation-setup__count sl-data">{recordings.length} recordings</span>
      </header>

      {inventory && inventory.failedFiles.length > 0 && (
        <div className="investigation-setup__warning" role="status">
          <AlertCircle aria-hidden="true" size={16} />
          <span>{inventory.failedFiles.length} imported file{inventory.failedFiles.length === 1 ? '' : 's'} could not provide recording metadata.</span>
        </div>
      )}

      <div className="investigation-setup__body">
        <section className="investigation-setup__pair" aria-labelledby="comparison-pair-title">
          <div className="investigation-setup__section-heading">
            <div>
              <p className="investigation-setup__eyebrow">Comparison pair</p>
              <h3 id="comparison-pair-title">Compare A and B</h3>
            </div>
            <span className="investigation-setup__status">
              {setup.state === 'valid' ? 'Ready' : setup.state === 'incomplete' ? 'Choose both sides' : 'Not configured'}
            </span>
          </div>
          <ComparePairBuilder
            onRecordingGroupAssignment={setAssignment}
            onSwap={swapTargets}
            recordings={recordings}
            recordingGroupAssignments={assignments}
          />
        </section>

        <section className="investigation-setup__inventory" aria-labelledby="recording-inventory-title">
          <div className="investigation-setup__section-heading">
            <div>
              <p className="investigation-setup__eyebrow">Current import</p>
              <h3 id="recording-inventory-title">Recording inventory</h3>
            </div>
          </div>
          <div className="investigation-setup__table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Recording</th>
                  <th>Duration</th>
                  <th>Channels</th>
                  <th>Sample rate</th>
                  <th>Role</th>
                </tr>
              </thead>
              <tbody>
                {recordings.map((recording) => {
                  const assignment = assignments[recording.recordingId]
                  return (
                    <tr key={recording.recordingId}>
                      <td>
                        <strong>{recording.fileName}</strong>
                        <span>{recording.signals.map((signal) => signal.displayName).join(', ')}</span>
                      </td>
                      <td className="sl-data">{formatDuration(recording.durationSeconds)}</td>
                      <td className="sl-data">{recording.channels}</td>
                      <td className="sl-data">{formatSampleRate(recording.sampleRate)}</td>
                      <td>{assignment && assignment !== 'unassigned' ? `Compare ${assignment}` : 'Available'}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </section>
      </div>

      <footer className="investigation-setup__footer">
        <p>A/B assignment changes workspace context only. Measurements remain backend-owned.</p>
        <div>
          <Button variant="outline" onClick={() => openEvidence('focused')}>Open focused evidence</Button>
          <Button disabled={setup.state !== 'valid'} onClick={() => openEvidence('compare')}>
            Open comparison evidence
            <ArrowRight aria-hidden="true" />
          </Button>
        </div>
      </footer>
    </main>
  )
}

export { InvestigationSetupPage }
