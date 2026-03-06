namespace MWG.EyesOfApollo.Desktop.Models
{
    public sealed class AudioDeviceInfo
    {
        public AudioDeviceInfo(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public string Id { get; }
        public string DisplayName { get; }

        public override string ToString() => DisplayName;
    }
}
