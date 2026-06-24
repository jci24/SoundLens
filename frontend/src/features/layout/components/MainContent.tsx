import type { ReactNode } from 'react'
import './MainContent.scss'

interface IMainContentProps {
  children: ReactNode
}

const MainContent = ({ children }: IMainContentProps) => {
  return <main className="main-content">{children}</main>
}

export { MainContent }
