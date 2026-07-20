using OpenAI.Chat;
using SoundLens.Api.Configuration;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Comparisons.Common;

namespace SoundLens.Tests;

public sealed class SelectedComparisonOrchestratorTests
{
    [Fact]
    public async Task BypassesResolverAndModelWithoutSelectedComparisonContext()
    {
        var resolver = new StubContextResolver(BuildContext());
        var chatClientProvider = new ThrowingChatClientProvider();
        var orchestrator = new SelectedComparisonOrchestrator(chatClientProvider, resolver);

        var response = await orchestrator.TryBuildResponseAsync(
            new AgentQueryCommand("Summarize the workspace.", null, null, null),
            CancellationToken.None);

        Assert.Null(response);
        Assert.Equal(0, resolver.CallCount);
        Assert.Equal(0, chatClientProvider.CallCount);
    }

    [Fact]
    public async Task BroadWorkspaceGuidanceBypassesSelectedMetricExplanation()
    {
        var resolver = new StubContextResolver(BuildContext());
        var chatClientProvider = new ThrowingChatClientProvider();
        var orchestrator = new SelectedComparisonOrchestrator(chatClientProvider, resolver);

        var response = await orchestrator.TryBuildResponseAsync(
            BuildCommand("What guidelines would you give me to analyse these files?"),
            CancellationToken.None);

        Assert.Null(response);
        Assert.Equal(0, resolver.CallCount);
        Assert.Equal(0, chatClientProvider.CallCount);
    }

    [Theory]
    [InlineData("What is the calibrated dB SPL difference?", "cannot determine a calibrated dB SPL")]
    [InlineData("What caused this selected difference?", "does not establish a cause")]
    public async Task ResolvesEvidenceBeforeTrustGuardAndNeverAcquiresModel(
        string question,
        string expectedAnswer)
    {
        var resolver = new StubContextResolver(BuildContext());
        var chatClientProvider = new ThrowingChatClientProvider();
        var orchestrator = new SelectedComparisonOrchestrator(chatClientProvider, resolver);

        var response = await orchestrator.TryBuildResponseAsync(
            BuildCommand(question, startTimeSeconds: 0, endTimeSeconds: 0.25),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Contains(expectedAnswer, response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(response.Limitations, limitation =>
            limitation.Contains("selected ROI only", StringComparison.OrdinalIgnoreCase));
        var observation = Assert.Single(response.StructuredObservations);
        Assert.Equal(AgentStructuredObservationKinds.ComparisonMetric, observation.Kind);
        Assert.Equal(AgentStructuredObservationStatuses.Limited, observation.Status);
        Assert.Equal(AgentObservationScopeKinds.RegionOfInterest, observation.Scope.Kind);
        Assert.Equal(1, resolver.CallCount);
        Assert.Equal(0, chatClientProvider.CallCount);
    }

    [Fact]
    public async Task AcquiresModelOnlyAfterResolutionAndTrustGuardsDecline()
    {
        var resolver = new StubContextResolver(BuildContext());
        var chatClientProvider = new ThrowingChatClientProvider();
        var orchestrator = new SelectedComparisonOrchestrator(chatClientProvider, resolver);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.TryBuildResponseAsync(
                BuildCommand("Explain the selected comparison evidence."),
                CancellationToken.None));

        Assert.Equal(ThrowingChatClientProvider.ExceptionMessage, exception.Message);
        Assert.Equal(1, resolver.CallCount);
        Assert.Equal(1, chatClientProvider.CallCount);
    }

    [Fact]
    public async Task PropagatesResolutionFailureWithoutAcquiringModel()
    {
        var resolver = new StubContextResolver(
            new ArgumentException("The selected signals are not an aligned pair."));
        var chatClientProvider = new ThrowingChatClientProvider();
        var orchestrator = new SelectedComparisonOrchestrator(chatClientProvider, resolver);

        var exception = await Assert.ThrowsAsync<SelectedComparisonResolutionException>(() =>
            orchestrator.TryBuildResponseAsync(
                BuildCommand("Explain the selected comparison evidence."),
                CancellationToken.None));

        Assert.Contains("not an aligned pair", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, resolver.CallCount);
        Assert.Equal(0, chatClientProvider.CallCount);
    }

    [Fact]
    public async Task DoesNotMisclassifyModelArgumentFailureAsResolutionFailure()
    {
        var resolver = new StubContextResolver(BuildContext());
        var chatClientProvider = new ThrowingChatClientProvider(
            new ArgumentException("Model configuration is invalid."));
        var orchestrator = new SelectedComparisonOrchestrator(chatClientProvider, resolver);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            orchestrator.TryBuildResponseAsync(
                BuildCommand("Explain the selected comparison evidence."),
                CancellationToken.None));

        Assert.Equal("Model configuration is invalid.", exception.Message);
        Assert.Equal(1, resolver.CallCount);
        Assert.Equal(1, chatClientProvider.CallCount);
    }

    private static AgentQueryCommand BuildCommand(
        string question,
        double? startTimeSeconds = null,
        double? endTimeSeconds = null) =>
        new(
            Question: question,
            SignalIds: ["recording-a:ch:0", "recording-b:ch:0"],
            StartTimeSeconds: startTimeSeconds,
            EndTimeSeconds: endTimeSeconds,
            ComparisonContext: new AgentComparisonSelection(
                "recording-a",
                "recording-b",
                "rmsAmplitudeDelta",
                "recording-a:ch:0",
                "recording-b:ch:0"));

    private static ResolvedComparisonExplanationContext BuildContext() => new(
        RecordingIdA: "recording-a",
        RecordingFileNameA: "quiet.wav",
        RecordingIdB: "recording-b",
        RecordingFileNameB: "loud.wav",
        MetricKey: "rmsAmplitudeDelta",
        MetricLabel: "RMS amplitude",
        Unit: "FS",
        ComparedPairCount: 1,
        MissingValueCount: 0,
        MeanDifference: -0.25,
        MedianDifference: -0.25,
        Spread: 0,
        CoverageLabel: "Weak evidence",
        CoverageCopy: "The current comparison rests on a very small amount of aligned evidence.",
        Limitations:
        [
            new RecordingComparisonLimitation("LowCoverage", "Comparison has low coverage.")
        ],
        Observation: new ResolvedComparisonObservation(
            SignalIdA: "recording-a:ch:0",
            DisplayNameA: "Channel 1",
            SignalIdB: "recording-b:ch:0",
            DisplayNameB: "Channel 1",
            ValueA: 0.25,
            ValueB: 0.5,
            Delta: -0.25),
        Findings: []);

    private sealed class StubContextResolver : IComparisonExplanationContextResolver
    {
        private readonly ResolvedComparisonExplanationContext? _context;
        private readonly Exception? _exception;

        public StubContextResolver(ResolvedComparisonExplanationContext context)
        {
            _context = context;
        }

        public StubContextResolver(Exception exception)
        {
            _exception = exception;
        }

        public int CallCount { get; private set; }

        public Task<ResolvedComparisonExplanationContext> ResolveAsync(
            AgentComparisonSelection selection,
            double? startTimeSeconds,
            double? endTimeSeconds,
            CancellationToken ct)
        {
            CallCount++;
            return _exception is null
                ? Task.FromResult(_context!)
                : Task.FromException<ResolvedComparisonExplanationContext>(_exception);
        }
    }

    private sealed class ThrowingChatClientProvider : IChatClientProvider
    {
        public const string ExceptionMessage = "Model acquisition proves orchestration ordering.";
        private readonly Exception _exception;

        public ThrowingChatClientProvider()
            : this(new InvalidOperationException(ExceptionMessage))
        {
        }

        public ThrowingChatClientProvider(Exception exception)
        {
            _exception = exception;
        }

        public int CallCount { get; private set; }

        public ChatClient GetRequiredClient()
        {
            CallCount++;
            throw _exception;
        }
    }
}
