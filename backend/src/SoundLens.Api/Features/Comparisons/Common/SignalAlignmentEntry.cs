namespace SoundLens.Api.Features.Comparisons.Common;

public sealed record SignalAlignmentEntry(
    SignalAlignmentSlot? Source,
    SignalAlignmentSlot? Target,
    SignalAlignmentOutcome Outcome,
    SignalAlignmentBasis Basis,
    string Detail);
