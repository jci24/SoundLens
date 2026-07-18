import { useRef, useState } from 'react'
import { ArrowUp } from 'lucide-react'
import type { ITimeWaveformRecording } from '../../types'
import type { TCopilotContextMode } from '../types/copilot.types'
import './CopilotInput.scss'

const MAX_CHARS = 500

const EXAMPLE_QUESTIONS = [
  'Which signal has more distortion?',
  'Is there clipping in any recording?',
  'What is causing the tonal peak?',
  'Compare loudness across all signals',
]

interface ISignalOption {
  signalId: string
  displayName: string
  fileName: string
}

interface ICopilotInputProps {
  isLoading: boolean
  showSuggestions: boolean
  recordings: ITimeWaveformRecording[]
  workspaceContextLabel: string
  onSubmit: (question: string, contextMode: TCopilotContextMode) => void
}

const CopilotInput = ({
  isLoading,
  showSuggestions,
  recordings,
  workspaceContextLabel,
  onSubmit,
}: ICopilotInputProps) => {
  const [question, setQuestion] = useState('')
  const [contextMode, setContextMode] = useState<TCopilotContextMode>('auto')
  const [mentionQuery, setMentionQuery] = useState<string | null>(null)
  const [mentionStart, setMentionStart] = useState(0)
  const textareaRef = useRef<HTMLTextAreaElement | null>(null)

  const allSignals: ISignalOption[] = recordings.flatMap((rec) =>
    rec.signals.map((sig) => ({ signalId: sig.signalId, displayName: sig.displayName, fileName: rec.fileName }))
  )

  const filteredSignals = mentionQuery !== null
    ? allSignals.filter((s) =>
        s.displayName.toLowerCase().includes(mentionQuery.toLowerCase()) ||
        s.fileName.toLowerCase().includes(mentionQuery.toLowerCase())
      )
    : []

  const trimmed = question.trim()
  const canSubmit = trimmed.length > 0 && trimmed.length <= MAX_CHARS && !isLoading
  const isOverLimit = question.length > MAX_CHARS

  const handleSubmit = () => {
    if (!canSubmit) return
    onSubmit(trimmed, contextMode)
    setQuestion('')
    setMentionQuery(null)
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (mentionQuery !== null && filteredSignals.length > 0 && (e.key === 'Escape')) {
      e.preventDefault()
      setMentionQuery(null)
      return
    }
    if (e.key === 'Enter' && !e.shiftKey && mentionQuery === null) {
      e.preventDefault()
      handleSubmit()
    }
  }

  const handleChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const value = e.target.value
    setQuestion(value)

    const cursor = e.target.selectionStart ?? value.length
    const textBeforeCursor = value.slice(0, cursor)
    const atIndex = textBeforeCursor.lastIndexOf('@')

    if (atIndex !== -1) {
      const afterAt = textBeforeCursor.slice(atIndex + 1)
      if (!afterAt.includes(' ') && !afterAt.includes('\n')) {
        setMentionQuery(afterAt)
        setMentionStart(atIndex)
        return
      }
    }
    setMentionQuery(null)
  }

  const insertMention = (signal: ISignalOption) => {
    const before = question.slice(0, mentionStart)
    const after = question.slice(mentionStart + 1 + (mentionQuery?.length ?? 0))
    const token = `@[${signal.displayName}](${signal.signalId})`
    const next = `${before}${token}${after}`
    setQuestion(next)
    setMentionQuery(null)
    textareaRef.current?.focus()
  }

  const handleSuggestion = (suggestion: string) => {
    setQuestion(suggestion)
    textareaRef.current?.focus()
  }

  return (
    <div className="copilot-input">
      {showSuggestions && (
        <div className="copilot-input__suggestions" aria-label="Example questions">
          {EXAMPLE_QUESTIONS.map((suggestion) => (
            <button
              key={suggestion}
              className="copilot-input__suggestion-pill"
              type="button"
              onClick={() => handleSuggestion(suggestion)}
            >
              {suggestion}
            </button>
          ))}
        </div>
      )}

      {mentionQuery !== null && filteredSignals.length > 0 && (
        <ul className="copilot-input__mention-list" role="listbox" aria-label="Signal mentions">
          {filteredSignals.map((signal) => (
            <li
              key={signal.signalId}
              className="copilot-input__mention-item"
              role="option"
              aria-selected={false}
              onMouseDown={(e) => { e.preventDefault(); insertMention(signal) }}
            >
              <span className="copilot-input__mention-name">{signal.displayName}</span>
              <span className="copilot-input__mention-file">{signal.fileName}</span>
            </li>
          ))}
        </ul>
      )}

      <div className="copilot-input__context-row">
        <label className="copilot-input__context-label" htmlFor="copilot-context-mode">
          Context
        </label>
        <select
          id="copilot-context-mode"
          className="copilot-input__context-select"
          disabled={isLoading}
          value={contextMode}
          onChange={(event) => setContextMode(event.target.value as TCopilotContextMode)}
        >
          <option value="auto">Auto</option>
          <option value="workspace">Workspace</option>
          <option value="general">General</option>
        </select>
        <span className="copilot-input__context-summary">
          {contextMode === 'general' ? 'Workspace context ignored' : workspaceContextLabel}
        </span>
      </div>

      <div className="copilot-input__field-row">
        <textarea
          ref={textareaRef}
          className={`copilot-input__textarea${isOverLimit ? ' copilot-input__textarea--over-limit' : ''}`}
          disabled={isLoading}
          maxLength={MAX_CHARS + 10}
          placeholder="Ask anything… type @ to mention a signal"
          rows={2}
          value={question}
          onChange={handleChange}
          onKeyDown={handleKeyDown}
          aria-label="Investigation question"
        />
        <button
          className="copilot-input__submit"
          disabled={!canSubmit}
          type="button"
          onClick={handleSubmit}
          aria-label="Investigate"
        >
          <ArrowUp size={16} />
        </button>
      </div>
    </div>
  )
}

export { CopilotInput }
