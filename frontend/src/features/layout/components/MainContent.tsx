import type { ReactNode } from 'react'
import './MainContent.scss'

interface IMainContentProps {
  children: ReactNode
}

const MainContent = ({ children }: IMainContentProps) => <main className="main-content">{children}</main>

export { MainContent }
