# ROI And Findings Demo Flow

Last updated: 2026-07-07

This document defines the repeatable demo script for the current SoundLens validation slice.

Use it when showing the product to a prospective customer, when manually validating the latest ROI and findings workflow, and when checking whether the workspace tells a coherent comparison story without extra explanation.

## Goal

Show a narrow but credible investigation loop:

1. Import two or more recordings.
2. Select one or more signals.
3. Compare waveform and spectrum evidence.
4. Select a time region of interest on the waveform.
5. Verify that the selected region drives spectrum and findings updates without confusing chart behavior.
6. Capture what the customer finds useful, confusing, or missing.

This is a validation-critical flow, not a broad product tour.

## Recommended Demo Inputs

Use a stable pair of WAV recordings whenever possible so each session starts from the same reference point.

Preferred characteristics:

- Same nominal sample rate
- Clear audible difference between variants
- At least one obvious transient or event that makes ROI selection meaningful
- At least one spectrum difference that can be discussed without speculative interpretation
- At least one recording or region that triggers a deterministic finding such as `TonalPeak` or `HarmonicSeries`

If a stable reference pair is not available yet, use the same internal recordings across all sessions until a better demo set is collected.

## Preflight

Before each session:

1. Start backend and frontend locally.
2. Confirm browser upload works.
3. Confirm at least one recording pair loads without backend errors.
4. Confirm waveform and spectrum both render.
5. Confirm region selection can be created, cleared, and recreated.
6. Confirm findings are visible in the metrics/findings area.
7. Confirm the browser zoom level is acceptable for the current screen.

If any of these fail, do not improvise around the failure. Log it as a validation blocker.

## Manual Validation Script

Run the same script before customer sessions and after meaningful ROI or findings changes.

### Step 1: Import

- Upload the reference recordings.
- Confirm the file rail shows the expected recordings and channels.
- Confirm the default workspace does not look empty or visually broken.

Expected result:

- Import completes without reloads or stale loading states.
- The workspace clearly shows what was loaded.

### Step 2: Signal Selection

- Select one signal from each recording, or two comparable channels.
- Switch between focused and compare layouts when relevant.

Expected result:

- Selection state stays stable across layout changes.
- The chart(s) reflect the selected signals only.
- The metrics panel updates consistently with the visible selection.

### Step 3: Waveform Comparison

- Start on the waveform surface.
- Ask the customer what event or difference they would inspect first.
- Use that answer to select a likely region of interest.

Expected result:

- Waveform charts remain visually stable when the first ROI is selected.
- Axis labels stay visible.
- No page refresh or loading loop occurs.

### Step 4: Region Of Interest

- Drag a region around the most meaningful event.
- Clear the region once.
- Create a second region in a different location.

Expected result:

- ROI selection is visible and understandable.
- Clearing the ROI returns the workspace to full-recording context.
- Re-selecting a region works without getting stuck on the previous selection.

### Step 5: Spectrum Follow-Through

- Switch to the spectrum surface while an ROI is active.
- Compare the spectrum against the full-recording mental model.
- Return to waveform and confirm ROI state still behaves correctly.

Expected result:

- Spectrum reflects the selected region rather than a silent refresh loop.
- Surface switching does not break ROI interaction.
- Findings remain attached to the current evidence context.

### Step 6: Findings Readout

- Read the deterministic findings aloud.
- Ask whether those findings are useful, obvious, too vague, or missing context.

Expected result:

- Findings help start the conversation.
- Findings do not over-claim root cause, compliance, or ranking.
- The customer can point to a next question or next comparison they would want.

## Session Questions

Ask these questions consistently:

1. What would you inspect first in your normal workflow?
2. Did the waveform, spectrum, and findings tell a coherent story?
3. Was region selection useful, or did it feel like extra interaction cost?
4. What evidence would you need before trusting this in a real investigation?
5. What would you want to export or share after this step?

## What To Capture

Record:

- Which recordings were used
- Which signals were selected
- Which ROI was discussed
- Which findings appeared
- Where the user hesitated
- What they asked for next
- Whether they trusted the evidence shown

Also capture:

- Any layout break
- Any loading state that lasts too long
- Any page refresh, chart jump, or stale selection behavior
- Any mismatch between waveform ROI and spectrum response

## Exit Criteria For This Slice

This validation slice is useful when:

- The demo can be run end to end with a consistent script.
- The repo contains a documented validation flow instead of ad hoc demo behavior.
- Customer feedback can be compared across sessions.
- Any follow-up ROI polish work is grounded in observed friction, not guesswork.
