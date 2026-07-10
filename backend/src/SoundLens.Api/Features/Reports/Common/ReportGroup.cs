using FastEndpoints;

namespace SoundLens.Api.Features.Reports.Common;

public sealed class ReportGroup : Group
{
    public ReportGroup()
    {
        Configure("report", endpointDefinition =>
        {
            endpointDefinition.Description(builder => builder.WithTags("Report"));
        });
    }
}
