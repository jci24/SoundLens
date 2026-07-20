using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static class ExternalSourceMetadataFactory
{
    private static readonly IReadOnlyDictionary<string, string> ClassifiedHosts =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["iso.org"] = AgentExternalSourceClasses.StandardsBody,
            ["www.iso.org"] = AgentExternalSourceClasses.StandardsBody,
            ["iec.ch"] = AgentExternalSourceClasses.StandardsBody,
            ["www.iec.ch"] = AgentExternalSourceClasses.StandardsBody,
            ["ecma-international.org"] = AgentExternalSourceClasses.StandardsBody,
            ["www.ecma-international.org"] = AgentExternalSourceClasses.StandardsBody,
            ["nist.gov"] = AgentExternalSourceClasses.PublicAuthority,
            ["www.nist.gov"] = AgentExternalSourceClasses.PublicAuthority
        };

    public static AgentExternalSourceMetadata Build(Uri uri)
    {
        var host = uri.IdnHost.ToLowerInvariant();
        var sourceClass = ClassifiedHosts.GetValueOrDefault(
            host,
            AgentExternalSourceClasses.Unclassified);

        return new AgentExternalSourceMetadata(
            host,
            sourceClass,
            AgentExternalSourceAccessStatuses.NotVerified,
            AgentExternalSourceApplicabilityStatuses.NotAssessed);
    }
}
