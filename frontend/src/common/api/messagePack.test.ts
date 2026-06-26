import { describe, expect, it } from 'vitest'
import { encode } from '@msgpack/msgpack'

import { readMessagePack } from './messagePack'

describe('messagePack', () => {
  it('normalizes decoded C# PascalCase payloads to the frontend camelCase contract', async () => {
    const payload = {
      RequestedBinCount: 128,
      SelectedSignals: [
        {
          SignalId: 'signal-1',
          Metrics: {
            PeakAmplitude: 1,
          },
        },
      ],
      YAxis: {
        Maximum: 1,
      },
    }

    const encoded = encode(payload)
    const response = new Response(encoded, {
      headers: {
        'content-type': 'application/x-msgpack',
      },
    })

    const result = await readMessagePack<{
      requestedBinCount: number
      selectedSignals: Array<{ signalId: string; metrics: { peakAmplitude: number } }>
      yAxis: { maximum: number }
    }>(response)

    expect(result.requestedBinCount).toBe(128)
    expect(result.selectedSignals[0]?.signalId).toBe('signal-1')
    expect(result.selectedSignals[0]?.metrics.peakAmplitude).toBe(1)
    expect(result.yAxis.maximum).toBe(1)
  })
})
