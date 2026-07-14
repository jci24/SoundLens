const SUPPORTED_COMPARISON_METRICS = new Set([
  'peakAmplitudeDelta',
  'rmsAmplitudeDelta',
  'crestFactorDelta',
  'clippingSampleCountDelta',
])

export function validateDataset(dataset) {
  const failures = []

  if (!isObject(dataset)) {
    return ['dataset must be an object']
  }

  if (!Array.isArray(dataset.filePaths) || dataset.filePaths.length === 0) {
    failures.push('filePaths must contain at least one fixture path')
  } else if (dataset.filePaths.some((filePath) => typeof filePath !== 'string' || filePath.trim() === '')) {
    failures.push('filePaths must contain non-empty strings')
  }

  if (!Array.isArray(dataset.cases) || dataset.cases.length === 0) {
    failures.push('cases must contain at least one eval case')
    return failures
  }

  const seenIds = new Set()
  for (const [index, evalCase] of dataset.cases.entries()) {
    const prefix = `cases[${index}]`
    if (!isObject(evalCase)) {
      failures.push(`${prefix} must be an object`)
      continue
    }

    if (!isNonEmptyString(evalCase.id)) {
      failures.push(`${prefix}.id must be a non-empty string`)
    } else if (seenIds.has(evalCase.id)) {
      failures.push(`duplicate case id "${evalCase.id}"`)
    } else {
      seenIds.add(evalCase.id)
    }

    if (!isNonEmptyString(evalCase.question)) {
      failures.push(`${prefix}.question must be a non-empty string`)
    }

    validateStringArray(evalCase, 'requiredAnswerPhrases', prefix, failures)
    validateStringArray(evalCase, 'forbiddenAnswerPhrases', prefix, failures)
    validateStringArray(evalCase, 'forbiddenAnswerPatterns', prefix, failures)
    validateStringArray(evalCase, 'requiredLimitationPhrases', prefix, failures)
    validateStringArray(evalCase, 'requiredEvidenceTools', prefix, failures)
    validateStringArray(evalCase, 'expectedTools', prefix, failures)
    validateStringArray(evalCase, 'forbiddenTools', prefix, failures)

    if (evalCase.requiredAnswerAnyPhraseGroups !== undefined) {
      if (!Array.isArray(evalCase.requiredAnswerAnyPhraseGroups) || evalCase.requiredAnswerAnyPhraseGroups.length === 0) {
        failures.push(`${prefix}.requiredAnswerAnyPhraseGroups must contain at least one phrase group`)
      } else {
        for (const [groupIndex, group] of evalCase.requiredAnswerAnyPhraseGroups.entries()) {
          if (!Array.isArray(group) || group.length === 0 || group.some((phrase) => !isNonEmptyString(phrase))) {
            failures.push(`${prefix}.requiredAnswerAnyPhraseGroups[${groupIndex}] must contain non-empty strings`)
          }
        }
      }
    }

    for (const pattern of evalCase.forbiddenAnswerPatterns ?? []) {
      try {
        new RegExp(pattern, 'i')
      } catch {
        failures.push(`${prefix}.forbiddenAnswerPatterns contains invalid regex "${pattern}"`)
      }
    }

    if (evalCase.comparison !== undefined) {
      validateComparisonCase(evalCase, prefix, failures)
    } else if (evalCase.expectedComparison !== undefined) {
      failures.push(`${prefix}.expectedComparison requires comparison selectors`)
    }
  }

  return failures
}

export function validateRunCount(runCount) {
  return Number.isInteger(runCount) && runCount > 0
    ? []
    : ['run count must be a positive integer']
}

export function gradeComparisonSetup(evalCase, comparisonResponse, selectedPair) {
  const failures = []
  const metric = (comparisonResponse?.aggregateMetrics ?? []).find(
    (candidate) => candidate.metricKey === evalCase.comparison.metricKey,
  )

  if (!metric) {
    failures.push(`comparison metric ${evalCase.comparison.metricKey} was not returned`)
  }

  if (!selectedPair) {
    failures.push(
      `aligned pair ${evalCase.comparison.signalDisplayNameA} vs ${evalCase.comparison.signalDisplayNameB} was not returned`,
    )
  }

  const actualLimitationCodes = new Set((comparisonResponse?.limitations ?? []).map((item) => item.code))
  for (const code of evalCase.expectedComparison?.limitationCodes ?? []) {
    if (!actualLimitationCodes.has(code)) {
      failures.push(`missing comparison limitation code ${code}`)
    }
  }

  if (metric && evalCase.expectedComparison?.meanDifference !== undefined) {
    const tolerance = evalCase.expectedComparison.tolerance ?? 0
    const difference = Math.abs(metric.meanDifference - evalCase.expectedComparison.meanDifference)
    if (difference > tolerance) {
      failures.push(
        `metric ${metric.metricKey} meanDifference expected ${evalCase.expectedComparison.meanDifference} +/- ${tolerance}, received ${metric.meanDifference}`,
      )
    }
  }

  return {
    pass: failures.length === 0,
    failures,
    metric: metric ?? null,
    limitationCodes: [...actualLimitationCodes],
  }
}

export function gradeResponse(evalCase, response) {
  const failures = []
  const normalizedAnswer = normalizeText(response?.answer)
  const citedEvidence = Array.isArray(response?.citedEvidence) ? response.citedEvidence : []
  const toolsUsed = new Set(Array.isArray(response?.toolsUsed) ? response.toolsUsed : [])
  const evidenceTools = new Set(citedEvidence.map((item) => item.toolName))
  const limitations = Array.isArray(response?.limitations) ? response.limitations : []

  if (!Array.isArray(response?.citedEvidence) || response.citedEvidence.length === 0) {
    failures.push('missing cited evidence')
  }

  for (const expectedTool of evalCase.expectedTools ?? []) {
    if (!toolsUsed.has(expectedTool)) {
      failures.push(`missing tool ${expectedTool}`)
    }
  }

  for (const forbiddenTool of evalCase.forbiddenTools ?? []) {
    if (toolsUsed.has(forbiddenTool)) {
      failures.push(`forbidden tool ${forbiddenTool}`)
    }
  }

  for (const requiredTool of evalCase.requiredEvidenceTools ?? []) {
    if (!evidenceTools.has(requiredTool)) {
      failures.push(`missing evidence tool ${requiredTool}`)
    }
  }

  for (const requiredPhrase of evalCase.requiredAnswerPhrases ?? []) {
    if (!normalizedAnswer.includes(normalizeText(requiredPhrase))) {
      failures.push(`missing phrase "${requiredPhrase}"`)
    }
  }

  for (const [index, phraseGroup] of (evalCase.requiredAnswerAnyPhraseGroups ?? []).entries()) {
    if (!phraseGroup.some((phrase) => normalizedAnswer.includes(normalizeText(phrase)))) {
      failures.push(`missing any-of phrase group ${index + 1}: ${phraseGroup.join(' | ')}`)
    }
  }

  for (const forbiddenPhrase of evalCase.forbiddenAnswerPhrases ?? []) {
    if (normalizedAnswer.includes(normalizeText(forbiddenPhrase))) {
      failures.push(`forbidden phrase "${forbiddenPhrase}"`)
    }
  }

  for (const pattern of evalCase.forbiddenAnswerPatterns ?? []) {
    if (new RegExp(pattern, 'i').test(String(response?.answer ?? ''))) {
      failures.push(`forbidden answer pattern /${pattern}/i`)
    }
  }

  const normalizedLimitations = limitations.map(normalizeText)
  for (const requiredPhrase of evalCase.requiredLimitationPhrases ?? []) {
    if (!normalizedLimitations.some((limitation) => limitation.includes(normalizeText(requiredPhrase)))) {
      failures.push(`missing limitation phrase "${requiredPhrase}"`)
    }
  }

  if (Array.isArray(evalCase.expectedEvidenceSignalLabels) && evalCase.expectedEvidenceSignalLabels.length > 0) {
    const evidenceSignals = new Set(citedEvidence.map((item) => item.signalId).filter(Boolean))
    if (evidenceSignals.size < evalCase.expectedEvidenceSignalLabels.length) {
      failures.push('too few cited evidence items')
    }
  }

  if (Array.isArray(response?.nextSteps) && response.nextSteps.some(hasInternalToolName)) {
    failures.push('nextSteps leak internal tool names')
  }

  return { pass: failures.length === 0, failures }
}

export function summarize(results) {
  let passedRuns = 0
  let failedRuns = 0
  let setupFailures = 0

  for (const result of results) {
    if (result.setup && !result.setup.pass) {
      setupFailures += 1
    }

    for (const run of result.runs ?? []) {
      if (run.grading?.pass) {
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
    setupFailures,
    pass: failedRuns === 0 && setupFailures === 0,
  }
}

export function normalizeText(value) {
  return String(value ?? '').trim().toLowerCase()
}

function validateComparisonCase(evalCase, prefix, failures) {
  const comparison = evalCase.comparison
  if (!isObject(comparison)) {
    failures.push(`${prefix}.comparison must be an object`)
    return
  }

  for (const property of ['recordingAFileName', 'recordingBFileName', 'metricKey', 'signalDisplayNameA', 'signalDisplayNameB']) {
    if (!isNonEmptyString(comparison[property])) {
      failures.push(`${prefix}.comparison.${property} must be a non-empty string`)
    }
  }

  if (isNonEmptyString(comparison.metricKey) && !SUPPORTED_COMPARISON_METRICS.has(comparison.metricKey)) {
    failures.push(`${prefix}.comparison.metricKey "${comparison.metricKey}" is not supported`)
  }

  if (comparison.recordingAFileName === comparison.recordingBFileName) {
    failures.push(`${prefix}.comparison recordings must use different fixture filenames`)
  }

  const hasStart = evalCase.startTimeSeconds !== undefined && evalCase.startTimeSeconds !== null
  const hasEnd = evalCase.endTimeSeconds !== undefined && evalCase.endTimeSeconds !== null
  if (hasStart !== hasEnd) {
    failures.push(`${prefix} ROI requires both startTimeSeconds and endTimeSeconds`)
  } else if (hasStart && (!Number.isFinite(evalCase.startTimeSeconds) || !Number.isFinite(evalCase.endTimeSeconds) || evalCase.startTimeSeconds < 0 || evalCase.endTimeSeconds <= evalCase.startTimeSeconds)) {
    failures.push(`${prefix} ROI must have finite boundaries with 0 <= start < end`)
  }

  if (evalCase.expectedComparison !== undefined) {
    const expected = evalCase.expectedComparison
    if (!isObject(expected)) {
      failures.push(`${prefix}.expectedComparison must be an object`)
      return
    }

    validateStringArray(expected, 'limitationCodes', `${prefix}.expectedComparison`, failures)
    if (expected.meanDifference !== undefined && !Number.isFinite(expected.meanDifference)) {
      failures.push(`${prefix}.expectedComparison.meanDifference must be finite`)
    }
    if (expected.tolerance !== undefined && (!Number.isFinite(expected.tolerance) || expected.tolerance < 0)) {
      failures.push(`${prefix}.expectedComparison.tolerance must be zero or greater`)
    }
  }
}

function validateStringArray(owner, property, prefix, failures) {
  if (owner[property] === undefined) {
    return
  }

  if (!Array.isArray(owner[property]) || owner[property].some((value) => !isNonEmptyString(value))) {
    failures.push(`${prefix}.${property} must contain non-empty strings`)
  }
}

function hasInternalToolName(value) {
  return /\b(?:get_[a-z_]+|compare_signals)\b/i.test(String(value))
}

function isObject(value) {
  return value !== null && typeof value === 'object' && !Array.isArray(value)
}

function isNonEmptyString(value) {
  return typeof value === 'string' && value.trim() !== ''
}
