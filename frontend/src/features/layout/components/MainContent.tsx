import type { ReactNode } from 'react'
import './MainContent.scss'

interface IMainContentProps {
  children: ReactNode
  isCopilotOpen?: boolean
}

const MainContent = ({ children, isCopilotOpen = false }: IMainContentProps) => {
  return (
    <main className={`main-content${isCopilotOpen ? ' main-content--copilot-open' : ''}`}>
      {children}
    </main>
  )
}

export { MainContent }
