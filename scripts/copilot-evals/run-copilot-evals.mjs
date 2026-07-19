#!/usr/bin/env node

import { access, mkdir, readFile, writeFile } from 'node:fs/promises'
import path from 'node:path'
import process from 'node:process'

import {
  gradeComparisonSetup,
  gradeResponse,
  summarize,
  summarizeRouting,
  validateDataset,
  validateRunCount,
} from './copilot-eval-lib.mjs'

const DEFAULT_API_BASE_URL = process.env.SOUNDLENS_API_BASE_URL ?? 'http://localhost:5123'
const DEFAULT_RUN_COUNT = Number.parseInt(process.env.SOUNDLENS_EVAL_RUNS ?? '3', 10)
const DEFAULT_DATASET_PATH = path.resolve(process.cwd(), 'scripts/copilot-evals/copilot-eval-cases.json')

const startedAt = new Date()
let artifactPath = defaultArtifactPath(startedAt)
let artifact = createArtifact(startedAt)

try {
  const args = parseArgs(process.argv.slice(2))
  artifactPath = path.resolve(process.cwd(), args.output ?? artifactPath)
  await execute(args)
} catch (error) {
  artifact.fatalError = formatError(error)
  artifact.completedAt = new Date().toISOString()
  console.error(`\nFatal: ${artifact.fatalError}`)
  process.exitCode = 1
} finally {
  try {
    await writeArtifact(artifactPath, artifact)
    console.log(`\nArtifact: ${artifactPath}`)
  } catch (error) {
    console.error(`Could not write eval artifact: ${formatError(error)}`)
    process.exitCode = 1
  }
}

async function execute(args) {
  const datasetPath = path.resolve(process.cwd(), args.dataset ?? DEFAULT_DATASET_PATH)
  const apiBaseUrl = args.apiBaseUrl ?? DEFAULT_API_BASE_URL
  const runCount = Number.isFinite(args.runs) ? args.runs : DEFAULT_RUN_COUNT
  const dataset = JSON.parse(await readFile(datasetPath, 'utf8'))
  const validationFailures = [
    ...validateDataset(dataset),
    ...validateRunCount(runCount),
  ]

  if (validationFailures.length > 0) {
    throw new Error(`Dataset validation failed:\n- ${validationFailures.join('\n- ')}`)
  }

  const cases = args.caseId
    ? dataset.cases.filter((evalCase) => evalCase.id === args.caseId)
    : dataset.cases
  if (cases.length === 0) {
    throw new Error(`Eval case "${args.caseId}" was not found in ${datasetPath}.`)
  }

  const datasetDirectory = path.dirname(datasetPath)
  const filePaths = await resolveFixturePaths(dataset.filePaths, datasetDirectory)
  artifact.configuration = {
    apiBaseUrl,
    dataset: path.relative(process.cwd(), datasetPath),
    caseId: args.caseId ?? null,
    runCount,
    fixtureFileNames: filePaths.map((filePath) => path.basename(filePath)),
  }

  console.log(`Using API base URL: ${apiBaseUrl}`)
  console.log(`Using dataset: ${datasetPath}`)
  console.log(`Using ${filePaths.length} fixture file(s)`)
  console.log(`Executing ${cases.length} eval case(s) with ${runCount} run(s) each`)

  const importResult = await importFiles(apiBaseUrl, filePaths)
  if (!Array.isArray(importResult.succeededFiles) || importResult.succeededFiles.length === 0) {
    throw new Error('No files were imported; cannot run copilot evals.')
  }

  const waveformResponse = await fetchJson(`${apiBaseUrl}/api/waveforms/time`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      binCount: 64,
      selectedSignalIds: null,
      startTimeSeconds: null,
      endTimeSeconds: null,
    }),
  })
  const recordingsByFileName = new Map(
    (waveformResponse.recordings ?? []).map((recording) => [recording.fileName, recording]),
  )
  const signalMap = buildSignalMap(waveformResponse.recordings ?? [])
  const results = []

  for (const evalCase of cases) {
    console.log(`\nCase: ${evalCase.id}`)
    const caseResult = {
      id: evalCase.id,
      expectedAnswerMode: evalCase.expectedAnswerMode ?? null,
      prompt: evalCase.question,
      metadata: evalCase.comparison ?? null,
      resolvedContext: null,
      setup: { pass: true, failures: [] },
      runs: [],
    }

    let query
    try {
      query = evalCase.comparison
        ? await prepareComparisonQuery(evalCase, recordingsByFileName, apiBaseUrl)
        : prepareGenericQuery(evalCase, signalMap)
      caseResult.resolvedContext = query.resolvedContext
      caseResult.setup = query.setup
    } catch (error) {
      caseResult.setup = { pass: false, failures: [formatError(error)] }
    }

    if (!caseResult.setup.pass) {
      console.log(`  setup: FAIL (${caseResult.setup.failures.join('; ')})`)
      results.push(caseResult)
      continue
    }

    for (let index = 0; index < runCount; index += 1) {
      try {
        const response = await fetchJson(`${apiBaseUrl}/api/agent/query`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(query.request),
        })
        const grading = gradeResponse(evalCase, response)
        caseResult.runs.push({ run: index + 1, response, grading })
        console.log(`  run ${index + 1}: ${grading.pass ? 'PASS' : 'FAIL'} (${grading.failures.join('; ') || 'all checks passed'})`)
      } catch (error) {
        const grading = { pass: false, failures: [formatError(error)] }
        caseResult.runs.push({ run: index + 1, response: null, grading })
        console.log(`  run ${index + 1}: FAIL (${grading.failures[0]})`)
      }
    }

    results.push(caseResult)
  }

  const summary = summarize(results)
  const routingSummary = summarizeRouting(results)
  artifact.results = results
  artifact.summary = summary
  artifact.routingSummary = routingSummary
  artifact.completedAt = new Date().toISOString()

  console.log('\nSummary')
  console.log(`  cases: ${summary.caseCount}`)
  console.log(`  setup failures: ${summary.setupFailures}`)
  console.log(`  runs: ${summary.runCount}`)
  console.log(`  passed: ${summary.passedRuns}`)
  console.log(`  failed: ${summary.failedRuns}`)
  if (routingSummary.evaluatedRuns > 0) {
    console.log(`  routing accuracy: ${(routingSummary.accuracy * 100).toFixed(1)}% (${routingSummary.correctRuns}/${routingSummary.evaluatedRuns})`)
  }

  if (!summary.pass) {
    process.exitCode = 1
  }
}

async function prepareComparisonQuery(evalCase, recordingsByFileName, apiBaseUrl) {
  const selectors = evalCase.comparison
  const recordingA = recordingsByFileName.get(selectors.recordingAFileName)
  const recordingB = recordingsByFileName.get(selectors.recordingBFileName)
  if (!recordingA || !recordingB) {
    const missing = [
      !recordingA ? selectors.recordingAFileName : null,
      !recordingB ? selectors.recordingBFileName : null,
    ].filter(Boolean)
    throw new Error(`Comparison recording not found: ${missing.join(', ')}`)
  }

  const comparisonResponse = await fetchJson(`${apiBaseUrl}/api/comparisons/recordings`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      recordingIdA: recordingA.recordingId,
      recordingIdB: recordingB.recordingId,
      startTimeSeconds: evalCase.startTimeSeconds ?? null,
      endTimeSeconds: evalCase.endTimeSeconds ?? null,
    }),
  })
  const selectedPair = (comparisonResponse.alignedSignals ?? []).find(
    (pair) => pair.displayNameA === selectors.signalDisplayNameA && pair.displayNameB === selectors.signalDisplayNameB,
  )
  const setup = gradeComparisonSetup(evalCase, comparisonResponse, selectedPair)
  const resolvedContext = {
    recordingIdA: recordingA.recordingId,
    recordingFileNameA: recordingA.fileName,
    recordingIdB: recordingB.recordingId,
    recordingFileNameB: recordingB.fileName,
    metricKey: selectors.metricKey,
    signalIdA: selectedPair?.signalIdA ?? null,
    signalDisplayNameA: selectedPair?.displayNameA ?? selectors.signalDisplayNameA,
    signalIdB: selectedPair?.signalIdB ?? null,
    signalDisplayNameB: selectedPair?.displayNameB ?? selectors.signalDisplayNameB,
    regionOfInterest: comparisonResponse.regionOfInterest ?? null,
    deterministicMetric: setup.metric,
    comparisonLimitations: comparisonResponse.limitations ?? [],
  }

  return {
    setup,
    resolvedContext,
    request: {
      contextMode: evalCase.contextMode ?? 'auto',
      question: evalCase.question,
      signalIds: selectedPair ? [selectedPair.signalIdA, selectedPair.signalIdB] : [],
      startTimeSeconds: evalCase.startTimeSeconds ?? null,
      endTimeSeconds: evalCase.endTimeSeconds ?? null,
      comparisonContext: selectedPair ? {
        recordingIdA: recordingA.recordingId,
        recordingIdB: recordingB.recordingId,
        metricKey: selectors.metricKey,
        signalIdA: selectedPair.signalIdA,
        signalIdB: selectedPair.signalIdB,
      } : null,
    },
  }
}

function prepareGenericQuery(evalCase, signalMap) {
  const signalIds = resolveSignalIds(evalCase, signalMap)
  return {
    setup: { pass: true, failures: [] },
    resolvedContext: { signalIds },
    request: {
      contextMode: evalCase.contextMode ?? 'auto',
      question: evalCase.question,
      signalIds,
      startTimeSeconds: evalCase.startTimeSeconds ?? null,
      endTimeSeconds: evalCase.endTimeSeconds ?? null,
    },
  }
}

function parseArgs(argv) {
  const parsed = {}
  const valueFlags = new Map([
    ['--dataset', 'dataset'],
    ['--api-base-url', 'apiBaseUrl'],
    ['--case', 'caseId'],
    ['--output', 'output'],
  ])

  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index]
    if (valueFlags.has(arg)) {
      const value = argv[index + 1]
      if (!value || value.startsWith('--')) {
        throw new Error(`${arg} requires a value.`)
      }
      parsed[valueFlags.get(arg)] = value
      index += 1
      continue
    }

    if (arg === '--runs') {
      parsed.runs = Number.parseInt(argv[index + 1], 10)
      index += 1
      continue
    }

    throw new Error(`Unknown argument: ${arg}`)
  }

  return parsed
}

async function importFiles(apiBaseUrl, filePaths) {
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
  const resolvedPaths = []
  for (const filePath of filePaths) {
    const resolvedPath = path.isAbsolute(filePath) ? filePath : path.resolve(datasetDirectory, filePath)
    await access(resolvedPath)
    resolvedPaths.push(resolvedPath)
  }
  return resolvedPaths
}

function buildSignalMap(recordings) {
  const signalMap = new Map()
  for (const recording of recordings) {
    for (const signal of recording.signals ?? []) {
      signalMap.set(`${recording.fileName}::${signal.displayName}`, signal.signalId)
    }
  }
  return signalMap
}

function resolveSignalIds(evalCase, signalMap) {
  if (!Array.isArray(evalCase.signals)) {
    return null
  }

  if (evalCase.signals.length === 0) {
    return [...signalMap.values()]
  }

  return evalCase.signals.map((signal) => {
    const signalId = signalMap.get(`${signal.fileName}::${signal.displayName}`)
    if (!signalId) {
      throw new Error(`Signal not found for ${signal.fileName} · ${signal.displayName}`)
    }
    return signalId
  })
}

function createArtifact(date) {
  return {
    schemaVersion: 2,
    startedAt: date.toISOString(),
    completedAt: null,
    configuration: null,
    results: [],
    summary: null,
    routingSummary: null,
    fatalError: null,
  }
}

function defaultArtifactPath(date) {
  const timestamp = date.toISOString().replaceAll(':', '-').replaceAll('.', '-')
  return path.resolve(process.cwd(), `artifacts/copilot-evals/copilot-evals-${timestamp}.json`)
}

async function writeArtifact(outputPath, value) {
  await mkdir(path.dirname(outputPath), { recursive: true })
  await writeFile(outputPath, `${JSON.stringify(value, null, 2)}\n`)
}

function formatError(error) {
  return error instanceof Error ? error.message : String(error)
}
