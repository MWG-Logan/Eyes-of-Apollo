namespace MWG.EyesOfApollo.Desktop.Rendering
{
    public class VisualizerState
    {
        private readonly object _syncRoot = new();
        private float[] _magnitudes = Array.Empty<float>();
        private float[] _peaks = Array.Empty<float>();
        private int _sampleRate;

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

        public float[] GetSnapshot()
        {
            lock (_syncRoot)
            {
                return _magnitudes.ToArray();
            }
        }

        public float[] GetPeaks()
        {
            lock (_syncRoot)
            {
                return _peaks.ToArray();
            }
        }

        public int GetSampleRate()
        {
            lock (_syncRoot)
            {
                return _sampleRate;
            }
        }
    }
}
