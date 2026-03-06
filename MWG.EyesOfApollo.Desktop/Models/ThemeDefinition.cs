namespace MWG.EyesOfApollo.Desktop.Models
{
    /// <summary>
    /// Defines a theme used by the visualizer.
    /// </summary>
    public sealed class ThemeDefinition
    {
        /// <summary>
        /// Gets or sets the theme name.
        /// </summary>
        public string Name { get; set; } = "Aurora";
        /// <summary>
        /// Gets or sets the background color (hex).
        /// </summary>
        public string Background { get; set; } = "#0B0D17";
        /// <summary>
        /// Gets or sets the primary accent color (hex).
        /// </summary>
        public string Primary { get; set; } = "#6C63FF";
        /// <summary>
        /// Gets or sets the secondary accent color (hex).
        /// </summary>
        public string Secondary { get; set; } = "#F5A623";
        /// <summary>
        /// Gets or sets the default visualization mode.
        /// </summary>
        public VisualizerMode Mode { get; set; } = VisualizerMode.Bars;
        /// <summary>
        /// Gets or sets the default bar count.
        /// </summary>
        public int BarCount { get; set; } = 96;
        /// <summary>
        /// Gets or sets the line thickness when using line mode.
        /// </summary>
        public float LineThickness { get; set; } = 2f;
        /// <summary>
        /// Gets or sets the spacing between bars.
        /// </summary>
        public float BarSpacing { get; set; } = 2f;

        /// <summary>
        /// Gets the parsed background color.
        /// </summary>
        public Color BackgroundColor => Color.FromArgb(Background);
        /// <summary>
        /// Gets the parsed primary color.
        /// </summary>
        public Color PrimaryColor => Color.FromArgb(Primary);
        /// <summary>
        /// Gets the parsed secondary color.
        /// </summary>
        public Color SecondaryColor => Color.FromArgb(Secondary);
    }
}
