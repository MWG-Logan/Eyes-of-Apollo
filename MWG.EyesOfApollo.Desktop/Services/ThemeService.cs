using System.Text.Json;
using MWG.EyesOfApollo.Desktop.Models;

namespace MWG.EyesOfApollo.Desktop.Services
{
    public class ThemeService
    {
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public string ThemesDirectory => Path.Combine(AppContext.BaseDirectory, "Themes");

        public async Task<IReadOnlyList<ThemeDefinition>> LoadThemesAsync()
        {
            Directory.CreateDirectory(ThemesDirectory);
            var files = Directory.GetFiles(ThemesDirectory, "*.json", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                var defaultTheme = CreateDefaultTheme();
                var filePath = Path.Combine(ThemesDirectory, "aurora.json");
                await SaveThemeAsync(filePath, defaultTheme);
                files = new[] { filePath };
            }

            var themes = new List<ThemeDefinition>();
            foreach (var file in files)
            {
                await using var stream = File.OpenRead(file);
                var theme = await JsonSerializer.DeserializeAsync<ThemeDefinition>(stream, _serializerOptions);
                if (theme != null)
                {
                    if (string.IsNullOrWhiteSpace(theme.Name))
                    {
                        theme.Name = Path.GetFileNameWithoutExtension(file);
                    }

                    themes.Add(theme);
                }
            }

            return themes;
        }

        public async Task SaveThemeAsync(string filePath, ThemeDefinition theme)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, theme, _serializerOptions);
        }

        private static ThemeDefinition CreateDefaultTheme()
        {
            return new ThemeDefinition
            {
                Name = "Aurora",
                Background = "#0B0D17",
                Primary = "#6C63FF",
                Secondary = "#F5A623",
                Mode = VisualizerMode.Bars,
                BarCount = 64,
                LineThickness = 2f,
                BarSpacing = 2f
            };
        }
    }
}
