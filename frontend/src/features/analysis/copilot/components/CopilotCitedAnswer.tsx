import type { ReactNode } from 'react'
import type { IAgentExternalCitation } from '../types/copilot.types'

interface ICopilotCitedAnswerProps {
  answer: string
  citations: IAgentExternalCitation[]
}

const CopilotCitedAnswer = ({ answer, citations }: ICopilotCitedAnswerProps) => {
  const citationsByEndIndex = citations.reduce<Map<number, IAgentExternalCitation[]>>((groups, citation) => {
    const current = groups.get(citation.endIndex) ?? []
    current.push(citation)
    groups.set(citation.endIndex, current)
    return groups
  }, new Map())
  const endIndexes = [...citationsByEndIndex.keys()].sort((left, right) => left - right)
  const content: ReactNode[] = []
  let cursor = 0

  endIndexes.forEach((endIndex) => {
    content.push(answer.slice(cursor, endIndex))
    citationsByEndIndex.get(endIndex)?.forEach((citation, index) => {
      content.push(
        <a
          aria-label={`Source: ${citation.title}`}
          className="copilot-response__inline-citation"
          href={citation.url}
          key={`${citation.url}-${citation.startIndex}-${citation.endIndex}-${index}`}
          rel="noreferrer"
          target="_blank"
          title={citation.title}
        >
          [{citations.indexOf(citation) + 1}]
        </a>,
      )
    })
    cursor = endIndex
  })
  content.push(answer.slice(cursor))

  return <p className="copilot-response__answer">{content}</p>
}

export { CopilotCitedAnswer }
