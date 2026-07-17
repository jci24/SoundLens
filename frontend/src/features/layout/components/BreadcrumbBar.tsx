import { ChevronRight } from 'lucide-react'
import { Link, useLocation } from 'react-router'
import './BreadcrumbBar.scss'

const routeLabels: Record<string, string> = {
  '/': 'Home',
  '/import': 'Import',
  '/setup': 'Configure',
  '/analysis': 'Analysis',
  '/evidence': 'Evidence',
}

const BreadcrumbBar = () => {
  const { pathname } = useLocation()
  const currentLabel = routeLabels[pathname] ?? 'Home'

  return (
    <nav className="breadcrumb-bar" aria-label="Breadcrumb">
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
  )
}

export { BreadcrumbBar }
