const SUPPORTED_COMPARISON_METRICS = new Set([
  'peakAmplitudeDelta',
  'rmsAmplitudeDelta',
  'crestFactorDelta',
  'clippingSampleCountDelta',
])
const SUPPORTED_ANSWER_MODES = new Set(['workspace', 'general', 'web', 'guidance'])
const SUPPORTED_CONTEXT_MODES = new Set(['auto', 'workspace', 'general'])
const SUPPORTED_EXPECTATIONS = new Set(['required', 'forbidden', 'optional'])
const SUPPORTED_SUFFICIENCY_STATUSES = new Set(['supported', 'partial', 'missing', 'contradicted', 'unavailable'])
const SUPPORTED_OBSERVATION_STATUSES = new Set(['complete', 'limited', 'mixed'])
const SUPPORTED_PLAN_EXPECTATIONS = new Set(['required', 'forbidden', 'optional'])
const SUPPORTED_PLAN_CATEGORIES = new Set(['analysis', 'inspection', 'audition', 'artifact'])
const SUPPORTED_PLAN_COST_CLASSES = new Set(['interactive', 'bounded'])
const SUPPORTED_PLAN_CAPABILITIES = new Set([
  'waveform',
  'spectrum',
  'level_dynamics',
  'roi',
  'playback',
  'evidence_inspector',
  'report_export',
])
const PLAN_ID_PATTERN = /^plan_v1_[0-9a-f]{24}$/
const MEASURED_RESULT_PATTERN = /[-+]?\d+(?:\.\d+)?\s*(?:dB(?:FS|\s*SPL)?|FS|Hz|kHz|samples?|ratio|%)\b/i

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
    validateStringArray(evalCase, 'forbiddenLimitationPhrases', prefix, failures)
    validateStringArray(evalCase, 'requiredEvidenceTools', prefix, failures)
    validateStringArray(evalCase, 'expectedTools', prefix, failures)
    validateStringArray(evalCase, 'forbiddenTools', prefix, failures)

    if (evalCase.expectedAnswerMode !== undefined && !SUPPORTED_ANSWER_MODES.has(evalCase.expectedAnswerMode)) {
      failures.push(`${prefix}.expectedAnswerMode must be workspace, general, web, or guidance`)
    }
    if (evalCase.contextMode !== undefined && !SUPPORTED_CONTEXT_MODES.has(evalCase.contextMode)) {
      failures.push(`${prefix}.contextMode must be auto, workspace, or general`)
    }
    validateExpectation(evalCase, 'evidenceExpectation', prefix, failures)
    validateExpectation(evalCase, 'externalCitationExpectation', prefix, failures)
    if (evalCase.investigationPlanExpectation !== undefined &&
        !SUPPORTED_PLAN_EXPECTATIONS.has(evalCase.investigationPlanExpectation)) {
      failures.push(`${prefix}.investigationPlanExpectation must be required, forbidden, or optional`)
    }
    validateStringArray(evalCase, 'expectedPlanCapabilityIds', prefix, failures)
    for (const capabilityId of evalCase.expectedPlanCapabilityIds ?? []) {
      if (!SUPPORTED_PLAN_CAPABILITIES.has(capabilityId)) {
        failures.push(`${prefix}.expectedPlanCapabilityIds contains unsupported capability "${capabilityId}"`)
      }
    }
    if (evalCase.expectedEvidenceSufficiencyStatus !== undefined &&
        !SUPPORTED_SUFFICIENCY_STATUSES.has(evalCase.expectedEvidenceSufficiencyStatus)) {
      failures.push(`${prefix}.expectedEvidenceSufficiencyStatus is not supported`)
    }
    if (evalCase.expectedEvidenceIntent !== undefined && !isNonEmptyString(evalCase.expectedEvidenceIntent)) {
      failures.push(`${prefix}.expectedEvidenceIntent must be a non-empty string`)
    }
    if (evalCase.expectedObservationStatus !== undefined &&
        !SUPPORTED_OBSERVATION_STATUSES.has(evalCase.expectedObservationStatus)) {
      failures.push(`${prefix}.expectedObservationStatus is not supported`)
    }
    if (evalCase.expectedFindingObservationCount !== undefined &&
        (!Number.isInteger(evalCase.expectedFindingObservationCount) || evalCase.expectedFindingObservationCount < 0)) {
      failures.push(`${prefix}.expectedFindingObservationCount must be a non-negative integer`)
    }

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
  const externalCitations = Array.isArray(response?.externalCitations) ? response.externalCitations : []
  const evidenceExpectation = evalCase.evidenceExpectation ?? 'required'
  const externalCitationExpectation = evalCase.externalCitationExpectation ?? 'optional'
  const planExpectation = evalCase.investigationPlanExpectation ?? 'optional'

  if (evalCase.expectedAnswerMode && response?.answerMode !== evalCase.expectedAnswerMode) {
    failures.push(`expected answer mode ${evalCase.expectedAnswerMode}, received ${response?.answerMode ?? 'missing'}`)
  }

  if (evalCase.expectedEvidenceSufficiencyStatus &&
      response?.evidenceSufficiency?.status !== evalCase.expectedEvidenceSufficiencyStatus) {
    failures.push(
      `expected evidence sufficiency ${evalCase.expectedEvidenceSufficiencyStatus}, received ${response?.evidenceSufficiency?.status ?? 'missing'}`,
    )
  }
  if (evalCase.expectedEvidenceIntent &&
      response?.evidenceSufficiency?.intent !== evalCase.expectedEvidenceIntent) {
    failures.push(
      `expected evidence intent ${evalCase.expectedEvidenceIntent}, received ${response?.evidenceSufficiency?.intent ?? 'missing'}`,
    )
  }

  const structuredObservations = Array.isArray(response?.structuredObservations)
    ? response.structuredObservations
    : []
  if (evalCase.expectedObservationStatus) {
    const metricObservations = structuredObservations.filter((observation) =>
      observation?.kind === 'comparison_metric')
    if (metricObservations.length !== 1) {
      failures.push(`expected one comparison metric observation, received ${metricObservations.length}`)
    } else if (metricObservations[0]?.status !== evalCase.expectedObservationStatus) {
      failures.push(
        `expected observation status ${evalCase.expectedObservationStatus}, received ${metricObservations[0]?.status ?? 'missing'}`,
      )
    }
  }
  if (evalCase.expectedFindingObservationCount !== undefined) {
    const findingCount = structuredObservations.filter((observation) =>
      observation?.kind === 'signal_finding').length
    if (findingCount !== evalCase.expectedFindingObservationCount) {
      failures.push(
        `expected ${evalCase.expectedFindingObservationCount} finding observations, received ${findingCount}`,
      )
    }
  }

  const planFailures = gradeInvestigationPlan(response?.investigationPlan)
  if (planExpectation === 'required' && !response?.investigationPlan) {
    failures.push('missing investigation plan')
  } else if (planExpectation === 'forbidden' && response?.investigationPlan) {
    failures.push('unexpected investigation plan')
  }
  if (response?.investigationPlan) {
    failures.push(...planFailures)
    const capabilityIds = new Set(
      Array.isArray(response.investigationPlan.steps)
        ? response.investigationPlan.steps.map((step) => step?.capabilityId).filter(Boolean)
        : [],
    )
    for (const capabilityId of evalCase.expectedPlanCapabilityIds ?? []) {
      if (!capabilityIds.has(capabilityId)) {
        failures.push(`missing investigation-plan capability ${capabilityId}`)
      }
    }
  }

  if (evidenceExpectation === 'required' && citedEvidence.length === 0) {
    failures.push('missing cited evidence')
  } else if (evidenceExpectation === 'forbidden' && citedEvidence.length > 0) {
    failures.push('unexpected SoundLens cited evidence')
  }

  if (externalCitationExpectation === 'required' && externalCitations.length === 0) {
    failures.push('missing external citations')
  } else if (externalCitationExpectation === 'forbidden' && externalCitations.length > 0) {
    failures.push('unexpected external citations')
  }

  for (const [index, citation] of externalCitations.entries()) {
    if (!isValidExternalCitation(citation, String(response?.answer ?? ''))) {
      failures.push(`invalid external citation ${index + 1}`)
    }
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

  for (const forbiddenPhrase of evalCase.forbiddenLimitationPhrases ?? []) {
    if (normalizedLimitations.some((limitation) => limitation.includes(normalizeText(forbiddenPhrase)))) {
      failures.push(`forbidden limitation phrase "${forbiddenPhrase}"`)
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

export function gradeInvestigationPlan(plan) {
  if (plan === undefined || plan === null) {
    return []
  }
  const failures = []
  if (!isObject(plan)) {
    return ['investigation plan must be an object']
  }
  if (!PLAN_ID_PATTERN.test(plan.planId ?? '') || plan.version !== '1' || plan.status !== 'preview') {
    failures.push('investigation plan has an invalid identity, version, or status')
  }
  if (!isNonEmptyString(plan.objective) || MEASURED_RESULT_PATTERN.test(plan.objective)) {
    failures.push('investigation plan objective is missing or contains a measured result')
  }
  if (!isValidPlanScope(plan.scope)) {
    failures.push('investigation plan scope is invalid')
  }
  if (!Array.isArray(plan.steps) || plan.steps.length < 1 || plan.steps.length > 6) {
    failures.push('investigation plan must contain between 1 and 6 steps')
    return failures
  }

  const seenStepIds = new Set()
  for (const [index, step] of plan.steps.entries()) {
    const expectedStepId = `step-${index + 1}`
    if (!isObject(step) || step.stepId !== expectedStepId || step.order !== index + 1 || seenStepIds.has(step.stepId)) {
      failures.push(`investigation plan step ${index + 1} has an invalid order or identifier`)
      continue
    }
    if (!SUPPORTED_PLAN_CAPABILITIES.has(step.capabilityId) ||
        !SUPPORTED_PLAN_CATEGORIES.has(step.category) ||
        !SUPPORTED_PLAN_COST_CLASSES.has(step.costClass) ||
        !isNonEmptyString(step.capabilityLabel) || typeof step.requiresApproval !== 'boolean') {
      failures.push(`investigation plan step ${index + 1} has invalid capability policy metadata`)
    }
    if (!isNonEmptyString(step.title) || !isNonEmptyString(step.purpose) ||
        MEASURED_RESULT_PATTERN.test(step.title ?? '') || MEASURED_RESULT_PATTERN.test(step.purpose ?? '')) {
      failures.push(`investigation plan step ${index + 1} has invalid or result-bearing text`)
    }
    for (const property of ['dependsOnStepIds', 'parameterKeys', 'requiredEvidence', 'completionCriteria']) {
      if (!Array.isArray(step[property]) || step[property].some((item) => !isNonEmptyString(item))) {
        failures.push(`investigation plan step ${index + 1} has invalid ${property}`)
      }
    }
    if (Array.isArray(step.dependsOnStepIds) &&
        (new Set(step.dependsOnStepIds).size !== step.dependsOnStepIds.length ||
         step.dependsOnStepIds.some((dependency) => !seenStepIds.has(dependency)))) {
      failures.push(`investigation plan step ${index + 1} has an invalid dependency`)
    }
    if (!Array.isArray(step.completionCriteria) || step.completionCriteria.length === 0 ||
        step.completionCriteria?.some((criterion) => MEASURED_RESULT_PATTERN.test(criterion))) {
      failures.push(`investigation plan step ${index + 1} has invalid completion criteria`)
    }
    seenStepIds.add(step.stepId)
  }
  return failures
}

function isValidPlanScope(scope) {
  if (!isObject(scope)) {
    return false
  }
  if (scope.kind === 'full_duration') {
    return scope.startTimeSeconds === null && scope.endTimeSeconds === null
  }
  return scope.kind === 'roi' && Number.isFinite(scope.startTimeSeconds) &&
    Number.isFinite(scope.endTimeSeconds) && scope.startTimeSeconds >= 0 &&
    scope.endTimeSeconds > scope.startTimeSeconds
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

export function summarizeRouting(results) {
  const byMode = {}
  const failures = []
  let evaluatedRuns = 0
  let correctRuns = 0

  for (const result of results) {
    const expectedMode = result.expectedAnswerMode
    if (!expectedMode) {
      continue
    }

    byMode[expectedMode] ??= { correctRuns: 0, evaluatedRuns: 0 }
    for (const run of result.runs ?? []) {
      evaluatedRuns += 1
      byMode[expectedMode].evaluatedRuns += 1
      const actualMode = run.response?.answerMode ?? null
      if (actualMode === expectedMode) {
        correctRuns += 1
        byMode[expectedMode].correctRuns += 1
      } else {
        failures.push({
          actualMode,
          caseId: result.id,
          expectedMode,
          run: run.run,
        })
      }
    }
  }

  return {
    accuracy: evaluatedRuns === 0 ? null : correctRuns / evaluatedRuns,
    byMode,
    correctRuns,
    evaluatedRuns,
    failures,
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

function validateExpectation(owner, property, prefix, failures) {
  if (owner[property] !== undefined && !SUPPORTED_EXPECTATIONS.has(owner[property])) {
    failures.push(`${prefix}.${property} must be required, forbidden, or optional`)
  }
}

function isValidExternalCitation(citation, answer) {
  if (!isObject(citation) || !isNonEmptyString(citation.title) || !isNonEmptyString(citation.url)) {
    return false
  }

  let url
  try {
    url = new URL(citation.url)
  } catch {
    return false
  }

  const metadata = citation.sourceMetadata
  return (url.protocol === 'http:' || url.protocol === 'https:') &&
    Number.isInteger(citation.startIndex) && citation.startIndex >= 0 &&
    Number.isInteger(citation.endIndex) && citation.endIndex > citation.startIndex &&
    citation.endIndex <= answer.length &&
    isObject(metadata) && metadata.publisherHost === url.hostname.toLowerCase() &&
    ['standards_body', 'public_authority', 'unclassified'].includes(metadata.sourceClass) &&
    metadata.accessStatus === 'not_verified' &&
    metadata.applicabilityStatus === 'not_assessed'
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
