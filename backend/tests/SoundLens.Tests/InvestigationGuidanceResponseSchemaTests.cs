using System.Text.Json;
using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class InvestigationGuidanceResponseSchemaTests
{
    [Fact]
    public void ConstrainsTheResponseShapeAndAvailableCapabilities()
    {
        var capabilities = InvestigationCapabilityCatalog.ResolveAvailable(true, false);
        using var document = JsonDocument.Parse(InvestigationGuidanceResponseSchema.Build(capabilities, false));
        var root = document.RootElement;
        var rootProperties = root.GetProperty("properties");
        var planSchema = rootProperties.GetProperty("plan").GetProperty("anyOf")[0];
        var steps = planSchema.GetProperty("properties").GetProperty("steps");
        var stepProperties = steps.GetProperty("items").GetProperty("properties");
        var capabilityIds = stepProperties.GetProperty("capabilityId").GetProperty("enum")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.False(root.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(1, steps.GetProperty("minItems").GetInt32());
        Assert.Equal(InvestigationPlanValidator.MaximumStepCount, steps.GetProperty("maxItems").GetInt32());
        Assert.Contains("waveform", capabilityIds);
        Assert.DoesNotContain("report_export", capabilityIds);
        Assert.Equal("null", rootProperties.GetProperty("plan").GetProperty("anyOf")[1]
            .GetProperty("type").GetString());
    }

    [Fact]
    public void ProducesAValidSchemaWhenNoCapabilitiesAreAvailable()
    {
        using var document = JsonDocument.Parse(InvestigationGuidanceResponseSchema.Build([], false));
        var capabilityIds = document.RootElement
            .GetProperty("properties")
            .GetProperty("plan")
            .GetProperty("anyOf")[0]
            .GetProperty("properties")
            .GetProperty("steps")
            .GetProperty("items")
            .GetProperty("properties")
            .GetProperty("capabilityId")
            .GetProperty("enum");

        Assert.Equal("__none_available__", Assert.Single(capabilityIds.EnumerateArray()).GetString());
    }

    [Fact]
    public void RequiresAPlanObjectForAnExplicitPlanRequest()
    {
        var capabilities = InvestigationCapabilityCatalog.ResolveAvailable(true, true);
        using var document = JsonDocument.Parse(InvestigationGuidanceResponseSchema.Build(capabilities, true));
        var plan = document.RootElement.GetProperty("properties").GetProperty("plan");

        Assert.Equal("object", plan.GetProperty("type").GetString());
        Assert.False(plan.TryGetProperty("anyOf", out _));
    }
}
