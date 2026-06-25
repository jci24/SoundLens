import { useEffect, useRef, useState, type RefObject } from 'react'

interface IUseSpectrumControlsPopoverResult {
  isOpen: boolean
  open: () => void
  popoverRef: RefObject<HTMLDivElement | null>
  toggle: () => void
}

const useSpectrumControlsPopover = (): IUseSpectrumControlsPopoverResult => {
  const [isOpen, setIsOpen] = useState(false)
  const popoverRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    if (!isOpen) {
      return
    }

    const handlePointerDown = (event: MouseEvent) => {
      const target = event.target

      if (!(target instanceof Node)) {
        return
      }

      if (!popoverRef.current?.contains(target)) {
        setIsOpen(false)
      }
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsOpen(false)
      }
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [isOpen])

  return {
    isOpen,
    open: () => setIsOpen(true),
    popoverRef,
    toggle: () => setIsOpen((current) => !current),
  }
}

export { useSpectrumControlsPopover }
