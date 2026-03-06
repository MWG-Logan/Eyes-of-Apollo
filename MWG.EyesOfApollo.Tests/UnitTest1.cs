using MWG.EyesOfApollo.Desktop.Models;
using MWG.EyesOfApollo.Desktop.Rendering;
using MWG.EyesOfApollo.Desktop.Services;

namespace MWG.EyesOfApollo.Tests;

public class AudioAnalyzerTests
{
    [Fact]
    public void ComputeSpectrum_ReturnsEmpty_WhenSamplesMissing()
    {
        var result = AudioAnalyzer.ComputeSpectrum(Array.Empty<float>(), 44100, 2, 64, AmplitudeScaleMode.Dbfs, FrequencyWeightingMode.AWeighting, FrequencyBinMode.Logarithmic, -90, -6);

        Assert.Empty(result);
    }

    [Fact]
    public void ComputeSpectrum_ReturnsExpectedLength()
    {
        var samples = TestSamples.GenerateSine(440, 44100, 4096);
        var result = AudioAnalyzer.ComputeSpectrum(samples, 44100, 1, 96, AmplitudeScaleMode.Dbfs, FrequencyWeightingMode.AWeighting, FrequencyBinMode.Logarithmic, -90, -6);

        Assert.Equal(96, result.Length);
    }

    [Fact]
    public void ComputeSpectrum_ReturnsEmpty_WhenSampleRateInvalid()
    {
        var samples = TestSamples.GenerateSine(440, 44100, 4096);
        var result = AudioAnalyzer.ComputeSpectrum(samples, 0, 1, 96, AmplitudeScaleMode.Dbfs, FrequencyWeightingMode.AWeighting, FrequencyBinMode.Logarithmic, -90, -6);

        Assert.Empty(result);
    }

    [Fact]
    public void ComputeSpectrum_EmphasizesDominantTone()
    {
        var samples = TestSamples.GenerateSine(440, 44100, 4096);
        var result = AudioAnalyzer.ComputeSpectrum(samples, 44100, 1, 96, AmplitudeScaleMode.Normalized, FrequencyWeightingMode.Flat, FrequencyBinMode.Logarithmic, -90, -6);

        var max = result.Max();
        var median = result.OrderBy(value => value).ElementAt(result.Length / 2);

        Assert.True(max > median);
    }

    [Fact]
    public void ComputeSpectrum_StaysWithinUnitRange()
    {
        var samples = TestSamples.GenerateSine(880, 44100, 4096);
        var result = AudioAnalyzer.ComputeSpectrum(samples, 44100, 1, 64, AmplitudeScaleMode.Dbfs, FrequencyWeightingMode.AWeighting, FrequencyBinMode.Linear, -90, -6);

        Assert.All(result, value => Assert.InRange(value, 0f, 1f));
    }

    [Fact]
    public void ComputeSpectrum_LinearBins_ShiftsPeakWithFrequency()
    {
        var low = TestSamples.GenerateSine(440, 44100, 4096);
        var high = TestSamples.GenerateSine(880, 44100, 4096);

        var lowSpectrum = AudioAnalyzer.ComputeSpectrum(low, 44100, 1, 96, AmplitudeScaleMode.Normalized, FrequencyWeightingMode.Flat, FrequencyBinMode.Linear, -90, -6);
        var highSpectrum = AudioAnalyzer.ComputeSpectrum(high, 44100, 1, 96, AmplitudeScaleMode.Normalized, FrequencyWeightingMode.Flat, FrequencyBinMode.Linear, -90, -6);

        var lowPeak = Array.IndexOf(lowSpectrum, lowSpectrum.Max());
        var highPeak = Array.IndexOf(highSpectrum, highSpectrum.Max());

        Assert.True(highPeak > lowPeak);
    }

    [Fact]
    public void ComputeSpectrum_LogBins_ShiftsPeakWithFrequency()
    {
        var low = TestSamples.GenerateSine(440, 44100, 4096);
        var high = TestSamples.GenerateSine(1760, 44100, 4096);

        var lowSpectrum = AudioAnalyzer.ComputeSpectrum(low, 44100, 1, 96, AmplitudeScaleMode.Normalized, FrequencyWeightingMode.Flat, FrequencyBinMode.Logarithmic, -90, -6);
        var highSpectrum = AudioAnalyzer.ComputeSpectrum(high, 44100, 1, 96, AmplitudeScaleMode.Normalized, FrequencyWeightingMode.Flat, FrequencyBinMode.Logarithmic, -90, -6);

        var lowPeak = Array.IndexOf(lowSpectrum, lowSpectrum.Max());
        var highPeak = Array.IndexOf(highSpectrum, highSpectrum.Max());

        Assert.True(highPeak > lowPeak);
    }

    [Fact]
    public void ComputeSpectrum_AWeighting_AttenuatesLowFrequency()
    {
        var low = TestSamples.GenerateSine(100, 44100, 4096);
        var high = TestSamples.GenerateSine(4000, 44100, 4096);

        var lowFlat = AudioAnalyzer.ComputeSpectrum(low, 44100, 1, 96, AmplitudeScaleMode.Normalized, FrequencyWeightingMode.Flat, FrequencyBinMode.Logarithmic, -90, -6).Max();
        var lowWeighted = AudioAnalyzer.ComputeSpectrum(low, 44100, 1, 96, AmplitudeScaleMode.Normalized, FrequencyWeightingMode.AWeighting, FrequencyBinMode.Logarithmic, -90, -6).Max();
        var highFlat = AudioAnalyzer.ComputeSpectrum(high, 44100, 1, 96, AmplitudeScaleMode.Normalized, FrequencyWeightingMode.Flat, FrequencyBinMode.Logarithmic, -90, -6).Max();
        var highWeighted = AudioAnalyzer.ComputeSpectrum(high, 44100, 1, 96, AmplitudeScaleMode.Normalized, FrequencyWeightingMode.AWeighting, FrequencyBinMode.Logarithmic, -90, -6).Max();

        Assert.True(lowWeighted < lowFlat);
        Assert.True(highWeighted >= highFlat);
    }

    [Fact]
    public void ComputeSpectrum_HigherAmplitudeProducesHigherPeak()
    {
        var lowAmplitude = TestSamples.GenerateSine(440, 44100, 4096, amplitude: 0.1f);
        var highAmplitude = TestSamples.GenerateSine(440, 44100, 4096, amplitude: 0.8f);

        var lowSpectrum = AudioAnalyzer.ComputeSpectrum(lowAmplitude, 44100, 1, 96, AmplitudeScaleMode.Normalized, FrequencyWeightingMode.Flat, FrequencyBinMode.Logarithmic, -90, -6);
        var highSpectrum = AudioAnalyzer.ComputeSpectrum(highAmplitude, 44100, 1, 96, AmplitudeScaleMode.Normalized, FrequencyWeightingMode.Flat, FrequencyBinMode.Logarithmic, -90, -6);

        Assert.True(highSpectrum.Max() > lowSpectrum.Max());
    }

    [Fact]
    public void ComputeSpectrum_BinModesYieldDifferentPeakPositions()
    {
        var samples = TestSamples.GenerateSine(600, 44100, 4096);

        var linear = AudioAnalyzer.ComputeSpectrum(samples, 44100, 1, 96, AmplitudeScaleMode.Normalized, FrequencyWeightingMode.Flat, FrequencyBinMode.Linear, -90, -6);
        var logarithmic = AudioAnalyzer.ComputeSpectrum(samples, 44100, 1, 96, AmplitudeScaleMode.Normalized, FrequencyWeightingMode.Flat, FrequencyBinMode.Logarithmic, -90, -6);

        var linearPeak = Array.IndexOf(linear, linear.Max());
        var logPeak = Array.IndexOf(logarithmic, logarithmic.Max());

        Assert.NotEqual(linearPeak, logPeak);
    }

    [Fact]
    public void ComputeSpectrum_MultiToneProducesMultipleStrongBins()
    {
        var toneA = TestSamples.GenerateSine(440, 44100, 4096, amplitude: 0.8f);
        var toneB = TestSamples.GenerateSine(880, 44100, 4096, amplitude: 0.8f);
        var mixed = TestSamples.Mix(toneA, toneB);

        var spectrum = AudioAnalyzer.ComputeSpectrum(mixed, 44100, 1, 96, AmplitudeScaleMode.Normalized, FrequencyWeightingMode.Flat, FrequencyBinMode.Logarithmic, -90, -6);

        var threshold = spectrum.Max() * 0.8f;
        var strongBins = spectrum.Count(value => value >= threshold);

        Assert.True(strongBins >= 2);
    }

    [Fact]
    public void ComputeSpectrum_DbRangeAffectsNormalization()
    {
        var samples = TestSamples.GenerateSine(440, 44100, 4096, amplitude: 0.5f);

        var narrowRange = AudioAnalyzer.ComputeSpectrum(samples, 44100, 1, 96, AmplitudeScaleMode.Dbfs, FrequencyWeightingMode.Flat, FrequencyBinMode.Logarithmic, -60, -6).Max();
        var wideRange = AudioAnalyzer.ComputeSpectrum(samples, 44100, 1, 96, AmplitudeScaleMode.Dbfs, FrequencyWeightingMode.Flat, FrequencyBinMode.Logarithmic, -90, -6).Max();

        Assert.True(wideRange > narrowRange);
    }
}

public class VisualizerStateTests
{
    [Fact]
    public void VisualizerState_PersistsSampleRate()
    {
        var state = new VisualizerState();
        state.Update(new[] { 0.1f, 0.2f }, sampleRate: 48000);

        Assert.Equal(48000, state.GetSampleRate());
    }

    [Fact]
    public void VisualizerState_StoresPeaksWhenProvided()
    {
        var state = new VisualizerState();
        var peaks = new[] { 0.8f, 0.4f };
        state.Update(new[] { 0.2f, 0.1f }, peaks, sampleRate: 44100);

        Assert.Equal(peaks, state.GetPeaks());
    }

    [Fact]
    public void VisualizerState_PreservesSampleRate_WhenNotProvided()
    {
        var state = new VisualizerState();
        state.Update(new[] { 0.2f, 0.1f }, sampleRate: 48000);
        state.Update(new[] { 0.4f, 0.3f });

        Assert.Equal(48000, state.GetSampleRate());
    }
}

static class TestSamples
{
    public static float[] GenerateSine(double frequency, int sampleRate, int length, float amplitude = 1f)
    {
        var samples = new float[length];
        var increment = 2 * Math.PI * frequency / sampleRate;

        for (var i = 0; i < length; i++)
        {
            samples[i] = amplitude * (float)Math.Sin(i * increment);
        }

        return samples;
    }

    public static float[] Mix(params float[][] buffers)
    {
        var length = buffers.Min(buffer => buffer.Length);
        var mixed = new float[length];

        for (var i = 0; i < length; i++)
        {
            var sum = 0f;
            foreach (var buffer in buffers)
            {
                sum += buffer[i];
            }

            mixed[i] = sum / buffers.Length;
        }

        return mixed;
    }
}
