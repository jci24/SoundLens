#!/usr/bin/env node

import { mkdir, writeFile } from 'node:fs/promises'
import path from 'node:path'
import process from 'node:process'

const outputDirectory = path.resolve(process.cwd(), 'scripts/copilot-evals/fixtures')

await mkdir(outputDirectory, { recursive: true })

await writeFixture('eval-quiet.wav', createMono16BitWav(8_000, new Int16Array(16).fill(8_192)))
await writeFixture('eval-quiet-copy.wav', createMono16BitWav(8_000, new Int16Array(16).fill(8_192)))
await writeFixture('eval-loud.wav', createMono16BitWav(8_000, new Int16Array(16).fill(16_384)))
await writeFixture('eval-clipped.wav', createMono16BitWav(8_000, Int16Array.from([32_767, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0])))
await writeFixture('eval-stereo.wav', createStereo16BitWav(
  8_000,
  new Int16Array(16).fill(8_192),
  new Int16Array(16).fill(12_288),
))
await writeFixture('eval-stereo-copy.wav', createStereo16BitWav(
  8_000,
  new Int16Array(16).fill(8_192),
  new Int16Array(16).fill(12_288),
))
await writeFixture('eval-stereo-crossed.wav', createStereo16BitWav(
  8_000,
  new Int16Array(16).fill(16_384),
  new Int16Array(16).fill(8_192),
))
await writeFixture('eval-silent-stereo.wav', createStereo16BitWav(
  8_000,
  new Int16Array(16),
  new Int16Array(16),
))
await writeFixture('eval-silent-stereo-copy.wav', createStereo16BitWav(
  8_000,
  new Int16Array(16),
  new Int16Array(16),
))

console.log(`Fixtures written to ${outputDirectory}`)

async function writeFixture(fileName, bytes) {
  await writeFile(path.join(outputDirectory, fileName), bytes)
}

function createMono16BitWav(sampleRate, samples) {
  return createInterleaved16BitWav(sampleRate, [samples])
}

function createStereo16BitWav(sampleRate, channelA, channelB) {
  if (channelA.length !== channelB.length) {
    throw new Error('Stereo fixture channels must have the same sample count.')
  }

  return createInterleaved16BitWav(sampleRate, [channelA, channelB])
}

function createInterleaved16BitWav(sampleRate, channels) {
  const bytesPerSample = 2
  const channelCount = channels.length
  const blockAlign = channelCount * bytesPerSample
  const byteRate = sampleRate * blockAlign
  const frameCount = channels[0].length
  const dataSize = frameCount * blockAlign
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

  for (let frameIndex = 0; frameIndex < frameCount; frameIndex += 1) {
    for (let channelIndex = 0; channelIndex < channelCount; channelIndex += 1) {
      const offset = 44 + (frameIndex * channelCount + channelIndex) * bytesPerSample
      buffer.writeInt16LE(channels[channelIndex][frameIndex], offset)
    }
  }

  return buffer
}
