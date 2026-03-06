namespace MWG.EyesOfApollo.Desktop.Rendering
{
    public class VisualizerState
    {
        private readonly object _syncRoot = new();
        private float[] _magnitudes = Array.Empty<float>();
        private float[] _peaks = Array.Empty<float>();

        public void Update(float[] magnitudes, float[]? peaks = null)
        {
            lock (_syncRoot)
            {
                _magnitudes = magnitudes;
                _peaks = peaks ?? Array.Empty<float>();
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
    }
}
