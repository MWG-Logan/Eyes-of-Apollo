using System.Numerics;

namespace MWG.EyesOfApollo.Desktop.Services
{
    public static class AudioAnalyzer
    {
        public static float[] ComputeSpectrum(float[] samples, int binCount)
        {
            if (samples.Length == 0 || binCount <= 0)
            {
                return Array.Empty<float>();
            }

            var fftSize = NextPowerOfTwo(Math.Min(samples.Length, 2048));
            var buffer = new Complex[fftSize];
            for (var i = 0; i < fftSize; i++)
            {
                var sample = i < samples.Length ? samples[i] : 0f;
                var window = 0.5f * (1f - MathF.Cos(2f * MathF.PI * i / (fftSize - 1)));
                buffer[i] = new Complex(sample * window, 0);
            }

            FFT(buffer);

            var magnitudes = new float[binCount];
            var maxIndex = fftSize / 2;
            var stride = Math.Max(1, maxIndex / binCount);

            for (var bin = 0; bin < binCount; bin++)
            {
                var start = bin * stride;
                var end = Math.Min(maxIndex, start + stride);
                var sum = 0d;

                for (var i = start; i < end; i++)
                {
                    var value = buffer[i].Magnitude;
                    sum += value;
                }

                var average = sum / Math.Max(1, end - start);
                magnitudes[bin] = (float)Math.Clamp(average * 4, 0, 1);
            }

            return magnitudes;
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
    }
}
