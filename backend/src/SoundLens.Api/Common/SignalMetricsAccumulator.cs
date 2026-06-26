namespace SoundLens.Api.Common;

public sealed class SignalMetricsAccumulator(double positiveFullScaleThreshold)
{
    private readonly double _positiveFullScaleThreshold = positiveFullScaleThreshold;
    private int _sampleCount;
    private double _sumSquares;
    private double _peakAmplitude;
    private int _clippingSampleCount;

    public void Include(double sample)
    {
        var normalizedSample = Math.Clamp(sample, -1.0, 1.0);
        var absoluteAmplitude = Math.Abs(normalizedSample);

        _sampleCount++;
        _sumSquares += normalizedSample * normalizedSample;
        _peakAmplitude = Math.Max(_peakAmplitude, absoluteAmplitude);

        if (sample <= -1.0 || sample >= _positiveFullScaleThreshold)
        {
            _clippingSampleCount++;
        }
    }

    public SignalDerivedMetrics Build()
    {
        if (_sampleCount == 0)
        {
            return new SignalDerivedMetrics(0, 0, 0, 0, false);
        }

        var rmsAmplitude = Math.Sqrt(_sumSquares / _sampleCount);
        var crestFactor = rmsAmplitude > 0 ? _peakAmplitude / rmsAmplitude : 0;

        return new SignalDerivedMetrics(
            _peakAmplitude,
            rmsAmplitude,
            crestFactor,
            _clippingSampleCount,
            _clippingSampleCount > 0);
    }
}
