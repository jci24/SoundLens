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
  showSuggestions: boolean
  onSubmit: (question: string) => void
}

const CopilotInput = ({ isLoading, showSuggestions, onSubmit }: ICopilotInputProps) => {
  const [question, setQuestion] = useState('')
  const textareaRef = useRef<HTMLTextAreaElement | null>(null)

  const trimmed = question.trim()
  const canSubmit = trimmed.length > 0 && trimmed.length <= MAX_CHARS && !isLoading
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
      {/* Suggestions only shown when thread is empty */}
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

      <div className="copilot-input__field-row">
        <textarea
          ref={textareaRef}
          className={`copilot-input__textarea${isOverLimit ? ' copilot-input__textarea--over-limit' : ''}`}
          disabled={isLoading}
          maxLength={MAX_CHARS + 10}
          placeholder="Ask anything about the recordings…"
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
    </div>
  )
}

export { CopilotInput }
