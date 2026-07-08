import { useRef, useState } from 'react'
import { ArrowUp } from 'lucide-react'
import './CopilotInput.scss'

const MAX_CHARS = 500

const EXAMPLE_QUESTIONS = [
  'Which signal has more distortion?',
  'Is there clipping in any recording?',
  'What is causing the tonal peak?',
  'Compare loudness across all signals',
]

interface ICopilotInputProps {
  isLoading: boolean
  onSubmit: (question: string) => void
}

const CopilotInput = ({ isLoading, onSubmit }: ICopilotInputProps) => {
  const [question, setQuestion] = useState('')
  const textareaRef = useRef<HTMLTextAreaElement | null>(null)

  const trimmed = question.trim()
  const canSubmit = trimmed.length > 0 && trimmed.length <= MAX_CHARS && !isLoading
  const remaining = MAX_CHARS - question.length
  const isOverLimit = question.length > MAX_CHARS

  const handleSubmit = () => {
    if (!canSubmit) return
    onSubmit(trimmed)
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSubmit()
    }
  }

  const handleSuggestion = (suggestion: string) => {
    setQuestion(suggestion)
    textareaRef.current?.focus()
  }

  return (
    <div className="copilot-input">
      {/* Wayfinder: Suggestions pattern — solve the blank canvas problem */}
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

      {/* Open input pattern */}
      <div className="copilot-input__field-row">
        <textarea
          ref={textareaRef}
          className={`copilot-input__textarea${isOverLimit ? ' copilot-input__textarea--over-limit' : ''}`}
          disabled={isLoading}
          maxLength={MAX_CHARS + 10}
          placeholder={'Ask about the loaded recordings — e.g. "Which signal has more distortion?"'}
          rows={2}
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
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

      <div className="copilot-input__footer">
        <span className={`copilot-input__char-count${isOverLimit ? ' copilot-input__char-count--over' : ''}`}>
          {isOverLimit ? `${Math.abs(remaining)} over limit` : `${remaining} remaining`}
        </span>
        <span className="copilot-input__hint">Shift+Enter for new line · Enter to investigate</span>
      </div>
    </div>
  )
}

export { CopilotInput }
