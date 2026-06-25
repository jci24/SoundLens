import {
  Check,
  ChevronDown,
} from 'lucide-react'
import * as React from 'react'
import { cn } from '@/lib/utils'

interface IComboboxContext {
  isOpen: boolean
  items: readonly string[]
  onSelect: (value: string) => void
  selectedValue?: string
  setIsOpen: React.Dispatch<React.SetStateAction<boolean>>
}

const ComboboxContext = React.createContext<IComboboxContext | null>(null)

interface IComboboxProps {
  children: React.ReactNode
  items: readonly string[]
  value?: string
  onValueChange?: (value: string) => void
}

const Combobox = ({ children, items, value, onValueChange }: IComboboxProps) => {
  const [internalValue, setInternalValue] = React.useState(items[0] ?? '')
  const [isOpen, setIsOpen] = React.useState(false)
  const selectedValue = value ?? internalValue

  const handleSelect = (nextValue: string) => {
    if (value === undefined) {
      setInternalValue(nextValue)
    }

    onValueChange?.(nextValue)
    setIsOpen(false)
  }

  return (
    <ComboboxContext.Provider
      value={{
        isOpen,
        items,
        onSelect: handleSelect,
        selectedValue,
        setIsOpen,
      }}
    >
      <div className="relative">{children}</div>
    </ComboboxContext.Provider>
  )
}

interface IComboboxInputProps {
  placeholder: string
  className?: string
}

const ComboboxInput = ({ placeholder, className }: IComboboxInputProps) => {
  const context = useComboboxContext()

  return (
    <button
      className={cn(
        'bg-background border-input text-foreground inline-flex h-8 min-w-[14rem] items-center justify-between gap-2.5 rounded-lg border px-3 text-[0.75rem] shadow-xs transition-[border-color,box-shadow] outline-none hover:border-border focus-visible:ring-3 focus-visible:ring-ring/50',
        className
      )}
      type="button"
      onClick={() => context.setIsOpen((current) => !current)}
    >
      <span className="truncate text-left">
        {context.selectedValue || placeholder}
      </span>
      <ChevronDown className="size-4 text-muted-foreground" />
    </button>
  )
}

interface IComboboxContentProps {
  children: React.ReactNode
}

const ComboboxContent = ({ children }: IComboboxContentProps) => {
  const context = useComboboxContext()

  if (!context.isOpen) {
    return null
  }

  return (
    <div className="bg-popover text-popover-foreground absolute right-0 z-20 mt-2 min-w-[14rem] overflow-hidden rounded-xl border border-border shadow-lg">
      {children}
    </div>
  )
}

interface IComboboxEmptyProps {
  children: React.ReactNode
}

const ComboboxEmpty = ({ children }: IComboboxEmptyProps) => {
  const context = useComboboxContext()

  if (context.items.length > 0) {
    return null
  }

  return <div className="px-3 py-2 text-[0.75rem] text-muted-foreground">{children}</div>
}

interface IComboboxListProps {
  children: (item: string) => React.ReactNode
}

const ComboboxList = ({ children }: IComboboxListProps) => {
  const context = useComboboxContext()

  return <div className="p-1">{context.items.map((item) => children(item))}</div>
}

interface IComboboxItemProps {
  children: React.ReactNode
  value: string
}

const ComboboxItem = ({ children, value }: IComboboxItemProps) => {
  const context = useComboboxContext()
  const isSelected = context.selectedValue === value

  return (
    <button
      className="hover:bg-muted flex w-full items-center justify-between rounded-md px-2.5 py-1.5 text-[0.75rem] text-left transition-colors"
      type="button"
      onClick={() => context.onSelect(value)}
    >
      <span>{children}</span>
      {isSelected && <Check className="size-4 text-foreground" />}
    </button>
  )
}

const useComboboxContext = () => {
  const context = React.useContext(ComboboxContext)

  if (!context) {
    throw new Error('Combobox components must be used inside Combobox.')
  }

  return context
}

export {
  Combobox,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxInput,
  ComboboxItem,
  ComboboxList,
}
