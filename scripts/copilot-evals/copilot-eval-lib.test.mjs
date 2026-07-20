import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

import {
  gradeComparisonSetup,
  gradeResponse,
  summarize,
  summarizeRouting,
  validateDataset,
  validateRunCount,
} from './copilot-eval-lib.mjs'

const validComparisonCase = {
  id: 'comparison-case',
  question: 'Explain the selected comparison.',
  comparison: {
    recordingAFileName: 'a.wav',
    recordingBFileName: 'b.wav',
    metricKey: 'rmsAmplitudeDelta',
    signalDisplayNameA: 'Channel 1',
    signalDisplayNameB: 'Channel 1',
  },
  expectedComparison: {
    meanDifference: 0,
    tolerance: 0.001,
    limitationCodes: ['LowCoverage'],
  },
}

test('validates generic and comparison cases without breaking the existing schema', () => {
  const failures = validateDataset({
    filePaths: ['./a.wav', './b.wav'],
    cases: [
      { id: 'generic', question: 'Is there clipping?', signals: [] },
      validComparisonCase,
    ],
  })

  assert.deepEqual(failures, [])
  assert.deepEqual(validateRunCount(3), [])
})

test('rejects duplicate IDs, unsupported metrics, incomplete selectors, and invalid runs', () => {
  const failures = validateDataset({
    filePaths: ['./a.wav'],
    cases: [
      { id: 'duplicate', question: 'First' },
      {
        id: 'duplicate',
        question: 'Second',
        comparison: {
          recordingAFileName: 'a.wav',
          recordingBFileName: 'b.wav',
          metricKey: 'perceivedQuality',
          signalDisplayNameA: 'Channel 1',
        },
      },
    ],
  })

  assert.ok(failures.some((failure) => failure.includes('duplicate case id')))
  assert.ok(failures.some((failure) => failure.includes('metricKey "perceivedQuality" is not supported')))
  assert.ok(failures.some((failure) => failure.includes('signalDisplayNameB must be a non-empty string')))
  assert.deepEqual(validateRunCount(0), ['run count must be a positive integer'])
})

test('rejects malformed phrase groups, patterns, ROI, and deterministic expectations', () => {
  const failures = validateDataset({
    filePaths: ['./a.wav'],
    cases: [{
      ...validComparisonCase,
      requiredAnswerAnyPhraseGroups: [[]],
      forbiddenAnswerPatterns: ['['],
      startTimeSeconds: 1,
      endTimeSeconds: 0,
      expectedComparison: { meanDifference: 'zero', tolerance: -1, limitationCodes: [42] },
    }],
  })

  assert.ok(failures.some((failure) => failure.includes('requiredAnswerAnyPhraseGroups[0]')))
  assert.ok(failures.some((failure) => failure.includes('invalid regex')))
  assert.ok(failures.some((failure) => failure.includes('0 <= start < end')))
  assert.ok(failures.some((failure) => failure.includes('meanDifference must be finite')))
  assert.ok(failures.some((failure) => failure.includes('tolerance must be zero or greater')))
  assert.ok(failures.some((failure) => failure.includes('limitationCodes must contain non-empty strings')))
})

test('validates routing modes and evidence expectations', () => {
  const valid = validateDataset({
    filePaths: ['./a.wav'],
    cases: [{
      id: 'general-definition',
      question: 'What is crest factor?',
      contextMode: 'auto',
      expectedAnswerMode: 'general',
      evidenceExpectation: 'forbidden',
      externalCitationExpectation: 'forbidden',
      expectedEvidenceSufficiencyStatus: 'supported',
      expectedEvidenceIntent: 'digital_level_difference',
      expectedObservationStatus: 'complete',
      expectedFindingObservationCount: 0,
    }],
  })
  assert.deepEqual(valid, [])

  const invalid = validateDataset({
    filePaths: ['./a.wav'],
    cases: [{
      id: 'invalid-routing',
      question: 'Route this.',
      contextMode: 'internet',
      expectedAnswerMode: 'classifier',
      evidenceExpectation: 'sometimes',
      externalCitationExpectation: 'maybe',
      expectedEvidenceSufficiencyStatus: 'certain',
      expectedEvidenceIntent: '',
      expectedObservationStatus: 'certain',
      expectedFindingObservationCount: -1,
    }],
  })
  assert.ok(invalid.some((failure) => failure.includes('expectedAnswerMode')))
  assert.ok(invalid.some((failure) => failure.includes('contextMode')))
  assert.ok(invalid.some((failure) => failure.includes('evidenceExpectation')))
  assert.ok(invalid.some((failure) => failure.includes('externalCitationExpectation')))
  assert.ok(invalid.some((failure) => failure.includes('expectedEvidenceSufficiencyStatus')))
  assert.ok(invalid.some((failure) => failure.includes('expectedEvidenceIntent')))
  assert.ok(invalid.some((failure) => failure.includes('expectedObservationStatus')))
  assert.ok(invalid.some((failure) => failure.includes('expectedFindingObservationCount')))
})

test('validates the committed trust and routing datasets', async () => {
  for (const fileName of ['copilot-eval-cases.json', 'copilot-routing-cases.json']) {
    const dataset = JSON.parse(await readFile(new URL(fileName, import.meta.url), 'utf8'))
    assert.deepEqual(validateDataset(dataset), [], `${fileName} should satisfy the eval schema`)
  }
})

test('grades all-of, any-of, limitations, evidence tools, and forbidden patterns', () => {
  const evalCase = {
    requiredAnswerPhrases: ['bounded'],
    requiredAnswerAnyPhraseGroups: [['cannot determine', 'not enough evidence']],
    forbiddenAnswerPhrases: ['proves'],
    forbiddenAnswerPatterns: ['\\b92\\s*dB\\s*SPL\\b'],
    requiredLimitationPhrases: ['selected ROI'],
    requiredEvidenceTools: ['comparison_view'],
  }
  const response = {
    answer: 'This is bounded: I cannot determine a cause from the evidence.',
    citedEvidence: [{ toolName: 'comparison_view', signalId: 'a:ch:0' }],
    limitations: ['Answer reflects the selected ROI only.'],
    nextSteps: [],
  }

  assert.deepEqual(gradeResponse(evalCase, response), { pass: true, failures: [] })

  const failed = gradeResponse(evalCase, {
    ...response,
    answer: 'This proves the level is 92 dB SPL.',
    citedEvidence: [{ toolName: 'get_signal_metrics' }],
    limitations: [],
  })
  assert.equal(failed.pass, false)
  assert.ok(failed.failures.some((failure) => failure.includes('any-of')))
  assert.ok(failed.failures.some((failure) => failure.includes('forbidden phrase')))
  assert.ok(failed.failures.some((failure) => failure.includes('forbidden answer pattern')))
  assert.ok(failed.failures.some((failure) => failure.includes('missing limitation phrase')))
  assert.ok(failed.failures.some((failure) => failure.includes('missing evidence tool')))
})

test('grades deterministic metric tolerance and comparison limitation codes', () => {
  const response = {
    aggregateMetrics: [{ metricKey: 'rmsAmplitudeDelta', meanDifference: 0.0005 }],
    limitations: [{ code: 'LowCoverage' }],
  }
  const pair = { signalIdA: 'a:ch:0', signalIdB: 'b:ch:0' }

  assert.equal(gradeComparisonSetup(validComparisonCase, response, pair).pass, true)

  const failed = gradeComparisonSetup(validComparisonCase, {
    aggregateMetrics: [{ metricKey: 'rmsAmplitudeDelta', meanDifference: 0.01 }],
    limitations: [],
  }, null)
  assert.equal(failed.pass, false)
  assert.ok(failed.failures.some((failure) => failure.includes('aligned pair')))
  assert.ok(failed.failures.some((failure) => failure.includes('LowCoverage')))
  assert.ok(failed.failures.some((failure) => failure.includes('meanDifference expected')))
})

test('grades answer mode, evidence isolation, and valid external citations', () => {
  const answer = 'Current product-sound practice uses controlled comparisons.'
  const webResult = gradeResponse({
    expectedAnswerMode: 'web',
    evidenceExpectation: 'forbidden',
    externalCitationExpectation: 'required',
    expectedTools: ['web_search'],
    forbiddenLimitationPhrases: ['dBFS'],
  }, {
    answer,
    answerMode: 'web',
    citedEvidence: [],
    externalCitations: [{
      title: 'Primary source',
      url: 'https://example.com/source',
      startIndex: 0,
      endIndex: answer.length,
    }],
    limitations: [],
    nextSteps: [],
    toolsUsed: ['web_search'],
  })
  assert.deepEqual(webResult, { pass: true, failures: [] })

  const leaked = gradeResponse({
    expectedAnswerMode: 'general',
    evidenceExpectation: 'forbidden',
    externalCitationExpectation: 'forbidden',
    forbiddenLimitationPhrases: ['dBFS'],
  }, {
    answer: 'A general answer.',
    answerMode: 'workspace',
    citedEvidence: [{ toolName: 'get_signal_metrics', signalId: 'private' }],
    externalCitations: [{ title: '', url: 'file:///tmp/source', startIndex: -1, endIndex: 0 }],
    limitations: ['Values are in dBFS.'],
    nextSteps: [],
    toolsUsed: [],
  })
  assert.equal(leaked.pass, false)
  assert.ok(leaked.failures.some((failure) => failure.includes('expected answer mode general')))
  assert.ok(leaked.failures.includes('unexpected SoundLens cited evidence'))
  assert.ok(leaked.failures.includes('unexpected external citations'))
  assert.ok(leaked.failures.includes('invalid external citation 1'))
  assert.ok(leaked.failures.some((failure) => failure.includes('forbidden limitation phrase')))
})

test('grades backend-owned evidence sufficiency status and intent', () => {
  const result = gradeResponse({
    evidenceExpectation: 'forbidden',
    expectedEvidenceSufficiencyStatus: 'contradicted',
    expectedEvidenceIntent: 'digital_level_difference',
  }, {
    answer: 'The aligned observations differ in direction.',
    citedEvidence: [],
    limitations: [],
    nextSteps: [],
    toolsUsed: [],
    evidenceSufficiency: {
      status: 'partial',
      intent: 'digital_level_difference',
    },
  })

  assert.equal(result.pass, false)
  assert.ok(result.failures.some((failure) => failure.includes('expected evidence sufficiency contradicted')))
  assert.ok(result.failures.every((failure) => !failure.includes('expected evidence intent')))
})

test('grades backend-owned structured metric and finding observations', () => {
  const result = gradeResponse({
    evidenceExpectation: 'forbidden',
    expectedObservationStatus: 'mixed',
    expectedFindingObservationCount: 1,
  }, {
    answer: 'Bounded answer.',
    citedEvidence: [],
    limitations: [],
    nextSteps: [],
    toolsUsed: [],
    structuredObservations: [
      { kind: 'comparison_metric', status: 'limited' },
      { kind: 'signal_finding', status: 'complete' },
      { kind: 'signal_finding', status: 'complete' },
    ],
  })

  assert.equal(result.pass, false)
  assert.ok(result.failures.includes('expected observation status mixed, received limited'))
  assert.ok(result.failures.includes('expected 1 finding observations, received 2'))
})

test('requires every repeated run and every setup to pass', () => {
  const summary = summarize([
    {
      setup: { pass: true },
      runs: [{ grading: { pass: true } }, { grading: { pass: false } }],
    },
    {
      setup: { pass: false },
      runs: [],
    },
  ])

  assert.deepEqual(summary, {
    caseCount: 2,
    runCount: 2,
    passedRuns: 1,
    failedRuns: 1,
    setupFailures: 1,
    pass: false,
  })
})

test('summarizes routing accuracy by expected answer mode', () => {
  const summary = summarizeRouting([
    {
      id: 'general',
      expectedAnswerMode: 'general',
      runs: [
        { run: 1, response: { answerMode: 'general' } },
        { run: 2, response: { answerMode: 'workspace' } },
      ],
    },
    {
      id: 'web',
      expectedAnswerMode: 'web',
      runs: [{ run: 1, response: { answerMode: 'web' } }],
    },
    { id: 'legacy', expectedAnswerMode: null, runs: [{ run: 1, response: {} }] },
  ])

  assert.equal(summary.evaluatedRuns, 3)
  assert.equal(summary.correctRuns, 2)
  assert.equal(summary.accuracy, 2 / 3)
  assert.deepEqual(summary.byMode.general, { correctRuns: 1, evaluatedRuns: 2 })
  assert.deepEqual(summary.byMode.web, { correctRuns: 1, evaluatedRuns: 1 })
  assert.deepEqual(summary.failures, [{
    actualMode: 'workspace',
    caseId: 'general',
    expectedMode: 'general',
    run: 2,
  }])
})

test('reports missing cited evidence and internal tool leakage actionably', () => {
  const result = gradeResponse({}, {
    answer: 'No evidence.',
    citedEvidence: [],
    limitations: [],
    nextSteps: ['Call compare_signals again.'],
  })

  assert.deepEqual(result.failures, [
    'missing cited evidence',
    'nextSteps leak internal tool names',
  ])
})

test('grades malformed response collections without throwing away diagnostics', () => {
  const result = gradeResponse({
    expectedTools: ['compare_signals'],
    requiredEvidenceTools: ['selected_comparison_context'],
    requiredLimitationPhrases: ['dBFS'],
  }, {
    answer: 'Malformed collections remain inspectable.',
    citedEvidence: { toolName: 'selected_comparison_context' },
    limitations: 'dBFS',
    toolsUsed: 'compare_signals',
  })

  assert.equal(result.pass, false)
  assert.deepEqual(result.failures, [
    'missing cited evidence',
    'missing tool compare_signals',
    'missing evidence tool selected_comparison_context',
    'missing limitation phrase "dBFS"',
  ])
})
