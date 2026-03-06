using Microsoft.Maui.Graphics;

namespace MWG.EyesOfApollo.Desktop.Models
{
    public sealed class ThemeDefinition
    {
        public string Name { get; set; } = "Aurora";
        public string Background { get; set; } = "#0B0D17";
        public string Primary { get; set; } = "#6C63FF";
        public string Secondary { get; set; } = "#F5A623";
        public VisualizerMode Mode { get; set; } = VisualizerMode.Bars;
        public int BarCount { get; set; } = 96;
        public float LineThickness { get; set; } = 2f;
        public float BarSpacing { get; set; } = 2f;

        public Color BackgroundColor => Color.FromArgb(Background);
        public Color PrimaryColor => Color.FromArgb(Primary);
        public Color SecondaryColor => Color.FromArgb(Secondary);
    }
}
