using System.Numerics;
using MWG.EyesOfApollo.Desktop.Models;

namespace MWG.EyesOfApollo.Desktop.Services
{
    public static class AudioAnalyzer
    {
        public static float[] ComputeSpectrum(float[] samples, int sampleRate, int channels, int binCount, AmplitudeScaleMode scaleMode, FrequencyWeightingMode weightingMode)
        {
            if (samples.Length == 0 || binCount <= 0)
            {
                return Array.Empty<float>();
            }

            var monoSamples = channels > 1 ? DownmixToMono(samples, channels) : samples;

            var fftSize = NextPowerOfTwo(Math.Min(monoSamples.Length, 2048));
            var buffer = new Complex[fftSize];
            for (var i = 0; i < fftSize; i++)
            {
                var sample = i < monoSamples.Length ? monoSamples[i] : 0f;
                var window = 0.5f * (1f - MathF.Cos(2f * MathF.PI * i / (fftSize - 1)));
                buffer[i] = new Complex(sample * window, 0);
            }

            FFT(buffer);

            var magnitudes = new float[binCount];
            var maxIndex = fftSize / 2;
            var minFrequency = 20d;
            var maxFrequency = Math.Min(20000d, sampleRate / 2d);
            var logMin = Math.Log10(minFrequency);
            var logMax = Math.Log10(maxFrequency);

            for (var bin = 0; bin < binCount; bin++)
            {
                var startLog = logMin + (logMax - logMin) * (bin / (double)binCount);
                var endLog = logMin + (logMax - logMin) * ((bin + 1) / (double)binCount);
                var startFreq = Math.Pow(10, startLog);
                var endFreq = Math.Pow(10, endLog);

                var startIndex = (int)Math.Clamp(Math.Floor(startFreq * fftSize / sampleRate), 1, maxIndex - 1);
                var endIndex = (int)Math.Clamp(Math.Ceiling(endFreq * fftSize / sampleRate), startIndex + 1, maxIndex);

                var sum = 0d;
                var centerFrequency = Math.Sqrt(startFreq * endFreq);
                var weight = weightingMode == FrequencyWeightingMode.AWeighting
                    ? DbToLinear(AWeightingDb(centerFrequency))
                    : 1d;
                for (var i = startIndex; i < endIndex; i++)
                {
                    sum += buffer[i].Magnitude;
                }

                var average = (sum / Math.Max(1, endIndex - startIndex)) * weight;
                magnitudes[bin] = scaleMode == AmplitudeScaleMode.Dbfs
                    ? ToDbfsNormalized(average)
                    : (float)Math.Clamp(average * 4, 0, 1);
            }

            return magnitudes;
        }

        private static float ToDbfsNormalized(double magnitude)
        {
            if (magnitude <= 0)
            {
                return 0f;
            }

            var db = 20 * Math.Log10(magnitude);
            const double minDb = -80;
            var normalized = (db - minDb) / (0 - minDb);
            return (float)Math.Clamp(normalized, 0, 1);
        }

        private static float[] DownmixToMono(float[] samples, int channels)
        {
            var sampleFrames = samples.Length / channels;
            var mono = new float[sampleFrames];
            var index = 0;

            for (var frame = 0; frame < sampleFrames; frame++)
            {
                var sum = 0f;
                for (var channel = 0; channel < channels; channel++)
                {
                    sum += samples[index++];
                }

                mono[frame] = sum / channels;
            }

            return mono;
        }

        private static int NextPowerOfTwo(int value)
        {
            var power = 1;
            while (power < value)
            {
                power <<= 1;
            }

            return power;
        }

        private static void FFT(Complex[] buffer)
        {
            var bits = (int)Math.Log2(buffer.Length);
            for (var j = 1; j < buffer.Length - 1; j++)
            {
                var swapPos = BitReverse(j, bits);
                if (swapPos <= j)
                {
                    continue;
                }

                (buffer[j], buffer[swapPos]) = (buffer[swapPos], buffer[j]);
            }

            for (var n = 2; n <= buffer.Length; n <<= 1)
            {
                var angle = -2 * Math.PI / n;
                var wN = new Complex(Math.Cos(angle), Math.Sin(angle));

                for (var i = 0; i < buffer.Length; i += n)
                {
                    var w = Complex.One;
                    for (var j = 0; j < n / 2; j++)
                    {
                        var even = buffer[i + j];
                        var odd = w * buffer[i + j + n / 2];
                        buffer[i + j] = even + odd;
                        buffer[i + j + n / 2] = even - odd;
                        w *= wN;
                    }
                }
            }
        }

        private static int BitReverse(int value, int bits)
        {
            var reversed = 0;
            for (var i = 0; i < bits; i++)
            {
                reversed = (reversed << 1) | (value & 1);
                value >>= 1;
            }

            return reversed;
        }

        private static double AWeightingDb(double frequency)
        {
            var f2 = frequency * frequency;
            var ra = (Math.Pow(12200, 2) * Math.Pow(f2, 2)) /
                     ((f2 + Math.Pow(20.6, 2)) * (f2 + Math.Pow(12200, 2)) *
                      Math.Sqrt((f2 + Math.Pow(107.7, 2)) * (f2 + Math.Pow(737.9, 2))));

            return 2.0 + 20 * Math.Log10(ra);
        }

        private static double DbToLinear(double db)
        {
            return Math.Pow(10, db / 20);
        }
    }
}
