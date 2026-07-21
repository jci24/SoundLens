interface ISonaIconProps {
  className?: string
}

const SonaIcon = ({ className }: ISonaIconProps) => (
  <svg
    aria-hidden="true"
    className={className}
    fill="none"
    viewBox="0 0 44 24"
    xmlns="http://www.w3.org/2000/svg"
  >
    <path d="M2 10v4" stroke="currentColor" strokeLinecap="round" strokeWidth="3" />
    <path
      d="M8 8v8c0 3 1.4 5 3.8 5 3.1 0 4.1-3.2 5-7.5l.9-4.3C18.5 5.1 20 3 22.5 3c2.8 0 4.2 2.5 5.1 6.8l.9 4.2c.9 4.1 1.9 7 4.9 7 2.4 0 3.6-2 3.6-5V8"
      stroke="currentColor"
      strokeLinecap="round"
      strokeLinejoin="round"
      strokeWidth="3"
    />
    <path d="M42 10v4" stroke="currentColor" strokeLinecap="round" strokeWidth="3" />
  </svg>
)

export { SonaIcon }
