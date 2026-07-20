import { useId, useState } from 'react'
import { ChevronDown, ChevronUp } from 'lucide-react'
import type { IAgentInvestigationPlan, IAgentInvestigationPlanScope } from '../types/copilot.types'
import './CopilotInvestigationPlan.scss'

interface ICopilotInvestigationPlanProps {
  plan: IAgentInvestigationPlan | null | undefined
}

const numberFormatter = new Intl.NumberFormat(undefined, { maximumFractionDigits: 3 })

const CopilotInvestigationPlan = ({ plan }: ICopilotInvestigationPlanProps) => {
  const [isOpen, setIsOpen] = useState(false)
  const contentId = useId()

  if (!plan) return null

  return (
    <section className="copilot-investigation-plan" aria-label="Investigation plan">
      <button
        aria-controls={contentId}
        aria-expanded={isOpen}
        className="copilot-investigation-plan__toggle"
        type="button"
        onClick={() => setIsOpen((current) => !current)}
      >
        <span>Investigation plan</span>
        <span className="copilot-investigation-plan__summary">
          {plan.steps.length} step{plan.steps.length === 1 ? '' : 's'} · Preview
        </span>
        {isOpen ? <ChevronUp size={13} /> : <ChevronDown size={13} />}
      </button>

      {isOpen && (
        <div className="copilot-investigation-plan__content" id={contentId}>
          <div className="copilot-investigation-plan__overview">
            <p>{plan.objective}</p>
            <span>{formatScope(plan.scope)} · Preview only; steps are not run automatically.</span>
          </div>
          <ol className="copilot-investigation-plan__steps">
            {plan.steps.map((step) => (
              <li key={step.stepId}>
                <div className="copilot-investigation-plan__step-heading">
                  <strong>{step.title}</strong>
                  <span>{step.capabilityLabel}</span>
                </div>
                <p>{step.purpose}</p>
                <dl>
                  <div>
                    <dt>Completion</dt>
                    <dd>{step.completionCriteria.join(' ')}</dd>
                  </div>
                  <div>
                    <dt>Requires</dt>
                    <dd>{step.requiredEvidence.map(formatToken).join(', ')}</dd>
                  </div>
                  <div>
                    <dt>Inputs</dt>
                    <dd>{step.parameterKeys.map(formatToken).join(', ')}</dd>
                  </div>
                  <div>
                    <dt>After</dt>
                    <dd>{step.dependsOnStepIds.length > 0 ? step.dependsOnStepIds.join(', ') : 'No dependency'}</dd>
                  </div>
                  <div>
                    <dt>Cost</dt>
                    <dd>{formatToken(step.costClass)}</dd>
                  </div>
                  <div>
                    <dt>Approval</dt>
                    <dd>{step.requiresApproval ? 'Required before execution' : 'Not required'}</dd>
                  </div>
                </dl>
              </li>
            ))}
          </ol>
          <code title={plan.planId}>{shortenPlanId(plan.planId)}</code>
        </div>
      )}
    </section>
  )
}

const formatScope = (scope: IAgentInvestigationPlanScope) => scope.kind === 'full_duration'
  ? 'Full duration'
  : `${numberFormatter.format(scope.startTimeSeconds ?? 0)}–${numberFormatter.format(scope.endTimeSeconds ?? 0)} s ROI`

const shortenPlanId = (planId: string) => `${planId.slice(0, 16)}…`

const formatToken = (value: string) => value.replaceAll('_', ' ')

export { CopilotInvestigationPlan }
