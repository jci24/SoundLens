import { useId, useState } from 'react'
import { ChevronDown, ChevronUp } from 'lucide-react'
import type { IAgentExternalCitation } from '../types/copilot.types'
import './CopilotSourceDetails.scss'

interface ICopilotSourceDetailsProps {
  citations: IAgentExternalCitation[]
}

const SOURCE_CLASS_LABELS: Record<IAgentExternalCitation['sourceMetadata']['sourceClass'], string> = {
  standards_body: 'Standards body',
  public_authority: 'Public authority',
  unclassified: 'Unclassified source',
}

const CopilotSourceDetails = ({ citations }: ICopilotSourceDetailsProps) => {
  const [isOpen, setIsOpen] = useState(false)
  const contentId = useId()
  const sources = [...new Map(citations.map((citation) => [citation.url, citation])).values()]

  return (
    <div className="copilot-source-details">
      <button
        aria-controls={contentId}
        aria-expanded={isOpen}
        className="copilot-source-details__toggle"
        type="button"
        onClick={() => setIsOpen((current) => !current)}
      >
        <span>Source details</span>
        <span>{sources.length} source{sources.length === 1 ? '' : 's'}</span>
        {isOpen ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
      </button>

      {isOpen && (
        <dl className="copilot-source-details__list" id={contentId}>
          {sources.map((source) => (
            <div key={source.url}>
              <dt>{source.title}</dt>
              <dd>{SOURCE_CLASS_LABELS[source.sourceMetadata.sourceClass]} · {source.sourceMetadata.publisherHost}</dd>
              <dd>Access not verified · Applicability not assessed</dd>
            </div>
          ))}
        </dl>
      )}
    </div>
  )
}

export { CopilotSourceDetails }
