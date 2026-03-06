namespace MWG.EyesOfApollo.Desktop.Rendering
{
    /// <summary>
    /// Thread-safe storage for the latest visualization data.
    /// </summary>
    public class VisualizerState
    {
        private readonly object _syncRoot = new();
        private float[] _magnitudes = Array.Empty<float>();
        private float[] _peaks = Array.Empty<float>();
        private int _sampleRate;

        /// <summary>
        /// Updates the stored magnitudes, optional peaks, and sample rate.
        /// </summary>
        /// <param name="magnitudes">The latest magnitudes.</param>
        /// <param name="peaks">Optional peak values.</param>
        /// <param name="sampleRate">Optional sample rate.</param>
        public void Update(float[] magnitudes, float[]? peaks = null, int sampleRate = 0)
        {
            lock (_syncRoot)
            {
                _magnitudes = magnitudes;
                _peaks = peaks ?? Array.Empty<float>();
                if (sampleRate > 0)
                {
                    _sampleRate = sampleRate;
                }
            }
        }

        /// <summary>
        /// Gets a copy of the latest magnitudes.
        /// </summary>
        public float[] GetSnapshot()
        {
            lock (_syncRoot)
            {
                return _magnitudes.ToArray();
            }
        }

        /// <summary>
        /// Gets a copy of the latest peaks.
        /// </summary>
        public float[] GetPeaks()
        {
            lock (_syncRoot)
            {
                return _peaks.ToArray();
            }
        }

        /// <summary>
        /// Gets the latest sample rate.
        /// </summary>
        public int GetSampleRate()
        {
            lock (_syncRoot)
            {
                return _sampleRate;
            }
        }
    }
}
