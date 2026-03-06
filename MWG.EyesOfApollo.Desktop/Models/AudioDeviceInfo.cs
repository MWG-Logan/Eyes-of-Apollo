namespace MWG.EyesOfApollo.Desktop.Models
{
    /// <summary>
    /// Represents an audio device available for capture.
    /// </summary>
    public sealed class AudioDeviceInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioDeviceInfo"/> class.
        /// </summary>
        /// <param name="id">The device identifier.</param>
        /// <param name="displayName">The display name shown to the user.</param>
        public AudioDeviceInfo(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        /// <summary>
        /// Gets the device identifier.
        /// </summary>
        public string Id { get; }
        /// <summary>
        /// Gets the display name shown to the user.
        /// </summary>
        public string DisplayName { get; }

        /// <inheritdoc />
        public override string ToString() => DisplayName;
    }
}
