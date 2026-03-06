namespace MWG.EyesOfApollo.Desktop.Models
{
    /// <summary>
    /// Defines frequency weighting applied to spectrum magnitudes.
    /// </summary>
    public enum FrequencyWeightingMode
    {
        /// <summary>
        /// No weighting (flat response).
        /// </summary>
        Flat,
        /// <summary>
        /// A-weighted response for perceptual alignment.
        /// </summary>
        AWeighting
    }
}
