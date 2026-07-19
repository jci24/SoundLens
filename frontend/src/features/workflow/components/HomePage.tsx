import { ArrowRight, SlidersHorizontal, Upload } from 'lucide-react'
import { Link } from 'react-router'
import { Button } from '../../../components/ui/button'
import type { IImportSessionFileSummary } from '../../../common/contracts/import'
import type { TImportSessionStatus } from '../../import/hooks/useCurrentImportSession'
import { RouteState } from './RouteState'
import './HomePage.scss'

interface IHomePageProps {
  error: string | null
  files: IImportSessionFileSummary[]
  onRetry: () => void
  status: TImportSessionStatus
}

const HomePage = ({ error, files, onRetry, status }: IHomePageProps) => {
  if (status === 'loading') {
    return <RouteState title="Restoring temporary workspace" />
  }

  if (status === 'error') {
    return <RouteState error={error} onRetry={onRetry} title="Workspace restoration failed" />
  }

  const hasRecordings = files.length > 0

  return (
    <div className="home-page">
      <header className="home-page__hero">
        <p className="home-page__eyebrow">Acoustic investigation workspace</p>
        <h2>Turn repeated recordings into reviewable evidence.</h2>
        <p className="home-page__lede">
          Compare product sound with deterministic DSP, traceable evidence, and grounded AI explanation.
        </p>
        {!hasRecordings && status === 'ready' && (
          <Button asChild size="lg">
            <Link to="/import">
              <Upload aria-hidden="true" />
              Import recordings
            </Link>
          </Button>
        )}
      </header>

      {status === 'ready' && hasRecordings && (
        <section className="home-page__current" aria-labelledby="current-investigation-title">
          <div className="home-page__current-heading">
            <div>
              <p className="home-page__eyebrow">Temporary workspace</p>
              <h3 id="current-investigation-title">Current investigation</h3>
            </div>
            <span className="home-page__count sl-data">{files.length} recordings</span>
          </div>
          <ul className="home-page__file-list">
            {files.map((file, index) => (
              <li key={`${file.fileName}-${index}`}>
                <span>{file.fileName}</span>
                <span className="sl-data">{file.contentType}</span>
              </li>
            ))}
          </ul>
          <div className="home-page__actions">
            {files.length > 1 ? (
              <Button asChild>
                <Link to="/setup">
                  <SlidersHorizontal aria-hidden="true" />
                  Configure comparison
                </Link>
              </Button>
            ) : (
              <Button asChild>
                <Link to="/evidence">
                  Inspect recording
                  <ArrowRight aria-hidden="true" />
                </Link>
              </Button>
            )}
            {files.length > 1 && (
              <Button asChild variant="outline">
                <Link to="/evidence">Open focused evidence</Link>
              </Button>
            )}
            <Button asChild variant="outline">
              <Link to="/import">Replace recordings</Link>
            </Button>
          </div>
          <p className="home-page__temporary-note">
            This workspace belongs to the current backend session and is not yet a saved project.
          </p>
        </section>
      )}
    </div>
  )
}

export { HomePage }
