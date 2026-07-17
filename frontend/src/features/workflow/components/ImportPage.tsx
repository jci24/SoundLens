import { ArrowLeft } from 'lucide-react'
import { Link, useNavigate } from 'react-router'
import { Button } from '../../../components/ui/button'
import type { IImportedFileSummary } from '../../../common/contracts/import'
import { ImportWorkspace } from '../../import/components/ImportWorkspace'
import './ImportPage.scss'

interface IImportPageProps {
  hasRecordings: boolean
  onImportedFiles: (files: IImportedFileSummary[]) => void
}

const ImportPage = ({ hasRecordings, onImportedFiles }: IImportPageProps) => {
  const navigate = useNavigate()

  const handleImportedFiles = (files: IImportedFileSummary[]) => {
    onImportedFiles(files)
    navigate(files.length > 1 ? '/setup' : '/evidence')
  }

  return (
    <section className="import-page">
      <div className="import-page__heading">
        <Button asChild aria-label="Return to home" size="icon-sm" variant="ghost">
          <Link to="/">
            <ArrowLeft aria-hidden="true" />
          </Link>
        </Button>
        <div>
          <p className="import-page__eyebrow">Temporary workspace</p>
          <h2>{hasRecordings ? 'Replace recordings' : 'Import recordings'}</h2>
          <p>Choose audio recordings to create the evidence workspace. A new import replaces the current set atomically.</p>
        </div>
      </div>
      <ImportWorkspace onImportedFiles={handleImportedFiles} />
    </section>
  )
}

export { ImportPage }
