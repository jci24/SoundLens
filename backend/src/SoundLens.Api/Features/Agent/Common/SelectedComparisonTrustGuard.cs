using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static class SelectedComparisonTrustGuard
{
    private const string CausalLimitation =
        "Selected comparison evidence is observational and does not establish causation.";

    public static AgentQueryResponse? TryBuildResponse(
        string question,
        ResolvedComparisonExplanationContext context,
        bool isRoiScoped)
    {
        if (UncalibratedSplRefusalPolicy.IsPhysicalSplRequest(question))
        {
            return new AgentQueryResponse(
                Answer: UncalibratedSplRefusalPolicy.BuildAnswer(context, isRoiScoped),
                CitedEvidence: SelectedComparisonResponseSupport.BuildContextEvidence(context),
                Limitations: SelectedComparisonResponseSupport.BuildLimitations(context, isRoiScoped),
                NextSteps:
                [
                    "Provide a validated acoustic calibration reference if you need physical dB SPL values.",
                    "Use the available digital comparison evidence to compare the selected recordings without treating it as physical SPL."
                ],
                ToolsUsed: []);
        }

        if (!UnsupportedCausalRefusalPolicy.IsUnsupportedCausalRequest(question))
        {
            return null;
        }

        return new AgentQueryResponse(
            Answer: UnsupportedCausalRefusalPolicy.BuildAnswer(context, isRoiScoped),
            CitedEvidence: SelectedComparisonResponseSupport.BuildCausalEvidence(context),
            Limitations: SelectedComparisonResponseSupport.BuildLimitations(
                context,
                isRoiScoped,
                CausalLimitation),
            NextSteps:
            [
                "Inspect the waveform and spectrum for the selected pair to identify associations worth testing.",
                "Repeat the comparison while changing one controlled factor at a time and record the setup conditions before attributing a cause."
            ],
            ToolsUsed: []);
    }
}
