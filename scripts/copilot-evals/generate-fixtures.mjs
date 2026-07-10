#!/usr/bin/env node

import { mkdir, writeFile } from 'node:fs/promises'
import path from 'node:path'
import process from 'node:process'

const outputDirectory = path.resolve(process.cwd(), 'scripts/copilot-evals/fixtures')

await mkdir(outputDirectory, { recursive: true })

await writeFixture('eval-quiet.wav', createMono16BitWav(8_000, new Int16Array(16).fill(8_192)))
await writeFixture('eval-loud.wav', createMono16BitWav(8_000, new Int16Array(16).fill(16_384)))
await writeFixture('eval-clipped.wav', createMono16BitWav(8_000, Int16Array.from([32_767, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0])))

console.log(`Fixtures written to ${outputDirectory}`)

async function writeFixture(fileName, bytes) {
  await writeFile(path.join(outputDirectory, fileName), bytes)
}

function createMono16BitWav(sampleRate, samples) {
  const bytesPerSample = 2
  const channelCount = 1
  const blockAlign = channelCount * bytesPerSample
  const byteRate = sampleRate * blockAlign
  const dataSize = samples.length * blockAlign
  const buffer = Buffer.alloc(44 + dataSize)

  buffer.write('RIFF', 0)
  buffer.writeUInt32LE(36 + dataSize, 4)
  buffer.write('WAVE', 8)
  buffer.write('fmt ', 12)
  buffer.writeUInt32LE(16, 16)
  buffer.writeUInt16LE(1, 20)
  buffer.writeUInt16LE(channelCount, 22)
  buffer.writeUInt32LE(sampleRate, 24)
  buffer.writeUInt32LE(byteRate, 28)
  buffer.writeUInt16LE(blockAlign, 32)
  buffer.writeUInt16LE(bytesPerSample * 8, 34)
  buffer.write('data', 36)
  buffer.writeUInt32LE(dataSize, 40)

  for (let index = 0; index < samples.length; index += 1) {
    buffer.writeInt16LE(samples[index], 44 + index * bytesPerSample)
  }

  return buffer
}
