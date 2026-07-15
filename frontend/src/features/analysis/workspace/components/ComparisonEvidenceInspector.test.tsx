import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { useState } from 'react'
import { ComparisonEvidenceInspector } from './ComparisonEvidenceInspector'
import type { IComparisonCoverageSummary } from '../../utils/comparisonEvidence'
import type { IRecordingComparisonMetricAggregate, IRecordingComparisonSignalObservation } from '../../types'

const metric: IRecordingComparisonMetricAggregate = {
  metricKey: 'clippingSampleCountDelta',
  unit: 'samples',
  comparedPairCount: 2,
  missingValueCount: 0,
  meanDifference: 0,
  medianDifference: 0,
  minimumDifference: 0,
  maximumDifference: 0,
  spread: 0,
}

const observation: IRecordingComparisonSignalObservation = {
  signalIdA: 'signal-a',
  displayNameA: 'Channel 1',
  channelIndexA: 0,
  signalIdB: 'signal-b',
  displayNameB: 'Channel 1',
  channelIndexB: 0,
  basis: 'DisplayName',
  peakAmplitudeA: 0.8,
  peakAmplitudeB: 0.8,
  peakAmplitudeDelta: 0,
  rmsAmplitudeA: 0.4,
  rmsAmplitudeB: 0.4,
  rmsAmplitudeDelta: 0,
  crestFactorA: 2,
  crestFactorB: 2,
  crestFactorDelta: 0,
  clippingSampleCountA: 0,
  clippingSampleCountB: 0,
  clippingSampleCountDelta: 0,
  hasClippingA: false,
  hasClippingB: false,
}

const coverageSummary: IComparisonCoverageSummary = {
  alignedPairCount: 2,
  comparedPairCount: 2,
  copy: 'The selected metric is supported by the currently aligned evidence set.',
  label: 'Stronger evidence',
  limitationCount: 0,
  missingValueCount: 0,
  tone: 'strong',
}

describe('ComparisonEvidenceInspector', () => {
  it('renders zero-valued backend evidence, scope, and an empty limitation state', () => {
    render(
      <ComparisonEvidenceInspector
        activeMetric={metric}
        activeObservation={observation}
        coverageSummary={coverageSummary}
        fileNameA="a-very-long-recording-name-for-product-a.wav"
        fileNameB="product-b.wav"
        isOpen
        limitations={[]}
        onOpenChange={vi.fn()}
        preventOutsideDismiss={false}
        returnFocusRef={{ current: null }}
        roiScopeLabel={null}
      />
    )

    const inspector = screen.getByRole('dialog', { name: 'Clipping samples' })
    expect(inspector).toHaveTextContent('a-very-long-recording-name-for-product-a.wav')
    expect(inspector).toHaveTextContent('Full duration')
    expect(inspector).toHaveTextContent('Mean delta A-B0 samples')
    expect(inspector).toHaveTextContent('Compare A0 samples')
    expect(inspector).toHaveTextContent('No additional comparison limitations were reported.')
  })

  it('closes on Escape and returns focus to the invoking control', async () => {
    const onOpenChange = vi.fn()
    const trigger = document.createElement('button')
    trigger.textContent = 'Evidence'
    document.body.append(trigger)
    trigger.focus()

    const InspectorHarness = () => {
      const [isOpen, setIsOpen] = useState(true)

      return (
        <ComparisonEvidenceInspector
          activeMetric={metric}
          activeObservation={observation}
          coverageSummary={coverageSummary}
          fileNameA="product-a.wav"
          fileNameB="product-b.wav"
          isOpen={isOpen}
          limitations={[]}
          onOpenChange={(nextIsOpen) => {
            onOpenChange(nextIsOpen)
            setIsOpen(nextIsOpen)
          }}
          preventOutsideDismiss={false}
          returnFocusRef={{ current: trigger }}
          roiScopeLabel="0.20 s to 0.80 s · 0.60 s"
        />
      )
    }

    render(<InspectorHarness />)

    fireEvent.keyDown(document, { key: 'Escape' })

    expect(onOpenChange).toHaveBeenCalledWith(false)
    await waitFor(() => expect(trigger).toHaveFocus())
    trigger.remove()
  })

  it('requests dismissal when the user interacts outside the non-modal inspector', async () => {
    const onOpenChange = vi.fn()
    const outsideControl = document.createElement('button')
    outsideControl.textContent = 'Outside control'
    document.body.append(outsideControl)

    render(
      <ComparisonEvidenceInspector
        activeMetric={metric}
        activeObservation={observation}
        coverageSummary={coverageSummary}
        fileNameA="product-a.wav"
        fileNameB="product-b.wav"
        isOpen
        limitations={[]}
        onOpenChange={onOpenChange}
        preventOutsideDismiss={false}
        returnFocusRef={{ current: null }}
        roiScopeLabel={null}
      />
    )

    await new Promise((resolve) => window.setTimeout(resolve, 0))
    fireEvent.pointerDown(outsideControl, { button: 0, pointerType: 'mouse' })
    fireEvent.click(outsideControl)

    await waitFor(() => expect(onOpenChange).toHaveBeenCalledWith(false))
    outsideControl.remove()
  })

  it('ignores outside dismissal while Copilot hands off to the inspector', async () => {
    const onOpenChange = vi.fn()
    const outsideControl = document.createElement('button')
    document.body.append(outsideControl)

    render(
      <ComparisonEvidenceInspector
        activeMetric={metric}
        activeObservation={observation}
        coverageSummary={coverageSummary}
        fileNameA="product-a.wav"
        fileNameB="product-b.wav"
        isOpen
        limitations={[]}
        onOpenChange={onOpenChange}
        preventOutsideDismiss
        returnFocusRef={{ current: null }}
        roiScopeLabel={null}
      />
    )

    await new Promise((resolve) => window.setTimeout(resolve, 0))
    fireEvent.pointerDown(outsideControl, { button: 0, pointerType: 'mouse' })
    fireEvent.click(outsideControl)

    expect(onOpenChange).not.toHaveBeenCalled()
    outsideControl.remove()
  })

  it('does not dismiss when focus transfers from Copilot or elsewhere in the workspace', async () => {
    const onOpenChange = vi.fn()
    const outsideControl = document.createElement('button')
    document.body.append(outsideControl)

    render(
      <ComparisonEvidenceInspector
        activeMetric={metric}
        activeObservation={observation}
        coverageSummary={coverageSummary}
        fileNameA="product-a.wav"
        fileNameB="product-b.wav"
        isOpen
        limitations={[]}
        onOpenChange={onOpenChange}
        preventOutsideDismiss={false}
        returnFocusRef={{ current: null }}
        roiScopeLabel={null}
      />
    )

    outsideControl.focus()
    await new Promise((resolve) => window.setTimeout(resolve, 0))

    expect(onOpenChange).not.toHaveBeenCalled()
    expect(screen.getByRole('dialog', { name: 'Clipping samples' })).toBeInTheDocument()
    outsideControl.remove()
  })
})
