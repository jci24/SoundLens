import { Bot, ChevronRight } from 'lucide-react'
import { Link, useLocation } from 'react-router'
import { Button } from '@/components/ui/button'
import './BreadcrumbBar.scss'

const routeLabels: Record<string, string> = {
  '/': 'Home',
  '/import': 'Import',
  '/setup': 'Configure',
  '/analysis': 'Analysis',
  '/evidence': 'Evidence',
}

interface IBreadcrumbBarProps {
  isCopilotOpen: boolean
  onCopilotToggle: () => void
}

const BreadcrumbBar = ({ isCopilotOpen, onCopilotToggle }: IBreadcrumbBarProps) => {
  const { pathname } = useLocation()
  const currentLabel = routeLabels[pathname] ?? 'Home'

  return (
    <div className="breadcrumb-bar">
      <nav className="breadcrumb-bar__trail" aria-label="Breadcrumb">
        {pathname === '/' ? (
          <span aria-current="page">Home</span>
        ) : (
          <>
            <Link to="/">Home</Link>
            <ChevronRight aria-hidden="true" size={13} />
            <span aria-current="page">{currentLabel}</span>
          </>
        )}
      </nav>
      {pathname !== '/evidence' && (
        <Button
          aria-expanded={isCopilotOpen}
          aria-label={isCopilotOpen ? 'Close Copilot' : 'Open Copilot'}
          size="sm"
          type="button"
          variant="ghost"
          onClick={onCopilotToggle}
        >
          <Bot aria-hidden="true" />
          Copilot
        </Button>
      )}
    </div>
  )
}

export { BreadcrumbBar }
