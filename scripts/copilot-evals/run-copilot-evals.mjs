#!/usr/bin/env node

import { access, readFile } from 'node:fs/promises'
import path from 'node:path'
import process from 'node:process'

const DEFAULT_API_BASE_URL = process.env.SOUNDLENS_API_BASE_URL ?? 'http://localhost:5123'
const DEFAULT_RUN_COUNT = Number.parseInt(process.env.SOUNDLENS_EVAL_RUNS ?? '3', 10)
const DEFAULT_DATASET_PATH = path.resolve(process.cwd(), 'scripts/copilot-evals/copilot-eval-cases.json')

const args = parseArgs(process.argv.slice(2))
const datasetPath = path.resolve(process.cwd(), args.dataset ?? DEFAULT_DATASET_PATH)
const apiBaseUrl = args.apiBaseUrl ?? DEFAULT_API_BASE_URL
const runCount = Number.isFinite(args.runs) ? args.runs : DEFAULT_RUN_COUNT

const dataset = JSON.parse(await readFile(datasetPath, 'utf8'))
const datasetDirectory = path.dirname(datasetPath)

if (!Array.isArray(dataset.cases) || dataset.cases.length === 0) {
  throw new Error(`No eval cases found in ${datasetPath}.`)
}

const filePaths = await resolveFixturePaths(dataset.filePaths, datasetDirectory)

console.log(`Using API base URL: ${apiBaseUrl}`)
console.log(`Using dataset: ${datasetPath}`)
console.log(`Using ${filePaths.length} fixture file(s)`)
console.log(`Executing ${dataset.cases.length} eval case(s) with ${runCount} run(s) each`)

const importResult = await importFiles(apiBaseUrl, filePaths)
const fileMap = new Map(importResult.succeededFiles.map((file) => [file.fileName, file]))

if (fileMap.size === 0) {
  throw new Error('No files were imported; cannot run copilot evals.')
}

const waveformResponse = await fetchJson(`${apiBaseUrl}/api/waveforms`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    requestedBinCount: 8,
    selectedSignalIds: null,
    startTimeSeconds: null,
    endTimeSeconds: null,
  }),
})

const signalMap = new Map()
for (const recording of waveformResponse.recordings ?? []) {
  for (const signal of recording.signals ?? []) {
    signalMap.set(`${recording.fileName}::${signal.displayName}`, signal.signalId)
  }
}

const results = []

for (const evalCase of dataset.cases) {
  console.log(`\nCase: ${evalCase.id}`)
  const signalIds = resolveSignalIds(evalCase, signalMap)
  const caseRuns = []

  for (let index = 0; index < runCount; index += 1) {
    const response = await fetchJson(`${apiBaseUrl}/api/agent/query`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        question: evalCase.question,
        signalIds,
        startTimeSeconds: evalCase.startTimeSeconds ?? null,
        endTimeSeconds: evalCase.endTimeSeconds ?? null,
      }),
    })

    const grading = gradeResponse(evalCase, response)
    caseRuns.push({ response, grading })
    console.log(`  run ${index + 1}: ${grading.pass ? 'PASS' : 'FAIL'} (${grading.failures.join('; ') || 'all checks passed'})`)
  }

  results.push({
    id: evalCase.id,
    prompt: evalCase.question,
    signalIds,
    runs: caseRuns,
  })
}

const summary = summarize(results)

console.log('\nSummary')
console.log(`  cases: ${summary.caseCount}`)
console.log(`  runs: ${summary.runCount}`)
console.log(`  passed: ${summary.passedRuns}`)
console.log(`  failed: ${summary.failedRuns}`)

if (summary.failedRuns > 0) {
  process.exitCode = 1
}

function parseArgs(argv) {
  const parsed = {}

  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index]
    if (arg === '--dataset') {
      parsed.dataset = argv[index + 1]
      index += 1
      continue
    }

    if (arg === '--api-base-url') {
      parsed.apiBaseUrl = argv[index + 1]
      index += 1
      continue
    }

    if (arg === '--runs') {
      parsed.runs = Number.parseInt(argv[index + 1], 10)
      index += 1
    }
  }

  return parsed
}

async function importFiles(apiBaseUrl, filePaths) {
  if (!Array.isArray(filePaths) || filePaths.length === 0) {
    throw new Error('Dataset must include at least one file path.')
  }

  return fetchJson(`${apiBaseUrl}/api/import`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ filePaths }),
  })
}

async function fetchJson(url, init) {
  const response = await fetch(url, init)
  if (!response.ok) {
    const body = await response.text()
    throw new Error(`Request failed (${response.status}) ${url}: ${body}`)
  }

  return response.json()
}

async function resolveFixturePaths(filePaths, datasetDirectory) {
  if (!Array.isArray(filePaths) || filePaths.length === 0) {
    throw new Error('Dataset must include at least one file path.')
  }

  const resolvedPaths = []

  for (const filePath of filePaths) {
    const resolvedPath = path.isAbsolute(filePath)
      ? filePath
      : path.resolve(datasetDirectory, filePath)

    await access(resolvedPath)
    resolvedPaths.push(resolvedPath)
  }

  return resolvedPaths
}

function resolveSignalIds(evalCase, signalMap) {
  if (!Array.isArray(evalCase.signals) || evalCase.signals.length === 0) {
    return null
  }

  return evalCase.signals.map((signal) => {
    const signalId = signalMap.get(`${signal.fileName}::${signal.displayName}`)
    if (!signalId) {
      throw new Error(`Signal not found for ${signal.fileName} · ${signal.displayName}`)
    }

    return signalId
  })
}

function gradeResponse(evalCase, response) {
  const failures = []
  const normalizedAnswer = normalizeText(response.answer)
  const toolSet = new Set(response.toolsUsed ?? [])

  if (!Array.isArray(response.limitations) || !response.limitations.some((item) => item.includes('dBFS'))) {
    failures.push('missing dBFS limitation')
  }

  if (!Array.isArray(response.citedEvidence) || response.citedEvidence.length === 0) {
    failures.push('missing cited evidence')
  }

  for (const expectedTool of evalCase.expectedTools ?? []) {
    if (!toolSet.has(expectedTool)) {
      failures.push(`missing tool ${expectedTool}`)
    }
  }

  for (const forbiddenTool of evalCase.forbiddenTools ?? []) {
    if (toolSet.has(forbiddenTool)) {
      failures.push(`forbidden tool ${forbiddenTool}`)
    }
  }

  for (const requiredPhrase of evalCase.requiredAnswerPhrases ?? []) {
    if (!normalizedAnswer.includes(normalizeText(requiredPhrase))) {
      failures.push(`missing phrase "${requiredPhrase}"`)
    }
  }

  for (const forbiddenPhrase of evalCase.forbiddenAnswerPhrases ?? []) {
    if (normalizedAnswer.includes(normalizeText(forbiddenPhrase))) {
      failures.push(`forbidden phrase "${forbiddenPhrase}"`)
    }
  }

  if (Array.isArray(evalCase.expectedEvidenceSignalLabels) && evalCase.expectedEvidenceSignalLabels.length > 0) {
    const evidenceSignals = new Set((response.citedEvidence ?? []).map((item) => item.signalId))
    if (evidenceSignals.size < evalCase.expectedEvidenceSignalLabels.length) {
      failures.push('too few cited evidence items')
    }
  }

  if (Array.isArray(response.nextSteps) && response.nextSteps.some((step) => step.includes('get_') || step.includes('compare_signals'))) {
    failures.push('nextSteps leak internal tool names')
  }

  return {
    pass: failures.length === 0,
    failures,
  }
}

function summarize(results) {
  let passedRuns = 0
  let failedRuns = 0

  for (const result of results) {
    for (const run of result.runs) {
      if (run.grading.pass) {
        passedRuns += 1
      } else {
        failedRuns += 1
      }
    }
  }

  return {
    caseCount: results.length,
    runCount: passedRuns + failedRuns,
    passedRuns,
    failedRuns,
  }
}

function normalizeText(value) {
  return String(value ?? '')
    .trim()
    .toLowerCase()
}
