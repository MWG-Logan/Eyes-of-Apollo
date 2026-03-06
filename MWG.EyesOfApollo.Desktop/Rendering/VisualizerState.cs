namespace MWG.EyesOfApollo.Desktop.Rendering
{
    public class VisualizerState
    {
        private readonly object _syncRoot = new();
        private float[] _magnitudes = Array.Empty<float>();

        public void Update(float[] magnitudes)
        {
            lock (_syncRoot)
            {
                _magnitudes = magnitudes;
            }
        }

        public float[] GetSnapshot()
        {
            lock (_syncRoot)
            {
                return _magnitudes.ToArray();
            }
        }
    }
}
