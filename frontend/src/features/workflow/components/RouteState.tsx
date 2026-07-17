import { AlertCircle, Loader2 } from 'lucide-react'
import { Button } from '../../../components/ui/button'
import './RouteState.scss'

interface IRouteStateProps {
  error?: string | null
  onRetry?: () => void
  title: string
}

const RouteState = ({ error, onRetry, title }: IRouteStateProps) => (
  <section className="route-state" aria-live="polite">
    {error ? <AlertCircle aria-hidden="true" size={20} /> : <Loader2 aria-hidden="true" className="route-state__spinner" size={20} />}
    <div>
      <h2>{title}</h2>
      {error && <p>{error}</p>}
    </div>
    {error && onRetry && (
      <Button type="button" variant="outline" onClick={onRetry}>
        Retry
      </Button>
    )}
  </section>
)

export { RouteState }
